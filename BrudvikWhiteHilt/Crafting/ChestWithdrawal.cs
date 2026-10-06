using BrudvikWhiteHilt.Chests;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Fetches what a crafting or a piece lacks from the chests around the local player into the player's inventory,
/// without moving the chests: the machine that holds a chest takes the items out and sends them, the way the game
/// lets only a chest's owner change it. Two players using the same chests at once therefore never pull them back and
/// forth. A chest this machine holds is taken from at once. When everything asked for has arrived, the crafting or
/// placing the player clicked goes on by itself.
/// </summary>
public static class ChestWithdrawal
{
    /// <summary>Name of the request RPC on a container's network object, answered by its owner.</summary>
    internal const string RequestRpc = "WhiteHilt_Withdraw";

    private const string ReplyRpc = "WhiteHilt_Withdrawn";
    private const int MaxItemsPerRequest = 32;
    private const int MaxAmount = 100000;
    private const int MaxTakeRounds = 100;
    private const int MaxAttempts = 3;
    private const int MaxPending = 200;
    private const int ScratchWidth = 8;
    private const int ScratchHeight = 64;

    private static readonly Dictionary<long, Request> pending = new();
    private static ZRoutedRpc registeredFor;
    private static long nextRequestId;
    private static Action continuation;
    private static float startedAt;

    /// <summary>
    /// True while the crafting or placing goes on after the fetch: the items are in the inventory now, so nothing is
    /// fetched again and only the inventory counts.
    /// </summary>
    public static bool Continuing { get; private set; }

    /// <summary>
    /// True while a fetch the player is waiting for is under way.
    /// </summary>
    public static bool Busy => continuation != null;

    /// <summary>
    /// Fetches what the inventory lacks of each requirement from the chests in range, nearest first. Chests held here
    /// are taken from at once; the others are asked for, and <paramref name="then"/> runs when they have answered.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="use">Crafting or building, which decides the range.</param>
    /// <param name="requirements">What is needed, with the amount each wants.</param>
    /// <param name="then">What to do once the items have arrived, e.g. craft again.</param>
    /// <returns>True if the player must wait for other players' chests.</returns>
    public static bool Fetch(Player player, NearbyContainers.Use use, IEnumerable<(string Name, int Amount)> requirements, Action then)
    {
        if (player == null || !NearbyContainers.IsActive(use))
        {
            return false;
        }

        EnsureRegistered();
        Dictionary<ZNetView, List<(string Name, int Amount)>> plan = Plan(player, use, requirements);
        if (plan.Count == 0)
        {
            return false;
        }

        long playerId = player.GetPlayerID();
        bool waiting = false;
        foreach (KeyValuePair<ZNetView, List<(string Name, int Amount)>> entry in plan)
        {
            ZNetView view = entry.Key;
            if (!view.GetZDO().HasOwner())
            {
                view.ClaimOwnership();
            }

            if (view.IsOwner())
            {
                Inventory scratch = Scratch();
                Extract(view, entry.Value, playerId, scratch);
                Deliver(view, scratch);
                continue;
            }

            Send(new Request(++nextRequestId, view, entry.Value, playerId));
            waiting = true;
        }

        NearbyContainers.ForgetCounts();
        if (!waiting)
        {
            return false;
        }

        continuation = then;
        startedAt = Time.time;
        return true;
    }

    /// <summary>
    /// Gives up on chests that have not answered within <c>HandoffTimeout</c> and goes on with what has arrived.
    /// Call once a frame for the local player.
    /// </summary>
    public static void Tick()
    {
        if (continuation == null || Time.time - startedAt < NearbyContainers.HandoffTimeout)
        {
            return;
        }

        int unanswered = pending.Values.Count(request => !request.Abandoned);
        foreach (Request request in pending.Values)
        {
            request.Abandoned = true;
        }

        Jotunn.Logger.LogWarning($"{unanswered} chests did not answer within {NearbyContainers.HandoffTimeout:0.#} s; going on with what has arrived.");
        Continue();
    }

