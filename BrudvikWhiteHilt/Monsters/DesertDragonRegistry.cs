using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Creates the Desert Dragon: the animated Red Dragon on a clone of the vanilla Hatchling, recoloured in sand and rust.
/// It flies over the Plains once Moder is slain and breathes a stream of fire at its prey. Also its corpse, which falls
/// out of the sky, its scales and its trophy.
/// </summary>
public static class DesertDragonRegistry
{
    /// <summary>Prefab name of the Desert Dragon.</summary>
    public const string DragonName = "WhiteHilt_DesertDragon";

    /// <summary>Prefab name of the Desert Dragon scale.</summary>
    public const string ScaleName = "WhiteHilt_DesertDragonScale";

    /// <summary>Prefab name of the Desert Dragon trophy.</summary>
    public const string TrophyName = "WhiteHilt_TrophyDesertDragon";

    private const string BaseCreature = "Hatchling";
    private const string BaseWeapon = "imp_fireball_attack";
    private const string BaseProjectile = "Imp_fireball_projectile";
    private const string FlameName = "WhiteHilt_DesertDragonFlame";
    private const string ProjectileName = "WhiteHilt_DesertDragonFlame_projectile";
    private const string CorpseName = "WhiteHilt_DesertDragonCorpse";
    private const string Model = "desertdragon";
    private const string AttackTrigger = "dragon_flame";
    private const string MouthBone = "head2_13";
    private const float FlameSpeed = 25f;

    // The Surtling fireball's trailing flames are about 0.7 m wide; at 0.7 times that they fill the dragon's open mouth.
    private const float FireballFlameWidth = 0.7f;
    private const float MouthFlameSize = 0.7f;

    // The downloaded dragon is about 107 units from wingtip to wingtip; this makes it about 8 m.
    private const float BaseScale = 0.075f;

    // In model units, measured in Unity: the middle of its body in flight, and how much higher the death pose starts.
    private static readonly Vector3 bodyCentre = new(0f, 27f, -16f);
    private const float DeathPoseRise = 69f;

    private static SpawnSystem.SpawnData spawn;

