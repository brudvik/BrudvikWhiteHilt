namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// The rules that keep a Nail Ship from being hurt by its own side, from plain values so they can be tested without the
/// game. A hit from the player, the player's ships, rams and ballistas is never ignored.
/// </summary>
public static class FriendlyFire
{
    /// <summary>
    /// Whether a hit on a piece of a ship is to be ignored as friendly fire.
    /// </summary>
    /// <param name="targetOnRaiderShip">Whether the piece belongs to a Nail Ship.</param>
    /// <param name="hasAttacker">Whether the hit names an attacker at all.</param>
    /// <param name="attackerIsRaider">Whether the attacker is a Nail Ship crew member or a raid guard, of any ship.</param>
    /// <param name="isTurretShot">Whether the hit is a turret shot, the type of a ballista bolt.</param>
    /// <returns>True when the hit is to be dropped.</returns>
    public static bool IgnoresHit(bool targetOnRaiderShip, bool hasAttacker, bool attackerIsRaider, bool isTurretShot)
    {
        if (!targetOnRaiderShip)
        {
            return false;
        }

        if (hasAttacker)
        {
            return attackerIsRaider;
        }

        // A Nail Ship's ballista fires with no owner, while the player's ballistas always carry the player. A land turret
        // with no owner would be ignored against a Nail Ship too, which is acceptable.
        return isTurretShot;
    }

    /// <summary>
    /// Whether the collision of two bodies is to be ignored: two Nail Ships that sail into each other do no harm.
    /// </summary>
    /// <param name="selfIsRaiderShip">Whether the body with the impact effect is a Nail Ship.</param>
    /// <param name="otherIsRaiderShip">Whether the body it struck is a Nail Ship.</param>
    /// <returns>True when the collision is to be ignored.</returns>
    public static bool IgnoresCollision(bool selfIsRaiderShip, bool otherIsRaiderShip)
    {
        return selfIsRaiderShip && otherIsRaiderShip;
    }
}
