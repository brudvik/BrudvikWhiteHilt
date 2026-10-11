using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Items.Summoning;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// The White Dragon's blasts: where each breath first strikes, ground, water or a foe, and where it slams down at the
/// end of a dive, the fire or frost bursts and batters every enemy of the dragon around it and throws them back.
/// Players, tame animals and buildings are spared. Runs on the machine that owns the dragon.
/// </summary>
public class DragonBlast : MonoBehaviour
{
    // Yagluth's meteor impact and the Deep North's frost meteor: a burst with its own sound, light and camera shake,
    // shown to every player.
    private const string FireEffect = "fx_goblinking_meteor_hit";
    private const string FrostEffect = "fx_fimbulvinter_meteor_hit";
    private const float EdgeShare = 0.5f;
    private const float Lift = 0.3f;

    private bool struck;

    /// <summary>Lets the next breath blast again.</summary>
    public void BeginBreath()
    {
        struck = false;
    }

    /// <summary>
    /// Blasts at the first strike of a breath.
    /// </summary>
    /// <param name="projectile">The flame or shard that struck, with its damage scaled for the dragon's stars.</param>
    /// <param name="point">Where it struck.</param>
    public void Strike(Projectile projectile, Vector3 point)
    {
        Character dragon = projectile.m_owner;
        if (struck || dragon == null)
        {
            return;
        }

        struck = true;
        HitData.DamageTypes shot = projectile.m_damage;
        HitData.DamageTypes unscaled = projectile.m_weapon?.m_shared.m_damages ?? default;
        bool frost = shot.m_frost > shot.m_fire;
        // The breath carries the dragon's level bonus; the blast gets the same.
        float baseElement = unscaled.m_fire + unscaled.m_frost;
        float factor = baseElement > 0f ? (shot.m_fire + shot.m_frost) / baseElement : 1f;
        Slam(dragon, point, DragonHorn.BlastDamage.Value * factor, DragonHorn.BlastRadius.Value, DragonHorn.BlastPush.Value, frost);
    }

    /// <summary>
    /// Bursts at a point: every enemy of the dragon within the radius takes the damage, half the element and half blunt,
    /// falling to half at the edge, and is thrown away from the point.
    /// </summary>
    /// <param name="dragon">The dragon.</param>
    /// <param name="point">The centre of the burst.</param>
    /// <param name="damage">Damage at the centre, already scaled.</param>
    /// <param name="radius">Radius in metres.</param>
    /// <param name="push">How hard enemies are thrown.</param>
    /// <param name="frost">Frost instead of fire.</param>
    public static void Slam(Character dragon, Vector3 point, float damage, float radius, float push, bool frost)
    {
        if (damage <= 0f || dragon == null)
        {
            return;
        }

        GameObject effect = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(frost ? FrostEffect : FireEffect) : null;
        if (effect != null)
        {
            Instantiate(effect, point, Quaternion.identity);
        }

        foreach (Character target in Character.GetAllCharacters())
        {
            if (target == dragon || target.IsDead() || !BaseAI.IsEnemy(dragon, target))
            {
                continue;
            }

            Vector3 centre = target.GetCenterPoint();
            float distance = Vector3.Distance(centre, point) - target.GetRadius();
            if (distance > radius)
            {
                continue;
            }

            float share = Mathf.Lerp(1f, EdgeShare, Mathf.Clamp01(distance / radius));
            Vector3 away = centre - point;
            away.y = 0f;
            HitData hit = new()
            {
                m_point = centre,
                m_dir = ((away.sqrMagnitude > 0.01f ? away.normalized : dragon.transform.forward) + Vector3.up * Lift).normalized,
                m_pushForce = push,
                m_attacker = dragon.GetZDOID(),
                m_hitType = HitData.HitType.EnemyHit,
                m_dodgeable = false,
                m_blockable = true
            };
            if (frost)
            {
                hit.m_damage.m_frost = damage * share / 2f;
            }
            else
            {
                hit.m_damage.m_fire = damage * share / 2f;
            }

            hit.m_damage.m_blunt = damage * share / 2f;
            target.Damage(hit);
        }
    }

    /// <summary>
    /// How much a dragon's own blows are raised: by its stars, as the game and the difficulty settings raise every
    /// attack, and by the power it was called with.
    /// </summary>
    /// <param name="dragon">The dragon.</param>
    /// <returns>The damage multiplier.</returns>
    public static float LevelFactor(Character dragon)
    {
        float vanilla = 1f + Mathf.Max(0, dragon.GetLevel() - 1) * 0.5f;
        return CreatureStars.DamageFactor(dragon, vanilla) * WhiteDragonAlly.Power(dragon);
    }
}
