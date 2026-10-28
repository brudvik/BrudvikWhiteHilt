using BrudvikWhiteHilt.Building;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Two hotbars: the travel bar and the build bar. The inactive one is stored in a hidden row of the inventory and shown
/// under the grid; switching swaps it with the first row. The bar switches by key, and on its own when a build tool is
/// taken out or put away.
/// </summary>
public static class HotbarSets
{
    private const string BuildActiveKey = "whitehilt_hotbar_build";
    private const float HudLabelOffset = 45f;

    private static bool tracking;
    private static bool toolWasHeld;
    private static Text hudLabel;

    // What the label was last made from; a change of the key clears labelMade.
    private static bool labelMade;
    private static bool labelBuild;
    private static string labelLanguage;
    private static bool keyWatched;

    /// <summary>
    /// True while the build bar is the active hotbar.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True for the build bar.</returns>
    public static bool BuildActive(Player player)
    {
        return player.m_customData.ContainsKey(BuildActiveKey);
    }

    /// <summary>
    /// Handles the switch key and the automatic switching. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        UpdateHudLabel(player);
        if (player.IsDead() || player.IsTeleporting() || !BackpackLayout.IsLocalInventory(player.m_inventory))
        {
            tracking = false;
            return;
        }

        if (!BackpackInput.Typing() && BackpackInput.Pressed(BackpackSettings.KeyHotbar))
        {
            Switch(player);
        }

        bool toolHeld = IsBuildTool(player.GetRightItem());
        if (tracking && toolHeld != toolWasHeld && BackpackSettings.AutoSwitchHotbar.Value && toolHeld != BuildActive(player))
        {
            Switch(player);
        }

        tracking = true;
        toolWasHeld = toolHeld;
    }

    /// <summary>
    /// Swaps the first row with the stored bar.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Switch(Player player)
    {
        InventoryGui gui = InventoryGui.instance;
        if (gui != null && gui.m_dragGo != null)
        {
            gui.SetupDragItem(null, null, 1);
        }

        Inventory inventory = player.m_inventory;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item.m_gridPos.y == 0)
            {
                item.m_gridPos = new Vector2i(item.m_gridPos.x, BackpackLayout.HotbarRow);
            }
            else if (item.m_gridPos.y == BackpackLayout.HotbarRow)
            {
                item.m_gridPos = new Vector2i(item.m_gridPos.x, 0);
            }
        }

        if (BuildActive(player))
        {
            player.m_customData.Remove(BuildActiveKey);
        }
        else
        {
            player.m_customData[BuildActiveKey] = "1";
        }

        inventory.Changed();
        player.Message(MessageHud.MessageType.TopLeft, BuildActive(player) ? "$whitehilt_hotbar_build" : "$whitehilt_hotbar_travel");
    }

    private static bool IsBuildTool(ItemDrop.ItemData item)
    {
        return item?.m_shared.m_buildPieces != null;
    }

    // The small label beside the hotbar on the HUD: which bar is active and the key that switches.
    private static void UpdateHudLabel(Player player)
    {
        if (hudLabel == null)
        {
            HotkeyBar bar = Hud.instance != null ? Hud.instance.GetComponentInChildren<HotkeyBar>(true) : null;
            if (bar == null || GUIManager.Instance == null)
            {
                return;
            }

            GameObject go = GUIManager.Instance.CreateText(string.Empty, bar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, GUIManager.Instance.AveriaSerifBold, 15, GUIManager.Instance.ValheimOrange, true, Color.black, 120f, 40f, false);
            go.name = "WhiteHiltHotbarLabel";
            hudLabel = go.GetComponent<Text>();
            hudLabel.alignment = TextAnchor.MiddleRight;
            hudLabel.raycastTarget = false;
            hudLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            hudLabel.rectTransform.localPosition = new Vector3(-HudLabelOffset, 0f, 0f);
            labelMade = false;
            if (!keyWatched)
            {
                keyWatched = true;
                BackpackSettings.KeyHotbar.SettingChanged += (_, _) => labelMade = false;
            }
        }

        // The White Hilt HUD has the food where the label would be.
        bool show = BackpackSettings.ShowHotbarLabel.Value && !player.IsDead() && !HudLayout.HudSettings.WhiteHilt;
        if (hudLabel.gameObject.activeSelf != show)
        {
            hudLabel.gameObject.SetActive(show);
        }

        if (!show)
        {
            return;
        }

        // Made again only when the bar, the key or the language changed, not every frame.
        bool build = BuildActive(player);
        string language = Localization.instance.GetSelectedLanguage();
        if (labelMade && build == labelBuild && language == labelLanguage)
        {
            return;
        }

        labelMade = true;
        labelBuild = build;
        labelLanguage = language;
        hudLabel.text = Localization.instance.Localize(build ? "$whitehilt_hotbar_short_build" : "$whitehilt_hotbar_short_travel")
            + "\n[" + BuildToolSettings.KeyName(BackpackSettings.KeyHotbar) + "]";
    }
}
