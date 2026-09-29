using Jotunn.Managers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Lays out the player's inventory grid: hides the rows not in use, shows the stored hotbar under the grid and moves the
/// equipment slots into a panel next to the inventory. The slots stay vanilla inventory elements, so dragging, tooltips
/// and right-click work as usual.
/// </summary>
public static class BackpackGui
{
    /// <summary>Space between the grid and the stored hotbar, for its label.</summary>
    public const float BarGap = 26f;

    private const float Padding = 14f;
    private const float TitleHeight = 24f;
    private const float SectionGap = 8f;
    private const int PanelColumns = 5;

    private static readonly Color PlaceholderColor = new(1f, 1f, 1f, 0.2f);

    private static readonly PanelSection[] sections =
    {
        new("$whitehilt_backpack_equipment", Row(BackpackLayout.GearRow, 0, 5)),
        new("$whitehilt_backpack_food", Row(BackpackLayout.GearRow, FoodSlots.FirstColumn, FoodSlots.Count))
    };

    private static readonly Dictionary<SlotKind, SlotLook> looks = new()
    {
        { SlotKind.Helmet, new SlotLook("HelmetLeather", "$whitehilt_backpack_helmet") },
        { SlotKind.Chest, new SlotLook("ArmorLeatherChest", "$whitehilt_backpack_chest") },
        { SlotKind.Legs, new SlotLook("ArmorLeatherLegs", "$whitehilt_backpack_legs") },
        { SlotKind.Cape, new SlotLook("CapeDeerHide", "$whitehilt_backpack_cape") },
        { SlotKind.Trinket, new SlotLook("TrinketBronzeHealth", "$whitehilt_backpack_trinket") },
        { SlotKind.Food, new SlotLook("CookedMeat", "$whitehilt_backpack_food_slot", "$whitehilt_backpack_food_hint") }
    };

    private static readonly Dictionary<Vector2i, Vector2i> panelPlaces = BuildPanelPlaces();

    private static Text barLabel;
    private static RectTransform panel;

    /// <summary>
    /// Sizes the inventory window for the visible rows and the stored hotbar.
    /// </summary>
    /// <param name="gui">The inventory window.</param>
    /// <param name="visibleRows">Rows shown in the grid.</param>
    public static void ResizeWindow(InventoryGui gui, int visibleRows)
    {
        float height = gui.m_playerHeight + (visibleRows + 1 - 4) * gui.m_invGridHeight + BarGap;
        gui.m_player.sizeDelta = new Vector2(gui.m_player.sizeDelta.x, height);
    }

