using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The traders' garb: for each biome a set of four pieces of clothing (hood, tunic, trousers, cape) dyed in the
/// biome's trader's colours, each piece sold at one of the biome's posts only, so the whole set takes a visit to every
/// post of the biome. A piece is light clothing, not armour: a little armour and a small bonus of the biome's kind, which
/// adds up over the pieces; the whole set worn gives the set's own bonus. Worn at the biome's posts, every piece also
/// gets better prices there (<see cref="TradeBonus(Player, int)"/>).
/// </summary>
public static class TraderGarb
{
    /// <summary>The pieces of a set, in the order the posts get them.</summary>
    public static readonly string[] Pieces = { "Hood", "Tunic", "Trousers", "Cape" };

    private const string HoodSource = "HelmetTrollLeather";

    // Capes for the traders who wear none.
    private static readonly Dictionary<string, string> capeSources = new() { { "Meadows", "CapeDeerHide" }, { "Plains", "CapeLinen" } };

    // The set bonuses as status effects, by biome index.
    private static readonly SE_Stats[] setEffects = new SE_Stats[TradingPostGoods.Biomes.Length];

    // The English piece names; the Norwegian ones are in Norwegian.json.
    private static readonly string[] pieceNames = { "Hood", "Tunic", "Trousers", "Cape" };

    // The biomes' names in English for the descriptions; the Norwegian ones are in Norwegian.json.
    private static readonly string[] biomeNames = { "Meadows", "Black Forest", "Swamp", "Mountains", "Plains", "Mistlands", "Ashlands", "Deep North" };

    /// <summary>
    /// Prefab name of one piece of a biome's garb.
    /// </summary>
    /// <param name="biome">The biome index (<see cref="TradingPostGoods.Biomes"/>).</param>
    /// <param name="piece">The piece index (<see cref="Pieces"/>).</param>
    /// <returns>The prefab name.</returns>
    public static string PrefabName(int biome, int piece) => $"WhiteHilt_TraderGarb_{TradingPostGoods.Biomes[biome]}_{Pieces[piece]}";

    /// <summary>
    /// The pieces a post sells by its place among its biome's posts: one each with four posts or more (a fifth post
    /// sells the first piece again, and so on), more at some posts when the biome has fewer, so every piece is sold
    /// somewhere.
    /// </summary>
    /// <param name="slot">The post's place among its biome's posts, from 0.</param>
    /// <param name="count">How many posts the biome has.</param>
    /// <returns>The piece indices.</returns>
    public static IReadOnlyList<int> PiecesAt(int slot, int count)
    {
        if (slot < 0 || count <= 0)
        {
            return Array.Empty<int>();
        }

        int share = Mathf.Min(count, Pieces.Length);
        return Enumerable.Range(0, Pieces.Length).Where(piece => piece % share == slot % share).ToArray();
    }

    /// <summary>
    /// What a piece costs, before the player's Trading: the biome's worth times <see cref="TradingPostSettings.GarbWorth"/>.
    /// </summary>
    /// <param name="biome">The biome index.</param>
    /// <param name="worth">Coins per plain material of the biome.</param>
    /// <returns>Coins, at least 1.</returns>
    public static int Price(int biome, float worth)
    {
        return Mathf.Max(1, Mathf.RoundToInt(TradingPostGoods.PlainWorth(biome) * worth));
    }

    /// <summary>
    /// The share by which prices get better at a post for the garb of its biome worn.
    /// </summary>
    /// <param name="pieces">How many pieces of the biome's garb are worn.</param>
    /// <param name="perPiece">Percent per piece.</param>
    /// <param name="wholeSet">Percent more for the whole set.</param>
    /// <returns>The share, from 0 and below 1.</returns>
    public static float TradeBonus(int pieces, float perPiece, float wholeSet)
    {
        pieces = Mathf.Clamp(pieces, 0, Pieces.Length);
        float percent = pieces * perPiece + (pieces == Pieces.Length ? wholeSet : 0f);
        return Mathf.Clamp(percent / 100f, 0f, 0.9f);
    }

    /// <summary>
    /// The share by which a post's prices get better for the garb of its biome a player wears.
    /// </summary>
    /// <param name="player">The player, or null for none.</param>
    /// <param name="biome">The post's biome index.</param>
    /// <returns>The share.</returns>
    public static float TradeBonus(Player player, int biome)
    {
        return TradeBonus(Worn(player, biome), TradingPostSettings.GarbTradeBonus.Value, TradingPostSettings.GarbSetTradeBonus.Value);
    }

    /// <summary>
    /// A price the player pays at a post, bettered by the garb of its biome they wear.
    /// </summary>
    /// <param name="price">The price.</param>
    /// <param name="bonus">The share from <see cref="TradeBonus(Player, int)"/>.</param>
    /// <returns>Coins, at least 1.</returns>
    public static int Lowered(int price, float bonus) => bonus <= 0f ? price : Mathf.Max(1, Mathf.RoundToInt(price * (1f - bonus)));

