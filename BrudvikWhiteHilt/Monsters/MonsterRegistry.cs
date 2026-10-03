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
/// Creates the Lindorm (the animated Worm Monster) and the giant spider (the animated Wolf Spider) on clones of the vanilla
/// Greyling, their corpses, their loot, the spider's web effect and the spider nest that spawns them in the Black Forest.
/// </summary>
public static class MonsterRegistry
{
    /// <summary>Prefab name of the Lindorm.</summary>
    public const string LindormName = "WhiteHilt_Lindorm";

    /// <summary>Prefab name of the giant spider.</summary>
    public const string SpiderName = "WhiteHilt_GiantSpider";

    /// <summary>Prefab name of the spider nest.</summary>
    public const string NestName = "WhiteHilt_SpiderNest";

    /// <summary>Prefab name of the spider silk.</summary>
    public const string SilkName = "WhiteHilt_SpiderSilk";

    /// <summary>Prefab name of the poison gland.</summary>
    public const string GlandName = "WhiteHilt_PoisonGland";

    /// <summary>Prefab name of the Lindorm scale.</summary>
    public const string ScaleName = "WhiteHilt_LindormScale";

    /// <summary>Prefab name of the Lindorm trophy.</summary>
    public const string LindormTrophyName = "WhiteHilt_TrophyLindorm";

    /// <summary>Prefab name of the giant spider trophy.</summary>
    public const string SpiderTrophyName = "WhiteHilt_TrophyGiantSpider";

    /// <summary>Name of the web status effect.</summary>
    public const string WebEffectName = "WhiteHilt_SpiderWeb";

    private const string BaseCreature = "Greyling";
    private const string BaseWeapon = "Greyling_attack";
    private const string LindormBite = "WhiteHilt_LindormBite";
    private const string SpiderBite = "WhiteHilt_SpiderBite";
    private const string LindormCorpseName = "WhiteHilt_LindormCorpse";
    private const string SpiderCorpseName = "WhiteHilt_GiantSpiderCorpse";

    // The downloaded spider is 3 cm across; this makes it about 1.6 m.
    private const float SpiderBaseScale = 50f;

    private static ZoneSystem.ZoneVegetation nestVegetation;
    private static SpawnSystem.SpawnData nightSpawn;
    private static readonly Dictionary<string, GameObject> creatureSounds = new();

