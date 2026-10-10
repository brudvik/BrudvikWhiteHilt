using BepInEx.Configuration;
using BrudvikWhiteHilt.Backpack;
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
/// list, event bar and build panel are hidden while it shows. The console rises above the ship controls when they are
/// in the way, keeps other mods' HUD elements clear of itself, and with <see cref="HudSettings.Calm"/> each part fades
/// while nothing about it changes.
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
    private const float StatusSize = 34f;
    private const float StatusGap = 6f;
    private const float TimeHeight = 14f;
    private const float ContextGap = 10f;
    private const float StripGap = 6f;
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
    private static readonly List<RectTransform> context = new();
    private static readonly List<RectTransform> single = new();
    private static readonly List<StatusEffect> effects = new();
    private static readonly List<HudIndicator> indicators = new();
    private static readonly List<TrayItem> tray = new();
    private static readonly List<string> lines = new();
    private static readonly List<FoodSlot> foodSlots = new();
    private static readonly List<StatusSlot> statusSlots = new();
    private static readonly HashSet<Transform> native = new();
    private static readonly HashSet<RectTransform> foreign = new();

    private static RectTransform root;
    private static RectTransform foodLayer;
    private static RectTransform statusLayer;
    private static HotkeyBar hotbar;
    private static Bar health;
    private static Bar stamina;
    private static Bar eitr;
    private static Bar adrenaline;
    private static Strip strip;
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
        float scale = Mathf.Clamp(HudSettings.Scale.Value, 0.6f, 1.6f);
        Rect bar = PlaceHotbar(hud, scale);
        float left = LayoutFood(player, bar, scale);
        left = PlaceGuardianPower(hud, left, bar);
        float top = LayoutVitals(player, bar, scale);
        Vector2 corner = LayoutStatus(player, bar, scale);
        top = Mathf.Max(LayoutStrip(bar, top, scale), corner.y);
        AvoidForeign(hud, Rect.MinMaxRect(left, bar.yMin, corner.x, top));
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
    // it: red while there is too little, White Hilt's colour when nearby chests make up the rest. Read from the game's
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

    // Centres the hotbar at the bottom, raised above the ship controls when they are under it. Returns the
    // box of its slots; with no slots yet, the box eight slots would take.
    private static Rect PlaceHotbar(Hud hud, float scale)
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

        float half = slots.width / 2f + (FoodSize + StatusSize) * 4f * scale;
        float lift = ContextTop(hud, width / 2f - half, width / 2f + half);
        if (lift > 0f)
        {
            bottom = Mathf.Max(bottom, lift + ContextGap);
        }

        Vector2 delta = new(width / 2f - slots.center.x, bottom - slots.yMin);
        if (measured && bar != null)
        {
            VanillaPlacement.Move((RectTransform)bar.transform, root, delta);
        }

        slots.position += delta;
        return slots;
    }

    // The top of the game's ship controls where they overlap the console's width; 0 when none do.
    private static float ContextTop(Hud hud, float left, float right)
    {
        // The build panel is hidden: its piece and cost are in the context strip.
        context.Clear();
        if (hud.m_shipHudRoot != null && hud.m_shipHudRoot.activeInHierarchy)
        {
            context.Add(hud.m_shipControlsRoot != null ? hud.m_shipControlsRoot.transform as RectTransform : null);
            context.Add(hud.m_shipWindIndicatorRoot);
        }

        float top = 0f;
        foreach (RectTransform rect in context)
        {
            single.Clear();
            single.Add(rect);
            if (VanillaPlacement.Bounds(root, single, out Rect box) && box.xMax > left && box.xMin < right
                && box.yMin < root.rect.height / 2f)
            {
                top = Mathf.Max(top, box.yMax);
            }
        }

        return top;
    }

    // The food the player has eaten, left of the hotbar: icon, a frame coloured by how much is left and the time under
    // it, flashing when it can be eaten again. Returns the left edge of the row.
    private static float LayoutFood(Player player, Rect bar, float scale)
    {
        List<Player.Food> foods = player.GetFoods();
        while (foodSlots.Count < foods.Count)
        {
            foodSlots.Add(new FoodSlot(foodLayer));
        }

        float size = FoodSize * scale;
        float gap = FoodGap * scale;
        float right = bar.xMin - WingGap * scale;
        float left = right - foods.Count * size - Mathf.Max(0, foods.Count - 1) * gap;
        float y = bar.yMin + TimeHeight * scale;
        bool hungry = foods.Count != lastFoods;
        lastFoods = foods.Count;
        for (int i = 0; i < foodSlots.Count; i++)
        {
            bool used = i < foods.Count;
            foodSlots[i].SetActive(used);
            if (used)
            {
                hungry |= foodSlots[i].Show(foods[i], new Vector2(left + i * (size + gap), y), size, scale);
            }
        }

        foodFader.Wake(hungry);
        return foods.Count > 0 ? left : right + WingGap * scale;
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
    // says how many are left out. Returns the tray's upper right corner.
    private static Vector2 LayoutStatus(Player player, Rect bar, float scale)
    {
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
                tray.Add(new TrayItem(effect.m_icon, effect.GetIconText(), Color.white, effect.m_flashIcon, remaining));
                flashing |= effect.m_flashIcon;
            }
        }

        foreach (HudIndicator indicator in indicators)
        {
            tray.Add(new TrayItem(indicator.Icon, indicator.Text, indicator.Tint, indicator.Flash, -1f));
            flashing |= indicator.Flash;
        }

        statusFader.Wake(flashing || tray.Count != lastTray);
        lastTray = tray.Count;

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
                statusSlots[i].ShowMore(tray.Count - shown + 1, position, size, scale);
            }
            else
            {
                statusSlots[i].Show(tray[i], position, size, scale);
            }
        }

        int rows = shown == 0 ? 0 : (shown - 1) / columns + 1;
        return new Vector2(left + Mathf.Min(shown, columns) * step, bottom + rows * rowStep);
    }

    // The context lines in a strip over the bars, centred on the hotbar; hidden when no line has anything to say.
    // Returns the top of the strip, or of the bars without it.
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
        KeyboardShortcut showAll = HudSettings.KeyShowAll.Value;
        bool held = showAll.MainKey != KeyCode.None && showAll.IsPressed() && !BackpackInput.Typing();
        bool calm = HudSettings.Calm.Value && !InventoryGui.IsVisible() && !held;
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
        if (group != null || owner == null)
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
        foodLayer = Layer(root, "Food");
        statusLayer = Layer(root, "Status");
        foodFader = new Fader(foodLayer.gameObject.AddComponent<CanvasGroup>());
        statusFader = new Fader(statusLayer.gameObject.AddComponent<CanvasGroup>());
        handsFader = new Fader(null);
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

        internal TrayItem(Sprite icon, string text, Color tint, bool flash, float remaining)
        {
            Icon = icon;
            Text = text ?? string.Empty;
            Tint = tint;
            Flash = flash;
            Remaining = remaining;
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
            time = Label(root, "Time", TextAnchor.UpperCenter);
        }

        internal void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        // Returns whether the food asks for attention: half gone or more, or ready to be eaten again.
        internal bool Show(Player.Food food, Vector2 position, float size, float scale)
        {
            float inset = IconInset * size;
            Place(root, position, new Vector2(size, size));
            Place(back.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(track.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(ring.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(icon.rectTransform, new Vector2(inset, inset), new Vector2(size - 2f * inset, size - 2f * inset));
            Place(time.rectTransform, new Vector2(0f, -TimeHeight * scale), new Vector2(size, TimeHeight * scale));

            float burn = food.m_item?.m_shared?.m_foodBurnTime ?? 0f;
            float share = burn > 0f ? Mathf.Clamp01(food.m_time / burn) : 1f;
            Color color = share > 0.5f ? foodFull : share > 0.2f ? foodHalf : foodLow;
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
    }

    // One place in the tray: an icon on a dark disc, flashing when it asks for it, a ring round it that empties as its
    // time runs out, and its text under it.
    private sealed class StatusSlot
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Image ring;
        private readonly Image icon;
        private readonly Text more;
        private readonly Text time;

        internal StatusSlot(Transform parent)
        {
            root = Element(parent, "Status");
            back = Box(root, "Back", panelColor);
            back.sprite = HudSprites.Disc;
            ring = Ring(root, "Ring", ringColor);
            icon = Box(root, "Icon", Color.white);
            icon.preserveAspect = true;
            more = Label(root, "More", TextAnchor.MiddleCenter);
            time = Label(root, "Time", TextAnchor.UpperCenter);
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
            if (!icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(true);
                more.gameObject.SetActive(false);
            }

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
            if (time.text != item.Text)
            {
                time.text = item.Text;
            }
        }

        internal void ShowMore(int count, Vector2 position, float size, float scale)
        {
            Layout(position, size, scale);
            if (icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(false);
                ring.gameObject.SetActive(false);
                more.gameObject.SetActive(true);
                time.text = string.Empty;
            }

            more.text = "+" + count;
        }

        private void Layout(Vector2 position, float size, float scale)
        {
            float inset = IconInset * size;
            Place(root, position, new Vector2(size, size));
            Place(back.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(ring.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(icon.rectTransform, new Vector2(inset, inset), new Vector2(size - 2f * inset, size - 2f * inset));
            Place(more.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(time.rectTransform, new Vector2(0f, -TimeHeight * scale), new Vector2(size, TimeHeight * scale));
            more.fontSize = Mathf.RoundToInt(15f * scale);
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
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            return bottomCentre.y + height;
        }
    }
}
