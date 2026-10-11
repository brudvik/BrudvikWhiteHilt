using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Summoning;

/// <summary>
/// The Dragon Horn, sold by the Bog Witch once Moder is slain. Blown like the other horns, it calls a White Dragon that
/// flies in, fights at the caller's side for a while and flies off again. Then the horn must rest a game day.
/// </summary>
public static class DragonHorn
{
    /// <summary>Prefab name of the horn.</summary>
    public const string ItemName = "WhiteHiltDragonHorn";

    private const string Section = "DragonHorn";
    private const string TraderPrefabName = "BogWitch";

    // Saved on the caller: when the horn may call again, in ticks of the world clock.
    private const string ReadyKey = "whitehilt_dragonhorn_ready";

    /// <summary>Whether the Bog Witch sells the horn and it calls the dragon.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Coins the horn costs.</summary>
    public static ConfigEntry<int> Price { get; private set; }

    /// <summary>Global key needed before the horn is sold and calls.</summary>
    public static ConfigEntry<string> RequiredKey { get; private set; }

    /// <summary>Game hours the horn rests after a call.</summary>
    public static ConfigEntry<float> CooldownHours { get; private set; }

    /// <summary>Game hours the dragon fights at the caller's side.</summary>
    public static ConfigEntry<float> StayHours { get; private set; }

    /// <summary>Seconds of blowing before the dragon is called.</summary>
    public static ConfigEntry<float> BlowSeconds { get; private set; }

    /// <summary>Distance over which the horn is heard.</summary>
    public static ConfigEntry<float> SoundRange { get; private set; }

    /// <summary>Stars of the White Dragon.</summary>
    public static ConfigEntry<int> Stars { get; private set; }

    /// <summary>Health of the White Dragon before stars.</summary>
    public static ConfigEntry<float> Health { get; private set; }

    /// <summary>Fire damage of each flame of its breath before stars.</summary>
    public static ConfigEntry<float> FireDamage { get; private set; }

    /// <summary>Flames in one breath.</summary>
    public static ConfigEntry<int> Flames { get; private set; }

    /// <summary>Seconds between two breaths at most.</summary>
    public static ConfigEntry<float> BreathSeconds { get; private set; }

    /// <summary>Whether the dragon fights bosses.</summary>
    public static ConfigEntry<bool> FightsBosses { get; private set; }

    /// <summary>Distance from the caller at which the dragon appears and flies in from.</summary>
    public static ConfigEntry<float> ArriveDistance { get; private set; }

    /// <summary>Distance from the caller beyond which the dragon flies off.</summary>
    public static ConfigEntry<float> LeaveDistance { get; private set; }

    /// <summary>On its way off, the dragon vanishes once no player is this close.</summary>
    public static ConfigEntry<float> VanishDistance { get; private set; }

    /// <summary>Longest time the dragon takes to fly in or off.</summary>
    public static ConfigEntry<float> FlightSeconds { get; private set; }

    /// <summary>Real seconds in a game hour.</summary>
    public static float GameHourSeconds => (EnvMan.instance != null ? EnvMan.instance.m_dayLengthSec : 1800f) / 24f;