    /// <summary>
    /// How many pieces of a biome's garb a player wears.
    /// </summary>
    /// <param name="player">The player, or null.</param>
    /// <param name="biome">The biome index.</param>
    /// <returns>The count.</returns>
    public static int Worn(Player player, int biome)
    {
        if (player == null || biome < 0 || biome >= TradingPostGoods.Biomes.Length)
        {
            return 0;
        }

        string prefix = $"WhiteHilt_TraderGarb_{TradingPostGoods.Biomes[biome]}_";
        return new[] { player.m_helmetItem, player.m_chestItem, player.m_legItem, player.m_shoulderItem }
            .Count(item => item?.m_dropPrefab != null && item.m_dropPrefab.name.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// Whether a character wears the whole Swamp garb, which keeps them dry.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>True with the set's bonus on.</returns>
    public static bool KeepsDry(Character character)
    {
        SE_Stats effect = setEffects[TradingPostGoods.IndexOf("Swamp")];
        return effect != null && character != null && character.GetSEMan() != null && character.GetSEMan().HaveStatusEffect(effect.NameHash());
    }

    /// <summary>
    /// The store entries of the garb a post sells, behind the key that opens its biome, at the player's prices.
    /// </summary>
    /// <param name="biome">The post's biome index.</param>
    /// <param name="postId">The post's id.</param>
    /// <param name="player">The player who trades, or null.</param>
    /// <returns>The entries; none while the post's place is unknown.</returns>
    public static IEnumerable<Trader.TradeItem> TradeItems(int biome, string postId, Player player)
    {
        if (ObjectDB.instance == null || biome < 0 || biome >= TradingPostGoods.Biomes.Length
            || !TradingPostService.TryGetSlot(postId, out int slot, out int count))
        {
            yield break;
        }

        foreach (int piece in PiecesAt(slot, count))
        {
            ItemDrop drop = ObjectDB.instance.GetItemPrefab(PrefabName(biome, piece))?.GetComponent<ItemDrop>();
            if (drop != null)
            {
                yield return new Trader.TradeItem
                {
                    m_prefab = drop,
                    m_stack = 1,
                    m_tooltip = string.Empty,
                    m_price = TradingSkill.Price(Price(biome, TradingPostSettings.GarbWorth.Value), player),
                    m_requiredGlobalKey = TradingPostGoods.OpenKey(biome),
                    m_buyPlayerEffects = new EffectList()
                };
            }
        }
    }

    /// <summary>
    /// Registers the texts and hooks the prefab event. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        for (int biome = 0; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            TraderLook look = Look(biome);
            for (int piece = 0; piece < Pieces.Length; piece++)
            {
                Translations.AddEnglish(Translations.ItemKey(PrefabName(biome, piece)), $"{look.name}'s {pieceNames[piece]}");
            }

            Translations.AddEnglish(DescriptionKey(biome),
                $"Clothing from {look.name}, sold only at the trading posts of the {biomeNames[biome]}, one piece at each. " +
                "Worn at those posts, every piece gets you better prices there, the whole set more.");
            Translations.AddEnglish(SetKey(biome), $"{look.name}'s Garb");
            Translations.AddEnglish(SetKey(biome) + "_tooltip", $"The whole of {look.name}'s garb. Better prices at the trading posts of the {biomeNames[biome]}.");
        }

        Translations.AddEnglish("whitehilt_tradergarb_dry", "Keeps you dry.");
        PrefabManager.OnVanillaPrefabsAvailable += AddGarb;
    }

