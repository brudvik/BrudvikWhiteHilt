using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Thins the ordinary fog around the local player near a White Hilt Ship with the Mast Wisp. The wisp's force field only
/// pushes the Mistlands mist particles; the fog of misty weather is a render setting the game sets every update.
/// </summary>
public static class ShipFog
{
    // The vanilla wisplight's force field reaches 15 m; the wisp sits on a 15 m ship, so its reach counts from the mast.
    private static float Reach => ShipSettings.MastWispReach.Value;
    private const float FadeSeconds = 2f;

    private static float factor = 1f;

    /// <summary>
    /// Scales the fog the game has just set. Called after <see cref="EnvMan.SetEnv"/>.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    public static void Apply(float dt)
    {
        Player player = Player.m_localPlayer;
        bool near = player != null && WhiteHiltShipUpgrades.IsNearMastWisp(player.transform.position, Reach);
        float target = near ? Mathf.Clamp01(ShipSettings.MastWispFogLeft.Value) : 1f;
        factor = Mathf.MoveTowards(factor, target, dt / FadeSeconds);
        if (factor < 1f)
        {
            RenderSettings.fogDensity *= factor;
        }
    }
}
