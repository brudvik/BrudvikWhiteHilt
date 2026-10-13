using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>What a Nail Ship is doing. The number is stored in the ship's ZDO.</summary>
public enum RaiderState
{
    /// <summary>Sails up to its target.</summary>
    Approach = 0,

    /// <summary>Lies alongside at bow-shot range while its archers shoot.</summary>
    Broadside = 1,

    /// <summary>Closes in so its warriors can leap aboard.</summary>
    Board = 2,

    /// <summary>Runs from the fight.</summary>
    Flee = 3,

    /// <summary>The crew is dead and the ship is the players'.</summary>
    Prize = 4,

    /// <summary>Lies at the quay of a draugr harbour.</summary>
    Moored = 5
}

/// <summary>How much sail the ship asks for; <c>RaiderShip</c> maps it to <c>Ship.Speed</c>.</summary>
public enum RaiderSail
{
    /// <summary>No sail.</summary>
    Stop,

    /// <summary>Half sail.</summary>
    Half,

    /// <summary>Full sail.</summary>
    Full
}

/// <summary>What the tactics read: positions in world X and Z as (x, z), headings in degrees from north.</summary>
public readonly struct RaiderInput
{
    /// <summary>Creates the input.</summary>
    /// <param name="state">The state now.</param>
    /// <param name="self">The Nail Ship's position.</param>
    /// <param name="selfHeading">The Nail Ship's heading.</param>
    /// <param name="target">The target ship's position.</param>
    /// <param name="targetVelocity">The target ship's velocity.</param>
    /// <param name="targetHeading">The target ship's heading.</param>
    /// <param name="crewShare">Share of the crew still alive, 0 to 1.</param>
    /// <param name="hullShare">Share of the hull's health left, 0 to 1.</param>
    /// <param name="secondsInState">Seconds since the state last changed.</param>
    /// <param name="side">-1 to lie on the target's port side, 1 on its starboard side.</param>
    /// <param name="hasMelee">Whether any melee crew is alive.</param>
    /// <param name="grappled">Whether a grapnel rope binds the Nail Ship to another ship; it cannot run then.</param>
    public RaiderInput(RaiderState state, Vector2 self, float selfHeading, Vector2 target, Vector2 targetVelocity,
        float targetHeading, float crewShare, float hullShare, float secondsInState, int side, bool hasMelee, bool grappled = false)
    {
        State = state;
        Self = self;
        SelfHeading = selfHeading;
        Target = target;
        TargetVelocity = targetVelocity;
        TargetHeading = targetHeading;
        CrewShare = crewShare;
        HullShare = hullShare;
        SecondsInState = secondsInState;
        Side = side;
        HasMelee = hasMelee;
        Grappled = grappled;
    }

    /// <summary>The state now.</summary>
    public RaiderState State { get; }

    /// <summary>The Nail Ship's position.</summary>
    public Vector2 Self { get; }

    /// <summary>The Nail Ship's heading; Prize and Moored keep it.</summary>
    public float SelfHeading { get; }

    /// <summary>The target ship's position.</summary>
    public Vector2 Target { get; }

    /// <summary>The target ship's velocity.</summary>
    public Vector2 TargetVelocity { get; }

    /// <summary>The target ship's heading.</summary>
    public float TargetHeading { get; }

    /// <summary>Share of the crew still alive, 0 to 1.</summary>
    public float CrewShare { get; }

    /// <summary>Share of the hull's health left, 0 to 1.</summary>
    public float HullShare { get; }

    /// <summary>Seconds since the state last changed.</summary>
    public float SecondsInState { get; }

    /// <summary>-1 to lie on the target's port side, 1 on its starboard side.</summary>
    public int Side { get; }

    /// <summary>Whether any melee crew is alive.</summary>
    public bool HasMelee { get; }

    /// <summary>Whether a grapnel rope binds the Nail Ship to another ship; it cannot run then.</summary>
    public bool Grappled { get; }
}

