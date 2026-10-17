using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Fades out the Mistlands mist round a White Hilt Ship with the Mast Wisp and ahead of it. The wisp's force field only
/// pushes the mist particles, slower than the ship sails, so on its own the ship ran well into the mist before it gave
/// way; the mist the game emits keeps out of the field, but what already hangs in the air has to be cleared.
/// </summary>
public static class ShipMist
{
    // The clearing reaches this many seconds of sailing ahead of the mast, so the ship meets mist already thinning.
    private const float LeadSeconds = 2.5f;

    // A particle in the clearing fades out over this long, so the mist melts away rather than blinking out.
    private const float FadeSeconds = 0.5f;

    // The clearing reaches this far below the waterline and, above it, as high as the wisp's reach.
    private const float BelowWaterline = 5f;

    // Beyond this from the camera there is no mist to clear; the game only shows it round the player.
    private const float MistRange = 150f;

    private static readonly List<(Vector3 From, Vector3 To, float Waterline)> clearings = new();
    private static ParticleSystem.Particle[] particles = new ParticleSystem.Particle[0];

    /// <summary>
    /// Fades the mist particles inside the clearing of every lit mast wisp near the local player.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    public static void Apply(float dt)
    {
        Player player = Player.m_localPlayer;
        ParticleMist mist = ParticleMist.instance;
        if (player == null || mist == null || mist.m_ps == null || !mist.m_ps.emission.enabled || dt <= 0f)
        {
            return;
        }

        WhiteHiltShipUpgrades.GetMastWispClearings(clearings, LeadSeconds);
        Vector3 here = player.transform.position;
        clearings.RemoveAll(clearing => Utils.DistanceXZ(clearing.From, here) > MistRange);
        if (clearings.Count == 0)
        {
            return;
        }

        ParticleSystem ps = mist.m_ps;
        int max = ps.main.maxParticles;
        if (particles.Length < max)
        {
            particles = new ParticleSystem.Particle[max];
        }

        int count = ps.GetParticles(particles);
        if (count == 0)
        {
            return;
        }

        float reach = WhiteHiltShipUpgrades.MastWispReach(player);
        bool local = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
        int fade = Mathf.Max(1, Mathf.CeilToInt(255f * dt / FadeSeconds));
        bool changed = false;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = local ? ps.transform.TransformPoint(particles[i].position) : particles[i].position;
            if (!Inside(position, reach))
            {
                continue;
            }

            Color32 color = particles[i].startColor;
            if (color.a <= fade)
            {
                particles[i].remainingLifetime = 0f;
            }
            else
            {
                color.a = (byte)(color.a - fade);
                particles[i].startColor = color;
            }

            changed = true;
        }

        if (changed)
        {
            ps.SetParticles(particles, count);
        }
    }

    // Whether a point lies within reach, along the ground, of the stretch a mast wisp clears, and not far above or
    // below the ship.
    private static bool Inside(Vector3 point, float reach)
    {
        foreach ((Vector3 from, Vector3 to, float waterline) in clearings)
        {
            if (point.y < waterline - BelowWaterline || point.y > waterline + reach)
            {
                continue;
            }

            Vector2 a = new(from.x, from.z);
            Vector2 ab = new Vector2(to.x, to.z) - a;
            Vector2 ap = new Vector2(point.x, point.z) - a;
            float along = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude) : 0f;
            if ((ap - ab * along).sqrMagnitude <= reach * reach)
            {
                return true;
            }
        }

        return false;
    }
}
