using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses.GateControl;

/// <summary>
/// What a gate control can work.
/// </summary>
public enum GateMechanismKind
{
    /// <summary>The leaves of a gatehouse; a drawbridge nearby follows them.</summary>
    Gate,

    /// <summary>A drawbridge on its own.</summary>
    Drawbridge,

    /// <summary>The portcullis of a stone gatehouse.</summary>
    Portcullis
}

/// <summary>
/// One gate, drawbridge or portcullis in the world. Open means the gate leaves are open, the drawbridge is lowered or
/// the portcullis is raised: the way is clear.
/// </summary>
public sealed class GateMechanism
{
    private const string UseDoorRpc = "UseDoor";

    private readonly Door door;
    private readonly PortcullisDriver portcullis;

    private GateMechanism(GateMechanismKind kind, Door door, PortcullisDriver portcullis)
    {
        Kind = kind;
        this.door = door;
        this.portcullis = portcullis;
    }

    /// <summary>What it is.</summary>
    public GateMechanismKind Kind { get; }

    /// <summary>The piece it belongs to.</summary>
    public GameObject Piece => door != null ? door.gameObject : portcullis.gameObject;

    /// <summary>Where it is.</summary>
    public Vector3 Position => Piece.transform.position;

    /// <summary>The network view of its piece.</summary>
    public ZNetView View => Piece.GetComponent<ZNetView>();

    /// <summary>True while the piece is in the loaded world.</summary>
    public bool IsValid => Piece != null && View != null && View.IsValid();

    /// <summary>True if the way is clear.</summary>
    public bool IsOpen => Kind == GateMechanismKind.Portcullis ? !portcullis.IsLowered : View.GetZDO().GetInt(ZDOVars.s_state) != 0;

    /// <summary>
    /// Opens or closes it; nothing happens if it already is. The owner of the piece carries it out.
    /// </summary>
    /// <param name="open">True to clear the way.</param>
    public void Set(bool open)
    {
        if (!IsValid || IsOpen == open)
        {
            return;
        }

        if (Kind == GateMechanismKind.Portcullis)
        {
            portcullis.RequestLowered(!open);
        }
        else
        {
            View.InvokeRPC(UseDoorRpc, true);
        }
    }

    /// <summary>
    /// Opens it if closed and closes it if open.
    /// </summary>
    public void Toggle()
    {
        Set(!IsOpen);
    }

    /// <summary>
    /// The gate mechanisms of one piece, if it has any.
    /// </summary>
    /// <param name="piece">A piece, e.g. a gatehouse.</param>
    /// <returns>Its mechanisms.</returns>
    public static IEnumerable<GateMechanism> Of(GameObject piece)
    {
        Door door = piece.GetComponent<Door>();
        if (door != null && piece.GetComponent<GateLeafDriver>() != null)
        {
            yield return new GateMechanism(GateMechanismKind.Gate, door, null);
        }

        if (door != null && piece.GetComponent<DrawbridgeDriver>() != null)
        {
            yield return new GateMechanism(GateMechanismKind.Drawbridge, door, null);
        }

        PortcullisDriver portcullis = piece.GetComponent<PortcullisDriver>();
        if (portcullis != null)
        {
            yield return new GateMechanism(GateMechanismKind.Portcullis, null, portcullis);
        }
    }
}

/// <summary>
/// Finds the gates, drawbridges and portcullises near a place, and holds the settings and texts the gate controls
/// share: the Gate Rope, the Windlass House and the Gate Horn.
/// </summary>
public static class GateMechanisms
{
    private const string Section = "Defences.GateControl";

    private static int pieceMask;

    /// <summary>How far from a Gate Rope a gate, drawbridge or portcullis may be.</summary>
    public static ConfigEntry<float> RopeRange { get; private set; }

    /// <summary>How far from a Windlass House a gate, drawbridge or portcullis may be.</summary>
    public static ConfigEntry<float> WindlassRange { get; private set; }

    /// <summary>How far from the player a Gate Horn reaches.</summary>
    public static ConfigEntry<float> HornRange { get; private set; }

