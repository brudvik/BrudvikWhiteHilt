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
/// The White Dragon's breath.
/// </summary>
public enum DragonBreath
{
    /// <summary>Fire, like the Desert Dragon.</summary>
    Fire,

    /// <summary>Frost shards that slow what they hit.</summary>
    Frost,

    /// <summary>Fire and frost by turns.</summary>
    Both
}

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

    /// <summary>Damage of the blast where a breath first strikes, half fire and half blunt, before stars.</summary>
    public static ConfigEntry<float> BlastDamage { get; private set; }

    /// <summary>Radius of the blast in metres.</summary>
    public static ConfigEntry<float> BlastRadius { get; private set; }

    /// <summary>How hard the blast throws enemies back.</summary>
    public static ConfigEntry<float> BlastPush { get; private set; }

    /// <summary>Size of the White Dragon, as a multiple of a Desert Dragon's.</summary>
    public static ConfigEntry<float> Size { get; private set; }

    /// <summary>How much louder the White Dragon roars than a Desert Dragon, and how much further it is heard.</summary>
    public static ConfigEntry<float> RoarLoudness { get; private set; }

    /// <summary>Which breath the White Dragon uses.</summary>
    public static ConfigEntry<DragonBreath> Breath { get; private set; }

    /// <summary>Frost damage of each shard of its frost breath before stars.</summary>
    public static ConfigEntry<float> FrostDamage { get; private set; }

    /// <summary>Seconds between two dives at most.</summary>
    public static ConfigEntry<float> DiveSeconds { get; private set; }

    /// <summary>Damage of the slam at the end of a dive, before stars; 0 means no dives.</summary>
    public static ConfigEntry<float> DiveDamage { get; private set; }

    /// <summary>Radius of the slam in metres.</summary>
    public static ConfigEntry<float> DiveRadius { get; private set; }

    /// <summary>How hard the slam throws enemies back.</summary>
    public static ConfigEntry<float> DivePush { get; private set; }

    /// <summary>How much faster than its normal flight the dragon dives.</summary>
    public static ConfigEntry<float> DiveSpeed { get; private set; }

    /// <summary>How far away, in metres, the dragon dives at a foe.</summary>
    public static ConfigEntry<float> DiveRange { get; private set; }

    /// <summary>Seconds between two terror roars at most; 0 means none.</summary>
    public static ConfigEntry<float> RoarSeconds { get; private set; }

    /// <summary>Radius of the terror roar in metres.</summary>
    public static ConfigEntry<float> RoarRadius { get; private set; }

    /// <summary>How much slower the roar leaves enemies, as a share of their speed.</summary>
    public static ConfigEntry<float> RoarSlow { get; private set; }

    /// <summary>Seconds the roar slows enemies.</summary>
    public static ConfigEntry<float> RoarSlowSeconds { get; private set; }

    /// <summary>Seconds the dragon keeps after something that hit its caller; 0 means it picks its own foes.</summary>
    public static ConfigEntry<float> GuardSeconds { get; private set; }

    /// <summary>The caller is blessed within this many metres of the dragon; 0 means no blessing.</summary>
    public static ConfigEntry<float> BlessingRange { get; private set; }

    /// <summary>How the blessed caller takes blunt, slash and pierce damage.</summary>
    public static ConfigEntry<HitData.DamageModifier> BlessingResistance { get; private set; }

    /// <summary>Health and stamina regeneration of the blessed caller, as a multiple.</summary>
    public static ConfigEntry<float> BlessingRegen { get; private set; }

    /// <summary>Global keys that each make the dragon stronger.</summary>
    public static ConfigEntry<string> PowerKeys { get; private set; }

    /// <summary>Extra health and damage for each of <see cref="PowerKeys"/> set, as a share.</summary>
    public static ConfigEntry<float> PowerPerKey { get; private set; }

    /// <summary>Whether a cold light and frost mist surround the dragon.</summary>
    public static ConfigEntry<bool> Aura { get; private set; }

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
        Health = WhiteHiltConfig.BindAdminOnly(Section, "Health", 1500f, "Health of the White Dragon before stars. Applies after a restart.",
            new AcceptableValueRange<float>(10f, 20000f));
        FireDamage = WhiteHiltConfig.BindAdminOnly(Section, "FireDamage", 25f, "Fire damage of each flame in its breath before stars. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 200f));
        Flames = WhiteHiltConfig.BindAdminOnly(Section, "Flames", 12, "Flames in one breath. Applies after a restart.", new AcceptableValueRange<int>(1, 30));
        BreathSeconds = WhiteHiltConfig.BindAdminOnly(Section, "BreathSeconds", 5f, "Seconds between two breaths at most. Applies after a restart.",
            new AcceptableValueRange<float>(2f, 60f));
        BlastDamage = WhiteHiltConfig.BindAdminOnly(Section, "BlastDamage", 450f,
            "Damage of the blast where each breath first strikes, half the breath's fire or frost and half blunt, before stars. It falls to half at the edge. 0 turns the blast off.",
            new AcceptableValueRange<float>(0f, 2000f));
        BlastRadius = WhiteHiltConfig.BindAdminOnly(Section, "BlastRadius", 7f, "Radius of the blast in metres.", new AcceptableValueRange<float>(1f, 20f));
        BlastPush = WhiteHiltConfig.BindAdminOnly(Section, "BlastPush", 80f, "How hard the blast throws enemies back.", new AcceptableValueRange<float>(0f, 300f));
        Size = WhiteHiltConfig.BindAdminOnly(Section, "Size", 1.5f, "Size of the White Dragon, as a multiple of a Desert Dragon's. Applies after a restart.",
            new AcceptableValueRange<float>(0.5f, 3f));
        RoarLoudness = WhiteHiltConfig.BindAdminOnly(Section, "RoarLoudness", 2f,
            "How much louder the White Dragon roars than a Desert Dragon, and how many times further it is heard. 1 = as loud. Applies after a restart.",
            new AcceptableValueRange<float>(1f, 4f));
        Breath = WhiteHiltConfig.BindAdminOnly(Section, "Breath", DragonBreath.Both,
            "Which breath the White Dragon uses: Fire, Frost, or Both by turns. Frost slows what it hits and bites on fire-proof foes. Applies after a restart.");
        FrostDamage = WhiteHiltConfig.BindAdminOnly(Section, "FrostDamage", 25f, "Frost damage of each shard of its frost breath before stars. Applies after a restart.",
            new AcceptableValueRange<float>(0f, 200f));
        DiveSeconds = WhiteHiltConfig.BindAdminOnly(Section, "DiveSeconds", 20f, "Seconds between two dives at a foe.", new AcceptableValueRange<float>(5f, 120f));
        DiveDamage = WhiteHiltConfig.BindAdminOnly(Section, "DiveDamage", 600f,
            "Damage of the slam when the dragon dives into the ground by a foe, half its breath's element and half blunt, before stars. It falls to half at the edge. 0 = no dives.",
            new AcceptableValueRange<float>(0f, 5000f));
        DiveRadius = WhiteHiltConfig.BindAdminOnly(Section, "DiveRadius", 10f, "Radius of the slam in metres.", new AcceptableValueRange<float>(2f, 25f));
        DivePush = WhiteHiltConfig.BindAdminOnly(Section, "DivePush", 150f, "How hard the slam throws enemies back.", new AcceptableValueRange<float>(0f, 400f));
        DiveSpeed = WhiteHiltConfig.BindAdminOnly(Section, "DiveSpeed", 1.8f, "How many times faster than its normal flight the dragon dives.",
            new AcceptableValueRange<float>(1f, 4f));
        DiveRange = WhiteHiltConfig.BindAdminOnly(Section, "DiveRange", 40f, "How far away, in metres, a foe may be for the dragon to dive at it.",
            new AcceptableValueRange<float>(10f, 100f));
        RoarSeconds = WhiteHiltConfig.BindAdminOnly(Section, "RoarSeconds", 30f,
            "Seconds between two terror roars while foes are near. It also roars when it arrives. 0 = no terror roar.", new AcceptableValueRange<float>(0f, 300f));
        RoarRadius = WhiteHiltConfig.BindAdminOnly(Section, "RoarRadius", 20f, "Metres around the dragon its roar staggers and slows foes.",
            new AcceptableValueRange<float>(5f, 60f));
        RoarSlow = WhiteHiltConfig.BindAdminOnly(Section, "RoarSlow", 0.6f, "How much slower the roar leaves foes (0.6 = 60% slower).",
            new AcceptableValueRange<float>(0f, 0.9f));
        RoarSlowSeconds = WhiteHiltConfig.BindAdminOnly(Section, "RoarSlowSeconds", 4f, "Seconds the roar slows foes.", new AcceptableValueRange<float>(0f, 20f));
        GuardSeconds = WhiteHiltConfig.BindAdminOnly(Section, "GuardSeconds", 10f,
            "Seconds the dragon goes after whatever hit you, before it picks its own foes again. 0 = it does not guard you.",
            new AcceptableValueRange<float>(0f, 60f));
        BlessingRange = WhiteHiltConfig.BindAdminOnly(Section, "BlessingRange", 30f,
            "While you are within this many metres of your dragon, it blesses you. 0 = no blessing.", new AcceptableValueRange<float>(0f, 100f));
        BlessingResistance = WhiteHiltConfig.BindAdminOnly(Section, "BlessingResistance", HitData.DamageModifier.SlightlyResistant,
            "How you take blunt, slash and pierce damage while blessed.");
        BlessingRegen = WhiteHiltConfig.BindAdminOnly(Section, "BlessingRegen", 1.5f, "Health and stamina regeneration while blessed, as a multiple.",
            new AcceptableValueRange<float>(1f, 3f));
        PowerKeys = WhiteHiltConfig.BindAdminOnly(Section, "PowerKeys", "defeated_goblinking,defeated_queen,defeated_fader",
            "Comma-separated global keys that each make the dragon stronger when it is called (Yagluth, the Queen, Fader).");
        PowerPerKey = WhiteHiltConfig.BindAdminOnly(Section, "PowerPerKey", 0.25f,
            "Extra health and damage for each of PowerKeys that is set (0.25 = +25% each).", new AcceptableValueRange<float>(0f, 2f));
        Aura = WhiteHiltConfig.BindAdminOnly(Section, "Aura", true, "A cold light and frost mist surround the dragon. Applies after a restart.");
        FightsBosses = WhiteHiltConfig.BindAdminOnly(Section, "FightsBosses", true, "The White Dragon also fights bosses; off, it leaves them to you.");
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
    /// Applies changed settings to the Bog Witch's goods, also for Bog Witches already in the world, and to the White
    /// Dragon's blessing and roar.
    /// </summary>
    public static void ApplyConfig()
    {
        WhiteDragonRegistry.ApplyConfig();
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
