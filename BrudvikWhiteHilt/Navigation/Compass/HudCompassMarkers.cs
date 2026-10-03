using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Navigation.Compass;

/// <summary>Reusable world-marker icons, sourced only from known map pins or explicit registrations.</summary>
public static class HudCompassMarkers
{
    private static readonly List<Minimap.PinData> pins = new();
    private static readonly Dictionary<string, Target> custom = new();
    private static readonly List<Image> pool = new();
    private static RectTransform container;
    private static Minimap boundMap;
    private static Player boundPlayer;
    private static float nextRefresh;

    /// <summary>Adds or updates an explicitly provided world marker. Does not discover world objects.</summary>
    /// <param name="id">Stable unique marker identifier.</param>
    /// <param name="position">The target world position.</param>
    /// <param name="icon">The icon sprite; null removes the marker.</param>
    public static void SetMarker(string id, Vector3 position, Sprite icon)
    {
        if (string.IsNullOrEmpty(id)) return;
        RefreshContext();
        if (icon == null) custom.Remove(id);
        else custom[id] = new Target { Position = position, Icon = icon };
    }

    /// <summary>Removes an explicitly registered marker.</summary>
    /// <param name="id">Its stable identifier.</param>
    public static void RemoveMarker(string id)
    {
        if (!string.IsNullOrEmpty(id)) custom.Remove(id);
    }

    internal static void Ensure(RectTransform root)
    {
        container = HudCompass.CreateRect(root, "MarkerContainer");
        container.sizeDelta = Vector2.zero;
        pool.Clear();
        pins.Clear();
        nextRefresh = 0f;
    }

    internal static void Update(Vector3 here, float heading, float width, float height)
    {
        RefreshContext();
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + HudCompassSettings.MarkerRefreshSeconds.Value;
            pins.Clear();
            if (boundMap != null)
            {
                foreach (Minimap.PinData pin in boundMap.m_pins)
                {
                    if (Eligible(pin)) pins.Add(pin);
                }
            }
        }

        int used = 0;
        if (HudCompassSettings.MarkerVisibility.Value)
        {
            foreach (Minimap.PinData pin in pins)
            {
                if (Eligible(pin)) Place(pin.m_pos, pin.m_icon, here, heading, width, height, ref used);
            }
            foreach (Target target in custom.Values) Place(target.Position, target.Icon, here, heading, width, height, ref used);
        }
        for (int index = used; index < pool.Count; index++)
        {
            if (pool[index].gameObject.activeSelf) pool[index].gameObject.SetActive(false);
        }
    }

    private static void RefreshContext()
    {
        if (boundPlayer != Player.m_localPlayer || boundMap != Minimap.instance)
        {
            boundPlayer = Player.m_localPlayer;
            boundMap = Minimap.instance;
            pins.Clear();
            custom.Clear();
            nextRefresh = 0f;
        }
    }

    private static bool Eligible(Minimap.PinData pin)
    {
        if (pin.m_shouldDelete || pin.m_checked || pin.m_icon == null) return false;
        bool own = pin.m_ownerID == 0 || pin.m_ownerID == Player.m_localPlayer.GetPlayerID();
        if (pin.m_type == Minimap.PinType.Boss) return HudCompassSettings.ShowBosses.Value;
        if (pin.m_type == Minimap.PinType.Death) return own && HudCompassSettings.ShowDeath.Value;
        bool ordinary = pin.m_type == Minimap.PinType.Icon0 || pin.m_type == Minimap.PinType.Icon1
            || pin.m_type == Minimap.PinType.Icon2 || pin.m_type == Minimap.PinType.Icon3 || pin.m_type == Minimap.PinType.Icon4;
        return ordinary && own && pin.m_save && HudCompassSettings.ShowOwnPins.Value;
    }

    private static void Place(Vector3 target, Sprite sprite, Vector3 here, float heading, float width, float height, ref int used)
    {
        if (sprite == null || (Mathf.Approximately(target.x, here.x) && Mathf.Approximately(target.z, here.z))) return;
        if (!CompassGeometry.Project(heading, CompassGeometry.Bearing(here, target), width, HudCompassSettings.VisibleDegrees.Value, out float position)) return;
        if (used == pool.Count)
        {
            Image created = HudCompass.CreateImage(container, "WorldMarker");
            created.preserveAspect = true;
            created.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pool.Add(created);
        }
        Image icon = pool[used++];
        if (!icon.gameObject.activeSelf) icon.gameObject.SetActive(true);
        icon.sprite = sprite;
        float size = Mathf.Min(HudCompassSettings.MarkerSize.Value, height * 0.28f);
        icon.rectTransform.sizeDelta = new Vector2(size, size);
        icon.rectTransform.anchoredPosition = new Vector2(position, -height + size / 2f + 1f);
        icon.color = new Color(1f, 1f, 1f, HudCompass.Fade(position, width));
    }

    private sealed class Target
    {
        internal Vector3 Position;
        internal Sprite Icon;
    }
}