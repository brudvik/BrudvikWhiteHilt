using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Shows on a requirement's icon how many of it the local player has, ∞ when a chest nearby keeps it unlimited, and a
/// gold bar for how close the best chest is to making it unlimited. Used by the crafting panel and the build menu.
/// </summary>
public static class RequirementOverlay
{
    private const string BadgeName = "WhiteHiltHave";
    private const string BarName = "WhiteHiltUnlock";
    private const string Infinity = "\u221E";
    private const float BadgeWidth = 28f;
    private const float BadgeHeight = 17f;

    private static readonly Color BadgeColor = new(0f, 0f, 0f, 0.7f);
    private static readonly Color ShortColor = new(1f, 0.5f, 0.45f);
    private static readonly Color GoldColor = new(1f, 0.82f, 0.3f);
    private static readonly Color BarBackgroundColor = new(0f, 0f, 0f, 0.65f);

    /// <summary>
    /// Checks whether the overlay is on for this use.
    /// </summary>
    /// <param name="use">Crafting or building.</param>
    /// <returns>True if requirements show how many the player has.</returns>
    public static bool IsOn(NearbyContainers.Use use)
    {
        return CraftingPanelSettings.ShowAvailable != null && CraftingPanelSettings.ShowAvailable.Value &&
               (use != NearbyContainers.Use.Building || CraftingPanelSettings.ShowInBuildMenu.Value);
    }

    /// <summary>
    /// Fills the overlay of a requirement and adds what the player has to its tooltip.
    /// </summary>
    /// <param name="elementRoot">The requirement element.</param>
    /// <param name="resItem">The required item.</param>
    /// <param name="own">How many are in the player's inventory.</param>
    /// <param name="inChests">How many are in the chests around the player that may be used.</param>
    /// <param name="need">How many the recipe or piece needs.</param>
    /// <param name="use">Crafting or building.</param>
    /// <param name="chestsActive">Whether nearby chests are used right now.</param>
    public static void Show(Transform elementRoot, ItemDrop resItem, int own, int inChests, int need, NearbyContainers.Use use, bool chestsActive)
    {
        ItemDrop.ItemData.SharedData shared = resItem.m_itemData.m_shared;
        string prefabName = resItem.gameObject.name;
        IUnlimitedItems source = NearbyContainers.Unlimited;
        bool unlimited = chestsActive && NearbyContainers.IsUnlimitedNearby(use, shared.m_name);
        string unlimitedChest = unlimited || source == null ? null : source.GetUnlimitedChest(prefabName, shared);
        int stored = 0;
        int required = 0;
        bool unlockable = !unlimited && unlimitedChest == null && source != null && source.TryGetUnlockProgress(prefabName, shared, out stored, out required);

        TMP_Text amount = elementRoot.Find("res_amount")?.GetComponent<TMP_Text>();
        TMP_Text badge = GetBadge(elementRoot, amount);
        int total = own + inChests;
        if (unlimited)
        {
            bool hasGlyph = badge.font == null || badge.font.HasCharacter(Infinity[0], true, true);
            badge.text = hasGlyph ? Infinity : "MAX";
            badge.color = GoldColor;
        }
        else
        {
            badge.text = Compact(total);
            badge.color = total >= need ? Color.white : ShortColor;
        }

        SetActive(badge.transform.parent.gameObject, true);
        ShowBar(elementRoot, unlockable && CraftingPanelSettings.ShowUnlockProgress.Value, required == 0 ? 0f : (float)stored / required);

        UITooltip tooltip = elementRoot.GetComponent<UITooltip>();
        if (tooltip == null)
        {
            return;
        }

        tooltip.m_text += "\n" + (inChests > 0
            ? string.Format(Localize("$whitehilt_req_have_split"), total, own, inChests)
            : string.Format(Localize("$whitehilt_req_have"), total));
        if (unlimited)
        {
            tooltip.m_text += "\n" + Localize("$whitehilt_req_unlimited");
        }
        else if (unlimitedChest != null)
        {
            tooltip.m_text += "\n" + string.Format(Localize(chestsActive ? "$whitehilt_req_unlimited_elsewhere" : "$whitehilt_req_unlimited_off"), unlimitedChest);
        }
        else if (unlockable)
        {
            tooltip.m_text += "\n" + string.Format(Localize("$whitehilt_req_unlock"), required - stored, stored, required);
        }
    }

    /// <summary>
    /// Hides the overlay of a requirement element that shows nothing or something else, like the build menu's station.
    /// </summary>
    /// <param name="elementRoot">The requirement element.</param>
    public static void Hide(Transform elementRoot)
    {
        if (elementRoot == null)
        {
            return;
        }

        Transform badge = elementRoot.Find(BadgeName);
        if (badge != null)
        {
            SetActive(badge.gameObject, false);
        }

        Transform bar = elementRoot.Find(BarName);
        if (bar != null)
        {
            SetActive(bar.gameObject, false);
        }
    }

    // 1234 is shown as 1.2k so it fits in the corner of the icon.
    private static string Compact(int value)
    {
        if (value < 1000)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        return value < 10000
            ? (value / 100 / 10f).ToString("0.#", CultureInfo.InvariantCulture) + "k"
            : (value / 1000).ToString(CultureInfo.InvariantCulture) + "k";
    }

    private static string Localize(string token)
    {
        return Localization.instance.Localize(token);
    }

    private static void SetActive(GameObject gameObject, bool active)
    {
        if (gameObject.activeSelf != active)
        {
            gameObject.SetActive(active);
        }
    }

    // A dark box on the left of the icon, between the item name at the top and the amount at the bottom.
    private static TMP_Text GetBadge(Transform elementRoot, TMP_Text template)
    {
        Transform found = elementRoot.Find(BadgeName);
        if (found != null)
        {
            return found.GetComponentInChildren<TMP_Text>(true);
        }

        GameObject root = new(BadgeName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(elementRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(1f, 2f);
        rect.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);
        Image background = root.GetComponent<Image>();
        background.color = BadgeColor;
        background.raycastTarget = false;

        GameObject textObject = new("text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(1f, 0f);
        textRect.offsetMax = new Vector2(-1f, 0f);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (template != null)
        {
            text.font = template.font;
            text.fontSharedMaterial = template.fontSharedMaterial;
        }

        text.enableAutoSizing = true;
        text.fontSizeMin = 8f;
        text.fontSizeMax = 14f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    // A thin bar along the bottom of the icon, under the amount.
    private static void ShowBar(Transform elementRoot, bool show, float fraction)
    {
        Transform bar = elementRoot.Find(BarName);
        if (bar == null)
        {
            if (!show)
            {
                return;
            }

            bar = CreateBar(elementRoot);
        }

        SetActive(bar.gameObject, show);
        if (show)
        {
            ((RectTransform)bar.GetChild(0)).anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }
    }

    private static Transform CreateBar(Transform elementRoot)
    {
        GameObject root = new(BarName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(elementRoot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(4f, 2f);
        rect.offsetMax = new Vector2(-4f, 5f);
        Image background = root.GetComponent<Image>();
        background.color = BarBackgroundColor;
        background.raycastTarget = false;

        GameObject fill = new("fill", typeof(RectTransform), typeof(Image));
        RectTransform fillRect = (RectTransform)fill.transform;
        fillRect.SetParent(rect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        Image image = fill.GetComponent<Image>();
        image.color = GoldColor;
        image.raycastTarget = false;
        return rect;
    }
}
