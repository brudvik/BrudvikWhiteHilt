using BrudvikWhiteHilt.Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Branding;

/// <summary>
/// The White Hilt logo with the mod version in the top right corner of the main menu.
/// </summary>
public static class MainMenuLogo
{
    private const string ObjectName = "WhiteHiltMenuLogo";
    private const float Size = 140f;
    private const float LabelHeight = 24f;
    private const float LabelExtraWidth = 120f;
    private const float Margin = 24f;

    private static GameObject current;
    private static bool subscribed;

    /// <summary>
    /// Adds the logo to the main menu once, hidden while the setting is off.
    /// </summary>
    /// <param name="startup">The main menu.</param>
    public static void Create(FejdStartup startup)
    {
        if (!subscribed)
        {
            subscribed = true;
            BrandingSettings.MainMenuLogo.SettingChanged += (_, _) => ApplySetting();
        }

        if (VisualHelper.IsHeadless || startup.m_mainMenu == null || startup.m_mainMenu.transform.Find(ObjectName) != null)
        {
            return;
        }

        RectTransform root = new GameObject(ObjectName, typeof(RectTransform)).GetComponent<RectTransform>();
        current = root.gameObject;
        ApplySetting();
        root.SetParent(startup.m_mainMenu.transform, false);
        // The bottom right holds the vanilla version label, merch store button and modded notice.
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(-Margin, -Margin);
        root.sizeDelta = new Vector2(Size, Size + LabelHeight);

        RectTransform logo = new GameObject("Logo", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        logo.SetParent(root, false);
        logo.anchorMin = logo.anchorMax = logo.pivot = new Vector2(0.5f, 1f);
        logo.anchoredPosition = Vector2.zero;
        logo.sizeDelta = new Vector2(Size, Size);
        Image image = logo.GetComponent<Image>();
        image.sprite = WhiteHiltLogo.Sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        // A copy of the vanilla version label, so the font and style match the menu.
        if (startup.m_versionLabel != null)
        {
            TMP_Text label = Object.Instantiate(startup.m_versionLabel, root);
            label.name = "Version";
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(LabelExtraWidth, LabelHeight);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.text = "White Hilt v" + global::BrudvikWhiteHilt.BrudvikWhiteHilt.PluginVersion;
        }
    }

    private static void ApplySetting()
    {
        if (current != null)
        {
            current.SetActive(BrandingSettings.MainMenuLogo.Value);
        }
    }
}
