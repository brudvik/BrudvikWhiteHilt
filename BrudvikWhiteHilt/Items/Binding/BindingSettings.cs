using BepInEx.Configuration;
using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BrudvikWhiteHilt.Items.Binding;

/// <summary>
/// Config of the trophy binding and the rune infusions, sections "Gear.Binding" and "Gear.Infusions". Server-synced.
/// </summary>
public static class BindingSettings
{
    private const string BindingSection = "Gear.Binding";
    private const string InfusionSection = "Gear.Infusions";

    private static readonly Dictionary<string, (float Bonus, float Infusion)> defaults = new()
    {
        ["BlackTroll"] = (0.05f, 0.10f),
        ["BlackAbomination"] = (0.08f, 0.12f),
        ["BlackSerpent"] = (0.08f, 0.12f),
        ["BlackStoneGolem"] = (0.10f, 0.15f),
        ["BlackDragon"] = (0.10f, 0.15f),
        ["BlackFulingBerserker"] = (0.12f, 0.18f),
        ["BlackSeekerSoldier"] = (0.15f, 0.21f),
        ["BlackMorgen"] = (0.20f, 0.25f),
        ["BlackBonemaw"] = (0.20f, 0.25f),
    };

    private static readonly Dictionary<string, ConfigEntry<float>> bonus = new();
    private static readonly Dictionary<string, ConfigEntry<float>> infusionStrength = new();
    private static readonly Dictionary<InfusionKind, ConfigEntry<string>> costs = new();

    /// <summary>Share of the infusion strength that the Grip of the Deep gives back as health.</summary>
    public static ConfigEntry<float> DeepLifeStealShare { get; private set; }

    /// <summary>How many times stronger Wolfsbane's poison is against beasts.</summary>
    public static ConfigEntry<float> WolfsbaneBeastMultiplier { get; private set; }

    /// <summary>Creatures (prefab names, comma separated) that Wolfsbane counts as beasts.</summary>
    public static ConfigEntry<string> WolfsbaneBeasts { get; private set; }

    /// <summary>Chance that a hit of a weapon etched with Dread sends the target running.</summary>
    public static ConfigEntry<float> DreadChance { get; private set; }

    /// <summary>How long a creature runs in Dread, in seconds.</summary>
    public static ConfigEntry<float> DreadSeconds { get; private set; }

    /// <summary>Extra damage of a weapon etched with the Berserker's Rage when its wielder is near death.</summary>
    public static ConfigEntry<float> BerserkerMaxBonus { get; private set; }

