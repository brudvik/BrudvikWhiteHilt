using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// On each creature of a Nail Ship's crew, and on the guards of a draugr harbour. A crew member keeps to its post on
/// the deck and fights from there; a warrior leaps aboard the target ship when it is his turn. A shore guard turns
/// back when it has strayed from its post. Everything is done by the creature's owner, from the
/// <c>MonsterAI.UpdateAI</c> postfix, and the fields are read from the ZDO once.
/// </summary>
public class RaiderCrew : MonoBehaviour
{
    // A crew member farther than this from its post is put back on it.
    private const float PostTolerance = 1.5f;

    // Room beyond the deck's edge before a warrior who walked there is put back.
    private const float DeckMargin = 0.5f;

    // A crew member whose ship is gone from the world is removed after this long.
    private const float OrphanSeconds = 3f;

    private const float ShipLookupInterval = 0.5f;

    private static readonly Dictionary<MonsterAI, RaiderCrew> byAi = new();

    private Character character;
    private MonsterAI ai;
    private ZNetView nview;
    private Rigidbody body;
    private ZDOID shipId = ZDOID.None;
    private Vector3 post;
    private bool archer;
    private bool jarl;
    private int index = -1;
    private string siteId = string.Empty;
    private RaiderShip ship;
    private float nextShipLookup;
    private float orphanSince;

    /// <summary>How many crew members and guards are loaded; the AI patch returns at once when it is zero.</summary>
    public static int Count => byAi.Count;

    /// <summary>The loaded crew members and guards.</summary>
    public static IEnumerable<RaiderCrew> All => byAi.Values;

    /// <summary>The Nail Ship this creature belongs to; none for a shore guard.</summary>
    public ZDOID ShipId => shipId;

    /// <summary>The raid site this creature belongs to; empty at sea.</summary>
    public string SiteId => siteId;

    /// <summary>Whether this creature shoots from its post.</summary>
    public bool Archer => archer;

    /// <summary>Whether this creature is the draugr jarl of a flagship.</summary>
    public bool Jarl => jarl;

    /// <summary>A melee crew member's number among the ship's melee crew, from 0; -1 for others.</summary>
    public int Index => index;

    /// <summary>The creature.</summary>
    public Character Character => character;

    /// <summary>Whether the creature is alive.</summary>
    public bool Alive => character != null && !character.IsDead();

    /// <summary>Whether the creature has leapt onto another ship.</summary>
    public bool Boarded => nview != null && nview.IsValid() && nview.GetZDO().GetBool(RaiderKeys.Boarded);

    /// <summary>
    /// Whether the ship this creature fights against is a merchant: the ship it stands on once it has leapt, else the
    /// ship its Nail Ship hunts. False for a shore guard, or when neither is loaded here.
    /// </summary>
    public bool FightsMerchant
    {
        get
        {
            RaiderShip raider = Boarded ? null : FindShip();
            Ship against = Boarded ? character.GetStandingOnShip() : raider != null ? raider.TargetShip : null;
            return Escorts.TradeShip.Is(against);
        }
    }

    /// <summary>
    /// Makes a creature part of a crew, if its ZDO says so. Safe to call more than once; call it again after the ZDO
    /// has been filled in for a creature made by this machine, as the creature woke before that.
    /// </summary>
    /// <param name="creature">The creature.</param>
    /// <returns>The crew component, or null when the creature belongs to no crew.</returns>
    public static RaiderCrew Attach(Character creature)
    {
        ZDO zdo = creature != null && !creature.IsPlayer() && creature.m_nview != null ? creature.m_nview.GetZDO() : null;
        if (zdo == null || (zdo.GetZDOID(RaiderKeys.Ship).IsNone() && string.IsNullOrEmpty(zdo.GetString(RaiderKeys.Site))))
        {
            return null;
        }

        RaiderCrew crew = creature.GetComponent<RaiderCrew>();
        if (crew == null)
        {
            // Its Awake reads the ZDO.
            crew = creature.gameObject.AddComponent<RaiderCrew>();
            RaiderVoice.Attach(crew);
            return crew;
        }

        crew.Read();
        return crew;
    }

    /// <summary>
    /// The crew component of the creature a monster AI belongs to.
    /// </summary>
    /// <param name="monster">The AI.</param>
    /// <returns>The component, or null when the creature is no crew member.</returns>
    public static RaiderCrew Of(MonsterAI monster)
    {
        return monster != null && byAi.TryGetValue(monster, out RaiderCrew crew) ? crew : null;
    }

