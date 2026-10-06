using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Arrows next to the Craft button that choose how many to craft at once. It drives vanilla's multi-crafting (Shift +
/// Craft, five at a time), so the requirements, the button text and the crafting itself follow the chosen amount.
/// </summary>
public static class CraftAmountSelector
{
    private const string RootName = "WhiteHiltCraftAmount";
    private const float Width = 110f;
    private const float Gap = 4f;
    private const float ArrowSize = 36f;

    private static readonly Color DisabledColor = new(0.6f, 0.6f, 0.6f);

    private static InventoryGui builtFor;
    private static RectTransform root;
    private static Button down;
    private static Button up;
    private static TMP_Text label;
    private static Vector2 craftButtonOffsetMin;
    private static int vanillaMultiAmount = -1;
    private static bool forcedMulti;
    private static int amount = 1;
    private static Recipe lastRecipe;
    private static ItemDrop.ItemData lastItem;

    /// <summary>
    /// Builds the arrows, starts over at one for a new recipe and hands the amount to vanilla. Call before vanilla
    /// updates the recipe.
    /// </summary>
    /// <param name="gui">The inventory screen.</param>
    public static void BeforeUpdate(InventoryGui gui)
    {
        EnsureBuilt(gui);
        Recipe recipe = gui.m_selectedRecipe.Recipe;
        ItemDrop.ItemData item = gui.m_selectedRecipe.ItemData;
        if (recipe != lastRecipe || item != lastItem)
        {
            lastRecipe = recipe;
            lastItem = item;
            amount = 1;
        }

        amount = Mathf.Clamp(amount, 1, MaxAmount());
        Apply(gui);
    }

    /// <summary>
    /// Shows the arrows and the amount, and reads the mouse wheel over them. Call after vanilla updates the recipe.
    /// </summary>
    /// <param name="gui">The inventory screen.</param>
    public static void AfterUpdate(InventoryGui gui)
    {
        if (root == null || builtFor != gui)
        {
            return;
        }

        bool enabled = CraftingPanelSettings.AmountSelector.Value;
        RectTransform craftRect = (RectTransform)gui.m_craftButton.transform;
        Vector2 offsetMin = enabled ? craftButtonOffsetMin + new Vector2(Width + Gap, 0f) : craftButtonOffsetMin;
        if (craftRect.offsetMin != offsetMin)
        {
            craftRect.offsetMin = offsetMin;
        }

        bool show = enabled && gui.m_craftTimer < 0f && gui.m_selectedRecipe.Recipe != null;
        if (root.gameObject.activeSelf != show)
        {
            root.gameObject.SetActive(show);
        }

        if (!show)
        {
            return;
        }

        bool multi = CanMultiCraft(gui);
        if (multi && IsPointerOver())
        {
            float wheel = Input.mouseScrollDelta.y;
            if (wheel != 0f)
            {
                Change(wheel > 0f ? 1 : -1);
            }
        }

        int max = MaxAmount();
        down.interactable = multi && amount > 1;
        up.interactable = multi && amount < max;
        label.text = (multi ? amount : 1).ToString();
        label.color = multi ? Color.white : DisabledColor;
    }

    /// <summary>
    /// Hands the amount to vanilla right before the Craft button starts crafting.
    /// </summary>
    /// <param name="gui">The inventory screen.</param>
    public static void BeforeCraft(InventoryGui gui)
    {
        Apply(gui);
    }

    /// <summary>
    /// How many of the selected recipe the Craft button makes at once: the chosen amount, or one.
    /// </summary>
    /// <param name="gui">The inventory screen.</param>
    /// <returns>The amount.</returns>
    public static int SelectedAmount(InventoryGui gui)
    {
        return CraftingPanelSettings.AmountSelector != null && CraftingPanelSettings.AmountSelector.Value && CanMultiCraft(gui) ? amount : 1;
    }

    private static int MaxAmount()
    {
        return Math.Max(1, CraftingPanelSettings.MaxCraftAmount.Value);
    }

    // Vanilla crafts several at once only when nothing is being upgraded.
    private static bool CanMultiCraft(InventoryGui gui)
    {
        return gui.m_selectedRecipe.Recipe != null && gui.m_selectedRecipe.ItemData == null;
    }

