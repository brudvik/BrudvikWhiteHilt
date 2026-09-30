using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Clock;

/// <summary>
/// The time of day at the top of the screen, in 24 hours, with the day number and a weather icon. Sunrise is 06:00 and
/// nightfall 18:00, as the game's own day and night. Warns before dark. Hidden in the inventory, in build mode, on the
/// large map and in menus.
/// </summary>
public static class GameClock
{
    private const float RefreshSeconds = 0.25f;
    private const float IconGap = 8f;
    private const float Nightfall = 18f;

    // Boss health bars sit at the top centre too; the clock moves below them.
    private const float BossBarOffset = 80f;

    private static readonly Color dayColor = new(1f, 0.95f, 0.85f);
    private static readonly Color warningColor = new(1f, 0.6f, 0.2f);
    private static readonly Dictionary<string, Sprite> icons = new();

    private static RectTransform root;
    private static Text label;
    private static Image icon;
    private static float nextRefresh;
    private static float lastHours = -1f;

    /// <summary>
    /// Shows, hides and updates the clock. Called after the game's HUD update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void Update(Hud hud)
    {
        if (!BackpackInput.Typing() && BackpackInput.Pressed(ClockSettings.KeyToggle))
        {
            ClockSettings.Enabled.Value = !ClockSettings.Enabled.Value;
        }

        Player player = Player.m_localPlayer;
        if (player == null || EnvMan.instance == null || ZNet.instance == null)
        {
            SetVisible(false);
            return;
        }

        float hours = Hours();
        WarnBeforeDusk(player, hours);

        if (!ShouldShow(player))
        {
            SetVisible(false);
            return;
        }

        Ensure(hud);
        SetVisible(true);
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshSeconds;
        Refresh(hours);
    }

    private static bool ShouldShow(Player player)
    {
        return ClockSettings.AllowClock.Value && ClockSettings.Enabled.Value && !player.IsDead() && !player.InPlaceMode()
            && !InventoryGui.IsVisible() && !Menu.IsVisible() && !TextInput.IsVisible() && !StoreGui.IsVisible()
            && (Minimap.instance == null || Minimap.instance.m_mode != Minimap.MapMode.Large);
    }

    /// <summary>
    /// The time of day in hours, 0 to 24, as the clock shows it.
    /// </summary>
    /// <returns>The hour; sunrise is 6 and nightfall 18.</returns>
    // The game's day fraction runs 0.15 - 0.85 of the day for daylight; rescaled, daylight is 06:00 - 18:00.
    internal static float Hours()
    {
        EnvMan env = EnvMan.instance;
        if (env.m_debugTimeOfDay)
        {
            return env.m_debugTime * 24f;
        }

        double seconds = ZNet.instance.GetTimeSeconds();
        float raw = (float)(seconds % env.m_dayLengthSec / env.m_dayLengthSec);
        return env.RescaleDayFraction(raw) * 24f;
    }

    private static void WarnBeforeDusk(Player player, float hours)
    {
        float before = ClockSettings.DuskWarningHours.Value;
        float warnAt = Nightfall - before;
        bool crossed = lastHours >= 0f && lastHours < warnAt && hours >= warnAt && hours - lastHours < 1f;
        lastHours = hours;
        if (before <= 0f || !crossed || EnvMan.instance.IsTimeSkipping() || !ClockSettings.AllowClock.Value)
        {
            return;
        }

        string message = Mathf.Approximately(before, 1f)
            ? "$msg_whitehilt_clock_dusk_one"
            : string.Format(Localization.instance.Localize("$msg_whitehilt_clock_dusk"), before.ToString("0.#"));
        player.Message(MessageHud.MessageType.Center, message);
    }

