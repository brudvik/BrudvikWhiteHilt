using BepInEx.Configuration;
using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Bestiary;

/// <summary>A crafted material counter, its recipe and the beast it affects.</summary>
public sealed class BeastCounter
{
    private const string KnownBiomesKey = "whitehilt_bestiary_biomes";

    /// <summary>The nine distinct material counters, in book order.</summary>
    public static readonly BeastCounter[] All =
    {
        new("BlackTroll", "TrollArrow", "Troll Arrow", "ArrowFlint", HitData.DamageType.Pierce, 27f, 0f,
            "Wood:8,Feathers:2,Flint:2,WhiteHiltLingonberries:3", "A flint arrow treated with lingonberries. Its preparation finds a way through the Black Troll's hide.",
            "Its sweeping club and thrown stones punish a careless approach. Keep room to dodge; do not shelter in a fragile building.",
            "Lingonberries grow in the Black Forest and may also drop from blueberry bushes."),
        new("BlackAbomination", "EmberArrow", "Ember Arrow", "ArrowFire", HitData.DamageType.Fire, 11f, 22f,
            "Wood:8,Feathers:2,WhiteHiltPeat:2,Resin:3", "An arrow packed with smouldering peat and resin. The mixture burns into the Black Abomination's wood.",
            "Its long legs sweep a wide area and its stomps are dangerous at close range. Fight away from trees and water that obstruct your escape.",
            "Peat is gathered on the drier banks of the Swamp."),
        new("BlackStoneGolem", "StonebreakerCoating", "Stonebreaker Coating", "MeadTasty", HitData.DamageType.Pickaxe, 0f, 0f,
            "WhiteHiltRockLichen:3,WhiteHilt_Slate:2,Resin:2", "A rock-lichen and slate preparation for a pickaxe. It opens cracks in the Black Stone Golem.",
            "Its heavy arms strike hard and can throw you off a mountain ledge. A pickaxe leaves you exposed: dodge the blow before moving in.",
            "Rock Lichen grows on Mountain stones and can drop from Stone Golems. Slate is mined from Mountain slate outcrops."),
        new("BlackFulingBerserker", "BerserkerCoating", "Berserker Coating", "MeadTasty", HitData.DamageType.Blunt, 0f, 0f,
            "WhiteHiltHenbane:3,Resin:2,Coal:1", "An alchemical henbane coating for a blunt weapon. Its blows unsettle the Black Fuling Berserker.",
            "Its flurry of heavy blows makes a failed block costly. Let the combination finish before closing in; watch for other fulings.",
            "Henbane grows in Plains fields and may also drop from wild barley."),
        new("BlackDragon", "RimeArrow", "Rime Arrow", "ArrowFrost", HitData.DamageType.Frost, 26f, 52f,
            "Wood:8,Feathers:2,WhiteHiltCrowberries:3,FreezeGland:1", "A frost arrow steeped in crowberries. The preparation chills the Black Dragon more deeply than ordinary frost.",
            "It never lands. Step sideways out of its widening fire breath and leave the ground flames. Fire resistance helps, but does not replace dodging.",
            "Crowberries grow on Mountain slopes and may drop from wolves. Drakes drop Freeze Glands."),
        new("BlackSeekerSoldier", "CarapaceWhetstone", "Carapace Whetstone", "MeadTasty", HitData.DamageType.Slash, 0f, 0f,
            "WhiteHiltWoad:3,WhiteHilt_Slate:2,Resin:2", "A slate whetstone dressed with woad for a slashing weapon. The treated edge cuts into the Black Seeker Soldier's joints.",
            "Its armoured front and heavy stomps punish a frontal duel. Move around it and strike its softer rear when it turns toward an ally.",
            "Woad grows on the Plains and may drop from wild flax. Slate is mined in the Mountains."),
        new("BlackMorgen", "SeidArrow", "Seid Arrow", "ArrowSilver", HitData.DamageType.Spirit, 26f, 20f,
            "Wood:8,Feathers:2,WhiteHiltJuniper:3,Silver:1", "An arrow prepared with juniper and silver. Its seid smoke reaches the corruption inside the Black Morgen.",
            "Its rolling attacks close distance quickly. Keep a clear escape route, avoid being pinned against rocks, and do not spend all your stamina chasing it.",
            "Juniper Berries grow on Mountain slopes and may drop from wild onions. Silver is mined in the Mountains."),
        new("BlackSerpent", "BogVenomArrow", "Bog Venom Arrow", "ArrowPoison", HitData.DamageType.Poison, 26f, 26f,
            "Wood:8,Feathers:2,WhiteHilt_PoisonGland:1,WhiteHiltSweetGale:3", "An arrow carrying poison gland extract bound with sweet gale. The mixture is prepared specifically for the Black Serpent.",
            "It bites at the ship and at swimmers. Shoot from the deck, keep the ship moving, and repair before setting out rather than jumping into the sea.",
            "Giant Spiders drop Poison Glands. Sweet Gale grows in the Swamp and may drop from thistles there."),
        new("BlackBonemaw", "StormArrow", "Storm Arrow", "ArrowCarapace", HitData.DamageType.Lightning, 32f, 30f,
            "Wood:8,Feathers:2,WhiteHiltRosehips:3,WhiteHilt_KrakenInk:1", "An arrow inked with kraken ink and rosehips. Its alchemical charge strikes through the Black Bonemaw's hide.",
            "Its attacks threaten both crew and hull in dangerous Ashlands waters. Keep distance, bring repairs, and let one crew member steer while the others shoot.",
            "Rosehips grow on the Plains and may drop from cloudberry bushes. Kraken drops Kraken Ink.")
    };

