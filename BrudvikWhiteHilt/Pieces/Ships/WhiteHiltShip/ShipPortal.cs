using BrudvikWhiteHilt.Patches.Portals;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// The rune circle on a White Hilt Ship's deck: a White Hilt portal that sails with the ship. Using it opens the travel
/// map; Shift + Use names it. Name, privacy and id live in the ship's ZDO under the portal keys, so the server lists
/// the ship among the portals and travellers arrive on its deck wherever it has sailed.
/// </summary>
public class ShipPortal : MonoBehaviour, Hoverable, Interactable, IWhiteHiltPortal
{
    /// <summary>
    /// Name of the deck portal object added to the ship prefab.
    /// </summary>
    public const string ObjectName = "WhiteHiltShipPortal";

    /// <summary>
    /// Diameter of the rune circle, in metres.
    /// </summary>
    public const float Diameter = 1.4f;

    /// <summary>
    /// Centre of the circle in ship space: the starboard deck between the mast and the helm; the cargo stands to port.
    /// </summary>
    public static readonly Vector3 DeckPosition = new(0.8f, 0.64f, -2.2f);

    private const int MaxNameLength = 30;
    private const string SetNameRpc = "WhiteHiltShipPortalSetName";
    private const string SetPrivateRpc = "WhiteHiltShipPortalSetPrivate";

    private ZNetView nview;
    private Piece piece;
    private Collider area;
    private GameObject visual;
    private bool installed;

    /// <inheritdoc/>
    public string PortalName => IsValid ? nview.GetZDO().GetString(WhiteHiltPortalComponent.NameKey) : string.Empty;

    /// <inheritdoc/>
    public bool IsPrivate => IsValid && nview.GetZDO().GetInt(WhiteHiltPortalComponent.PrivacyKey) != 0;

    /// <inheritdoc/>
    public string Id => IsValid ? nview.GetZDO().GetString(WhiteHiltPortalComponent.IdKey) : string.Empty;

    /// <inheritdoc/>
    public Vector3 Position => transform.position;

    private bool IsValid => nview != null && nview.IsValid();

    /// <summary>
    /// Shows or hides the circle as the upgrade is put on or taken off. Called every frame by the ship.
    /// </summary>
    /// <param name="on">True while the ship has the upgrade.</param>
    public void SetInstalled(bool on)
    {
        if (on && IsValid && nview.IsOwner() && string.IsNullOrEmpty(Id))
        {
            nview.GetZDO().Set(WhiteHiltPortalComponent.IdKey, Guid.NewGuid().ToString());
        }

        if (installed == on)
        {
            return;
        }

        installed = on;
        if (visual != null)
        {
            visual.SetActive(on);
        }

        if (area != null)
        {
            area.enabled = on;
        }
    }

    /// <inheritdoc/>
    public bool CanEdit(Player player)
    {
        long creator = piece != null ? piece.GetCreator() : 0L;
        return !PortalSettings.OwnerOnlyEdit || creator == 0L || creator == player.GetPlayerID();
    }

    /// <inheritdoc/>
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
        if (!IsValid || !installed || Player.m_localPlayer == null)
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
        string label = string.IsNullOrEmpty(name) ? "$whitehilt_shipportal_unnamed" : $"\"{name}\"";
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
        if (hold || !IsValid || !installed || user is not Player player)
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
        nview = GetComponentInParent<ZNetView>();
        piece = GetComponentInParent<Piece>();
        area = GetComponent<Collider>();
        visual = transform.Find("visual")?.gameObject;
        if (!IsValid)
        {
            return;
        }

        nview.Register<string>(SetNameRpc, RPC_SetName);
        nview.Register<bool>(SetPrivateRpc, RPC_SetPrivate);
    }

    private void RPC_SetName(long sender, string name)
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(WhiteHiltPortalComponent.NameKey, name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name);
        }
    }

    private void RPC_SetPrivate(long sender, bool isPrivate)
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(WhiteHiltPortalComponent.PrivacyKey, isPrivate ? 1 : 0);
        }
    }
}