    /// <summary>
    /// Binds the settings, adds the texts and registers the horn and the Bog Witch's offer. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "The Bog Witch sells the Dragon Horn, and it calls a White Dragon.");
        Price = WhiteHiltConfig.BindAdminOnly(Section, "Price", 3000, "Coins the Dragon Horn costs at the Bog Witch.", new AcceptableValueRange<int>(0, 20000));
        RequiredKey = WhiteHiltConfig.BindAdminOnly(Section, "RequiredKey", "defeated_dragon",
            "Global key needed before the Bog Witch sells the horn and before it calls (defeated_dragon = Moder slain). Empty for none.");
        CooldownHours = WhiteHiltConfig.BindAdminOnly(Section, "CooldownHours", 24f,
            "Game hours the horn rests after a call. A game day of 24 hours is 30 real minutes, and sleeping passes the hours too.",
            new AcceptableValueRange<float>(1f, 240f));
        StayHours = WhiteHiltConfig.BindAdminOnly(Section, "StayHours", 3f,
            "Game hours the White Dragon fights at your side before it flies off (3 = about 4 real minutes).", new AcceptableValueRange<float>(0.5f, 24f));
        BlowSeconds = WhiteHiltConfig.BindAdminOnly(Section, "BlowSeconds", 3f, "Seconds of uninterrupted blowing before the dragon is called.",
            new AcceptableValueRange<float>(1f, 10f));
        SoundRange = WhiteHiltConfig.BindAdminOnly(Section, "SoundRange", 150f, "Maximum distance at which the horn can be heard.",
            new AcceptableValueRange<float>(10f, 500f));
        Stars = WhiteHiltConfig.BindAdminOnly(Section, "Stars", 5, "Stars of the White Dragon. Stars give it more health, damage and size.",
            new AcceptableValueRange<int>(0, 5));
        Health = WhiteHiltConfig.BindAdminOnly(Section, "Health", 600f, "Health of the White Dragon before stars. Applies after a restart.",
            new AcceptableValueRange<float>(10f, 20000f));
        FireDamage = WhiteHiltConfig.BindAdminOnly(Section, "FireDamage", 8f, "Fire damage of each flame in its breath before stars. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 200f));
        Flames = WhiteHiltConfig.BindAdminOnly(Section, "Flames", 8, "Flames in one breath. Applies after a restart.", new AcceptableValueRange<int>(1, 30));
        BreathSeconds = WhiteHiltConfig.BindAdminOnly(Section, "BreathSeconds", 10f, "Seconds between two breaths at most. Applies after a restart.",
            new AcceptableValueRange<float>(2f, 60f));
        FightsBosses = WhiteHiltConfig.BindAdminOnly(Section, "FightsBosses", false, "The White Dragon also fights bosses; off, it leaves them to you.");
        ArriveDistance = WhiteHiltConfig.BindAdminOnly(Section, "ArriveDistance", 80f, "Metres away, out of sight behind you, the dragon appears before it flies in.",
            new AcceptableValueRange<float>(30f, 200f));
        LeaveDistance = WhiteHiltConfig.BindAdminOnly(Section, "LeaveDistance", 100f, "If you get further than this from the dragon, in metres, it flies off.",
            new AcceptableValueRange<float>(40f, 300f));
        VanishDistance = WhiteHiltConfig.BindAdminOnly(Section, "VanishDistance", 90f, "On its way off, the dragon vanishes once no player is within this many metres.",
            new AcceptableValueRange<float>(40f, 300f));
        FlightSeconds = WhiteHiltConfig.BindAdminOnly(Section, "FlightSeconds", 30f, "Longest time, in seconds, the dragon takes to fly in or off.",
            new AcceptableValueRange<float>(5f, 120f));

        Translations.AddEnglishNameAndDescription(Translations.ItemKey(ItemName), "Dragon Horn",
            "A pale horn carved from the tooth of a mountain dragon. Its call brings a White Dragon to fight at your side for a while. Then the horn must rest a full day.");
        Translations.AddEnglish("whitehilt_dragonhorn_silent", "The horn is silent.");
        Translations.AddEnglish("whitehilt_dragonhorn_locked", "No dragon answers before Moder is slain.");
        Translations.AddEnglish("whitehilt_dragonhorn_here", "Your White Dragon is already at your side.");
        Translations.AddEnglish("whitehilt_dragonhorn_resting", "The White Dragon rests. Call again in {0} hours.");
        Translations.AddEnglish("whitehilt_dragonhorn_answer", "A White Dragon answers the horn!");
        Translations.AddEnglish("whitehilt_dragonhorn_nothing", "Nothing answers the horn.");
        PrefabManager.OnVanillaPrefabsAvailable += AddItem;
        PrefabManager.OnPrefabsRegistered += AddToTrader;
    }

    /// <summary>
    /// Applies changed settings to the Bog Witch's goods, also for Bog Witches already in the world.
    /// </summary>
    public static void ApplyConfig()
    {
        if (ZNetScene.instance == null)
        {
            return;
        }

        AddToTrader();
        foreach (Trader trader in UnityEngine.Object.FindObjectsByType<Trader>(FindObjectsSortMode.None))
        {
            if (Utils.GetPrefabName(trader.gameObject) == TraderPrefabName)
            {
                UpdateTrader(trader);
            }
        }
    }

    /// <summary>
    /// Why the local player cannot call the dragon now, or null if they can.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>The text to show, or null.</returns>
    public static string Problem(Player player)
    {
        if (!Enabled.Value)
        {
            return "$whitehilt_dragonhorn_silent";
        }

        string key = RequiredKey.Value?.Trim();
        if (!string.IsNullOrEmpty(key) && !ZoneSystem.instance.GetGlobalKey(key))
        {
            return "$whitehilt_dragonhorn_locked";
        }

        if (WhiteDragonAlly.HasDragon(player.GetPlayerID()))
        {
            return "$whitehilt_dragonhorn_here";
        }

        double left = RestingSeconds(player);
        return left > 0d
            ? string.Format(Localization.instance.Localize("$whitehilt_dragonhorn_resting"), Mathf.CeilToInt((float)(left / GameHourSeconds)))
            : null;
    }