    private ConfigEntry<float> bonus;
    private ConfigEntry<float> pierce;
    private ConfigEntry<float> element;
    private ConfigEntry<int> attacks;
    private ConfigEntry<int> yield;

    /// <summary>The beast's definition.</summary>
    public BeastDefinition Beast { get; }
    /// <summary>Stable counter identifier.</summary>
    public string Key { get; }
    /// <summary>English item name.</summary>
    public string Name { get; }
    /// <summary>Vanilla visual and projectile template.</summary>
    public string CopyFrom { get; }
    /// <summary>Damage channel, or the required melee channel for a treatment.</summary>
    public HitData.DamageType DamageType { get; }
    /// <summary>Default ingredient list.</summary>
    public string Recipe { get; }
    /// <summary>English item description.</summary>
    public string Description { get; }
    /// <summary>English combat warning.</summary>
    public string Danger { get; }
    /// <summary>English gathering directions.</summary>
    public string Gathering { get; }
    /// <summary>Whether this counter is ammunition.</summary>
    public bool IsArrow => CopyFrom != "MeadTasty";
    /// <summary>Item prefab identifier.</summary>
    public string PrefabName => "WhiteHilt" + Key;
    /// <summary>Localized item name key.</summary>
    public string NameKey => Translations.ItemKey(PrefabName);
    /// <summary>Serialized hit marker; it is consumed before vanilla status effects are applied.</summary>
    public int Marker => ("WhiteHiltCounter_" + Key).GetStableHashCode();
    /// <summary>Configured extra material damage share, before armour.</summary>
    public float Bonus => bonus.Value;
    /// <summary>Configured arrow pierce damage.</summary>
    public float Pierce => pierce.Value;
    /// <summary>Configured elemental arrow damage.</summary>
    public float Element => element.Value;
    /// <summary>Configured number of eligible attacks per treatment.</summary>
    public int Attacks => attacks.Value;
    /// <summary>Configured output per craft.</summary>
    public int Yield => yield.Value;

    private BeastCounter(string beast, string key, string name, string copyFrom, HitData.DamageType damageType,
        float basePierce, float baseElement, string recipe, string description, string danger, string gathering)
    {
        Beast = BeastDefinition.All.Single(definition => definition.Key == beast);
        Key = key;
        Name = name;
        CopyFrom = copyFrom;
        DamageType = damageType;
        Recipe = recipe;
        Description = description;
        Danger = danger;
        Gathering = gathering;
        BasePierce = basePierce;
        BaseElement = baseElement;
    }

    private float BasePierce { get; }
    private float BaseElement { get; }