/// <summary>What the tactics ask for.</summary>
public readonly struct RaiderOrder
{
    /// <summary>Creates the order.</summary>
    /// <param name="state">The state to be in.</param>
    /// <param name="course">The course to steer, in degrees from north.</param>
    /// <param name="sail">The sail to carry.</param>
    public RaiderOrder(RaiderState state, float course, RaiderSail sail)
    {
        State = state;
        Course = course;
        Sail = sail;
    }

    /// <summary>The state to be in.</summary>
    public RaiderState State { get; }

    /// <summary>The course to steer, in degrees from north.</summary>
    public float Course { get; }

    /// <summary>The sail to carry.</summary>
    public RaiderSail Sail { get; }
}

/// <summary>The settings the tactics use, as plain values so the tests need no config.</summary>
public readonly struct RaiderTuning
{
    /// <summary>Creates the tuning.</summary>
    /// <param name="broadsideDistance">Metres it keeps alongside while its archers shoot.</param>
    /// <param name="broadsideSeconds">Seconds alongside before it boards; 0 means never.</param>
    /// <param name="boardDistance">Metres within which its warriors leap across.</param>
    /// <param name="fleeCrewShare">It flees when this share of its crew is left.</param>
    /// <param name="fleeHullShare">It flees when this share of its hull is left.</param>
    public RaiderTuning(float broadsideDistance, float broadsideSeconds, float boardDistance, float fleeCrewShare, float fleeHullShare)
    {
        BroadsideDistance = broadsideDistance;
        BroadsideSeconds = broadsideSeconds;
        BoardDistance = boardDistance;
        FleeCrewShare = fleeCrewShare;
        FleeHullShare = fleeHullShare;
    }

    /// <summary>Metres it keeps alongside while its archers shoot.</summary>
    public float BroadsideDistance { get; }

    /// <summary>Seconds alongside before it boards; 0 means never.</summary>
    public float BroadsideSeconds { get; }

    /// <summary>Metres within which its warriors leap across.</summary>
    public float BoardDistance { get; }

    /// <summary>It flees when this share of its crew is left.</summary>
    public float FleeCrewShare { get; }

    /// <summary>It flees when this share of its hull is left.</summary>
    public float FleeHullShare { get; }

    /// <summary>
    /// The same tuning with another distance alongside, for an escort that keeps farther off than the flagship.
    /// </summary>
    /// <param name="broadsideDistance">Metres it keeps alongside.</param>
    /// <returns>The new tuning.</returns>
    public RaiderTuning WithBroadside(float broadsideDistance)
    {
        return new RaiderTuning(broadsideDistance, BroadsideSeconds, BoardDistance, FleeCrewShare, FleeHullShare);
    }
}

/// <summary>
/// Decides what a Nail Ship does next from plain values: its state, course and sail. It uses no game types, so it is
/// tested without the game; <c>RaiderShip</c> reads the world, calls <see cref="Decide"/> and steers.
/// </summary>
public static class RaiderTactics
{
    // How close to its station point the ship must be before it lines up with the target.
    private const float StationReach = 10f;

    // The lead point looks this many metres ahead per 8 m of distance, at most 15 seconds.
    private const float LeadSecondsPerMetre = 1f / 8f;
    private const float LeadSecondsMax = 15f;

    /// <summary>
    /// Decides the state, course and sail. Flee wins over everything but a grapnel rope; Prize and Moored only stop.
    /// </summary>
    /// <param name="input">What the ship knows.</param>
    /// <param name="tuning">The settings.</param>
    /// <returns>The order.</returns>
    public static RaiderOrder Decide(RaiderInput input, RaiderTuning tuning)
    {
        if (input.State == RaiderState.Prize || input.State == RaiderState.Moored)
        {
            return new RaiderOrder(input.State, Wrap(input.SelfHeading), RaiderSail.Stop);
        }

        float distance = Vector2.Distance(input.Self, input.Target);
        RaiderState state = NextState(input, tuning, distance);
        switch (state)
        {
            case RaiderState.Flee:
                return new RaiderOrder(state, Away(input), RaiderSail.Full);
            case RaiderState.Approach:
                return new RaiderOrder(state, CourseTo(input.Self, LeadPoint(input.Self, input.Target, input.TargetVelocity), input.SelfHeading), RaiderSail.Full);
            case RaiderState.Board:
                return Alongside(input, state, tuning.BoardDistance * 0.6f, RaiderSail.Half);
            default:
                RaiderSail sail = distance > 2f * tuning.BroadsideDistance ? RaiderSail.Full : RaiderSail.Half;
                return Alongside(input, state, tuning.BroadsideDistance, sail);
        }
    }