    /// <summary>
    /// Answers a finished call of the local player's horn: the dragon is called and the horn rests.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Sounded(Player player)
    {
        string problem = Problem(player);
        if (problem != null)
        {
            player.Message(MessageHud.MessageType.Center, problem);
            return;
        }

        if (!WhiteDragonAlly.Summon(player))
        {
            player.Message(MessageHud.MessageType.Center, "$whitehilt_dragonhorn_nothing");
            return;
        }

        DateTime ready = ZNet.instance.GetTime().AddSeconds(CooldownHours.Value * GameHourSeconds);
        player.m_customData[ReadyKey] = ready.Ticks.ToString(CultureInfo.InvariantCulture);
        player.Message(MessageHud.MessageType.Center, "$whitehilt_dragonhorn_answer");
    }

    private static double RestingSeconds(Player player)
    {
        if (ZNet.instance == null || !player.m_customData.TryGetValue(ReadyKey, out string saved)
            || !long.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ready))
        {
            return 0d;
        }

        return (new DateTime(ready) - ZNet.instance.GetTime()).TotalSeconds;
    }

    // A clone of the celebration horn with pale trim; it has no recipe, as only the Bog Witch sells it.
    private static void AddItem()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddItem;
        try
        {
            CustomItem item = new(ItemName, "TankardAnniversary", new ItemConfig
            {
                Name = Translations.Token(Translations.ItemKey(ItemName)),
                Description = Translations.Token(Translations.ItemKey(ItemName) + "_description")
            });
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_ammoType = string.Empty;
            shared.m_useDurability = false;
            shared.m_maxStackSize = 1;
            shared.m_attack = new Attack();
            shared.m_secondaryAttack = new Attack();
            shared.m_startEffect = new EffectList();
            shared.m_triggerEffect = new EffectList();
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    VisualHelper.Recolor(item.ItemPrefab, PaleTrim);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = Enumerable.Repeat(icon, shared.m_icons.Length).ToArray();
                    }

                    HornCaller.LoadSound();
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"Dragon Horn appearance or audio: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
            Jotunn.Logger.LogInfo("Dragon Horn added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Dragon Horn failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Runs for every ZNetScene, so it must be idempotent.
    private static void AddToTrader()
    {
        Trader trader = PrefabManager.Instance.GetPrefab(TraderPrefabName)?.GetComponent<Trader>();
        if (trader == null)
        {
            Jotunn.Logger.LogWarning($"Dragon Horn: trader {TraderPrefabName} not found, the horn cannot be bought");
            return;
        }

        UpdateTrader(trader);
    }

    // Puts the horn among the Bog Witch's goods at the configured price and key, or takes it away.
    private static void UpdateTrader(Trader trader)
    {
        ItemDrop horn = PrefabManager.Instance.GetPrefab(ItemName)?.GetComponent<ItemDrop>();
        if (horn == null)
        {
            return;
        }

        Trader.TradeItem entry = trader.m_items.Find(candidate => candidate.m_prefab == horn);
        if (!Enabled.Value)
        {
            if (entry != null)
            {
                trader.m_items.Remove(entry);
            }

            return;
        }

        if (entry == null)
        {
            // StoreGui.BuySelectedItem plays these after every purchase without a null check.
            entry = new Trader.TradeItem { m_prefab = horn, m_stack = 1, m_buyPlayerEffects = new EffectList() };
            trader.m_items.Add(entry);
        }

        entry.m_price = Price.Value;
        entry.m_requiredGlobalKey = RequiredKey.Value?.Trim() ?? string.Empty;
    }

    // The celebration horn's coloured trim turned to the pale blue-white of the dragon; the horn keeps its colour.
    private static Color32 PaleTrim(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out _, out float saturation, out float value);
        if (saturation > 0.5f && value > 0.2f)
        {
            Color32 pale = Color.HSVToRGB(0.56f, 0.15f, Mathf.Clamp01(0.6f + value * 0.4f));
            pale.a = pixel.a;
            return pale;
        }

        return pixel;
    }
}
