using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Portraits;

/// <summary>
/// Draws players on the map as a portrait on a see-through black disc, with the name under it in soft shadow. A player
/// whose portrait is not known gets the first letter of the name on a coloured disc instead. The portraits live in their
/// own layer on top of everything else on the map, following the vanilla player pins, whose icon and name are hidden.
/// You get one too in place of the vanilla marker. A ring with a point circles the portrait and shows the heading:
/// where your camera looks, and where a nearby player faces.
/// </summary>
public static class PortraitPins
{
    private const string LayerName = "WhiteHiltPortraits";
    private const float LargeSize = 46f;
    private const float SmallSize = 30f;
    private const int LargeFont = 15;
    private const int SmallFont = 12;
    private const float TestOffset = 20f;

    // The heading picture covers this many portrait widths; the disc's radius is RingTexture / 2 / HeadingScale.
    private const float HeadingScale = 1.75f;
    private const int RingTexture = 256;
    private const float RingInner = 0.93f;
    private const float RingOuter = 1.1f;
    private const float TipReach = 1.66f;
    private const float TipHalfAngle = 32f;
    private const float RingOutline = 7f;

    private static readonly Color backdrop = new(0f, 0f, 0f, 0.55f);
    private static readonly Color ownRing = new(1f, 0.82f, 0.32f, 1f);
    private static readonly Color otherRing = new(0.95f, 0.95f, 0.92f, 1f);
    private static readonly Dictionary<Minimap.PinData, long> owners = new();
    private static readonly Dictionary<Minimap.PinData, ZDOID> characters = new();
    private static readonly Dictionary<Minimap.PinData, PortraitView> views = new();
    private static readonly List<Minimap.PinData> gone = new();

    private static Sprite disc;
    private static Sprite ring;
    private static Sprite ringWithTip;
    private static Minimap.PinData testPin;
    private static RectTransform largeLayer;
    private static RectTransform smallLayer;
    private static PortraitView ownView;

    /// <summary>
    /// Asks the map to redraw its pins, e.g. when a portrait has arrived.
    /// </summary>
    public static void Refresh()
    {
        if (Minimap.instance != null)
        {
            Minimap.instance.m_pinUpdateRequired = true;
        }
    }

    /// <summary>
    /// Remembers which player each vanilla player pin stands for. Called after the map updates its player pins.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void MapPlayers(Minimap map)
    {
        owners.Clear();
        characters.Clear();
        int count = Mathf.Min(map.m_playerPins.Count, map.m_tempPlayerInfo.Count);
        for (int i = 0; i < count; i++)
        {
            owners[map.m_playerPins[i]] = map.m_tempPlayerInfo[i].m_characterID.UserID;
            characters[map.m_playerPins[i]] = map.m_tempPlayerInfo[i].m_characterID;
        }

        if (testPin != null)
        {
            owners[testPin] = ZDOMan.GetSessionID();
            if (Player.m_localPlayer != null)
            {
                characters[testPin] = Player.m_localPlayer.GetZDOID();
            }
        }
    }

    /// <summary>
    /// Moves the portraits onto the player pins and your own marker. Called after the map's own update, so pins that
    /// vanilla made again this frame are already in place.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        if (map.m_mapImageLarge == null || map.m_mapImageSmall == null)
        {
            return;
        }

        if (largeLayer == null || smallLayer == null)
        {
            largeLayer = CreateLayer(map.m_mapImageLarge.rectTransform);
            smallLayer = CreateLayer(map.m_mapImageSmall.rectTransform);
        }

        bool large = map.m_mode == Minimap.MapMode.Large;
        bool shown = large || map.m_mode == Minimap.MapMode.Small;
        bool enabled = PortraitSettings.Enabled.Value;
        RectTransform layer = large ? largeLayer : smallLayer;
        if (layer.GetSiblingIndex() != layer.parent.childCount - 1)
        {
            layer.SetAsLastSibling();
        }

