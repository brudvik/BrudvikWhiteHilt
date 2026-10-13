using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.ShipUpgrades;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using BrudvikWhiteHilt.SeaBattles.Damage;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.Ram;

/// <summary>
/// Lets a White Hilt Ship with the Ram Prow ram what it strikes bow-first: another ship, which it always holes, or a
/// creature, which it hurts and pushes. Sits on the ship's root, where the game delivers the ship's collisions. Only the
/// ship's owner rams, so a ram happens once however many players are near.
/// </summary>
public class ShipRam : MonoBehaviour
{
    // The camera shake of a ram, felt by everyone near: it reaches this far and is full at 10 m/s.
    private const float ShakeRange = 40f;
    private const float ShakeFullSpeed = 10f;
    private const float ShakeLeast = 0.2f;
    private const string ShakeRpc = "WhiteHiltShipRamShake";

    // Old entries of the cooldown list are dropped when it holds this many.
    private const int CooldownListSize = 32;
    private const float CooldownKeep = 60f;
    private const float NeverRammed = -1f;

    private readonly Dictionary<int, float> rammedAt = new();
    private ZNetView nview;
    private Ship ship;
    private WearNTear hull;
    private WhiteHiltShipUpgrades upgrades;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        ship = GetComponent<Ship>();
        hull = GetComponent<WearNTear>();
        upgrades = GetComponent<WhiteHiltShipUpgrades>();
        if (nview != null && nview.GetZDO() != null)
        {
            nview.Register<Vector3, float>(ShakeRpc, RPC_Shake);
        }
    }

    // The game sends this to the ship's root for every body the hull strikes, on every machine that simulates it; only the
    // ship's owner goes on. A ram is a strike by the bow's front quarter at a closing speed of at least the setting, on a ship
    // or a creature that has not been rammed within the cooldown.
    private void OnCollisionEnter(Collision collision)
    {
        if (ship == null || nview == null || !nview.IsValid() || !nview.IsOwner() || upgrades == null
            || !RamSettings.Enabled.Value || !upgrades.Has(ShipRamUpgrade.Bit))
        {
            return;
        }

        Rigidbody otherBody = collision.rigidbody;
        Ship otherShip = otherBody != null ? otherBody.GetComponent<Ship>() : null;
        Character creature = otherShip == null ? collision.collider.GetComponentInParent<Character>() : null;
        if ((otherShip == null && !IsRammable(creature)) || otherShip == ship)
        {
            return;
        }

        Vector3 contact = Foremost(collision, out Vector3 local);
        BoxCollider box = ship.m_floatCollider;
        Vector3 middle = box != null ? transform.InverseTransformPoint(box.transform.TransformPoint(box.center)) : Vector3.zero;
        float length = box != null ? box.size.z * box.transform.lossyScale.z : 0f;
        if (!RamRules.IsBowContact(local - middle, length))
        {
            return;
        }

        float speed = RamRules.ApproachSpeed(collision.relativeVelocity, transform.forward);
        float damage = RamRules.Damage(speed, RamSettings.MinSpeed.Value, RamSettings.DamagePerSpeed.Value);
        int targetId = otherShip != null ? otherShip.GetInstanceID() : creature.GetInstanceID();
        if (damage <= 0f || !CooledDown(targetId))
        {
            return;
        }

        rammedAt[targetId] = Time.time;
        bool hit = otherShip != null ? RamShip(otherShip, contact, damage) : RamCreature(creature, contact, speed, damage);
        if (!hit)
        {
            return;
        }

        SelfDamage(damage);
        Effects(contact, speed);
        Jotunn.Logger.LogDebug($"Ram: {name} struck {(otherShip != null ? otherShip.name : creature.name)} at {speed:F1} m/s for {damage:F0}");
    }

    // A creature that can be rammed: alive, not a player and not tamed (a pet or a mount is not an enemy).
    private static bool IsRammable(Character creature)
    {
        return creature != null && !creature.IsPlayer() && !creature.IsTamed() && !creature.IsDead();
    }

    // The contact point nearest the bow among the contacts of the collision, in world space, and the same point in the
    // ship's space.
    private Vector3 Foremost(Collision collision, out Vector3 local)
    {
        Vector3 best = collision.GetContact(0).point;
        local = transform.InverseTransformPoint(best);
        for (int i = 1; i < collision.contactCount; i++)
        {
            Vector3 point = collision.GetContact(i).point;
            Vector3 inShip = transform.InverseTransformPoint(point);
            if (inShip.z > local.z)
            {
                best = point;
                local = inShip;
            }
        }

        return best;
    }

    private bool CooledDown(int targetId)
    {
        if (rammedAt.Count >= CooldownListSize)
        {
            List<int> old = new();
            foreach (KeyValuePair<int, float> entry in rammedAt)
            {
                if (Time.time - entry.Value > CooldownKeep)
                {
                    old.Add(entry.Key);
                }
            }

            old.ForEach(key => rammedAt.Remove(key));
        }

        return RamRules.CooledDown(rammedAt.TryGetValue(targetId, out float last) ? last : NeverRammed, Time.time, RamSettings.Cooldown.Value);
    }

    // Hurts a ship's hull with a blow half blunt and half chop. The type tells the hull's owner it is a ram, so the hit always
    // makes a hole (see ShipDamage.TryHit). The hit goes to the hull as any hit does, and its owner works it out.
    private bool RamShip(Ship target, Vector3 contact, float damage)
    {
        WearNTear targetHull = target.GetComponent<WearNTear>();
        if (targetHull == null)
        {
            return false;
        }

        HitData hit = NewHit(contact);
        hit.m_damage.m_blunt = damage / 2f;
        hit.m_damage.m_chop = damage / 2f;
        targetHull.Damage(hit);
        return true;
    }

    // Hurts a creature with a blunt blow and pushes it along the bow.
    private bool RamCreature(Character target, Vector3 contact, float speed, float damage)
    {
        HitData hit = NewHit(contact);
        hit.m_damage.m_blunt = damage;
        hit.m_pushForce = RamRules.Push(speed, RamSettings.PushPerSpeed.Value);
        target.Damage(hit);
        return true;
    }

    // A ram's hit: from the bow, along the water. It is credited to the local player when they are aboard, so a creature it
    // kills counts as theirs; with nobody aboard it has no attacker.
    private HitData NewHit(Vector3 contact)
    {
        Vector3 along = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Player player = Player.m_localPlayer;
        return new HitData
        {
            m_hitType = HitData.HitType.Boat,
            m_point = contact,
            m_dir = along,
            m_toolTier = short.MaxValue,
            m_attacker = player != null && ship.IsPlayerInBoat(player) ? player.GetZDOID() : ZDOID.None
        };
    }

    // The ramming ship takes its share of the damage, but only a destructible one does: the White Hilt ships are not.
    private void SelfDamage(float damage)
    {
        ShipDamage own = ShipDamage.Of(ship);
        float share = RamSettings.SelfDamageShare.Value;
        if (hull == null || share <= 0f || (own != null && own.IsImmune) || hull.m_damages.m_blunt == HitData.DamageModifier.Immune)
        {
            return;
        }

        hull.ApplyDamage(damage * share);
    }

    // The ship's own water-impact effect (splash and a thud, as when it slams onto a wave) at the contact, played by the owner
    // as the game plays it, and a camera shake sent to every machine, so all who are near feel the blow. The struck hull plays
    // its own hit effect when it takes the damage.
    private void Effects(Vector3 contact, float speed)
    {
        if (!VisualHelper.IsHeadless)
        {
            ship.m_waterImpactEffect.Create(contact, Quaternion.identity);
        }

        nview.InvokeRPC(ZNetView.Everybody, ShakeRpc, contact, Mathf.Clamp(speed / ShakeFullSpeed, ShakeLeast, 1f));
    }

    private static void RPC_Shake(long sender, Vector3 contact, float strength)
    {
        if (!VisualHelper.IsHeadless && GameCamera.instance != null)
        {
            GameCamera.instance.AddShake(contact, ShakeRange, Mathf.Clamp01(strength), false);
        }
    }
}