    /// <summary>
    /// Places the slots of the player's grid. Called after every vanilla update of the grid.
    /// </summary>
    /// <param name="grid">The player's inventory grid.</param>
    /// <param name="player">The local player.</param>
    public static void Arrange(InventoryGrid grid, Player player)
    {
        List<InventoryElement> elements = grid.m_elements;
        InventoryGui gui = InventoryGui.instance;
        if (gui == null || elements.Count != BackpackLayout.Width * BackpackLayout.TotalRows)
        {
            return;
        }

        int rows = BackpackLayout.VisibleRows(player);
        float step = grid.m_elementSpace;
        float barOffset = rows * step + BarGap;
        Vector2 size = ((RectTransform)elements[0].transform).rect.size;
        EnsurePanel(gui, size, step);

        foreach (InventoryElement element in elements)
        {
            Vector2i pos = element.Position;
            SlotKind kind = BackpackLayout.KindAt(pos, rows);
            switch (kind)
            {
                case SlotKind.Grid:
                    SetActive(element, true);
                    break;
                case SlotKind.Hotbar:
                    SetActive(element, true);
                    RectTransform top = (RectTransform)elements[pos.x].transform;
                    Place((RectTransform)element.transform, top.anchoredPosition + new Vector2(0f, -barOffset));
                    break;
                case SlotKind.Void:
                    SetActive(element, false);
                    break;
                default:
                    SetActive(element, panelPlaces.ContainsKey(pos));
                    PlaceInPanel(element, size, step);
                    ShowPlaceholder(element, kind);
                    ShowFoodKey(element, kind);
                    break;
            }
        }

        grid.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (rows + 1) * step + BarGap);
        UpdateBarLabel(grid, player, rows);
    }

    private static Vector2i[] Row(int y, int fromX, int count)
    {
        Vector2i[] slots = new Vector2i[count];
        for (int i = 0; i < count; i++)
        {
            slots[i] = new Vector2i(fromX + i, y);
        }

        return slots;
    }

    private static Dictionary<Vector2i, Vector2i> BuildPanelPlaces()
    {
        Dictionary<Vector2i, Vector2i> places = new();
        for (int section = 0; section < sections.Length; section++)
        {
            for (int column = 0; column < sections[section].Slots.Length; column++)
            {
                places[sections[section].Slots[column]] = new Vector2i(column, section);
            }
        }

        return places;
    }

    private static float SectionTop(int section, float slotHeight)
    {
        return Padding + section * (TitleHeight + slotHeight + SectionGap);
    }

    private static void EnsurePanel(InventoryGui gui, Vector2 size, float step)
    {
        if (panel == null || panel.parent != gui.m_player)
        {
            float width = Padding * 2f + (PanelColumns - 1) * step + size.x;
            float height = SectionTop(sections.Length, size.y) - SectionGap + Padding;
            GameObject go = GUIManager.Instance.CreateWoodpanel(gui.m_player, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero,
                width, height, false);
            go.name = "WhiteHiltEquipmentPanel";
            panel = (RectTransform)go.transform;
            panel.pivot = new Vector2(0f, 1f);
            for (int i = 0; i < sections.Length; i++)
            {
                AddTitle(sections[i].Title, width - Padding * 2f, SectionTop(i, size.y));
            }
        }

        Place(panel, new Vector2(BackpackSettings.PanelOffsetX.Value, BackpackSettings.PanelOffsetY.Value));
    }

    private static void AddTitle(string title, float width, float top)
    {
        GameObject go = GUIManager.Instance.CreateText(Localization.instance.Localize(title), panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, GUIManager.Instance.AveriaSerifBold, 16, GUIManager.Instance.ValheimOrange, true, Color.black, width, TitleHeight, false);
        Text text = go.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        text.rectTransform.pivot = new Vector2(0f, 1f);
        text.rectTransform.anchoredPosition = new Vector2(Padding, -top);
    }

    private static void PlaceInPanel(InventoryElement element, Vector2 size, float step)
    {
        if (!panelPlaces.TryGetValue(element.Position, out Vector2i place))
        {
            return;
        }

        RectTransform rect = (RectTransform)element.transform;
        if (rect.parent != panel)
        {
            rect.SetParent(panel, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
        }

        float left = Padding + place.x * step;
        float top = -(SectionTop(place.y, size.y) + TitleHeight);
        Place(rect, new Vector2(left + rect.pivot.x * size.x, top - (1f - rect.pivot.y) * size.y));
    }

    private static void ShowPlaceholder(InventoryElement element, SlotKind kind)
    {
        if (element.m_used || !looks.TryGetValue(kind, out SlotLook look))
        {
            return;
        }

        if (look.Icon == null && ObjectDB.instance != null)
        {
            look.Icon = ObjectDB.instance.GetItemPrefab(look.Prefab)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        }

        if (look.Icon != null)
        {
            element.m_icon.enabled = true;
            element.m_icon.sprite = look.Icon;
            element.m_icon.color = PlaceholderColor;
        }

        element.m_tooltip.m_topic = Localization.instance.Localize(look.Name);
        element.m_tooltip.m_text = Localization.instance.Localize(look.Hint);
    }

    private static void ShowFoodKey(InventoryElement element, SlotKind kind)
    {
        if (kind != SlotKind.Food || element.transform.Find("binding")?.GetComponent<TMP_Text>() is not TMP_Text binding)
        {
            return;
        }

        string key = Building.BuildToolSettings.KeyName(FoodSlots.Key(element.Position.x - FoodSlots.FirstColumn));
        binding.enabled = key.Length > 0;
        if (binding.text != key)
        {
            binding.text = key;
        }
    }

    private static void UpdateBarLabel(InventoryGrid grid, Player player, int rows)
    {
        if (barLabel == null || barLabel.transform.parent != grid.m_gridRoot)
        {
            GameObject go = GUIManager.Instance.CreateText(string.Empty, grid.m_gridRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                Vector2.zero, GUIManager.Instance.AveriaSerifBold, 14, GUIManager.Instance.ValheimOrange, true, Color.black, 400f, 22f, false);
            go.name = "WhiteHiltStoredBarLabel";
            barLabel = go.GetComponent<Text>();
            barLabel.alignment = TextAnchor.MiddleLeft;
            barLabel.raycastTarget = false;
        }

        RectTransform first = (RectTransform)grid.m_elements[0].transform;
        RectTransform label = barLabel.rectTransform;
        label.anchorMin = first.anchorMin;
        label.anchorMax = first.anchorMax;
        label.pivot = new Vector2(0f, 1f);
        float left = first.anchoredPosition.x - first.rect.width * first.pivot.x;
        float rowTop = first.anchoredPosition.y + first.rect.height * (1f - first.pivot.y);
        float lastRowBottom = rowTop - (rows - 1) * grid.m_elementSpace - first.rect.height;
        Place(label, new Vector2(left, lastRowBottom - 2f));

        string stored = HotbarSets.BuildActive(player) ? "$whitehilt_hotbar_travel" : "$whitehilt_hotbar_build";
        string text = string.Format(Localization.instance.Localize("$whitehilt_hotbar_stored"),
            Localization.instance.Localize(stored), Building.BuildToolSettings.KeyName(BackpackSettings.KeyHotbar));
        if (barLabel.text != text)
        {
            barLabel.text = text;
        }
    }

    private static void SetActive(InventoryElement element, bool active)
    {
        if (element.gameObject.activeSelf != active)
        {
            element.gameObject.SetActive(active);
        }
    }

    private static void Place(RectTransform rect, Vector2 position)
    {
        if (rect.anchoredPosition != position)
        {
            rect.anchoredPosition = position;
        }
    }

    private sealed class PanelSection
    {
        public PanelSection(string title, Vector2i[] slots)
        {
            Title = title;
            Slots = slots;
        }

        public string Title { get; }

        public Vector2i[] Slots { get; }
    }

    private sealed class SlotLook
    {
        public SlotLook(string prefab, string name, string hint = "$whitehilt_backpack_slot_hint")
        {
            Prefab = prefab;
            Name = name;
            Hint = hint;
        }

        public string Prefab { get; }

        public string Name { get; }

        public string Hint { get; }

        public Sprite Icon { get; set; }
    }
}
