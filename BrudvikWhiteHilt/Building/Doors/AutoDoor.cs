using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Doors;

/// <summary>
/// Closes a door, gate or window on its own: a while after the last one went through, when rain or night begins (windows)
/// and when enemies come near. The owner sets the closed state; vanilla handles sound, animation and sync.
/// </summary>
public class AutoDoor : MonoBehaviour
{
    private const string HoldRpc = "WhiteHiltDoorHold";
    private const float TickSeconds = 0.5f;

    private static readonly int HoldKey = "whitehilt_door_hold".GetStableHashCode();
    // The doors in the world, ticked a few per frame by one AutoDoorTicker rather than each by its own Update, as
    // there can be hundreds and each only needs a look twice a second.
    private static readonly List<AutoDoor> instances = new();
    private static int cursor;
    private static AutoDoorTicker ticker;

    private Door door;
    private ZNetView nview;
    private Piece piece;
    private string prefabName;
    private Vector3 localCentre;
    private int index = -1;
    private float openedAt = -1f;
    private float lastBusy;
    private bool weatherKnown;
    private bool wasWindowWeather;
    private bool weatherClosePending;

    /// <summary>
    /// Whether the door is a door, a gate or a window.
    /// </summary>
    public DoorKind Kind => AutoDoorSettings.Classify(prefabName);

    /// <summary>
    /// True while the door stands open.
    /// </summary>
    public bool IsOpen => nview != null && nview.IsValid()
        && ((nview.GetZDO().GetInt(ZDOVars.s_state) != 0) != door.m_invertedOpenClosedText);

    /// <summary>
    /// True while the door is held open with Shift + Use.
    /// </summary>
    public bool IsHeld => nview != null && nview.IsValid() && nview.GetZDO().GetBool(HoldKey);

    /// <summary>
    /// True if Shift + Use may hold this door open, i.e. something would otherwise close it.
    /// </summary>
    public bool CanHold
    {
        get
        {
            if (!AutoDoorSettings.Enabled.Value || !AutoDoorSettings.AllowHoldOpen.Value || !IsManaged)
            {
                return false;
            }

            DoorKind kind = Kind;
            return AutoDoorSettings.ClosesByTimer(kind)
                || (kind == DoorKind.Window && (AutoDoorSettings.WindowsCloseInRain.Value || AutoDoorSettings.WindowsCloseAtNight.Value))
                || (AutoDoorSettings.RaidClose.Value && !AutoDoorSettings.RaidIgnoresHoldOpen.Value);
        }
    }

    /// <summary>
    /// True if the mod may close this door: not a key door, not excluded, and built by a player if that is required.
    /// </summary>
    public bool IsManaged
    {
        get
        {
            if (door == null || door.m_keyItem != null || door.m_canNotBeClosed || AutoDoorSettings.IsExcluded(prefabName))
            {
                return false;
            }

            return !AutoDoorSettings.OnlyPlayerBuilt.Value || (piece != null && piece.IsPlacedByPlayer());
        }
    }

