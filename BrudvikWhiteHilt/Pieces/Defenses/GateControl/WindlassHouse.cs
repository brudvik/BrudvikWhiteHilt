using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses.GateControl;

/// <summary>
/// The Windlass House: a small stone house with the machinery of the gate nearby. Its great wheel raises and lowers the
/// drawbridge, its crank the portcullis, its lever opens and closes the gate leaves, and Shift + use on any of them
/// shuts everything at once. The wheel, crank and lever turn to show how things stand.
/// </summary>
public class WindlassHouse : MonoBehaviour, Hoverable, Interactable
{
    private const float FindSeconds = 0.5f;

    private Piece piece;
    private ZNetView nview;
    private List<GateMechanism> found = new();
    private float foundAt = float.NegativeInfinity;

    /// <summary>True once built; the placement ghost has no network view and works nothing.</summary>
    public bool IsPlaced => nview != null && nview.IsValid();

    /// <summary>The gates, drawbridges and portcullises within reach, nearest first, looked for twice a second.</summary>
    public List<GateMechanism> Nearby()
    {
        if (Time.time - foundAt > FindSeconds || found.Any(mechanism => !mechanism.IsValid))
        {
            foundAt = Time.time;
            found = GateMechanisms.Around(transform.position, GateMechanisms.WindlassRange.Value);
        }

        return found;
    }

    /// <summary>The nearest mechanism of a kind within reach, or null.</summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The mechanism.</returns>
    public GateMechanism Nearest(GateMechanismKind kind)
    {
        return Nearby().FirstOrDefault(mechanism => mechanism.Kind == kind);
    }

    /// <summary>
    /// Shuts the gate, raises the drawbridge and lowers the portcullis within reach.
    /// </summary>
    /// <param name="user">The player.</param>
    public void ShutAll(Humanoid user)
    {
        if (GateMechanisms.ShutAll(transform.position, GateMechanisms.WindlassRange.Value) > 0)
        {
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_gate_shut_done"));
        }

        foundAt = float.NegativeInfinity;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer)
        {
            return false;
        }

        if (alt)
        {
            ShutAll(user);
        }

        return alt;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return GateHorn.TeachAt(user, item, transform.position, GateMechanisms.WindlassRange.Value);
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        float range = GateMechanisms.WindlassRange.Value;
        string text = GetHoverName();
        foreach ((GateMechanismKind kind, string format) in WindlassControl.Controls)
        {
            text += "\n" + GateMechanisms.Describe(format, kind, Nearest(kind), range);
        }

        text += "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_gate_shut_all";
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

    private void Awake()
    {
        piece = GetComponent<Piece>();
        nview = GetComponent<ZNetView>();
    }
}

/// <summary>
/// The wheel, the crank or the lever of a Windlass House: it works one kind of mechanism and turns to show its state.
/// </summary>
public class WindlassControl : MonoBehaviour, Hoverable, Interactable
{
    /// <summary>The controls of the house: what each works and its hover text.</summary>
    public static readonly (GateMechanismKind Kind, string Format)[] Controls =
    {
        (GateMechanismKind.Drawbridge, "$whitehilt_gate_wheel"),
        (GateMechanismKind.Portcullis, "$whitehilt_gate_crank"),
        (GateMechanismKind.Gate, "$whitehilt_gate_lever")
    };

    private const float TurnSpeed = 180f;

    /// <summary>What the control works.</summary>
    public GateMechanismKind m_kind;

    /// <summary>The axis it turns about, in its own space.</summary>
    public Vector3 m_axis = Vector3.forward;

    /// <summary>Degrees it stands at when the mechanism is closed.</summary>
    public float m_closedAngle;

    /// <summary>Degrees it stands at when the mechanism is open.</summary>
    public float m_openAngle = 270f;

    private WindlassHouse house;
    private Quaternion rest;
    private float angle;
    private bool placed;

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer || house == null)
        {
            return false;
        }

        if (alt)
        {
            house.ShutAll(user);
            return true;
        }

        GateMechanism mechanism = house.Nearest(m_kind);
        if (mechanism == null)
        {
            user.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$whitehilt_gate_none"),
                GateMechanisms.Name(m_kind), Mathf.RoundToInt(GateMechanisms.WindlassRange.Value)));
            return true;
        }

        if (GateMechanisms.MayUse(mechanism))
        {
            mechanism.Toggle();
        }

        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return house != null && house.UseItem(user, item);
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (house == null)
        {
            return string.Empty;
        }

        string format = Controls.First(control => control.Kind == m_kind).Format;
        string text = house.GetHoverName() + "\n[<color=yellow><b>$KEY_Use</b></color>] "
            + GateMechanisms.Describe(format, m_kind, house.Nearest(m_kind), GateMechanisms.WindlassRange.Value)
            + "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_gate_shut_all";
        return Localization.instance.Localize(text);
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return house != null ? house.GetHoverName() : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    private void Awake()
    {
        house = GetComponentInParent<WindlassHouse>();
        rest = transform.localRotation;
        angle = m_closedAngle;
    }

    // Turns towards the angle of the mechanism's state; the first time it is placed there at once.
    private void Update()
    {
        if (house == null || !house.IsPlaced)
        {
            return;
        }

        GateMechanism mechanism = house.Nearest(m_kind);
        float target = mechanism != null && mechanism.IsOpen ? m_openAngle : m_closedAngle;
        angle = placed ? Mathf.MoveTowards(angle, target, TurnSpeed * Time.deltaTime) : target;
        placed = true;
        transform.localRotation = rest * Quaternion.AngleAxis(angle, m_axis);
    }
}

/// <summary>
/// The Windlass House piece.
/// </summary>
public class WindlassHousePiece : DefensePieceBase
{
    /// <summary>
    /// Constructor for the WindlassHousePiece class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public WindlassHousePiece(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "vindehus";

    /// <inheritdoc/>
    protected override string FullName => "Windlass House";

    /// <inheritdoc/>
    protected override string Description => "A small stone house with the gate's machinery: a great wheel for the drawbridge, a crank for the portcullis and a lever for the gate. Shift + use on any of them shuts everything at once.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 20, Recover = true },
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Bronze", Amount = 4, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(3000f);

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        StoneDefense.Harden(prefab);
        prefab.AddComponent<WindlassHouse>();
        AddControl(groups, "wheel", GateMechanismKind.Drawbridge, Vector3.forward, 0f, 300f);
        AddControl(groups, "crank", GateMechanismKind.Portcullis, Vector3.right, 0f, 720f);
        AddControl(groups, "lever", GateMechanismKind.Gate, Vector3.right, -25f, 25f);
    }

    private void AddControl(IDictionary<string, Transform> groups, string group, GateMechanismKind kind, Vector3 axis, float closed, float open)
    {
        if (!groups.TryGetValue(group, out Transform pivot))
        {
            Jotunn.Logger.LogWarning($"{FullName}: the {group} was not found in the layout.");
            return;
        }

        WindlassControl control = pivot.gameObject.AddComponent<WindlassControl>();
        control.m_kind = kind;
        control.m_axis = axis;
        control.m_closedAngle = closed;
        control.m_openAngle = open;
    }
}
