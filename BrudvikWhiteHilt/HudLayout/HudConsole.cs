using BepInEx.Configuration;
using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// The White Hilt HUD: one console at the bottom centre. The game's hotbar sits in the middle with health, stamina, eitr
/// and adrenaline as bars with numbers above it, the food and the guardian power to its left and the status effects to
/// its right, other mods' effects and indicators among them. Over it all a strip shows the context lines, such as the
/// ship's read-out while steering, the piece being built and its cost, and warnings. The game's own bars, food, status
/// list, event bar and build panel are hidden while it shows. The console always stays in its place at the bottom,
/// keeps other mods' HUD elements clear of itself, and with <see cref="HudSettings.Calm"/> each part fades while
/// nothing about it changes.
/// </summary>
public static class HudConsole
{
    private const float SupplyRoom = 26f;
    private const float WingGap = 14f;
    private const float BarHeight = 9f;
    private const float BarGap = 6f;
    private const float NumberHeight = 18f;
    private const float FoodSize = 42f;
    private const float FoodGap = 6f;
    private const float IconInset = 0.2f;

    // The game lets you eat three foods; a mod that allows more gets more places.
    private const int FoodPlaces = 3;
    private const float CrossLength = 0.42f;
    private const float CrossWidth = 3f;
    private const float StatusSize = 34f;
    private const float StatusGap = 6f;
    private const float TimeHeight = 14f;
    private const float StripGap = 8f;
    private const float StripLine = 20f;
    private const float StripPadding = 14f;
    private const int StripLines = 3;
    private const float FallbackSlots = 8f;
    private const float FallbackSlotHeight = 64f;
    private const float FadeSpeed = 3f;
    private const float StatusLinger = 3f;
    private const int HotbarKeys = 8;
    private const float ForeignGap = 8f;
    private const string BuildId = "whitehilt.build";
    private const int BuildPriority = 120;
    private const string BuildSeparator = "   ·   ";
    private const float HintGap = 12f;
    private const float HintMargin = 16f;
    private const float MinHintScale = 0.6f;
    private const float NameWidth = 190f;
    private const float CycleSeconds = 1.5f;
    private const int MaxListed = 16;
    private const float PulseSpeed = 6f;
    private const float RingHalf = 0.5f;
    private const float RingLow = 0.2f;
    private const float HiddenLinger = 0.1f;
    private const float PanelPadding = 10f;

    private static readonly Color panelColor = new(0.04f, 0.05f, 0.05f, 0.72f);
    private static readonly Color textColor = new(1f, 0.95f, 0.85f);
    private static readonly Color dimColor = new(0.73f, 0.7f, 0.64f);
    private static readonly Color lowColor = new(1f, 0.4f, 0.35f);
    private static readonly Color healthColor = new(0.78f, 0.26f, 0.23f);
    private static readonly Color healthBack = new(0.23f, 0.14f, 0.13f, 0.85f);
    private static readonly Color staminaColor = new(0.88f, 0.71f, 0.23f);
    private static readonly Color staminaBack = new(0.23f, 0.2f, 0.13f, 0.85f);
    private static readonly Color eitrColor = new(0.6f, 0.45f, 1f);
    private static readonly Color eitrBack = new(0.16f, 0.13f, 0.27f, 0.85f);
    private static readonly Color adrenalineColor = new(0.95f, 0.55f, 0.2f);
    private static readonly Color adrenalineBack = new(0.27f, 0.17f, 0.1f, 0.85f);
    private static readonly Color foodFull = new(0.66f, 0.83f, 0.6f);
    private static readonly Color foodHalf = new(0.88f, 0.71f, 0.23f);
    private static readonly Color foodLow = new(0.94f, 0.54f, 0.49f);
    private static readonly Color ringColor = new(0.93f, 0.87f, 0.69f, 0.85f);

    private static readonly List<RectTransform> keep = new();
    private static readonly List<RectTransform> hotbarSlots = new();
    private static readonly List<RectTransform> single = new();
    private static readonly List<StatusEffect> effects = new();
    private static readonly List<HudIndicator> indicators = new();
    private static readonly List<TrayItem> tray = new();
    private static readonly List<string> lines = new();
    private static readonly List<FoodSlot> foodSlots = new();
    private static readonly List<StatusSlot> statusSlots = new();
    private static readonly HashSet<Transform> native = new();
    private static readonly HashSet<RectTransform> foreign = new();
    private static readonly List<GameObject> hintGroups = new();
    private static readonly List<Graphic> graphics = new();
    private static readonly List<RectTransform> drawn = new();
    private static readonly Dictionary<GameObject, CanvasGroup> hintAlphas = new();

    private static RectTransform root;
    private static RectTransform foodLayer;
    private static RectTransform statusLayer;
    private static HotkeyBar hotbar;
    private static Bar health;
    private static Bar stamina;
    private static Bar eitr;
    private static Bar adrenaline;
    private static Strip strip;
    private static Image panel;
    private static Fader foodFader;
    private static Fader statusFader;
    private static Fader handsFader;
    private static CanvasGroup hotbarGroup;
    private static CanvasGroup powerGroup;
    private static float lastMaxAdrenaline;
    private static float lastHealth;
    private static int lastFoods;
    private static int lastTray;
    private static ItemDrop.ItemData lastRight;
    private static ItemDrop.ItemData lastLeft;
    private static Fader hintFader;
    private static int lastHintKey;
    private static bool showAllHeld;

    /// <summary>The build tools' key hint, placed left of the console in the White Hilt HUD.</summary>
    internal static Text BuildHint { get; set; }

