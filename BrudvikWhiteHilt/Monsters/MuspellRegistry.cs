using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Makes the Muspell Giant: a fire giant of Muspelheim, a clone of the vanilla troll grown to about 9 m, charred black
/// with glowing cracks. Its punches, ground slam and thrown rocks hit harder and burn, its rocks and its steps leave
/// burning pools (<see cref="MuspellGiant"/>), and fire and lava do not hurt it, while frost cracks it. Once the Queen
/// is slain <see cref="MuspellService"/> now and then lets one walk out of the fire near a player in the Ashlands.
/// </summary>
public static class MuspellRegistry
{
    /// <summary>Prefab name of the Muspell Giant.</summary>
    public const string PrefabName = "WhiteHilt_MuspellGiant";

    /// <summary>Prefab name of the Surtstone.</summary>
    public const string SurtstoneName = "WhiteHilt_Surtstone";

    /// <summary>Prefab name of the Muspell Giant trophy.</summary>
    public const string TrophyName = "WhiteHilt_TrophyMuspellGiant";

    private const string NameKey = "enemy_whitehilt_muspellgiant";
    private const string RockName = "WhiteHilt_MuspellRock";
    private const string SplashName = "WhiteHilt_MuspellRockSplash";
    private const string SlamAoeName = "WhiteHilt_MuspellSlamAoe";
    private static readonly string[] trollWeapons = { "troll_groundslam", "troll_punch", "troll_throw" };
    private static readonly Color glowColor = new(1f, 0.42f, 0.1f);

