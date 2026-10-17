using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Makes the Lava Worm: the Lindorm's model, charred black and glowing from within, on a clone of the vanilla Greyling.
/// It lives in the Ashlands' lava, which does not hurt it; its bite burns and drags its prey towards the lava, and
/// <see cref="LavaWormService"/> lets it rise near players who come close to the lava.
/// </summary>
public static class LavaWormRegistry
{
    /// <summary>Prefab name of the Lava Worm.</summary>
    public const string PrefabName = "WhiteHilt_LavaWorm";

    /// <summary>Prefab name of the Lava Worm scale.</summary>
    public const string ScaleName = "WhiteHilt_LavaWormScale";

    /// <summary>Prefab name of the Lava Worm trophy.</summary>
    public const string TrophyName = "WhiteHilt_TrophyLavaWorm";

    private const string NameKey = "enemy_whitehilt_lavaworm";
    private const string BiteName = "WhiteHilt_LavaWormBite";
    private const string CorpseName = "WhiteHilt_LavaWormCorpse";
    private const string Model = "lindorm";
    // Its materials are copies of the Asksvin's, so they are its own and not the Lindorm's, which are made from the
    // serpent's: they can be coloured without colouring the Lindorm.
    private const string TemplateCreature = "Asksvin";

    private static readonly Color Char = new(0.32f, 0.24f, 0.22f);
    private static readonly Color Glow = new Color(1f, 0.32f, 0.04f) * 0.9f;
    private static readonly HashSet<Material> dressed = new();

