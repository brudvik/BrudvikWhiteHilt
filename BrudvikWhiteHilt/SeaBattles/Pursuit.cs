using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// The pursuit speed cap, on the machine that owns the hunted ship. A ship is hunted while a loaded Nail Ship that is
/// neither moored nor a prize has it as its target. The first hunter starts a chase; the ship is then held below the
/// raiders' speed for the shock, and afterwards for as long as live Nail crew stand on it. The reference speed is the
/// fastest hunter's horizontal speed over the water, smoothed (quickly up, slowly down) so a raider's turn or a gust does
/// not move the cap; the raiders' sails are fixed by the tailwind, so their speed is what they can really reach. The phase
/// is written to the hunted ship's data, so every client can show the messages and the HUD line.
/// </summary>
public static class Pursuit
{
    // A chase ends, and the next attack gets a new shock, when nobody has hunted the ship or fought aboard for this long.
    private const float GraceSeconds = 30f;

    // The cap brakes the ship by at most this much per second.
    private const float Brake = 3f;

    // Whether Nail crew are aboard is looked up this often.
    private const float AboardInterval = 0.5f;

    private static readonly int PhaseKey = "whitehilt_pursuit_phase".GetStableHashCode();
    private static readonly Dictionary<Ship, Chase> chases = new();
    private static readonly Dictionary<Ship, PursuitPhase> shown = new();

    private sealed class Chase
    {
        public float Start;
        public float LastActive;
        public float Reference;
        public float NextAboard;
        public bool EnemiesAboard;
    }

    /// <summary>Whether a chase is still being kept on this machine, so the cap can be lifted after the raiders unload.</summary>
    public static bool Active => chases.Count > 0;

    /// <summary>
    /// Runs after the physics step of every ship, while Nail Ships are loaded: the owner of a hunted ship caps its speed,
    /// and a player aboard is told when the cap changes.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public static void Step(Ship ship)
    {
        if (ship == null || ship.m_nview == null || !ship.m_nview.IsValid() || RaiderShip.Is(ship))
        {
            return;
        }

        if (ship.m_nview.IsOwner() && SeaBattleSettings.PursuitEnabled.Value)
        {
            Own(ship);
        }

        Announce(ship);
    }

    /// <summary>
    /// The phase of a ship's chase as its owner last wrote it.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>The phase; free for a ship that is not chased.</returns>
    public static PursuitPhase PhaseOf(Ship ship)
    {
        if (ship == null || ship.m_nview == null || !ship.m_nview.IsValid())
        {
            return PursuitPhase.Free;
        }

        return (PursuitPhase)ship.m_nview.GetZDO().GetInt(PhaseKey);
    }

    /// <summary>
    /// The HUD line for a held ship.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <returns>The text, or null when the ship is free.</returns>
    public static string HudLine(Ship ship)
    {
        return PhaseOf(ship) == PursuitPhase.Free ? null : Localization.instance.Localize("$whitehilt_pursuit_hud");
    }

    private static void Own(Ship ship)
    {
        float now = Time.time;
        float fastest = HuntersSpeed(ship, out bool hunted);
        chases.TryGetValue(ship, out Chase chase);

        if (chase == null)
        {
            if (!hunted)
            {
                return;
            }

            chase = new Chase { Start = now, LastActive = now };
            chases[ship] = chase;
        }

        if (hunted)
        {
            chase.LastActive = now;
            chase.Reference = PursuitRules.Reference(chase.Reference, fastest, Time.fixedDeltaTime);
        }

        if (now >= chase.NextAboard)
        {
            chase.EnemiesAboard = EnemiesAboard(ship);
            chase.NextAboard = now + AboardInterval;
        }

        if (chase.EnemiesAboard)
        {
            chase.LastActive = now;
        }
        else if (now - chase.LastActive > GraceSeconds)
        {
            chases.Remove(ship);
            Write(ship, PursuitPhase.Free);
            return;
        }

        PursuitPhase phase = PursuitRules.Phase(now - chase.Start, SeaBattleSettings.PursuitShockSeconds.Value, chase.EnemiesAboard);
        Write(ship, phase);
        float? cap = PursuitRules.Cap(phase, chase.Reference, SeaBattleSettings.PursuitSpeedFactor.Value, SeaBattleSettings.PursuitFloorSpeed.Value);
        Clamp(ship, cap);
    }

    // The fastest horizontal speed among the Nail Ships that hunt this ship.
    private static float HuntersSpeed(Ship ship, out bool hunted)
    {
        hunted = false;
        float fastest = 0f;
        foreach (RaiderShip raider in RaiderShip.All)
        {
            if (raider.State is RaiderState.Moored or RaiderState.Prize || raider.Body == null || raider.TargetShip != ship)
            {
                continue;
            }

            hunted = true;
            Vector3 velocity = raider.Body.linearVelocity;
            fastest = Mathf.Max(fastest, new Vector2(velocity.x, velocity.z).magnitude);
        }

        return fastest;
    }

    // Whether a living, boarded Nail crew member stands on this ship.
    private static bool EnemiesAboard(Ship ship)
    {
        foreach (RaiderCrew crew in RaiderCrew.All)
        {
            if (crew.Alive && crew.Boarded && crew.Character.GetStandingOnShip() == ship)
            {
                return true;
            }
        }

        return false;
    }

    private static void Clamp(Ship ship, float? cap)
    {
        Rigidbody body = ship.GetComponent<Rigidbody>();
        if (cap == null || body == null)
        {
            return;
        }

        Vector3 velocity = body.linearVelocity;
        Vector2 flat = new(velocity.x, velocity.z);
        float speed = flat.magnitude;
        float limited = PursuitRules.Limit(speed, cap, Time.fixedDeltaTime, Brake);
        if (speed > 0.001f && limited < speed)
        {
            flat *= limited / speed;
            body.linearVelocity = new Vector3(flat.x, velocity.y, flat.y);
        }
    }

    private static void Write(Ship ship, PursuitPhase phase)
    {
        if (PhaseOf(ship) != phase)
        {
            ship.m_nview.GetZDO().Set(PhaseKey, (int)phase);
        }
    }

    // On every client: tell the local player aboard when the phase changes.
    private static void Announce(Ship ship)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.GetStandingOnShip() != ship)
        {
            return;
        }

        PursuitPhase phase = PhaseOf(ship);
        shown.TryGetValue(ship, out PursuitPhase before);
        if (phase == before)
        {
            return;
        }

        if (phase == PursuitPhase.Free)
        {
            shown.Remove(ship);
        }
        else
        {
            shown[ship] = phase;
        }

        string token = phase switch
        {
            PursuitPhase.Shock => "$msg_whitehilt_pursuit_shock",
            PursuitPhase.Held => "$msg_whitehilt_pursuit_held",
            _ => "$msg_whitehilt_pursuit_free",
        };
        player.Message(MessageHud.MessageType.Center, token);
    }
}
