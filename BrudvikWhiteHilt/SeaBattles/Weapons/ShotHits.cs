using BrudvikWhiteHilt.SeaBattles.Damage;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Weapons;

/// <summary>What a projectile of the ship weapons does when it hits something, besides the game's own damage.</summary>
public enum ShotKind
{
    /// <summary>A pitch pot: sets the deck it lands on alight.</summary>
    PitchPot = 0,

    /// <summary>A chain shot: tears the sail of the ship it hits.</summary>
    ChainShot = 1,

    /// <summary>A grapnel bolt: binds the ship it hits to the ship it was shot from.</summary>
    Grapnel = 2
}

/// <summary>
/// On each projectile clone of the ship weapons: it listens to the projectile's hit and does what the weapon does beyond
/// damage. The hit is reported on the machine that simulates the projectile, which is the machine of whoever threw or shot
/// it, so everything here only sends RPCs; the owner of the ship that was hit decides what happens.
/// </summary>
public class ShotHits : MonoBehaviour
{
    // A pot lands on the deck, a rail or the mast; the fire is made at the deck point nearest the landing if that is no
    // farther than this, and a pot that hits the hull's side below the rail makes no fire.
    private const float DeckReach = 2.5f;

    // A rope is made fast this high above the deck, as a cleat on a rail is, and the holder's end is looked for this far out
    // from the ballista towards the other ship, so it lands on the rail and not on the ballista.
    private const float CleatHeight = 0.8f;
    private const float RailSearch = 4f;

    private static bool groundFireLogged;

    /// <summary>What this projectile does when it hits. Set when the prefab is made.</summary>
    public ShotKind m_kind;

    private Projectile projectile;
    private Ship shooterShip;
    private Vector3 shooterBallista;

    /// <summary>
    /// Tells a shot which ship it was shot from, and where that ship's ballista stands. Called on the machine that
    /// fires it, right after launch: the gunner may have let go of the ballista when the shot lands.
    /// </summary>
    /// <param name="ship">The ship the gunner mans.</param>
    /// <param name="ballistaLocal">Where the ballista stands, in the ship's local space.</param>
    public void SetShooter(Ship ship, Vector3 ballistaLocal)
    {
        shooterShip = ship;
        shooterBallista = ballistaLocal;
    }

    private void Awake()
    {
        projectile = GetComponent<Projectile>();
    }

    private void Start()
    {
        if (projectile != null)
        {
            projectile.m_onHit += OnHit;
        }
    }

    // Runs where the projectile is simulated. A failure here is logged and never reaches the game's hit code.
    private void OnHit(Collider collider, Vector3 point, bool water)
    {
        if (projectile == null || projectile.m_nview == null || !projectile.m_nview.IsValid() || !projectile.m_nview.IsOwner())
        {
            return;
        }

        try
        {
            switch (m_kind)
            {
                case ShotKind.PitchPot:
                    PitchPotHit(collider, point, water);
                    break;
                case ShotKind.ChainShot:
                    ChainShotHit(collider, water);
                    break;
                case ShotKind.Grapnel:
                    GrapnelHit(collider, point, water);
                    break;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Ship weapons: a hit of {name} was not handled: {ex}");
        }
    }

    // A pot that lands on a ship's deck sets it alight; one that falls in the water does nothing. Ground fires are not made:
    // the only fire patch the mod has belongs to the dragon (it needs a dragon as its source and the dragon's own settings),
    // so a pot on dry ground leaves nothing but its damage.
    private void PitchPotHit(Collider collider, Vector3 point, bool water)
    {
        if (water)
        {
            return;
        }

        Rigidbody body = collider != null ? collider.attachedRigidbody : null;
        Ship ship = body != null ? body.GetComponent<Ship>() : null;
        if (ship == null)
        {
            LogGroundOnce(collider);
            return;
        }

        // The Nail Ship's crew would set its own deck alight with a pot that hits its rail.
        bool crewPot = projectile.m_owner != null && RaiderCrew.Of(projectile.m_owner.GetComponent<MonsterAI>()) != null;
        if (!DamageSettings.Enabled.Value || (crewPot && RaiderShip.Is(ship)))
        {
            return;
        }

        DeckFires fires = DeckFires.Of(ship);
        if (fires != null && RaiderDeck.TryDeckPoint(ship, point, out Vector3 deck) && Vector3.Distance(deck, point) <= DeckReach)
        {
            fires.RequestFire(deck);
        }
    }

    // A chain shot that hits another ship tears its sail: the ship's owner is asked to do it. A hit on the ship it was shot
    // from, such as the bow, does nothing.
    private void ChainShotHit(Collider collider, bool water)
    {
        Ship ship = water ? null : ShipOf(collider);
        if (ship == null || ship == shooterShip)
        {
            return;
        }

        ShipDamage.Of(ship)?.RequestTear();
    }

    // A grapnel bolt that hits another ship asks the ship it was shot from to make a rope fast between its own rail, on the
    // side facing the hit, and the deck point nearest the hit. That ship's owner decides; a shot with no ship behind it (the
    // Nail Ship's own) makes no rope.
    private void GrapnelHit(Collider collider, Vector3 point, bool water)
    {
        Ship target = water ? null : ShipOf(collider);
        if (target == null || shooterShip == null || target == shooterShip)
        {
            return;
        }

        GrappleLink link = GrappleLink.Of(shooterShip);
        if (link == null)
        {
            return;
        }

        Vector3 hook = RaiderDeck.TryDeckPoint(target, point, out Vector3 deck) ? deck : point;
        hook += target.transform.up * CleatHeight;
        Transform own = shooterShip.transform;
        float side = own.InverseTransformPoint(hook).x >= 0f ? 1f : -1f;
        Vector3 outboard = own.TransformPoint(shooterBallista) + own.right * (side * RailSearch);
        Vector3 cleat = RaiderDeck.TryDeckPoint(shooterShip, outboard, out Vector3 rail) ? rail : outboard;
        cleat += own.up * CleatHeight;
        link.RequestGrapple(target, own.InverseTransformPoint(cleat), target.transform.InverseTransformPoint(hook));
    }

    private static Ship ShipOf(Collider collider)
    {
        Rigidbody body = collider != null ? collider.attachedRigidbody : null;
        return body != null ? body.GetComponent<Ship>() : null;
    }

    private static void LogGroundOnce(Collider collider)
    {
        if (!groundFireLogged && collider != null && collider.GetComponent<Heightmap>() != null)
        {
            groundFireLogged = true;
            Jotunn.Logger.LogInfo("Pitch pot: a pot that lands on dry ground makes no ground fire, as the dragon's fire patch has no source-less form.");
        }
    }
}
