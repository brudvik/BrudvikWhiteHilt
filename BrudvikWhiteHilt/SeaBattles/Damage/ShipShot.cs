using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// Fires a ballista bolt, as the vanilla turret fires one: a little random spread, the ammunition's own damage, push and
/// stagger, and the vanilla ballista's shot effect and hit noise. The Ship Ballista and the Nail Ship's own ballista both
/// shoot through here, one with its gunner as the owner of the bolt and the other with none.
/// </summary>
public static class ShipShot
{
    private const string VanillaBallista = "piece_turret";

    private static Turret vanillaBallista;

    /// <summary>
    /// Fires one bolt.
    /// </summary>
    /// <param name="from">Where the bolt leaves.</param>
    /// <param name="direction">The way it points before the spread is added; need not be a unit vector.</param>
    /// <param name="ammo">The ammunition, whose projectile, speed, damage and spread are used.</param>
    /// <param name="owner">Who shot it, so kills count for them and damage obeys the rules between them and the target;
    /// null for a shot nobody owns, which hurts anything it hits.</param>
    /// <param name="hitNoise">How far the hit is heard by creatures; negative takes the vanilla ballista's noise.</param>
    /// <param name="accuracy">Spread in degrees either way, up and sideways; negative takes the ammunition's own.</param>
    /// <returns>The projectile that was fired, or null when nothing was.</returns>
    public static GameObject Fire(Vector3 from, Vector3 direction, ItemDrop.ItemData ammo, Character owner, float hitNoise, float accuracy = -1f)
    {
        Attack attack = ammo?.m_shared?.m_attack;
        if (attack == null || attack.m_attackProjectile == null || direction.sqrMagnitude < 1e-6f)
        {
            return null;
        }

        Turret template = VanillaTemplate();
        Quaternion rotation = Quaternion.LookRotation(direction);
        if (template != null && template.m_shootEffect != null)
        {
            template.m_shootEffect.Create(from, rotation);
        }

        Vector3 forward = direction.normalized;
        Vector3 axis = Vector3.Cross(forward, Vector3.up);
        float spread = accuracy >= 0f ? accuracy : attack.m_projectileAccuracy;
        forward = Quaternion.AngleAxis(Random.Range(-spread, spread), axis) * forward;
        forward = Quaternion.AngleAxis(Random.Range(-spread, spread), Vector3.up) * forward;

        GameObject projectile = Object.Instantiate(attack.m_attackProjectile, from, rotation);
        HitData hit = new()
        {
            m_toolTier = (short)ammo.m_shared.m_toolTier,
            m_pushForce = ammo.m_shared.m_attackForce,
            m_backstabBonus = ammo.m_shared.m_backstabBonus,
            m_staggerMultiplier = attack.m_staggerMultiplier,
            m_blockable = ammo.m_shared.m_blockable,
            m_dodgeable = ammo.m_shared.m_dodgeable,
            m_skill = ammo.m_shared.m_skillType,
            m_itemWorldLevel = (byte)Game.m_worldLevel,
            m_hitType = HitData.HitType.Turret
        };
        hit.m_damage.Add(ammo.GetDamage());
        if (ammo.m_shared.m_attackStatusEffect != null)
        {
            hit.m_statusEffectHash = ammo.m_shared.m_attackStatusEffect.NameHash();
        }

        float noise = hitNoise >= 0f ? hitNoise : template != null ? template.m_hitNoise : -1f;
        projectile.GetComponent<IProjectile>()?.Setup(owner, forward * attack.m_projectileVel, noise, hit, null, ammo);
        return projectile;
    }

    // The vanilla ballista's component, for its shot effect and hit noise, found once the game's prefabs exist.
    private static Turret VanillaTemplate()
    {
        if (vanillaBallista == null && ZNetScene.instance != null)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(VanillaBallista);
            vanillaBallista = prefab != null ? prefab.GetComponent<Turret>() : null;
        }

        return vanillaBallista;
    }
}
