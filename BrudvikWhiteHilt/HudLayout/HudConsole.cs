using BepInEx.Configuration;
using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
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
public static partial class HudConsole
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
}