    /// <summary>
    /// Lays out the console, or gives the game its own HUD back. Called after the game's HUD late update, so it wins over
    /// the positions and animations the game set this frame.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void Update(Hud hud)
    {
        if (!BackpackInput.Typing() && BackpackInput.Pressed(HudSettings.KeyToggleStyle))
        {
            HudSettings.Style.Value = HudSettings.WhiteHilt ? HudStyle.Vanilla : HudStyle.WhiteHilt;
        }

        Player player = Player.m_localPlayer;
        if (!HudSettings.WhiteHilt || hud.m_rootObject == null)
        {
            if (VanillaPlacement.Any)
            {
                VanillaPlacement.RestoreAll();
            }

            foreign.Clear();
            ShowVanillaHands();
            SetVisible(false);
            return;
        }

        VanillaPlacement.Prune();
        Ensure(hud);
        HideVanilla(hud);
        if (player == null || player.IsDead())
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        KeyboardShortcut showAll = HudSettings.KeyShowAll.Value;
        showAllHeld = showAll.MainKey != KeyCode.None && showAll.IsPressed() && !BackpackInput.Typing();
        float scale = Mathf.Clamp(HudSettings.Scale.Value, 0.6f, 1.6f);
        Rect bar = PlaceHotbar(hud);
        float left = LayoutFood(player, bar, scale);
        left = PlaceGuardianPower(hud, left, bar);
        float top = LayoutVitals(player, bar, scale);
        Vector2 corner = LayoutStatus(player, bar, scale);
        float panelTop = PlacePanel(Rect.MinMaxRect(left, bar.yMin, corner.x, Mathf.Max(top, corner.y)), scale);
        top = Mathf.Max(LayoutStrip(bar, Mathf.Max(top, panelTop), scale), corner.y);
        Rect console = Rect.MinMaxRect(left, bar.yMin, corner.x, top);
        AvoidForeign(hud, console);
        PlaceHints(player, console);
        Fade(hud, player);
    }

