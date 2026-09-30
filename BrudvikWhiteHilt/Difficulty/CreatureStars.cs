using BepInEx.Bootstrap;
using BrudvikWhiteHilt.Difficulty.Beasts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Creature stars beyond vanilla: higher level-up chances, up to 5 stars, a health and damage bonus from the pressure,
/// softer health, damage and loot for stars above 2, and growth for those stars.
/// Values chosen at spawn are stored in the creature's ZDO, so they do not change later.
/// </summary>
public static class CreatureStars
{
    /// <summary>ZDO key of a creature's health bonus.</summary>
    public const string HealthKey = "whitehilt_hp";

    /// <summary>ZDO key of a creature's damage bonus.</summary>
    public const string DamageKey = "whitehilt_dmg";

    /// <summary>ZDO key that marks a beast.</summary>
    public const string BeastKey = "whitehilt_beast";

    /// <summary>Level of a 5 star creature.</summary>
    public const int MaxLevel = 6;

    private const string CllcGuid = "org.bepinex.plugins.creaturelevelcontrol";

    // A spawn group comes out within a few seconds; only one of it may have 4 or 5 stars.
    private const float GroupWindow = 5f;
    private const float GroupRange = 40f;

    private static readonly List<(Vector3 Position, float Time)> recentHighStars = new();
    private static bool? cllcLoaded;
    private static int spawnDepth;
    private static bool spawnIsEvent;
    private static Character current;

    /// <summary>
    /// Whether stars are changed: on in config, and Creature Level and Loot Control is not installed.
    /// </summary>
    public static bool Active => DifficultySettings.Enabled.Value && DifficultySettings.StarsEnabled.Value && !CllcLoaded;

    private static bool CllcLoaded => cllcLoaded ??= Chainloader.PluginInfos.ContainsKey(CllcGuid);

    /// <summary>
    /// Called before the game spawns a creature from a spawn system, spawner or spawn area.
    /// </summary>
    /// <param name="isEvent">Whether a raid spawns it.</param>
    public static void BeginSpawn(bool isEvent)
    {
        if (spawnDepth++ == 0)
        {
            spawnIsEvent = isEvent;
            current = null;
        }
    }

    /// <summary>
    /// Called when the spawn is done.
    /// </summary>
    public static void EndSpawn()
    {
        if (spawnDepth > 0 && --spawnDepth == 0)
        {
            current = null;
        }
    }

    /// <summary>
    /// Multiplier of the vanilla level-up chance for the creature being spawned.
    /// </summary>
    /// <returns>The multiplier, 1 when nothing changes.</returns>
    public static float LevelUpChanceMultiplier()
    {
        if (!AppliesToCurrentSpawn())
        {
            return 1f;
        }

        return 1f + DifficultyState.Pressure * (DifficultySettings.LevelUpChanceMax.Value - 1f);
    }

