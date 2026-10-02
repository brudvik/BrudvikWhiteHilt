using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Compass;

/// <summary>
/// A compass in the bottom-right corner of the minimap and the top-left corner of the large map. The map is north up,
/// so the letters stay put and the needle turns to where the camera looks, with the bearing in degrees under it. The
/// letters follow the game's language. The dial and needle are rendered from the Seadogs Compass by
/// AssetSource/make_compass.py.
/// </summary>
public static class MapCompass
{
    private const string DialResource = "BrudvikWhiteHilt.Assets.Compass.CompassDial.png";
    private const string NeedleResource = "BrudvikWhiteHilt.Assets.Compass.CompassNeedle.png";
    private const float SmallMargin = 10f;
    private const float LargeMargin = 16f;

    // Where the dial's plain band is (painted free of letters by make_compass.py), as a share of its half width.
    private const float LetterRadius = 0.74f;
    private const float LetterShare = 0.17f;
    private const float DegreeShare = 0.2f;
    private const int MinDegreeFont = 11;
    private const int MaxDegreeFont = 16;

    private static readonly string[] letterKeys = { "$whitehilt_compass_n", "$whitehilt_compass_e", "$whitehilt_compass_s", "$whitehilt_compass_w" };
    private static readonly Color letterColour = new(0.11f, 0.09f, 0.06f, 1f);
    private static readonly Color northColour = new(0.62f, 0.07f, 0.04f, 1f);

    private static Sprite dial;
    private static Sprite needle;
    private static bool failed;
    private static CompassView small;
    private static CompassView large;

    /// <summary>
    /// Shows, hides and turns the compasses. Called after the map's own update.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        if (failed || map.m_smallRoot == null || map.m_largeRoot == null)
        {
            return;
        }

        Camera camera = Utils.GetMainCamera();
        bool known = camera != null && Player.m_localPlayer != null;
        float yaw = known ? camera.transform.eulerAngles.y : 0f;
        small = Show(small, map.m_smallRoot.transform, known && CompassSettings.Minimap.Value, CompassSettings.MinimapSize.Value,
            new Vector2(1f, 0f), new Vector2(-SmallMargin, SmallMargin), yaw);
        large = Show(large, map.m_largeRoot.transform, known && CompassSettings.LargeMap.Value, CompassSettings.LargeMapSize.Value,
            new Vector2(0f, 1f), new Vector2(LargeMargin, -LargeMargin), yaw);
    }

    private static CompassView Show(CompassView view, Transform parent, bool visible, float size, Vector2 corner, Vector2 offset, float yaw)
    {
        if (!visible)
        {
            if (view != null)
            {
                view.Root.SetActive(false);
            }

            return view;
        }

        if (view == null)
        {
            view = Create(parent, corner, offset);
            if (view == null)
            {
                return null;
            }
        }

        view.Root.SetActive(true);
        if (view.Root.transform.GetSiblingIndex() != parent.childCount - 1)
        {
            view.Root.transform.SetAsLastSibling();
        }

        bool degrees = CompassSettings.Degrees.Value;
        view.Layout(size, degrees);
        view.Needle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        int bearing = Mathf.RoundToInt(yaw) % 360;
        if (degrees && bearing != view.Bearing)
        {
            view.Bearing = bearing;
            view.Degrees.text = bearing + "°";
        }

        return view;
    }

    private static CompassView Create(Transform parent, Vector2 corner, Vector2 offset)
    {
        if (dial == null || needle == null)
        {
            try
            {
                dial = LoadSprite(DialResource);
                needle = LoadSprite(NeedleResource);
            }
            catch (Exception ex)
            {
                failed = true;
                Jotunn.Logger.LogWarning($"Compass: could not load its pictures: {ex.Message}");
                return null;
            }
        }

        GameObject root = new("WhiteHiltCompass", typeof(RectTransform));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = corner;
        rect.anchoredPosition = offset;

        CompassView view = new() { Root = root, Rect = rect };
        view.Dial = AddImage(rect, "Dial", dial);
        RectTransform face = view.Dial.rectTransform;
        face.anchorMin = face.anchorMax = face.pivot = new Vector2(0.5f, 1f);

        view.Letters = new Text[letterKeys.Length];
        for (int i = 0; i < letterKeys.Length; i++)
        {
            Text letter = AddText(face, "Letter", Localization.instance.Localize(letterKeys[i]), i == 0 ? northColour : letterColour, false);
            Shadow shadow = letter.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(1f, 1f, 1f, 0.3f);
            shadow.effectDistance = new Vector2(1f, -1f);
            view.Letters[i] = letter;
        }

        view.Needle = AddImage(face, "Needle", needle);
        RectTransform pointer = view.Needle.rectTransform;
        pointer.anchorMin = Vector2.zero;
        pointer.anchorMax = Vector2.one;
        pointer.sizeDelta = Vector2.zero;

        view.Degrees = AddText(rect, "Degrees", string.Empty, Color.white, true);
        RectTransform degrees = view.Degrees.rectTransform;
        degrees.anchorMin = degrees.anchorMax = degrees.pivot = new Vector2(0.5f, 0f);
        degrees.anchoredPosition = Vector2.zero;
        return view;
    }

    private static Sprite LoadSprite(string resource)
    {
        Texture2D texture = AssetUtilsExtended.LoadTextureFromEmbeddedResource(resource);
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private static Image AddImage(RectTransform parent, string name, Sprite sprite)
    {
        GameObject image = new(name, typeof(RectTransform), typeof(Image));
        image.transform.SetParent(parent, false);
        Image graphic = image.GetComponent<Image>();
        graphic.sprite = sprite;
        graphic.raycastTarget = false;
        graphic.preserveAspect = true;
        return graphic;
    }

    private static Text AddText(RectTransform parent, string name, string value, Color colour, bool outline)
    {
        GameObject text = GUIManager.Instance.CreateText(value, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, MinDegreeFont, colour, outline, Color.black, 40f, 20f, false);
        text.name = name;
        Text label = text.GetComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>
    /// The parts of one compass.
    /// </summary>
    private sealed class CompassView
    {
        public GameObject Root;
        public RectTransform Rect;
        public Image Dial;
        public Image Needle;
        public Text[] Letters;
        public Text Degrees;
        public int Bearing = -1;

        private float size = -1f;
        private bool degrees;

        public void Layout(float width, bool showDegrees)
        {
            if (Mathf.Approximately(width, size) && showDegrees == degrees)
            {
                return;
            }

            size = width;
            degrees = showDegrees;
            int degreeFont = Mathf.Clamp(Mathf.RoundToInt(width * DegreeShare), MinDegreeFont, MaxDegreeFont);
            float textHeight = showDegrees ? degreeFont + 4f : 0f;
            Rect.sizeDelta = new Vector2(width, width + textHeight);
            Dial.rectTransform.sizeDelta = new Vector2(width, width);
            Dial.rectTransform.anchoredPosition = Vector2.zero;

            int letterFont = Mathf.Max(8, Mathf.RoundToInt(width * LetterShare));
            for (int i = 0; i < Letters.Length; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                RectTransform letter = Letters[i].rectTransform;
                letter.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (LetterRadius * width / 2f);
                letter.sizeDelta = new Vector2(letterFont * 1.5f, letterFont * 1.5f);
                Letters[i].fontSize = letterFont;
            }

            Degrees.gameObject.SetActive(showDegrees);
            Degrees.fontSize = degreeFont;
            Degrees.rectTransform.sizeDelta = new Vector2(width * 2f, textHeight);
            Bearing = -1;
        }
    }
}
