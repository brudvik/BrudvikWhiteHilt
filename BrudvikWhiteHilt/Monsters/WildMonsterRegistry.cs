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
/// Makes the five wild monsters (see <see cref="WildMonsters"/>) and their loot. They are clones of vanilla creatures
/// with their own animated models, so they all go through one code path; what only one of them does is in the
/// <c>Configure</c> methods at the end, and in small components. Each is made in its own try block, so one failure does
/// not cost the player the other four.
/// </summary>
internal static class WildMonsterRegistry
{
    private static readonly Dictionary<string, SpawnSystem.SpawnData> spawns = new();
    private static readonly HashSet<Material> dressed = new();

    /// <summary>
    /// Binds the settings, registers the English texts, and makes the prefabs once the vanilla creatures can be cloned.
    /// </summary>
    public static void Initialize()
    {
        WildMonsterSettings.Initialize();
        AddTexts();
        CreatureManager.OnVanillaCreaturesAvailable += Add;
    }

    /// <summary>
    /// Applies changed or server-synced settings to the spawn data of every monster.
    /// </summary>
    public static void ApplyConfig()
    {
        foreach (WildMonster monster in WildMonsters.All)
        {
            if (!spawns.TryGetValue(monster.Id, out SpawnSystem.SpawnData data))
            {
                continue;
            }

            WildMonsterSettings settings = WildMonsterSettings.Of(monster);
            data.m_enabled = settings.Enabled.Value && settings.SpawnChance.Value > 0f;
            data.m_spawnChance = settings.SpawnChance.Value;
            data.m_spawnInterval = settings.SpawnSeconds.Value;
            data.m_maxSpawned = settings.MaxSpawned.Value;
            if (settings.GroupMin != null)
            {
                data.m_groupSizeMin = settings.GroupMin.Value;
                data.m_groupSizeMax = Math.Max(settings.GroupMin.Value, settings.GroupMax.Value);
            }

            if (settings.RequiredKey != null)
            {
                data.m_requiredGlobalKey = settings.RequiredKey.Value;
            }
        }
    }

    /// <summary>
    /// Mist Stag: it charges, so it does not circle its prey first.
    /// </summary>
    /// <param name="parts">The stag being made.</param>
    internal static void ConfigureStag(WildMonsterParts parts)
    {
        parts.AI.m_viewRange = 14f;
        parts.AI.m_hearRange = 10f;
        parts.AI.m_alertRange = 10f;
        parts.AI.m_circulateWhileCharging = false;
    }

    /// <summary>
    /// Nidhogg Spawn: it roars once when it is alerted, and its run is the walk clip played faster, as its own run clip
    /// stops moving after a second and a half.
    /// </summary>
    /// <param name="parts">The Spawn being made.</param>
    internal static void ConfigureSpawn(WildMonsterParts parts)
    {
        parts.AI.m_viewRange = 35f;
        parts.AI.m_hearRange = 30f;
        parts.AI.m_alertRange = 20f;
        parts.AI.m_circulateWhileCharging = false;

        Animator animator = parts.Model.GetComponentInChildren<Animator>();
        AnimatorOverrideController clips = new(animator.runtimeAnimatorController);
        clips["run"] = clips["walk"];
        animator.runtimeAnimatorController = clips;
        parts.Prefab.AddComponent<CreatureGait>().WalkSpeed = WildMonsters.NidhoggSpawn.WalkSpeed;
        parts.Prefab.AddComponent<SpawnRoar>();
    }

    /// <summary>
    /// Yggdrasil Warden: it sleeps like a dead tree. The game's own sleep keeps it still until a player comes near, and
    /// <see cref="WardenDormancy"/> shows the dormant clip while it does.
    /// </summary>
    /// <param name="parts">The Warden being made.</param>
    internal static void ConfigureWarden(WildMonsterParts parts)
    {
        parts.AI.m_viewRange = 25f;
        parts.AI.m_hearRange = 20f;
        parts.AI.m_alertRange = 15f;
        parts.AI.m_circulateWhileCharging = false;
        parts.AI.m_sleeping = true;
        parts.AI.m_wakeupRange = parts.Settings.WakeRange.Value;
        parts.AI.m_noiseWakeup = false;
        // It wakes with the same roar that alerts it.
        parts.AI.m_wakeupEffects = parts.AI.m_alertedEffects;
        parts.Prefab.AddComponent<WardenDormancy>();
    }

