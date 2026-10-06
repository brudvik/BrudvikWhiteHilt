using BrudvikWhiteHilt.Chests.Helpers;
using BrudvikWhiteHilt.Crafting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Collection;

/// <summary>A collector that sorts loose items and the contents of its own basket into White Hilt chests.</summary>
internal sealed class CollectionPostComponent : MonoBehaviour
{
    private const string PausedKey = "whitehilt_collection_paused";
    private const string StuckKey = "whitehilt_collection_stuck";
    private const string PulseRpc = "WhiteHilt_CollectionPulse";
    private const string SortedEffect = "vfx_pick_wisp";
    private const float GlowIntensity = 0.5f;
    private const float GlowRange = 2f;
    private const float FlareSeconds = 1.2f;
    private const float LastSortedShownSeconds = 120f;
    private static readonly Color IdleColor = new(1f, 0.76f, 0.3f);
    private static readonly Color StuckColor = new(1f, 0.3f, 0.15f);
    private static readonly Vector3 BasketCenter = new(0f, 0.3f, 0.65f);
    private static readonly HashSet<CollectionPostComponent> posts = new();
    internal static ChestModule Module;
    private ZNetView view;
    private Container basket;
    private Light glow;
    private float nextRound;
    private int cursor;
    private int basketCursor;
    private float flare;
    private int lastSorted;
    private float lastSortedAt = -1f;

    // Basket stacks the last attempt could place nowhere; only the basket owner keeps them.
    private readonly HashSet<ItemDrop.ItemData> stuck = new();

    // What this round moved, announced to everyone once the round is over.
    private int roundMoved;
    private readonly HashSet<ZDOID> roundChests = new();

    private bool Active => CollectionSettings.Enabled.Value && view != null && view.IsValid() && !view.GetZDO().GetBool(PausedKey);

    /// <summary>Status lines and the pause key, appended to the basket's hover text.</summary>
    /// <returns>The localized lines.</returns>
    internal string GetHoverText()
    {
        var localization = Localization.instance;
        var lines = new List<string>();
        int chests = Player.m_localPlayer != null ? Receivers(Player.m_localPlayer).Count : 0;
        if (!Active) lines.Add(localization.Localize("$whitehilt_collection_paused"));
        else if (chests == 0) lines.Add("<color=orange>" + localization.Localize("$whitehilt_collection_nochests") + "</color>");
        else lines.Add(Format("$whitehilt_collection_active", chests));
        int stuckStacks = view != null && view.IsValid() ? view.GetZDO().GetInt(StuckKey) : 0;
        if (Active && stuckStacks > 0) lines.Add("<color=orange>" + Format("$whitehilt_collection_stuck", stuckStacks) + "</color>");
        if (lastSortedAt >= 0f && Time.time - lastSortedAt < LastSortedShownSeconds)
            lines.Add(Format("$whitehilt_collection_last", lastSorted, Mathf.FloorToInt(Time.time - lastSortedAt)));
        lines.Add(localization.Localize("$whitehilt_collection_ranges"));
        lines.Add(localization.Localize("[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_collection_toggle"));
        return "\n" + string.Join("\n", lines);
    }

    private static string Format(string token, params object[] values) =>
        string.Format(CultureInfo.CurrentCulture, Localization.instance.Localize(token), values);

    /// <summary>Pauses or resumes the post through its owner.</summary>
    internal void RequestToggle()
    {
        if (view != null && view.IsValid() && PrivateArea.CheckAccess(transform.position)) view.InvokeRPC("WhiteHilt_CollectionToggle");
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        basket = GetComponent<Container>();
        glow = GetComponentInChildren<Light>();
        if (view == null || !view.IsValid()) return;
        posts.Add(this);
        view.Register("WhiteHilt_CollectionToggle", Toggle);
        view.Register<ZPackage>(PulseRpc, Pulse);
    }

    private void OnDestroy() => posts.Remove(this);

    private void Toggle(long sender)
    {
        if (view.IsOwner()) view.GetZDO().Set(PausedKey, !view.GetZDO().GetBool(PausedKey));
    }

    // Runs a collection round now and then. Only the player nearest the post collects drops, so two players do not
    // collect the same drop; the post's owner empties its basket.
    private void Update()
    {
        UpdateGlow();
        var player = Player.m_localPlayer;
        if (player == null || Module == null || !Active || Time.time < nextRound) return;
        nextRound = Time.time + CollectionSettings.Interval.Value;
        if (!PrivateArea.CheckAccess(transform.position, 0f, false)) return;
        var nearest = Player.GetAllPlayers().OrderBy(candidate => (candidate.transform.position - transform.position).sqrMagnitude)
            .ThenBy(candidate => candidate.GetPlayerID()).FirstOrDefault();
        bool collects = nearest == player;
        if (!collects && !view.IsOwner()) return;

        roundMoved = 0;
        roundChests.Clear();
        var receivers = Receivers(player);
        EmptyBasket(player, receivers);
        if (collects && receivers.Count > 0) CollectDrops(player, receivers);
        if (roundMoved > 0) Announce();
    }