    private static void Refresh(float hours)
    {
        int round = Mathf.Clamp(ClockSettings.RoundMinutes.Value, 1, 60);
        int minutes = Mathf.FloorToInt(hours * 60f) / round * round % (24 * 60);
        string time = $"{minutes / 60:00}:{minutes % 60:00}";
        string text = ClockSettings.ShowDay.Value
            ? $"{string.Format(Localization.instance.Localize("$whitehilt_clock_day"), EnvMan.instance.GetDay())}  ·  {time}"
            : time;

        float warning = ClockSettings.DuskWarningHours.Value;
        label.color = warning > 0f && hours >= Nightfall - warning && hours < Nightfall ? warningColor : dayColor;
        label.fontSize = Mathf.Clamp(ClockSettings.FontSize.Value, 10, 60);
        if (label.text != text)
        {
            label.text = text;
        }

        Sprite weather = ClockSettings.ShowWeather.Value ? WeatherIcon(hours) : null;
        icon.gameObject.SetActive(weather != null);
        float size = label.fontSize * 1.4f;
        float width = label.preferredWidth;
        if (weather != null)
        {
            icon.sprite = weather;
            icon.rectTransform.sizeDelta = new Vector2(size, size);
            icon.rectTransform.anchoredPosition = new Vector2(-(width + size) / 2f - IconGap / 2f, 0f);
            label.rectTransform.anchoredPosition = new Vector2((size + IconGap) / 2f, 0f);
        }
        else
        {
            label.rectTransform.anchoredPosition = Vector2.zero;
        }

        label.rectTransform.sizeDelta = new Vector2(width + 4f, size);
        float boss = EnemyHud.instance != null && EnemyHud.instance.ShowingBossHud() ? BossBarOffset : 0f;
        root.anchoredPosition = new Vector2(0f, -ClockSettings.OffsetY.Value - boss);
    }

    private static Sprite WeatherIcon(float hours)
    {
        EnvSetup env = EnvMan.instance.GetCurrentEnvironment();
        string name = env?.m_name?.ToLowerInvariant() ?? string.Empty;
        bool night = hours < 6f || hours >= Nightfall;
        string key;
        if (name.Contains("snow") || (env != null && env.m_isFreezing))
        {
            key = "Snow";
        }
        else if (name.Contains("thunder") || name.Contains("storm"))
        {
            key = "Storm";
        }
        else if (name.Contains("ashrain"))
        {
            key = "Ash";
        }
        else if (name.Contains("rain") || (env != null && env.m_isWet))
        {
            key = "Rain";
        }
        else if (name.Contains("mist") || name.Contains("fog"))
        {
            key = "Fog";
        }
        else if (night)
        {
            key = "Moon";
        }
        else
        {
            key = name.Contains("clear") ? "Sun" : "Cloud";
        }

        return LoadIcon(key);
    }

    private static Sprite LoadIcon(string key)
    {
        if (!icons.TryGetValue(key, out Sprite sprite))
        {
            try
            {
                sprite = AssetUtilsExtended.LoadTextureFromEmbeddedResource($"BrudvikWhiteHilt.Assets.Weather{key}.png").ConvertToSprite();
            }
            catch (System.Exception ex)
            {
                Jotunn.Logger.LogWarning($"Clock: weather icon {key} failed to load: {ex.Message}");
            }

            icons[key] = sprite;
        }

        return sprite;
    }

    private static void SetVisible(bool visible)
    {
        if (root != null && root.gameObject.activeSelf != visible)
        {
            root.gameObject.SetActive(visible);
            nextRefresh = 0f;
        }
    }

    // Under the HUD root, so the clock goes away with the HUD (Ctrl + F3, cutscenes, media mode).
    private static void Ensure(Hud hud)
    {
        if (root != null && root.parent == hud.m_rootObject.transform)
        {
            return;
        }

        GameObject clock = new("WhiteHiltClock", typeof(RectTransform));
        root = (RectTransform)clock.transform;
        root.SetParent(hud.m_rootObject.transform, false);
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(10f, 40f);

        GameObject text = GUIManager.Instance.CreateText(string.Empty, root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, ClockSettings.FontSize.Value, dayColor, true, Color.black, 300f, 40f, false);
        text.name = "Time";
        label = text.GetComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.raycastTarget = false;

        GameObject image = new("Weather", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(root, false);
        icon = image.GetComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        nextRefresh = 0f;
    }
}
