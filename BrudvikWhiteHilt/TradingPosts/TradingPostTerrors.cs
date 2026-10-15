using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// One biome's plague: the named monster that now and then terrorises a post of the biome (<see cref="TradingPostTerror"/>),
/// cloned from one of the biome's creatures, bigger, in its own colour and glowing, and always dropping its own trophy,
/// which the plagued post's trader takes as proof and pays a bounty for.
/// </summary>
public sealed class TerrorDefinition
{
    /// <summary>
    /// The plagues, one per biome in the order of <see cref="TradingPostGoods.Biomes"/>.
    /// </summary>
    public static readonly TerrorDefinition[] All =
    {
        new("Bristleback", "Bristleback", "Boar", "TrophyBoar", 2.4f, new Color(0.62f, 0.38f, 0.2f)),
        new("Gnawtooth", "Gnawtooth", "Greydwarf_Elite", "TrophyGreydwarfBrute", 1.5f, new Color(0.35f, 0.55f, 0.25f)),
        new("MireMother", "Mire Mother", "Abomination", "TrophyAbomination", 1.25f, new Color(0.4f, 0.45f, 0.22f)),
        new("Whiteclaw", "Whiteclaw", "Fenring", "TrophyFenring", 1.5f, new Color(0.85f, 0.9f, 0.98f)),
        new("Skullsplitter", "Skullsplitter", "GoblinBrute", "TrophyGoblinBrute", 1.35f, new Color(0.75f, 0.25f, 0.15f)),
        new("Clickjaw", "Clickjaw", "Seeker", "TrophySeeker", 1.8f, new Color(0.5f, 0.3f, 0.75f)),
        new("Ashfang", "Ashfang", "Asksvin", "TrophyAsksvin", 1.6f, new Color(0.95f, 0.45f, 0.12f)),
        new("Icemaw", "Icemaw", "Barka", "TrophyBarka", 1.3f, new Color(0.45f, 0.7f, 0.95f))
    };

    private TerrorDefinition(string key, string englishName, string baseCreature, string baseTrophy, float scale, Color colour)
    {
        Key = key;
        EnglishName = englishName;
        BaseCreature = baseCreature;
        BaseTrophy = baseTrophy;
        Scale = scale;
        Colour = colour;
    }

    /// <summary>Identifier, in prefab names.</summary>
    public string Key { get; }

    /// <summary>English name.</summary>
    public string EnglishName { get; }

    /// <summary>The vanilla creature it is cloned from.</summary>
    public string BaseCreature { get; }

    /// <summary>The vanilla trophy its trophy is cloned from.</summary>
    public string BaseTrophy { get; }

    /// <summary>How much bigger than the creature it is.</summary>
    public float Scale { get; }

    /// <summary>Its colour, and its glow's.</summary>
    public Color Colour { get; }

    /// <summary>Prefab name of the monster.</summary>
    public string PrefabName => $"WhiteHilt_Terror{Key}";

    /// <summary>Prefab name of its trophy.</summary>
    public string TrophyName => $"WhiteHilt_TrophyTerror{Key}";

    /// <summary>Translation key of its name.</summary>
    public string NameKey => $"enemy_whitehilt_terror{Key.ToLowerInvariant()}";

    /// <summary>Translation key of its trophy's name.</summary>
    public string TrophyKey => Translations.ItemKey(TrophyName);

    /// <summary>
    /// A biome's plague.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <returns>The plague, or null for a biome without.</returns>
    public static TerrorDefinition Of(int biome) => biome >= 0 && biome < All.Length ? All[biome] : null;

    /// <summary>
    /// The biome whose plague drops a trophy, or -1 for any other item.
    /// </summary>
    /// <param name="prefabName">The item's prefab name.</param>
    /// <returns>The biome index.</returns>
    public static int BiomeOfTrophy(string prefabName) => Array.FindIndex(All, terror => terror.TrophyName == prefabName);
}

