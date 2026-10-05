using BrudvikWhiteHilt.Chests;
using BrudvikWhiteHilt.Chests.Helpers;
using BrudvikWhiteHilt.Crafting;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Quartermaster;

/// <summary>
/// The store of a Quartermaster's Table: everything that stacks in the chests, carts and ship holds around it, as one
/// list. Taking gives at most a stack at a time and never empties the store of an item: unlimited stacks refill, and
/// of items players stored themselves <see cref="QuartermasterSettings.KeepAtLeast"/> always stay behind.
/// </summary>
public static class QuartermasterStore
{
    /// <summary>
    /// One kind of item in the store.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>A stack of the item as it lies in a chest; cloned when taking.</summary>
        public ItemDrop.ItemData Sample;

        /// <summary>How many lie in the chests.</summary>
        public int Count;

        /// <summary>True if a chest keeps the item without limit.</summary>
        public bool Unlimited;

        /// <summary>The chest it lies in, for the filter: a White Hilt chest's name, or "Other".</summary>
        public string Group;

        /// <summary>Shared item name.</summary>
        public string Name => Sample.m_shared.m_name;

        /// <summary>Item quality, or level for fish.</summary>
        public int Quality => Sample.m_quality;
    }

    /// <summary>
    /// Tells which chest a container is and puts items back into chests, or null while the chest module is off.
    /// </summary>
    internal static ChestModule Module { get; set; }

    /// <summary>
    /// The containers the local player may use around a table, nearest first.
    /// </summary>
    /// <param name="table">The table's position.</param>
    /// <returns>The containers.</returns>
    public static List<Container> Around(Vector3 table)
    {
        return NearbyContainers.Around(table, QuartermasterSettings.Range.Value);
    }

    /// <summary>
    /// Lists what can be taken from the containers, one entry per item and quality, sorted by group and name.
    /// </summary>
    /// <param name="containers">The containers around the table.</param>
    /// <returns>The entries.</returns>
    public static List<Entry> List(List<Container> containers)
    {
        Dictionary<(string Name, int Quality), Entry> entries = Collect(containers, null);
        string other = Localization.instance.Localize("$whitehilt_qm_other");
        foreach (Entry entry in entries.Values)
        {
            entry.Group ??= other;
        }

        return entries.Values
            .OrderBy(entry => entry.Group == other ? 1 : 0)
            .ThenBy(entry => entry.Group, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => Localization.instance.Localize(entry.Name), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Quality)
            .ToList();
    }

    // The entries of the containers, unsorted and without the group of those in no labelled chest; only of one item
    // when a name is given.
    private static Dictionary<(string Name, int Quality), Entry> Collect(List<Container> containers, string name)
    {
        Dictionary<(string Name, int Quality), Entry> entries = new();
        foreach (Container container in containers)
        {
            string group = null;
            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
            {
                if ((name != null && item.m_shared.m_name != name) || !IsOffered(item))
                {
                    continue;
                }

                group ??= Module?.GetChestLabel(container);

                bool unlimited = IsUnlimitedIn(container, item);
                if (!unlimited && !QuartermasterSettings.IncludeFinite.Value)
                {
                    continue;
                }

                if (!entries.TryGetValue((item.m_shared.m_name, item.m_quality), out Entry entry))
                {
                    entry = new Entry { Sample = item };
                    entries[(item.m_shared.m_name, item.m_quality)] = entry;
                }

                entry.Count += item.m_stack;
                if (unlimited && !entry.Unlimited)
                {
                    entry.Unlimited = true;
                    entry.Group = group;
                }

                entry.Group ??= group;
            }
        }

        return entries;
    }

    /// <summary>
    /// How many of an entry may be taken in all: any amount if unlimited, otherwise all but the kept ones.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns>The amount.</returns>
    public static int Takeable(Entry entry)
    {
        return entry.Unlimited ? int.MaxValue : Mathf.Max(0, entry.Count - QuartermasterSettings.KeepAtLeast.Value);
    }

    /// <summary>
    /// Takes up to <paramref name="wanted"/> of an entry into the player's inventory, or into a cart or ship hold, at
    /// most one stack, never more than fits and never the kept ones. Unlimited chests are taken from first, so they
    /// refill. Every container must have been handed over to the player (<see cref="Chests.ContainerHandoff"/>).
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="containers">The containers to take from.</param>
    /// <param name="entry">The entry.</param>
    /// <param name="wanted">How many are wanted; at most a stack is given.</param>
    /// <param name="target">The cart or ship hold to load, or null for the player's inventory.</param>
    /// <returns>How many were taken.</returns>
    public static int Take(Player player, List<Container> containers, Entry entry, int wanted, Container target = null)
    {
        Inventory inventory = target != null ? target.GetInventory() : player.GetInventory();
        int amount = Mathf.Min(wanted, entry.Sample.m_shared.m_maxStackSize, Takeable(entry));
        amount = Fitting(inventory, entry.Sample, amount);
        if (amount <= 0)
        {
            return 0;
        }

        ItemDrop.ItemData item = entry.Sample.Clone();
        List<Container> order = containers.Where(container => container != target)
            .OrderBy(container => HoldsUnlimited(container, entry) ? 0 : 1).ToList();
        int taken = NearbyContainers.TakeFrom(order, entry.Name, amount, entry.Quality);
        if (taken <= 0)
        {
            return 0;
        }

        // It was checked to fit; should it still be refused, it lands on the ground rather than being lost.
        item.m_stack = taken;
        if (!inventory.AddItem(item))
        {
            Transform at = target != null ? target.transform : player.transform;
            ItemDrop.DropItem(item, taken, at.position + at.forward + Vector3.up, Quaternion.identity);
        }

        if (target != null)
        {
            target.Save();
            if (NearbyContainers.ShowTakenChests)
            {
                ContainerPulse.Play(target);
            }
        }

        return taken;
    }

    /// <summary>
    /// Takes enough of an item that the player, or the cart or ship hold, has <paramref name="need"/> of it, a stack
    /// at a time.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="containers">The containers to take from.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="need">How many there should be.</param>
    /// <param name="target">The cart or ship hold to load, or null for the player's inventory.</param>
    /// <returns>True if there is now all of it.</returns>
    public static bool Pack(Player player, List<Container> containers, string name, int need, Container target = null)
    {
        Inventory inventory = target != null ? target.GetInventory() : player.GetInventory();
        List<Container> sources = containers.Where(container => container != target).ToList();
        // Read once; what is taken is counted off, rather than the chests read again for every stack.
        List<Entry> entries = Collect(sources, name).Values.OrderByDescending(entry => entry.Unlimited).ThenBy(entry => entry.Quality).ToList();
        while (true)
        {
            int have = inventory.CountItems(name);
            if (have >= need)
            {
                return true;
            }

            Entry entry = entries.FirstOrDefault(candidate => Takeable(candidate) > 0);
            int taken = entry == null ? 0 : Take(player, sources, entry, need - have, target);
            if (taken <= 0 || inventory.CountItems(name) <= have)
            {
                return false;
            }

            entry.Count -= taken;
        }
    }

    /// <summary>
    /// Counts an item in the containers, and tells whether a chest keeps it without limit.
    /// </summary>
    /// <param name="containers">The containers around the table.</param>
    /// <param name="prefab">The item prefab name.</param>
    /// <param name="count">How many lie in the containers.</param>
    /// <param name="unlimited">True if a chest keeps it without limit.</param>
    public static void CountStock(List<Container> containers, string prefab, out int count, out bool unlimited)
    {
        count = 0;
        unlimited = false;
        foreach (Container container in containers)
        {
            if (container == null || container.GetInventory() == null)
            {
                continue;
            }

            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
            {
                if (item.m_dropPrefab != null && item.m_dropPrefab.name == prefab)
                {
                    count += item.m_stack;
                    unlimited |= IsUnlimitedIn(container, item);
                }
            }
        }
    }

    /// <summary>
    /// Checks whether a container is a cart's load or a ship's hold, which the table can load.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <returns>True for carts and ships.</returns>
    public static bool IsCargo(Container container)
    {
        return container != null && (container.GetComponentInParent<Vagon>() != null || container.GetComponentInParent<Ship>() != null);
    }

    /// <summary>
    /// The containers that hold any of the given items: the ones taking or packing them may change.
    /// </summary>
    /// <param name="containers">The containers around the table.</param>
    /// <param name="names">Shared item names.</param>
    /// <returns>The containers, in their order.</returns>
    public static List<Container> Holding(List<Container> containers, ICollection<string> names)
    {
        return containers.Where(container => container != null && container.GetInventory() != null
            && container.GetInventory().GetAllItems().Any(item => names.Contains(item.m_shared.m_name))).ToList();
    }

    /// <summary>
    /// The chests that could take an item back: the ones putting it back may change.
    /// </summary>
    /// <param name="containers">The containers around the table.</param>
    /// <param name="item">A stack of the item.</param>
    /// <returns>The chests.</returns>
    public static List<Container> Receiving(List<Container> containers, ItemDrop.ItemData item)
    {
        return Module == null ? new List<Container>() : containers.Where(container => Module.CollectionPriority(container, item) >= 0).ToList();
    }

    /// <summary>
    /// The stacks in the player's inventory that a White Hilt chest around the table would take, one entry per item
    /// and quality. Equipped items are left out.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="containers">The containers around the table.</param>
    /// <returns>The entries; Count is the amount in the inventory.</returns>
    public static List<Entry> Returnable(Player player, List<Container> containers)
    {
        Dictionary<(string Name, int Quality), Entry> entries = new();
        if (Module == null)
        {
            return new List<Entry>();
        }

        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
        {
            if (item.m_equipped || !IsOffered(item))
            {
                continue;
            }

            if (entries.TryGetValue((item.m_shared.m_name, item.m_quality), out Entry entry))
            {
                entry.Count += item.m_stack;
                continue;
            }

            if (containers.Any(container => Module.CollectionPriority(container, item) >= 0))
            {
                entries[(item.m_shared.m_name, item.m_quality)] = new Entry { Sample = item, Count = item.m_stack };
            }
        }

        return entries.Values.OrderBy(entry => Localization.instance.Localize(entry.Name), StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>
    /// Puts every stack of an item back into the chests around the table, the same way the Collection Post sorts:
    /// category chests first, then the Everlasting Chest, each time chests that absorb or already hold it first.
    /// Only chests handed over to the player (<see cref="Chests.ContainerHandoff"/>) are used.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="containers">The containers around the table.</param>
    /// <param name="table">The table's position, for the nearest-chest order.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="quality">Item quality.</param>
    /// <returns>How many were put back.</returns>
    public static int PutBack(Player player, List<Container> containers, Vector3 table, string name, int quality)
    {
        if (Module == null)
        {
            return 0;
        }

        Inventory inventory = player.GetInventory();
        int returned = 0;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems().ToList())
        {
            if (item.m_shared.m_name != name || item.m_quality != quality || item.m_equipped || !IsOffered(item))
            {
                continue;
            }

            foreach (Container chest in Receivers(containers, item, table))
            {
                if (chest == null || !chest.m_nview.IsValid() || !chest.m_nview.IsOwner())
                {
                    continue;
                }

                int accepted = Module.DepositCollected(chest, item);
                if (accepted <= 0)
                {
                    continue;
                }

                if (NearbyContainers.ShowTakenChests)
                {
                    ContainerPulse.Play(chest);
                }

                returned += accepted;
                int left = item.m_stack - accepted;
                inventory.RemoveItem(item, accepted);
                if (left <= 0)
                {
                    break;
                }
            }
        }

        return returned;
    }

    /// <summary>
    /// Puts every stack in a cart or ship hold back into the chests around, the same way the Collection Post sorts.
    /// Gear and items with stars or their own data stay in the hold. Only chests handed over to the player are used.
    /// </summary>
    /// <param name="cargo">The cart or ship hold, handed over to the player.</param>
    /// <param name="containers">The chests to put into, without the cargo.</param>
    /// <param name="site">The site's position, for the nearest-chest order.</param>
    /// <returns>How many items were moved.</returns>
    public static int Unload(Container cargo, List<Container> containers, Vector3 site)
    {
        if (Module == null || cargo == null || !cargo.m_nview.IsValid() || !cargo.m_nview.IsOwner())
        {
            return 0;
        }

        Inventory inventory = cargo.GetInventory();
        int moved = 0;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems().ToList())
        {
            if (!IsOffered(item))
            {
                continue;
            }

            foreach (Container chest in Receivers(containers, item, site))
            {
                if (chest == null || !chest.m_nview.IsValid() || !chest.m_nview.IsOwner())
                {
                    continue;
                }

                int accepted = Module.DepositCollected(chest, item);
                if (accepted <= 0)
                {
                    continue;
                }

                if (NearbyContainers.ShowTakenChests)
                {
                    ContainerPulse.Play(chest);
                }

                moved += accepted;
                int left = item.m_stack - accepted;
                inventory.RemoveItem(item, accepted);
                if (left <= 0)
                {
                    break;
                }
            }
        }

        cargo.Save();
        return moved;
    }

    // Only ordinary stacks: no gear, no skill stars or names, nothing the chest config keeps out.
    private static bool IsOffered(ItemDrop.ItemData item)
    {
        return item.m_dropPrefab != null && ChestSupply.IsStackable(item.m_shared) && ChestSupply.IsPlain(item)
            && !NearbyContainers.IsExcluded(item.m_shared.m_name);
    }

    private static bool IsUnlimitedIn(Container container, ItemDrop.ItemData item)
    {
        return NearbyContainers.Unlimited != null && NearbyContainers.Unlimited.IsUnlimitedIn(container, item);
    }

    private static bool HoldsUnlimited(Container container, Entry entry)
    {
        return container.GetInventory().GetAllItems()
            .Any(item => item.m_shared.m_name == entry.Name && item.m_quality == entry.Quality && IsUnlimitedIn(container, item));
    }

    // The most of an amount that fits in the inventory.
    private static int Fitting(Inventory inventory, ItemDrop.ItemData sample, int amount)
    {
        if (amount <= 0 || inventory.CanAddItem(sample, amount))
        {
            return amount;
        }

        int low = 0;
        int high = amount;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            if (inventory.CanAddItem(sample, middle))
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        return low;
    }

    private static List<Container> Receivers(List<Container> containers, ItemDrop.ItemData item, Vector3 table)
    {
        return containers.Select(chest => (Chest: chest, Priority: Module.CollectionPriority(chest, item)))
            .Where(entry => entry.Priority >= 0).OrderBy(entry => entry.Priority)
            .ThenBy(entry => Module.AbsorbsCollected(entry.Chest, item) ? 0 : 1)
            .ThenBy(entry => entry.Chest.GetInventory().ContainsItemByName(item.m_shared.m_name) ? 0 : 1)
            .ThenBy(entry => (entry.Chest.transform.position - table).sqrMagnitude)
            .Select(entry => entry.Chest)
            .ToList();
    }
}
