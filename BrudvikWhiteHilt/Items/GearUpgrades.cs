using BepInEx.Configuration;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items;

/// <summary>
/// The kinds of White Hilt gear that are upgraded through the biome levels.
/// </summary>
public enum GearKind
{
    /// <summary>A helmet, chest or leg piece.</summary>
    Armor,

    /// <summary>A cape.</summary>
    Cape,

    /// <summary>A weapon.</summary>
    Weapon,

    /// <summary>A shield.</summary>
    Shield,
}

/// <summary>
/// Upgrades White Hilt armor, weapons and shields past the game's quality 4, one level per biome after the Swamp.
/// Each level costs that biome's material and adds armor, damage or block power, so the gear keeps up with the world.
/// </summary>
public static class GearUpgrades
{
    /// <summary>
    /// The highest quality the game itself gives gear; the levels above it are the biome levels.
    /// </summary>
    public const int VanillaMaxQuality = 4;

    private static readonly string[] Biomes = { "Mountain", "Plains", "Mistlands", "Ashlands" };
    private static readonly string[] BiomeCosts = { "Silver:10", "BlackMetal:10", "Carapace:10", "FlametalNew:10" };

    private static readonly Dictionary<string, Track> trackByPrefab = new();
    private static readonly Dictionary<string, GearKind> kindBySharedName = new();
    private static readonly Dictionary<Piece.Requirement, (int From, int To)> levels = new();
    private static readonly Dictionary<string, (Piece.Requirement[] Source, Piece.Requirement[] Upgrades, Piece.Requirement[] Result)> appended = new();

    private static Track armorTrack;
    private static Track weaponTrack;
    private static Track shieldTrack;
    private static ConfigEntry<float>[] biomeArmor;
    private static ConfigEntry<float> capeArmorPerBiome;
    private static ConfigEntry<float>[] biomeDamage;
    private static ConfigEntry<float>[] biomeBlock;

    /// <summary>
    /// Binds the upgrade costs and what the biome levels add, once.
    /// </summary>
    public static void BindConfig()
    {
        if (armorTrack != null)
        {
            return;
        }

        armorTrack = new Track("Gear.Armor", "armor", string.Empty);
        biomeArmor = new[]
        {
            WhiteHiltConfig.BindAdminOnly("Gear.Armor", "MountainArmor", 2f,
                "Armor a helmet, chest or leg piece gains at quality 5.", new AcceptableValueRange<float>(0f, 100f)),
            WhiteHiltConfig.BindAdminOnly("Gear.Armor", "PlainsArmor", 4f,
                "Armor a helmet, chest or leg piece gains at quality 6.", new AcceptableValueRange<float>(0f, 100f)),
            WhiteHiltConfig.BindAdminOnly("Gear.Armor", "MistlandsArmor", 6f,
                "Armor a helmet, chest or leg piece gains at quality 7.", new AcceptableValueRange<float>(0f, 100f)),
            WhiteHiltConfig.BindAdminOnly("Gear.Armor", "AshlandsArmor", 6f,
                "Armor a helmet, chest or leg piece gains at quality 8.", new AcceptableValueRange<float>(0f, 100f)),
        };
        capeArmorPerBiome = WhiteHiltConfig.BindAdminOnly("Gear.Armor", "CapeArmorPerBiome", 2f,
            "Armor a cape gains at each quality from 5 to 8.", new AcceptableValueRange<float>(0f, 100f));

        weaponTrack = new Track("Gear.Weapons", "the weapon", string.Empty);
        biomeDamage = BindShares("Gear.Weapons", "Damage", new[] { 0.1f, 0.3f, 0.35f, 0.3f },
            "Share of the weapon's base damage it gains at quality {0}, on each damage type it deals.");

        // Shields also take a Lindorm scale for every level.
        shieldTrack = new Track("Gear.Shields", "the shield", $"{MonsterRegistry.ScaleName}:1");
        biomeBlock = BindShares("Gear.Shields", "Block", new[] { 0.2f, 0.4f, 0.4f, 0.4f },
            "Share of the shield's base block power it gains at quality {0}.");
    }

    /// <summary>
    /// Makes a piece of gear upgradeable through the biome levels.
    /// </summary>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <param name="sharedName">The item's name token.</param>
    /// <param name="kind">What kind of gear it is.</param>
    public static void Register(string prefabName, string sharedName, GearKind kind)
    {
        BindConfig();
        trackByPrefab[prefabName] = kind switch
        {
            GearKind.Weapon => weaponTrack,
            GearKind.Shield => shieldTrack,
            _ => armorTrack,
        };
        kindBySharedName[sharedName] = kind;
    }

    /// <summary>
    /// The highest quality a kind of gear can be upgraded to: 4, plus one per biome level that has a cost.
    /// </summary>
    /// <param name="kind">The kind of gear.</param>
    /// <returns>The highest quality.</returns>
    public static int MaxQuality(GearKind kind)
    {
        BindConfig();
        return kind switch
        {
            GearKind.Weapon => weaponTrack.MaxQuality,
            GearKind.Shield => shieldTrack.MaxQuality,
            _ => armorTrack.MaxQuality,
        };
    }