/// <summary>
/// Registers the plagues' monsters, their trophies and the bounty notice, and the texts of it all.
/// </summary>
public static class TradingPostTerrors
{
    /// <summary>Prefab name of the bounty notice a plagued post's trader hands out.</summary>
    public const string NoticeName = "WhiteHilt_TerrorNotice";

    /// <summary>The monster's level: three stars.</summary>
    public const int Level = 4;

    private const string NoticeSource = "LeatherScraps";
    private const float NoticeLength = 0.35f;
    private static readonly Color noticeTint = new(0.95f, 0.75f, 0.6f);

    /// <summary>
    /// The bounty a plagued post pays for its monster's trophy, before anything else.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <param name="worth">Coins per plain material of the biome (<see cref="TradingPostSettings.TerrorBounty"/>).</param>
    /// <returns>Coins, at least 1.</returns>
    public static int Bounty(int biome, float worth) => Mathf.Max(1, Mathf.RoundToInt(TradingPostGoods.PlainWorth(biome) * worth));

    /// <summary>
    /// Registers the texts and hooks the prefab events. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        foreach (TerrorDefinition terror in TerrorDefinition.All)
        {
            Translations.AddEnglish(terror.NameKey, terror.EnglishName);
            Translations.AddEnglishNameAndDescription(terror.TrophyKey, $"{terror.EnglishName}'s Head",
                $"The head of {terror.EnglishName}, who plagued a trading post. The trader there pays a bounty for it.");
        }

