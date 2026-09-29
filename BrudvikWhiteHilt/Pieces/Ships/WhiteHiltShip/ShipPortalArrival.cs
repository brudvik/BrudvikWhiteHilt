using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// Puts a traveller on the deck of the ship whose portal they went to. The travel list only knows where the ship was a
/// moment ago, so once the teleport is over the player is moved onto the rune circle where the ship is now.
/// </summary>
public static class ShipPortalArrival
{
    // How long after the teleport the ship may take to load before the player is left where they landed.
    private const float WaitForShip = 10f;
    private const float AboveDeck = 0.4f;

    private static ZDOID pendingShip = ZDOID.None;
    private static float giveUpAt;
    private static bool teleportSeen;

    /// <summary>
    /// Remembers the ship the local player is travelling to.
    /// </summary>
    /// <param name="ship">The ship, or <see cref="ZDOID.None"/> for a portal on land.</param>
    public static void Expect(ZDOID ship)
    {
        pendingShip = ship;
        teleportSeen = false;
        giveUpAt = 0f;
    }

    /// <summary>
    /// Moves the local player onto the ship once the teleport is over. Called every frame.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (pendingShip.IsNone())
        {
            return;
        }

        if (player.IsTeleporting())
        {
            teleportSeen = true;
            return;
        }

        if (!teleportSeen)
        {
            // The teleport did not start after all.
            pendingShip = ZDOID.None;
            return;
        }

        if (giveUpAt == 0f)
        {
            giveUpAt = Time.time + WaitForShip;
        }

        GameObject ship = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(pendingShip) : null;
        if (ship == null)
        {
            if (Time.time > giveUpAt)
            {
                pendingShip = ZDOID.None;
            }

            return;
        }

        pendingShip = ZDOID.None;
        Vector3 deck = ship.transform.TransformPoint(ShipPortal.DeckPosition) + Vector3.up * AboveDeck;
        player.transform.position = deck;
        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            Rigidbody shipBody = ship.GetComponent<Rigidbody>();
            body.position = deck;
            body.linearVelocity = shipBody != null ? shipBody.linearVelocity : Vector3.zero;
        }
    }
}
