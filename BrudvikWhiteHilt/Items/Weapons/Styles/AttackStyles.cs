using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Items.Weapons.Styles;

/// <summary>
/// What each <see cref="AttackStyle"/> plays and how long it takes, and the arithmetic that keeps a weapon as strong
/// in every style as in its own. Pure data and maths, so the tests can check it without the game.
/// </summary>
/// <remarks>
/// Every attack of the player's animator is an Any State transition on its trigger alone, so any weapon can play any
/// attack animation. The seconds per hit were measured from the game's Player_animator: the time until the clip's
/// Chain event (when the next click is taken) or else until the state leaves, with the clip's Speed events and the
/// state's speed included. The hit area angles are those of the vanilla weapon each animation belongs to.
/// </remarks>
public static class AttackStyles
{
    /// <summary>
    /// The trigger the Rune Sword's own cuts play under: the dual knives' combo, whose clips are swapped for the
    /// Rune Sword's while it is in hand.
    /// </summary>
    public const string RuneTrigger = "dual_knives";

    private const float MinFactor = 0.25f;
    private const float MaxFactor = 4f;

    /// <summary>
    /// Seconds per hit of the Rune Sword's three cuts, as made by AssetSource/Unity/BuildAttackClips.cs: until the
    /// Chain event of the strike (0.6 s) and the thrust (0.52 s), the whirl to its end (1.15 s), each at the dual
    /// knives' state speed of 1.1. Declared before the table below, which reads it while the class initialises.
    /// </summary>
    public static readonly float[] RuneSecondsPerHit = { 0.545f, 0.473f, 1.045f };

    private static readonly Dictionary<AttackStyle, StyleMotion> Motions = new()
    {
        [AttackStyle.Slash] = new("swing_longsword", false, 90f, 0.692f, 0.536f, 0.747f),
        [AttackStyle.Chop] = new("swing_axe", false, 90f, 0.801f, 0.557f, 0.861f),
        [AttackStyle.Stab] = new("knife_stab", false, 60f, 0.274f, 0.299f, 0.543f),
        [AttackStyle.Lunge] = new("spear_poke", false, 40f, 0.511f),
        [AttackStyle.Cleave] = new("battleaxe_attack", true, 90f, 1.423f, 0.746f, 1.097f),
        [AttackStyle.Greatsword] = new("greatsword", true, 90f, 0.783f, 0.641f, 0.833f),
        [AttackStyle.Slam] = new("swing_sledge", true, 90f, 1.693f),
        [AttackStyle.Hew] = new("swing_pickaxe", true, 60f, 1.234f),
        [AttackStyle.Polearm] = new("atgeir_attack", true, 20f, 0.667f, 0.7f, 1.148f),
        [AttackStyle.Rune] = new(RuneTrigger, false, 90f, RuneSecondsPerHit)
    };

    /// <summary>
    /// The style a vanilla attack animation belongs to.
    /// </summary>
    /// <param name="trigger">The attack's <c>m_attackAnimation</c>.</param>
    /// <param name="style">The style, if the animation is one of them.</param>
    /// <returns>True if it is.</returns>
    public static bool TryFromTrigger(string trigger, out AttackStyle style)
    {
        foreach (KeyValuePair<AttackStyle, StyleMotion> pair in Motions)
        {
            if (pair.Value.Trigger == trigger)
            {
                style = pair.Key;
                return true;
            }
        }

        style = default;
        return false;
    }

    /// <summary>
    /// The animation trigger of a style, without the chain number the game appends.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <returns>The trigger.</returns>
    public static string Trigger(AttackStyle style) => Motions[style].Trigger;

    /// <summary>
    /// How many hits make one combo of the style.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <returns>The number of hits.</returns>
    public static int ChainLevels(AttackStyle style) => Motions[style].SecondsPerHit.Length;

    /// <summary>
    /// Whether the style holds the weapon with both hands, so it cannot be used with a shield.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <returns>True for two-handed styles.</returns>
    public static bool IsTwoHanded(AttackStyle style) => Motions[style].TwoHanded;

    /// <summary>
    /// Width of the hit area in degrees, from the vanilla weapon the animation belongs to.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <returns>The angle.</returns>
    public static float Angle(AttackStyle style) => Motions[style].Angle;

    /// <summary>
    /// Average seconds between two hits of the style.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <returns>Seconds per hit.</returns>
    public static float SecondsPerHit(AttackStyle style) => Motions[style].SecondsPerHit.Average();