    /// <summary>Seconds the Gate Horn is blown before the gate answers.</summary>
    public static ConfigEntry<float> HornSeconds { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        RopeRange = WhiteHiltConfig.BindAdminOnly(Section, "RopeRange", 15f, "How far from a Gate Rope the gate, drawbridge or portcullis it works may be, in metres.",
            new AcceptableValueRange<float>(5f, 40f));
        WindlassRange = WhiteHiltConfig.BindAdminOnly(Section, "WindlassRange", 25f, "How far from a Windlass House the gate, drawbridge and portcullis it works may be, in metres.",
            new AcceptableValueRange<float>(5f, 60f));
        HornRange = WhiteHiltConfig.BindAdminOnly(Section, "HornRange", 40f, "How far a Gate Horn is heard by a gate, in metres.",
            new AcceptableValueRange<float>(10f, 100f));
        HornSeconds = WhiteHiltConfig.BindAdminOnly(Section, "HornSeconds", 2f, "Seconds the Gate Horn is blown before the gate answers.",
            new AcceptableValueRange<float>(1f, 6f));

        Translations.AddEnglish("whitehilt_gate_kind_gate", "the gate");
        Translations.AddEnglish("whitehilt_gate_kind_drawbridge", "the drawbridge");
        Translations.AddEnglish("whitehilt_gate_kind_portcullis", "the portcullis");
        Translations.AddEnglish("whitehilt_gate_open", "open");
        Translations.AddEnglish("whitehilt_gate_closed", "closed");
        Translations.AddEnglish("whitehilt_gate_raised", "raised");
        Translations.AddEnglish("whitehilt_gate_lowered", "lowered");
        Translations.AddEnglish("whitehilt_gate_works", "Works {0} ({1})");
        Translations.AddEnglish("whitehilt_gate_none", "No {0} within {1} m");
        Translations.AddEnglish("whitehilt_gate_nothing", "No gate, drawbridge or portcullis within {0} m");
        Translations.AddEnglish("whitehilt_gate_rope_pull", "Pull the rope");
        Translations.AddEnglish("whitehilt_gate_rope_switch", "Work {0} instead");
        Translations.AddEnglish("whitehilt_gate_now_works", "The rope now works {0}");
        Translations.AddEnglish("whitehilt_gate_wheel", "Turn the wheel: {0} ({1})");
        Translations.AddEnglish("whitehilt_gate_crank", "Turn the crank: {0} ({1})");
        Translations.AddEnglish("whitehilt_gate_lever", "Pull the lever: {0} ({1})");
        Translations.AddEnglish("whitehilt_gate_shut_all", "Shut everything");
        Translations.AddEnglish("whitehilt_gate_shut_done", "The gate is shut, the bridge raised and the portcullis lowered");
        Translations.AddEnglish("whitehilt_gate_teach", "Use a Gate Horn here to teach it this gate's call");
        Translations.AddEnglish("whitehilt_gate_taught", "The horn now calls this gate");
        Translations.AddEnglish("whitehilt_gate_no_gate", "There is no gate near here to teach the horn");
    }

    /// <summary>
    /// The gates, drawbridges and portcullises within range of a place, nearest first.
    /// </summary>
    /// <param name="position">The place.</param>
    /// <param name="range">The range in metres.</param>
    /// <returns>The mechanisms.</returns>
    public static List<GateMechanism> Around(Vector3 position, float range)
    {
        HashSet<GameObject> pieces = new();
        if (pieceMask == 0)
        {
            pieceMask = LayerMask.GetMask("piece", "piece_nonsolid", "Default");
        }

        foreach (Collider collider in Physics.OverlapSphere(position, range, pieceMask))
        {
            ZNetView view = collider.GetComponentInParent<ZNetView>();
            if (view != null && view.IsValid())
            {
                pieces.Add(view.gameObject);
            }
        }

        return pieces.SelectMany(GateMechanism.Of)
            .Where(mechanism => Vector3.Distance(mechanism.Position, position) <= range)
            .OrderBy(mechanism => (mechanism.Position - position).sqrMagnitude)
            .ToList();
    }

    /// <summary>
    /// The nearest mechanism of a kind within range of a place, or null.
    /// </summary>
    /// <param name="position">The place.</param>
    /// <param name="range">The range in metres.</param>
    /// <param name="kind">The kind.</param>
    /// <returns>The mechanism, or null.</returns>
    public static GateMechanism Nearest(Vector3 position, float range, GateMechanismKind kind)
    {
        return Around(position, range).FirstOrDefault(mechanism => mechanism.Kind == kind);
    }

    /// <summary>
    /// Checks whether the local player may work a mechanism: the same rule as opening a door inside a ward.
    /// </summary>
    /// <param name="mechanism">The mechanism.</param>
    /// <returns>True if allowed; a ward refusing it flashes.</returns>
    public static bool MayUse(GateMechanism mechanism)
    {
        return PrivateArea.CheckAccess(mechanism.Position);
    }

    /// <summary>
    /// The translated name of a kind, e.g. "the portcullis".
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The name.</returns>
    public static string Name(GateMechanismKind kind)
    {
        return Localization.instance.Localize("$whitehilt_gate_kind_" + kind.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// The translated state of a mechanism, e.g. "open" or "lowered".
    /// </summary>
    /// <param name="mechanism">The mechanism.</param>
    /// <returns>The state.</returns>
    public static string State(GateMechanism mechanism)
    {
        string key = mechanism.Kind == GateMechanismKind.Portcullis
            ? mechanism.IsOpen ? "raised" : "lowered"
            : mechanism.IsOpen ? "open" : "closed";
        return Localization.instance.Localize("$whitehilt_gate_" + key);
    }

    /// <summary>
    /// A line saying what a control works and in what state, or that there is none in range.
    /// </summary>
    /// <param name="format">A text key with {0} for the name and {1} for the state.</param>
    /// <param name="kind">The kind worked.</param>
    /// <param name="mechanism">The mechanism, or null.</param>
    /// <param name="range">The range, for the none-found line.</param>
    /// <returns>The line.</returns>
    public static string Describe(string format, GateMechanismKind kind, GateMechanism mechanism, float range)
    {
        return mechanism == null
            ? string.Format(Localization.instance.Localize("$whitehilt_gate_none"), Name(kind), Mathf.RoundToInt(range))
            : string.Format(Localization.instance.Localize(format), Name(kind), State(mechanism));
    }

    /// <summary>
    /// Shuts every gate, raises every drawbridge and lowers every portcullis within range, where the player may.
    /// </summary>
    /// <param name="position">The place.</param>
    /// <param name="range">The range in metres.</param>
    /// <returns>How many were worked.</returns>
    public static int ShutAll(Vector3 position, float range)
    {
        int count = 0;
        foreach (GateMechanism mechanism in Around(position, range))
        {
            if (mechanism.IsOpen && MayUse(mechanism))
            {
                mechanism.Set(false);
                count++;
            }
        }

        return count;
    }
}
