using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.ShipUpgrades;
using BrudvikWhiteHilt.Pieces.Ships.Ballista;
using BrudvikWhiteHilt.SeaBattles.Damage;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>
/// On the Nail Ship, on every client. The ship's owner makes its crew throw pitch pots at the deck of the ship it fights,
/// and, from the stage of the world the settings name, has a ballista on its bow shoot Ship Bolts at that ship's hull. The
/// pots are thrown on a ballistic arc led ahead of the target, by a living crew member, who owns the pot so it does not hurt
/// the crew's own side. The ballista is the player ballista's own model, for show: every client turns it towards the target
/// from the ZDO's target id, and only the owner shoots. A hit on a deck is turned into a fire by <see cref="ShotHits"/>.
/// </summary>
public class NailShipWeapons : MonoBehaviour
{
    // The ship's weapons are looked at this often, and the world's key for the ballista this often.
    private const float TickInterval = 0.25f;
    private const float KeyInterval = 2f;

    // A pot is thrown from this high above the feet of the crew member, and aimed at a deck point scattered this far
    // round the player it threatens.
    private const float ChestHeight = 1.5f;
    private const float DeckScatter = 1.5f;

    // A throw or shot that could not be made (no thrower, out of reach, no arc) is tried again after this long.
    private const float RetrySeconds = 1.5f;

    // The model follows a target this much farther than the ballista shoots, so it is already turned when the range is met.
    private const float FollowMargin = 1.2f;

    // The bolt leaves this far in front of the muzzle, clear of the ballista's own box.
    private const float MuzzleAdvance = 0.3f;

    // Without a model (a dedicated server draws none) the bolt leaves this far above the bow holdfast.
    private const string HoldfastPath = "interactive/front";
    private const float HoldfastLift = 1.2f;

    private static readonly List<RaiderCrew> crewBuffer = new();

    private RaiderShip raider;
    private Ship ship;
    private ZNetView nview;
    private ShipBallista ballista;
    private Ship target;
    private bool ballistaOn;
    private float nextTick;
    private float nextKeyCheck;
    private float nextPot;
    private float nextBolt;
    private bool warnedPot;
    private bool warnedBolt;

    /// <summary>Whether the world has reached the stage from which this ship carries a ballista.</summary>
    public bool HasBallista => ballistaOn;

    private void Awake()
    {
        raider = GetComponent<RaiderShip>();
        ship = GetComponent<Ship>();
        nview = GetComponent<ZNetView>();
        ballista = GetComponentInChildren<ShipBallista>(true);
        if (raider == null || ship == null || nview == null || nview.GetZDO() == null)
        {
            enabled = false;
        }
    }

    // Everyone looks at the target a few times a second and turns the ballista's model every frame; only the owner throws
    // and shoots.
    private void Update()
    {
        if (!nview.IsValid())
        {
            return;
        }

        if (Time.time >= nextTick)
        {
            nextTick = Time.time + TickInterval;
            Tick();
        }

        PoseBallista();
    }

    private void Tick()
    {
        if (Time.time >= nextKeyCheck)
        {
            nextKeyCheck = Time.time + KeyInterval;
            RefreshBallista();
        }

        RaiderState state = raider.State;
        target = raider.TargetShip;
        if (!nview.IsOwner() || target == null)
        {
            return;
        }

        if (WeaponSettings.PitchPots.Value && WeaponRules.CanThrow(state))
        {
            TryThrowPot();
        }

        if (ballistaOn && WeaponRules.CanShoot(state))
        {
            TryShootBolt();
        }
    }

