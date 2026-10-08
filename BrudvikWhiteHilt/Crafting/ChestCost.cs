using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Paying with what lies in the inventory and in the chests around the local player, the same way everywhere: the
/// workbenches of the mod, its build and terrain tools and dyeing do it as crafting and building with the hammer do.
/// What the player has counts every chest in range they may use, so nothing looks missing because another player's
/// machine held a chest a moment ago. Paying first fetches what lies in such chests (<see cref="ChestWithdrawal"/>) and
/// does the click again once it has arrived; then it takes from the inventory first and the chests after.
/// </summary>
public static class ChestCost
{
    private const string WaitingMessage = "$msg_whitehilt_chests_waiting";

    /// <summary>
    /// How gathering what a payment needs went.
    /// </summary>
    public enum Outcome
    {
        /// <summary>Everything is at hand; take it.</summary>
        Ready,

        /// <summary>It is being fetched from other players' chests; the click is done again when it has arrived.</summary>
        Fetching,

        /// <summary>An earlier fetch is still under way; nothing was asked for.</summary>
        Busy,

        /// <summary>Something is missing, even counting every chest.</summary>
        Lacking
    }

    /// <summary>
    /// How many of an item the player has: the inventory and every chest in range they may use. A stack a chest keeps
    /// full without limit counts as enough for anything.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="use">What it is for, which decides the range and whether chests count at all.</param>
    /// <param name="name">Shared item name.</param>
    /// <returns>The count.</returns>
    public static int Have(Player player, NearbyContainers.Use use, string name)
    {
        return Count(player, use, name, heldOnly: false);
    }

    /// <summary>
    /// How many of an item can be taken right now: the inventory and the chests handed over to the player.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="use">What it is for.</param>
    /// <param name="name">Shared item name.</param>
    /// <returns>The count.</returns>
    public static int AtHand(Player player, NearbyContainers.Use use, string name)
    {
        return Count(player, use, name, heldOnly: true);
    }

    /// <summary>
    /// Whether a chest in range keeps an item full without limit, so it never runs out.
    /// </summary>
    /// <param name="use">What it is for.</param>
    /// <param name="name">Shared item name.</param>
    /// <returns>True if it never runs out here.</returns>
    public static bool IsUnlimited(NearbyContainers.Use use, string name)
    {
        return NearbyContainers.IsActive(use) && NearbyContainers.IsUnlimitedNearby(use, name);
    }

    /// <summary>
    /// Brings what a payment needs to hand: asks the other players' chests for it unless this is the click done again
    /// after such a fetch, then checks that all of it is here.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="use">What it is for.</param>
    /// <param name="needs">Shared item names and amounts; a name may come more than once.</param>
    /// <param name="again">Does the click again once fetched items have arrived.</param>
    /// <returns>How it went.</returns>
    public static Outcome Gather(Player player, NearbyContainers.Use use, IEnumerable<(string Name, int Amount)> needs, Action again)
    {
        List<(string Name, int Amount)> total = Total(needs);
        if (total.Count == 0)
        {
            return Outcome.Ready;
        }

        if (!ChestWithdrawal.Continuing)
        {
            if (ChestWithdrawal.Busy)
            {
                return Outcome.Busy;
            }

            if (ChestWithdrawal.Fetch(player, use, total, again ?? (() => { })))
            {
                return Outcome.Fetching;
            }
        }

        return total.All(need => AtHand(player, use, need.Name) >= need.Amount) ? Outcome.Ready : Outcome.Lacking;
    }

    /// <summary>
    /// Gathers and takes a payment, telling the player when it waits for the chests or something is missing.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="use">What it is for.</param>
    /// <param name="needs">Shared item names and amounts.</param>
    /// <param name="again">Does the click again once fetched items have arrived.</param>
    /// <param name="lacking">The message when something is missing, or null for none.</param>
    /// <returns>True if paid.</returns>
    public static bool Pay(Player player, NearbyContainers.Use use, IEnumerable<(string Name, int Amount)> needs, Action again, string lacking)
    {
        List<(string Name, int Amount)> total = Total(needs);
        switch (Gather(player, use, total, again))
        {
            case Outcome.Ready:
                Take(player, use, total);
                return true;
            case Outcome.Lacking:
                if (lacking != null)
                {
                    player.Message(MessageHud.MessageType.Center, lacking);
                }

                return false;
            default:
                player.Message(MessageHud.MessageType.Center, WaitingMessage);
                return false;
        }
    }

    /// <summary>
    /// Tells the player that the chests are being fetched from.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void ShowWaiting(Player player)
    {
        player.Message(MessageHud.MessageType.Center, WaitingMessage);
    }

    /// <summary>
    /// Takes items from the inventory first, then from the chests handed over to the player.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="use">What it is for.</param>
    /// <param name="needs">Shared item names and amounts.</param>
    public static void Take(Player player, NearbyContainers.Use use, IEnumerable<(string Name, int Amount)> needs)
    {
        foreach ((string name, int amount) in Total(needs))
        {
            Take(player, use, name, amount);
        }
    }

    /// <summary>
    /// Takes an item from the inventory first, then from the chests handed over to the player.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="use">What it is for.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="amount">How many.</param>
    public static void Take(Player player, NearbyContainers.Use use, string name, int amount)
    {
        if (amount <= 0 || string.IsNullOrEmpty(name))
        {
            return;
        }

        Inventory inventory = player.GetInventory();
        int own = Mathf.Min(amount, inventory.CountItems(name));
        if (own > 0)
        {
            inventory.RemoveItem(name, own);
        }

        if (amount > own && NearbyContainers.IsActive(use))
        {
            NearbyContainers.Take(use, name, amount - own);
        }
    }

    private static int Count(Player player, NearbyContainers.Use use, string name, bool heldOnly)
    {
        int count = player.GetInventory().CountItems(name);
        if (NearbyContainers.IsActive(use))
        {
            count += NearbyContainers.CountForCheck(use, name, -1, heldOnly);
        }

        return count;
    }

    private static List<(string Name, int Amount)> Total(IEnumerable<(string Name, int Amount)> needs)
    {
        return needs.Where(need => !string.IsNullOrEmpty(need.Name) && need.Amount > 0)
            .GroupBy(need => need.Name)
            .Select(group => (group.Key, group.Sum(need => need.Amount)))
            .ToList();
    }
}