    // Brightens briefly after each delivery; turns red and breathes while basket stacks fit in no chest.
    private void UpdateGlow()
    {
        if (glow == null) return;
        bool active = Active;
        glow.enabled = active;
        if (!active) return;
        flare = Mathf.Max(0f, flare - Time.deltaTime / FlareSeconds);
        bool stuckItems = view.GetZDO().GetInt(StuckKey) > 0;
        float breath = stuckItems ? 0.75f + 0.25f * Mathf.Sin(Time.time * 3f) : 1f;
        glow.color = Color.Lerp(stuckItems ? StuckColor : IdleColor, Color.white, flare * 0.5f);
        glow.intensity = GlowIntensity * breath + flare * 2.5f;
        glow.range = GlowRange + flare * 3f;
    }

    // Moves loose drops within reach into chests that take them, a limited number per round so a large pile is done
    // over several rounds. A drop near several posts goes to the nearest, so posts do not compete for it.
    private void CollectDrops(Player player, List<Container> receivers)
    {
        var receiversByPost = new Dictionary<CollectionPostComponent, List<Container>> { [this] = receivers };
        List<Container> ReceiversOf(CollectionPostComponent post)
        {
            if (!receiversByPost.TryGetValue(post, out var list)) receiversByPost[post] = list = post.Receivers(player);
            return list;
        }

        float radius = CollectionSettings.Radius.Value * CollectionSettings.Radius.Value;
        var drops = new List<ItemDrop>(ItemDrop.s_instances);
        int processed = 0;
        int examined = 0;
        while (examined < drops.Count && examined < CollectionSettings.ScanLimit.Value && processed < CollectionSettings.BatchSize.Value)
        {
            var drop = drops[(cursor + examined) % drops.Count];
            examined++;
            if (drop == null || drop.IsPiece()
                || drop.GetComponent<Fish>() != null || drop.InTar() || drop.m_nview == null || !drop.m_nview.IsValid()
                || (drop.transform.position - transform.position).sqrMagnitude > radius
                || !PrivateArea.CheckAccess(drop.transform.position, 0f, false)) continue;
            if (!CollectionSettings.PlayerDrops.Value && (!drop.m_autoPickup || drop.m_nview.GetZDO().GetBool("whitehilt_player_drop"))) continue;
            var chosen = posts.Where(post => post != null && post.Active
                    && (post.transform.position - drop.transform.position).sqrMagnitude <= radius
                    && PrivateArea.CheckAccess(post.transform.position, 0f, false)
                    && ReceiversOf(post).Any(chest => Module.CollectionPriority(chest, drop.m_itemData) >= 0))
                .OrderBy(post => (post.transform.position - drop.transform.position).sqrMagnitude)
                .ThenBy(post => post.view.GetZDO().m_uid.ToString()).FirstOrDefault();
            if (chosen != this) continue;
            processed++;
            if (!drop.CanPickup())
            {
                drop.RequestOwn();
                continue;
            }
            drop.Load();
            Deliver(drop.m_itemData, drop.transform.position, receivers, player.GetPlayerID(), () => drop.CanPickup() && Active, amount =>
            {
                drop.m_itemData.m_stack -= amount;
                if (drop.m_itemData.m_stack > 0) { drop.Save(); return true; }
                drop.m_nview.Destroy();
                return false;
            }, out _);
        }
        cursor = drops.Count == 0 ? 0 : (cursor + examined) % drops.Count;
    }

    // The basket owner sorts it, only while nobody has it open, a batch of stacks per round in turn, so stacks that
    // fit nowhere never keep the others waiting. What fits nowhere stays for the player to take back.
    private void EmptyBasket(Player player, List<Container> receivers)
    {
        if (basket == null || !view.IsOwner() || BasketOpen()) return;
        var inventory = basket.GetInventory();
        if (inventory == null) return;
        var items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
        stuck.RemoveWhere(item => !items.Contains(item));
        if (receivers.Count == 0) stuck.UnionWith(items);
        else if (items.Count > 0)
        {
            int start = basketCursor % items.Count;
            int examined = 0;
            while (examined < items.Count && examined < CollectionSettings.BatchSize.Value)
            {
                var item = items[(start + examined) % items.Count];
                examined++;
                Deliver(item, transform.position, receivers, player.GetPlayerID(), () => view.IsOwner() && !BasketOpen() && Active, amount =>
                {
                    bool all = amount >= item.m_stack;
                    inventory.RemoveItem(item, amount);
                    return !all;
                }, out bool waiting);
                if (!waiting && inventory.ContainsItem(item)) stuck.Add(item);
                else stuck.Remove(item);
                if (!view.IsOwner() || BasketOpen()) return;
            }
            basketCursor = start + examined;
        }
        if (view.GetZDO().GetInt(StuckKey) != stuck.Count) view.GetZDO().Set(StuckKey, stuck.Count);
    }

