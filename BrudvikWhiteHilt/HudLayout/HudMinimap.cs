using BrudvikWhiteHilt.Navigation.Waypoints;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// The minimap in the White Hilt HUD: round, or the game's own square one.
/// </summary>
public enum MinimapStyle
{
    /// <summary>A round map in a dark compass ring with what lies beyond it on the ring.</summary>
    Round,

    /// <summary>The game's own square minimap.</summary>
    Vanilla,
}

/// <summary>
/// Makes the game's minimap round in the White Hilt HUD: the map and its pins go under a round mask, the square frame
/// round them is hidden, and a dark ring goes round the map with N, E, S and W on it. The map is north up, so the ring
/// stands still. Pins, bosses, your death and the waypoint that lie beyond what the map shows sit on the ring in the
/// direction they lie, so the ring tells what is where further off. The biome's name goes under the ring. Everything
/// goes back as the game had it with the vanilla minimap or HUD.
/// </summary>
public static class HudMinimap
{
    private const float RingShare = 0.84f;
    private const float RefreshSeconds = 1f;
    private const float BeyondShare = 0.92f;
    private const float NameGap = 4f;
    private const int MaxMarks = 24;

    private static readonly Color ringColor = new(0.03f, 0.04f, 0.04f, 1f);
    private static readonly Color letterColor = new(0.94f, 0.89f, 0.77f);
    private static readonly Color northColor = new(1f, 0.54f, 0.48f);
    private static readonly Color waypointColor = new(0.91f, 0.64f, 0.24f);
    private static readonly string[] letterKeys = { "$whitehilt_compass_n", "$whitehilt_compass_e", "$whitehilt_compass_s", "$whitehilt_compass_w" };
    private static readonly List<Graphic> hidden = new();
    private static readonly List<Graphic> found = new();
    private static readonly List<Image> marks = new();
    private static readonly List<(Transform Child, Transform Parent, int Index)> moved = new();
    private static readonly Text[] letters = new Text[4];

    private static Minimap bound;
    private static RectTransform mask;
    private static RectTransform ring;
    private static Image ringImage;
    private static float nextRefresh;

    /// <summary>Whether the minimap is round now.</summary>
    public static bool Round => mask != null;

    /// <summary>
    /// Makes the minimap round or gives it back, and places the ring's markers. Called after the map's own update.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Update(Minimap map)
    {
        bool round = HudSettings.WhiteHilt && HudSettings.Minimap.Value == MinimapStyle.Round && map.m_mapImageSmall != null
            && map.m_smallRoot != null && Player.m_localPlayer != null;
        if (bound != map)
        {
            Revert();
            bound = map;
        }

        if (!round)
        {
            Revert();
            return;
        }

        if (mask == null)
        {
            Apply(map);
        }

        Place(map);
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            HideFrame(map);
        }