    /// <summary>
    /// The styles a weapon gets when it states none of its own: its vanilla animation first, then the others that suit
    /// the same grip. Null for animations that are not varied (bows, crossbows, staffs).
    /// </summary>
    /// <param name="nativeTrigger">The vanilla weapon's attack animation.</param>
    /// <returns>The styles, signature first, or null.</returns>
    public static AttackStyle[] DefaultPool(string nativeTrigger)
    {
        if (!TryFromTrigger(nativeTrigger, out AttackStyle native))
        {
            return null;
        }

        return native switch
        {
            AttackStyle.Slash => new[] { AttackStyle.Slash, AttackStyle.Chop, AttackStyle.Stab },
            AttackStyle.Chop => new[] { AttackStyle.Chop, AttackStyle.Slash },
            AttackStyle.Stab => new[] { AttackStyle.Stab, AttackStyle.Slash },
            AttackStyle.Lunge => new[] { AttackStyle.Lunge, AttackStyle.Stab },
            AttackStyle.Cleave => new[] { AttackStyle.Cleave, AttackStyle.Greatsword, AttackStyle.Hew },
            AttackStyle.Greatsword => new[] { AttackStyle.Greatsword, AttackStyle.Cleave, AttackStyle.Slam },
            AttackStyle.Slam => new[] { AttackStyle.Slam, AttackStyle.Cleave, AttackStyle.Hew },
            AttackStyle.Polearm => new[] { AttackStyle.Polearm, AttackStyle.Cleave },
            _ => null
        };
    }

    /// <summary>
    /// Factor on each hit's damage in a style, so the weapon deals as much damage per second as in its own style.
    /// A combo's last hit counts with the weapon's finishing multiplier; single-hit styles have no finisher.
    /// </summary>
    /// <param name="native">The weapon's own style.</param>
    /// <param name="style">The style swung.</param>
    /// <param name="lastChainMultiplier">The weapon's <c>m_lastChainDamageMultiplier</c>.</param>
    /// <param name="evenOut">0 keeps the damage per hit, 1 evens out damage per second fully.</param>
    /// <returns>The factor.</returns>
    public static float DamageFactor(AttackStyle native, AttackStyle style, float lastChainMultiplier, float evenOut)
    {
        float ratio = DamagePerSecond(native, lastChainMultiplier) / DamagePerSecond(style, lastChainMultiplier);
        return Clamp((float)Math.Pow(ratio, evenOut));
    }

    /// <summary>
    /// Factor on each hit's stamina in a style, so the weapon costs as much stamina per second as in its own style.
    /// </summary>
    /// <param name="native">The weapon's own style.</param>
    /// <param name="style">The style swung.</param>
    /// <param name="evenOut">0 keeps the stamina per hit, 1 evens out stamina per second fully.</param>
    /// <returns>The factor.</returns>
    public static float StaminaFactor(AttackStyle native, AttackStyle style, float evenOut)
    {
        float ratio = SecondsPerHit(style) / SecondsPerHit(native);
        return Clamp((float)Math.Pow(ratio, evenOut));
    }

    /// <summary>
    /// Picks the style of a new combo: the first (the weapon's signature) with the given share, else one of the
    /// others with equal chances.
    /// </summary>
    /// <param name="styles">The styles that can be used now, signature first.</param>
    /// <param name="signatureShare">Chance of the signature, 0 to 1.</param>
    /// <param name="roll">A random number from 0 (inclusive) to 1 (exclusive).</param>
    /// <returns>The style.</returns>
    public static AttackStyle Pick(IReadOnlyList<AttackStyle> styles, float signatureShare, float roll)
    {
        if (styles.Count == 1 || roll < signatureShare || signatureShare >= 1f)
        {
            return styles[0];
        }

        int others = styles.Count - 1;
        int index = 1 + (int)((roll - signatureShare) / (1f - signatureShare) * others);
        return styles[Math.Min(index, others)];
    }

    private static float DamagePerSecond(AttackStyle style, float lastChainMultiplier)
    {
        int hits = ChainLevels(style);
        float averageHit = hits > 1 ? (hits - 1 + lastChainMultiplier) / hits : 1f;
        return averageHit / SecondsPerHit(style);
    }

    private static float Clamp(float factor)
    {
        return Math.Max(MinFactor, Math.Min(MaxFactor, factor));
    }

    private sealed class StyleMotion
    {
        public StyleMotion(string trigger, bool twoHanded, float angle, params float[] secondsPerHit)
        {
            Trigger = trigger;
            TwoHanded = twoHanded;
            Angle = angle;
            SecondsPerHit = secondsPerHit;
        }

        public string Trigger { get; }

        public bool TwoHanded { get; }

        public float Angle { get; }

        public float[] SecondsPerHit { get; }
    }
}
