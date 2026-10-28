#nullable enable annotations

using BrudvikWhiteHilt.Chests.Constants;
using BrudvikWhiteHilt.Chests.Events;
using BrudvikWhiteHilt.Chests.Extensions;
using BrudvikWhiteHilt.Chests.Helpers;
using BrudvikWhiteHilt.Chests.Utils;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Piece
{
    /// <summary>
    /// Shows in the inventory screen and in hover texts which items are unlimited and how far the rest are from it.
    /// </summary>
    public class ChestProgressUi
    {
        private const string Gold = "#FFD24D";
        private const string Infinity = "\u221E";
        private static readonly Color ProgressBarColor = new(0.4f, 0.75f, 1f, 1f);

        // A mark on a slot: nothing, or the infinity sign; any other value is the unlock progress from 0 to 1.
        private const float NoMark = -2f;
        private const float UnlimitedMark = -1f;

        private readonly ChestSupply supply;
        private readonly Func<Container?, CustomPieceExtended?> findPiece;
        private readonly Func<Container?, bool> isCargo;
        private readonly Func<long> supplyRevision;

        // The open chest's contents as last seen: each stack with its size and level. The grid is drawn every frame,
        // but the counts and marks below are worked out again only when these, or the supply, change.
        private Inventory? seenInventory;
        private readonly List<ItemDrop.ItemData> seenItems = new();
        private readonly List<int> seenStacks = new();
        private readonly List<int> seenQualities = new();
        private int contentsVersion;

        private int totalsVersion = -1;
        private Dictionary<string, int> totals = new();

        // The mark of each stack in seenItems, and what they were worked out for.
        private readonly List<float> marks = new();
        private int marksVersion = -1;
        private long marksRevision;
        private ChestCategory marksCategory;
        private bool marksCargo;

        // The summary of each category, and the supply and language it was made for.
        private readonly Dictionary<ChestCategory, Summary> summaries = new();

        private TMP_FontAsset? checkedFont;
        private string unlimitedText = string.Empty;

        private sealed class Summary
        {
            public long Revision;
            public string? Language;
            public string? Text;
            public string? Suffix;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChestProgressUi"/> class.
        /// </summary>
        /// <param name="supply">Decides which items are unlimited.</param>
        /// <param name="findPiece">Finds the chest definition for a container, or null if it is not one of ours.</param>
        /// <param name="isCargo">Checks whether a container is a cart or ship hold that keeps unlimited items full.</param>
        /// <param name="supplyRevision">A number that changes whenever the settings or the world's unlocked and discovered
        /// items change, which is all that decides what is unlimited.</param>
        public ChestProgressUi(ChestSupply supply, Func<Container?, CustomPieceExtended?> findPiece, Func<Container?, bool> isCargo,
            Func<long> supplyRevision)
        {
            this.supply = supply;
            this.findPiece = findPiece;
            this.isCargo = isCargo;
            this.supplyRevision = supplyRevision;
        }

        /// <summary>
        /// Marks unlimited stacks with an infinity sign and shows a progress bar under items that can still be unlocked.
        /// </summary>
        public void HandleGridUpdated(object sender, InventoryGridUpdatedPatchEvent e)
        {
            if (!TryGetOpenChest(e.Grid, out var category, out var cargo)) return;

            // The game redraws every slot each frame, so the marks are set again each frame, from what UpdateMarks
            // worked out when the contents last changed.
            UpdateMarks(e.Grid.m_inventory, category, cargo);
            for (var i = 0; i < seenItems.Count; i++)
            {
                var mark = marks[i];
                if (mark == NoMark) continue;

                var item = seenItems[i];
                var element = e.Grid.GetElement(item.m_gridPos.x, item.m_gridPos.y, e.Grid.m_width);
                if (element == null) continue;

                if (mark == UnlimitedMark)
                {
                    element.m_amount.gameObject.SetActive(true);
                    element.m_amount.text = GetUnlimitedText(element.m_amount);
                    continue;
                }

                element.m_durability.gameObject.SetActive(true);
                element.m_durability.SetValue(mark);
                element.m_durability.SetColor(ProgressBarColor);
            }
        }

        // Works out for each stack whether it is unlimited or how far it is from being unlocked, when the contents,
        // the chest or the supply changed since the last time.
        private void UpdateMarks(Inventory inventory, ChestCategory category, bool cargo)
        {
            TrackContents(inventory);
            var revision = supplyRevision();
            if (marksVersion == contentsVersion && marksRevision == revision && marksCategory == category && marksCargo == cargo) return;

            marksVersion = contentsVersion;
            marksRevision = revision;
            marksCategory = category;
            marksCargo = cargo;
            marks.Clear();

            var mode = supply.Mode;
            var stored = GetTotals(inventory);
            foreach (var item in seenItems)
            {
                marks.Add(GetMark(item, mode, category, cargo, stored));
            }
        }

        private float GetMark(ItemDrop.ItemData item, ChestMode mode, ChestCategory category, bool cargo, Dictionary<string, int> stored)
        {
            if (cargo ? supply.IsCargoSupplied(item) : supply.IsSupplied(mode, category, item)) return UnlimitedMark;
            if (cargo || mode != ChestMode.Linear || item.m_dropPrefab == null) return NoMark;

            var key = ChestSupply.GetLevelKey(item);
            if (!supply.CanUnlock(category, key, item.m_shared)) return NoMark;

            stored.TryGetValue(key, out var amount);
            return Mathf.Clamp01((float)amount / supply.GetUnlockAmount(item.m_shared));
        }

        /// <summary>
        /// Adds the item's status in the chest to its tooltip.
        /// </summary>
        public void HandleItemTooltip(object sender, ItemTooltipPatchEvent e)
        {
            if (!TryGetOpenChest(e.Grid, out var category, out var cargo)) return;
            if (cargo && !supply.IsCargoSupplied(e.Item)) return;

            var amount = 0;
            if (e.Item.m_dropPrefab != null) GetTotals(e.Grid.m_inventory).TryGetValue(ChestSupply.GetLevelKey(e.Item), out amount);

            var status = supply.GetStatus(category, e.Item, amount);
            e.Tooltip.Set(e.Tooltip.m_topic, $"{e.Tooltip.m_text}\n\n<color={Gold}>{status}</color>", e.Grid.m_tooltipAnchor, Vector2.zero);
        }

        /// <summary>
        /// Adds the chest's progress to the title of the open container panel.
        /// </summary>
        public void HandleContainerPanelUpdated(object sender, ContainerPanelUpdatedPatchEvent e)
        {
            var piece = findPiece(e.Gui.m_currentContainer);
            if (piece == null) return;

            var summary = GetCachedSummary(piece.CustomPieceConfig.ItemCategory);
            if (summary.Suffix == null) return;

            var suffix = summary.Suffix;
            var title = e.Gui.m_containerName.text ?? string.Empty;
            if (!title.EndsWith(suffix, StringComparison.Ordinal)) e.Gui.m_containerName.text = title + suffix;
        }

        /// <summary>
        /// Adds the chest's progress, and the items closest to being unlocked, to the chest's hover text.
        /// </summary>
        public void HandleContainerHoverText(object sender, ContainerHoverTextPatchEvent e)
        {
            var piece = findPiece(e.Container);
            if (piece == null) return;

            var category = piece.CustomPieceConfig.ItemCategory;
            var summary = GetCachedSummary(category).Text;
            if (summary != null)
            {
                e.Text += $"\n{summary}";
            }
            else if (category != ChestCategory.None)
            {
                e.Text += $"\n{Texts.Get("bsc_hover_normal")}";
                return;
            }

            var inventory = e.Container.GetInventory();
            if (inventory == null) return;

            foreach (var progress in supply.GetClosestUnlocks(category, inventory, 3))
            {
                e.Text += $"\n  {progress.DisplayName} {progress.Stored}/{progress.Required}";
            }
        }

        /// <summary>
        /// Describes how many of a category's items are unlimited.
        /// </summary>
        /// <param name="category">The chest category.</param>
        /// <returns>The summary, or null if none of the category's items can become unlimited.</returns>
        public string? GetSummary(ChestCategory category)
        {
            if (category == ChestCategory.None) return null;

            supply.CountProgress(category, out var supplied, out var total);
            return total == 0 ? null : Texts.Get("bsc_unlimited_count", $"<color={Gold}>{supplied}/{total}</color>");
        }

        // The summary of a category, made again only when the supply or the language changed: the container panel
        // and the hover text ask every frame, and counting goes through every item of the category.
        private Summary GetCachedSummary(ChestCategory category)
        {
            var revision = supplyRevision();
            var language = Localization.instance?.GetSelectedLanguage();
            if (summaries.TryGetValue(category, out var summary) && summary.Revision == revision && summary.Language == language) return summary;

            if (summary == null) summaries[category] = summary = new Summary();
            summary.Revision = revision;
            summary.Language = language;
            summary.Text = GetSummary(category);
            summary.Suffix = summary.Text == null ? null : $" - {summary.Text}";
            return summary;
        }

        private bool TryGetOpenChest(InventoryGrid grid, out ChestCategory category, out bool cargo)
        {
            category = ChestCategory.None;
            cargo = false;
            var gui = InventoryGui.instance;
            if (gui == null || grid != gui.m_containerGrid) return false;

            var piece = findPiece(gui.m_currentContainer);
            if (piece == null)
            {
                cargo = isCargo(gui.m_currentContainer);
                return cargo;
            }

            category = piece.CustomPieceConfig.ItemCategory;
            return true;
        }

        // The grid asks for every slot's tooltip each frame; count the chest only when its contents changed.
        private Dictionary<string, int> GetTotals(Inventory inventory)
        {
            TrackContents(inventory);
            if (totalsVersion != contentsVersion)
            {
                totals = inventory.CountByLevelKey();
                totalsVersion = contentsVersion;
            }
            return totals;
        }

        // Compares the inventory with what was last seen of it, stack by stack, and counts a new version of the
        // contents when anything differs. It allocates nothing while the contents stay the same.
        private void TrackContents(Inventory inventory)
        {
            var items = inventory.GetAllItems();
            var same = inventory == seenInventory && items.Count == seenItems.Count;
            for (var i = 0; same && i < items.Count; i++)
            {
                var item = items[i];
                same = ReferenceEquals(item, seenItems[i]) && item.m_stack == seenStacks[i] && item.m_quality == seenQualities[i];
            }
            if (same) return;

            seenInventory = inventory;
            seenItems.Clear();
            seenStacks.Clear();
            seenQualities.Clear();
            foreach (var item in items)
            {
                seenItems.Add(item);
                seenStacks.Add(item.m_stack);
                seenQualities.Add(item.m_quality);
            }
            contentsVersion++;
        }

        private string GetUnlimitedText(TMP_Text text)
        {
            if (text.font != checkedFont || unlimitedText.Length == 0)
            {
                checkedFont = text.font;
                var hasInfinity = text.font != null && text.font.HasCharacter(Infinity[0], true, true);
                unlimitedText = $"<color={Gold}>{(hasInfinity ? Infinity : "MAX")}</color>";
            }
            return unlimitedText;
        }
    }
}
