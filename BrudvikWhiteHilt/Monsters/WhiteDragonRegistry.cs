using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Summoning;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Creates the White Dragon that the Dragon Horn calls: a pale clone of the Desert Dragon, tame from birth, with a
/// weaker breath of its own, no loot and a white corpse.
/// </summary>
public static class WhiteDragonRegistry
{
    /// <summary>Prefab name of the White Dragon.</summary>
    public const string DragonName = "WhiteHilt_WhiteDragon";

    private const string FlameName = "WhiteHilt_WhiteDragonFlame";
    private const string CorpseName = "WhiteHilt_WhiteDragonCorpse";

    // The vanilla summoned skeleton lends its petting effect and words.
    private const string SummonTemplate = "Skeleton_Friendly";

    /// <summary>
    /// Adds the name, and the prefab once the Desert Dragon exists. Call after <see cref="DesertDragonRegistry.Initialize"/>.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("enemy_whitehilt_whitedragon", "White Dragon");
        CreatureManager.OnVanillaCreaturesAvailable += Add;
    }

    private static void Add()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Add;
        try
        {
            AddDragon(AddFlame());
            Jotunn.Logger.LogInfo("White Dragon added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("White Dragon failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // The Desert Dragon's breath with the Dragon Horn's own damage, flames and pace.
    private static CustomItem AddFlame()
    {
        CustomItem weapon = new(FlameName, DesertDragonRegistry.FlameName);
        ItemDrop.ItemData.SharedData shared = weapon.ItemDrop.m_itemData.m_shared;
        shared.m_damages = new HitData.DamageTypes { m_fire = DragonHorn.FireDamage.Value };
        shared.m_aiAttackInterval = DragonHorn.BreathSeconds.Value;
        shared.m_attack.m_projectileBursts = DragonHorn.Flames.Value;
        ItemManager.Instance.AddItem(weapon);
        return weapon;
    }

    private static void AddDragon(CustomItem flame)
    {
        string name = Translations.Token("enemy_whitehilt_whitedragon");
        CustomCreature creature = new(DragonName, DesertDragonRegistry.DragonName, new CreatureConfig { Name = name });
        // The Desert Dragon's drops are still Jotunn mocks here, and its clone would keep them unresolved.
        creature.FixReference = true;
        GameObject prefab = creature.Prefab;

        Humanoid humanoid = prefab.GetComponent<Humanoid>();
        humanoid.m_name = name;
        humanoid.m_health = DragonHorn.Health.Value;
        humanoid.m_defaultItems = new[] { flame.ItemPrefab };

        // A called friend leaves nothing to farm.
        prefab.GetComponent<CharacterDrop>()?.m_drops.Clear();

        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_despawnInDay = false;
        ai.m_fleeIfLowHealth = 0f;
        ai.m_attackPlayerObjects = false;

        Tameable tameable = prefab.AddComponent<Tameable>();
        tameable.m_startsTamed = true;
        tameable.m_commandable = false;
        // Never hungry: IsHungry counts from the year 1 when it was never fed.
        tameable.m_fedDuration = float.MaxValue;
        Tameable template = PrefabManager.Instance.GetPrefab(SummonTemplate)?.GetComponent<Tameable>();
        if (template != null)
        {
            tameable.m_petEffect = template.m_petEffect;
            tameable.m_tameText = template.m_tameText;
            tameable.m_nameBeforeText = template.m_nameBeforeText;
        }

        prefab.AddComponent<WhiteDragonAlly>();
        Whiten(prefab);
        WhitenCorpses(humanoid);
        CreatureManager.Instance.AddCreature(creature);
    }

    // The Desert Dragon's corpse would lie there sand-coloured.
    private static void WhitenCorpses(Character character)
    {
        foreach (EffectList.EffectData effect in character.m_deathEffects.m_effectPrefabs)
        {
            if (effect.m_prefab == null || effect.m_prefab.GetComponent<MonsterCorpse>() == null)
            {
                continue;
            }

            GameObject corpse = PrefabManager.Instance.CreateClonedPrefab(CorpseName, effect.m_prefab);
            Whiten(corpse);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(corpse, false));
            effect.m_prefab = corpse;
        }
    }

    private static void Whiten(GameObject root)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        VisualHelper.Recolor(root, pixel =>
        {
            Color.RGBToHSV(pixel, out _, out float saturation, out float value);
            Color32 white = Color.HSVToRGB(0.58f, saturation * 0.12f, Mathf.Clamp01(0.35f + value * 0.75f));
            white.a = pixel.a;
            return white;
        });
    }
}