        Translations.AddEnglishNameAndDescription(Translations.ItemKey(NoticeName), "Bounty Notice",
            "A trader's notice of the monster plaguing the post, with a circle on the map where it was last seen. Read it to see the circle. " +
            "Bring the monster's head back to the trader for the bounty.");
        Translations.AddEnglish("whitehilt_terror_plagued", "Plagued by $1");
        Translations.AddEnglish("whitehilt_terror_ask", "Plagued by $1: buy the free Bounty Notice");
        Translations.AddEnglish("whitehilt_terror_pin", "$1, last seen");
        Translations.AddEnglish("msg_whitehilt_terror_notice", "$1 was last seen in the circle on your map. $2 days left.");
        Translations.AddEnglish("msg_whitehilt_terror_lapsed", "The bounty has lapsed; the monster has left these lands.");
        Translations.AddEnglish("msg_whitehilt_terror_tracks", "Fresh tracks. $1 is near.");
        Translations.AddEnglish("msg_whitehilt_terror_comes", "$1 comes down to the trading post!");
        Translations.AddEnglish("msg_whitehilt_terror_paid", "Bounty for $1: $2 coins. The trader gives you half off the post's garb.");
        TradingPostTrader.Dialogue("terror_greet", "Thank the gods, a fighter! $1 haunts this place.", "Careful out there, $1 was seen again last night.");
        TradingPostTrader.Dialogue("terror_talk", "$1 came down to the yard last night. I barely slept.", "Bring me the head of $1 and I pay you well.",
            "Nobody dares come here while $1 roams the land.");
        CreatureManager.OnVanillaCreaturesAvailable += AddTerrors;
        PrefabManager.OnVanillaPrefabsAvailable += AddNotice;
    }

    private static void AddTerrors()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= AddTerrors;
        foreach (TerrorDefinition terror in TerrorDefinition.All)
        {
            try
            {
                CustomItem trophy = CreateTrophy(terror);
                CreateCreature(terror, trophy.ItemPrefab);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"{terror.EnglishName} failed to load!");
                Jotunn.Logger.LogError(ex);
            }
        }
    }

    private static CustomItem CreateTrophy(TerrorDefinition terror)
    {
        CustomItem item = new(terror.TrophyName, terror.BaseTrophy);
        ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = Translations.Token(terror.TrophyKey);
        shared.m_description = Translations.Token($"{terror.TrophyKey}_description");
        shared.m_value = 0;
        shared.m_maxStackSize = 1;
        Tint(item.ItemPrefab, terror.Colour);
        Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
        if (icon != null)
        {
            shared.m_icons = new[] { icon };
        }

        ItemManager.Instance.AddItem(item);
        return item;
    }

    // The monster: a bigger, tinted, glowing clone of the creature, dropping its own trophy every time.
    private static void CreateCreature(TerrorDefinition terror, GameObject trophy)
    {
        string name = Translations.Token(terror.NameKey);
        CustomCreature creature = new(terror.PrefabName, terror.BaseCreature, new CreatureConfig { Name = name });
        GameObject prefab = creature.Prefab;
        prefab.transform.localScale *= terror.Scale;
        Character character = prefab.GetComponent<Character>();
        character.m_name = name;
        character.m_boss = false;

        CharacterDrop drops = prefab.GetComponent<CharacterDrop>() ?? prefab.AddComponent<CharacterDrop>();
        drops.m_drops.RemoveAll(drop => drop.m_prefab != null && drop.m_prefab.name.Contains("Trophy"));
        drops.m_drops.Add(new CharacterDrop.Drop
        {
            m_prefab = trophy,
            m_amountMin = 1,
            m_amountMax = 1,
            m_chance = 1f,
            m_levelMultiplier = false,
            m_onePerPlayer = false,
            m_dontScale = true
        });

        // It stays in its own ground and does not tame.
        BaseAI ai = prefab.GetComponent<BaseAI>();
        if (ai != null)
        {
            ai.m_randomMoveRange = 30f;
        }

        foreach (Component part in new Component[] { prefab.GetComponent<Procreation>(), prefab.GetComponent<Tameable>() }.Where(part => part != null))
        {
            UnityEngine.Object.DestroyImmediate(part);
        }

        Tint(prefab, terror.Colour);
        AddGlow(prefab, terror.Colour);
        prefab.AddComponent<TerrorBehaviour>();
        Monsters.MonsterRegistry.AddCloneSounds(prefab, terror.Key.ToLowerInvariant());
        if (terror.Key is "MireMother" or "Gnawtooth" or "Icemaw")
        {
            Monsters.MonsterRegistry.SetFootsteps(prefab, "step_heavy");
        }

        prefab.AddComponent<Monsters.ArrivalSound>().clip = "arrival_terror";
        CreatureManager.Instance.AddCreature(creature);
    }

    // The bounty notice: a scrap of leather drawn as the treasure maps are, a little reddened.
    private static void AddNotice()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddNotice;
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(NoticeName)),
                Description = Translations.Token(Translations.ItemKey(NoticeName) + "_description")
            };
            CustomItem item = new(NoticeName, NoticeSource, config);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_weight = 0.1f;
            shared.m_maxStackSize = 1;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Misc;
            shared.m_value = 0;
            shared.m_teleportable = true;
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("treasuremap"), ForagingAssets.LoadTexture("treasuremap_albedo"), size: NoticeLength);
                    VisualHelper.Tint(item.ItemPrefab, noticeTint);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = Enumerable.Repeat(icon, Mathf.Max(1, shared.m_icons?.Length ?? 0)).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{NoticeName}: no custom look: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{NoticeName} failed to load: {ex.Message}");
        }
    }

    // Shifts the colours halfway towards the plague's, keeping light and shade.
    private static void Tint(GameObject root, Color colour)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        VisualHelper.Recolor(root, pixel =>
        {
            Color.RGBToHSV(pixel, out _, out _, out float value);
            Color tinted = Color.Lerp(pixel, colour * Mathf.Clamp(value * 1.3f, 0.15f, 1.2f), 0.55f);
            tinted.a = pixel.a / 255f;
            return tinted;
        });
    }

    // A light in the plague's colour in its body, so it is seen coming at night.
    private static void AddGlow(GameObject prefab, Color colour)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        GameObject glow = new("whitehilt_terror_glow");
        glow.transform.SetParent(prefab.transform, false);
        CapsuleCollider body = prefab.GetComponent<CapsuleCollider>();
        glow.transform.localPosition = body != null ? body.center : Vector3.up;
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = colour;
        light.range = 8f;
        light.intensity = 1.4f;
        light.shadows = LightShadows.None;
    }
}
