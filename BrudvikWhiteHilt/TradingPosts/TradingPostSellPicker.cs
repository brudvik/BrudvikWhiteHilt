using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Lets a player choose what to sell at a trading post. The game's store window has one sell button that sells the
/// first valuable it finds; at a post the button says which stack it sells and for how much, and a small button beside
/// it steps to the next stack the post buys. At Haldor's and Hildir's the window is as the game made it.
/// </summary>
public static class TradingPostSellPicker
{
    private const string NextButtonName = "whitehilt_sell_next";
    private const float Gap = 4f;

    private static int index;
    private static string originalLabel;

    /// <summary>
    /// Registers the button's text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_trade_sell", "Sell $1 (+$2)");
        Translations.AddEnglish("whitehilt_trade_sell_none", "Nothing to sell");
    }

    /// <summary>
    /// Starts at the first stack again, when the window opens.
    /// </summary>
    public static void Reset()
    {
        index = 0;
    }

    /// <summary>
    /// The stack the sell button sells now at a post, or null.
    /// </summary>
    /// <param name="biome">The post's biome index.</param>
    /// <param name="inventory">The player's inventory.</param>
    /// <returns>The stack, or null when the post buys nothing the player has.</returns>
    public static ItemDrop.ItemData Current(int biome, Inventory inventory)
    {
        List<ItemDrop.ItemData> stacks = Candidates(biome, inventory);
        if (stacks.Count == 0)
        {
            return null;
        }

        index = ((index % stacks.Count) + stacks.Count) % stacks.Count;
        return stacks[index];
    }

    /// <summary>
    /// Shows the post's choice on the window, every frame it is open: the sell button's text and the button that steps
    /// to the next stack; or the game's own window at any other trader.
    /// </summary>
    /// <param name="gui">The store window.</param>
    /// <param name="trader">The post's trader, or null at any other trader.</param>
    public static void Update(StoreGui gui, TradingPostTrader trader)
    {
        Button sell = gui.m_sellButton;
        if (sell == null)
        {
            return;
        }

        TMP_Text label = sell.GetComponentInChildren<TMP_Text>(true);
        Button next = NextButton(gui, sell);
        if (trader == null || Player.m_localPlayer == null)
        {
            next.gameObject.SetActive(false);
            if (label != null && originalLabel != null)
            {
                label.text = originalLabel;
            }

            return;
        }

        List<ItemDrop.ItemData> stacks = Candidates(trader.Biome, Player.m_localPlayer.GetInventory());
        next.gameObject.SetActive(stacks.Count > 1);
        if (label == null)
        {
            return;
        }

        originalLabel ??= label.text;
        ItemDrop.ItemData item = Current(trader.Biome, Player.m_localPlayer.GetInventory());
        label.enableAutoSizing = true;
        label.text = item == null
            ? Localization.instance.Localize("$whitehilt_trade_sell_none")
            : Localization.instance.Localize("$whitehilt_trade_sell",
                (item.m_stack > 1 ? item.m_stack + " " : string.Empty) + Localization.instance.Localize(item.m_shared.m_name),
                TradingPostStore.PayFor(trader.Biome, item).ToString());
    }

    private static List<ItemDrop.ItemData> Candidates(int biome, Inventory inventory)
    {
        return inventory.GetAllItemsInGridOrder().Where(item => !item.m_equipped && TradingPostStore.PayFor(biome, item) > 0).ToList();
    }

    // The step button, made once from a copy of the sell button: square, just left of it, without the sell button's own
    // click and gamepad key.
    private static Button NextButton(StoreGui gui, Button sell)
    {
        Transform existing = sell.transform.parent.Find(NextButtonName);
        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        GameObject copy = Object.Instantiate(sell.gameObject, sell.transform.parent);
        copy.name = NextButtonName;
        // The game's gamepad hint (UIGamepad, in an assembly the mod does not reference) would press this button with the
        // sell button's key.
        foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true).Where(behaviour => behaviour.GetType().Name == "UIGamepad"))
        {
            Object.Destroy(behaviour);
        }

        Button next = copy.GetComponent<Button>();
        next.onClick = new Button.ButtonClickedEvent();
        next.onClick.AddListener(() => index++);
        next.interactable = true;
        RectTransform from = (RectTransform)sell.transform;
        RectTransform to = (RectTransform)copy.transform;
        float side = from.rect.height;
        to.sizeDelta = new Vector2(side, from.sizeDelta.y);
        // The copy keeps the sell button's anchors and pivot: its right edge goes Gap left of the sell button's left edge.
        to.anchoredPosition = from.anchoredPosition - new Vector2(from.rect.width * from.pivot.x + Gap + side * (1f - from.pivot.x), 0f);
        TMP_Text text = copy.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "»";
        }

        return next;
    }
}
