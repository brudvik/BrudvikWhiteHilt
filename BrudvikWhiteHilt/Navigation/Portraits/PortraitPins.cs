using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Portraits;

/// <summary>
/// Draws other players on the map as a portrait on a see-through black disc, with the name under it in soft shadow.
/// A player whose portrait is not known gets the first letter of the name on a coloured disc instead. The vanilla pin
/// is kept underneath, only its icon and name are hidden.
/// </summary>
public static class PortraitPins
{
    private const string ViewName = "WhiteHiltPortrait";
    private const float LargeSize = 46f;
    private const float SmallSize = 30f;
    private const int LargeFont = 15;
    private const int SmallFont = 12;
    private const float TestOffset = 20f;

    private static readonly Color backdrop = new(0f, 0f, 0f, 0.55f);
    private static readonly Dictionary<Minimap.PinData, long> owners = new();

    private static Sprite disc;
    private static Minimap.PinData testPin;

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
        int count = Mathf.Min(map.m_playerPins.Count, map.m_tempPlayerInfo.Count);
        for (int i = 0; i < count; i++)
        {
            owners[map.m_playerPins[i]] = map.m_tempPlayerInfo[i].m_characterID.UserID;
        }

        if (testPin != null)
        {
            owners[testPin] = ZDOMan.GetSessionID();
        }
    }

    /// <summary>
    /// Puts a portrait on every visible player pin. Called after the map places its pins; vanilla destroys and makes
    /// pin objects again when the map changes mode or a pin leaves the view, so this runs every time.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Decorate(Minimap map)
    {
        bool large = map.m_mode == Minimap.MapMode.Large;
        bool showName = large || PortraitSettings.ShowNamesOnMinimap.Value;
        foreach (KeyValuePair<Minimap.PinData, long> owner in owners)
        {
            Minimap.PinData pin = owner.Key;
            if (pin.m_uiElement == null)
            {
                continue;
            }

            Transform existing = pin.m_uiElement.Find(ViewName);
            if (!PortraitSettings.Enabled.Value)
            {
                if (existing != null)
                {
                    Object.Destroy(existing.gameObject);
                    pin.m_iconElement.enabled = true;
                }

                continue;
            }

            PortraitView view = existing != null ? existing.GetComponent<PortraitView>() : Create(pin.m_uiElement);
            view.Show(PortraitNetwork.Get(owner.Value), pin.m_name, large ? LargeSize : SmallSize, large ? LargeFont : SmallFont, showName);
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

    private static PortraitView Create(RectTransform pin)
    {
        GameObject root = new(ViewName, typeof(RectTransform));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(pin, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);

        PortraitView view = root.AddComponent<PortraitView>();
        view.Backdrop = AddImage<Image>(rect, "Backdrop");
        view.Backdrop.sprite = Disc();
        view.Picture = AddImage<RawImage>(rect, "Picture");

        view.Letter = AddText(rect, "Letter", TextAnchor.MiddleCenter);
        Stretch(view.Letter.rectTransform);

        view.Name = AddText(rect, "Name", TextAnchor.UpperCenter);
        RectTransform name = view.Name.rectTransform;
        name.anchorMin = name.anchorMax = new Vector2(0.5f, 0f);
        name.pivot = new Vector2(0.5f, 1f);
        name.sizeDelta = new Vector2(200f, 24f);
        name.anchoredPosition = new Vector2(0f, -2f);
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

    /// <summary>
    /// The parts of one portrait pin.
    /// </summary>
    private sealed class PortraitView : MonoBehaviour
    {
        public Image Backdrop;
        public RawImage Picture;
        public Text Letter;
        public Text Name;

        public void Show(Texture2D portrait, string name, float size, int fontSize, bool showName)
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
