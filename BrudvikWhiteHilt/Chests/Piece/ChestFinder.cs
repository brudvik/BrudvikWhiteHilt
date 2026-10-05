#nullable enable annotations

using BrudvikWhiteHilt.Chests.Constants;
using BrudvikWhiteHilt.Chests.Events;
using BrudvikWhiteHilt.Chests.Helpers;
using BrudvikWhiteHilt.Chests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Piece
{
    /// <summary>
    /// Shows where an item in the player's own inventory belongs: while the pointer rests on it, the chests and wall
    /// drawers of its kind nearby light up, and its tooltip names the chest.
    /// </summary>
    public class ChestFinder
    {
        private const float RefreshSeconds = 0.25f;

        // Renewed every refresh while the item is pointed at, so the light goes out soon after the pointer leaves.
        private const float LitSeconds = 0.8f;

        private readonly ItemCatalog catalog;
        private readonly Func<ChestCategory, string?> chestName;
        private readonly Func<ChestCategory, (Sprite? Icon, Color Color)> chestLook;
        private readonly Func<IReadOnlyCollection<ChestCategory>, float, float, int> highlight;
        private readonly Func<float> range;

        // The tooltip line of each item, by prefab name; the same for every stack, and asked for at every redraw.
        private readonly Dictionary<string, string> lines = new();
        private float nextRefresh;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChestFinder"/> class.
        /// </summary>
        /// <param name="catalog">Tells which chests an item goes in.</param>
        /// <param name="chestName">Names a chest kind.</param>
        /// <param name="chestLook">Gives a chest kind's colour.</param>
        /// <param name="highlight">Lights up the chests of kinds within a range for a time; returns how many.</param>
        /// <param name="range">How far away chests light up; 0 for none.</param>
        public ChestFinder(ItemCatalog catalog, Func<ChestCategory, string?> chestName, Func<ChestCategory, (Sprite? Icon, Color Color)> chestLook,
            Func<IReadOnlyCollection<ChestCategory>, float, float, int> highlight, Func<float> range)
        {
            this.catalog = catalog;
            this.chestName = chestName;
            this.chestLook = chestLook;
            this.highlight = highlight;
            this.range = range;
        }

        /// <summary>
        /// Forgets the tooltip lines, e.g. after the chests' item lists change.
        /// </summary>
        public void Reset()
        {
            lines.Clear();
        }

        /// <summary>
        /// Lights up the chests for the item the pointer rests on. Call every frame.
        /// </summary>
        public void Update()
        {
            if (Time.time < nextRefresh) return;
            nextRefresh = Time.time + RefreshSeconds;

            var reach = range();
            var gui = InventoryGui.instance;
            if (reach <= 0f || gui == null || !InventoryGui.IsVisible() || gui.m_playerGrid == null) return;

            var grid = gui.m_playerGrid;
            var element = grid.GetHoveredElement();
            var item = element != null ? grid.m_inventory?.GetItemAt(element.Position.x, element.Position.y) : null;
            if (item?.m_dropPrefab == null) return;

            var categories = catalog.GetCategories(item.m_dropPrefab.name);
            if (categories.Count > 0) highlight(categories, reach, LitSeconds);
        }

        /// <summary>
        /// Names the chest an item in the player's own inventory belongs in, in its tooltip.
        /// </summary>
        public void HandleItemTooltip(object sender, ItemTooltipPatchEvent e)
        {
            var gui = InventoryGui.instance;
            if (gui == null || e.Grid != gui.m_playerGrid || e.Item.m_dropPrefab == null) return;

            var line = Line(e.Item.m_dropPrefab.name);
            if (line.Length > 0) e.Tooltip.Set(e.Tooltip.m_topic, $"{e.Tooltip.m_text}\n\n{line}", e.Grid.m_tooltipAnchor, Vector2.zero);
        }

        private string Line(string prefab)
        {
            if (lines.TryGetValue(prefab, out var cached)) return cached;

            var names = catalog.GetCategories(prefab)
                .Select(category => (Name: chestName(category), Look: chestLook(category)))
                .Where(chest => !string.IsNullOrEmpty(chest.Name))
                .Select(chest => $"<color=#{ColorUtility.ToHtmlStringRGB(chest.Look.Color)}>{chest.Name}</color>")
                .ToList();
            var line = names.Count == 0 ? string.Empty : Texts.Get("bsc_tooltip_chest", string.Join(" / ", names));
            lines[prefab] = line;
            return line;
        }
    }
}
