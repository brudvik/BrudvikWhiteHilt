using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Fleets;

/// <summary>One line of the <c>[SeaBattles.Fleets] FleetTiers</c> text: the fleet size a global key gives.</summary>
public readonly struct FleetTier
{
    /// <summary>Creates a tier.</summary>
    /// <param name="key">The global key that selects the tier.</param>
    /// <param name="size">Ships in the fleet, 1 to 3.</param>
    /// <param name="chance">Chance in percent, 0 to 100, that an encounter becomes a fleet.</param>
    public FleetTier(string key, int size, float chance)
    {
        Key = key;
        Size = size;
        Chance = chance;
    }

    /// <summary>The global key that selects the tier.</summary>
    public string Key { get; }

    /// <summary>Ships in the fleet, 1 to 3.</summary>
    public int Size { get; }

    /// <summary>Chance in percent, 0 to 100, that an encounter becomes a fleet.</summary>
    public float Chance { get; }
}

/// <summary>
/// The decisions of a Nail Ship fleet from plain values: how many ships come, where the escorts hold their places in
/// the wedge, when they break formation and when they run. Positions are world X and Z as (x, z) and headings are
/// degrees from north, as in <see cref="RaiderTactics"/>. No game types, so it is tested without the game; the
/// fleet's ships read the world and call it.
/// </summary>
public static class FleetRules
{
    /// <summary>Most ships in a fleet: the flagship and two escorts.</summary>
    public const int MaxSize = 3;

    /// <summary>Seconds the flagship's data may be missing before it counts as gone.</summary>
    public const float MissingGraceSeconds = 5f;

    /// <summary>The jarl's level when <c>JarlLevels</c> has no valid entry at all.</summary>
    public const int JarlFallbackLevel = 3;

    // How far back and to the side an escort lies, as a share of the spacing.
    private const float WedgeShare = 0.7f;

    // Within this many metres of its slot an escort only matches the flagship's heading.
    private const float SlotReach = 10f;

