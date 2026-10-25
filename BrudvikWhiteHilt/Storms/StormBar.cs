using BrudvikWhiteHilt.Building.Doors;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// A built Storm Bar. Where the comfort is high enough, use it while a storm is coming or raging and every door and
/// window within <see cref="StormSettings.BarRadius"/> slams shut and is barred: no one opens them, inside or out, and
/// no gust tears them open. Someone at the bar lifts it; it lifts by itself once the storm has passed. The bar keeps
/// the number of the storm it was put across in, so a bar left across holds for that storm only.
/// </summary>
public class StormBar : MonoBehaviour, Hoverable, Interactable
{
    private const string BarRpc = "WhiteHiltStormBar";
    private const float UseDistance = 3f;

    private static readonly int BarredKey = "whitehilt_stormbar_storm".GetStableHashCode();
    private static readonly List<StormBar> loaded = new();

    private ZNetView view;
    private Piece piece;

    /// <summary>Whether this is a built bar, not the ghost of one being placed.</summary>
    public bool IsBuilt => view != null && view.IsValid();

    /// <summary>Whether the bar is across now.</summary>
    public bool IsBarred => IsBuilt && Active && StormBarRules.Holds(view.GetZDO().GetInt(BarredKey), StormState.Number, StormState.Visible);

    private static bool Active => StormSettings.Enabled != null && StormSettings.Enabled.Value && StormSettings.BarEnabled.Value;

    /// <summary>
    /// The bar across that holds a door, or null. Doors and windows within its reach are held; gates, doors with a key
    /// and doors that cannot be closed are not.
    /// </summary>
    /// <param name="door">The door.</param>
    /// <returns>The bar.</returns>
    public static StormBar Holding(Door door)
    {
        if (!Active || !Barrable(door))
        {
            return null;
        }

        float radius = StormSettings.BarRadius.Value;
        Vector3 position = door.transform.position;
        return loaded.FirstOrDefault(bar => bar != null && bar.IsBarred && Vector3.Distance(bar.transform.position, position) <= radius);
    }

    /// <summary>
    /// Whether a point lies within the reach of a bar that is across.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <returns>True if it does.</returns>
    public static bool BarredAt(Vector3 point)
    {
        float radius = StormSettings.BarRadius.Value;
        return Active && loaded.Any(bar => bar != null && bar.IsBarred && Vector3.Distance(bar.transform.position, point) <= radius);
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (!IsBuilt)
        {
            return string.Empty;
        }

        string name = Localization.instance.Localize(piece != null ? piece.m_name : string.Empty);
        if (!Active)
        {
            return name;
        }

        if (IsBarred)
        {
            return Localization.instance.Localize($"{name}\n<color=orange>$whitehilt_stormbar_barred</color>"
                + "\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_stormbar_lift");
        }

        int comfort = Comfort();
        int needed = StormSettings.BarComfort.Value;
        string comfortLine = Localization.instance.Localize("$whitehilt_stormbar_comfort", comfort.ToString(), needed.ToString());
        string color = comfort >= needed ? "#a0a0a0" : "orange";
        string text = $"{name}\n<color={color}>{comfortLine}</color>";
        return Localization.instance.Localize(StormHere()
            ? text + "\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_stormbar_bar"
            : text + "\n<color=#a0a0a0>$whitehilt_stormbar_no_storm</color>");
    }

    /// <inheritdoc/>
    public string GetHoverName() => piece != null ? Localization.instance.Localize(piece.m_name) : string.Empty;

    /// <inheritdoc/>
    public float GetHoverOffset() => 0f;

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || !IsBuilt || !Active || user is not Player player
            || Vector3.Distance(player.transform.position, transform.position) > UseDistance + 2f)
        {
            return false;
        }

        if (!PrivateArea.CheckAccess(transform.position))
        {
            return false;
        }

        if (IsBarred)
        {
            view.InvokeRPC(BarRpc, false);
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_stormbar_lifted");
            return true;
        }

        int comfort = Comfort();
        switch (StormBarRules.Refusal(StormHere(), comfort, StormSettings.BarComfort.Value))
        {
            case StormBarRefusal.NoStorm:
                player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_stormbar_no_storm");
                return false;
            case StormBarRefusal.Comfort:
                player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_whitehilt_stormbar_comfort",
                    comfort.ToString(), StormSettings.BarComfort.Value.ToString()));
                return false;
        }

        view.InvokeRPC(BarRpc, true);
        SlamShut();
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_stormbar_barred");
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    // A door, or a window, that a bar may hold: not a gate, and not one with a key or one that cannot be closed.
    private static bool Barrable(Door door)
    {
        if (door == null || door.m_nview == null || !door.m_nview.IsValid() || door.m_keyItem != null || door.m_canNotBeClosed)
        {
            return false;
        }

        string prefab = Utils.GetPrefabName(door.gameObject);
        return AutoDoorSettings.Classify(prefab) != DoorKind.Gate;
    }

    // Every open door and window within reach slams shut, through the game's own door so everyone sees and hears it.
    private void SlamShut()
    {
        float radius = StormSettings.BarRadius.Value;
        foreach (Door door in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            if (Barrable(door) && door.m_nview.GetZDO().GetInt(ZDOVars.s_state) != 0
                && Vector3.Distance(door.transform.position, transform.position) <= radius)
            {
                door.m_nview.InvokeRPC("UseDoor", true);
            }
        }
    }

    private bool StormHere()
    {
        return StormState.Visible && StormSettings.HasStorms(WorldGenerator.instance != null
            ? WorldGenerator.instance.GetBiome(transform.position)
            : Heightmap.Biome.Meadows);
    }

    // The comfort where the bar stands, as the game counts it for resting there.
    private int Comfort()
    {
        Cover.GetCoverForPoint(transform.position + Vector3.up, out float cover, out bool underRoof);
        return SE_Rested.CalculateComfortLevel(underRoof && cover >= 0.8f, transform.position);
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        if (IsBuilt)
        {
            view.Register<bool>(BarRpc, RPC_Bar);
        }
    }

    private void OnEnable()
    {
        loaded.Add(this);
    }

    private void OnDisable()
    {
        loaded.Remove(this);
    }

    // On the owner: puts the bar across for the storm now, or lifts it.
    private void RPC_Bar(long sender, bool bar)
    {
        if (view.IsOwner())
        {
            view.GetZDO().Set(BarredKey, bar ? StormState.Number : 0);
        }
    }
}
