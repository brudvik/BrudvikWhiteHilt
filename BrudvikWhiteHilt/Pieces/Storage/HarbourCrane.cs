using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Pieces.Defenses.Siege;
using BrudvikWhiteHilt.Progression;
using BrudvikWhiteHilt.Quartermaster;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Storage;

/// <summary>
/// The Harbour Crane on the quay. Using it opens the store of the chests around it with the ship alongside as where
/// taken items go, and with a button to unload the ship. When something is loaded or unloaded the crane swings its jib
/// out over the ship, luffs it to reach the hold, lowers the hook to it, raises it and swings back; every player sees
/// it, so it also works with a ship as tall as Skidbladnir.
/// </summary>
public class HarbourCrane : QuartermasterSite, Interactable, Hoverable
{
    // The layout's measures (build_defence_extras.py).
    private static readonly Vector3 JibRoot = new(0f, 5.0f, 0f);
    private static readonly Vector3 Tip = new(0f, 5.45f, 4.6f);
    private const float RestHook = 2.6f;

    private const string SwingRpc = "WhiteHiltCraneSwing";
    private const float SwingOut = 2.5f;
    private const float Lower = 1.5f;
    private const float Pause = 0.6f;
    private const float Raise = 1.5f;
    private const float SwingBack = 2.5f;
    private const float FindSeconds = 1f;

    private Piece piece;
    private Transform jib;
    private Transform fall;
    private Transform hook;
    private float started = float.NegativeInfinity;
    private float targetYaw;
    private float targetLuff;
    private float targetHook = RestHook;
    private Container ship;
    private float foundAt = float.NegativeInfinity;

    /// <inheritdoc/>
    public override Container PreferredTarget(List<Container> containers)
    {
        float range = SiegeSettings.CraneShipRange.Value;
        return containers.Where(QuartermasterStore.IsCargo)
            .Where(container => (container.transform.position - transform.position).sqrMagnitude <= range * range)
            .OrderBy(container => (container.transform.position - transform.position).sqrMagnitude)
            .FirstOrDefault();
    }

    /// <inheritdoc/>
    public override void OnCargoMoved(Container cargo)
    {
        if (nview != null && nview.IsValid() && cargo != null)
        {
            nview.InvokeRPC(ZNetView.Everybody, SwingRpc, cargo.transform.position);
        }
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer || !QuartermasterSettings.Enabled.Value)
        {
            return false;
        }

        QuartermasterPanel.Open(this);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (Time.time - foundAt > FindSeconds)
        {
            foundAt = Time.time;
            ship = PreferredTarget(QuartermasterStore.Around(transform.position));
        }

        string line = ship != null
            ? string.Format(Localization.instance.Localize("$whitehilt_crane_ship"), Localization.instance.Localize(ship.m_name))
            : string.Format(Localization.instance.Localize("$whitehilt_crane_no_ship"), Mathf.RoundToInt(SiegeSettings.CraneShipRange.Value));
        return Localization.instance.Localize($"{GetHoverName()}\n{line}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_crane_open");
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

    /// <inheritdoc/>
    protected override void Awake()
    {
        base.Awake();
        piece = GetComponent<Piece>();
        jib = transform.Find("jib");
        fall = transform.Find("fall");
        hook = transform.Find("hook");
        if (nview != null && nview.IsValid())
        {
            nview.Register<Vector3>(SwingRpc, RPC_Swing);
        }

        Pose(0f, 0f, RestHook);
    }

    // On everyone: aims the jib at the hold and starts the swing out, down, up and back.
    private void RPC_Swing(long sender, Vector3 hold)
    {
        Vector3 local = transform.InverseTransformPoint(hold);
        targetYaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        // Luff the jib up so its reach matches the hold's distance; it cannot reach further than its length.
        Vector3 arm = Tip - JibRoot;
        float length = arm.magnitude;
        float rest = Mathf.Atan2(arm.y, arm.z);
        float reach = Mathf.Clamp(new Vector2(local.x, local.z).magnitude, 1.5f, length * Mathf.Cos(rest));
        targetLuff = Mathf.Acos(reach / length) * Mathf.Rad2Deg - rest * Mathf.Rad2Deg;
        // The hook goes down to a little above the hold, however high the ship stands.
        Vector3 tip = TipAt(targetYaw, targetLuff);
        targetHook = Mathf.Clamp(local.y + 1.2f, 0.3f, tip.y - 0.6f);
        started = Time.time;
    }

    private void Update()
    {
        if (jib == null)
        {
            return;
        }

        float t = Time.time - started;
        float total = SwingOut + Lower + Pause + Raise + SwingBack;
        if (t < 0f || t > total)
        {
            return;
        }

        float swing, hookY;
        if (t < SwingOut)
        {
            swing = Mathf.SmoothStep(0f, 1f, t / SwingOut);
            hookY = RestHook;
        }
        else if (t < SwingOut + Lower)
        {
            swing = 1f;
            hookY = Mathf.SmoothStep(RestHook, targetHook, (t - SwingOut) / Lower);
        }
        else if (t < SwingOut + Lower + Pause)
        {
            swing = 1f;
            hookY = targetHook;
        }
        else if (t < SwingOut + Lower + Pause + Raise)
        {
            swing = 1f;
            hookY = Mathf.SmoothStep(targetHook, RestHook, (t - SwingOut - Lower - Pause) / Raise);
        }
        else
        {
            swing = Mathf.SmoothStep(1f, 0f, (t - SwingOut - Lower - Pause - Raise) / SwingBack);
            hookY = RestHook;
        }

        Pose(targetYaw * swing, targetLuff * swing, hookY);
    }

    // Where the tip is with the jib turned and luffed, in the crane's space.
    private static Vector3 TipAt(float yaw, float luff)
    {
        return JibRoot + Turn(yaw, luff) * (Tip - JibRoot);
    }

    private static Quaternion Turn(float yaw, float luff)
    {
        return Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-luff, 0f, 0f);
    }

    // Turns and luffs the jib about its root on the mast, hangs the hook under the tip and stretches the fall to it.
    private void Pose(float yaw, float luff, float hookY)
    {
        if (jib == null || fall == null || hook == null)
        {
            return;
        }

        Quaternion turn = Turn(yaw, luff);
        jib.localRotation = turn;
        jib.localPosition = JibRoot - turn * JibRoot;
        Vector3 tip = TipAt(yaw, luff);
        Quaternion heading = Quaternion.Euler(0f, yaw, 0f);
        fall.localPosition = tip;
        fall.localRotation = heading;
        fall.localScale = new Vector3(1f, Mathf.Max(0.05f, (tip.y - hookY) / (Tip.y - RestHook)), 1f);
        hook.localPosition = new Vector3(tip.x, hookY, tip.z);
        hook.localRotation = heading;
    }
}

/// <summary>
/// The Harbour Crane piece.
/// </summary>
public class HarbourCranePiece : DefensePieceBase
{
    /// <summary>
    /// Constructor for the HarbourCranePiece class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public HarbourCranePiece(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "havnekran";

    /// <inheritdoc/>
    protected override string FullName => "Harbour Crane";

    /// <inheritdoc/>
    protected override string Description => "A wooden crane on a stone base for the quay. Use it to load the ship alongside straight from the chests around, or to unload it into them; the crane swings out over the ship and lowers its hook into the hold.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "RoundLog", Amount = 10, Recover = true },
        new() { Item = "Wood", Amount = 20, Recover = true },
        new() { Item = "Stone", Amount = 20, Recover = true },
        new() { Item = "LeatherScraps", Amount = 6, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 1500f;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        prefab.AddComponent<HarbourCrane>();
    }
}