    /// <summary>
    /// Puts the build piece and its cost in the context strip. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        WhiteHiltHud.RegisterContext(BuildId, BuildPriority, BuildLine);
    }

    /// <summary>
    /// Notes which elements the game's HUD is made with, so elements other mods add later can be told from them.
    /// Called before the HUD wakes.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void RememberNative(Hud hud)
    {
        native.Clear();
        if (hud.m_rootObject == null)
        {
            return;
        }

        foreach (Transform child in hud.m_rootObject.transform)
        {
            native.Add(child);
        }
    }

    // The game's health with its food, the stamina, eitr and adrenaline bars and the status effect list. The guardian
    // power and the hotbar stay, even when they sit inside one of these.
    private static void HideVanilla(Hud hud)
    {
        keep.Clear();
        keep.Add(hud.m_gpRoot);
        keep.Add(Hotbar(hud)?.transform as RectTransform);
        VanillaPlacement.Hide(hud.m_healthPanel, keep);
        VanillaPlacement.Hide(hud.m_healthBarRoot, keep);
        VanillaPlacement.Hide(hud.m_foodBarRoot, keep);
        VanillaPlacement.Hide(hud.m_foodBaseBar, keep);
        VanillaPlacement.Hide(hud.m_foodIcon != null ? hud.m_foodIcon.rectTransform : null, keep);
        VanillaPlacement.Hide(hud.m_foodText != null ? hud.m_foodText.rectTransform : null, keep);
        foreach (Image image in hud.m_foodBars ?? new Image[0])
        {
            VanillaPlacement.Hide(image != null ? image.rectTransform : null, keep);
        }

        foreach (Image image in hud.m_foodIcons ?? new Image[0])
        {
            VanillaPlacement.Hide(image != null ? image.rectTransform : null, keep);
        }

        VanillaPlacement.Hide(hud.m_staminaBar2Root, keep);
        VanillaPlacement.Hide(hud.m_eitrBarRoot, keep);
        VanillaPlacement.Hide(hud.m_adrenalineBarRoot, keep);
        VanillaPlacement.Hide(hud.m_statusEffectListRoot, keep);

        // The event bar, the build piece and its cost are lines in the context strip.
        VanillaPlacement.Hide(hud.m_eventBar != null ? hud.m_eventBar.transform as RectTransform : null, keep);
        VanillaPlacement.Hide(hud.m_buildSelection != null ? hud.m_buildSelection.rectTransform : null, keep);
        VanillaPlacement.Hide(hud.m_pieceDescription != null ? hud.m_pieceDescription.rectTransform : null, keep);
        VanillaPlacement.Hide(hud.m_buildIcon != null ? hud.m_buildIcon.rectTransform : null, keep);
        VanillaPlacement.Hide(hud.m_snappingIcon != null ? hud.m_snappingIcon.rectTransform : null, keep);
        foreach (GameObject item in hud.m_requirementItems ?? new GameObject[0])
        {
            VanillaPlacement.Hide(item != null ? item.transform as RectTransform : null, keep);
        }
    }

    // The piece the build tool will place, bold, then each thing it costs with the amount in the game's own colour for
    // it: red while there is too little, White Hilt's colour when nearby chests make up the rest. After it, dimmed,
    // how many you have or that a chest keeps it unlimited, as the build menu's icons show. Read from the game's
    // build panel, so other mods' changes to it come along.
    private static string BuildLine()
    {
        Hud hud = Hud.instance;
        Player player = Player.m_localPlayer;
        if (!HudSettings.WhiteHilt || hud == null || player == null || !player.InPlaceMode() || hud.m_buildHud == null
            || !hud.m_buildHud.activeInHierarchy || hud.m_buildSelection == null || string.IsNullOrEmpty(hud.m_buildSelection.text))
        {
            return null;
        }

        StringBuilder line = new();
        line.Append("<b>").Append(hud.m_buildSelection.text).Append("</b>");
        foreach (GameObject item in hud.m_requirementItems ?? new GameObject[0])
        {
            if (item == null || !item.activeSelf)
            {
                continue;
            }

            TMP_Text name = item.transform.Find("res_name")?.GetComponent<TMP_Text>();
            TMP_Text amount = item.transform.Find("res_amount")?.GetComponent<TMP_Text>();
            if (name == null || !name.gameObject.activeSelf || string.IsNullOrEmpty(name.text))
            {
                continue;
            }

            line.Append(BuildSeparator).Append(name.text);
            if (amount != null && amount.gameObject.activeSelf && !string.IsNullOrEmpty(amount.text))
            {
                line.Append(" <color=#").Append(ColorUtility.ToHtmlStringRGB(amount.color)).Append('>').Append(amount.text).Append("</color>");
            }

            if (RequirementOverlay.TryGetShown(item.transform, out int have, out bool unlimited))
            {
                string text = unlimited ? Translations.Word("whitehilt_req_unlimited_short") : string.Format(Translations.Word("whitehilt_req_have_short"), have);
                line.Append(" <color=#").Append(ColorUtility.ToHtmlStringRGB(dimColor)).Append(">(").Append(text).Append(")</color>");
            }
        }

        return line.ToString();
    }

    // Keeps elements other mods put on the HUD clear of the console: one that overlaps it is lifted into a band over
    // it, stacked with the others there, and kept there.
    private static void AvoidForeign(Hud hud, Rect console)
    {
        foreign.RemoveWhere(rect => rect == null);
        float zone = console.yMax + ForeignGap;
        Vector2 screen = root.rect.size;
        foreach (Transform child in hud.m_rootObject.transform)
        {
            if (child is not RectTransform rect || native.Contains(child) || child == root || !child.gameObject.activeInHierarchy
                || child.name.StartsWith("WhiteHilt") || child.name.StartsWith("Brudvik"))
            {
                continue;
            }

            single.Clear();
            single.Add(rect);
            if (!VanillaPlacement.Bounds(root, single, out Rect box) || box.width < 1f || box.height < 1f
                || box.width > screen.x / 2f || box.height > screen.y / 2f)
            {
                continue;
            }

            if (!foreign.Contains(rect) && !box.Overlaps(console))
            {
                continue;
            }

            foreign.Add(rect);
            VanillaPlacement.Move(rect, root, new Vector2(0f, zone - box.yMin));
            zone += box.height + ForeignGap;
        }
    }

    private static HotkeyBar Hotbar(Hud hud)
    {
        if (hotbar == null)
        {
            hotbar = hud.GetComponentInChildren<HotkeyBar>(true);
        }

        return hotbar;
    }

    // Centres the hotbar at the bottom, always in the same place. Returns the box of its slots; with no slots yet, the
    // box eight slots would take.
    private static Rect PlaceHotbar(Hud hud)
    {
        float width = root.rect.width;
        float bottom = HudSettings.BottomMargin.Value;
        HotkeyBar bar = Hotbar(hud);
        hotbarSlots.Clear();
        if (bar != null && bar.m_elements != null)
        {
            foreach (HotkeyBar.ElementData element in bar.m_elements)
            {
                if (element?.m_go != null && element.m_go.transform is RectTransform rect)
                {
                    hotbarSlots.Add(rect);
                }
            }
        }

        bool measured = VanillaPlacement.Bounds(root, hotbarSlots, out Rect slots);
        if (!measured)
        {
            float space = bar != null ? bar.m_elementSpace : 70f;
            slots = new Rect(0f, 0f, space * FallbackSlots, FallbackSlotHeight);
        }

        Vector2 delta = new(width / 2f - slots.center.x, bottom - slots.yMin);
        if (measured && bar != null)
        {
            VanillaPlacement.Move((RectTransform)bar.transform, root, delta);
        }

        slots.position += delta;
        return slots;
    }

    // The food the player has eaten, left of the hotbar: icon, a frame coloured by how much is left and the time under
    // it, flashing when it can be eaten again. Returns the left edge of the row.
    // Every place for food stays, so the console keeps its shape: an empty one is a red ring with a red cross, and
    // with nothing eaten at all the red rings pulse.
    private static float LayoutFood(Player player, Rect bar, float scale)
    {
        List<Player.Food> foods = player.GetFoods();
        int places = Mathf.Max(FoodPlaces, foods.Count);
        while (foodSlots.Count < places)
        {
            foodSlots.Add(new FoodSlot(foodLayer));
        }

        float size = FoodSize * scale;
        float gap = FoodGap * scale;
        float right = bar.xMin - WingGap * scale;
        float left = right - places * size - (places - 1) * gap;
        float y = bar.yMin + TimeHeight * scale;
        bool hungry = foods.Count != lastFoods || foods.Count < places;
        lastFoods = foods.Count;
        for (int i = 0; i < foodSlots.Count; i++)
        {
            bool used = i < places;
            foodSlots[i].SetActive(used);
            if (!used)
            {
                continue;
            }

            Vector2 position = new(left + i * (size + gap), y);
            if (i < foods.Count)
            {
                hungry |= foodSlots[i].Show(foods[i], position, size, scale);
            }
            else
            {
                foodSlots[i].ShowEmpty(position, size, scale, foods.Count == 0);
            }
        }

        foodFader.Wake(hungry);
        return left;
    }

    // The game's guardian power, moved left of the food with its bottom on the hotbar's. Returns the console's left
    // edge with it.
    private static float PlaceGuardianPower(Hud hud, float left, Rect bar)
    {
        RectTransform power = hud.m_gpRoot;
        single.Clear();
        single.Add(power);
        if (power == null || !VanillaPlacement.Bounds(root, single, out Rect box))
        {
            return left;
        }

        float gap = WingGap * Mathf.Clamp(HudSettings.Scale.Value, 0.6f, 1.6f);
        float shift = left - gap - box.xMax;
        VanillaPlacement.Move(power, root, new Vector2(shift, bar.yMin - box.yMin));
        return box.xMin + shift;
    }

    // Health, stamina, eitr (for those who have it) and adrenaline (while there is some) side by side over the hotbar,
    // as wide as it, health the widest, each with its number above it. Returns the top of the row.
    private static float LayoutVitals(Player player, Rect bar, float scale)
    {
        float maxEitr = player.GetMaxEitr();
        float maxAdrenaline = player.GetMaxAdrenaline();
        if (maxAdrenaline > 0f)
        {
            lastMaxAdrenaline = maxAdrenaline;
        }

        float current = player.GetAdrenaline();
        bool hasEitr = maxEitr > 0f;
        bool hasAdrenaline = current > 0f && lastMaxAdrenaline > 0f;
        float weights = 5f + 3f + (hasEitr ? 2f : 0f) + (hasAdrenaline ? 2f : 0f);
        int count = 2 + (hasEitr ? 1 : 0) + (hasAdrenaline ? 1 : 0);
        float gap = BarGap * scale;
        float unit = (bar.width - gap * (count - 1)) / weights;
        float y = bar.yMax + SupplyRoom;
        float x = bar.xMin;

        float maxHealth = player.GetMaxHealth();
        float hp = player.GetHealth();
        health.Show(hp, maxHealth, new Vector2(x, y), unit * 5f, scale, hp < maxHealth * 0.25f);
        x += unit * 5f + gap;
        stamina.Show(player.GetStamina(), player.GetMaxStamina(), new Vector2(x, y), unit * 3f, scale, false);
        x += unit * 3f + gap;
        eitr.SetActive(hasEitr);
        if (hasEitr)
        {
            eitr.Show(player.GetEitr(), maxEitr, new Vector2(x, y), unit * 2f, scale, false);
            x += unit * 2f + gap;
        }

        adrenaline.SetActive(hasAdrenaline);
        if (hasAdrenaline)
        {
            adrenaline.Show(current, lastMaxAdrenaline, new Vector2(x, y), unit * 2f, scale, false);
        }

        return y + (BarHeight + NumberHeight) * scale + 1f;
    }

    // The status effects right of the hotbar, rows growing upwards, each with its time under it; the game's, White
    // Hilt's and other mods' alike, then the indicators other mods registered. When they do not all fit, the last place
    // goes through the ones left out, with how many they are. In the inventory and while the show-all key is held,
    // they are a list instead, each with its name and time. Returns the tray's upper right corner.
    private static Vector2 LayoutStatus(Player player, Rect bar, float scale)
    {
        bool listed = InventoryGui.IsVisible() || showAllHeld;
        effects.Clear();
        player.GetSEMan().GetHUDStatusEffects(effects);
        WhiteHiltHud.ReadIndicators(indicators);
        tray.Clear();
        bool flashing = false;
        foreach (StatusEffect effect in effects)
        {
            if (effect != null && !effect.m_hidden)
            {
                float remaining = effect.m_ttl > 0f ? Mathf.Clamp01(1f - effect.m_time / effect.m_ttl) : -1f;
                string name = listed ? Localization.instance.Localize(effect.m_name) : null;
                tray.Add(new TrayItem(effect.m_icon, effect.GetIconText(), Color.white, effect.m_flashIcon, remaining, name));
                flashing |= effect.m_flashIcon;
            }
        }

        foreach (HudIndicator indicator in indicators)
        {
            tray.Add(new TrayItem(indicator.Icon, indicator.Text, indicator.Tint, indicator.Flash, -1f, indicator.Name));
            flashing |= indicator.Flash;
        }

        statusFader.Wake(flashing || tray.Count != lastTray);
        lastTray = tray.Count;
        if (listed)
        {
            return ListStatus(bar, scale);
        }

        int max = Mathf.Max(2, HudSettings.MaxStatusEffects.Value);
        int columns = Mathf.Max(1, HudSettings.StatusColumns.Value);
        bool overflow = tray.Count > max;
        int shown = overflow ? max : tray.Count;
        while (statusSlots.Count < shown)
        {
            statusSlots.Add(new StatusSlot(statusLayer));
        }

        float size = StatusSize * scale;
        float step = size + StatusGap * scale;
        float rowStep = size + TimeHeight * scale + StatusGap * scale;
        float left = bar.xMax + WingGap * scale;
        float bottom = bar.yMin + TimeHeight * scale;
        for (int i = 0; i < statusSlots.Count; i++)
        {
            bool used = i < shown;
            statusSlots[i].SetActive(used);
            if (!used)
            {
                continue;
            }

            Vector2 position = new(left + i % columns * step, bottom + i / columns * rowStep);
            if (overflow && i == shown - 1)
            {
                int hidden = tray.Count - shown + 1;
                int turn = Mathf.FloorToInt(Time.unscaledTime / CycleSeconds) % hidden;
                statusSlots[i].ShowMore(tray[shown - 1 + turn], hidden, position, size, scale);
            }
            else
            {
                statusSlots[i].Show(tray[i], position, size, scale);
            }
        }

        int rows = shown == 0 ? 0 : (shown - 1) / columns + 1;
        return new Vector2(left + Mathf.Min(shown, columns) * step, bottom + rows * rowStep);
    }

    // Every status effect and indicator in a column right of the hotbar, each with its name and time beside it.
    // Returns the list's upper right corner.
    private static Vector2 ListStatus(Rect bar, float scale)
    {
        int count = Mathf.Min(tray.Count, MaxListed);
        while (statusSlots.Count < count)
        {
            statusSlots.Add(new StatusSlot(statusLayer));
        }

        float size = StatusSize * scale;
        float step = size + StatusGap * scale;
        float left = bar.xMax + WingGap * scale;
        float names = NameWidth * scale;
        for (int i = 0; i < statusSlots.Count; i++)
        {
            bool used = i < count;
            statusSlots[i].SetActive(used);
            if (used)
            {
                statusSlots[i].ShowListed(tray[i], new Vector2(left, bar.yMin + i * step), size, scale, names);
            }
        }

        return new Vector2(left + size + StatusGap * scale + names, bar.yMin + count * step);
    }

    // The half dark rounded panel behind the console, from the guardian power to the status effects and from under the
    // hotbar to over the bars, so they read against snow and sky; none with an opacity of 0. Returns the panel's top,
    // or the top of what it would go round without it.
    private static float PlacePanel(Rect around, float scale)
    {
        bool shown = HudSettings.PanelOpacity.Value > 0f;
        if (panel.gameObject.activeSelf != shown)
        {
            panel.gameObject.SetActive(shown);
        }

        if (!shown)
        {
            return around.yMax;
        }

        float pad = PanelPadding * scale;
        Place(panel.rectTransform, new Vector2(around.xMin - pad, around.yMin - pad),
            new Vector2(around.width + 2f * pad, around.height + 2f * pad));
        panel.pixelsPerUnitMultiplier = HudSprites.RoundedBoxCorner / Mathf.Max(1f, HudSettings.PanelRadius.Value * scale);
        return around.yMax + pad;
    }

    // The context lines in a strip over the panel, clear of it, centred on the hotbar; hidden when no line has anything
    // to say. Returns the top of the strip, or the given top without it.
    private static float LayoutStrip(Rect bar, float top, float scale)
    {
        WhiteHiltHud.ReadContexts(lines);
        if (lines.Count > StripLines)
        {
            lines.RemoveRange(StripLines, lines.Count - StripLines);
        }

        return strip.Show(lines, new Vector2(bar.center.x, top + StripGap * scale), scale) ?? top;
    }

    // With Calm, each part fades while nothing about it changes, and comes back when it does; everything shows while
    // the inventory is open. The vitals wake themselves while they are not full.
    private static void Fade(Hud hud, Player player)
    {
        bool calm = HudSettings.Calm.Value && !InventoryGui.IsVisible() && !showAllHeld;
        float now = Time.unscaledTime;
        float linger = Mathf.Max(1f, HudSettings.CalmSeconds.Value);
        float floor = Mathf.Clamp01(HudSettings.CalmOpacity.Value);

        float hp = player.GetHealth();
        bool hurt = hp < lastHealth - 0.5f;
        lastHealth = hp;
        ItemDrop.ItemData right = player.GetRightItem();
        ItemDrop.ItemData left = player.GetLeftItem();
        bool handsChanged = right != lastRight || left != lastLeft;
        lastRight = right;
        lastLeft = left;
        handsFader.Wake(hurt || handsChanged || HotbarPressed() || player.GetAttackDrawPercentage() > 0f);

        health.Fader.Wake(hp < player.GetMaxHealth() - 0.5f);
        stamina.Fader.Wake(player.GetStamina() < player.GetMaxStamina() - 0.5f);
        eitr.Fader.Wake(player.GetEitr() < player.GetMaxEitr() - 0.5f);
        adrenaline.Fader.Wake(true);

        health.Fader.Apply(calm, now, linger, floor);
        stamina.Fader.Apply(calm, now, linger, floor);
        eitr.Fader.Apply(calm, now, linger, floor);
        adrenaline.Fader.Apply(calm, now, linger, floor);
        foodFader.Apply(calm, now, linger, floor);
        statusFader.Apply(calm, now, linger * StatusLinger, floor);
        handsFader.Apply(calm, now, linger, floor);

        hotbarGroup = Group(hotbarGroup, Hotbar(hud)?.gameObject);
        powerGroup = Group(powerGroup, hud.m_gpRoot != null ? hud.m_gpRoot.gameObject : null);
        if (hotbarGroup != null) hotbarGroup.alpha = handsFader.Alpha;
        if (powerGroup != null) powerGroup.alpha = handsFader.Alpha;

        // The panel fades with the quiet screen once everything on it has faded.
        float shown = Mathf.Max(Mathf.Max(health.Fader.Alpha, stamina.Fader.Alpha), Mathf.Max(foodFader.Alpha, Mathf.Max(statusFader.Alpha, handsFader.Alpha)));
        panel.color = new Color(panelColor.r, panelColor.g, panelColor.b, Mathf.Clamp01(HudSettings.PanelOpacity.Value) * shown);
    }

    // The key hints beside the console: the game's (for fighting, building, the inventory, fishing) at its right and the
    // build tools' at its left, each made smaller to fit, or over the console when there is too little room. As set,
    // they show for a few seconds after taking out a tool or weapon, always, or only on demand; always in the
    // inventory and while the show-all key is held.
    private static void PlaceHints(Player player, Rect console)
    {
        HudHints mode = HudSettings.KeyHints.Value;
        bool inventory = InventoryGui.IsVisible();
        string buildText = BuildHint != null && BuildHint.gameObject.activeInHierarchy ? BuildHint.text : string.Empty;
        int key = (player.InPlaceMode() ? 1 : 0) + (inventory ? 2 : 0) + 4 * (player.GetCurrentWeapon()?.GetHashCode() ?? 0)
            + 31 * buildText.GetHashCode();
        hintFader.Wake(key != lastHintKey || inventory || showAllHeld);
        lastHintKey = key;
        float linger = mode == HudHints.Hidden ? HiddenLinger : Mathf.Max(1f, HudSettings.KeyHintSeconds.Value);
        hintFader.Apply(mode != HudHints.Always, Time.unscaledTime, mode == HudHints.Always ? float.MaxValue : linger, 0f);

        KeyHints hints = KeyHints.instance;
        hintGroups.Clear();
        if (hints != null)
        {
            hintGroups.Add(hints.m_buildHints);
            hintGroups.Add(hints.m_combatHints);
            hintGroups.Add(hints.m_inventoryHints);
            hintGroups.Add(hints.m_inventoryWithContainerHints);
            hintGroups.Add(hints.m_fishingHints);
            hintGroups.Add(hints.m_barberHints);
        }

        foreach (GameObject group in hintGroups)
        {
            if (group == null || !group.activeInHierarchy || group.transform is not RectTransform rect)
            {
                continue;
            }

            // Measured by what is drawn in it, as a hint group may be a row as wide as the screen.
            drawn.Clear();
            group.GetComponentsInChildren(false, graphics);
            foreach (Graphic graphic in graphics)
            {
                if (graphic.enabled && graphic.color.a > 0f)
                {
                    drawn.Add(graphic.rectTransform);
                }
            }

            if (VanillaPlacement.Bounds(root, drawn, out Rect box) && box.width > 1f && box.height > 1f)
            {
                Fit(rect, box, console, true);
            }

            HintAlpha(group).alpha = hintFader.Alpha;
        }

        if (buildText.Length > 0)
        {
            RectTransform rect = BuildHint.rectTransform;
            float ratio = rect.lossyScale.x / Mathf.Max(0.0001f, root.lossyScale.x);
            Vector2 corner = (Vector2)root.InverseTransformPoint(rect.position) - root.rect.min;
            Fit(rect, new Rect(corner, new Vector2(BuildHint.preferredWidth, BuildHint.preferredHeight) * ratio), console, false);
            HintAlpha(BuildHint.gameObject).alpha = hintFader.Alpha;
        }
    }

    // Fits a box of hints beside the console, at its right or left, its bottom on the console's, made smaller to fit
    // the room there; with too little room, over the console instead. Scales first and moves on the next frame, as the
    // box is measured at the scale it has.
    private static void Fit(RectTransform mover, Rect box, Rect console, bool right)
    {
        float share = VanillaPlacement.ScaleOf(mover);
        float width = box.width / share;
        float screen = root.rect.width;
        float from = right ? console.xMax + HintGap : HintMargin;
        float to = right ? screen - HintMargin : console.xMin - HintGap;
        float target = Mathf.Min(1f, (to - from) / width);
        Vector2 corner;
        if (target >= MinHintScale)
        {
            corner = new Vector2(right ? from : to - width * target, console.yMin);
        }
        else
        {
            target = Mathf.Min(1f, (screen / 2f - HintMargin) / width);
            corner = new Vector2(right ? console.xMax - width * target : console.xMin, console.yMax + HintGap);
        }

        if (Mathf.Abs(target - share) > 0.01f)
        {
            VanillaPlacement.Scale(mover, target);
            return;
        }

        VanillaPlacement.Move(mover, root, corner - box.min);
    }

    private static CanvasGroup HintAlpha(GameObject owner)
    {
        if (!hintAlphas.TryGetValue(owner, out CanvasGroup group) || group == null)
        {
            group = owner.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = owner.AddComponent<CanvasGroup>();
            }

            hintAlphas[owner] = group;
        }

        return group;
    }

    private static bool HotbarPressed()
    {
        for (int i = 1; i <= HotbarKeys; i++)
        {
            if (ZInput.GetButtonDown($"Hotbar{i}") || ZInput.GetButtonDown($"Hotbar{i}Alt"))
            {
                return true;
            }
        }

        return ZInput.GetButtonDown("JoyHotbarLeft") || ZInput.GetButtonDown("JoyHotbarRight") || ZInput.GetButtonDown("JoyHotbarUse");
    }

    private static CanvasGroup Group(CanvasGroup group, GameObject owner)
    {
        if (owner == null || (group != null && group.gameObject == owner))
        {
            return group;
        }

        group = owner.GetComponent<CanvasGroup>();
        return group != null ? group : owner.AddComponent<CanvasGroup>();
    }

    // The hotbar and the guardian power are the game's: shown fully again when the console is not used.
    private static void ShowVanillaHands()
    {
        if (hotbarGroup != null) hotbarGroup.alpha = 1f;
        if (powerGroup != null) powerGroup.alpha = 1f;
        foreach (CanvasGroup group in hintAlphas.Values)
        {
            if (group != null) group.alpha = 1f;
        }
    }

    private static void SetVisible(bool visible)
    {
        if (root != null && root.gameObject.activeSelf != visible)
        {
            root.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            ShowVanillaHands();
        }
    }

    // Under the HUD root, filling it, so the console goes away with the HUD (Ctrl + F3, cutscenes, media mode) and
    // its positions are measured from the screen's lower left corner.
    private static void Ensure(Hud hud)
    {
        if (root != null && root.parent == hud.m_rootObject.transform)
        {
            return;
        }

        if (root != null)
        {
            Object.Destroy(root.gameObject);
        }

        foodSlots.Clear();
        statusSlots.Clear();
        hotbar = null;
        hotbarGroup = null;
        powerGroup = null;
        root = Layer(hud.m_rootObject.transform, "WhiteHiltConsole");
        root.SetAsFirstSibling();
        panel = Box(root, "Panel", Color.clear);
        panel.sprite = HudSprites.RoundedBox;
        panel.type = Image.Type.Sliced;
        foodLayer = Layer(root, "Food");
        statusLayer = Layer(root, "Status");
        foodFader = new Fader(foodLayer.gameObject.AddComponent<CanvasGroup>());
        statusFader = new Fader(statusLayer.gameObject.AddComponent<CanvasGroup>());
        handsFader = new Fader(null);
        hintFader = new Fader(null);
        health = new Bar(root, "Health", healthColor, healthBack);
        stamina = new Bar(root, "Stamina", staminaColor, staminaBack);
        eitr = new Bar(root, "Eitr", eitrColor, eitrBack);
        adrenaline = new Bar(root, "Adrenaline", adrenalineColor, adrenalineBack);
        strip = new Strip(root);
    }

    // A layer filling its parent, measured from the lower left corner.
    private static RectTransform Layer(Transform parent, string name)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static RectTransform Element(Transform parent, string name)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        return rect;
    }

    private static Image Box(Transform parent, string name, Color color)
    {
        Image image = Element(parent, name).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(Transform parent, string name, TextAnchor alignment)
    {
        GameObject go = GUIManager.Instance.CreateText(string.Empty, parent, Vector2.zero, Vector2.zero, Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, 14, textColor, true, Color.black, 10f, 10f, false);
        go.name = name;
        Text text = go.GetComponent<Text>();
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = true;
        text.rectTransform.pivot = Vector2.zero;
        return text;
    }

    // A countdown ring: the share left, from the top round clockwise.
    private static Image Ring(Transform parent, string name, Color color)
    {
        Image ring = Box(parent, name, color);
        ring.sprite = HudSprites.Ring;
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;
        return ring;
    }

    // A countdown's colour: its own while more than half is left, yellow under half, red under a fifth, the red
    // pulsing.
    private static Color Countdown(Color full, float share)
    {
        if (share > RingHalf)
        {
            return full;
        }

        if (share > RingLow)
        {
            return foodHalf;
        }

        return new Color(foodLow.r, foodLow.g, foodLow.b, 0.55f + 0.45f * Mathf.Sin(Time.time * PulseSpeed));
    }

    private static Color Faint(Color color)
    {
        return new Color(color.r, color.g, color.b, 0.25f);
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static string Duration(float seconds)
    {
        return seconds >= 60f ? Mathf.CeilToInt(seconds / 60f) + "m" : Mathf.FloorToInt(seconds) + "s";
    }

    private static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    // Fades a part of the console with Calm: shown while it is awake and for a while after, else faded to the floor.
    private sealed class Fader
    {
        private readonly CanvasGroup group;
        private float awake = float.MinValue;

        internal Fader(CanvasGroup group)
        {
            this.group = group;
            if (group != null)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        internal float Alpha { get; private set; } = 1f;

        internal void Wake(bool changed)
        {
            if (changed)
            {
                awake = Time.unscaledTime;
            }
        }

        internal void Apply(bool calm, float now, float linger, float floor)
        {
            float target = !calm || now - awake < linger ? 1f : floor;
            Alpha = Mathf.MoveTowards(Alpha, target, Time.unscaledDeltaTime * FadeSpeed);
            if (group != null)
            {
                group.alpha = Alpha;
            }
        }
    }

    // A status effect or an indicator as the tray shows it.
    private readonly struct TrayItem
    {
        internal readonly Sprite Icon;
        internal readonly string Text;
        internal readonly Color Tint;
        internal readonly bool Flash;

        // Share of the time left, for the countdown ring; below 0 for none.
        internal readonly float Remaining;

        // The name, shown in the list; null when only the icons show.
        internal readonly string Name;

        internal TrayItem(Sprite icon, string text, Color tint, bool flash, float remaining, string name)
        {
            Icon = icon;
            Text = text ?? string.Empty;
            Tint = tint;
            Flash = flash;
            Remaining = remaining;
            Name = name ?? string.Empty;
        }
    }

    // One vital: the number over a bar.
    private sealed class Bar
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Image fill;
        private readonly Text number;
        private string shown;

        internal Bar(Transform parent, string name, Color color, Color backColor)
        {
            root = Element(parent, name);
            Fader = new Fader(root.gameObject.AddComponent<CanvasGroup>());
            back = Box(root, "Back", backColor);
            fill = Box(root, "Fill", color);
            number = Label(root, "Number", TextAnchor.LowerLeft);
        }

        internal Fader Fader { get; }

        internal void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        internal void Show(float value, float max, Vector2 position, float width, float scale, bool low)
        {
            SetActive(true);
            float height = BarHeight * scale;
            Place(root, position, new Vector2(width, height + NumberHeight * scale));
            Place(back.rectTransform, Vector2.zero, new Vector2(width, height));
            float share = max > 0f ? Mathf.Clamp01(value / max) : 0f;
            Place(fill.rectTransform, Vector2.zero, new Vector2(width * share, height));
            Place(number.rectTransform, new Vector2(0f, height + 1f), new Vector2(width, NumberHeight * scale));
            number.fontSize = Mathf.RoundToInt(15f * scale);
            string text = $"<color=#{Hex(low ? lowColor : textColor)}>{Mathf.CeilToInt(value)}</color>"
                + $"<size={Mathf.RoundToInt(12f * scale)}><color=#{Hex(dimColor)}> / {Mathf.CeilToInt(max)}</color></size>";
            if (text != shown)
            {
                number.text = text;
                shown = text;
            }
        }
    }

    // One food: the icon on a dark disc, a ring round it that empties and turns from green to red as the food runs
    // out, and the time left under it.
    private sealed class FoodSlot
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Image track;
        private readonly Image ring;
        private readonly Image icon;
        private readonly Image crossA;
        private readonly Image crossB;
        private readonly Text time;

        internal FoodSlot(Transform parent)
        {
            root = Element(parent, "Food");
            back = Box(root, "Back", panelColor);
            back.sprite = HudSprites.Disc;
            track = Box(root, "Track", Faint(foodFull));
            track.sprite = HudSprites.Ring;
            ring = Ring(root, "Ring", foodFull);
            icon = Box(root, "Icon", Color.white);
            icon.preserveAspect = true;
            crossA = Box(root, "Cross", foodLow);
            crossB = Box(root, "Cross", foodLow);
            foreach (Image bar in new[] { crossA, crossB })
            {
                bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            crossA.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            crossB.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            crossA.gameObject.SetActive(false);
            crossB.gameObject.SetActive(false);
            time = Label(root, "Time", TextAnchor.UpperCenter);
        }

        internal void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        // A place with no food in it: the dark disc in a red ring, a red cross over it and no time under it. The ring
        // pulses when nothing at all is eaten.
        internal void ShowEmpty(Vector2 position, float size, float scale, bool starving)
        {
            Layout(position, size, scale);
            SetEmpty(true);
            Color color = starving
                ? new Color(foodLow.r, foodLow.g, foodLow.b, 0.55f + 0.45f * Mathf.Sin(Time.time * PulseSpeed * 0.5f))
                : new Color(foodLow.r, foodLow.g, foodLow.b, 0.8f);
            ring.color = color;
            ring.fillAmount = 1f;
            track.color = Faint(foodLow);
            float length = size * CrossLength;
            float width = Mathf.Max(2f, CrossWidth * scale);
            crossA.rectTransform.sizeDelta = crossB.rectTransform.sizeDelta = new Vector2(length, width);
            crossA.rectTransform.anchoredPosition = crossB.rectTransform.anchoredPosition = Vector2.zero;
            crossA.color = crossB.color = color;
            if (time.text.Length > 0)
            {
                time.text = string.Empty;
            }
        }

        // Returns whether the food asks for attention: half gone or more, or ready to be eaten again.
        internal bool Show(Player.Food food, Vector2 position, float size, float scale)
        {
            Layout(position, size, scale);
            SetEmpty(false);
            float burn = food.m_item?.m_shared?.m_foodBurnTime ?? 0f;
            float share = burn > 0f ? Mathf.Clamp01(food.m_time / burn) : 1f;
            Color color = Countdown(foodFull, share);
            ring.color = color;
            ring.fillAmount = share;
            track.color = Faint(color);
            Sprite sprite = food.m_item?.GetIcon();
            if (icon.sprite != sprite)
            {
                icon.sprite = sprite;
            }

            bool again = food.CanEatAgain();
            icon.color = again ? new Color(1f, 1f, 1f, 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f) : Color.white;
            float seconds = food.m_time / Game.m_foodRate;
            time.fontSize = Mathf.RoundToInt(12f * scale);
            time.color = seconds >= 60f ? dimColor : new Color(1f, 1f, 1f, 0.4f + Mathf.Sin(Time.time * 10f) * 0.6f);
            string text = Duration(seconds);
            if (time.text != text)
            {
                time.text = text;
            }

            return again || share <= 0.5f;
        }

        private void Layout(Vector2 position, float size, float scale)
        {
            float inset = IconInset * size;
            Place(root, position, new Vector2(size, size));
            Place(back.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(track.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(ring.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(icon.rectTransform, new Vector2(inset, inset), new Vector2(size - 2f * inset, size - 2f * inset));
            Place(time.rectTransform, new Vector2(0f, -TimeHeight * scale), new Vector2(size, TimeHeight * scale));
        }

        private void SetEmpty(bool empty)
        {
            if (icon.gameObject.activeSelf == empty)
            {
                icon.gameObject.SetActive(!empty);
                crossA.gameObject.SetActive(empty);
                crossB.gameObject.SetActive(empty);
            }
        }
    }

    // One place in the tray: an icon on a dark disc, flashing when it asks for it, a ring round it that empties and
    // turns yellow and red as its time runs out, and its text under it; in the list, its name and time beside it.
    private sealed class StatusSlot
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Image ring;
        private readonly Image icon;
        private readonly Text more;
        private readonly Text time;
        private readonly Text name;

        internal StatusSlot(Transform parent)
        {
            root = Element(parent, "Status");
            back = Box(root, "Back", panelColor);
            back.sprite = HudSprites.Disc;
            ring = Ring(root, "Ring", ringColor);
            icon = Box(root, "Icon", Color.white);
            icon.preserveAspect = true;
            more = Label(root, "More", TextAnchor.UpperRight);
            time = Label(root, "Time", TextAnchor.UpperCenter);
            name = Label(root, "Name", TextAnchor.MiddleLeft);
        }

        internal void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        internal void Show(TrayItem item, Vector2 position, float size, float scale)
        {
            Layout(position, size, scale);
            SetText(more, false, string.Empty);
            SetText(name, false, string.Empty);
            Fill(item, item.Text);
        }

        // The place for the effects left out: one of them in turn, with how many they are in its corner.
        internal void ShowMore(TrayItem item, int count, Vector2 position, float size, float scale)
        {
            Layout(position, size, scale);
            SetText(more, true, "+" + count);
            SetText(name, false, string.Empty);
            Fill(item, item.Text);
        }

        // A row of the list: the icon with its name and time beside it.
        internal void ShowListed(TrayItem item, Vector2 position, float size, float scale, float width)
        {
            Layout(position, size, scale);
            SetText(more, false, string.Empty);
            Place(name.rectTransform, new Vector2(size + StatusGap * scale, 0f), new Vector2(width, size));
            name.fontSize = Mathf.RoundToInt(14f * scale);
            string text = item.Text.Length > 0 ? $"{item.Name}  <color=#{Hex(dimColor)}>{item.Text}</color>" : item.Name;
            SetText(name, true, text);
            Fill(item, string.Empty);
        }

        private void Fill(TrayItem item, string under)
        {
            if (icon.sprite != item.Icon)
            {
                icon.sprite = item.Icon;
            }

            icon.color = item.Flash && Mathf.Sin(Time.time * 10f) > 0f ? lowColor : item.Tint;
            bool timed = item.Remaining >= 0f;
            if (ring.gameObject.activeSelf != timed)
            {
                ring.gameObject.SetActive(timed);
            }

            ring.fillAmount = timed ? item.Remaining : 0f;
            ring.color = timed ? Countdown(ringColor, item.Remaining) : ringColor;
            if (time.text != under)
            {
                time.text = under;
            }
        }

        private static void SetText(Text text, bool shown, string value)
        {
            if (text.gameObject.activeSelf != shown)
            {
                text.gameObject.SetActive(shown);
            }

            if (shown && text.text != value)
            {
                text.text = value;
            }
        }

        private void Layout(Vector2 position, float size, float scale)
        {
            float inset = IconInset * size;
            Place(root, position, new Vector2(size, size));
            Place(back.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(ring.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(icon.rectTransform, new Vector2(inset, inset), new Vector2(size - 2f * inset, size - 2f * inset));
            Place(more.rectTransform, new Vector2(size * 0.3f, size * 0.55f), new Vector2(size, size * 0.6f));
            Place(time.rectTransform, new Vector2(0f, -TimeHeight * scale), new Vector2(size, TimeHeight * scale));
            more.fontSize = Mathf.RoundToInt(12f * scale);
            time.fontSize = Mathf.RoundToInt(11f * scale);
            time.color = dimColor;
        }
    }

    // The context lines: centred text on a dark panel sized to it.
    private sealed class Strip
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Text text;
        private string shown;

        internal Strip(Transform parent)
        {
            root = Element(parent, "Context");
            back = Box(root, "Back", panelColor);
            back.sprite = HudSprites.RoundedBox;
            back.type = Image.Type.Sliced;
            text = Label(root, "Lines", TextAnchor.MiddleCenter);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }

        // Returns the strip's top, or null when it is hidden.
        internal float? Show(List<string> lines, Vector2 bottomCentre, float scale)
        {
            bool any = lines.Count > 0;
            if (root.gameObject.activeSelf != any)
            {
                root.gameObject.SetActive(any);
            }

            if (!any)
            {
                shown = null;
                return null;
            }

            string joined = string.Join("\n", lines);
            text.fontSize = Mathf.RoundToInt(14f * scale);
            if (joined != shown)
            {
                text.text = joined;
                shown = joined;
            }

            float height = lines.Count * StripLine * scale + 6f * scale;
            float width = text.preferredWidth + 2f * StripPadding * scale;
            Place(root, new Vector2(bottomCentre.x - width / 2f, bottomCentre.y), new Vector2(width, height));
            Place(back.rectTransform, Vector2.zero, new Vector2(width, height));

            // Rounded as the panel under it, at most half the strip's height.
            float radius = Mathf.Min(HudSettings.PanelRadius.Value * scale, height / 2f);
            back.pixelsPerUnitMultiplier = HudSprites.RoundedBoxCorner / Mathf.Max(1f, radius);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            return bottomCentre.y + height;
        }
    }
}