    /// <summary>Registers settings and English book text before content discovery.</summary>
    public static void Initialize()
    {
        foreach (BeastCounter counter in All)
        {
            string section = "Bestiary." + counter.Key;
            WhiteHiltConfig.SetSectionLabel(section, Translations.Token(counter.NameKey));
            counter.bonus = WhiteHiltConfig.BindAdminOnly(section, "Bonus", 0.5f,
                "Extra material damage against this counter's black beast, as a share of the attack's combat damage before resistance and armour. Other targets get no bonus.", new AcceptableValueRange<float>(0f, 3f));
            counter.yield = WhiteHiltConfig.BindAdminOnly(section, "Yield", counter.IsArrow ? 20 : 1,
                "Items made per craft.", new AcceptableValueRange<int>(1, 100));
            if (counter.IsArrow)
            {
                counter.pierce = WhiteHiltConfig.BindAdminOnly(section, "Pierce", counter.BasePierce,
                    "Pierce damage per arrow.", new AcceptableValueRange<float>(0f, 500f));
                counter.element = WhiteHiltConfig.BindAdminOnly(section, "Element", counter.BaseElement,
                    "Elemental damage per arrow; unused for Troll Arrows.", new AcceptableValueRange<float>(0f, 500f));
            }
            else
            {
                counter.attacks = WhiteHiltConfig.BindAdminOnly(section, "Attacks", 30,
                    "Eligible attacks per treatment, including misses. Only the weapon held when applied is treated. Mining attacks use Stonebreaker Coating too.", new AcceptableValueRange<int>(1, 500));
            }
            Translations.AddEnglishNameAndDescription(counter.NameKey, counter.Name, counter.Description);
            Translations.AddEnglish("whitehilt_counter_" + counter.Key.ToLowerInvariant() + "_danger", counter.Danger);
            Translations.AddEnglish("whitehilt_counter_" + counter.Key.ToLowerInvariant() + "_gathering", counter.Gathering);
        }
    }

    /// <summary>Finds a counter from its stable key.</summary>
    /// <param name="key">Counter key.</param>
    /// <returns>The counter.</returns>
    public static BeastCounter Get(string key) => All.Single(counter => counter.Key == key);

    /// <summary>Remembers a biome's stable identity when the local character discovers it.</summary>
    /// <param name="player">Character whose exploration is saved.</param>
    /// <param name="biome">Discovered biome, independent of its localized sector name.</param>
    public static void DiscoverBiome(Player player, Heightmap.Biome biome)
    {
        if (player == null || biome == Heightmap.Biome.None)
        {
            return;
        }
        Heightmap.Biome known = KnownBiomes(player) | biome;
        player.m_customData[KnownBiomesKey] = ((int)known).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Lists only counters belonging to a biome this character has already discovered.</summary>
    /// <param name="player">Reader; a missing player has no available pages.</param>
    /// <returns>Discovered pages, in stable book order. A multibiome beast appears once.</returns>
    public static BeastCounter[] Discovered(Player player)
    {
        Heightmap.Biome known = KnownBiomes(player);
        return All.Where(counter => (counter.Beast.Biome & known) != Heightmap.Biome.None).ToArray();
    }

    /// <summary>Builds crafting requirements from this definition.</summary>
    /// <returns>The recipe ingredients.</returns>
    public RequirementConfig[] Requirements() => Recipe.Split(',').Select(entry =>
    {
        string[] fields = entry.Split(':');
        return new RequirementConfig { Item = fields[0], Amount = int.Parse(fields[1]) };
    }).ToArray();

    /// <summary>Consumes a hit marker and adds material damage only against its matching black prefab.</summary>
    /// <param name="prefabName">Actual target prefab, never its localized display name.</param>
    /// <param name="hit">Incoming serialized attack.</param>
    public static void Apply(string prefabName, HitData hit)
    {
        if (hit == null)
        {
            return;
        }
        BeastCounter counter = All.FirstOrDefault(candidate => candidate.Marker == hit.m_statusEffectHash);
        if (counter == null)
        {
            return;
        }
        hit.m_statusEffectHash = 0;
        if (prefabName != counter.Beast.PrefabName)
        {
            return;
        }
        HitData.DamageTypes damage = hit.m_damage;
        float combat = damage.m_damage + damage.m_blunt + damage.m_slash + damage.m_pierce + damage.m_fire
            + damage.m_frost + damage.m_lightning + damage.m_poison + damage.m_spirit;
        if (counter.DamageType == HitData.DamageType.Pickaxe)
        {
            combat += damage.m_pickaxe;
        }
        hit.m_damage.m_nonPlayer += Mathf.Max(0f, combat) * counter.Bonus;
    }

    private static Heightmap.Biome KnownBiomes(Player player)
    {
        if (player == null)
        {
            return Heightmap.Biome.None;
        }
        Heightmap.Biome known = Heightmap.Biome.None;
        if (player.m_customData.TryGetValue(KnownBiomesKey, out string saved)
            && int.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mask))
        {
            known = (Heightmap.Biome)mask;
        }
        foreach (Heightmap.Biome biome in Enum.GetValues(typeof(Heightmap.Biome)))
        {
            if (biome == Heightmap.Biome.None)
            {
                continue;
            }
            string token = BiomeSector.GetBiomeName(biome);
            string localized = Localization.instance?.Localize(token);
            if (player.m_knownBiome.Contains(token) || (localized != null && player.m_knownBiome.Contains(localized)))
            {
                known |= biome;
            }
        }
        return known;
    }
}