    /// <summary>
    /// Reads the text <c>key=size:chance%; key=...</c>. Spaces are ignored and the chance may be left out (always).
    /// A part without a key, with a size or chance that is not a number is skipped; a size is held to 1 to 3 and a
    /// chance to 0 to 100. Never throws.
    /// </summary>
    /// <param name="text">The config text.</param>
    /// <returns>The tiers in the order of the text.</returns>
    public static IReadOnlyList<FleetTier> ParseTiers(string text)
    {
        List<FleetTier> tiers = new();
        foreach (string part in (text ?? string.Empty).Split(';'))
        {
            int equals = part.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            string key = part.Substring(0, equals).Trim();
            string[] fields = part.Substring(equals + 1).Split(':');
            if (key.Length == 0 || fields.Length > 2
                || !int.TryParse(fields[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int size))
            {
                continue;
            }

            float chance = 100f;
            if (fields.Length == 2
                && !float.TryParse(fields[1].Trim().TrimEnd('%').Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out chance))
            {
                continue;
            }

            if (float.IsNaN(chance))
            {
                continue;
            }

            tiers.Add(new FleetTier(key, Mathf.Clamp(size, 1, MaxSize), Mathf.Clamp(chance, 0f, 100f)));
        }

        return tiers;
    }

    /// <summary>
    /// Picks the fleet size for one encounter: the tier of the last key that is set, if the roll is under its chance;
    /// otherwise a lone ship. When the last tier fails its roll an earlier tier is not tried.
    /// </summary>
    /// <param name="tiers">The tiers, from <see cref="ParseTiers"/>.</param>
    /// <param name="hasKey">Tells whether a global key is set.</param>
    /// <param name="roll">A random number from 0 up to but not including 1.</param>
    /// <returns>The number of ships, 1 to 3.</returns>
    public static int PickSize(IReadOnlyList<FleetTier> tiers, Func<string, bool> hasKey, float roll)
    {
        if (tiers == null || hasKey == null)
        {
            return 1;
        }

        FleetTier? picked = null;
        foreach (FleetTier tier in tiers)
        {
            if (hasKey(tier.Key))
            {
                picked = tier;
            }
        }

        return picked.HasValue && roll * 100f < picked.Value.Chance ? Mathf.Clamp(picked.Value.Size, 1, MaxSize) : 1;
    }

    /// <summary>
    /// Picks the jarl's level from the text <c>key=level; key=...</c>: the level of the last key that is set, or the
    /// first entry's level when none is (a fleet an admin sends early). Parts that do not read are skipped, and a level
    /// is held to 1 to <paramref name="maxLevel"/>. Never throws.
    /// </summary>
    /// <param name="text">The config text.</param>
    /// <param name="hasKey">Tells whether a global key is set.</param>
    /// <param name="maxLevel">The highest level a creature can have.</param>
    /// <param name="fallback">The level when the text has no valid entry.</param>
    /// <returns>The level, stars plus one.</returns>
    public static int PickLevel(string text, Func<string, bool> hasKey, int maxLevel, int fallback)
    {
        int? first = null;
        int? picked = null;
        foreach (string part in (text ?? string.Empty).Split(';'))
        {
            int equals = part.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            string key = part.Substring(0, equals).Trim();
            if (key.Length == 0
                || !int.TryParse(part.Substring(equals + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
            {
                continue;
            }

            level = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
            first ??= level;
            if (hasKey != null && hasKey(key))
            {
                picked = level;
            }
        }

        return picked ?? first ?? Mathf.Clamp(fallback, 1, Mathf.Max(1, maxLevel));
    }

    /// <summary>
    /// The place of a ship in the wedge behind the flagship: rank 1 back and to port, rank 2 back and to starboard,
    /// each 0.7 times the spacing back and to the side. Rank 0 is the flagship itself.
    /// </summary>
    /// <param name="flagship">The flagship's position.</param>
    /// <param name="heading">The flagship's heading, degrees from north.</param>
    /// <param name="rank">0 for the flagship, 1 or 2 for an escort; higher ranks are taken as 2.</param>
    /// <param name="spacing">Metres between the ships.</param>
    /// <returns>The slot.</returns>
    public static Vector2 SlotPoint(Vector2 flagship, float heading, int rank, float spacing)
    {
        if (rank <= 0)
        {
            return flagship;
        }

        Vector2 forward = Forward(heading);
        Vector2 starboard = Starboard(heading);
        float offset = WedgeShare * spacing;
        return flagship - forward * offset + starboard * (rank == 1 ? -offset : offset);
    }

    /// <summary>
    /// What an escort in the wedge asks for to reach its slot: full sail when behind it and far, half sail when near,
    /// no sail when ahead of it, and the flagship's heading once within 10 m. The state is Approach.
    /// </summary>
    /// <param name="self">The escort's position.</param>
    /// <param name="slot">Its slot, from <see cref="SlotPoint"/>.</param>
    /// <param name="heading">The flagship's heading, degrees from north.</param>
    /// <param name="spacing">Metres between the ships; "far" is more than this from the slot.</param>
    /// <returns>The order.</returns>
    public static RaiderOrder EscortSail(Vector2 self, Vector2 slot, float heading, float spacing)
    {
        float course = Mathf.Repeat(float.IsNaN(heading) ? 0f : heading, 360f);
        Vector2 toSlot = slot - self;
        if (toSlot.magnitude <= SlotReach)
        {
            return new RaiderOrder(RaiderState.Approach, course, RaiderSail.Half);
        }

        // Ahead of the slot along the flagship's heading: let the wedge catch up, do not sail on.
        if (Vector2.Dot(toSlot, Forward(heading)) < 0f)
        {
            return new RaiderOrder(RaiderState.Approach, course, RaiderSail.Stop);
        }

        float toward = RaiderTactics.CourseTo(self, slot, heading);
        return new RaiderOrder(RaiderState.Approach, toward, toSlot.magnitude > spacing ? RaiderSail.Full : RaiderSail.Half);
    }

    /// <summary>
    /// Whether the flagship holds back so the escorts can keep up: any escort more than one spacing out of its slot.
    /// The flagship then sails half instead of full while it approaches.
    /// </summary>
    /// <param name="escortSlotErrors">Each loaded escort's distance from its slot, in metres.</param>
    /// <param name="spacing">Metres between the ships.</param>
    /// <returns>True when the flagship should wait.</returns>
    public static bool FlagshipWaits(IReadOnlyList<float> escortSlotErrors, float spacing)
    {
        if (escortSlotErrors == null)
        {
            return false;
        }

        for (int i = 0; i < escortSlotErrors.Count; i++)
        {
            if (escortSlotErrors[i] > spacing)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the escorts leave the wedge and fight on their own: the flagship has stopped approaching (any state but
    /// Approach) or is within the break distance of its target.
    /// </summary>
    /// <param name="flagshipState">The flagship's state.</param>
    /// <param name="flagshipDistance">Metres from the flagship to its target.</param>
    /// <param name="breakDistance">The break distance, in metres.</param>
    /// <returns>True when the formation is broken.</returns>
    public static bool BreaksFormation(RaiderState flagshipState, float flagshipDistance, float breakDistance)
    {
        return flagshipState != RaiderState.Approach || flagshipDistance <= breakDistance;
    }

    /// <summary>
    /// The side of the target an escort lies on once the formation is broken: rank 1 takes the side the flagship does
    /// not, the others the flagship's.
    /// </summary>
    /// <param name="rank">0 for the flagship, 1 or 2 for an escort.</param>
    /// <param name="flagshipSide">The flagship's side: -1 port, 1 starboard (0 counts as starboard).</param>
    /// <returns>-1 for port, 1 for starboard.</returns>
    public static int EscortSide(int rank, int flagshipSide)
    {
        int side = flagshipSide < 0 ? -1 : 1;
        return rank == 1 ? -side : side;
    }

    /// <summary>
    /// The distance an escort keeps from the target once the formation is broken: the broadside distance, or
    /// <paramref name="spread"/> times it for an escort on the same side as the flagship, so two ships on one side do
    /// not lie on top of each other.
    /// </summary>
    /// <param name="rank">0 for the flagship, 1 or 2 for an escort.</param>
    /// <param name="flagshipSide">The flagship's side: -1 port, 1 starboard.</param>
    /// <param name="broadside">The broadside distance, in metres.</param>
    /// <param name="spread">The factor for the same side, at least 1.</param>
    /// <returns>Metres from the target.</returns>
    public static float EscortDistance(int rank, int flagshipSide, float broadside, float spread)
    {
        bool sameSideAsFlagship = rank > 0 && EscortSide(rank, flagshipSide) == (flagshipSide < 0 ? -1 : 1);
        return sameSideAsFlagship ? broadside * Mathf.Max(1f, spread) : broadside;
    }

    /// <summary>
    /// Whether the escorts turn and run: the flagship is gone (sunk) or a prize and that setting is on, or the jarl has
    /// fallen and that setting is on. An escort that a grapnel rope binds never runs; that rule is in the tactics.
    /// </summary>
    /// <param name="flagshipGone">The flagship no longer exists.</param>
    /// <param name="flagshipState">The flagship's state; Prize counts as fallen.</param>
    /// <param name="jarlFallen">The flagship's jarl has been slain.</param>
    /// <param name="fleeOnFlagship">The setting <c>FleeWhenFlagshipFalls</c>.</param>
    /// <param name="fleeOnJarl">The setting <c>FleeWhenJarlFalls</c>.</param>
    /// <returns>True when the morale breaks.</returns>
    public static bool MoraleBreaks(bool flagshipGone, RaiderState flagshipState, bool jarlFallen, bool fleeOnFlagship, bool fleeOnJarl)
    {
        bool flagshipFallen = flagshipGone || flagshipState == RaiderState.Prize;
        return (fleeOnFlagship && flagshipFallen) || (fleeOnJarl && jarlFallen);
    }

    /// <summary>
    /// Whether the flagship has been out of sight of the fleet's data for so long that it counts as gone. A ship whose
    /// data a machine does not have yet is not a ship that has sunk, so a short gap is not enough.
    /// </summary>
    /// <param name="missingSeconds">Seconds the flagship's data has been missing, 0 while it is there.</param>
    /// <returns>True from <see cref="MissingGraceSeconds"/> on.</returns>
    public static bool FlagshipGone(float missingSeconds)
    {
        return missingSeconds >= MissingGraceSeconds;
    }

    /// <summary>
    /// How far an escort is from its place in the wedge, in metres.
    /// </summary>
    /// <param name="self">The escort's position.</param>
    /// <param name="flagship">The flagship's position.</param>
    /// <param name="heading">The flagship's heading, degrees from north.</param>
    /// <param name="rank">The escort's rank, 1 or 2.</param>
    /// <param name="spacing">Metres between the ships.</param>
    /// <returns>The distance to its slot.</returns>
    public static float SlotError(Vector2 self, Vector2 flagship, float heading, int rank, float spacing)
    {
        return Vector2.Distance(self, SlotPoint(flagship, heading, rank, spacing));
    }

    /// <summary>
    /// The flagship's order while its escorts are behind: it sails half instead of full while it approaches, so the
    /// wedge can catch up. Any other state or sail is left as it is.
    /// </summary>
    /// <param name="order">What the tactics asked for.</param>
    /// <param name="waits">The result of <see cref="FlagshipWaits"/>.</param>
    /// <returns>The order to follow.</returns>
    public static RaiderOrder FlagshipOrder(RaiderOrder order, bool waits)
    {
        return waits && order.State == RaiderState.Approach && order.Sail == RaiderSail.Full
            ? new RaiderOrder(order.State, order.Course, RaiderSail.Half)
            : order;
    }

    /// <summary>
    /// The level a crew member of the flagship gets: its own level, or the least level when that is higher.
    /// </summary>
    /// <param name="current">The level it has (stars plus one).</param>
    /// <param name="least">The least level, from the settings.</param>
    /// <returns>The level, at most the game's highest.</returns>
    public static int CrewLevel(int current, int least)
    {
        return Mathf.Clamp(Mathf.Max(current, least), 1, Difficulty.CreatureStars.MaxLevel);
    }

    /// <summary>
    /// Which of the free posts is nearest the mast, where the jarl stands.
    /// </summary>
    /// <param name="posts">Ship-local posts.</param>
    /// <param name="candidates">The indexes of the posts that may be taken.</param>
    /// <param name="mast">The mast, ship-local; only across and along the deck counts.</param>
    /// <returns>The index into <paramref name="posts"/>; the first of equally near ones; -1 when there is no candidate.</returns>
    public static int NearestPost(IReadOnlyList<Vector3> posts, IReadOnlyList<int> candidates, Vector3 mast)
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        if (posts == null || candidates == null)
        {
            return best;
        }

        foreach (int index in candidates)
        {
            if (index < 0 || index >= posts.Count)
            {
                continue;
            }

            float x = posts[index].x - mast.x;
            float z = posts[index].z - mast.z;
            float distance = x * x + z * z;
            if (distance < bestDistance)
            {
                best = index;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>
    /// Whether the jarl has been slain: the flagship carries one, its owner has seen him alive, and no living jarl is
    /// loaded now. An owner that has never seen him cannot tell a dead jarl from one it has not met yet.
    /// </summary>
    /// <param name="carriesJarl">The flagship's data says it has a jarl.</param>
    /// <param name="seenAlive">This machine has seen the jarl alive.</param>
    /// <param name="aliveNow">A living jarl is loaded here now.</param>
    /// <returns>True when the jarl has fallen.</returns>
    public static bool JarlHasFallen(bool carriesJarl, bool seenAlive, bool aliveNow)
    {
        return carriesJarl && seenAlive && !aliveNow;
    }

    /// <summary>
    /// The crew bonus of a ship: the extra warriors for the players aboard the target, and on the flagship the extra
    /// crew of the settings.
    /// </summary>
    /// <param name="bonus">The bonus for the players aboard the target.</param>
    /// <param name="flagship">Whether it is the flagship.</param>
    /// <param name="extra">The flagship's extra crew.</param>
    /// <returns>The number of extra warriors.</returns>
    public static int CrewBonus(int bonus, bool flagship, int extra)
    {
        return Mathf.Max(0, bonus) + (flagship ? Mathf.Max(0, extra) : 0);
    }

    /// <summary>
    /// The number of draws from the loot list for a ship's hold.
    /// </summary>
    /// <param name="normal">The draws of every Nail Ship.</param>
    /// <param name="flagship">Whether it is the flagship.</param>
    /// <param name="extra">The flagship's extra draws.</param>
    /// <returns>The draws.</returns>
    public static int HoldRolls(int normal, bool flagship, int extra)
    {
        return Mathf.Max(0, normal) + (flagship ? Mathf.Max(0, extra) : 0);
    }

    /// <summary>
    /// Whether the hold carries a Raid Chart: always on the flagship when the setting says so, else by chance.
    /// </summary>
    /// <param name="flagship">Whether it is the flagship.</param>
    /// <param name="flagshipAlways">The setting <c>FlagshipRaidChart</c>.</param>
    /// <param name="chance">The chance of any Nail Ship, 0 to 1.</param>
    /// <param name="roll">A random number from 0 up to but not including 1.</param>
    /// <returns>True when the hold gets a chart.</returns>
    public static bool ChartInHold(bool flagship, bool flagshipAlways, float chance, float roll)
    {
        return (flagship && flagshipAlways) || roll < chance;
    }

    // The unit vector of a heading in (x, z).
    private static Vector2 Forward(float heading)
    {
        float radians = (float.IsNaN(heading) ? 0f : heading) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    // The starboard side of heading h is (cos h, -sin h), as in RaiderTactics.StationPoint.
    private static Vector2 Starboard(float heading)
    {
        float radians = (float.IsNaN(heading) ? 0f : heading) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians));
    }
}
