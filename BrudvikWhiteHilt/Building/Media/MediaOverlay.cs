using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Building.Media;

/// <summary>
/// A screen overlay above everything else for films and photos: black fade, title card, countdown and short notices.
/// </summary>
public static class MediaOverlay
{
    private const float ToastSeconds = 3f;

    private static GameObject root;
    private static Image black;
    private static Text title;
    private static Text subtitle;
    private static Text center;
    private static Text toast;
    private static float toastUntil;

    /// <summary>
    /// Sets how black the screen is, 0 to 1.
    /// </summary>
    /// <param name="alpha">The blackness.</param>
    public static void SetBlack(float alpha)
    {
        if (Ensure())
        {
            black.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            black.gameObject.SetActive(alpha > 0.001f);
        }
    }

    /// <summary>
    /// Shows the title card, or hides it with an empty title and name.
    /// </summary>
    /// <param name="titleText">The film's title.</param>
    /// <param name="subtitleText">Name and date.</param>
    /// <param name="alpha">How visible the text is, 0 to 1.</param>
    public static void SetTitle(string titleText, string subtitleText, float alpha)
    {
        if (!Ensure())
        {
            return;
        }

        bool show = alpha > 0.001f;
        title.gameObject.SetActive(show);
        subtitle.gameObject.SetActive(show);
        title.text = titleText;
        subtitle.text = subtitleText;
        title.color = new Color(title.color.r, title.color.g, title.color.b, alpha);
        subtitle.color = new Color(subtitle.color.r, subtitle.color.g, subtitle.color.b, alpha);
    }

    /// <summary>
    /// Shows large text in the middle of the screen, such as the countdown; empty hides it.
    /// </summary>
    /// <param name="text">The text.</param>
    public static void SetCenter(string text)
    {
        if (Ensure())
        {
            center.text = text;
            center.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }

    /// <summary>
    /// Shows a short notice at the bottom of the screen, also when the HUD is hidden.
    /// </summary>
    /// <param name="text">The notice.</param>
    public static void Toast(string text)
    {
        if (Ensure())
        {
            toast.text = text;
            toastUntil = Time.unscaledTime + ToastSeconds;
        }
    }

    /// <summary>
    /// Hides the film parts of the overlay.
    /// </summary>
    public static void ClearFilm()
    {
        if (root != null)
        {
            SetBlack(0f);
            SetTitle(string.Empty, string.Empty, 0f);
            SetCenter(string.Empty);
        }
    }

    /// <summary>
    /// Fades notices out and keeps them off photos. Called every frame.
    /// </summary>
    public static void Tick()
    {
        if (root == null)
        {
            return;
        }

        bool show = Time.unscaledTime < toastUntil && !MediaMode.Capturing;
        toast.gameObject.SetActive(show);
        if (show)
        {
            float alpha = Mathf.Clamp01((toastUntil - Time.unscaledTime) / 0.5f);
            toast.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private static bool Ensure()
    {
        if (root != null)
        {
            return true;
        }

        if (GUIManager.Instance == null || GUIManager.Instance.AveriaSerifBold == null)
        {
            return false;
        }

        root = new GameObject("WhiteHiltMediaOverlay", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject blackObject = new("Black", typeof(RectTransform));
        blackObject.transform.SetParent(root.transform, false);
        Stretch((RectTransform)blackObject.transform);
        black = blackObject.AddComponent<Image>();
        black.raycastTarget = false;

        title = CreateText("Title", 72, new Vector2(0f, 60f), GUIManager.Instance.ValheimOrange);
        subtitle = CreateText("Subtitle", 32, new Vector2(0f, -30f), Color.white);
        center = CreateText("Center", 120, Vector2.zero, Color.white);
        toast = CreateText("Toast", 22, new Vector2(0f, -470f), Color.white);

        SetBlack(0f);
        SetTitle(string.Empty, string.Empty, 0f);
        SetCenter(string.Empty);
        toast.gameObject.SetActive(false);
        return true;
    }

    private static Text CreateText(string name, int size, Vector2 position, Color color)
    {
        Text text = GUIManager.Instance.CreateText(string.Empty, root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position,
            GUIManager.Instance.AveriaSerifBold, size, color, true, Color.black, 1800f, size * 1.6f, false).GetComponent<Text>();
        text.name = name;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