    /// <summary>
    /// On the owner of a container: takes what another player asks for out of the containers on the network object,
    /// saves them and sends the items back. A machine that does not own it answers so, and the asker tries again.
    /// </summary>
    /// <param name="view">The containers' network object.</param>
    /// <param name="sender">The asking peer.</param>
    /// <param name="package">The request.</param>
    internal static void Serve(ZNetView view, long sender, ZPackage package)
    {
        long id = package.ReadLong();
        long playerId = package.ReadLong();
        int count = package.ReadInt();
        if (count < 0 || count > MaxItemsPerRequest)
        {
            return;
        }

        List<(string Name, int Amount)> wanted = new();
        for (int i = 0; i < count; i++)
        {
            string name = package.ReadString();
            int amount = package.ReadInt();
            if (!string.IsNullOrEmpty(name) && amount > 0 && amount <= MaxAmount)
            {
                wanted.Add((name, amount));
            }
        }

        EnsureRegistered();
        bool served = view != null && view.IsValid() && view.IsOwner();
        ZPackage items = new();
        if (served)
        {
            Inventory scratch = Scratch();
            Extract(view, wanted, playerId, scratch);
            scratch.Save(items);
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, id, served, items);
    }

    // How much to ask each chest for: what the inventory lacks of each requirement, from the nearest chests that have
    // it. Chests on one network object (a ship's hold and sea chest) are asked together.
    private static Dictionary<ZNetView, List<(string Name, int Amount)>> Plan(Player player, NearbyContainers.Use use,
        IEnumerable<(string Name, int Amount)> requirements)
    {
        Dictionary<ZNetView, List<(string Name, int Amount)>> plan = new();
        foreach (IGrouping<string, (string Name, int Amount)> group in requirements.Where(requirement => requirement.Amount > 0).GroupBy(requirement => requirement.Name))
        {
            int missing = group.Sum(requirement => requirement.Amount) - player.GetInventory().CountItems(group.Key);
            if (missing <= 0 || NearbyContainers.IsExcluded(group.Key))
            {
                continue;
            }

            foreach ((Container container, int available) in NearbyContainers.Suppliers(use, group.Key))
            {
                int take = Mathf.Min(missing, available);
                if (!plan.TryGetValue(container.m_nview, out List<(string Name, int Amount)> list))
                {
                    list = new List<(string Name, int Amount)>();
                    plan[container.m_nview] = list;
                }

                list.Add((group.Key, take));
                missing -= take;
                if (missing <= 0)
                {
                    break;
                }
            }
        }

        return plan;
    }

    // Takes the wanted items out of the containers on a network object this machine owns into the scratch inventory,
    // and saves what changed. Nothing is taken for a player who may not open them (a private chest of another).
    private static void Extract(ZNetView view, List<(string Name, int Amount)> wanted, long playerId, Inventory scratch)
    {
        List<Container> containers = view.GetComponentsInChildren<Container>(true).Where(container => container.m_nview == view).ToList();
        if (containers.Count == 0 || containers.Any(container => !container.CheckAccess(playerId)))
        {
            return;
        }

        HashSet<Container> changed = new();
        foreach ((string name, int amount) in wanted)
        {
            if (NearbyContainers.IsExcluded(name))
            {
                continue;
            }

            int taken = 0;
            foreach (Container container in containers)
            {
                int from = ExtractFrom(container, name, amount - taken, scratch);
                if (from > 0)
                {
                    changed.Add(container);
                    taken += from;
                }

                if (taken >= amount)
                {
                    break;
                }
            }
        }

        foreach (Container container in changed)
        {
            container.Save();
        }
    }

    // Moves up to amount of an item from a container into the scratch inventory, stack by stack, keeping each stack's
    // own data (quality, stars, crafter). A stack the chest keeps without limit refills as soon as it is taken from, so
    // it is taken from again.
    private static int ExtractFrom(Container container, string name, int amount, Inventory scratch)
    {
        Inventory inventory = container.GetInventory();
        if (inventory == null)
        {
            return 0;
        }

        int taken = 0;
        for (int round = 0; round < MaxTakeRounds && taken < amount; round++)
        {
            int available = inventory.CountItems(name) - (NearbyContainers.LeaveOne ? 1 : 0);
            ItemDrop.ItemData stack = inventory.GetAllItems().FirstOrDefault(item => item.m_shared.m_name == name);
            if (available <= 0 || stack == null)
            {
                break;
            }

            int take = Mathf.Min(amount - taken, Mathf.Min(stack.m_stack, available));
            ItemDrop.ItemData copy = stack.Clone();
            copy.m_stack = take;
            if (!scratch.AddItem(copy))
            {
                break;
            }

            inventory.RemoveItem(stack, take);
            taken += take;
        }

        return taken;
    }

