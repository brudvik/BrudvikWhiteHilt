using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.ValkyrieStone;

/// <summary>
/// Carries the local player to their last death point in this world for one Surtling Core, once per death.
/// The death point is the one the game keeps for the map marker; the used one is stored on the player.
/// </summary>
public class ValkyrieStoneComponent : MonoBehaviour, Hoverable, Interactable
{
    private const string UsedDeathKey = "whitehilt_valkyrie_used";
    private const string FuelPrefab = "SurtlingCore";
    private const string FallbackFuelName = "$item_surtlingcore";

    // A little above the death point, so the arrival does not start inside the ground.
    private const float ArrivalLift = 1f;

    private Piece piece;

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
    public string GetHoverText()
    {
        string text = $"{GetHoverName()}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_valkyrie_travel{CostText()}";
        if (!TryGetUnusedDeath(Player.m_localPlayer, out _, out string reason))
        {
            text += $"\n{reason}";
        }

        return Localization.instance.Localize(text);
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user is not Player player || player != Player.m_localPlayer)
        {
            return false;
        }

        if (!TryGetUnusedDeath(player, out _, out string reason))
        {
            player.Message(MessageHud.MessageType.Center, reason);
            return true;
        }

        if (player.GetInventory().CountItems(FuelName()) < PortalSettings.ValkyrieCost)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_valkyrie_nocore");
            return true;
        }

        UnifiedPopup.Push(new YesNoPopup(
            GetHoverName(),
            "$whitehilt_valkyrie_confirm",
            () =>
            {
                UnifiedPopup.Pop();
                Travel(player);
            },
            UnifiedPopup.Pop));
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private static void Travel(Player player)
    {
        // Checked again: the player may have died, dropped the core or used another stone while the popup was open.
        if (player == null || player.IsDead() || !TryGetUnusedDeath(player, out Vector3 deathPoint, out _))
        {
            return;
        }

        string fuel = FuelName();
        int cost = PortalSettings.ValkyrieCost;
        if (player.GetInventory().CountItems(fuel) < cost)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_valkyrie_nocore");
            return;
        }

        if (!player.TeleportTo(deathPoint + Vector3.up * ArrivalLift, player.transform.rotation, true))
        {
            return;
        }

        if (cost > 0)
        {
            player.GetInventory().RemoveItem(fuel, cost);
        }

        player.m_customData[UsedDeathKey] = DeathKey(deathPoint);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_valkyrie_travel");
    }

    private static bool TryGetUnusedDeath(Player player, out Vector3 deathPoint, out string reason)
    {
        deathPoint = Vector3.zero;
        PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
        if (player == null || profile == null || !profile.HaveDeathPoint())
        {
            reason = "$whitehilt_valkyrie_none";
            return false;
        }

        deathPoint = profile.GetDeathPoint();
        if (PortalSettings.ValkyrieOncePerDeath && player.m_customData.TryGetValue(UsedDeathKey, out string used) && used == DeathKey(deathPoint))
        {
            reason = "$whitehilt_valkyrie_used";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    // The world is part of the key, because the game keeps one death point per world.
    private static string DeathKey(Vector3 point)
    {
        CultureInfo invariant = CultureInfo.InvariantCulture;
        return $"{ZNet.instance.GetWorldUID()}:{point.x.ToString("R", invariant)},{point.y.ToString("R", invariant)},{point.z.ToString("R", invariant)}";
    }

    // One core keeps the translated text; other amounts are spelled out from the item name.
    private static string CostText()
    {
        int cost = PortalSettings.ValkyrieCost;
        return cost switch
        {
            <= 0 => string.Empty,
            1 => " ($whitehilt_valkyrie_cost)",
            _ => $" ({cost} {FuelName()})"
        };
    }

    private static string FuelName()
    {
        GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(FuelPrefab) : null;
        ItemDrop itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        return itemDrop != null ? itemDrop.m_itemData.m_shared.m_name : FallbackFuelName;
    }

    private void Awake()
    {
        piece = GetComponent<Piece>();
    }
}