    /// <summary>
    /// Binds the settings, registers the English texts, and makes the prefab once the vanilla creatures can be cloned.
    /// </summary>
    public static void Initialize()
    {
        MuspellSettings.Initialize();
        Translations.AddEnglish(NameKey, "Muspell Giant");
        Translations.AddEnglish("msg_whitehilt_muspell_comes", "The ground trembles. Something vast walks out of the fire...");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(SurtstoneName), "Surtstone",
            "A heart of stone that has never stopped burning. It came out of Muspelheim inside a giant.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(TrophyName), "Muspell Giant Trophy",
            "A head of molten rock, still cooling. Surt will miss one of his sons.");
        CreatureManager.OnVanillaCreaturesAvailable += Add;
    }

    private static void Add()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Add;
        try
        {
            MonsterRegistry.AddItem(SurtstoneName, "SurtlingCore", prefab => VisualHelper.Recolor(prefab, Ember(0.5f)));
            MonsterRegistry.AddItem(TrophyName, "TrophyFrostTroll", prefab => VisualHelper.Recolor(prefab, Ember(0.4f)));
            AddGiant();
            Jotunn.Logger.LogInfo("Muspell Giant added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Muspell Giant failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Charcoal, with the brightest parts of the texture turned to embers.
    private static Func<Color32, Color32> Ember(float value)
    {
        return pixel =>
        {
            Color.RGBToHSV(pixel, out _, out _, out float v);
            float heat = Mathf.SmoothStep(0f, 1f, (v - 0.5f) / 0.5f);
            Color32 result = Color.Lerp(new Color(v * value, v * value * 0.8f, v * value * 0.75f), new Color(1f, 0.45f, 0.1f), heat);
            result.a = pixel.a;
            return result;
        };
    }

    private static void AddGiant()
    {
        float scale = MuspellSettings.Scale.Value;
        GameObject splash = PrefabManager.Instance.CreateEmptyPrefab(SplashName, false);
        foreach (Component part in splash.GetComponents<Component>().Where(part => part is MeshRenderer || part is MeshFilter || part is Collider))
        {
            UnityEngine.Object.DestroyImmediate(part);
        }

        splash.AddComponent<MuspellRockSplash>();
        GameObject[] weapons = trollWeapons.Select(name => AddWeapon(name, scale, splash)).ToArray();

        CustomCreature creature = new(PrefabName, "Troll", new CreatureConfig
        {
            Name = Translations.Token(NameKey),
            Faction = Character.Faction.Demon,
            DropConfigs = new[]
            {
                MonsterRegistry.Drop(SurtstoneName, 1, 2, 100f),
                MonsterRegistry.Drop("FlametalOreNew", 6, 10, 100f),
                MonsterRegistry.Drop(TrophyName, 1, 1, MuspellSettings.TrophyChance.Value)
            }
        });

        GameObject prefab = creature.Prefab;
        prefab.transform.localScale = Vector3.one * scale;
        Humanoid humanoid = prefab.GetComponent<Humanoid>();
        humanoid.m_name = Translations.Token(NameKey);
        humanoid.m_health = MuspellSettings.Health.Value;
        humanoid.m_defaultItems = weapons;
        humanoid.m_randomWeapon = new GameObject[0];
        humanoid.m_randomSets = new Humanoid.ItemSet[0];
        humanoid.m_walkSpeed = 3f;
        humanoid.m_runSpeed = 5.5f;
        humanoid.m_speed = 3f;
        humanoid.m_tolerateFire = true;
        humanoid.m_tolerateSmoke = true;
        humanoid.m_damageModifiers = new HitData.DamageModifiers
        {
            m_blunt = HitData.DamageModifier.Normal,
            m_slash = HitData.DamageModifier.Resistant,
            m_pierce = HitData.DamageModifier.Resistant,
            m_chop = HitData.DamageModifier.Ignore,
            m_pickaxe = HitData.DamageModifier.Normal,
            m_fire = HitData.DamageModifier.Immune,
            m_frost = HitData.DamageModifier.VeryWeak,
            m_lightning = HitData.DamageModifier.Normal,
            m_poison = HitData.DamageModifier.Immune,
            m_spirit = HitData.DamageModifier.Resistant
        };

        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_viewRange = 50f;
        ai.m_hearRange = 50f;
        ai.m_alertRange = 30f;
        ai.m_avoidLava = false;
        ai.m_avoidFire = false;
        ai.m_afraidOfFire = false;
        ai.m_fleeIfLowHealth = 0f;
        ai.m_fleeIfHurtWhenTargetCantBeReached = false;

        if (!VisualHelper.IsHeadless)
        {
            VisualHelper.Recolor(prefab, Ember(0.35f));
            AddGlow(prefab);
        }

        if (MuspellSettings.Sounds.Value)
        {
            MonsterRegistry.AddCloneSounds(prefab, "muspell");
            MonsterRegistry.SetFootsteps(prefab, "step_heavy");
            prefab.AddComponent<LoopSound>().clip = "amb_muspell";
        }

        prefab.AddComponent<MuspellGiant>();
        prefab.AddComponent<ArrivalSound>().clip = "arrival_muspell";
        CreatureManager.Instance.AddCreature(creature);
    }

    // A troll's weapon made the giant's: its reach grown with the giant, its blunt damage raised and fire added. The slam
    // gets an area as big as the giant, and the throw a molten rock that leaves a burning pool where it lands.
    private static GameObject AddWeapon(string source, float scale, GameObject splash)
    {
        CustomItem weapon = new($"WhiteHilt_Muspell_{source}", source);
        ItemDrop.ItemData.SharedData shared = weapon.ItemDrop.m_itemData.m_shared;
        Burn(ref shared.m_damages);
        Attack attack = shared.m_attack;
        if (attack.m_attackProjectile == null)
        {
            attack.m_attackRange *= scale;
            attack.m_attackHeight *= scale;
            attack.m_attackRayWidth *= scale;
            shared.m_aiAttackRange *= scale;
            shared.m_aiAttackRangeMin *= scale;
        }
        else
        {
            attack.m_attackProjectile = MoltenRock(attack.m_attackProjectile, scale, splash);
        }

        if (shared.m_spawnOnHit != null && shared.m_spawnOnHit.GetComponent<Aoe>() != null)
        {
            shared.m_spawnOnHit = SlamArea(shared.m_spawnOnHit, scale);
        }

        ItemManager.Instance.AddItem(weapon);
        return weapon.ItemPrefab;
    }

    private static void Burn(ref HitData.DamageTypes damages)
    {
        damages.m_blunt *= MuspellSettings.DamageMultiplier.Value;
        damages.m_fire += damages.m_blunt * MuspellSettings.FireShare.Value;
    }

    private static GameObject SlamArea(GameObject source, float scale)
    {
        GameObject area = PrefabManager.Instance.CreateClonedPrefab(SlamAoeName, source);
        area.transform.localScale = source.transform.localScale * scale;
        Aoe aoe = area.GetComponent<Aoe>();
        aoe.m_radius *= scale;
        Burn(ref aoe.m_damage);
        PrefabManager.Instance.AddPrefab(new CustomPrefab(area, false));
        return area;
    }

    private static GameObject MoltenRock(GameObject source, float scale, GameObject splash)
    {
        GameObject rock = PrefabManager.Instance.CreateClonedPrefab(RockName, source);
        rock.transform.localScale = source.transform.localScale * Mathf.Sqrt(scale);
        Projectile projectile = rock.GetComponent<Projectile>();
        projectile.m_spawnOnHit = splash;
        projectile.m_spawnOnHitChance = 1f;
        projectile.m_spawnCount = 1;
        if (!VisualHelper.IsHeadless)
        {
            VisualHelper.Recolor(rock, Ember(0.45f));
            Light light = new GameObject("muspell_rock_glow").AddComponent<Light>();
            light.transform.SetParent(rock.transform, false);
            light.type = LightType.Point;
            light.color = glowColor;
            light.range = 5f;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;
        }

        PrefabManager.Instance.AddPrefab(new CustomPrefab(rock, false));
        return rock;
    }

    // A warm light in its chest, so it is seen coming from afar at night. It grows with the giant.
    private static void AddGlow(GameObject prefab)
    {
        GameObject glow = new("muspell_glow");
        glow.transform.SetParent(prefab.transform, false);
        CapsuleCollider body = prefab.GetComponent<CapsuleCollider>();
        glow.transform.localPosition = body != null ? body.center : Vector3.up * 2f;
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = glowColor;
        light.range = 6f * MuspellSettings.Scale.Value;
        light.intensity = 2f;
        light.shadows = LightShadows.None;
    }
}