    /// <summary>Lightning damage of Rowan's Ward, as a share of the shield's base block power.</summary>
    public static ConfigEntry<float> WardShare { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        if (DeepLifeStealShare != null)
        {
            return;
        }

        foreach (BeastDefinition beast in BeastDefinition.All)
        {
            (float bonusDefault, float infusionDefault) = defaults.TryGetValue(beast.Key, out var values) ? values : (0.1f, 0.15f);
            bonus[beast.Key] = WhiteHiltConfig.BindAdminOnly(BindingSection, $"{beast.Key}Bonus", bonusDefault,
                $"Extra damage (block power for a shield) of a White Hilt weapon bound with the {beast.EnglishName} trophy (0.2 = 20%).",
                new AcceptableValueRange<float>(0f, 2f));
            infusionStrength[beast.Key] = WhiteHiltConfig.BindAdminOnly(BindingSection, $"{beast.Key}Infusion", infusionDefault,
                $"Strength of a rune etched into a weapon bound with the {beast.EnglishName} trophy, as a share of the weapon's base damage (0.25 = 25%).",
                new AcceptableValueRange<float>(0f, 2f));
        }

        foreach (Infusion infusion in Infusion.All)
        {
            costs[infusion.Kind] = WhiteHiltConfig.BindAdminOnly(InfusionSection, $"{infusion.Kind}Cost", infusion.DefaultCost,
                $"What etching {infusion.EnglishName} takes besides its rune: Prefab:Amount, comma separated. Empty: only the rune.");
        }

        DeepLifeStealShare = WhiteHiltConfig.BindAdminOnly(InfusionSection, "DeepLifeStealShare", 0.5f,
            "Share of the infusion strength the Grip of the Deep gives back as health of the damage dealt (0.5 with a strength of 25% = 12.5%).",
            new AcceptableValueRange<float>(0f, 4f));
        WolfsbaneBeastMultiplier = WhiteHiltConfig.BindAdminOnly(InfusionSection, "WolfsbaneBeastMultiplier", 3f,
            "How many times stronger Wolfsbane's poison is against beasts.", new AcceptableValueRange<float>(1f, 10f));
        WolfsbaneBeasts = WhiteHiltConfig.BindAdminOnly(InfusionSection, "WolfsbaneBeasts",
            "Wolf,Wolf_cub,Fenring,Fenring_Cultist,Ulv,Bjorn,Unbjorn,Boar,Deer,Lox,Hare,Asksvin",
            "Creatures (prefab names, comma separated) that Wolfsbane counts as beasts.");
        DreadChance = WhiteHiltConfig.BindAdminOnly(InfusionSection, "DreadChance", 0.25f,
            "Chance that a hit of a weapon etched with Dread sends the target running (0.25 = 25%). Never bosses.",
            new AcceptableValueRange<float>(0f, 1f));
        DreadSeconds = WhiteHiltConfig.BindAdminOnly(InfusionSection, "DreadSeconds", 4f,
            "How long a creature runs in Dread, in seconds.", new AcceptableValueRange<float>(1f, 30f));
        BerserkerMaxBonus = WhiteHiltConfig.BindAdminOnly(InfusionSection, "BerserkerMaxBonus", 0.6f,
            "Extra damage of a weapon etched with the Berserker's Rage when its wielder is near death; it grows with the health lost (0.6 = 60%, 30% at half health).",
            new AcceptableValueRange<float>(0f, 3f));
        WardShare = WhiteHiltConfig.BindAdminOnly(InfusionSection, "WardShare", 0.5f,
            "Lightning damage dealt by Rowan's Ward to a foe whose blow is blocked, as a share of the shield's base block power (0.5 = half).",
            new AcceptableValueRange<float>(0f, 3f));
    }

    /// <summary>
    /// Extra damage or block power of gear bound with a beast's trophy.
    /// </summary>
    /// <param name="beast">The beast.</param>
    /// <returns>The share, e.g. 0.2.</returns>
    public static float Bonus(BeastDefinition beast)
    {
        return bonus.TryGetValue(beast.Key, out ConfigEntry<float> entry) ? entry.Value : 0f;
    }

    /// <summary>
    /// Strength of an infusion on gear bound with a beast's trophy, as a share of the base damage.
    /// </summary>
    /// <param name="beast">The beast.</param>
    /// <returns>The share, e.g. 0.25.</returns>
    public static float InfusionStrength(BeastDefinition beast)
    {
        return infusionStrength.TryGetValue(beast.Key, out ConfigEntry<float> entry) ? entry.Value : 0f;
    }

    /// <summary>
    /// What etching an infusion costs besides its rune, for the items that exist.
    /// </summary>
    /// <param name="kind">The infusion.</param>
    /// <returns>Shared name and amount of each item.</returns>
    public static List<(string SharedName, int Amount)> Cost(InfusionKind kind)
    {
        List<(string, int)> result = new();
        if (!costs.TryGetValue(kind, out ConfigEntry<string> entry) || ObjectDB.instance == null)
        {
            return result;
        }

        foreach (string part in (entry.Value ?? string.Empty).Split(',').Select(part => part.Trim()).Where(part => part.Length > 0))
        {
            string[] fields = part.Split(':');
            if (fields.Length != 2 || !int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount <= 0)
            {
                continue;
            }

            ItemDrop item = ObjectDB.instance.GetItemPrefab(fields[0].Trim())?.GetComponent<ItemDrop>();
            if (item != null)
            {
                result.Add((item.m_itemData.m_shared.m_name, amount));
            }
        }

        return result;
    }
}
