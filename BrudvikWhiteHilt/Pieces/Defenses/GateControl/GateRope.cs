using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses.GateControl;

/// <summary>
/// A rope made fast on a post near a gate. Pulling it opens or closes the nearest gate, drawbridge or portcullis,
/// whichever the rope is set to; Shift + use sets it to the next kind there is nearby. Set up one rope for each, e.g.
/// one for the gate and one for the portcullis, to work them from inside the walls.
/// </summary>
public class GateRope : MonoBehaviour, Interactable, Hoverable
{
    private const float PullSeconds = 0.6f;
    private const float PullDepth = 0.25f;
    private const float FindSeconds = 0.5f;

    private static readonly int kindKey = "whitehilt_rope_kind".GetStableHashCode();

    private ZNetView nview;
    private Piece piece;
    private Transform pull;
    private Vector3 pullRest;
    private float pulledAt = float.NegativeInfinity;
    private List<GateMechanism> found = new();
    private float foundAt = float.NegativeInfinity;

    /// <summary>What the rope works, kept on the rope for everyone.</summary>
    public GateMechanismKind Kind => nview != null && nview.IsValid() ? (GateMechanismKind)nview.GetZDO().GetInt(kindKey) : GateMechanismKind.Gate;

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer || nview == null || !nview.IsValid())
        {
            return false;
        }

        float range = GateMechanisms.RopeRange.Value;
        List<GateMechanism> around = GateMechanisms.Around(transform.position, range);
        if (alt)
        {
            List<GateMechanismKind> kinds = around.Select(mechanism => mechanism.Kind).Distinct().OrderBy(kind => kind).ToList();
            if (kinds.Count == 0)
            {
                user.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$whitehilt_gate_nothing"), Mathf.RoundToInt(range)));
                return true;
            }

            if (!PrivateArea.CheckAccess(transform.position))
            {
                return true;
            }

            GateMechanismKind next = NextKind(kinds);
            nview.ClaimOwnership();
            nview.GetZDO().Set(kindKey, (int)next);
            user.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$whitehilt_gate_now_works"), GateMechanisms.Name(next)));
            return true;
        }

        GateMechanism mechanism = around.FirstOrDefault(candidate => candidate.Kind == Kind);
        if (mechanism == null)
        {
            user.Message(MessageHud.MessageType.Center,
                string.Format(Localization.instance.Localize("$whitehilt_gate_none"), GateMechanisms.Name(Kind), Mathf.RoundToInt(range)));
            return true;
        }

        if (GateMechanisms.MayUse(mechanism))
        {
            mechanism.Toggle();
            pulledAt = Time.time;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return GateHorn.TeachAt(user, item, transform.position, GateMechanisms.RopeRange.Value);
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        string name = GetHoverName();
        if (nview == null || !nview.IsValid())
        {
            return Localization.instance.Localize(name);
        }

        float range = GateMechanisms.RopeRange.Value;
        List<GateMechanism> around = Nearby();
        GateMechanism mechanism = around.FirstOrDefault(candidate => candidate.Kind == Kind);
        string text = name + "\n" + GateMechanisms.Describe("$whitehilt_gate_works", Kind, mechanism, range)
            + "\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_gate_rope_pull";
        List<GateMechanismKind> kinds = around.Select(candidate => candidate.Kind).Distinct().OrderBy(kind => kind).ToList();
        if (kinds.Count > 1 || (kinds.Count == 1 && kinds[0] != Kind))
        {
            text += "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] "
                + string.Format(Localization.instance.Localize("$whitehilt_gate_rope_switch"), GateMechanisms.Name(NextKind(kinds)));
        }

        return Localization.instance.Localize(text + "\n<color=#AAAAAA>$whitehilt_gate_teach</color>");
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? piece.m_name : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    // The next kind after the current one among those nearby, round to the first.
    private GateMechanismKind NextKind(List<GateMechanismKind> kinds)
    {
        GateMechanismKind current = Kind;
        foreach (GateMechanismKind kind in kinds)
        {
            if (kind > current)
            {
                return kind;
            }
        }

        return kinds[0];
    }

    // The hover text is asked for every frame; looking round for gates twice a second is enough.
    private List<GateMechanism> Nearby()
    {
        if (Time.time - foundAt > FindSeconds || found.Any(mechanism => !mechanism.IsValid))
        {
            foundAt = Time.time;
            found = GateMechanisms.Around(transform.position, GateMechanisms.RopeRange.Value);
        }

        return found;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        pull = transform.Find("pull");
        if (pull != null)
        {
            pullRest = pull.localPosition;
        }
    }

    // The toggle dips down and comes back up when the rope is pulled.
    private void Update()
    {
        if (pull == null)
        {
            return;
        }

        float t = (Time.time - pulledAt) / PullSeconds;
        pull.localPosition = pullRest + Vector3.down * (t >= 0f && t < 1f ? Mathf.Sin(t * Mathf.PI) * PullDepth : 0f);
    }
}

/// <summary>
/// The Gate Rope: a post with a cleat and a rope that works a gate, drawbridge or portcullis nearby.
/// </summary>
public class GateRopePiece : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public GateRopePiece(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "porttau";

    /// <inheritdoc/>
    protected override string FullName => "Gate Rope";

    /// <inheritdoc/>
    protected override string Description => "A rope made fast on a post. Pull it to open or close the gate, drawbridge or portcullis nearby, from inside the walls. Shift + use sets which one; set up one rope for each.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 4, Recover = true },
        new() { Item = "LeatherScraps", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 400f;

    /// <inheritdoc/>
    protected override string BuildStation => null;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        // The cloned pole needs a workbench; the rope is put up wherever the gate is.
        prefab.GetComponent<Piece>().m_craftingStation = null;
        prefab.AddComponent<GateRope>();
    }
}
