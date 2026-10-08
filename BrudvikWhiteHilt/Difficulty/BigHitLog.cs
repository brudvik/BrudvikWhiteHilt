using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Writes a line to the log when the local player takes a hard hit, with everything that scaled it: who struck, at
/// which level, the mod's and the game's damage factors, the damage before and after armour, and how much burns, poisons
/// or haunts afterwards. Hard hits players report can then be traced from their log instead of guessed at.
/// </summary>
public static class BigHitLog
{
    private const string Section = "Difficulty";

    private static ConfigEntry<float> threshold;

    /// <summary>
    /// The hit being taken, between the start and the end of the game's damage handling.
    /// </summary>
    public sealed class Pending
    {
        /// <summary>Total damage as it arrived, before any scaling.</summary>
        public float Raw;

        /// <summary>The damage types as they arrived.</summary>
        public string Types;

        /// <summary>Health before the hit.</summary>
        public float HealthBefore;

        /// <summary>Who struck, with their level and factors.</summary>
        public string Attacker;

        /// <summary>Whether the player was blocking.</summary>
        public bool Blocking;
    }

    /// <summary>
    /// Binds the setting. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        threshold = WhiteHiltConfig.BindLocal(Section, "LogHitsAbove", 100f,
            "Write a line to the log (search for [BigHit]) when you lose more health than this in one hit, or a hit arrives this strong, "
            + "with who struck and every factor that scaled it. 0 turns it off.", new AcceptableValueRange<float>(0f, 10000f));
    }

    /// <summary>
    /// Notes a hit on the local player before the game scales it.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="hit">The hit as it arrived.</param>
    /// <returns>What to report afterwards, or null when off.</returns>
    public static Pending Begin(Player player, HitData hit)
    {
        if (threshold == null || threshold.Value <= 0f || hit == null)
        {
            return null;
        }

        return new Pending
        {
            Raw = hit.GetTotalDamage(),
            Types = Types(hit.m_damage),
            HealthBefore = player.GetHealth(),
            Attacker = Describe(hit.GetAttacker()),
            Blocking = player.IsBlocking()
        };
    }

    /// <summary>
    /// Writes the line when the hit was hard.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="hit">The hit, after the game scaled it.</param>
    /// <param name="pending">What was noted before.</param>
    public static void End(Player player, HitData hit, Pending pending)
    {
        if (pending == null || player == null)
        {
            return;
        }

        float lost = pending.HealthBefore - player.GetHealth();
        if (lost < threshold.Value && pending.Raw < threshold.Value)
        {
            return;
        }

        Game game = Game.instance;
        Vector3 position = player.transform.position;
        string line = string.Format(CultureInfo.InvariantCulture,
            "[BigHit] lost {0:0} health at once ({1:0} -> {2:0}{3}) from {4}. Arrived {5:0} ({6}); "
            + "game: player-count scale x{7:0.00}, world enemy damage x{8:0.00}, damage taken x{9:0.00}; "
            + "armour {10:0}, blocking {11}; taken at once after scaling, resistance and armour {12:0} "
            + "(fire, poison and spirit come over time instead).",
            lost, pending.HealthBefore, player.GetHealth(), player.IsDead() ? ", dead" : string.Empty, pending.Attacker,
            pending.Raw, pending.Types,
            game != null ? game.GetDifficultyDamageScalePlayer(position) : 1f, Game.m_enemyDamageRate, Game.m_localDamgeTakenRate,
            player.GetBodyArmor(), pending.Blocking ? "yes" : "no", hit.GetTotalDamage());
        Jotunn.Logger.LogWarning(line);
    }

    // "Deathsquito level 3 (2 stars), damage x2.00 (vanilla x2.00), mod health x1.20, damage x1.10, beast no".
    private static string Describe(Character attacker)
    {
        if (attacker == null)
        {
            return "nothing (fall, fire or a trap)";
        }

        string name = Utils.GetPrefabName(attacker.gameObject);
        if (attacker.IsPlayer())
        {
            return $"player {name}";
        }

        int level = attacker.GetLevel();
        float vanilla = 1f + (level - 1) * 0.5f;
        float factor = CreatureStars.DamageFactor(attacker, vanilla);
        ZDO zdo = attacker.m_nview != null && attacker.m_nview.IsValid() ? attacker.m_nview.GetZDO() : null;
        float healthBonus = zdo != null ? zdo.GetFloat(CreatureStars.HealthKey, 1f) : 1f;
        float damageBonus = zdo != null ? zdo.GetFloat(CreatureStars.DamageKey, 1f) : 1f;
        bool beast = zdo != null && zdo.GetBool(CreatureStars.BeastKey);
        return string.Format(CultureInfo.InvariantCulture,
            "{0} level {1} ({2} stars{3}), level damage x{4:0.00} (vanilla x{5:0.00}), mod pressure health x{6:0.00} damage x{7:0.00}{8}",
            name, level, level - 1, attacker.IsBoss() ? ", boss" : string.Empty, factor, vanilla, healthBonus, damageBonus, beast ? ", black beast" : string.Empty);
    }

    private static string Types(HitData.DamageTypes damage)
    {
        string text = string.Empty;
        void Add(string name, float value)
        {
            if (value > 0f)
            {
                text += (text.Length > 0 ? " " : string.Empty) + name + " " + value.ToString("0", CultureInfo.InvariantCulture);
            }
        }

        Add("blunt", damage.m_blunt);
        Add("slash", damage.m_slash);
        Add("pierce", damage.m_pierce);
        Add("chop", damage.m_chop);
        Add("pickaxe", damage.m_pickaxe);
        Add("fire", damage.m_fire);
        Add("frost", damage.m_frost);
        Add("lightning", damage.m_lightning);
        Add("poison", damage.m_poison);
        Add("spirit", damage.m_spirit);
        return text.Length > 0 ? text : "none";
    }
}
