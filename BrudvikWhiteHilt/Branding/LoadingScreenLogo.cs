using BrudvikWhiteHilt.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Branding;

/// <summary>
/// Turns the vanilla loading screen black with the White Hilt logo in the middle, when entering a world, teleporting
/// and respawning. The tip and progress text stay where they are. Sleeping keeps the vanilla screen.
/// </summary>
public static class LoadingScreenLogo
{
    private const string ObjectName = "WhiteHiltLoadingLogo";

    // Share of the loading image's height, and how far the logo sits above the middle to leave room for the tip.
    private const float LogoHeight = 0.4f;
    private const float LogoLift = 0.05f;

    private static GameObject logo;
    private static Sprite vanillaSprite;
    private static Color vanillaColor;
    private static bool shown;

    /// <summary>
    /// Adds the hidden logo to the loading image of a new HUD.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void Create(Hud hud)
    {
        Image image = hud.m_loadingImage;
        if (VisualHelper.IsHeadless || image == null)
        {
            return;
        }

        vanillaSprite = image.sprite;
        vanillaColor = image.color;
        shown = false;

        logo = new GameObject(ObjectName, typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        RectTransform rect = logo.GetComponent<RectTransform>();
        rect.SetParent(image.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f - LogoHeight / 2f + LogoLift);
        rect.anchorMax = new Vector2(0.5f, 0.5f + LogoHeight / 2f + LogoLift);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        AspectRatioFitter fitter = logo.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        fitter.aspectRatio = 1f;

        Image logoImage = logo.GetComponent<Image>();
        logoImage.sprite = WhiteHiltLogo.Sprite;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
        logo.SetActive(false);
    }

    /// <summary>
    /// Shows the logo while the loading screen is up and the player is not sleeping. Call after the HUD updates it.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void Refresh(Hud hud)
    {
        if (logo == null)
        {
            return;
        }

        bool show = BrandingSettings.LoadingScreenLogo.Value && hud.m_loadingScreen.gameObject.activeSelf
            && (hud.m_sleepingProgress == null || !hud.m_sleepingProgress.activeSelf);
        if (show == shown)
        {
            return;
        }

        shown = show;
        Image image = hud.m_loadingImage;
        image.sprite = show ? null : vanillaSprite;
        image.color = show ? Color.black : vanillaColor;
        logo.SetActive(show);
    }
}