        float size = large ? LargeSize : SmallSize;
        int font = large ? LargeFont : SmallFont;
        bool showName = large || PortraitSettings.ShowNamesOnMinimap.Value;
        UpdateOwn(map, layer, large, shown && enabled, size, font);
        RemoveGone();
        foreach (KeyValuePair<Minimap.PinData, long> owner in owners)
        {
            Minimap.PinData pin = owner.Key;
            views.TryGetValue(pin, out PortraitView view);
            if (!enabled || !shown || pin.m_uiElement == null)
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                }

                if (!enabled && pin.m_iconElement != null)
                {
                    pin.m_iconElement.enabled = true;
                }

                continue;
            }

            if (view == null)
            {
                view = Create(layer);
                views[pin] = view;
            }

            Place(view, layer, pin.m_uiElement.position);
            view.Show(PortraitNetwork.Get(owner.Value), pin.m_name, size, font, showName, HeadingOf(pin), otherRing);
            pin.m_iconElement.enabled = false;
            if (pin.m_NamePinData != null && pin.m_NamePinData.PinNameGameObject != null)
            {
                pin.m_NamePinData.PinNameGameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Adds or removes a pin with your own portrait 20 m east of you, to check the look without another player.
    /// </summary>
    /// <returns>What happened, for the console.</returns>
    public static string ToggleTestPin()
    {
        Minimap map = Minimap.instance;
        Player player = Player.m_localPlayer;
        if (testPin != null)
        {
            map?.RemovePin(testPin);
            owners.Remove(testPin);
            testPin = null;
            return "Portrait test pin removed.";
        }

        if (map == null || player == null)
        {
            return "Join a world first.";
        }

        testPin = map.AddPin(player.transform.position + Vector3.right * TestOffset, Minimap.PinType.Player, player.GetPlayerName(), false, false);
        owners[testPin] = ZDOMan.GetSessionID();
        Refresh();
        return PortraitNetwork.Get(ZDOMan.GetSessionID()) != null
            ? "Portrait test pin added 20 m east of you."
            : "Portrait test pin added, but you have no portrait yet: it is taken in the main menu when the character is shown.";
    }

    // Your portrait sits under the others, so it never hides another player.
    private static void UpdateOwn(Minimap map, RectTransform layer, bool large, bool show, float size, int font)
    {
        SetMarkerVisible(map.m_largeMarker, !show);
        SetMarkerVisible(map.m_smallMarker, !show);
        RectTransform marker = large ? map.m_largeMarker : map.m_smallMarker;
        Player player = Player.m_localPlayer;
        if (!show || marker == null || !marker.gameObject.activeInHierarchy || player == null)
        {
            if (ownView != null)
            {
                ownView.gameObject.SetActive(false);
            }

            return;
        }

        if (ownView == null)
        {
            ownView = Create(layer);
        }

        Place(ownView, layer, marker.position);
        ownView.transform.SetAsFirstSibling();
        Quaternion? heading = PortraitSettings.HeadingMarker.Value ? marker.rotation : null;
        ownView.Show(PortraitNetwork.Get(ZDOMan.GetSessionID()), player.GetPlayerName(), size, font, false, heading, ownRing);
    }

    // Only players whose character is loaded nearby have a known facing; the others get the ring alone.
    private static Quaternion? HeadingOf(Minimap.PinData pin)
    {
        if (!PortraitSettings.HeadingMarker.Value || ZNetScene.instance == null || !characters.TryGetValue(pin, out ZDOID id) || id.IsNone())
        {
            return null;
        }

        GameObject character = ZNetScene.instance.FindInstance(id);
        return character != null ? Quaternion.Euler(0f, 0f, -character.transform.eulerAngles.y) : null;
    }

    private static void SetMarkerVisible(RectTransform marker, bool visible)
    {
        Image image = marker != null ? marker.GetComponent<Image>() : null;
        if (image != null && image.enabled != visible)
        {
            image.enabled = visible;
        }
    }

    private static void RemoveGone()
    {
        gone.Clear();
        foreach (KeyValuePair<Minimap.PinData, PortraitView> view in views)
        {
            if (view.Value == null || !owners.ContainsKey(view.Key))
            {
                gone.Add(view.Key);
            }
        }

        foreach (Minimap.PinData pin in gone)
        {
            if (views[pin] != null)
            {
                Object.Destroy(views[pin].gameObject);
            }

            views.Remove(pin);
        }
    }

    private static void Place(PortraitView view, RectTransform layer, Vector3 position)
    {
        if (view.transform.parent != layer)
        {
            view.transform.SetParent(layer, false);
        }

        view.gameObject.SetActive(true);
        view.transform.position = position;
    }

    private static RectTransform CreateLayer(RectTransform map)
    {
        Transform old = map.Find(LayerName);
        if (old != null)
        {
            Object.Destroy(old.gameObject);
        }

        GameObject root = new(LayerName, typeof(RectTransform));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(map, false);
        Stretch(rect);
        return rect;
    }

    private static PortraitView Create(RectTransform layer)
    {
        GameObject root = new("Portrait", typeof(RectTransform));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(layer, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

        PortraitView view = root.AddComponent<PortraitView>();
        view.Backdrop = AddImage<Image>(rect, "Backdrop");
        view.Backdrop.sprite = Disc();
        view.Picture = AddImage<RawImage>(rect, "Picture");

        view.Letter = AddText(rect, "Letter", TextAnchor.MiddleCenter);
        Stretch(view.Letter.rectTransform);

        view.Heading = AddImage<Image>(rect, "Heading");
        RectTransform heading = view.Heading.rectTransform;
        heading.anchorMin = heading.anchorMax = new Vector2(0.5f, 0.5f);
        heading.anchoredPosition = Vector2.zero;

        view.Name = AddText(rect, "Name", TextAnchor.UpperCenter);
        RectTransform name = view.Name.rectTransform;
        name.anchorMin = name.anchorMax = new Vector2(0.5f, 0f);
        name.pivot = new Vector2(0.5f, 1f);
        name.sizeDelta = new Vector2(200f, 24f);
        AddShadow(view.Name, new Vector2(1f, -1f), 0.6f);
        AddShadow(view.Name, new Vector2(2f, -2f), 0.25f);
        return view;
    }

    private static T AddImage<T>(RectTransform parent, string name) where T : Graphic
    {
        GameObject image = new(name, typeof(RectTransform), typeof(T));
        image.transform.SetParent(parent, false);
        T graphic = image.GetComponent<T>();
        graphic.raycastTarget = false;
        Stretch(graphic.rectTransform);
        return graphic;
    }

    private static Text AddText(RectTransform parent, string name, TextAnchor alignment)
    {
        GameObject text = GUIManager.Instance.CreateText(string.Empty, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, LargeFont, Color.white, false, Color.black, 200f, 24f, false);
        text.name = name;
        Text label = text.GetComponent<Text>();
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    private static void AddShadow(Text text, Vector2 distance, float alpha)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectDistance = distance;
        shadow.effectColor = new Color(0f, 0f, 0f, alpha);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static Sprite Disc()
    {
        if (disc != null)
        {
            return disc;
        }

        const int size = 128;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false) { name = "WhiteHiltPortraitDisc", wrapMode = TextureWrapMode.Clamp };
        Color32[] pixels = new Color32[size * size];
        float centre = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(size / 2f - distance - 0.5f) * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        disc = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return disc;
    }

    private static Sprite Ring(bool tip)
    {
        Sprite cached = tip ? ringWithTip : ring;
        if (cached != null)
        {
            return cached;
        }

        // White ring (tinted by the Image) with a soft dark outline, drawn from a signed distance; the point faces up.
        const int size = RingTexture;
        float centre = (size - 1) / 2f;
        float radius = size / 2f / HeadingScale;
        float inner = RingInner * radius;
        float outer = RingOuter * radius;
        float half = TipHalfAngle * Mathf.Deg2Rad;
        float baseRadius = (inner + outer) / 2f;
        Vector2 apex = new(0f, TipReach * radius);
        Vector2 left = new(-Mathf.Sin(half) * baseRadius, Mathf.Cos(half) * baseRadius);
        Vector2 right = new(Mathf.Sin(half) * baseRadius, Mathf.Cos(half) * baseRadius);

        Texture2D texture = new(size, size, TextureFormat.RGBA32, true) { name = tip ? "WhiteHiltHeadingTip" : "WhiteHiltHeadingRing", wrapMode = TextureWrapMode.Clamp };
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new(x - centre, y - centre);
                float distance = point.magnitude;
                float shape = Mathf.Abs(distance - (inner + outer) / 2f) - (outer - inner) / 2f;
                if (tip)
                {
                    shape = Mathf.Min(shape, Mathf.Max(Triangle(point, apex, left, right), inner - distance));
                }

                float fill = Mathf.Clamp01(0.5f - shape);
                float edge = Mathf.Clamp01(0.5f - (shape - RingOutline)) * 0.65f;
                float alpha = fill + edge * (1f - fill);
                byte grey = (byte)(alpha > 0f ? fill / alpha * 255f : 0f);
                pixels[y * size + x] = new Color32(grey, grey, grey, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        if (tip)
        {
            ringWithTip = sprite;
        }
        else
        {
            ring = sprite;
        }

        return sprite;
    }

    // Signed distance to a triangle: negative inside.
    private static float Triangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float distance = Mathf.Min(Segment(point, a, b), Mathf.Min(Segment(point, b, c), Segment(point, c, a)));
        bool inside = Side(point, a, b) == Side(point, b, c) && Side(point, b, c) == Side(point, c, a);
        return inside ? -distance : distance;
    }

    private static float Segment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(point, a + ab * t);
    }

    private static bool Side(Vector2 point, Vector2 a, Vector2 b)
    {
        return (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x) >= 0f;
    }

    /// <summary>
    /// The parts of one portrait pin.
    /// </summary>
    private sealed class PortraitView : MonoBehaviour
    {
        public Image Backdrop;
        public RawImage Picture;
        public Text Letter;
        public Text Name;
        public Image Heading;

        public void Show(Texture2D portrait, string name, float size, int fontSize, bool showName, Quaternion? heading, Color ringColour)
        {
            ((RectTransform)transform).sizeDelta = new Vector2(size, size);
            Picture.texture = portrait;
            Picture.gameObject.SetActive(portrait != null);
            Letter.gameObject.SetActive(portrait == null);
            Backdrop.color = portrait != null ? backdrop : Tint(name);
            if (portrait == null)
            {
                Letter.text = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
                Letter.fontSize = Mathf.RoundToInt(size * 0.5f);
            }

            Name.gameObject.SetActive(showName && !string.IsNullOrEmpty(name));
            Name.text = name;
            Name.fontSize = fontSize;
            Name.rectTransform.anchoredPosition = new Vector2(0f, -(RingOuter - 1f) * size / 2f - 2f);

            bool ringShown = PortraitSettings.HeadingMarker.Value;
            Heading.gameObject.SetActive(ringShown);
            if (ringShown)
            {
                Heading.sprite = Ring(heading.HasValue);
                Heading.color = ringColour;
                Heading.rectTransform.sizeDelta = new Vector2(size * HeadingScale, size * HeadingScale);
                Heading.rectTransform.rotation = heading ?? Quaternion.identity;
            }
        }

        // Each name keeps its own muted colour, so players without a portrait can still be told apart.
        private static Color Tint(string name)
        {
            float hue = string.IsNullOrEmpty(name) ? 0f : (name.GetStableHashCode() & 0xffff) / 65535f;
            Color colour = Color.HSVToRGB(hue, 0.45f, 0.5f);
            colour.a = 0.8f;
            return colour;
        }
    }
}