    /// <summary>
    /// Garm Hound: it hunts in packs, so it circles its prey.
    /// </summary>
    /// <param name="parts">The hound being made.</param>
    internal static void ConfigureGarm(WildMonsterParts parts)
    {
        parts.AI.m_viewRange = 40f;
        parts.AI.m_hearRange = 40f;
        parts.AI.m_alertRange = 25f;
        parts.AI.m_circulateWhileCharging = true;
    }

    /// <summary>
    /// Greenland Shark: a swimmer. It keeps the serpent's senses and its dislike of land, and swims at its run speed.
    /// </summary>
    /// <param name="parts">The shark being made.</param>
    internal static void ConfigureShark(WildMonsterParts parts)
    {
        parts.Humanoid.m_swimSpeed = WildMonsters.GreenlandShark.RunSpeed;
        parts.Humanoid.m_swimTurnSpeed = WildMonsters.GreenlandShark.RunTurnSpeed;
        parts.Humanoid.m_canSwim = true;
        parts.AI.m_avoidWater = false;
    }

    private static void Add()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Add;
        Try("Wild monster loot", AddLoot);
        foreach (WildMonster monster in WildMonsters.All)
        {
            Try(monster.EnglishName, () => AddMonster(monster));
        }
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

    private static void AddTexts()
    {
        foreach (WildMonster monster in WildMonsters.All)
        {
            Translations.AddEnglish(monster.NameKey, monster.EnglishName);
        }

        AddItemText(WildMonsters.MistStagMeat, "Mist Stag Meat", "Pale meat that stays cold in the pan. Cook it hot and long.");
        AddItemText(WildMonsters.MistStagTrophy, "Mist Stag Trophy", "It gnawed at the world tree, and you took its head.");
        AddItemText(WildMonsters.BroodScale, "Brood Scale", "A black scale with a red edge, still warm from a dragon's blood.");
        AddItemText(WildMonsters.SpawnTrophy, "Nidhogg Spawn Trophy", "Dragon spawn have no mother, and no mercy.");
        AddItemText(WildMonsters.LivingHeartwood, "Living Heartwood", "Wood that still grows. It hums when you hold it.");
        AddItemText(WildMonsters.WardenTrophy, "Yggdrasil Warden Trophy", "The tree stood still. Then it did not.");
        AddItemText(WildMonsters.GarmFang, "Garm Fang", "A fang blackened by hellfire, hot to the root.");
        AddItemText(WildMonsters.GarmTrophy, "Garm Hound Trophy", "Whelp of the hound at the gate of Hel.");
        AddItemText(WildMonsters.SharkMeat, "Shark Meat", "Raw, it will poison you. Hang it and wait, and it turns into food.");
        AddItemText(WildMonsters.SharkTrophy, "Greenland Shark Trophy", "It had seen five hundred winters. It did not see you.");
    }