    // On the asking machine: puts the items that arrived into the inventory, or at the player's feet when it is full.
    private static void Deliver(ZNetView view, Inventory scratch)
    {
        Player player = Player.m_localPlayer;
        List<ItemDrop.ItemData> items = scratch.GetAllItems().ToList();
        if (items.Count == 0)
        {
            return;
        }

        if (player == null)
        {
            Jotunn.Logger.LogWarning($"{items.Count} stacks from a chest arrived with no player to take them.");
            return;
        }

        Inventory inventory = player.GetInventory();
        foreach (ItemDrop.ItemData item in items)
        {
            if (!inventory.AddItem(item))
            {
                ItemDrop.DropItem(item, item.m_stack, player.transform.position + player.transform.forward + Vector3.up, Quaternion.identity);
            }
        }

        Container shown = view != null && view.IsValid() ? view.GetComponentInChildren<Container>() : null;
        if (shown != null && NearbyContainers.ShowTakenChests)
        {
            ContainerPulse.Play(shown);
        }

        NearbyContainers.ForgetCounts();
    }

    private static void Send(Request request)
    {
        if (pending.Count >= MaxPending)
        {
            pending.Clear();
        }

        pending[request.Id] = request;
        ZPackage package = new();
        package.Write(request.Id);
        package.Write(request.PlayerId);
        package.Write(request.Wanted.Count);
        foreach ((string name, int amount) in request.Wanted)
        {
            package.Write(name);
            package.Write(amount);
        }

        request.Attempts++;
        request.View.InvokeRPC(RequestRpc, package);
    }

    // On the asking machine. A reply from a machine that no longer owned the chest is sent again, to the owner now;
    // items that arrive after the wait was given up are still put in the inventory, as they left the chest.
    private static void OnReply(long sender, long id, bool served, ZPackage items)
    {
        if (!pending.TryGetValue(id, out Request request))
        {
            return;
        }

        if (!served && !request.Abandoned && request.Attempts < MaxAttempts && request.View != null && request.View.IsValid())
        {
            Send(request);
            return;
        }

        pending.Remove(id);
        if (served)
        {
            Inventory scratch = Scratch();
            scratch.Load(items);
            Deliver(request.View, scratch);
        }

        if (!pending.Values.Any(other => !other.Abandoned))
        {
            Continue();
        }
    }

    // Runs what the player clicked once more, now that the items are in the inventory.
    private static void Continue()
    {
        Action then = continuation;
        continuation = null;
        if (then == null)
        {
            return;
        }

        Continuing = true;
        try
        {
            then();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"Could not go on after fetching from the chests: {ex}");
        }
        finally
        {
            Continuing = false;
        }
    }

    private static Inventory Scratch()
    {
        return new Inventory("WhiteHiltWithdrawal", null, ScratchWidth, ScratchHeight);
    }

    // Replies come through a routed RPC to the asking peer, so they arrive even if the chest has unloaded meanwhile.
    private static void EnsureRegistered()
    {
        if (ZRoutedRpc.instance == null || registeredFor == ZRoutedRpc.instance)
        {
            return;
        }

        ZRoutedRpc.instance.Register<long, bool, ZPackage>(ReplyRpc, OnReply);
        registeredFor = ZRoutedRpc.instance;
        pending.Clear();
        continuation = null;
    }

    private sealed class Request
    {
        public Request(long id, ZNetView view, List<(string Name, int Amount)> wanted, long playerId)
        {
            Id = id;
            View = view;
            Wanted = wanted;
            PlayerId = playerId;
        }

        public long Id { get; }

        public ZNetView View { get; }

        public List<(string Name, int Amount)> Wanted { get; }

        public long PlayerId { get; }

        public int Attempts { get; set; }

        public bool Abandoned { get; set; }
    }
}
