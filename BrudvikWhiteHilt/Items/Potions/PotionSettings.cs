using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Items.Potions;

/// <summary>
/// Admin-only, server-synced numbers of the Gift potions, one config section per potion.
/// </summary>
public static class PotionSettings
{
    private const string SectionPrefix = "Potions.";

    private static bool initialized;

    /// <summary>
    /// Gift of Baldur settings.
    /// </summary>
    public static class Baldur
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Stealth modifier of the status effect.</summary>
        public static ConfigEntry<float> StealthModifier { get; internal set; }

        /// <summary>Noise modifier of the status effect.</summary>
        public static ConfigEntry<float> NoiseModifier { get; internal set; }

        /// <summary>Stealth factor the player is set to.</summary>
        public static ConfigEntry<float> Stealth { get; internal set; }

        /// <summary>Multiplier on sneak stamina usage.</summary>
        public static ConfigEntry<float> SneakStaminaMultiplier { get; internal set; }
    }

    /// <summary>
    /// Gift of Brokkr settings.
    /// </summary>
    public static class Brokkr
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Levels added to every skill.</summary>
        public static ConfigEntry<float> SkillBonus { get; internal set; }

        /// <summary>Highest skill level the bonus can reach.</summary>
        public static ConfigEntry<float> MaxSkillLevel { get; internal set; }

        /// <summary>Multiplier on building and tool stamina usage.</summary>
        public static ConfigEntry<float> HomeItemStaminaMultiplier { get; internal set; }
    }

    /// <summary>
    /// Gift of Fenrir settings.
    /// </summary>
    public static class Fenrir
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Animation speed multiplier while attacking.</summary>
        public static ConfigEntry<float> AttackSpeed { get; internal set; }

        /// <summary>Share of damage dealt returned as health.</summary>
        public static ConfigEntry<float> LifeSteal { get; internal set; }

        /// <summary>Movement speed modifier.</summary>
        public static ConfigEntry<float> SpeedModifier { get; internal set; }

        /// <summary>Multiplier on attack stamina usage.</summary>
        public static ConfigEntry<float> AttackStaminaMultiplier { get; internal set; }
    }

    /// <summary>
    /// Gift of Freya settings.
    /// </summary>
    public static class Freya
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Stamina added on top of a full refill when drunk.</summary>
        public static ConfigEntry<float> BonusStamina { get; internal set; }

        /// <summary>Stamina cost of every action.</summary>
        public static ConfigEntry<float> StaminaUse { get; internal set; }

        /// <summary>Stamina regeneration added.</summary>
        public static ConfigEntry<float> StaminaRegenBonus { get; internal set; }
    }

    /// <summary>
    /// Gift of Freyr settings.
    /// </summary>
    public static class Freyr
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Health regeneration multiplier.</summary>
        public static ConfigEntry<float> HealthRegenMultiplier { get; internal set; }

        /// <summary>Stamina regeneration multiplier.</summary>
        public static ConfigEntry<float> StaminaRegenMultiplier { get; internal set; }

        /// <summary>Extra carry weight.</summary>
        public static ConfigEntry<float> CarryWeight { get; internal set; }

        /// <summary>Multiplier on building and tool stamina usage.</summary>
        public static ConfigEntry<float> HomeItemStaminaMultiplier { get; internal set; }

        /// <summary>Health healed per second.</summary>
        public static ConfigEntry<float> HealPerSecond { get; internal set; }

        /// <summary>Stamina restored per second.</summary>
        public static ConfigEntry<float> StaminaPerSecond { get; internal set; }
    }

    /// <summary>
    /// Gift of Hel settings.
    /// </summary>
    public static class Hel
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Share of max health below which Hel heals the player.</summary>
        public static ConfigEntry<float> TriggerHealthFraction { get; internal set; }
    }

    /// <summary>
    /// Gift of Hugin settings.
    /// </summary>
    public static class Hugin
    {
        /// <summary>Level every skill is set to.</summary>
        public static ConfigEntry<float> SkillLevel { get; internal set; }
    }

    /// <summary>
    /// Gift of Idunn settings.
    /// </summary>
    public static class Idunn
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Health regeneration multiplier.</summary>
        public static ConfigEntry<float> HealthRegenMultiplier { get; internal set; }

        /// <summary>Stamina regeneration multiplier.</summary>
        public static ConfigEntry<float> StaminaRegenMultiplier { get; internal set; }

        /// <summary>Eitr regeneration multiplier.</summary>
        public static ConfigEntry<float> EitrRegenMultiplier { get; internal set; }

        /// <summary>Health healed per second.</summary>
        public static ConfigEntry<float> HealPerSecond { get; internal set; }
    }

    /// <summary>
    /// Gift of Loki settings.
    /// </summary>
    public static class Loki
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Eitr added when drunk.</summary>
        public static ConfigEntry<float> BonusEitr { get; internal set; }

        /// <summary>Eitr regeneration added.</summary>
        public static ConfigEntry<float> EitrRegenBonus { get; internal set; }
    }

    /// <summary>
    /// Gift of Njord settings.
    /// </summary>
    public static class Njord
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Swim speed modifier.</summary>
        public static ConfigEntry<float> SwimSpeedModifier { get; internal set; }

        /// <summary>Multiplier on swim stamina usage.</summary>
        public static ConfigEntry<float> SwimStaminaMultiplier { get; internal set; }

        /// <summary>Stamina below which a swimmer is topped up.</summary>
        public static ConfigEntry<float> MinSwimStamina { get; internal set; }

        /// <summary>Stamina added by a top-up.</summary>
        public static ConfigEntry<float> SwimStaminaRefill { get; internal set; }
    }

    /// <summary>
    /// Gift of Odin settings.
    /// </summary>
    public static class Odin
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Max health added.</summary>
        public static ConfigEntry<float> BonusMaxHealth { get; internal set; }

        /// <summary>Multiplier on fall damage.</summary>
        public static ConfigEntry<float> FallDamageMultiplier { get; internal set; }

        /// <summary>Health regeneration multiplier added.</summary>
        public static ConfigEntry<float> HealthRegenBonus { get; internal set; }

        /// <summary>Health healed per second.</summary>
        public static ConfigEntry<float> HealPerSecond { get; internal set; }
    }

    /// <summary>
    /// Gift of Ratatoskr settings.
    /// </summary>
    public static class Ratatoskr
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Movement speed modifier.</summary>
        public static ConfigEntry<float> SpeedModifier { get; internal set; }

        /// <summary>Run stamina drain modifier.</summary>
        public static ConfigEntry<float> RunStaminaDrainModifier { get; internal set; }

        /// <summary>Jump height modifier.</summary>
        public static ConfigEntry<float> JumpModifier { get; internal set; }

        /// <summary>Multiplier on sneak stamina usage.</summary>
        public static ConfigEntry<float> SneakStaminaMultiplier { get; internal set; }
    }

    /// <summary>
    /// Gift of Skadi settings.
    /// </summary>
    public static class Skadi
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }
    }

    /// <summary>
    /// Gift of Sleipnir settings.
    /// </summary>
    public static class Sleipnir
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Movement speed modifier.</summary>
        public static ConfigEntry<float> SpeedModifier { get; internal set; }

        /// <summary>Jump height modifier.</summary>
        public static ConfigEntry<float> JumpModifier { get; internal set; }

        /// <summary>Multiplier on fall damage.</summary>
        public static ConfigEntry<float> FallDamageMultiplier { get; internal set; }
    }

    /// <summary>
    /// Gift of Surt settings.
    /// </summary>
    public static class Surt
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }
    }

    /// <summary>
    /// Gift of Thor settings.
    /// </summary>
    public static class Thor
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Multiplier on building and tool stamina usage.</summary>
        public static ConfigEntry<float> HomeItemStaminaMultiplier { get; internal set; }

        /// <summary>Multiplier on attack stamina usage.</summary>
        public static ConfigEntry<float> AttackStaminaMultiplier { get; internal set; }

        /// <summary>Multiplier on chop damage.</summary>
        public static ConfigEntry<float> ChopDamageMultiplier { get; internal set; }

        /// <summary>Multiplier on pickaxe damage.</summary>
        public static ConfigEntry<float> PickaxeDamageMultiplier { get; internal set; }
    }

    /// <summary>
    /// Gift of Tyr settings.
    /// </summary>
    public static class Tyr
    {
        /// <summary>Duration in minutes.</summary>
        public static ConfigEntry<float> DurationMinutes { get; internal set; }

        /// <summary>Extra carry weight.</summary>
        public static ConfigEntry<float> CarryWeight { get; internal set; }

        /// <summary>Multiplier on block stamina usage.</summary>
        public static ConfigEntry<float> BlockStaminaMultiplier { get; internal set; }

        /// <summary>Multiplier on dodge stamina usage.</summary>
        public static ConfigEntry<float> DodgeStaminaMultiplier { get; internal set; }

        /// <summary>Multiplier on knockback taken.</summary>
        public static ConfigEntry<float> PushForceMultiplier { get; internal set; }
    }

    /// <summary>
    /// Binds every potion setting. Safe to call more than once; must run after <see cref="WhiteHiltConfig.Initialize"/>.
    /// </summary>
    public static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        const string baldur = "GiftOfBaldur";
        Baldur.DurationMinutes = BindDuration(baldur, 20f);
        Baldur.StealthModifier = Bind(baldur, "StealthModifier", -0.99f, -1f, 0f, "Stealth modifier; -1 makes you nearly impossible to detect.");
        Baldur.NoiseModifier = Bind(baldur, "NoiseModifier", -0.99f, -1f, 0f, "Noise modifier; -1 makes you nearly silent.");
        Baldur.Stealth = Bind(baldur, "Stealth", 0.01f, 0f, 1f, "Stealth factor while active; lower is harder to detect.");
        Baldur.SneakStaminaMultiplier = Bind(baldur, "SneakStaminaMultiplier", 0f, 0f, 1f, "Multiplier on sneak stamina usage.");

        const string brokkr = "GiftOfBrokkr";
        Brokkr.DurationMinutes = BindDuration(brokkr, 20f);
        Brokkr.SkillBonus = Bind(brokkr, "SkillBonus", 25f, 0f, 100f, "Levels added to every skill.");
        Brokkr.MaxSkillLevel = Bind(brokkr, "MaxSkillLevel", 100f, 0f, 100f, "Highest skill level the bonus can reach.");
        Brokkr.HomeItemStaminaMultiplier = Bind(brokkr, "HomeItemStaminaMultiplier", 0f, 0f, 1f, "Multiplier on building and tool stamina usage.");

        const string fenrir = "GiftOfFenrir";
        Fenrir.DurationMinutes = BindDuration(fenrir, 20f);
        Fenrir.AttackSpeed = Bind(fenrir, "AttackSpeed", 1.5f, 1f, 3f, "Animation speed multiplier while attacking.");
        Fenrir.LifeSteal = Bind(fenrir, "LifeSteal", 0.15f, 0f, 1f, "Share of damage dealt returned as health.");
        Fenrir.SpeedModifier = Bind(fenrir, "SpeedModifier", 0.25f, 0f, 2f, "Movement speed modifier; 0.25 is 25% faster.");
        Fenrir.AttackStaminaMultiplier = Bind(fenrir, "AttackStaminaMultiplier", 0.5f, 0f, 1f, "Multiplier on attack stamina usage.");

        const string freya = "GiftOfFreya";
        Freya.DurationMinutes = BindDuration(freya, 20f);
        Freya.BonusStamina = Bind(freya, "BonusStamina", 400f, 0f, 5000f, "Stamina added on top of a full refill when drunk.");
        Freya.StaminaUse = Bind(freya, "StaminaUse", -0.9f, -10f, 10f, "Stamina cost of running, jumping, attacking, blocking, dodging, swimming, building and sneaking; negative restores stamina.");
        Freya.StaminaRegenBonus = Bind(freya, "StaminaRegenBonus", 40f, 0f, 500f, "Stamina regeneration added.");

        const string freyr = "GiftOfFreyr";
        Freyr.DurationMinutes = BindDuration(freyr, 20f);
        Freyr.HealthRegenMultiplier = Bind(freyr, "HealthRegenMultiplier", 2f, 0f, 20f, "Health regeneration multiplier.");
        Freyr.StaminaRegenMultiplier = Bind(freyr, "StaminaRegenMultiplier", 2f, 0f, 20f, "Stamina regeneration multiplier.");
        Freyr.CarryWeight = Bind(freyr, "CarryWeight", 150f, 0f, 1000f, "Extra carry weight.");
        Freyr.HomeItemStaminaMultiplier = Bind(freyr, "HomeItemStaminaMultiplier", 0f, 0f, 1f, "Multiplier on building and tool stamina usage.");
        Freyr.HealPerSecond = Bind(freyr, "HealPerSecond", 1f, 0f, 100f, "Health healed per second.");
        Freyr.StaminaPerSecond = Bind(freyr, "StaminaPerSecond", 5f, 0f, 100f, "Stamina restored per second.");

        const string hel = "GiftOfHel";
        Hel.DurationMinutes = BindDuration(hel, 30f);
        Hel.TriggerHealthFraction = Bind(hel, "TriggerHealthFraction", 0.1f, 0f, 1f, "Share of max health below which Hel heals you to full.");

        const string hugin = "GiftOfHugin";
        Hugin.SkillLevel = Bind(hugin, "SkillLevel", 100f, 0f, 100f, "Level every skill is set to when drunk.");

        const string idunn = "GiftOfIdunn";
        Idunn.DurationMinutes = BindDuration(idunn, 20f);
        Idunn.HealthRegenMultiplier = Bind(idunn, "HealthRegenMultiplier", 1.25f, 0f, 20f, "Health regeneration multiplier.");
        Idunn.StaminaRegenMultiplier = Bind(idunn, "StaminaRegenMultiplier", 1.5f, 0f, 20f, "Stamina regeneration multiplier.");
        Idunn.EitrRegenMultiplier = Bind(idunn, "EitrRegenMultiplier", 1.5f, 0f, 20f, "Eitr regeneration multiplier.");
        Idunn.HealPerSecond = Bind(idunn, "HealPerSecond", 0f, 0f, 100f, "Health healed per second.");

        const string loki = "GiftOfLoki";
        Loki.DurationMinutes = BindDuration(loki, 20f);
        Loki.BonusEitr = Bind(loki, "BonusEitr", 500f, 0f, 5000f, "Eitr added when drunk.");
        Loki.EitrRegenBonus = Bind(loki, "EitrRegenBonus", 80f, 0f, 1000f, "Eitr regeneration added.");

        const string njord = "GiftOfNjord";
        Njord.DurationMinutes = BindDuration(njord, 20f);
        Njord.SwimSpeedModifier = Bind(njord, "SwimSpeedModifier", 1f, 0f, 5f, "Swim speed modifier; 1 is twice as fast.");
        Njord.SwimStaminaMultiplier = Bind(njord, "SwimStaminaMultiplier", 0f, 0f, 1f, "Multiplier on swim stamina usage.");
        Njord.MinSwimStamina = Bind(njord, "MinSwimStamina", 20f, 0f, 500f, "Stamina below which a swimmer is topped up.");
        Njord.SwimStaminaRefill = Bind(njord, "SwimStaminaRefill", 50f, 0f, 500f, "Stamina added by a top-up.");

        const string odin = "GiftOfOdin";
        Odin.DurationMinutes = BindDuration(odin, 10f);
        Odin.BonusMaxHealth = Bind(odin, "BonusMaxHealth", 50f, 0f, 1000f, "Max health added while active.");
        Odin.FallDamageMultiplier = Bind(odin, "FallDamageMultiplier", 0.5f, 0f, 1f, "Multiplier on fall damage.");
        Odin.HealthRegenBonus = Bind(odin, "HealthRegenBonus", 1f, 0f, 200f, "Health regeneration multiplier added; 1 doubles it.");
        Odin.HealPerSecond = Bind(odin, "HealPerSecond", 2f, 0f, 100f, "Health healed per second.");

        const string ratatoskr = "GiftOfRatatoskr";
        Ratatoskr.DurationMinutes = BindDuration(ratatoskr, 20f);
        Ratatoskr.SpeedModifier = Bind(ratatoskr, "SpeedModifier", 0.75f, 0f, 3f, "Movement speed modifier; 0.75 is 75% faster.");
        Ratatoskr.RunStaminaDrainModifier = Bind(ratatoskr, "RunStaminaDrainModifier", -0.8f, -1f, 1f, "Run stamina drain modifier; -0.8 is 80% less.");
        Ratatoskr.JumpModifier = Bind(ratatoskr, "JumpModifier", 0.5f, 0f, 5f, "Jump height modifier.");
        Ratatoskr.SneakStaminaMultiplier = Bind(ratatoskr, "SneakStaminaMultiplier", 0f, 0f, 1f, "Multiplier on sneak stamina usage.");

        Skadi.DurationMinutes = BindDuration("GiftOfSkadi", 20f);

        const string sleipnir = "GiftOfSleipnir";
        Sleipnir.DurationMinutes = BindDuration(sleipnir, 20f);
        Sleipnir.SpeedModifier = Bind(sleipnir, "SpeedModifier", 0.5f, 0f, 3f, "Movement speed modifier; 0.5 is 50% faster.");
        Sleipnir.JumpModifier = Bind(sleipnir, "JumpModifier", 1.5f, 0f, 5f, "Jump height modifier.");
        Sleipnir.FallDamageMultiplier = Bind(sleipnir, "FallDamageMultiplier", 0f, 0f, 1f, "Multiplier on fall damage.");

        Surt.DurationMinutes = BindDuration("GiftOfSurt", 20f);

        const string thor = "GiftOfThor";
        Thor.DurationMinutes = BindDuration(thor, 20f);
        Thor.HomeItemStaminaMultiplier = Bind(thor, "HomeItemStaminaMultiplier", 0.1f, 0f, 1f, "Multiplier on building and tool stamina usage.");
        Thor.AttackStaminaMultiplier = Bind(thor, "AttackStaminaMultiplier", 0.5f, 0f, 1f, "Multiplier on attack stamina usage.");
        Thor.ChopDamageMultiplier = Bind(thor, "ChopDamageMultiplier", 2f, 1f, 10f, "Multiplier on chop damage.");
        Thor.PickaxeDamageMultiplier = Bind(thor, "PickaxeDamageMultiplier", 2f, 1f, 10f, "Multiplier on pickaxe damage.");
        Helpers.Translations.AddDynamic("se_giftofthor_tooltip", () => new object[]
        {
            Helpers.Translations.Number(Thor.ChopDamageMultiplier.Value),
            Helpers.Translations.Number(Thor.PickaxeDamageMultiplier.Value)
        });

        const string tyr = "GiftOfTyr";
        Tyr.DurationMinutes = BindDuration(tyr, 20f);
        Tyr.CarryWeight = Bind(tyr, "CarryWeight", 100f, 0f, 1000f, "Extra carry weight.");
        Tyr.BlockStaminaMultiplier = Bind(tyr, "BlockStaminaMultiplier", 0f, 0f, 1f, "Multiplier on block stamina usage.");
        Tyr.DodgeStaminaMultiplier = Bind(tyr, "DodgeStaminaMultiplier", 0.25f, 0f, 1f, "Multiplier on dodge stamina usage.");
        Tyr.PushForceMultiplier = Bind(tyr, "PushForceMultiplier", 0.1f, 0f, 1f, "Multiplier on knockback taken.");
    }

    /// <summary>
    /// Binds the duration of a potion, for potions that keep their settings in their own class.
    /// </summary>
    /// <param name="potion">Base name of the potion, e.g. GiftOfEir.</param>
    /// <param name="defaultMinutes">Default duration.</param>
    /// <returns>The entry.</returns>
    internal static ConfigEntry<float> BindDuration(string potion, float defaultMinutes)
    {
        return Bind(potion, "DurationMinutes", defaultMinutes, 1f, 240f, "Duration in minutes.");
    }

    /// <summary>
    /// Binds a number of a potion in its section, for potions that keep their settings in their own class.
    /// </summary>
    /// <param name="potion">Base name of the potion, e.g. GiftOfEir.</param>
    /// <param name="key">Setting name.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <param name="min">Lowest value.</param>
    /// <param name="max">Highest value.</param>
    /// <param name="description">English description.</param>
    /// <returns>The entry.</returns>
    internal static ConfigEntry<float> Bind(string potion, string key, float defaultValue, float min, float max, string description)
    {
        WhiteHiltConfig.SetSectionLabel(SectionPrefix + potion, Helpers.Translations.Token(Helpers.Translations.ItemKey(potion + "Mead")));
        return WhiteHiltConfig.BindAdminOnly(SectionPrefix + potion, key, defaultValue, description, new AcceptableValueRange<float>(min, max));
    }
}