    // The touch flag is vanilla's own "craft several" switch; Shift + Craft keeps its five when the amount is one.
    private static void Apply(InventoryGui gui)
    {
        if (vanillaMultiAmount < 0)
        {
            vanillaMultiAmount = gui.m_multiCraftAmount;
        }

        // A craft in progress finishes with the amount it started with.
        if (gui.m_craftTimer >= 0f)
        {
            return;
        }

        if (CraftingPanelSettings.AmountSelector.Value && amount > 1 && CanMultiCraft(gui))
        {
            gui.m_multiCraftAmount = amount;
            gui.m_touchMultiCrafting = true;
            forcedMulti = true;
            return;
        }

        gui.m_multiCraftAmount = vanillaMultiAmount;
        if (forcedMulti)
        {
            gui.m_touchMultiCrafting = false;
            forcedMulti = false;
        }
    }

    private static void Change(int step)
    {
        amount = Mathf.Clamp(amount + step, 1, MaxAmount());
    }

    private static bool IsPointerOver()
    {
        Canvas canvas = root.GetComponentInParent<Canvas>();
        Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(root, Input.mousePosition, camera);
    }

    // Adds the craft amount arrows to the crafting panel, again when the inventory is rebuilt for another world. A
    // failure is logged and the panel works as in vanilla.
    private static void EnsureBuilt(InventoryGui gui)
    {
        if (builtFor == gui)
        {
            return;
        }

        builtFor = gui;
        root = null;
        try
        {
            Build(gui);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"Could not add the craft amount arrows: {ex}");
        }
    }

    // On the left of the Craft button, which is narrowed to make room.
    private static void Build(InventoryGui gui)
    {
        RectTransform craftRect = (RectTransform)gui.m_craftButton.transform;
        craftButtonOffsetMin = craftRect.offsetMin;

        GameObject rootObject = new(RootName, typeof(RectTransform));
        RectTransform rect = (RectTransform)rootObject.transform;
        rect.SetParent(craftRect.parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(craftButtonOffsetMin.x, craftButtonOffsetMin.y);
        rect.offsetMax = new Vector2(craftButtonOffsetMin.x + Width, craftRect.offsetMax.y);

        down = CreateArrow(gui.m_qualityLevelDown, rect, "Down", "<", 0f, () => Change(-1));
        up = CreateArrow(gui.m_qualityLevelUp, rect, "Up", ">", 1f, () => Change(1));

        GameObject labelObject = new("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(ArrowSize, 0f);
        labelRect.offsetMax = new Vector2(-ArrowSize, 0f);
        label = labelObject.GetComponent<TextMeshProUGUI>();
        TMP_Text template = gui.m_craftButton.GetComponentInChildren<TMP_Text>(true);
        if (template != null)
        {
            label.font = template.font;
            label.fontSharedMaterial = template.fontSharedMaterial;
        }

        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.text = "1";

        root = rect;
    }

    // A copy of vanilla's quality arrow, so it looks and sounds the same.
    private static Button CreateArrow(Button source, RectTransform parent, string name, string fallbackText, float side, Action onClick)
    {
        GameObject copy;
        if (source != null)
        {
            copy = Object.Instantiate(source.gameObject, parent, false);
            foreach (UIGamePad pad in copy.GetComponentsInChildren<UIGamePad>(true))
            {
                if (pad.m_hint != null)
                {
                    Object.Destroy(pad.m_hint);
                }

                Object.Destroy(pad);
            }

            foreach (UITooltip tooltip in copy.GetComponentsInChildren<UITooltip>(true))
            {
                Object.Destroy(tooltip);
            }
        }
        else
        {
            copy = Jotunn.Managers.GUIManager.Instance.CreateButton(fallbackText, parent, Vector2.zero, Vector2.zero, Vector2.zero, ArrowSize, ArrowSize);
        }

        copy.name = name;
        copy.SetActive(true);
        RectTransform rect = (RectTransform)copy.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(side, 0.5f);
        rect.pivot = new Vector2(side, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(ArrowSize, ArrowSize);

        Button button = copy.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => onClick());
        return button;
    }
}
