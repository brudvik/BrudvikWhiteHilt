using BrudvikWhiteHilt.Patches.Portals;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;

/// <summary>
/// A White Hilt portal. Using it opens the travel map (<see cref="PortalTravelPanel"/>); Shift + Use names it. Name,
/// privacy and id use the same ZDO keys as the Portal Stations mod, so its stations carry on as White Hilt portals.
/// </summary>
public class WhiteHiltPortalComponent : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
    /// <summary>
    /// ZDO key of the portal's name.
    /// </summary>
    public static readonly int NameKey = "stationName".GetStableHashCode();

    /// <summary>
    /// ZDO key of the privacy: 0 public, anything else private.
    /// </summary>
    public static readonly int PrivacyKey = "StationFilter".GetStableHashCode();

    /// <summary>
    /// ZDO key of the portal's stable id.
    /// </summary>
    public static readonly int IdKey = "StationGUID".GetStableHashCode();

    private const int MaxNameLength = 30;
    private const string SetNameRpc = "WhiteHiltPortalSetName";
    private const string SetPrivateRpc = "WhiteHiltPortalSetPrivate";

    // Serialized, so the value set on the prefab is copied to every placed portal.
    [SerializeField]
    private bool ground;

    private ZNetView nview;
    private Piece piece;

    /// <summary>
    /// True for the portal that lies on the ground.
    /// </summary>
    public bool Ground
    {
        get => ground;
        set => ground = value;
    }

    /// <summary>
    /// The portal's name, or empty.
    /// </summary>
    public string PortalName => IsValid ? nview.GetZDO().GetString(NameKey) : string.Empty;

    /// <summary>
    /// True if only its builder may travel to it.
    /// </summary>
    public bool IsPrivate => IsValid && nview.GetZDO().GetInt(PrivacyKey) != 0;

    /// <summary>
    /// The portal's stable id, or empty until the owner has given it one.
    /// </summary>
    public string Id => IsValid ? nview.GetZDO().GetString(IdKey) : string.Empty;

    private bool IsValid => nview != null && nview.IsValid();

    /// <summary>
    /// True if the player may rename the portal and change its privacy.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True if allowed.</returns>
    public bool CanEdit(Player player)
    {
        long creator = piece != null ? piece.GetCreator() : 0L;
        return !PortalSettings.OwnerOnlyEdit || creator == 0L || creator == player.GetPlayerID();
    }

    /// <summary>
    /// Switches the portal between public and private.
    /// </summary>
    public void TogglePrivate()
    {
        if (IsValid)
        {
            nview.InvokeRPC(SetPrivateRpc, !IsPrivate);
        }
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (!IsValid || Player.m_localPlayer == null)
        {
            return string.Empty;
        }

        string text = $"{GetHoverName()}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_portal_travel";
        if (CanEdit(Player.m_localPlayer))
        {
            text += "\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_portal_rename";
        }

        return Localization.instance.Localize(text) + RunePortalPatch.GetRuneHoverText(transform.position);
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        string name = PortalName;
        string label = string.IsNullOrEmpty(name) ? (piece != null ? piece.m_name : string.Empty) : $"\"{name}\"";
        return IsPrivate ? $"{label} ($whitehilt_portalmap_private)" : label;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || !IsValid || user is not Player player)
        {
            return false;
        }

        if (alt)
        {
            if (!CanEdit(player))
            {
                player.Message(MessageHud.MessageType.Center, "$whitehilt_portal_notowner");
                return true;
            }

            TextInput.instance.RequestText(this, "$whitehilt_portal_name", MaxNameLength);
            return true;
        }

        PortalTravelPanel.Open(this);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    /// <inheritdoc/>
    public string GetText()
    {
        return PortalName;
    }

    /// <inheritdoc/>
    public void SetText(string text)
    {
        if (IsValid)
        {
            nview.InvokeRPC(SetNameRpc, (text ?? string.Empty).Trim());
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        if (!IsValid)
        {
            return;
        }

        nview.Register<string>(SetNameRpc, RPC_SetName);
        nview.Register<bool>(SetPrivateRpc, RPC_SetPrivate);
        if (nview.IsOwner() && string.IsNullOrEmpty(Id))
        {
            nview.GetZDO().Set(IdKey, Guid.NewGuid().ToString());
        }
    }

    private void RPC_SetName(long sender, string name)
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(NameKey, name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name);
        }
    }

    private void RPC_SetPrivate(long sender, bool isPrivate)
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(PrivacyKey, isPrivate ? 1 : 0);
        }
    }
}