    /// <summary>
    /// Binds the settings, registers the English texts, and makes the prefab once the vanilla creatures can be cloned.
    /// </summary>
    public static void Initialize()
    {
        LavaWormSettings.Initialize();
        Translations.AddEnglish(NameKey, "Lava Worm");
        Translations.AddEnglish("msg_whitehilt_lavaworm_rises", "The lava boils over...");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(ScaleName), "Lava Worm Scale",
            "A black plate with a glowing seam. It has lain in lava for longer than you have lived.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(TrophyName), "Lava Worm Trophy",
            "It pulled the bold into the fire. You pulled back harder.");
        CreatureManager.OnVanillaCreaturesAvailable += Add;
    }

    private static void Add()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Add;
        try
        {
            AddLoot();
            AddWorm();
            Jotunn.Logger.LogInfo("Lava Worm added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Lava Worm failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void AddLoot()
    {
        MonsterRegistry.AddItem(ScaleName, "SerpentScale", prefab => VisualHelper.Recolor(prefab, Ember(0.35f)));
        MonsterRegistry.AddItem(TrophyName, "TrophySerpent", prefab => VisualHelper.Recolor(prefab, Ember(0.45f)));
    }

    // Charcoal, with the brightest parts of the texture turned to embers.
    private static Func<Color32, Color32> Ember(float value)
    {
        return pixel =>
        {
            Color.RGBToHSV(pixel, out _, out _, out float v);
            float heat = Mathf.SmoothStep(0f, 1f, (v - 0.55f) / 0.45f);
            Color32 result = Color.Lerp(new Color(v * value, v * value * 0.85f, v * value * 0.8f), new Color(1f, 0.45f, 0.1f), heat);
            result.a = pixel.a;
            return result;
        };
    }

    private static void AddWorm()
    {
        float scale = LavaWormSettings.Scale.Value;
        Material template = CreatureVisual.TemplateOf(PrefabManager.Instance.GetPrefab(TemplateCreature));
        // A negative force pulls: the hit's direction runs from the worm to its prey.
        CustomItem bite = MonsterRegistry.AddWeapon(BiteName, "lindorm_bite",
            new HitData.DamageTypes { m_pierce = LavaWormSettings.Damage.Value, m_fire = LavaWormSettings.FireDamage.Value },
            range: 3.2f * scale, height: 1.6f * scale, angle: 60f, rayWidth: 0.6f * scale, interval: 3f, force: -LavaWormSettings.Drag.Value);
        GameObject corpse = MonsterRegistry.CreateCorpse(CorpseName, Model, template, scale, sinkAfter: 3f, sinkSpeed: 0.8f, lifetime: 10f);

        CustomCreature creature = new(PrefabName, "Greyling", new CreatureConfig
        {
            Name = Translations.Token(NameKey),
            Faction = Character.Faction.Demon,
            DropConfigs = new[]
            {
                MonsterRegistry.Drop(ScaleName, 2, 4, 100f),
                MonsterRegistry.Drop(TrophyName, 1, 1, LavaWormSettings.TrophyChance.Value)
            }
        });
        GameObject prefab = creature.Prefab;
        Transform visual = MonsterRegistry.PrepareClone(prefab);
        Humanoid humanoid = MonsterRegistry.SetUpHumanoid(prefab, LavaWormSettings.Health.Value, bite, corpse, TemplateCreature);
        humanoid.m_name = Translations.Token(NameKey);
        humanoid.m_walkSpeed = 2.5f;
        humanoid.m_runSpeed = 6.5f;
        humanoid.m_speed = 2.5f;
        humanoid.m_turnSpeed = 80f;
        humanoid.m_runTurnSpeed = 120f;
        humanoid.m_tolerateFire = true;
        humanoid.m_tolerateSmoke = true;
        humanoid.m_damageModifiers = new HitData.DamageModifiers
        {
            m_blunt = HitData.DamageModifier.Normal,
            m_slash = HitData.DamageModifier.Resistant,
            m_pierce = HitData.DamageModifier.Resistant,
            m_chop = HitData.DamageModifier.Ignore,
            m_pickaxe = HitData.DamageModifier.Ignore,
            m_fire = HitData.DamageModifier.Immune,
            m_frost = HitData.DamageModifier.VeryWeak,
            m_lightning = HitData.DamageModifier.Normal,
            m_poison = HitData.DamageModifier.Resistant,
            m_spirit = HitData.DamageModifier.Immune
        };

        MonsterRegistry.SetCollider(prefab, new Vector3(0f, 1.3f, 0.1f) * scale, radius: 0.9f * scale, height: 2.6f * scale, eye: new Vector3(0f, 2.3f, 0.6f) * scale);
        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_viewRange = 35f;
        ai.m_hearRange = 35f;
        ai.m_alertRange = 25f;
        ai.m_enableHuntPlayer = true;
        ai.m_attackPlayerObjects = false;
        ai.m_fleeIfHurtWhenTargetCantBeReached = false;
        ai.m_fleeIfNotAlerted = false;
        ai.m_fleeIfLowHealth = 0f;
        ai.m_avoidFire = false;
        ai.m_afraidOfFire = false;
        ai.m_avoidLava = false;
        ai.m_randomMoveRange = 4f;

        GameObject model = MonsterRegistry.AttachVisual(visual, Model, template, scale);
        LevelEffects levels = visual.gameObject.AddComponent<LevelEffects>();
        levels.m_levelSetups = Enumerable.Range(1, Difficulty.CreatureStars.MaxLevel - 1)
            .Select(stars => new LevelEffects.LevelSetup { m_scale = 1f + Mathf.Min(stars, 2) * LavaWormSettings.StarScale.Value })
            .ToList();
        if (!VisualHelper.IsHeadless)
        {
            Dress(model);
            AddGlow(model.transform, scale);
        }

        if (LavaWormSettings.Sounds.Value)
        {
            MonsterRegistry.AddCreatureSounds(humanoid, ai, bite, "lavaworm");
            prefab.AddComponent<LoopSound>().clip = "amb_lavaworm";
        }

        prefab.AddComponent<LavaWormBurrow>();
        prefab.AddComponent<ArrivalSound>().clip = "arrival_lavaworm";
        CreatureManager.Instance.AddCreature(creature);
    }

    // Charred hide that glows where its texture is bright, as if the lava still runs in the cracks. The corpse shares
    // the materials, so it glows too.
    private static void Dress(GameObject model)
    {
        foreach (Material material in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(renderer => renderer.sharedMaterials))
        {
            if (material == null || !dressed.Add(material))
            {
                continue;
            }

            if (material.HasProperty("_Color"))
            {
                material.color *= Char;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Glow);
                if (material.HasProperty("_EmissionMap"))
                {
                    material.SetTexture("_EmissionMap", material.mainTexture);
                }
            }
        }
    }

    // A warm light round its head and body, rising and sinking with the model.
    private static void AddGlow(Transform model, float scale)
    {
        GameObject glow = new("lavaworm_glow");
        glow.transform.SetParent(model, false);
        // In the model's units, which it scales by the worm's size; the light's range is in metres.
        glow.transform.localPosition = new Vector3(0f, 2f, 0.5f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.45f, 0.12f);
        light.range = 7f * scale;
        light.intensity = 1.6f;
        light.shadows = LightShadows.None;
    }
}
