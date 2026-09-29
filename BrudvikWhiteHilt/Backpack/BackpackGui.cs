using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Lays out the player's inventory grid: hides the rows not in use and shows the stored hotbar under the grid.
/// The slots stay vanilla inventory elements, so dragging, tooltips and right-click work as usual.
/// </summary>
public static class BackpackGui
{
    /// <summary>Space between the grid and the stored hotbar, for its label.</summary>
    public const float BarGap = 26f;

    private static Text barLabel;

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
        if (elements.Count != BackpackLayout.Width * BackpackLayout.TotalRows)
        {
            return;
        }

        int rows = BackpackLayout.VisibleRows(player);
        float barOffset = rows * grid.m_elementSpace + BarGap;
        foreach (InventoryElement element in elements)
        {
            Vector2i pos = element.Position;
            switch (BackpackLayout.KindAt(pos, rows))
            {
                case SlotKind.Grid:
                    SetActive(element, true);
                    break;
                case SlotKind.Hotbar:
                    SetActive(element, true);
                    RectTransform top = (RectTransform)elements[pos.x].transform;
                    Place((RectTransform)element.transform, top.anchoredPosition + new Vector2(0f, -barOffset));
                    break;
                default:
                    SetActive(element, false);
                    break;
            }
        }

        grid.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (rows + 1) * grid.m_elementSpace + BarGap);
        UpdateBarLabel(grid, player, rows);
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
}
