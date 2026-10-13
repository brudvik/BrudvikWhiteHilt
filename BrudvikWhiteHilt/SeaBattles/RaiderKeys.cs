namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// The ZDO keys of the Nail Ships and their crews, hashed once. A key with a name instead of a hash holds a
/// <see cref="ZDOID"/>, which the game stores under two hashes of the name.
/// </summary>
internal static class RaiderKeys
{
    /// <summary>The ship a Nail Ship hunts (a ZDOID, stored under this name).</summary>
    public const string Target = "whitehilt_raider_target";

    /// <summary>The Nail Ship a crew member belongs to (a ZDOID, stored under this name).</summary>
    public const string Ship = "whitehilt_raider_ship";

    /// <summary>The Nail Ship's <see cref="RaiderState"/>.</summary>
    public static readonly int State = "whitehilt_raider_state".GetStableHashCode();

    /// <summary>-1 to lie on the target's port side, 1 on its starboard side.</summary>
    public static readonly int Side = "whitehilt_raider_side".GetStableHashCode();

    /// <summary>Index of the crew tier in the config text.</summary>
    public static readonly int Tier = "whitehilt_raider_tier".GetStableHashCode();

    /// <summary>Extra melee crew for the players aboard the target; written by whoever sends the ship.</summary>
    public static readonly int Bonus = "whitehilt_raider_bonus".GetStableHashCode();

    /// <summary>Crew spawned; 0 until the owner has spawned it.</summary>
    public static readonly int CrewSize = "whitehilt_raider_crewsize".GetStableHashCode();

    /// <summary>Crew alive at the owner's last count, so a new owner knows before it has seen the crew.</summary>
    public static readonly int Alive = "whitehilt_raider_alive".GetStableHashCode();

    /// <summary>World ticks when the ship became a prize; 0 when it is not.</summary>
    public static readonly int Prize = "whitehilt_raider_prize".GetStableHashCode();

    /// <summary>World ticks when a player was last within the despawn range.</summary>
    public static readonly int Seen = "whitehilt_raider_seen".GetStableHashCode();

    /// <summary>The saga deed has been announced.</summary>
    public static readonly int Saga = "whitehilt_raider_saga".GetStableHashCode();

    /// <summary>Index of the melee crew member whose turn it is to leap; -1 for nobody.</summary>
    public static readonly int Leap = "whitehilt_raider_leap".GetStableHashCode();

    /// <summary>The raid site a ship, crew member or hoard belongs to; empty at sea.</summary>
    public static readonly int Site = "whitehilt_raider_site".GetStableHashCode();

    /// <summary>A crew member's post: ship-local for crew, world for shore guards.</summary>
    public static readonly int Post = "whitehilt_raider_post".GetStableHashCode();

    /// <summary>A crew member's role: 0 melee, 1 archer.</summary>
    public static readonly int Role = "whitehilt_raider_role".GetStableHashCode();

    /// <summary>A melee crew member's number among the ship's melee crew, from 0.</summary>
    public static readonly int Index = "whitehilt_raider_index".GetStableHashCode();

    /// <summary>The crew member has leapt onto another ship.</summary>
    public static readonly int Boarded = "whitehilt_raider_boarded".GetStableHashCode();
}