    // Switches the ballista's model on or off with the world's key, on every client. A ship whose model found no deck stays
    // without one, and a dedicated server keeps the state without a model.
    private void RefreshBallista()
    {
        ballistaOn = WeaponSettings.NailBallista.Value && WeaponRules.KeyReached(WeaponSettings.NailBallistaKey.Value,
            key => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key));
        if (ballista != null && !VisualHelper.IsHeadless && !ballista.PlaceFailed && ballista.gameObject.activeSelf != ballistaOn)
        {
            ballista.gameObject.SetActive(ballistaOn);
        }
    }

    // Turns the model towards the target while the ship is fighting it, and lets it rest otherwise.
    private void PoseBallista()
    {
        if (!ballistaOn || ballista == null || !ballista.isActiveAndEnabled)
        {
            return;
        }

        bool aiming = target != null && WeaponRules.CanShoot(raider.State)
            && WeaponRules.InRange(Vector3.Distance(transform.position, target.transform.position), WeaponSettings.NailBallistaRange.Value * FollowMargin);
        ballista.AimAt(aiming ? HullPoint(target) : Vector3.zero, aiming);
    }

    // One pot, every PitchPotSeconds: a living crew member throws it at a deck point of the target, led by the target's
    // course. The pot is a real projectile that belongs to the thrower.
    private void TryThrowPot()
    {
        if (Time.time < nextPot)
        {
            return;
        }

        if (nextPot <= 0f)
        {
            nextPot = Time.time + WeaponSettings.PitchPotSeconds.Value * Random.Range(0.5f, 1f);
            return;
        }

        RaiderCrew thrower = PickThrower();
        ItemDrop.ItemData pot = Item(WeaponSettings.PitchPotName);
        Attack attack = pot?.m_shared?.m_attack;
        if (pot == null || attack == null || attack.m_attackProjectile == null)
        {
            if (!warnedPot)
            {
                warnedPot = true;
                Jotunn.Logger.LogWarning($"Nail Ship: no {WeaponSettings.PitchPotName} to throw; its crew throws nothing.");
            }

            nextPot = Time.time + WeaponSettings.PitchPotSeconds.Value;
            return;
        }

        Vector3 from = thrower != null ? thrower.Character.transform.position + Vector3.up * ChestHeight : Vector3.zero;
        if (thrower == null || !TryDeckSpot(out Vector3 spot) || !WeaponRules.InRange(Vector3.Distance(from, spot), WeaponSettings.PitchPotRange.Value)
            || !TryArc(from, spot, attack, out Vector3 velocity))
        {
            nextPot = Time.time + RetrySeconds;
            return;
        }

        nextPot = Time.time + WeaponSettings.PitchPotSeconds.Value;
        GameObject projectile = Instantiate(attack.m_attackProjectile, from, Quaternion.LookRotation(velocity));
        HitData hit = new()
        {
            m_pushForce = pot.m_shared.m_attackForce,
            m_backstabBonus = pot.m_shared.m_backstabBonus,
            m_blockable = pot.m_shared.m_blockable,
            m_dodgeable = pot.m_shared.m_dodgeable,
            m_skill = pot.m_shared.m_skillType,
            m_itemWorldLevel = (byte)Game.m_worldLevel
        };
        hit.m_damage.Add(pot.GetDamage());
        projectile.GetComponent<IProjectile>()?.Setup(thrower.Character, velocity, -1f, hit, null, null);
    }

    // One bolt, every NailBallistaSeconds, at the target's hull at the waterline, led ahead of it; nobody owns the bolt, so it
    // hurts whatever it hits. The model is turned to the target by every client; the bolt starts at its muzzle.
    private void TryShootBolt()
    {
        if (Time.time < nextBolt)
        {
            return;
        }

        if (nextBolt <= 0f)
        {
            nextBolt = Time.time + WeaponSettings.NailBallistaSeconds.Value * Random.Range(0.5f, 1f);
            return;
        }

        ItemDrop.ItemData ammo = Item(ShipBolts.PrefabName);
        Attack attack = ammo?.m_shared?.m_attack;
        if (ammo == null || attack == null || attack.m_attackProjectile == null)
        {
            if (!warnedBolt)
            {
                warnedBolt = true;
                Jotunn.Logger.LogWarning($"Nail Ship: no {ShipBolts.PrefabName} to shoot; its ballista stays quiet.");
            }

            nextBolt = Time.time + WeaponSettings.NailBallistaSeconds.Value;
            return;
        }

        Vector3 from = MuzzlePosition();
        Vector3 hull = HullPoint(target);
        if (!WeaponRules.InRange(Vector3.Distance(from, hull), WeaponSettings.NailBallistaRange.Value)
            || !TryArc(from, hull, attack, out Vector3 velocity))
        {
            nextBolt = Time.time + RetrySeconds;
            return;
        }

        nextBolt = Time.time + WeaponSettings.NailBallistaSeconds.Value;
        Vector3 direction = velocity.normalized;
        ShipShot.Fire(from + direction * MuzzleAdvance, direction, ammo, null, -1f, WeaponSettings.NailBallistaAccuracy.Value);
    }

    // The launch velocity of an attack's projectile at a point of the target, led by the target's course: the flat arc, or
    // the steep one when the flat one does not reach. Gravity comes from the projectile's own prefab.
    private bool TryArc(Vector3 from, Vector3 point, Attack attack, out Vector3 velocity)
    {
        Projectile prefab = attack.m_attackProjectile.GetComponent<Projectile>();
        float gravity = prefab != null ? prefab.m_gravity : 0f;
        Vector3 course = raider.TargetVelocity;
        return Ballistics.Intercept(from, point, course, attack.m_projectileVel, gravity, false, out velocity)
            || Ballistics.Intercept(from, point, course, attack.m_projectileVel, gravity, true, out velocity);
    }

    // A fresh copy of an item's data from the game's item list, so damage, speed and ammunition are the ones the settings gave.
    private static ItemDrop.ItemData Item(string prefabName)
    {
        GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName) : null;
        return prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData.Clone() : null;
    }

    // A living crew member of this ship who is still aboard, picked at random.
    private RaiderCrew PickThrower()
    {
        crewBuffer.Clear();
        ZDOID id = nview.GetZDO().m_uid;
        foreach (RaiderCrew member in RaiderCrew.All)
        {
            if (member.ShipId == id && member.Alive && !member.Boarded && member.Character != null)
            {
                crewBuffer.Add(member);
            }
        }

        return crewBuffer.Count > 0 ? crewBuffer[Random.Range(0, crewBuffer.Count)] : null;
    }

    // A point on the target's deck: beside one of the players aboard it, so the pot threatens someone, else anywhere on it.
    private bool TryDeckSpot(out Vector3 spot)
    {
        spot = Vector3.zero;
        List<Player> aboard = new();
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != null && !player.IsDead() && target.IsPlayerInBoat(player))
            {
                aboard.Add(player);
            }
        }

        Vector3 near = target.transform.position;
        if (aboard.Count > 0)
        {
            Vector2 scatter = Random.insideUnitCircle * DeckScatter;
            near = aboard[Random.Range(0, aboard.Count)].transform.position + new Vector3(scatter.x, 0f, scatter.y);
        }

        return RaiderDeck.TryDeckPoint(target, near, out spot);
    }

    // The middle of the target's hull at the waterline, as the damage component measures it, else the ship's own position.
    private static Vector3 HullPoint(Ship other)
    {
        ShipDamage damage = ShipDamage.Of(other);
        return damage != null ? other.transform.TransformPoint(damage.HullCentre) : other.transform.position;
    }

    // Where the bolt leaves: the model's muzzle when this machine has one, else a point above the bow holdfast.
    private Vector3 MuzzlePosition()
    {
        if (ballista != null && ballista.isActiveAndEnabled)
        {
            return ballista.MuzzlePosition;
        }

        Transform holdfast = ship.transform.Find(HoldfastPath);
        return (holdfast != null ? holdfast.position : ship.transform.position) + ship.transform.up * HoldfastLift;
    }
}
