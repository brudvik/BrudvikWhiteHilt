namespace BrudvikWhiteHilt.Household;

/// <summary>
/// The keys of what a member of the household keeps in their network data: who they are, what they wear and carry,
/// and where they are told to be. All of it is written by the owner.
/// </summary>
public static class HouseholdKeys
{
    /// <summary>The member's name.</summary>
    public static readonly int Name = "whitehilt_hh_name".GetStableHashCode();

    /// <summary>Whether the member is a woman.</summary>
    public static readonly int Female = "whitehilt_hh_female".GetStableHashCode();

    /// <summary>The member's role, a <see cref="HouseholdRole"/>.</summary>
    public static readonly int Role = "whitehilt_hh_role".GetStableHashCode();

    /// <summary>What the member wears and carries, as the game saves an inventory.</summary>
    public static readonly int Gear = "whitehilt_hh_gear".GetStableHashCode();

    /// <summary>Where the member lives: they go back there when sent home.</summary>
    public static readonly int Home = "whitehilt_hh_home".GetStableHashCode();

    /// <summary>The player the member follows, 0 for nobody.</summary>
    public static readonly int Follows = "whitehilt_hh_follows".GetStableHashCode();

    /// <summary>
    /// The farm the member belongs to: the <see cref="SeatId"/> of its High Seat, 0 for none. Network ids are given anew
    /// each time the world is loaded, so the link is a number of the mod's own.
    /// </summary>
    public static readonly int Farm = "whitehilt_hh_farm_id".GetStableHashCode();

    /// <summary>Where the member's own bed stands, unset for none; the bed is found again by its place.</summary>
    public static readonly int BedAt = "whitehilt_hh_bed_at".GetStableHashCode();

    /// <summary>A High Seat's own number, given once when it is built and kept with it.</summary>
    public static readonly int SeatId = "whitehilt_hs_id".GetStableHashCode();

    /// <summary>
    /// Custom data on an item a player handed to a member: the player can take it back at the High Seat. The gear a
    /// member came with has none, so it cannot be taken.
    /// </summary>
    public const string Given = "whitehilt_hh_given";
}

/// <summary>
/// What a member of the household is.
/// </summary>
public enum HouseholdRole
{
    /// <summary>Works on the farm: production, fields or animals.</summary>
    Farmhand,

    /// <summary>Guards the farm.</summary>
    Huskarl,
}