    /// <summary>
    /// Asks the owner to hold the door open or let it close on its own again.
    /// </summary>
    /// <param name="hold">True to hold it open.</param>
    public void RequestHold(bool hold)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(HoldRpc, hold);
        }
    }

    /// <summary>
    /// Drops the hold once the door is closed, so it closes on its own again next time. Called on the owner after UseDoor.
    /// </summary>
    public void ClearHoldIfClosed()
    {
        if (nview != null && nview.IsValid() && nview.IsOwner() && !IsOpen && IsHeld)
        {
            nview.GetZDO().Set(HoldKey, false);
        }
    }

    /// <summary>
    /// The lines added to the door's hover text.
    /// </summary>
    /// <returns>Localized text starting with a line break.</returns>
    public string GetHoverText()
    {
        string key = "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] ";
        string text = IsHeld
            ? key + "$whitehilt_door_release\n<color=orange>$whitehilt_door_held</color>"
            : key + "$whitehilt_door_hold";
        return Localization.instance.Localize(text);
    }

    /// <summary>
    /// Closes unattended timer-managed doors before leaving their active area, while their owner can still save them.
    /// </summary>
    /// <param name="referencePosition">The new centre of the local active area.</param>
    public static void CloseOutsideActiveArea(Vector3 referencePosition)
    {
        if (!AutoDoorSettings.Enabled.Value)
        {
            return;
        }

        foreach (AutoDoor auto in instances)
        {
            if (auto != null && auto.nview != null && auto.nview.IsValid() && auto.nview.IsOwner()
                && !ZNetScene.InActiveArea(auto.nview.GetZDO().GetPosition(), referencePosition)
                && auto.IsOpen && auto.IsManaged && !auto.IsHeld
                && AutoDoorSettings.ClosesByTimer(auto.Kind) && !auto.IsOpeningBusy())
            {
                auto.Close();
            }
        }
    }

    private void Awake()
    {
        door = GetComponent<Door>();
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        prefabName = Utils.GetPrefabName(gameObject);
        localCentre = FindLocalCentre();

        if (nview != null && nview.GetZDO() != null)
        {
            nview.Register<bool>(HoldRpc, RPC_Hold);
            index = instances.Count;
            instances.Add(this);
            if (ticker == null)
            {
                ticker = new GameObject("WhiteHiltAutoDoors").AddComponent<AutoDoorTicker>();
            }
        }
    }

    private void OnDestroy()
    {
        if (index < 0)
        {
            return;
        }

        // The last door takes this one's place, so removing is quick when a whole area unloads.
        int last = instances.Count - 1;
        AutoDoor moved = instances[last];
        instances[index] = moved;
        moved.index = index;
        instances.RemoveAt(last);
        index = -1;
    }

    /// <summary>
    /// Ticks the next doors in turn, so that each is ticked about twice a second. Called every frame by
    /// the <see cref="AutoDoorTicker"/>.
    /// </summary>
    /// <param name="deltaTime">The frame's time.</param>
    public static void TickSome(float deltaTime)
    {
        int count = instances.Count;
        if (count == 0)
        {
            return;
        }

        int budget = Mathf.Min(count, Mathf.CeilToInt(count * deltaTime / TickSeconds));
        for (int i = 0; i < budget; i++)
        {
            if (cursor >= instances.Count)
            {
                cursor = 0;
            }

            AutoDoor auto = instances[cursor++];
            // As with an Update of its own, a door that is switched off or in an inactive object is not ticked.
            if (auto != null && auto.isActiveAndEnabled)
            {
                auto.Visit();
            }
        }
    }

    private void Visit()
    {
        if (nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            openedAt = -1f;
            weatherKnown = false;
            return;
        }

        Tick();
    }

    // Decides whether to close the door now: on a timer when nobody is in the opening, at once when enemies come near,
    // and for windows when rain or night begins (not all through it, so they can be opened again).
    private void Tick()
    {
        // Windows close when rain or night begins, not all through it, so they can be opened again.
        bool windowWeather = Kind == DoorKind.Window && AutoDoorSettings.IsWindowWeather();
        if (weatherKnown && windowWeather && !wasWindowWeather)
        {
            weatherClosePending = true;
        }

        wasWindowWeather = windowWeather;
        weatherKnown = true;
        if (!windowWeather)
        {
            weatherClosePending = false;
        }

        if (!IsOpen)
        {
            openedAt = -1f;
            weatherClosePending = false;
            return;
        }

        float now = Time.time;
        if (openedAt < 0f)
        {
            openedAt = now;
            lastBusy = now;
        }

        if (!AutoDoorSettings.Enabled.Value || !IsManaged)
        {
            return;
        }

        bool held = IsHeld;
        bool busy = IsOpeningBusy();
        if (busy)
        {
            lastBusy = now;
        }

        if (AutoDoorSettings.RaidClose.Value && !busy && (!held || AutoDoorSettings.RaidIgnoresHoldOpen.Value) && IsEnemyNear())
        {
            Close();
            return;
        }

        if (held)
        {
            weatherClosePending = false;
            return;
        }

        if (weatherClosePending)
        {
            Close();
            return;
        }

        DoorKind kind = Kind;
        if (!busy && AutoDoorSettings.ClosesByTimer(kind) && now - Mathf.Max(openedAt, lastBusy) >= AutoDoorSettings.Delay(kind))
        {
            Close();
        }
    }

    private void Close()
    {
        if (!nview.IsValid() || !nview.IsOwner() || !IsOpen)
        {
            return;
        }

        nview.GetZDO().Set(ZDOVars.s_state, door.m_invertedOpenClosedText ? 1 : 0);
        ClearHoldIfClosed();
        door.UpdateState();
    }

    // Whether a player, or a tamed animal if the setting says so, stands in the opening, so the door is not shut on
    // them.
    private bool IsOpeningBusy()
    {
        Vector3 centre = transform.TransformPoint(localCentre);
        float radius = AutoDoorSettings.ClearRadius.Value;
        float radiusSqr = radius * radius;
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != null && !player.IsDead() && (player.transform.position - centre).sqrMagnitude <= radiusSqr)
            {
                return true;
            }
        }

        if (!AutoDoorSettings.TamesBlockClosing.Value)
        {
            return false;
        }

        foreach (Character character in Character.GetAllCharacters())
        {
            if (character != null && character.IsTamed() && !character.IsDead() && (character.transform.position - centre).sqrMagnitude <= radiusSqr)
            {
                return true;
            }
        }

        return false;
    }

    // Whether a hostile creature that has noticed someone or is out hunting is near the door, not one only wandering
    // past.
    private bool IsEnemyNear()
    {
        Vector3 centre = transform.TransformPoint(localCentre);
        float radius = AutoDoorSettings.RaidRadius.Value;
        float radiusSqr = radius * radius;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (character == null || character.IsPlayer() || character.IsDead() || character.IsTamed()
                || (character.transform.position - centre).sqrMagnitude > radiusSqr)
            {
                continue;
            }

            // Only enemies that have noticed someone or are out hunting, not a greyling wandering past.
            BaseAI ai = character.GetBaseAI();
            if (ai != null && (ai.IsAlerted() || ai.HuntPlayer()) && IsHostile(character, ai))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHostile(Character character, BaseAI ai)
    {
        switch (character.GetFaction())
        {
            case Character.Faction.Players:
            case Character.Faction.AnimalsVeg:
            case Character.Faction.PlayerSpawned:
                return false;
            case Character.Faction.Dverger:
                return ai.IsAggravated();
            default:
                return true;
        }
    }

    // A door's pivot sits at its hinge; distances are measured from the middle of the opening.
    private Vector3 FindLocalCentre()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            return Vector3.zero;
        }

        Bounds bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        {
            bounds.Encapsulate(colliders[i].bounds);
        }

        return transform.InverseTransformPoint(bounds.center);
    }

    private void RPC_Hold(long sender, bool hold)
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(HoldKey, hold);
        }
    }
}

/// <summary>
/// Ticks all <see cref="AutoDoor"/>s, a few each frame. Made by the first door that loads, and gone with the scene.
/// </summary>
public class AutoDoorTicker : MonoBehaviour
{
    private void Update()
    {
        AutoDoor.TickSome(Time.deltaTime);
    }
}