    /// <summary>
    /// How many living crew members that have leapt aboard stand on a ship. Only the crew loaded on this machine are
    /// counted, which is all of them where the ship's owner is, since the crew leaps from a ship in the same place.
    /// </summary>
    /// <param name="ship">The ship they stand on.</param>
    /// <returns>The number of raiders on its deck.</returns>
    public static int AliveAboard(Ship ship)
    {
        int count = 0;
        foreach (RaiderCrew crew in byAi.Values)
        {
            if (crew.Alive && crew.Boarded && crew.character.GetStandingOnShip() == ship)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Lets the creature hold its post, leap or turn back, after the game's AI has decided what to do. Does nothing on
    /// machines that do not own the creature.
    /// </summary>
    public void Hold()
    {
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || character.IsDead())
        {
            return;
        }

        if (!shipId.IsNone())
        {
            if (!Boarded)
            {
                HoldOnDeck();
            }

            return;
        }

        if (siteId.Length > 0)
        {
            HoldShore();
        }
    }

    private void Awake()
    {
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        body = GetComponent<Rigidbody>();
        nview = character != null ? character.m_nview : null;
        if (ai == null || nview == null || nview.GetZDO() == null)
        {
            enabled = false;
            return;
        }

        Read();
        byAi[ai] = this;
    }

    private void OnDestroy()
    {
        if (ai != null)
        {
            byAi.Remove(ai);
        }
    }

    private void Read()
    {
        ZDO zdo = nview.GetZDO();
        shipId = zdo.GetZDOID(RaiderKeys.Ship);
        post = zdo.GetVec3(RaiderKeys.Post, Vector3.zero);
        archer = zdo.GetInt(RaiderKeys.Role) == 1;
        jarl = zdo.GetBool(RaiderKeys.Jarl);
        index = zdo.GetInt(RaiderKeys.Index, -1);
        siteId = zdo.GetString(RaiderKeys.Site, string.Empty);
    }

    // A crew member on the deck keeps its post while nobody has come aboard, so melee crew wait for their turn to leap
    // and archers shoot from where they stand. With players aboard, the warriors may fight, but one who walks off the
    // deck is put back. A ship that has gone from the world takes its crew with it.
    private void HoldOnDeck()
    {
        RaiderShip raider = FindShip();
        if (raider == null)
        {
            RemoveIfShipIsGone();
            return;
        }

        orphanSince = 0f;
        Vector3 world = raider.transform.TransformPoint(post);
        if (!archer && TryLeap(raider))
        {
            return;
        }

        if (archer || raider.Ship.m_players.Count == 0)
        {
            ai.StopMoving();
            if (Utils.DistanceXZ(transform.position, world) > PostTolerance)
            {
                PutAt(raider, world);
            }

            return;
        }

        BoxCollider deck = raider.Ship.m_floatCollider;
        if (deck != null && !RaiderDeck.InsideDeck(deck.transform.InverseTransformPoint(transform.position), deck.center, deck.size, DeckMargin))
        {
            ai.StopMoving();
            PutAt(raider, world);
        }
    }

    // The warrior whose turn it is leaps onto the target ship's deck, if the deck is within reach. The ship's owner
    // hands out the turns, so two owners never send the same warrior; once he has leapt he is an ordinary creature.
    private bool TryLeap(RaiderShip raider)
    {
        if (raider.State != RaiderState.Board || index < 0 || (raider.LeapTurn != index && raider.LeapTurn2 != index))
        {
            return false;
        }

        Ship target = raider.TargetShip;
        if (target == null || !RaiderDeck.TryDeckPoint(target, transform.position, out Vector3 deck)
            || Vector3.Distance(transform.position, deck) > SeaBattleSettings.BoardDistance.Value)
        {
            return false;
        }

        Vector3 landing = deck + Vector3.up * 0.2f;
        raider.Ship.m_waterImpactEffect.Create(landing, Quaternion.identity);
        Rigidbody targetBody = target.GetComponent<Rigidbody>();
        body.position = landing;
        body.linearVelocity = targetBody != null ? targetBody.GetPointVelocity(landing) : Vector3.zero;
        nview.GetZDO().Set(RaiderKeys.Boarded, true);
        return true;
    }

    // Moves the creature's body to a point on the deck, carried along at the ship's speed.
    private void PutAt(RaiderShip raider, Vector3 world)
    {
        body.position = world + Vector3.up * 0.1f;
        body.linearVelocity = raider.Body != null ? raider.Body.GetPointVelocity(world) : Vector3.zero;
    }

    // A shore guard that has strayed from its post turns back, unless a player stands within the hold radius of the
    // post, where the guards fight as the game's AI has it.
    private void HoldShore()
    {
        float radius = SeaBattleSettings.Raids.GuardHoldRadius.Value;
        if (Utils.DistanceXZ(transform.position, post) <= radius)
        {
            return;
        }

        foreach (Player player in Player.GetAllPlayers())
        {
            if (!player.IsDead() && Utils.DistanceXZ(player.transform.position, post) <= radius)
            {
                return;
            }
        }

        ai.MoveTo(Time.deltaTime, post, 1f, false);
    }

    // The Nail Ship this creature serves, looked up twice a second while it is not at hand.
    private RaiderShip FindShip()
    {
        if (ship != null)
        {
            return ship;
        }

        if (Time.time < nextShipLookup || ZNetScene.instance == null)
        {
            return null;
        }

        nextShipLookup = Time.time + ShipLookupInterval;
        GameObject found = ZNetScene.instance.FindInstance(shipId);
        ship = found != null ? found.GetComponent<RaiderShip>() : null;
        return ship;
    }

    // The ship's ZDO no longer exists: it sank or was removed. Its crew goes with it, without loot. A ship whose ZDO
    // exists but is not loaded here is only waited for.
    private void RemoveIfShipIsGone()
    {
        if (ZDOMan.instance == null || ZDOMan.instance.GetZDO(shipId) != null)
        {
            orphanSince = 0f;
            return;
        }

        if (orphanSince == 0f)
        {
            orphanSince = Time.time;
        }
        else if (Time.time - orphanSince >= OrphanSeconds)
        {
            ZNetScene.instance.Destroy(gameObject);
        }
    }
}