    /// <summary>
    /// Called after a creature's Awake. Grows loaded creatures and gives a new spawn its pressure bonus.
    /// </summary>
    /// <param name="character">The creature.</param>
    public static void OnAwake(Character character)
    {
        if (character.IsPlayer())
        {
            return;
        }

        ApplySize(character);
        ZNetView nview = character.m_nview;
        if (spawnDepth == 0 || current != null || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        current = character;
        if (!AppliesToCurrentSpawn())
        {
            return;
        }

        float pressure = DifficultyState.Pressure;
        ZDO zdo = nview.GetZDO();
        float health = 1f + pressure * DifficultySettings.PressureHealthBonus.Value;
        float damage = 1f + pressure * DifficultySettings.PressureDamageBonus.Value;
        if (health > 1.001f)
        {
            zdo.Set(HealthKey, health);
        }

        if (damage > 1.001f)
        {
            zdo.Set(DamageKey, damage);
        }

        character.SetupMaxHealth();
    }

    /// <summary>
    /// Called before the game sets the level of the creature being spawned. Rolls stars beyond 2.
    /// </summary>
    /// <param name="character">The creature.</param>
    /// <param name="level">The level the game rolled; raised here.</param>
    public static void OnSetLevel(Character character, ref int level)
    {
        if (character != current || level < 3 || !AppliesToCurrentSpawn())
        {
            return;
        }

        Vector3 position = character.transform.position;
        Heightmap.Biome biome = WorldGenerator.instance != null ? WorldGenerator.instance.GetBiome(position) : Heightmap.Biome.None;
        int maxLevel = Mathf.Min(DifficultyState.UnlockedMaxStars, DifficultySettings.GetBiomeMaxStars(biome)) + 1;
        float scale = DifficultyState.Pressure;
        while (level < maxLevel && level < MaxLevel && Random.value < StepChance(level) / 100f * scale)
        {
            level++;
        }

        if (level >= 5)
        {
            LimitGroup(position, ref level);
        }
    }

    /// <summary>
    /// Health multiplier of a creature, or 0 if vanilla's own applies.
    /// </summary>
    /// <param name="character">The creature.</param>
    /// <returns>Multiplier of the base health.</returns>
    public static float HealthFactor(Character character)
    {
        ZDO zdo = character.m_nview != null ? character.m_nview.GetZDO() : null;
        if (zdo == null || character.IsPlayer())
        {
            return 0f;
        }

        int level = character.GetLevel();
        float bonus = zdo.GetFloat(HealthKey, 1f);
        if (level <= 3 && Mathf.Approximately(bonus, 1f))
        {
            return 0f;
        }

        float levelFactor = level <= 3 ? level : 3f + (level - 3) * DifficultySettings.HealthPerExtraStar.Value;
        float total = levelFactor * bonus;
        return zdo.GetBool(BeastKey) ? total : Mathf.Min(total, DifficultySettings.MaxHealthMultiplier.Value);
    }

    /// <summary>
    /// Damage multiplier of a creature's attack.
    /// </summary>
    /// <param name="character">The attacking creature.</param>
    /// <param name="vanilla">The vanilla multiplier for its level.</param>
    /// <returns>The multiplier to use.</returns>
    public static float DamageFactor(Character character, float vanilla)
    {
        ZDO zdo = character != null && character.m_nview != null ? character.m_nview.GetZDO() : null;
        if (zdo == null || character.IsPlayer())
        {
            return vanilla;
        }

        int level = character.GetLevel();
        float bonus = zdo.GetFloat(DamageKey, 1f);
        if (level <= 3 && Mathf.Approximately(bonus, 1f))
        {
            return vanilla;
        }

        // Vanilla gives 2 at 2 stars; stars above add less.
        float levelFactor = level <= 3 ? vanilla : 2f + (level - 3) * DifficultySettings.DamagePerExtraStar.Value;
        return Mathf.Min(levelFactor * bonus, DifficultySettings.MaxDamageMultiplier.Value);
    }

    /// <summary>
    /// Loot multiplier of a dying creature.
    /// </summary>
    /// <param name="vanilla">The vanilla multiplier, 2 to the power of its stars.</param>
    /// <param name="drop">The creature's drop component.</param>
    /// <returns>The multiplier to use.</returns>
    public static int DropFactor(int vanilla, CharacterDrop drop)
    {
        int level = drop.m_character != null ? drop.m_character.GetLevel() : 1;
        return level switch
        {
            < 4 => vanilla,
            4 => DifficultySettings.Drops3Stars.Value,
            5 => DifficultySettings.Drops4Stars.Value,
            _ => DifficultySettings.Drops5Stars.Value
        };
    }

    /// <summary>
    /// Grows or shrinks a creature to fit its stars.
    /// </summary>
    /// <param name="character">The creature.</param>
    public static void ApplySize(Character character)
    {
        if (character.IsPlayer())
        {
            return;
        }

        float target = SizeFactor(character);
        StarScale scale = character.GetComponent<StarScale>();
        if (scale == null)
        {
            if (Mathf.Approximately(target, 1f))
            {
                return;
            }

            scale = character.gameObject.AddComponent<StarScale>();
        }

        if (Mathf.Approximately(target, scale.Applied))
        {
            return;
        }

        character.transform.localScale *= target / scale.Applied;
        scale.Applied = target;
    }

    /// <summary>
    /// Whether stars and bonuses may change a creature: no players, bosses, tame creatures or beasts.
    /// </summary>
    /// <param name="character">The creature.</param>
    /// <returns>True if it may be changed.</returns>
    public static bool IsEligible(Character character)
    {
        return !character.IsPlayer() && !character.IsBoss() && !character.IsTamed()
            && BeastDefinition.ByPrefab(Utils.GetPrefabName(character.gameObject)) == null;
    }

    private static bool AppliesToCurrentSpawn()
    {
        return current != null && Active && IsEligible(current) && (!spawnIsEvent || DifficultySettings.IncludeRaids.Value);
    }

    private static float StepChance(int level)
    {
        return level switch
        {
            3 => DifficultySettings.Chance3Stars.Value,
            4 => DifficultySettings.Chance4Stars.Value,
            _ => DifficultySettings.Chance5Stars.Value
        };
    }

    private static void LimitGroup(Vector3 position, ref int level)
    {
        float now = Time.time;
        recentHighStars.RemoveAll(entry => now - entry.Time > GroupWindow);
        if (recentHighStars.Any(entry => Vector3.Distance(entry.Position, position) < GroupRange))
        {
            level = 4;
            return;
        }

        recentHighStars.Add((position, now));
    }

    private static float SizeFactor(Character character)
    {
        int extraStars = character.GetLevel() - 3;
        if (extraStars <= 0 || !DifficultySettings.Enabled.Value || !DifficultySettings.SizeEnabled.Value || character.InInterior())
        {
            return 1f;
        }

        bool large = DifficultySettings.IsLargeCreature(Utils.GetPrefabName(character.gameObject));
        float perStar = DifficultySettings.SizePerExtraStar.Value * (large ? DifficultySettings.LargeCreatureFactor.Value : 1f);
        return Mathf.Min(1f + perStar * extraStars, DifficultySettings.MaxSize.Value);
    }
}
