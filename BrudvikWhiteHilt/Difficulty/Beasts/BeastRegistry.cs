using BrudvikWhiteHilt.Helpers;
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

    private static void CreateCreature(BeastDefinition beast, GameObject trophy)
    {
        string name = Translations.Token(beast.NameKey);
        CustomCreature creature = new(beast.PrefabName, beast.BaseCreature, new CreatureConfig { Name = name });
        GameObject prefab = creature.Prefab;

        Character character = prefab.GetComponent<Character>();
        character.m_name = name;
        float speed = DifficultySettings.BeastSpeed.Value;
        character.m_speed *= speed;
        character.m_walkSpeed *= speed;
        character.m_runSpeed *= speed;
        character.m_swimSpeed *= speed;

        CharacterDrop drops = prefab.GetComponent<CharacterDrop>();
        if (drops != null)
        {
            drops.m_drops.RemoveAll(drop => drop.m_prefab != null && drop.m_prefab.name.StartsWith("Trophy", StringComparison.Ordinal));
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
        AddGlow(prefab);
        prefab.AddComponent<BeastBehaviour>();
        CreatureManager.Instance.AddCreature(creature);
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
