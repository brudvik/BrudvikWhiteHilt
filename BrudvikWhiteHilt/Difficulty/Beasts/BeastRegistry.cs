using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty.Beasts;

/// <summary>
/// Creates the beasts and their trophies as black clones of the vanilla creatures and trophies.
/// </summary>
public static class BeastRegistry
{
    private static readonly Color glowColor = new(1f, 0.1f, 0.04f);

    /// <summary>
    /// Adds every beast once the vanilla creatures can be cloned.
    /// </summary>
    public static void Initialize()
    {
        CreatureManager.OnVanillaCreaturesAvailable += AddBeasts;
    }

    // Registers every beast once the vanilla creatures are there, each on its own so one failing does not take the
    // rest.
    private static void AddBeasts()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= AddBeasts;
        foreach (BeastDefinition beast in BeastDefinition.All)
        {
            try
            {
                CustomItem trophy = CreateTrophy(beast);
                CreateCreature(beast, trophy.ItemPrefab);
                Jotunn.Logger.LogInfo($"{beast.EnglishName} added!");
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"{beast.EnglishName} failed to load!");
                Jotunn.Logger.LogError(ex);
            }
        }
    }

    private static CustomItem CreateTrophy(BeastDefinition beast)
    {
        CustomItem item = new(beast.TrophyName, beast.BaseTrophy);
        ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
        shared.m_name = Translations.Token(beast.TrophyKey);
        shared.m_description = Translations.Token($"{beast.TrophyKey}_description");
        Darken(item.ItemPrefab);

        Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
        if (icon != null)
        {
            shared.m_icons = new[] { icon };
        }

        ItemManager.Instance.AddItem(item);
        return item;
    }

    // Registers a beast as a faster, darker, glowing clone of a creature, dropping its own trophy.
    private static void CreateCreature(BeastDefinition beast, GameObject trophy)
    {
        string name = Translations.Token(beast.NameKey);
        CustomCreature creature = new(beast.PrefabName, beast.BaseCreature, new CreatureConfig { Name = name });
        // A mod monster's drops are still Jotunn mocks here, and its clone would keep them unresolved.
        creature.FixReference = CreatureManager.Instance.GetCreature(beast.BaseCreature) != null;
        GameObject prefab = creature.Prefab;

        Character character = prefab.GetComponent<Character>();
        character.m_name = name;
        float speed = DifficultySettings.BeastSpeed.Value;
        character.m_speed *= speed;
        character.m_walkSpeed *= speed;
        character.m_runSpeed *= speed;
        character.m_swimSpeed *= speed;
        character.m_flySlowSpeed *= speed;
        character.m_flyFastSpeed *= speed;

        CharacterDrop drops = prefab.GetComponent<CharacterDrop>();
        if (drops != null)
        {
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
        }

        Darken(prefab);
        DarkenCorpses(beast, character);
        AddGlow(prefab);
        prefab.AddComponent<BeastBehaviour>();
        CreatureManager.Instance.AddCreature(creature);
    }

    // A beast cloned from one of the mod's monsters would otherwise leave that monster's coloured corpse.
    private static void DarkenCorpses(BeastDefinition beast, Character character)
    {
        foreach (EffectList.EffectData effect in character.m_deathEffects.m_effectPrefabs)
        {
            if (effect.m_prefab == null || effect.m_prefab.GetComponent<MonsterCorpse>() == null)
            {
                continue;
            }

            GameObject corpse = PrefabManager.Instance.CreateClonedPrefab($"{beast.PrefabName}Corpse", effect.m_prefab);
            Darken(corpse);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(corpse, false));
            effect.m_prefab = corpse;
        }
    }

    private static void Darken(GameObject root)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        VisualHelper.Recolor(root, pixel =>
        {
            Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
            Color32 dark = Color.HSVToRGB(hue, saturation * 0.35f, value * 0.22f);
            dark.a = pixel.a;
            return dark;
        });
    }

    // A red light in the beast's body, so it is seen coming at night.
    private static void AddGlow(GameObject prefab)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        GameObject glow = new("whitehilt_beast_glow");
        glow.transform.SetParent(prefab.transform, false);
        CapsuleCollider body = prefab.GetComponent<CapsuleCollider>();
        glow.transform.localPosition = body != null ? body.center : Vector3.up;

        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.range = 7f;
        light.intensity = 1.6f;
        light.shadows = LightShadows.None;
    }
}