    /// <summary>
    /// Registers the translations, and the prefabs once the vanilla creatures can be cloned.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("enemy_whitehilt_desertdragon", "Desert Dragon");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(ScaleName), "Desert Dragon Scale",
            "A sand-coloured scale, still warm long after the dragon fell from the sky.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(TrophyName), "Desert Dragon Trophy",
            "It still smells of smoke.");
        CreatureManager.OnVanillaCreaturesAvailable += Add;
    }

    /// <summary>
    /// Applies changed or server-synced config values to the spawn.
    /// </summary>
    public static void ApplyConfig()
    {
        if (spawn == null)
        {
            return;
        }

        spawn.m_enabled = MonsterSettings.DragonEnabled.Value && MonsterSettings.DragonSpawnChance.Value > 0f;
        spawn.m_requiredGlobalKey = MonsterSettings.DragonRequiredKey.Value?.Trim() ?? string.Empty;
        spawn.m_spawnChance = MonsterSettings.DragonSpawnChance.Value;
        spawn.m_spawnInterval = MonsterSettings.DragonSpawnSeconds.Value;
        spawn.m_maxSpawned = MonsterSettings.DragonSpawnMax.Value;
    }

    private static void Add()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Add;
        try
        {
            MonsterRegistry.AddItem(ScaleName, "SerpentScale", prefab => VisualHelper.Recolor(prefab, SandTint));
            MonsterRegistry.AddItem(TrophyName, "TrophyHatchling", prefab => VisualHelper.Recolor(prefab, SandTint));
            AddDragon();
            ApplyConfig();
            Jotunn.Logger.LogInfo("Desert Dragon added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Desert Dragon failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void AddDragon()
    {
        float size = MonsterSettings.DragonScale.Value;
        float scale = BaseScale * size;
        Vector3 modelPosition = new Vector3(0f, 1.1f * size, 0f) - bodyCentre * scale;
        Material template = CreatureVisual.TemplateOf(PrefabManager.Instance.GetPrefab(BaseCreature));
        CustomItem flame = AddFlame();
        GameObject corpse = CreateCorpse(template, scale, modelPosition);

        CustomCreature creature = new(DragonName, BaseCreature, new CreatureConfig
        {
            Name = Translations.Token("enemy_whitehilt_desertdragon"),
            Faction = Character.Faction.PlainsMonsters,
            DropConfigs = new[]
            {
                MonsterRegistry.Drop(ScaleName, 2, 3, 100f),
                MonsterRegistry.Drop("SurtlingCore", 1, 1, 50f),
                MonsterRegistry.Drop(TrophyName, 1, 1, MonsterSettings.DragonTrophyChance.Value)
            },
            SpawnConfigs = new[]
            {
                new SpawnConfig
                {
                    Name = $"{DragonName}_Plains",
                    Biome = Heightmap.Biome.Plains,
                    RequiredGlobalKey = MonsterSettings.DragonRequiredKey.Value,
                    SpawnChance = MonsterSettings.DragonSpawnChance.Value,
                    SpawnInterval = MonsterSettings.DragonSpawnSeconds.Value,
                    MaxSpawned = MonsterSettings.DragonSpawnMax.Value,
                    SpawnDistance = 50f,
                    MinAltitude = 1f,
                    GroundOffset = MonsterSettings.DragonFlyHeightMin.Value,
                    MinLevel = 1,
                    MaxLevel = 2
                }
            }
        });
        spawn = creature.Spawns.FirstOrDefault();
        GameObject prefab = creature.Prefab;
        Transform visual = MonsterRegistry.PrepareClone(prefab, "Attack collider");
        Humanoid humanoid = MonsterRegistry.SetUpHumanoid(prefab, MonsterSettings.DragonHealth.Value, flame, corpse, BaseCreature);
        humanoid.m_name = Translations.Token("enemy_whitehilt_desertdragon");
        humanoid.m_flying = true;
        humanoid.m_flySlowSpeed = MonsterSettings.DragonFlySpeed.Value * 0.45f;
        humanoid.m_flyFastSpeed = MonsterSettings.DragonFlySpeed.Value;
        humanoid.m_flyTurnSpeed = 60f;
        humanoid.m_damageModifiers = new HitData.DamageModifiers
        {
            m_blunt = HitData.DamageModifier.Normal,
            m_slash = HitData.DamageModifier.Normal,
            m_pierce = HitData.DamageModifier.Normal,
            m_chop = HitData.DamageModifier.Ignore,
            m_pickaxe = HitData.DamageModifier.Ignore,
            m_fire = HitData.DamageModifier.Immune,
            m_frost = HitData.DamageModifier.Weak,
            m_lightning = HitData.DamageModifier.Normal,
            m_poison = HitData.DamageModifier.Resistant,
            m_spirit = HitData.DamageModifier.Immune
        };

        MonsterRegistry.SetCollider(prefab, new Vector3(0f, 1.1f, 0f) * size, radius: 1.1f * size, height: 2.2f * size, eye: new Vector3(0f, 1.1f, 2.2f) * size);
        // The Hatchling's long hitbox along the body; it does not touch the ground.
        Transform hitbox = prefab.transform.Find("Attack collider");
        CapsuleCollider body = hitbox != null ? hitbox.GetComponent<CapsuleCollider>() : null;
        if (body != null)
        {
            hitbox.localPosition = new Vector3(0f, 1.1f, -0.4f) * size;
            hitbox.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.center = Vector3.zero;
            body.direction = 1;
            body.radius = 1f * size;
            body.height = 7f * size;
        }

        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_randomFly = false;
        ai.m_flyAltitudeMin = MonsterSettings.DragonFlyHeightMin.Value;
        ai.m_flyAltitudeMax = Mathf.Max(MonsterSettings.DragonFlyHeightMin.Value, MonsterSettings.DragonFlyHeightMax.Value);
        ai.m_viewRange = 50f;
        ai.m_hearRange = 50f;
        ai.m_alertRange = 40f;
        ai.m_attackPlayerObjects = false;
        ai.m_avoidFire = false;
        ai.m_afraidOfFire = false;
        ai.m_fleeIfHurtWhenTargetCantBeReached = false;
        ai.m_circulateWhileChargingFlying = true;

        GameObject model = MonsterRegistry.AttachVisual(visual, Model, template, scale);
        model.transform.localPosition = modelPosition;
        Dress(model);
        if (MonsterSettings.DragonSounds.Value)
        {
            MonsterRegistry.AddCreatureSounds(humanoid, ai, flame, "dragon");
        }
        prefab.AddComponent<DragonFire>();
        CreatureManager.Instance.AddCreature(creature);
    }

    // A dense stream of Surtling flames from the mouth, aimed at the prey below, widening as they fly.
    private static CustomItem AddFlame()
    {
        float range = MonsterSettings.DragonBreathRange.Value;
        float width = MonsterSettings.DragonBreathWidth.Value;
        GameObject projectile = PrefabManager.Instance.CreateClonedPrefab(ProjectileName, BaseProjectile);
        // Flames fly a little past the breath range, then die out.
        projectile.GetComponent<Projectile>().m_ttl = range * 1.5f / FlameSpeed;
        Transform ball = projectile.transform.Find("Sphere");
        if (ball != null)
        {
            ball.gameObject.SetActive(false);
        }

        DragonFlame growth = projectile.AddComponent<DragonFlame>();
        growth.StartSize = MouthFlameSize;
        growth.EndSize = width / FireballFlameWidth;
        growth.StartRadius = 0.3f;
        growth.EndRadius = width / 2f;
        // Full width halfway through the range: about where the fire meets the ground below a circling dragon.
        growth.GrowSeconds = range * 0.5f / FlameSpeed;
        PrefabManager.Instance.AddPrefab(new CustomPrefab(projectile, false));

        CustomItem weapon = new(FlameName, BaseWeapon);
        ItemDrop.ItemData.SharedData shared = weapon.ItemDrop.m_itemData.m_shared;
        shared.m_damages = new HitData.DamageTypes { m_fire = MonsterSettings.DragonFireDamage.Value };
        shared.m_aiAttackInterval = MonsterSettings.DragonBreathSeconds.Value;
        shared.m_aiAttackRange = MonsterSettings.DragonBreathRange.Value;
        shared.m_aiAttackRangeMin = 3f;
        shared.m_aiAttackMaxAngle = 20f;
        shared.m_aiWhenFlying = true;
        shared.m_aiWhenWalking = true;
        Attack attack = shared.m_attack;
        attack.m_attackAnimation = AttackTrigger;
        attack.m_attackChainLevels = 1;
        attack.m_attackRandomAnimations = 0;
        attack.m_attackOriginJoint = MouthBone;
        attack.m_attackRange = 0.2f;
        attack.m_attackHeight = 0f;
        attack.m_attackProjectile = projectile;
        attack.m_projectileVel = FlameSpeed;
        attack.m_projectileVelMin = FlameSpeed;
        attack.m_projectileAccuracy = 4f;
        attack.m_projectileAccuracyMin = 4f;
        attack.m_projectiles = 1;
        attack.m_projectileBursts = MonsterSettings.DragonFlames.Value;
        attack.m_burstInterval = 0.07f;
        ItemManager.Instance.AddItem(weapon);
        return weapon;
    }

    private static GameObject CreateCorpse(Material template, float scale, Vector3 modelPosition)
    {
        GameObject prefab = MonsterRegistry.CreateCorpse(CorpseName, Model, template, scale, sinkAfter: 9f, sinkSpeed: 0.6f, lifetime: 16f);
        MonsterCorpse corpse = prefab.GetComponent<MonsterCorpse>();
        corpse.Visual.localPosition = modelPosition;
        corpse.Falls = true;
        corpse.FallStartOffset = -DeathPoseRise * scale;
        corpse.FallDelay = 1.2f;
        corpse.FallSeconds = 2.5f;
        Dress(corpse.Visual.gameObject);
        return prefab;
    }

    // Sand and rust instead of red and yellow; the thin wing membranes are seen from both sides.
    private static void Dress(GameObject model)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        VisualHelper.Recolor(model, DragonSkin);
        foreach (Material material in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(renderer => renderer.sharedMaterials))
        {
            if (material != null && material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }
        }
    }

    private static Color32 DragonSkin(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        bool red = hue < 0.07f || hue > 0.85f;
        bool yellow = !red && hue < 0.4f;
        if (saturation < 0.15f || (!red && !yellow))
        {
            return pixel;
        }

        Color32 result = red
            ? Color.HSVToRGB(0.085f, saturation * 0.55f, Mathf.Clamp01(value * 1.15f))
            : Color.HSVToRGB(0.055f, Mathf.Clamp01(saturation * 0.9f), value * 0.85f);
        result.a = pixel.a;
        return result;
    }

    private static Color32 SandTint(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out _, out float saturation, out float value);
        Color32 result = Color.HSVToRGB(0.08f, Mathf.Clamp01(0.25f + saturation * 0.4f), value);
        result.a = pixel.a;
        return result;
    }
}