    /// <summary>
    /// Adds the upgrade costs to a White Hilt gear recipe; its own requirements then only apply to crafting.
    /// </summary>
    /// <param name="prefabName">The recipe's item.</param>
    /// <param name="requirements">The crafting requirements.</param>
    /// <returns>The requirements with the upgrade costs, or the same array for other items.</returns>
    public static Piece.Requirement[] Append(string prefabName, Piece.Requirement[] requirements)
    {
        if (!trackByPrefab.TryGetValue(prefabName, out Track track) || requirements == null)
        {
            return requirements;
        }

        Piece.Requirement[] current = track.GetUpgrades();
        if (appended.TryGetValue(prefabName, out var cached) && cached.Source == requirements && cached.Upgrades == current)
        {
            return cached.Result;
        }

        foreach (Piece.Requirement requirement in requirements)
        {
            levels[requirement] = (1, 1);
        }

        Piece.Requirement[] result = requirements.Concat(current).ToArray();
        appended[prefabName] = (requirements, current, result);
        return result;
    }

    /// <summary>
    /// Gets what a requirement of a White Hilt gear recipe costs at a quality.
    /// </summary>
    /// <param name="requirement">The requirement.</param>
    /// <param name="quality">The quality crafted or upgraded to.</param>
    /// <param name="amount">The amount needed.</param>
    /// <returns>False for requirements of other recipes.</returns>
    public static bool TryGetAmount(Piece.Requirement requirement, int quality, out int amount)
    {
        if (!levels.TryGetValue(requirement, out var range))
        {
            amount = 0;
            return false;
        }

        int level = Mathf.Max(1, quality);
        amount = level < range.From || level > range.To ? 0 : level == 1 ? requirement.m_amount : requirement.m_amountPerLevel;
        return true;
    }

    /// <summary>
    /// The station level a recipe needs. The biome levels need the station level of quality 4, since their
    /// materials already gate them and no station goes high enough otherwise.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    /// <param name="quality">The quality crafted or upgraded to.</param>
    /// <returns>The station level.</returns>
    public static int RequiredStationLevel(Recipe recipe, int quality)
    {
        bool upgradeable = recipe.m_item != null && trackByPrefab.ContainsKey(recipe.m_item.name);
        return recipe.GetRequiredStationLevel(upgradeable ? Mathf.Min(quality, VanillaMaxQuality) : quality);
    }

    /// <summary>
    /// Corrects the game's armor of a White Hilt piece above quality 4 to the armor of its biome levels.
    /// </summary>
    /// <param name="shared">The item's shared data.</param>
    /// <param name="quality">The quality.</param>
    /// <returns>The armor to add to the game's value.</returns>
    public static float ArmorCorrection(ItemDrop.ItemData.SharedData shared, int quality)
    {
        if (quality <= VanillaMaxQuality || biomeArmor == null || !kindBySharedName.TryGetValue(shared.m_name, out GearKind kind)
            || kind is not (GearKind.Armor or GearKind.Cape))
        {
            return 0f;
        }

        int biomeLevels = Mathf.Min(quality - VanillaMaxQuality, biomeArmor.Length);
        float gained = kind == GearKind.Cape ? biomeLevels * capeArmorPerBiome.Value : biomeArmor.Take(biomeLevels).Sum(entry => entry.Value);
        return gained - (quality - VanillaMaxQuality) * shared.m_armorPerLevel;
    }

    /// <summary>
    /// Corrects the game's damage of a White Hilt weapon above quality 4 to the damage of its biome levels.
    /// </summary>
    /// <param name="shared">The item's shared data.</param>
    /// <param name="quality">The quality.</param>
    /// <param name="damages">The game's damage, corrected in place.</param>
    public static void CorrectDamage(ItemDrop.ItemData.SharedData shared, int quality, ref HitData.DamageTypes damages)
    {
        if (quality <= VanillaMaxQuality || biomeDamage == null || !kindBySharedName.TryGetValue(shared.m_name, out GearKind kind)
            || kind != GearKind.Weapon)
        {
            return;
        }

        int biomeLevels = Mathf.Min(quality - VanillaMaxQuality, biomeDamage.Length);
        float share = biomeDamage.Take(biomeLevels).Sum(entry => entry.Value);
        HitData.DamageTypes based = shared.m_damages;
        damages.m_damage += based.m_damage * share;
        damages.m_blunt += based.m_blunt * share;
        damages.m_slash += based.m_slash * share;
        damages.m_pierce += based.m_pierce * share;
        damages.m_fire += based.m_fire * share;
        damages.m_frost += based.m_frost * share;
        damages.m_lightning += based.m_lightning * share;
        damages.m_poison += based.m_poison * share;
        damages.m_spirit += based.m_spirit * share;
        damages.Add(shared.m_damagesPerLevel, -(quality - VanillaMaxQuality));
    }

