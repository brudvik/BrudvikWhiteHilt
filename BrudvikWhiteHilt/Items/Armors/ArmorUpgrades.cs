using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Armors;

/// <summary>
/// Upgrades White Hilt armor past the game's quality 4, one level per biome after the Swamp. Each level costs that
/// biome's material and adds armor, so the gear keeps up with the world.
/// </summary>
public static class ArmorUpgrades
{
    /// <summary>
    /// The highest quality the game itself gives armor; the levels above it are the biome levels.
    /// </summary>
    public const int VanillaMaxQuality = 4;

    private const string Section = "Gear.Armor";

    private static readonly Dictionary<string, bool> capeByPrefab = new();
    private static readonly Dictionary<string, bool> capeBySharedName = new();
    private static readonly Dictionary<Piece.Requirement, (int From, int To)> levels = new();
    private static readonly Dictionary<string, (Piece.Requirement[] Source, Piece.Requirement[] Upgrades, Piece.Requirement[] Result)> appended = new();

    private static ConfigEntry<string> swampUpgrade;
    private static ConfigEntry<string>[] biomeUpgrades;
    private static ConfigEntry<float>[] biomeArmor;
    private static ConfigEntry<float> capeArmorPerBiome;
    private static string upgradesText;
    private static Piece.Requirement[] upgrades = new Piece.Requirement[0];

    /// <summary>
    /// The highest quality White Hilt armor can be upgraded to: 4, plus one per biome level that has a cost.
    /// </summary>
    public static int MaxQuality
    {
        get
        {
            int count = biomeUpgrades?.TakeWhile(entry => !string.IsNullOrWhiteSpace(entry.Value)).Count() ?? 0;
            return VanillaMaxQuality + count;
        }
    }

    /// <summary>
    /// Binds the upgrade costs and the armor of the biome levels, once.
    /// </summary>
    public static void BindConfig()
    {
        if (swampUpgrade != null)
        {
            return;
        }

        const string format = "A comma separated list of Prefab:Amount, e.g. \"Silver:10\".";
        swampUpgrade = WhiteHiltConfig.BindAdminOnly(Section, "UpgradeSwamp", "Iron:5",
            $"Cost of each upgrade to quality 2, 3 and 4. {format}");
        biomeUpgrades = new[]
        {
            WhiteHiltConfig.BindAdminOnly(Section, "UpgradeMountain", "Silver:10",
                $"Cost of the upgrade to quality 5. {format} Empty: armor stops at quality 4."),
            WhiteHiltConfig.BindAdminOnly(Section, "UpgradePlains", "BlackMetal:10",
                $"Cost of the upgrade to quality 6. {format} Empty: armor stops at the level before."),
            WhiteHiltConfig.BindAdminOnly(Section, "UpgradeMistlands", "Carapace:10",
                $"Cost of the upgrade to quality 7. {format} Empty: armor stops at the level before."),
            WhiteHiltConfig.BindAdminOnly(Section, "UpgradeAshlands", "FlametalNew:10",
                $"Cost of the upgrade to quality 8. {format} Empty: armor stops at the level before."),
        };
        biomeArmor = new[]
        {
            WhiteHiltConfig.BindAdminOnly(Section, "MountainArmor", 2f,
                "Armor a helmet, chest or leg piece gains at quality 5.", new AcceptableValueRange<float>(0f, 100f)),
            WhiteHiltConfig.BindAdminOnly(Section, "PlainsArmor", 4f,
                "Armor a helmet, chest or leg piece gains at quality 6.", new AcceptableValueRange<float>(0f, 100f)),
            WhiteHiltConfig.BindAdminOnly(Section, "MistlandsArmor", 6f,
                "Armor a helmet, chest or leg piece gains at quality 7.", new AcceptableValueRange<float>(0f, 100f)),
            WhiteHiltConfig.BindAdminOnly(Section, "AshlandsArmor", 6f,
                "Armor a helmet, chest or leg piece gains at quality 8.", new AcceptableValueRange<float>(0f, 100f)),
        };
        capeArmorPerBiome = WhiteHiltConfig.BindAdminOnly(Section, "CapeArmorPerBiome", 2f,
            "Armor a cape gains at each quality from 5 to 8.", new AcceptableValueRange<float>(0f, 100f));
    }

    /// <summary>
    /// Makes an armor piece upgradeable through the biome levels.
    /// </summary>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <param name="sharedName">The item's name token.</param>
    /// <param name="cape">True for a cape.</param>
    public static void Register(string prefabName, string sharedName, bool cape)
    {
        capeByPrefab[prefabName] = cape;
        capeBySharedName[sharedName] = cape;
    }

    /// <summary>
    /// Adds the upgrade costs to a White Hilt armor recipe; its own requirements then only apply to crafting.
    /// </summary>
    /// <param name="prefabName">The recipe's item.</param>
    /// <param name="requirements">The crafting requirements.</param>
    /// <returns>The requirements with the upgrade costs, or the same array for other items.</returns>
    public static Piece.Requirement[] Append(string prefabName, Piece.Requirement[] requirements)
    {
        if (!capeByPrefab.ContainsKey(prefabName) || requirements == null)
        {
            return requirements;
        }

        Piece.Requirement[] current = GetUpgrades();
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
    /// Gets what a requirement of a White Hilt armor recipe costs at a quality.
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
        bool upgradeable = recipe.m_item != null && capeByPrefab.ContainsKey(recipe.m_item.name);
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
        if (quality <= VanillaMaxQuality || biomeArmor == null || !capeBySharedName.TryGetValue(shared.m_name, out bool cape))
        {
            return 0f;
        }

        int biomeLevels = Mathf.Min(quality - VanillaMaxQuality, biomeArmor.Length);
        float gained = cape ? biomeLevels * capeArmorPerBiome.Value : biomeArmor.Take(biomeLevels).Sum(entry => entry.Value);
        return gained - (quality - VanillaMaxQuality) * shared.m_armorPerLevel;
    }

    private static Piece.Requirement[] GetUpgrades()
    {
        if (swampUpgrade == null || ObjectDB.instance == null)
        {
            return upgrades;
        }

        string text = string.Join("|", new[] { swampUpgrade }.Concat(biomeUpgrades).Select(entry => entry.Value));
        if (text == upgradesText)
        {
            return upgrades;
        }

        List<Piece.Requirement> list = Parse(swampUpgrade.Value, 2, VanillaMaxQuality);
        int quality = VanillaMaxQuality;
        foreach (ConfigEntry<string> entry in biomeUpgrades.TakeWhile(entry => !string.IsNullOrWhiteSpace(entry.Value)))
        {
            quality++;
            list.AddRange(Parse(entry.Value, quality, quality));
        }

        upgradesText = text;
        upgrades = list.ToArray();
        return upgrades;
    }

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
                Jotunn.Logger.LogWarning($"White Hilt armor upgrades: skipping \"{part.Trim()}\"");
                continue;
            }

            // The amount lives in m_amountPerLevel: with m_amount 0 the game does not need the material to discover the recipe.
            Piece.Requirement requirement = new() { m_resItem = material, m_amount = 0, m_amountPerLevel = amount, m_recover = false };
            levels[requirement] = (from, to);
            requirements.Add(requirement);
        }

        return requirements;
    }
}