    /// <summary>
    /// Moves what fits of a stack into the receivers in order. Chests that are not yet ours are requested together and
    /// skipped this round, but they hold back chests of a lower priority, so nothing lands in the Everlasting Chest
    /// while a category chest for it is still changing hands.
    /// </summary>
    /// <param name="item">The stack.</param>
    /// <param name="from">Where the stack lies, for the nearest-chest order.</param>
    /// <param name="receivers">The receiving chests.</param>
    /// <param name="playerId">The player transferring.</param>
    /// <param name="canContinue">Whether the stack may still be moved.</param>
    /// <param name="take">Removes an amount from the stack; returns whether some is left.</param>
    /// <param name="waiting">True when a chest that may take the rest is still changing hands.</param>
    private void Deliver(ItemDrop.ItemData item, Vector3 from, List<Container> receivers, long playerId, Func<bool> canContinue,
        Func<int, bool> take, out bool waiting)
    {
        waiting = false;
        int heldBack = int.MaxValue;
        foreach (var (chest, priority) in Ordered(receivers, item, from))
        {
            if (priority > heldBack) break;
            if (!canContinue()) { waiting = true; return; }
            if (!ContainerHandoff.Ready(chest, playerId))
            {
                waiting = true;
                heldBack = priority;
                continue;
            }
            int accepted = Module.DepositCollected(chest, item);
            if (accepted <= 0) continue;
            roundMoved += accepted;
            roundChests.Add(chest.m_nview.GetZDO().m_uid);
            if (!take(accepted)) return;
        }
    }

    private bool BasketOpen() => basket.IsInUse() || view.GetZDO().GetInt(ZDOVars.s_inUse) != 0;

    // Category chests before the Everlasting Chest; within each, chests that absorb the item, then chests already
    // holding it, then the nearest. An item can belong to several categories, and a category can have several chests
    // and drawers: all of them are candidates in this order.
    private static List<(Container Chest, int Priority)> Ordered(List<Container> receivers, ItemDrop.ItemData item, Vector3 from) =>
        receivers.Select(chest => (Chest: chest, Priority: Module.CollectionPriority(chest, item)))
            .Where(entry => entry.Priority >= 0).OrderBy(entry => entry.Priority)
            .ThenBy(entry => Module.AbsorbsCollected(entry.Chest, item) ? 0 : 1)
            .ThenBy(entry => entry.Chest.GetInventory().ContainsItemByName(item.m_shared.m_name) ? 0 : 1)
            .ThenBy(entry => (entry.Chest.transform.position - from).sqrMagnitude)
            .ToList();

    private void Announce()
    {
        var package = new ZPackage();
        package.Write(roundMoved);
        package.Write(roundChests.Count);
        foreach (var id in roundChests) package.Write(id);
        view.InvokeRPC(ZNetView.Everybody, PulseRpc, package);
    }

    // Every client lights up the post and sparkles over the basket and each chest that received something.
    private void Pulse(long sender, ZPackage package)
    {
        lastSorted = package.ReadInt();
        lastSortedAt = Time.time;
        flare = 1f;
        if (ZNet.instance != null && ZNet.instance.IsDedicated()) return;
        ChestEffects.Play(transform.TransformPoint(BasketCenter), SortedEffect);
        int count = package.ReadInt();
        for (int index = 0; index < count; index++)
        {
            var chest = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(package.ReadZDOID()) : null;
            if (chest != null) ChestEffects.Play(chest.transform.position + Vector3.up * 0.8f, SortedEffect);
        }
    }

    private List<Container> Receivers(Player player)
    {
        if (Module == null) return new List<Container>();
        float radius = CollectionSettings.ChestRadius.Value * CollectionSettings.ChestRadius.Value;
        return NearbyContainers.Registered().Where(chest => chest != null && Module.IsCollectionChest(chest)
            && chest.m_nview != null && chest.m_nview.IsValid() && chest.GetInventory() != null
            && !chest.IsInUse() && chest.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 0
            && (chest.transform.position - transform.position).sqrMagnitude <= radius
            && chest.CheckAccess(player.GetPlayerID()) && (!chest.m_checkGuardStone || PrivateArea.CheckAccess(chest.transform.position, 0f, false))).ToList();
    }
}