    private static void AddItemText(string prefab, string name, string description)
    {
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(prefab), name, description);
    }

    // The loot: recoloured clones of vanilla items, so they are easy to tell apart from the originals.
    private static void AddLoot()
    {
        MonsterRegistry.AddItem(WildMonsters.MistStagMeat, "DeerMeat", prefab => VisualHelper.Tint(prefab, new Color(0.75f, 0.85f, 1f)));
        MonsterRegistry.AddItem(WildMonsters.MistStagTrophy, "TrophyDeer", prefab => VisualHelper.Recolor(prefab, Tone(0.58f, 0.15f, 1.1f)));
        MonsterRegistry.AddItem(WildMonsters.BroodScale, "SerpentScale", prefab => VisualHelper.Recolor(prefab, Tone(0f, 0.55f, 0.35f)));
        MonsterRegistry.AddItem(WildMonsters.SpawnTrophy, "TrophySeekerBrute", prefab => VisualHelper.Recolor(prefab, Tone(0f, 0.7f, 0.45f)));
        MonsterRegistry.AddItem(WildMonsters.LivingHeartwood, "YggdrasilWood", prefab => VisualHelper.Tint(prefab, new Color(0.65f, 1f, 0.7f)));
        MonsterRegistry.AddItem(WildMonsters.WardenTrophy, "TrophyGreydwarfShaman", prefab => VisualHelper.Recolor(prefab, Tone(0.3f, 0.45f, 0.5f)));
        MonsterRegistry.AddItem(WildMonsters.GarmFang, "WolfFang", prefab => VisualHelper.Recolor(prefab, Tone(0.02f, 0.6f, 0.3f)));
        MonsterRegistry.AddItem(WildMonsters.GarmTrophy, "TrophyWolf", prefab => VisualHelper.Recolor(prefab, Tone(0.03f, 0.6f, 0.35f)));
        MonsterRegistry.AddItem(WildMonsters.SharkMeat, "SerpentMeat", prefab => VisualHelper.Tint(prefab, new Color(0.75f, 0.8f, 0.8f)));
        MonsterRegistry.AddItem(WildMonsters.SharkTrophy, "TrophySerpent", prefab => VisualHelper.Recolor(prefab, Tone(0.5f, 0.12f, 0.55f)));
    }

    // Moves every pixel towards a hue and a saturation, and scales its brightness.
    private static Func<Color32, Color32> Tone(float hue, float saturation, float value)
    {
        return pixel =>
        {
            Color.RGBToHSV(pixel, out _, out float s, out float v);
            Color32 result = Color.HSVToRGB(hue, Mathf.Clamp01(s * 0.4f + saturation * 0.6f), Mathf.Clamp01(v * value + 0.04f));
            result.a = pixel.a;
            return result;
        };
    }

    // Makes one monster: a clone of a vanilla creature with its own model, weapons, loot, corpse and spawn.
    private static void AddMonster(WildMonster monster)
    {
        WildMonsterSettings settings = WildMonsterSettings.Of(monster);
        float size = settings.Scale.Value;
        float modelScale = monster.ModelScale * size;
        Material template = CreatureVisual.TemplateOf(PrefabManager.Instance.GetPrefab(monster.TemplateCreature));

        CustomItem[] weapons = new CustomItem[monster.Attacks.Length];
        for (int i = 0; i < weapons.Length; i++)
        {
            WildAttack attack = monster.Attacks[i];
            float damage = i == 0 ? settings.Damage.Value : settings.SecondDamage.Value;
            float extra = i == 0 && settings.ExtraDamage != null ? settings.ExtraDamage.Value : 0f;
            weapons[i] = MonsterRegistry.AddWeapon(monster.PrefabName + attack.Name, attack.Trigger, Damages(attack, damage, extra),
                attack.Range * size, attack.Height * size, attack.Angle, attack.RayWidth * size, attack.Interval, attack.Force);
        }

        GameObject corpse = MonsterRegistry.CreateCorpse(monster.PrefabName + "Corpse", monster.Id, template, modelScale,
            monster.CorpseSinkAfter, monster.CorpseSinkSpeed, monster.CorpseLifetime);
        CustomCreature creature = new(monster.PrefabName, monster.BaseCreature, new CreatureConfig
        {
            Name = Translations.Token(monster.NameKey),
            Faction = monster.Faction,
            DropConfigs = monster.Drops.Select(drop => MonsterRegistry.Drop(drop.Item, drop.Min, drop.Max, 100f))
                .Append(MonsterRegistry.Drop(monster.Trophy, 1, 1, settings.TrophyChance.Value)).ToArray(),
            SpawnConfigs = new[] { SpawnOf(monster, settings) }
        });

        GameObject prefab = creature.Prefab;
        // The serpent's tail is a chain of joints in the model, which is replaced.
        foreach (Tail tail in prefab.GetComponents<Tail>())
        {
            UnityEngine.Object.DestroyImmediate(tail);
        }

        Transform visual = MonsterRegistry.PrepareClone(prefab);
        bool swimmer = monster.BaseCreature == "Serpent";
        Humanoid humanoid = MonsterRegistry.SetUpHumanoid(prefab, settings.Health.Value, weapons[0], corpse, swimmer ? "Serpent" : monster.TemplateCreature);
        humanoid.m_defaultItems = weapons.Select(weapon => weapon.ItemPrefab).ToArray();
        humanoid.m_name = Translations.Token(monster.NameKey);
        humanoid.m_walkSpeed = monster.WalkSpeed;
        humanoid.m_runSpeed = monster.RunSpeed;
        humanoid.m_speed = monster.WalkSpeed;
        humanoid.m_turnSpeed = monster.TurnSpeed;
        humanoid.m_runTurnSpeed = monster.RunTurnSpeed;
        humanoid.m_canSwim = swimmer;
        humanoid.m_damageModifiers = monster.Modifiers;

        MonsterRegistry.SetCollider(prefab, monster.CapsuleCenter * size, monster.CapsuleRadius * size, monster.CapsuleHeight * size, monster.Eye * size);
        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_avoidWater = !swimmer;
        ai.m_afraidOfFire = false;
        ai.m_avoidFire = false;
        ai.m_fleeIfNotAlerted = false;

        GameObject model = MonsterRegistry.AttachVisual(visual, monster.Id, template, modelScale);
        LevelEffects levels = visual.gameObject.AddComponent<LevelEffects>();
        levels.m_levelSetups = Enumerable.Range(1, Difficulty.CreatureStars.MaxLevel - 1)
            .Select(stars => new LevelEffects.LevelSetup { m_scale = 1f + Mathf.Min(stars, 2) * settings.StarScale.Value })
            .ToList();
        if (!VisualHelper.IsHeadless)
        {
            Dress(model, monster.Look);
        }

        if (settings.Sounds.Value)
        {
            bool two = weapons.Length > 1;
            MonsterRegistry.AddCreatureSounds(humanoid, ai, weapons[0], monster.SoundPrefix, "attack", two ? weapons[1] : null, two ? "attack2" : null);
        }

        monster.Configure?.Invoke(new WildMonsterParts { Settings = settings, Prefab = prefab, Humanoid = humanoid, AI = ai, Model = model });
        CreatureManager.Instance.AddCreature(creature);
        spawns[monster.Id] = creature.Spawns.FirstOrDefault();
        ApplyConfig();
    }

    private static SpawnConfig SpawnOf(WildMonster monster, WildMonsterSettings settings)
    {
        return new SpawnConfig
        {
            Name = $"{monster.PrefabName}_Spawn",
            Biome = monster.Biome,
            BiomeArea = monster.BiomeArea,
            SpawnAtDay = monster.SpawnAtDay,
            SpawnAtNight = monster.SpawnAtNight,
            SpawnInForest = true,
            SpawnOutsideForest = true,
            SpawnChance = settings.SpawnChance.Value,
            SpawnInterval = settings.SpawnSeconds.Value,
            MaxSpawned = settings.MaxSpawned.Value,
            MinGroupSize = settings.GroupMin != null ? settings.GroupMin.Value : 1,
            MaxGroupSize = settings.GroupMax != null ? settings.GroupMax.Value : 1,
            GroupRadius = monster.GroupRadius,
            SpawnDistance = monster.SpawnDistance,
            MinAltitude = monster.MinAltitude,
            MaxAltitude = monster.MaxAltitude,
            GroundOffset = monster.GroundOffset,
            MaxTilt = 35f,
            MinLevel = 1,
            MaxLevel = 2,
            RequiredGlobalKey = settings.RequiredKey != null ? settings.RequiredKey.Value : string.Empty
        };
    }

    // The damage of an attack: its main kind, and a second kind added to it.
    private static HitData.DamageTypes Damages(WildAttack attack, float damage, float extra)
    {
        HitData.DamageTypes damages = new();
        SetDamage(ref damages, attack.Type, damage);
        if (extra > 0f)
        {
            SetDamage(ref damages, attack.ExtraType, extra);
        }

        return damages;
    }

    private static void SetDamage(ref HitData.DamageTypes damages, HitData.DamageType type, float amount)
    {
        switch (type)
        {
            case HitData.DamageType.Blunt: damages.m_blunt = amount; break;
            case HitData.DamageType.Slash: damages.m_slash = amount; break;
            case HitData.DamageType.Pierce: damages.m_pierce = amount; break;
            case HitData.DamageType.Fire: damages.m_fire = amount; break;
            case HitData.DamageType.Frost: damages.m_frost = amount; break;
            case HitData.DamageType.Lightning: damages.m_lightning = amount; break;
            case HitData.DamageType.Poison: damages.m_poison = amount; break;
            case HitData.DamageType.Spirit: damages.m_spirit = amount; break;
        }
    }

    // Colours the model's materials at runtime. They are shared by every monster of a kind and its corpse, so each is
    // changed once, however many times a model is made.
    private static void Dress(GameObject model, WildLook look)
    {
        foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || look.Skip.Any(part => material.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) || !dressed.Add(material))
                {
                    continue;
                }

                if (material.HasProperty("_Color"))
                {
                    material.color *= look.Tint;
                }

                Offset(material, "_Hue", look.Hue);
                Offset(material, "_Saturation", look.Saturation);
                Offset(material, "_Value", look.Value);
                if (look.Glossiness >= 0f && material.HasProperty("_Glossiness"))
                {
                    material.SetFloat("_Glossiness", look.Glossiness);
                }

                if (look.Matte)
                {
                    foreach (string property in new[] { "_Metallic", "_MetalGloss" }.Where(material.HasProperty))
                    {
                        material.SetFloat(property, 0f);
                    }
                }
            }
        }
    }

    private static void Offset(Material material, string property, float amount)
    {
        if (amount != 0f && material.HasProperty(property))
        {
            material.SetFloat(property, material.GetFloat(property) + amount);
        }
    }
}
