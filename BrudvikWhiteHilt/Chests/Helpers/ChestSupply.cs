#nullable enable annotations

using BrudvikWhiteHilt.Chests.Configuration;
using BrudvikWhiteHilt.Chests.Constants;
using BrudvikWhiteHilt.Chests.Extensions;
using BrudvikWhiteHilt.Chests.Utils;
using BrudvikWhiteHilt.Difficulty.Beasts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Helpers
{
    /// <summary>
    /// How far an item in a chest is from being unlocked.
    /// </summary>
    public class UnlockProgress
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnlockProgress"/> class.
        /// </summary>
        /// <param name="displayName">The localized item name.</param>
        /// <param name="stored">The amount in the chest.</param>
        /// <param name="required">The amount needed to unlock the item.</param>
        public UnlockProgress(string displayName, int stored, int required)
        {
            DisplayName = displayName;
            Stored = stored;
            Required = required;
        }

        /// <summary>The localized item name.</summary>
        public string DisplayName { get; }

        /// <summary>The amount in the chest.</summary>
        public int Stored { get; }

        /// <summary>The amount needed to unlock the item.</summary>
        public int Required { get; }
    }

    /// <summary>
    /// Decides, for the active <see cref="ChestMode"/>, which items a chest supplies without limit.
    /// </summary>
    public class ChestSupply
    {
        // Guards against modded recipes with extreme amounts filling a chest with one item.
        private const int MaxStacksPerItem = 10;
        private const char LevelSeparator = '@';

        private readonly ChestSettings settings;
        private readonly ItemCatalog catalog;
        private readonly WorldProgress progress;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChestSupply"/> class.
        /// </summary>
        /// <param name="settings">The plugin settings.</param>
        /// <param name="catalog">The generated item lists per chest category.</param>
        /// <param name="progress">The world-wide unlocked and discovered items.</param>
        public ChestSupply(ChestSettings settings, ItemCatalog catalog, WorldProgress progress)
        {
            this.settings = settings;
            this.catalog = catalog;
            this.progress = progress;
        }

        /// <summary>
        /// Gets the active chest mode.
        /// </summary>
        public ChestMode Mode => settings.Mode.Value;

        /// <summary>
        /// Gets a value indicating whether the world-wide progress is known, which the progression modes depend on.
        /// </summary>
        public bool IsReady => Mode == ChestMode.Full || progress.IsReady;

        /// <summary>
        /// Checks whether a stack in a chest is refilled without limit under the given mode.
        /// </summary>
        /// <param name="mode">The chest mode to evaluate.</param>
        /// <param name="category">The chest's category; <see cref="ChestCategory.None"/> accepts any item.</param>
        /// <param name="item">The stack in the chest.</param>
        /// <returns>True if the stack is kept full.</returns>
        public bool IsSupplied(ChestMode mode, ChestCategory category, ItemDrop.ItemData item)
        {
            if (!IsPlain(item)) return false;
            if (mode == ChestMode.Full) return true;
            if (item.m_dropPrefab == null) return false;

            return IsSupplied(mode, category, GetLevelKey(item), item.m_shared);
        }

        /// <summary>
        /// Checks whether a stack in a cart or ship hold is kept full. Cargo only refills items that stack.
        /// </summary>
        /// <param name="item">The stack in the cargo.</param>
        /// <returns>True if the stack is kept full.</returns>
        public bool IsCargoSupplied(ItemDrop.ItemData item)
        {
            return IsStackable(item.m_shared) && IsSupplied(Mode, ChestCategory.None, item);
        }

        /// <summary>
        /// Checks whether a stack is an ordinary copy of its item, like the chest adds itself. Stacks with skill stars
        /// (a quality above 1 on an item without levels) or their own data, such as a dog's name, always belong to the
        /// player who stored them.
        /// </summary>
        /// <param name="item">The stack.</param>
        /// <returns>True if the stack can be replaced by the chest's own stack.</returns>
        public static bool IsPlain(ItemDrop.ItemData item)
        {
            return (item.m_quality <= 1 || HasLevels(item.m_shared)) && (item.m_customData == null || item.m_customData.Count == 0)
                && (item.m_dropPrefab == null || !IsEarnedOnly(item.m_dropPrefab.name));
        }

        /// <summary>
        /// Checks whether the quality of a stackable item is a level of its own, like a fish's, so every level is
        /// unlocked and supplied on its own.
        /// </summary>
        /// <param name="shared">The item's shared data.</param>
        /// <returns>True if the item comes in levels.</returns>
        public static bool HasLevels(ItemDrop.ItemData.SharedData shared)
        {
            return shared.m_maxQuality > 1 && IsStackable(shared);
        }

        /// <summary>
        /// Gets the key a level of an item is unlocked and counted under: the prefab name for level 1, so older
        /// records stay valid, and <c>name@level</c> above it.
        /// </summary>
        /// <param name="prefabName">The item prefab name.</param>
        /// <param name="quality">The item level.</param>
        /// <returns>The key.</returns>
        public static string GetLevelKey(string prefabName, int quality)
        {
            return quality <= 1 ? prefabName : prefabName + LevelSeparator + quality.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the key a stack is unlocked and counted under. Only items with levels get one per level; stars on
        /// other items count towards the item itself.
        /// </summary>
        /// <param name="item">The stack; it must have a drop prefab.</param>
        /// <returns>The key.</returns>
        public static string GetLevelKey(ItemDrop.ItemData item)
        {
            var name = item.m_dropPrefab.name;
            return HasLevels(item.m_shared) ? GetLevelKey(name, item.m_quality) : name;
        }

        /// <summary>
        /// Splits a key from <see cref="GetLevelKey(string, int)"/> into the prefab name and the level.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="quality">The item level, 1 for a plain prefab name.</param>
        /// <returns>The item prefab name.</returns>
        public static string SplitLevelKey(string key, out int quality)
        {
            quality = 1;
            var at = key.LastIndexOf(LevelSeparator);
            if (at <= 0 || !int.TryParse(key.Substring(at + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var level) || level <= 1)
            {
                return key;
            }

            quality = level;
            return key.Substring(0, at);
        }

        /// <summary>
        /// Checks whether an item is only earned by a kill and never supplied: the black beast trophies, which are bound
        /// to White Hilt gear.
        /// </summary>
        /// <param name="prefabName">Prefab name of the item.</param>
        /// <returns>True if no chest, cart or ship hold may make it unlimited.</returns>
        public static bool IsEarnedOnly(string prefabName)
        {
            return BeastDefinition.All.Any(beast => beast.TrophyName == prefabName);
        }

        /// <summary>
        /// Checks whether a stack in a chest belongs to the chest itself, so it is never taken by Take all and is
        /// deleted instead of dropped when the chest is destroyed. Items that do not stack only count when the chest
        /// adds them itself and they are unchanged, so upgraded or crafted gear a player stored is kept.
        /// </summary>
        /// <param name="mode">The chest mode to evaluate.</param>
        /// <param name="category">The chest's category; <see cref="ChestCategory.None"/> accepts any item.</param>
        /// <param name="item">The stack in the chest.</param>
        /// <returns>True if the stack is supplied by the chest.</returns>
        public bool IsStock(ChestMode mode, ChestCategory category, ItemDrop.ItemData item)
        {
            if (!IsSupplied(mode, category, item)) return false;
            if (IsStackable(item.m_shared)) return true;

            return category != ChestCategory.None && item.m_dropPrefab != null &&
                   catalog.Contains(category, item.m_dropPrefab.name) &&
                   item.m_quality <= 1 && item.m_crafterID == 0;
        }

        /// <summary>
        /// Gets the number of full stacks a chest keeps of an unlimited item: one, or more when a single recipe or
        /// build piece needs more than one stack.
        /// </summary>
        /// <param name="prefabName">The item prefab name.</param>
        /// <param name="shared">The item's shared data.</param>
        /// <returns>The number of stacks.</returns>
        public int GetStackCount(string prefabName, ItemDrop.ItemData.SharedData shared)
        {
            if (!IsStackable(shared)) return 1;

            var required = catalog.GetLargestRequirement(prefabName);
            var stacks = (required + shared.m_maxStackSize - 1) / shared.m_maxStackSize;
            return Mathf.Clamp(stacks, 1, MaxStacksPerItem);
        }

        /// <summary>
        /// Checks whether an item put into a chest should disappear instead of forming a new stack, because the chest
        /// already holds it without limit.
        /// </summary>
        /// <param name="category">The chest's category; <see cref="ChestCategory.None"/> accepts any item.</param>
        /// <param name="inventory">The chest's inventory.</param>
        /// <param name="item">The item being put into the chest.</param>
        /// <returns>True if the item should be absorbed.</returns>
        public bool AbsorbsDeposit(ChestCategory category, Inventory inventory, ItemDrop.ItemData item)
        {
            if (!IsReady || item.m_dropPrefab == null || !IsStackable(item.m_shared)) return false;
            if (!IsSupplied(Mode, category, item)) return false;

            var key = GetLevelKey(item);
            var present = false;
            foreach (var stored in inventory.GetAllItems())
            {
                // Moving a stack within the chest must never delete it.
                if (stored == item) return false;
                if (stored.m_dropPrefab != null && IsPlain(stored) && GetLevelKey(stored) == key) present = true;
            }
            return present;
        }

        /// <summary>
        /// Gets the items a chest of the given category is filled with when they are missing. In Linear mode every
        /// unlocked level of an item with levels is added too.
        /// </summary>
        /// <param name="category">The chest category.</param>
        /// <returns>The keys from <see cref="GetLevelKey(string, int)"/>.</returns>
        public IEnumerable<string> GetItemsToAdd(ChestCategory category)
        {
            var mode = Mode;
            foreach (var name in catalog.GetItems(category))
            {
                if (mode == ChestMode.Full)
                {
                    yield return name;
                    continue;
                }

                var shared = catalog.GetShared(name);
                if (shared == null) continue;
                if (IsSupplied(mode, category, name, shared)) yield return name;
                if (mode != ChestMode.Linear || !HasLevels(shared)) continue;

                for (var quality = 2; quality <= shared.m_maxQuality; quality++)
                {
                    var key = GetLevelKey(name, quality);
                    if (IsSupplied(mode, category, key, shared)) yield return key;
                }
            }
        }

        /// <summary>
        /// Counts how many of a category's items are unlimited, and how many could become unlimited.
        /// </summary>
        /// <param name="category">The chest category.</param>
        /// <param name="supplied">The number of unlimited items.</param>
        /// <param name="total">The number of items that can become unlimited.</param>
        public void CountProgress(ChestCategory category, out int supplied, out int total)
        {
            var mode = Mode;
            supplied = 0;
            total = 0;
            foreach (var name in catalog.GetItems(category))
            {
                var shared = catalog.GetShared(name);
                if (shared == null || (mode != ChestMode.Full && !IsStackable(shared))) continue;

                total++;
                if (IsSupplied(mode, category, name, shared)) supplied++;
            }
        }

        /// <summary>
        /// Describes what happens to a stack in a chest, for the item tooltip.
        /// </summary>
        /// <param name="category">The chest's category; <see cref="ChestCategory.None"/> accepts any item.</param>
        /// <param name="item">The stack in the chest.</param>
        /// <param name="stored">The total amount of this item, at this level, in the chest.</param>
        /// <returns>The status text.</returns>
        public string GetStatus(ChestCategory category, ItemDrop.ItemData item, int stored)
        {
            var mode = Mode;
            if (IsSupplied(mode, category, item)) return Texts.Get("bsc_status_unlimited");
            if (item.m_dropPrefab == null) return Texts.Get("bsc_status_stored");

            var name = item.m_dropPrefab.name;
            if (!BelongsIn(category, name)) return Texts.Get("bsc_status_wrong_chest");
            if (!IsStackable(item.m_shared)) return Texts.Get("bsc_status_not_stackable");
            if (mode == ChestMode.Discovered) return Texts.Get("bsc_status_discover");

            // Unlocked already, but this stack has stars or its own data.
            if (!CanUnlock(category, GetLevelKey(item), item.m_shared)) return Texts.Get("bsc_status_stored");

            var missing = Math.Max(0, GetUnlockAmount(item.m_shared) - stored);
            return Texts.Get("bsc_status_store_more", missing);
        }

        /// <summary>
        /// Finds the items in a chest that are closest to being unlocked in Linear mode.
        /// </summary>
        /// <param name="category">The chest's category; <see cref="ChestCategory.None"/> accepts any item.</param>
        /// <param name="inventory">The chest's inventory.</param>
        /// <param name="count">The maximum number of items to return.</param>
        /// <returns>The display name, stored amount and required amount of each item, closest first.</returns>
        public List<UnlockProgress> GetClosestUnlocks(ChestCategory category, Inventory inventory, int count)
        {
            var result = new List<UnlockProgress>();
            if (Mode != ChestMode.Linear) return result;

            var totals = inventory.CountByLevelKey();
            foreach (var item in inventory.GetAllItems())
            {
                if (item.m_dropPrefab == null) continue;

                // Several stacks of one item share one entry.
                var key = GetLevelKey(item);
                if (!totals.TryGetValue(key, out var stored)) continue;

                totals.Remove(key);
                if (!CanUnlock(category, key, item.m_shared)) continue;

                result.Add(new UnlockProgress(GetDisplayName(key), stored, GetUnlockAmount(item.m_shared)));
            }

            return result.OrderByDescending(progress => (float)progress.Stored / progress.Required).Take(count).ToList();
        }

        /// <summary>
        /// Gets the localized display name of an item, with its level when it is above 1.
        /// </summary>
        /// <param name="key">The item prefab name, or a key from <see cref="GetLevelKey(string, int)"/>.</param>
        /// <returns>The display name, or the prefab name if the item is unknown.</returns>
        public string GetDisplayName(string key)
        {
            var prefabName = SplitLevelKey(key, out var quality);
            var shared = catalog.GetShared(prefabName) ??
                         ObjectDB.instance?.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>()?.m_itemData.m_shared;
            var name = shared == null ? prefabName : Localization.instance.Localize(shared.m_name);
            return quality > 1 ? Texts.Get("bsc_item_level", name, quality) : name;
        }

        /// <summary>
        /// Checks whether storing enough of this item in a chest of the given category unlocks it in Linear mode.
        /// </summary>
        /// <param name="category">The chest category; <see cref="ChestCategory.None"/> accepts any item.</param>
        /// <param name="key">The item prefab name, or a key from <see cref="GetLevelKey(string, int)"/> for a level.</param>
        /// <param name="shared">The item's shared data.</param>
        /// <returns>True if the item can still be unlocked from this chest.</returns>
        public bool CanUnlock(ChestCategory category, string key, ItemDrop.ItemData.SharedData shared)
        {
            return IsStackable(shared) && BelongsIn(category, SplitLevelKey(key, out _)) && !progress.IsUnlocked(key);
        }

        /// <summary>
        /// Gets the number of items a chest must hold to unlock the item in Linear mode.
        /// </summary>
        /// <param name="shared">The item's shared data.</param>
        /// <returns>The required amount.</returns>
        public int GetUnlockAmount(ItemDrop.ItemData.SharedData shared)
        {
            return shared.m_maxStackSize * settings.UnlockStacks.Value;
        }

        /// <summary>
        /// Unlocks an item, or one level of it, for the whole world.
        /// </summary>
        /// <param name="key">The item prefab name, or a key from <see cref="GetLevelKey(string, int)"/> for a level.</param>
        public void Unlock(string key)
        {
            progress.Unlock(key);
        }

        /// <summary>
        /// Checks whether an item is unlimited in the chest it is placed in.
        /// </summary>
        /// <param name="prefabName">The item prefab name.</param>
        /// <param name="shared">The item's shared data.</param>
        /// <returns>True if the item's chest keeps it full.</returns>
        public bool IsUnlimited(string prefabName, ItemDrop.ItemData.SharedData shared)
        {
            var category = catalog.GetCategory(prefabName);
            return category != ChestCategory.None && IsSupplied(Mode, category, prefabName, shared);
        }

        /// <summary>
        /// Adds up, per item, how much a chest holds of the items it can still unlock in Linear mode.
        /// </summary>
        /// <param name="category">The chest's category; <see cref="ChestCategory.None"/> accepts any item.</param>
        /// <param name="inventory">The chest's inventory.</param>
        /// <returns>The stored amount per key from <see cref="GetLevelKey(ItemDrop.ItemData)"/>.</returns>
        public Dictionary<string, int> GetUnlockableAmounts(ChestCategory category, Inventory inventory)
        {
            var result = new Dictionary<string, int>();
            foreach (var item in inventory.GetAllItems())
            {
                if (item.m_dropPrefab == null) continue;

                var key = GetLevelKey(item);
                if (!result.ContainsKey(key) && !CanUnlock(category, key, item.m_shared)) continue;

                result.TryGetValue(key, out var amount);
                result[key] = amount + item.m_stack;
            }
            return result;
        }

        /// <summary>
        /// Checks whether an item stacks. Items that do not stack are never duplicated in the progression modes.
        /// </summary>
        /// <param name="shared">The item's shared data.</param>
        /// <returns>True if more than one of the item fits in a slot.</returns>
        public static bool IsStackable(ItemDrop.ItemData.SharedData shared) => shared.m_maxStackSize > 1;

        private bool IsSupplied(ChestMode mode, ChestCategory category, string key, ItemDrop.ItemData.SharedData shared)
        {
            if (mode == ChestMode.Full) return true;
            if (!IsStackable(shared) || !BelongsIn(category, SplitLevelKey(key, out _))) return false;

            return mode == ChestMode.Linear ? progress.IsUnlocked(key) : progress.IsDiscovered(shared.m_name);
        }

        private bool BelongsIn(ChestCategory category, string prefabName)
        {
            return category == ChestCategory.None || catalog.Contains(category, prefabName);
        }
    }
}