    private static TraderLook Look(int biome) => TraderLooks.Get((Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), TradingPostGoods.Biomes[biome]));

    private static string DescriptionKey(int biome) => $"item_whitehilt_tradergarb_{TradingPostGoods.Biomes[biome].ToLowerInvariant()}_description";

    private static string SetKey(int biome) => $"whitehilt_tradergarb_{TradingPostGoods.Biomes[biome].ToLowerInvariant()}";

    private static void AddGarb()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddGarb;
        for (int biome = 0; biome < TradingPostGoods.Biomes.Length; biome++)
        {
            TraderLook look = Look(biome);
            setEffects[biome] = SetEffect(biome);
            Color main = TraderLook.ToColour(look.main);
            Color accent = TraderLook.ToColour(look.accent);
            string cape = !string.IsNullOrEmpty(look.cape) ? look.cape : capeSources.TryGetValue(look.biome, out string fallback) ? fallback : "CapeDeerHide";
            Color capeColour = look.capeColour != null && look.capeColour.Length >= 3 ? TraderLook.ToColour(look.capeColour) : main;
            Add(biome, 0, HoodSource, TraderClothes.Dye(main, accent, keepLeather: true));
            Add(biome, 1, look.tunic, TraderClothes.Dye(main, accent, keepLeather: true));
            Add(biome, 2, look.trousers, TraderClothes.Dye(TraderLook.ToColour(look.legs), null, keepLeather: true));
            Add(biome, 3, cape, TraderClothes.Dye(capeColour, accent, keepLeather: false));
        }
    }

    // One piece: a copy of a vanilla piece of clothing, dyed, with the biome's small bonus and the set.
    private static void Add(int biome, int piece, string source, Func<Color32, Color32> dye)
    {
        string prefabName = PrefabName(biome, piece);
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(prefabName)),
                Description = Translations.Token(DescriptionKey(biome))
            };
            CustomItem item = new(prefabName, source, config);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_value = 0;
            shared.m_weight = 1f;
            shared.m_maxQuality = 1;
            shared.m_armor = 2f * (biome + 1);
            shared.m_armorPerLevel = 0f;
            shared.m_useDurability = false;
            shared.m_teleportable = true;
            shared.m_movementModifier = 0f;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
            shared.m_equipStatusEffect = null;
            shared.m_setName = SetKey(biome);
            shared.m_setSize = Pieces.Length;
            shared.m_setStatusEffect = setEffects[biome];
            PieceBonus(biome, shared);
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    UniformLook.Recolor(item.ItemPrefab, shared, dye);
                    UniformLook.RenderIcon(item.ItemPrefab, shared);
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{prefabName}: not dyed: {ex.Message}");
                }
            }

            // The set's bonus shows the tunic.
            if (piece == 1 && setEffects[biome] != null && shared.m_icons != null && shared.m_icons.Length > 0)
            {
                setEffects[biome].m_icon = shared.m_icons[0];
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{prefabName} failed to load: {ex.Message}");
        }
    }

    // Each piece's small bonus, of the biome's kind; the game adds them up over the pieces worn.
    private static void PieceBonus(int biome, ItemDrop.ItemData.SharedData shared)
    {
        switch (TradingPostGoods.Biomes[biome])
        {
            case "Meadows":
                shared.m_movementModifier = 0.015f;
                break;
            case "BlackForest":
                shared.m_sneakStaminaModifier = -0.1f;
                break;
            case "Swamp":
                shared.m_swimStaminaModifier = -0.1f;
                break;
            case "Mountain":
                shared.m_jumpStaminaModifier = -0.1f;
                break;
            case "Plains":
                shared.m_runStaminaModifier = -0.05f;
                break;
            case "Mistlands":
                shared.m_eitrRegenModifier = 0.05f;
                break;
            case "AshLands":
                shared.m_heatResistanceModifier = 0.1f;
                break;
            case "DeepNorth":
                shared.m_attackStaminaModifier = -0.04f;
                break;
        }
    }

    // The whole set's bonus, as a status effect the game puts on while every piece is worn.
    private static SE_Stats SetEffect(int biome)
    {
        SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = $"SE_WhiteHilt_TraderGarb_{TradingPostGoods.Biomes[biome]}";
        effect.m_name = Translations.Token(SetKey(biome));
        effect.m_tooltip = Translations.Token(SetKey(biome) + "_tooltip");
        switch (TradingPostGoods.Biomes[biome])
        {
            case "Meadows":
                effect.m_staminaRegenMultiplier = 1.2f;
                break;
            case "BlackForest":
                effect.m_noiseModifier = -0.3f;
                effect.m_stealthModifier = -0.2f;
                break;
            case "Swamp":
                effect.m_mods = Resist(HitData.DamageType.Poison, HitData.DamageModifier.Resistant);
                effect.m_tooltip += "\n" + Translations.Token("whitehilt_tradergarb_dry");
                break;
            case "Mountain":
                effect.m_mods = Resist(HitData.DamageType.Frost, HitData.DamageModifier.Resistant);
                break;
            case "Plains":
                effect.m_addMaxCarryWeight = 50f;
                break;
            case "Mistlands":
                effect.m_fallDamageModifier = -0.5f;
                effect.m_jumpModifier = new Vector3(0f, 0.15f, 0f);
                break;
            case "AshLands":
                effect.m_mods = Resist(HitData.DamageType.Fire, HitData.DamageModifier.Resistant);
                break;
            case "DeepNorth":
                effect.m_mods = Resist(HitData.DamageType.Frost, HitData.DamageModifier.VeryResistant);
                effect.m_healthRegenMultiplier = 1.15f;
                break;
        }

        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
        return effect;
    }

    private static List<HitData.DamageModPair> Resist(HitData.DamageType type, HitData.DamageModifier modifier)
    {
        return new List<HitData.DamageModPair> { new() { m_type = type, m_modifier = modifier } };
    }
}