    /// <summary>
    /// The point ahead of a moving target to sail for: its position plus its velocity times a time that grows with the
    /// distance, at most 15 seconds.
    /// </summary>
    /// <param name="self">The Nail Ship's position.</param>
    /// <param name="target">The target's position.</param>
    /// <param name="targetVelocity">The target's velocity.</param>
    /// <returns>The point.</returns>
    public static Vector2 LeadPoint(Vector2 self, Vector2 target, Vector2 targetVelocity)
    {
        float seconds = Mathf.Min(Vector2.Distance(self, target) * LeadSecondsPerMetre, LeadSecondsMax);
        return target + targetVelocity * seconds;
    }

    /// <summary>
    /// The point <paramref name="distance"/> metres to one side of the target, square to its heading.
    /// </summary>
    /// <param name="target">The target's position.</param>
    /// <param name="targetHeading">The target's heading, degrees from north.</param>
    /// <param name="side">-1 for port, 1 for starboard.</param>
    /// <param name="distance">Metres from the target.</param>
    /// <returns>The point.</returns>
    public static Vector2 StationPoint(Vector2 target, float targetHeading, int side, float distance)
    {
        float radians = targetHeading * Mathf.Deg2Rad;
        // The starboard side of heading h is (cos h, -sin h) in (x, z).
        Vector2 starboard = new(Mathf.Cos(radians), -Mathf.Sin(radians));
        return target + starboard * (side < 0 ? -distance : distance);
    }

    /// <summary>
    /// The course from one point to another in degrees from north, 0 to 360.
    /// </summary>
    /// <param name="from">Where from.</param>
    /// <param name="to">Where to.</param>
    /// <param name="fallback">The course to keep when the points are the same.</param>
    /// <returns>The course.</returns>
    public static float CourseTo(Vector2 from, Vector2 to, float fallback)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude < 1e-6f)
        {
            return Wrap(fallback);
        }

        return Wrap(Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg);
    }

    // Works out the state: Flee wins; Approach turns into Broadside when near; Broadside into Board after its time. A ship
    // that a grapnel rope binds never flees, even one already fleeing, and goes straight to boarding when it has warriors.
    private static RaiderState NextState(RaiderInput input, RaiderTuning tuning, float distance)
    {
        if (input.Grappled)
        {
            return input.HasMelee ? RaiderState.Board : RaiderState.Broadside;
        }

        if (input.State == RaiderState.Flee || input.CrewShare <= tuning.FleeCrewShare || input.HullShare <= tuning.FleeHullShare)
        {
            return RaiderState.Flee;
        }

        if (input.State == RaiderState.Approach)
        {
            return distance <= 1.5f * tuning.BroadsideDistance ? RaiderState.Broadside : RaiderState.Approach;
        }

        if (input.State == RaiderState.Broadside && input.HasMelee && tuning.BroadsideSeconds > 0f
            && input.SecondsInState >= tuning.BroadsideSeconds)
        {
            return RaiderState.Board;
        }

        return input.State;
    }

    // Lies alongside the target at a distance: sails for the station point, and lines up with the target's heading
    // once there.
    private static RaiderOrder Alongside(RaiderInput input, RaiderState state, float stationDistance, RaiderSail sail)
    {
        Vector2 station = StationPoint(input.Target, input.TargetHeading, input.Side, stationDistance);
        float course = Vector2.Distance(input.Self, station) <= StationReach
            ? Wrap(input.TargetHeading)
            : CourseTo(input.Self, station, input.SelfHeading);
        return new RaiderOrder(state, course, sail);
    }

    // Straight away from the target; when on top of it, away along its heading.
    private static float Away(RaiderInput input)
    {
        Vector2 delta = input.Self - input.Target;
        return delta.sqrMagnitude < 1e-6f ? Wrap(input.TargetHeading) : CourseTo(input.Target, input.Self, input.TargetHeading);
    }

    private static float Wrap(float degrees)
    {
        return Mathf.Repeat(float.IsNaN(degrees) ? 0f : degrees, 360f);
    }
}