    /// <summary>
    /// Corrects the game's block power of a White Hilt shield above quality 4 to the block power of its biome levels.
    /// </summary>
    /// <param name="shared">The item's shared data.</param>
    /// <param name="quality">The quality.</param>
    /// <returns>The block power to add to the game's value.</returns>
    public static float BlockCorrection(ItemDrop.ItemData.SharedData shared, int quality)
    {
        if (quality <= VanillaMaxQuality || biomeBlock == null || !kindBySharedName.TryGetValue(shared.m_name, out GearKind kind)
            || kind != GearKind.Shield)
        {
            return 0f;
        }

        int biomeLevels = Mathf.Min(quality - VanillaMaxQuality, biomeBlock.Length);
        float gained = shared.m_blockPower * biomeBlock.Take(biomeLevels).Sum(entry => entry.Value);
        return gained - (quality - VanillaMaxQuality) * shared.m_blockPowerPerLevel;
    }

    private static ConfigEntry<float>[] BindShares(string section, string suffix, float[] defaults, string description)
    {
        ConfigEntry<float>[] entries = new ConfigEntry<float>[Biomes.Length];
        for (int i = 0; i < Biomes.Length; i++)
        {
            entries[i] = WhiteHiltConfig.BindAdminOnly(section, Biomes[i] + suffix, defaults[i],
                string.Format(CultureInfo.InvariantCulture, description, VanillaMaxQuality + 1 + i), new AcceptableValueRange<float>(0f, 5f));
        }

        return entries;
    }

    private static string WithExtra(string cost, string extra)
    {
        return string.IsNullOrEmpty(extra) ? cost : $"{cost}, {extra}";
    }

    // Reads a configured list of upgrade materials ("Item:amount, ..."), skipping and logging entries that are not real
    // items or amounts.
    private static List<Piece.Requirement> Parse(string text, int from, int to)
    {
        List<Piece.Requirement> requirements = new();
        foreach (string part in (text ?? string.Empty).Split(','))
        {
            string[] fields = part.Split(':');
            string name = fields[0].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            ItemDrop material = ObjectDB.instance.GetItemPrefab(name)?.GetComponent<ItemDrop>();
            if (material == null || fields.Length != 2
                || !int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount <= 0)
            {
                Jotunn.Logger.LogWarning($"White Hilt gear upgrades: skipping \"{part.Trim()}\"");
                continue;
            }

            // The amount lives in m_amountPerLevel: with m_amount 0 the game does not need the material to discover the recipe.
            Piece.Requirement requirement = new() { m_resItem = material, m_amount = 0, m_amountPerLevel = amount, m_recover = false };
            levels[requirement] = (from, to);
            requirements.Add(requirement);
        }

        return requirements;
    }

    /// <summary>
    /// The upgrade costs of one kind of gear, in its own config section.
    /// </summary>
    private sealed class Track
    {
        private readonly ConfigEntry<string> swamp;
        private readonly ConfigEntry<string>[] biomes;
        private string upgradesText;
        private Piece.Requirement[] upgrades = new Piece.Requirement[0];

        public Track(string section, string what, string extra)
        {
            const string format = "A comma separated list of Prefab:Amount, e.g. \"Silver:10\".";
            swamp = WhiteHiltConfig.BindAdminOnly(section, "UpgradeSwamp", WithExtra("Iron:5", extra),
                $"Cost of each upgrade to quality 2, 3 and 4. {format}");
            biomes = new ConfigEntry<string>[Biomes.Length];
            for (int i = 0; i < Biomes.Length; i++)
            {
                int quality = VanillaMaxQuality + 1 + i;
                string empty = i == 0 ? $"Empty: {what} stops at quality {VanillaMaxQuality}." : $"Empty: {what} stops at the level before.";
                biomes[i] = WhiteHiltConfig.BindAdminOnly(section, "Upgrade" + Biomes[i], WithExtra(BiomeCosts[i], extra),
                    $"Cost of the upgrade to quality {quality}. {format} {empty}");
            }
        }

        public int MaxQuality => VanillaMaxQuality + biomes.TakeWhile(entry => !string.IsNullOrWhiteSpace(entry.Value)).Count();

        /// <summary>
        /// The extra upgrade requirements of this gear, parsed again only when the configured text changed. The swamp
        /// list covers the vanilla levels; each further biome adds one level above the vanilla maximum.
        /// </summary>
        public Piece.Requirement[] GetUpgrades()
        {
            if (ObjectDB.instance == null)
            {
                return upgrades;
            }

            string text = string.Join("|", new[] { swamp }.Concat(biomes).Select(entry => entry.Value));
            if (text == upgradesText)
            {
                return upgrades;
            }

            List<Piece.Requirement> list = Parse(swamp.Value, 2, VanillaMaxQuality);
            int quality = VanillaMaxQuality;
            foreach (ConfigEntry<string> entry in biomes.TakeWhile(entry => !string.IsNullOrWhiteSpace(entry.Value)))
            {
                quality++;
                list.AddRange(Parse(entry.Value, quality, quality));
            }

            upgradesText = text;
            upgrades = list.ToArray();
            return upgrades;
        }
    }
}