        Mark(map, Player.m_localPlayer);
    }

    // Puts the map and its pins under a round mask in the map's place, and a ring after it.
    private static void Apply(Minimap map)
    {
        RectTransform image = map.m_mapImageSmall.rectTransform;
        Transform parent = image.parent;
        int index = image.GetSiblingIndex();

        mask = (RectTransform)new GameObject("WhiteHiltRoundMap", typeof(RectTransform), typeof(Image), typeof(Mask)).transform;
        mask.SetParent(parent, false);
        mask.SetSiblingIndex(index);
        Image maskImage = mask.GetComponent<Image>();
        maskImage.sprite = HudSprites.Disc;
        maskImage.raycastTarget = false;
        mask.GetComponent<Mask>().showMaskGraphic = false;
        Center(mask, image, 1f);

        Move(image, mask);
        Move(map.m_pinRootSmall, mask);
        Move(map.m_pinNameRootSmall, mask);

        ring = (RectTransform)new GameObject("WhiteHiltMapRing", typeof(RectTransform), typeof(Image)).transform;
        ring.SetParent(parent, false);
        ring.SetSiblingIndex(mask.GetSiblingIndex() + 1);
        ringImage = ring.GetComponent<Image>();
        ringImage.sprite = HudSprites.WideRing;
        ringImage.raycastTarget = false;
        for (int i = 0; i < letters.Length; i++)
        {
            GameObject text = GUIManager.Instance.CreateText(Localization.instance.Localize(letterKeys[i]), ring, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, GUIManager.Instance.AveriaSerifBold, 14, i == 0 ? northColor : letterColor, true, Color.black,
                30f, 24f, false);
            letters[i] = text.GetComponent<Text>();
            letters[i].alignment = TextAnchor.MiddleCenter;
            letters[i].raycastTarget = false;
        }

        nextRefresh = 0f;
    }

    // Moves a child under the mask, remembering where it was; it keeps its place on the screen.
    private static void Move(Transform child, Transform target)
    {
        if (child == null)
        {
            return;
        }

        moved.Add((child, child.parent, child.GetSiblingIndex()));
        child.SetParent(target, true);
    }

    // Sizes the ring round the map, its letters in the middle of its band, and puts the biome's name under it. The
    // mask stays where it was put: the map is inside it and moves with it.
    private static void Place(Minimap map)
    {
        RectTransform image = map.m_mapImageSmall.rectTransform;
        Center(ring, image, 1f / RingShare);
        ringImage.color = new Color(ringColor.r, ringColor.g, ringColor.b, Mathf.Clamp01(HudSettings.MinimapRingOpacity.Value));
        float outer = ring.sizeDelta.x / 2f;
        float middle = outer * (1f + RingShare) / 2f;
        int font = Mathf.Clamp(Mathf.RoundToInt(outer * (1f - RingShare) * 0.95f), 9, 22);
        for (int i = 0; i < letters.Length; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            letters[i].fontSize = font;
            letters[i].rectTransform.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * middle;
        }

        RectTransform name = map.m_biomeNameSmall != null ? map.m_biomeNameSmall.rectTransform : null;
        RectTransform space = ring.parent as RectTransform;
        if (name != null && space != null && VanillaPlacement.Bounds(space, new[] { name }, out Rect box)
            && VanillaPlacement.Bounds(space, new[] { ring }, out Rect around))
        {
            VanillaPlacement.Move(name, space, new Vector2(around.center.x - box.center.x, around.yMin - NameGap - box.yMax));
        }
    }

    // Puts a rect on the centre of the map's image, as large as it times a factor, in its parent's space.
    private static void Center(RectTransform rect, RectTransform image, float factor)
    {
        RectTransform parent = (RectTransform)rect.parent;
        Vector3[] corners = new Vector3[4];
        image.GetWorldCorners(corners);
        Vector3 a = parent.InverseTransformPoint(corners[0]);
        Vector3 c = parent.InverseTransformPoint(corners[2]);
        float size = Mathf.Min(Mathf.Abs(c.x - a.x), Mathf.Abs(c.y - a.y)) * factor;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.localPosition = (a + c) / 2f;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    // Hides the square frame round the map: whatever is drawn in the small map but the map, its pins, the player and
    // ship markers and the biome's name.
    private static void HideFrame(Minimap map)
    {
        map.m_smallRoot.GetComponentsInChildren(true, found);
        foreach (Graphic graphic in found)
        {
            Transform t = graphic.transform;
            if (!graphic.enabled || t.IsChildOf(mask) || t.IsChildOf(ring) || Keeps(t, map.m_smallMarker) || Keeps(t, map.m_smallShipMarker)
                || (map.m_biomeNameSmall != null && t == map.m_biomeNameSmall.transform))
            {
                continue;
            }

            graphic.enabled = false;
            hidden.Add(graphic);
        }
    }

    private static bool Keeps(Transform t, Transform keep)
    {
        return keep != null && t.IsChildOf(keep);
    }

    // The pins and the waypoint beyond what the map shows, on the ring in the direction they lie.
    private static void Mark(Minimap map, Player player)
    {
        int used = 0;
        if (HudSettings.MinimapRingMarkers.Value)
        {
            Vector3 here = player.transform.position;
            float reach = map.m_mapImageSmall.uvRect.width * map.m_textureSize * map.m_pixelSize / 2f * BeyondShare;
            if (WaypointTarget.Has)
            {
                Mark(here, WaypointTarget.Position, null, reach, ref used);
            }

            foreach (Minimap.PinData pin in map.m_pins)
            {
                if (used >= MaxMarks)
                {
                    break;
                }

                if (Shown(pin, player))
                {
                    Mark(here, pin.m_pos, pin.m_icon, reach, ref used);
                }
            }
        }

        for (int i = used; i < marks.Count; i++)
        {
            if (marks[i].gameObject.activeSelf)
            {
                marks[i].gameObject.SetActive(false);
            }
        }
    }

    private static bool Shown(Minimap.PinData pin, Player player)
    {
        if (pin.m_shouldDelete || pin.m_checked || pin.m_icon == null)
        {
            return false;
        }

        bool own = pin.m_ownerID == 0 || pin.m_ownerID == player.GetPlayerID();
        return pin.m_type switch
        {
            Minimap.PinType.Boss => true,
            Minimap.PinType.Death => own,
            Minimap.PinType.Icon0 or Minimap.PinType.Icon1 or Minimap.PinType.Icon2 or Minimap.PinType.Icon3 or Minimap.PinType.Icon4 => own && pin.m_save,
            _ => false
        };
    }

    // One marker on the ring, when the target lies beyond the map; the waypoint, which has no icon, is a gold disc.
    private static void Mark(Vector3 here, Vector3 target, Sprite icon, float reach, ref int used)
    {
        Vector3 away = target - here;
        away.y = 0f;
        if (away.magnitude <= reach)
        {
            return;
        }

        if (used == marks.Count)
        {
            Image created = new GameObject("WhiteHiltRingMark", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            created.rectTransform.SetParent(ring, false);
            created.rectTransform.anchorMin = created.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            created.preserveAspect = true;
            created.raycastTarget = false;
            marks.Add(created);
        }

        Image mark = marks[used++];
        if (!mark.gameObject.activeSelf)
        {
            mark.gameObject.SetActive(true);
        }

        float outer = ring.sizeDelta.x / 2f;
        float size = outer * (1f - RingShare) * (icon != null ? 1.1f : 0.7f);
        float middle = outer * (1f + RingShare) / 2f;
        Vector2 direction = new Vector2(away.x, away.z).normalized;
        mark.sprite = icon != null ? icon : HudSprites.Disc;
        mark.color = icon != null ? Color.white : waypointColor;
        mark.rectTransform.sizeDelta = new Vector2(size, size);
        mark.rectTransform.anchoredPosition = direction * middle;
    }

    // Gives the map back as the game had it.
    private static void Revert()
    {
        for (int i = moved.Count - 1; i >= 0; i--)
        {
            (Transform child, Transform parent, int index) = moved[i];
            if (child != null && parent != null)
            {
                child.SetParent(parent, true);
                child.SetSiblingIndex(Mathf.Min(index, parent.childCount - 1));
            }
        }

        moved.Clear();
        foreach (Graphic graphic in hidden)
        {
            if (graphic != null)
            {
                graphic.enabled = true;
            }
        }

        hidden.Clear();
        if (bound != null && bound.m_biomeNameSmall != null)
        {
            VanillaPlacement.Restore(bound.m_biomeNameSmall.rectTransform);
        }

        if (mask != null)
        {
            Object.Destroy(mask.gameObject);
        }

        if (ring != null)
        {
            Object.Destroy(ring.gameObject);
        }

        mask = null;
        ring = null;
        marks.Clear();
    }
}