    /// <summary>
    /// Registers the translations, and the prefabs once the vanilla creatures can be cloned.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("enemy_whitehilt_lindorm", "Lindorm");
        Translations.AddEnglish("enemy_whitehilt_giantspider", "Giant Spider");
        Translations.AddEnglish("enemy_whitehilt_spidernest", "Spider Nest");
        Translations.AddEnglish("msg_whitehilt_lindorm_rises", "The ground heaves beneath you...");
        Translations.AddEnglish("se_whitehilt_spiderweb", "Webbed");
        Translations.AddEnglish("se_whitehilt_spiderweb_tooltip", "Sticky web slows you down.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(SilkName), "Spider Silk",
            "Strong, sticky thread from a giant spider. It holds better than any rope you have twisted.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(GlandName), "Poison Gland",
            "A swollen sac of green venom. Handle it with gloves, and keep it away from the stew.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(ScaleName), "Lindorm Scale",
            "A dark, ridged plate from the Lindorm's hide. Hard as iron and light as bark.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(LindormTrophyName), "Lindorm Trophy",
            "The ground heaved, and you were still standing when it stopped.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(SpiderTrophyName), "Giant Spider Trophy",
            "Eight eyes, and none of them saw you coming.");
        CreatureManager.OnVanillaCreaturesAvailable += Add;
    }

    /// <summary>
    /// Applies changed or server-synced config values to the nest vegetation.
    /// </summary>
    public static void ApplyConfig()
    {
        if (nightSpawn != null)
        {
            nightSpawn.m_enabled = MonsterSettings.SpiderEnabled.Value && MonsterSettings.NightSpawnChance.Value > 0f;
            nightSpawn.m_spawnChance = MonsterSettings.NightSpawnChance.Value;
            nightSpawn.m_spawnInterval = MonsterSettings.NightSpawnSeconds.Value;
            nightSpawn.m_maxSpawned = MonsterSettings.NightSpawnMax.Value;
        }

        if (nestVegetation == null)
        {
            return;
        }

        nestVegetation.m_enable = MonsterSettings.SpiderEnabled.Value && MonsterSettings.NestChancePerZone.Value > 0f;
        nestVegetation.m_max = MonsterSettings.NestChancePerZone.Value;
    }

    private static void Add()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Add;
        Try("Monster loot", AddLoot);
        Try("Lindorm", AddLindorm);
        Try("Giant spider", () =>
        {
            GameObject spider = AddSpider();
            AddNest(spider);
        });
    }

    private static void Try(string what, Action action)
    {
        try
        {
            action();
            Jotunn.Logger.LogInfo($"{what} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{what} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void AddLoot()
    {
        AddItem(SilkName, "LinenThread", prefab => VisualHelper.Tint(prefab, new Color(0.95f, 0.93f, 0.88f)));
        AddItem(GlandName, "FreezeGland", prefab =>
        {
            VisualHelper.RecolorHue(prefab, 0.45f, 0.75f, 0.27f, 1f, 0.85f);
            foreach (Light light in prefab.GetComponentsInChildren<Light>(true))
            {
                light.color = new Color(0.45f, 1f, 0.3f);
            }
        });
        AddItem(ScaleName, "SerpentScale", prefab => VisualHelper.Recolor(prefab, Darken(0.62f, 0.35f, 0.55f)));
        AddItem(LindormTrophyName, "TrophySerpent", prefab => VisualHelper.Recolor(prefab, Darken(0.62f, 0.3f, 0.6f)));
        AddItem(SpiderTrophyName, "TrophySeeker", prefab => VisualHelper.Recolor(prefab, Darken(0.08f, 0.55f, 0.6f)));
    }

    private static Func<Color32, Color32> Darken(float hue, float saturation, float value)
    {
        return pixel =>
        {
            Color.RGBToHSV(pixel, out _, out float s, out float v);
            Color32 result = Color.HSVToRGB(hue, Mathf.Clamp01(s * 0.4f + saturation * 0.6f), Mathf.Clamp01(v * value + 0.04f));
            result.a = pixel.a;
            return result;
        };
    }

    internal static void AddItem(string name, string copyFrom, Action<GameObject> recolor)
    {
        CustomItem item = new(name, copyFrom);
        ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
        string key = Translations.ItemKey(name);
        shared.m_name = Translations.Token(key);
        shared.m_description = Translations.Token($"{key}_description");
        if (!VisualHelper.IsHeadless)
        {
            recolor(item.ItemPrefab);
            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }

        ItemManager.Instance.AddItem(item);
    }

    private static void AddLindorm()
    {
        float scale = MonsterSettings.LindormScale.Value;
        Material template = CreatureVisual.TemplateOf(PrefabManager.Instance.GetPrefab("Serpent"));
        CustomItem bite = AddWeapon(LindormBite, "lindorm_bite", new HitData.DamageTypes { m_pierce = MonsterSettings.LindormDamage.Value },
            range: 3.2f * scale, height: 1.6f * scale, angle: 60f, rayWidth: 0.6f * scale, interval: 2.5f);
        GameObject corpse = CreateCorpse(LindormCorpseName, "lindorm", template, scale, sinkAfter: 3f, sinkSpeed: 0.8f, lifetime: 10f);

        CustomCreature creature = new(LindormName, BaseCreature, new CreatureConfig
        {
            Name = Translations.Token("enemy_whitehilt_lindorm"),
            Faction = Character.Faction.ForestMonsters,
            DropConfigs = new[]
            {
                Drop(ScaleName, 2, 4, 100f),
                Drop("Entrails", 1, 3, 100f),
                Drop(LindormTrophyName, 1, 1, MonsterSettings.LindormTrophyChance.Value)
            }
        });
        GameObject prefab = creature.Prefab;
        Transform visual = PrepareClone(prefab);
        Humanoid humanoid = SetUpHumanoid(prefab, MonsterSettings.LindormHealth.Value, bite, corpse, "Serpent");
        humanoid.m_name = Translations.Token("enemy_whitehilt_lindorm");
        humanoid.m_walkSpeed = 2.5f;
        humanoid.m_runSpeed = 6f;
        humanoid.m_speed = 2.5f;
        humanoid.m_turnSpeed = 80f;
        humanoid.m_runTurnSpeed = 120f;
        humanoid.m_damageModifiers = new HitData.DamageModifiers
        {
            m_blunt = HitData.DamageModifier.Normal,
            m_slash = HitData.DamageModifier.Normal,
            m_pierce = HitData.DamageModifier.Resistant,
            m_chop = HitData.DamageModifier.Ignore,
            m_pickaxe = HitData.DamageModifier.Ignore,
            m_fire = HitData.DamageModifier.Weak,
            m_frost = HitData.DamageModifier.Normal,
            m_lightning = HitData.DamageModifier.Normal,
            m_poison = HitData.DamageModifier.Resistant,
            m_spirit = HitData.DamageModifier.Immune
        };

        SetCollider(prefab, new Vector3(0f, 1.3f, 0.1f) * scale, radius: 0.9f * scale, height: 2.6f * scale, eye: new Vector3(0f, 2.3f, 0.6f) * scale);
        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_viewRange = 35f;
        ai.m_hearRange = 35f;
        ai.m_alertRange = 25f;
        ai.m_enableHuntPlayer = true;
        ai.m_attackPlayerObjects = true;
        ai.m_fleeIfHurtWhenTargetCantBeReached = false;
        ai.m_fleeIfNotAlerted = false;
        ai.m_avoidFire = false;
        ai.m_afraidOfFire = false;
        ai.m_randomMoveRange = 6f;

        AttachVisual(visual, "lindorm", template, scale);
        LevelEffects levels = visual.gameObject.AddComponent<LevelEffects>();
        levels.m_levelSetups = Enumerable.Range(1, Difficulty.CreatureStars.MaxLevel - 1)
            .Select(stars => new LevelEffects.LevelSetup { m_scale = 1f + Mathf.Min(stars, 2) * MonsterSettings.LindormStarScale.Value })
            .ToList();
        if (MonsterSettings.LindormSounds.Value)
        {
            AddCreatureSounds(humanoid, ai, bite, "lindorm");
        }
        prefab.AddComponent<LindormBurrow>();
        CreatureManager.Instance.AddCreature(creature);
    }

    private static GameObject AddSpider()
    {
        float scale = SpiderBaseScale * MonsterSettings.SpiderScale.Value;
        float size = MonsterSettings.SpiderScale.Value;
        Material template = CreatureVisual.TemplateOf(PrefabManager.Instance.GetPrefab("Seeker"));
        StatusEffect web = AddWebEffect();
        CustomItem bite = AddWeapon(SpiderBite, "spider_bite",
            new HitData.DamageTypes { m_pierce = MonsterSettings.SpiderDamage.Value, m_poison = MonsterSettings.SpiderPoison.Value },
            range: 1.6f * size, height: 0.8f * size, angle: 70f, rayWidth: 0.4f * size, interval: 2f);
        bite.ItemDrop.m_itemData.m_shared.m_attackStatusEffect = web;
        bite.ItemDrop.m_itemData.m_shared.m_attackStatusEffectChance = 1f;
        GameObject corpse = CreateCorpse(SpiderCorpseName, "giantspider", template, scale, sinkAfter: 8f, sinkSpeed: 0.15f, lifetime: 14f);

        CustomCreature creature = new(SpiderName, BaseCreature, new CreatureConfig
        {
            Name = Translations.Token("enemy_whitehilt_giantspider"),
            Faction = Character.Faction.ForestMonsters,
            DropConfigs = new[]
            {
                Drop(SilkName, 1, 2, 100f),
                Drop(GlandName, 1, 1, 50f),
                Drop(SpiderTrophyName, 1, 1, MonsterSettings.SpiderTrophyChance.Value)
            },
            SpawnConfigs = new[]
            {
                new SpawnConfig
                {
                    Name = $"{SpiderName}_Night",
                    Biome = Heightmap.Biome.BlackForest,
                    SpawnAtDay = false,
                    SpawnAtNight = true,
                    SpawnInForest = true,
                    SpawnOutsideForest = false,
                    SpawnChance = MonsterSettings.NightSpawnChance.Value,
                    SpawnInterval = MonsterSettings.NightSpawnSeconds.Value,
                    MaxSpawned = MonsterSettings.NightSpawnMax.Value,
                    SpawnDistance = 40f,
                    MinAltitude = 1f,
                    MaxTilt = 30f,
                    MinLevel = 1,
                    MaxLevel = 2
                }
            }
        });
        nightSpawn = creature.Spawns.FirstOrDefault();
        GameObject prefab = creature.Prefab;
        Transform visual = PrepareClone(prefab);
        Humanoid humanoid = SetUpHumanoid(prefab, MonsterSettings.SpiderHealth.Value, bite, corpse, "Seeker");
        humanoid.m_name = Translations.Token("enemy_whitehilt_giantspider");
        humanoid.m_walkSpeed = 2.5f;
        humanoid.m_runSpeed = 7f;
        humanoid.m_speed = 2.5f;
        humanoid.m_turnSpeed = 250f;
        humanoid.m_runTurnSpeed = 300f;
        humanoid.m_canSwim = false;
        humanoid.m_damageModifiers = new HitData.DamageModifiers
        {
            m_blunt = HitData.DamageModifier.Weak,
            m_slash = HitData.DamageModifier.Normal,
            m_pierce = HitData.DamageModifier.Normal,
            m_chop = HitData.DamageModifier.Ignore,
            m_pickaxe = HitData.DamageModifier.Ignore,
            m_fire = HitData.DamageModifier.Weak,
            m_frost = HitData.DamageModifier.Normal,
            m_lightning = HitData.DamageModifier.Normal,
            m_poison = HitData.DamageModifier.Immune,
            m_spirit = HitData.DamageModifier.Normal
        };

        SetCollider(prefab, new Vector3(0f, 0.45f, 0f) * size, radius: 0.45f * size, height: 0.9f * size, eye: new Vector3(0f, 0.45f, 0.35f) * size);
        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_viewRange = 25f;
        ai.m_hearRange = 20f;
        ai.m_alertRange = 15f;
        ai.m_avoidWater = true;
        ai.m_afraidOfFire = true;
        ai.m_avoidFire = true;
        ai.m_circulateWhileCharging = true;
        ai.m_fleeIfNotAlerted = false;

        AttachVisual(visual, "giantspider", template, scale);
        LevelEffects levels = visual.gameObject.AddComponent<LevelEffects>();
        levels.m_levelSetups = Enumerable.Range(1, Difficulty.CreatureStars.MaxLevel - 1)
            .Select(stars => new LevelEffects.LevelSetup { m_scale = 1f + Mathf.Min(stars, 2) * MonsterSettings.SpiderStarScale.Value })
            .ToList();
        if (MonsterSettings.SpiderSounds.Value)
        {
            AddCreatureSounds(humanoid, ai, bite, "spider", "bite");
        }
        CreatureManager.Instance.AddCreature(creature);
        return prefab;
    }

    internal static void AddCreatureSounds(Humanoid humanoid, MonsterAI ai, CustomItem weapon, string prefix, string attack = "attack")
    {
        GameObject source = (ai.m_alertedEffects.m_effectPrefabs ?? Array.Empty<EffectList.EffectData>()).Select(effect => effect.m_prefab)
            .FirstOrDefault(effect => effect != null && effect.GetComponentInChildren<ZSFX>(true) != null);
        source ??= PrefabManager.Instance.GetPrefab("Wolf")?.GetComponent<MonsterAI>()?.m_alertedEffects.m_effectPrefabs
            ?.Select(effect => effect.m_prefab)
            .FirstOrDefault(effect => effect != null && effect.GetComponentInChildren<ZSFX>(true) != null);
        if (source == null)
        {
            Jotunn.Logger.LogWarning($"{prefix}: no alert sound prefab to clone");
            return;
        }

        Dictionary<string, GameObject> sounds = new();
        foreach (string action in new[] { "idle", "alert", attack, "hit", "death" })
        {
            string name = prefix + action;
            if (!creatureSounds.TryGetValue(name, out GameObject effect))
            {
                AudioClip clip = ForagingAssets.LoadAudio(name);
                effect = PrefabManager.Instance.CreateClonedPrefab("sfx_whitehilt_" + name, source);
                effect.GetComponentInChildren<ZSFX>(true).m_audioClips = new[] { clip };
                creatureSounds[name] = effect;
            }
            sounds[action] = effect;
        }

        ai.m_idleSound = ReplaceSound(ai.m_idleSound, sounds["idle"]);
        ai.m_alertedEffects = ReplaceSound(ai.m_alertedEffects, sounds["alert"]);
        humanoid.m_hitEffects = ReplaceSound(humanoid.m_hitEffects, sounds["hit"]);
        humanoid.m_critHitEffects = ReplaceSound(humanoid.m_critHitEffects, sounds["hit"]);
        humanoid.m_deathEffects = ReplaceSound(humanoid.m_deathEffects, sounds["death"]);
        ItemDrop.ItemData.SharedData shared = weapon.ItemDrop.m_itemData.m_shared;
        shared.m_startEffect = RemoveSounds(shared.m_startEffect);
        shared.m_attack.m_startEffect = RemoveSounds(shared.m_attack.m_startEffect);
        shared.m_attack.m_triggerEffect = RemoveSounds(shared.m_attack.m_triggerEffect);
        shared.m_triggerEffect = ReplaceSound(shared.m_triggerEffect, sounds[attack]);
    }

    private static EffectList ReplaceSound(EffectList effects, GameObject sound)
    {
        return new EffectList
        {
            m_effectPrefabs = RemoveSounds(effects).m_effectPrefabs
                .Append(new EffectList.EffectData { m_prefab = sound, m_enabled = true })
                .ToArray()
        };
    }

    private static EffectList RemoveSounds(EffectList effects)
    {
        return new EffectList
        {
            m_effectPrefabs = (effects.m_effectPrefabs ?? Array.Empty<EffectList.EffectData>())
                .Where(effect => effect.m_prefab != null && effect.m_prefab.GetComponentInChildren<ZSFX>(true) == null)
                .ToArray()
        };
    }

    private static StatusEffect AddWebEffect()
    {
        SE_Stats web = ScriptableObject.CreateInstance<SE_Stats>();
        web.name = WebEffectName;
        web.m_name = Translations.Token("se_whitehilt_spiderweb");
        web.m_tooltip = Translations.Token("se_whitehilt_spiderweb_tooltip");
        web.m_ttl = MonsterSettings.SpiderWebSeconds.Value;
        web.m_speedModifier = MonsterSettings.SpiderWebSeconds.Value > 0f ? -0.5f : 0f;
        web.m_icon = PrefabManager.Instance.GetPrefab(SilkName)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(web, fixReference: false));
        return web;
    }

    private static void AddNest(GameObject spider)
    {
        GameObject nest = PrefabManager.Instance.CreateClonedPrefab(NestName, "Spawner_GreydwarfNest");
        Destructible destructible = nest.GetComponent<Destructible>();
        destructible.m_health = MonsterSettings.NestHealth.Value;

        SpawnArea area = nest.GetComponent<SpawnArea>();
        area.m_prefabs = new List<SpawnArea.SpawnData> { new() { m_prefab = spider, m_weight = 1f, m_minLevel = 1, m_maxLevel = 2 } };
        area.m_maxNear = MonsterSettings.NestMaxNear.Value;
        area.m_maxTotal = MonsterSettings.NestMaxNear.Value * 3;
        area.m_spawnIntervalSec = MonsterSettings.NestSpawnSeconds.Value;
        area.m_triggerDistance = 45f;
        area.m_nearRadius = 20f;
        area.m_spawnRadius = 3.5f;
        area.m_levelupChance = MonsterSettings.NestLevelUpChance.Value;

        HoverText hover = nest.GetComponent<HoverText>();
        if (hover != null)
        {
            hover.m_text = Translations.Token("enemy_whitehilt_spidernest");
        }

        GameObject silk = PrefabManager.Instance.GetPrefab(SilkName);
        DropOnDestroyed drops = nest.GetComponent<DropOnDestroyed>();
        if (drops != null && silk != null)
        {
            drops.m_dropWhenDestroyed = new DropTable
            {
                m_drops = new List<DropTable.DropData> { new() { m_item = silk, m_stackMin = 3, m_stackMax = 5, m_weight = 1f } },
                m_dropMin = 1,
                m_dropMax = 1,
                m_dropChance = 1f
            };
        }

        // The greydwarf magic (glowing trails, smoke, light and whispers) does not belong in a spider's nest.
        foreach (string child in new[] { "particles", "sfx" })
        {
            Transform part = nest.transform.Find(child);
            if (part != null)
            {
                UnityEngine.Object.DestroyImmediate(part.gameObject);
            }
        }

        if (!VisualHelper.IsHeadless)
        {
            DressNest(nest);
        }

        CustomVegetation vegetation = new(nest, false, new VegetationConfig
        {
            Biome = Heightmap.Biome.BlackForest,
            Min = 1,
            Max = MonsterSettings.NestChancePerZone.Value,
            // One try per zone (the default) often fails the checks below, which made nests far rarer than the chance.
            ForcePlacement = true,
            GroupSizeMin = 1,
            GroupSizeMax = 1,
            MinAltitude = 2f,
            MaxTilt = 20f,
            InForest = true,
            ForestThresholdMin = 0f,
            ForestThresholdMax = 1.15f,
            BlockCheck = true
        });
        ZoneManager.Instance.AddCustomVegetation(vegetation);
        nestVegetation = vegetation.Vegetation;
        ApplyConfig();
        OldLand.OldLandFiller.Register(nestVegetation, 3f);
    }

    // Pale, web-covered pile with a ring of eggs around its foot.
    private static void DressNest(GameObject nest)
    {
        Transform pile = nest.transform.Find("Pile") ?? nest.transform;
        VisualHelper.Recolor(pile.gameObject, pixel =>
        {
            Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
            Color32 webbed = Color.HSVToRGB(hue, saturation * 0.15f, Mathf.Clamp01(value * 0.55f + 0.38f));
            webbed.a = pixel.a;
            return webbed;
        });

        GameObject egg = PrefabManager.Instance.GetPrefab("ChickenEgg");
        MeshFilter eggMesh = egg != null ? egg.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(filter => filter.sharedMesh != null) : null;
        MeshRenderer eggRenderer = eggMesh != null ? eggMesh.GetComponent<MeshRenderer>() : null;
        if (eggMesh == null || eggRenderer == null)
        {
            return;
        }

        const int Eggs = 7;
        float eggScale = 0.55f / eggMesh.sharedMesh.bounds.size.y;
        for (int i = 0; i < Eggs; i++)
        {
            float angle = (i + 0.3f * Mathf.Sin(i * 2.1f)) * Mathf.PI * 2f / Eggs;
            float radius = 2.3f + 0.3f * Mathf.Cos(i * 1.7f);
            Vector3 position = new(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
            Quaternion rotation = Quaternion.Euler(10f * Mathf.Sin(i * 3.1f), i * 47f, 12f * Mathf.Cos(i * 2.3f));
            GameObject model = VisualHelper.CreateModel(nest.transform, eggMesh.sharedMesh, null, eggRenderer, position, rotation, eggScale * (0.85f + 0.1f * (i % 3)));
            model.name = $"egg {i}";
        }
    }

    private static CustomItem AddWeapon(string name, string trigger, HitData.DamageTypes damages, float range, float height, float angle, float rayWidth, float interval)
    {
        CustomItem weapon = new(name, BaseWeapon);
        ItemDrop.ItemData.SharedData shared = weapon.ItemDrop.m_itemData.m_shared;
        shared.m_damages = damages;
        shared.m_attackForce = 40f;
        shared.m_aiAttackRange = range * 0.85f;
        shared.m_aiAttackInterval = interval;
        shared.m_aiAttackMaxAngle = 25f;
        Attack attack = shared.m_attack;
        attack.m_attackAnimation = trigger;
        // Chains and random variants would append a number to the trigger name.
        attack.m_attackChainLevels = 1;
        attack.m_attackRandomAnimations = 0;
        attack.m_attackRange = range;
        attack.m_attackHeight = height;
        attack.m_attackAngle = angle;
        attack.m_attackRayWidth = rayWidth;
        attack.m_hitTerrain = false;
        ItemManager.Instance.AddItem(weapon);
        return weapon;
    }

    internal static GameObject CreateCorpse(string name, string creature, Material template, float scale, float sinkAfter, float sinkSpeed, float lifetime)
    {
        GameObject prefab = PrefabManager.Instance.CreateEmptyPrefab(name);
        foreach (Component part in prefab.GetComponents<Component>().Where(part => part is MeshRenderer || part is MeshFilter || part is Collider))
        {
            UnityEngine.Object.DestroyImmediate(part);
        }

        GameObject visual = CreatureVisual.Attach(prefab.transform, creature, template, Vector3.zero, Quaternion.identity, scale);
        MonsterCorpse corpse = prefab.AddComponent<MonsterCorpse>();
        corpse.Visual = visual.transform;
        corpse.SinkAfter = sinkAfter;
        corpse.SinkSpeed = sinkSpeed;
        corpse.Lifetime = lifetime;
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
        return prefab;
    }

    // The base creature's own look, feet and level sizes go; its body, AI, sync and drops stay, and so do the children in keep.
    internal static Transform PrepareClone(GameObject prefab, params string[] keep)
    {
        foreach (FootStep step in prefab.GetComponents<FootStep>())
        {
            UnityEngine.Object.DestroyImmediate(step);
        }

        foreach (Transform child in prefab.transform.Cast<Transform>().ToArray())
        {
            if (child.name != "Visual" && child.name != "EyePos" && !keep.Contains(child.name))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        Transform visual = prefab.transform.Find("Visual");
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;
        // Character finds the first Animator in its children, so the Greyling's must go before ours is added.
        foreach (MonoBehaviour behaviour in visual.GetComponents<MonoBehaviour>())
        {
            UnityEngine.Object.DestroyImmediate(behaviour);
        }

        UnityEngine.Object.DestroyImmediate(visual.GetComponent<LODGroup>());
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<Animator>());
        for (int i = visual.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(visual.GetChild(i).gameObject);
        }

        return visual;
    }

    internal static Humanoid SetUpHumanoid(GameObject prefab, float health, CustomItem weapon, GameObject corpse, string effectsFrom)
    {
        Humanoid humanoid = prefab.GetComponent<Humanoid>();
        humanoid.m_health = health;
        humanoid.m_defaultItems = new[] { weapon.ItemPrefab };
        humanoid.m_randomWeapon = new GameObject[0];
        humanoid.m_randomArmor = new GameObject[0];
        humanoid.m_randomShield = new GameObject[0];
        humanoid.m_randomSets = new Humanoid.ItemSet[0];
        humanoid.m_tolerateWater = true;

        GameObject source = PrefabManager.Instance.GetPrefab(effectsFrom);
        Character sourceCharacter = source != null ? source.GetComponent<Character>() : null;
        EffectList.EffectData[] deathEffects = (sourceCharacter != null ? sourceCharacter.m_deathEffects : humanoid.m_deathEffects).m_effectPrefabs
            .Where(effect => effect.m_prefab != null && effect.m_prefab.GetComponent<Ragdoll>() == null)
            .ToArray();
        humanoid.m_deathEffects = new EffectList
        {
            m_effectPrefabs = deathEffects
                .Append(new EffectList.EffectData { m_prefab = corpse, m_enabled = true, m_inheritParentRotation = true, m_inheritParentScale = true })
                .ToArray()
        };

        if (sourceCharacter != null)
        {
            humanoid.m_hitEffects = sourceCharacter.m_hitEffects;
            humanoid.m_critHitEffects = sourceCharacter.m_critHitEffects;
            MonsterAI sourceAI = source.GetComponent<MonsterAI>();
            MonsterAI ai = prefab.GetComponent<MonsterAI>();
            if (sourceAI != null && ai != null)
            {
                ai.m_alertedEffects = sourceAI.m_alertedEffects;
                ai.m_idleSound = sourceAI.m_idleSound;
            }
        }

        return humanoid;
    }

    internal static void SetCollider(GameObject prefab, Vector3 center, float radius, float height, Vector3 eye)
    {
        CapsuleCollider capsule = prefab.GetComponent<CapsuleCollider>();
        capsule.center = center;
        capsule.radius = radius;
        capsule.height = Mathf.Max(height, radius * 2f);
        capsule.direction = 1;
        Transform eyePos = prefab.transform.Find("EyePos");
        if (eyePos != null)
        {
            eyePos.localPosition = eye;
        }
    }

    internal static GameObject AttachVisual(Transform visual, string creature, Material template, float scale)
    {
        GameObject model = CreatureVisual.Attach(visual, creature, template, Vector3.zero, Quaternion.identity, scale);
        model.GetComponentInChildren<Animator>().gameObject.AddComponent<CharacterAnimEvent>().m_headRotation = false;
        return model;
    }

    internal static DropConfig Drop(string item, int min, int max, float chance)
    {
        return new DropConfig { Item = item, MinAmount = min, MaxAmount = max, Chance = chance, LevelMultiplier = false };
    }
}
