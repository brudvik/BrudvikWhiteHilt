using BrudvikWhiteHilt.Items.Runes;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.RuneRack;

/// <summary>
/// Decides what a portal may carry from the rune posts around it.
/// </summary>
public static class RunePortalRules
{
    /// <summary>
    /// How close a rune post must stand to the portal you travel from; 0 when the rune posts are off.
    /// </summary>
    public static float Range => PortalSettings.RunePostRange;

    /// <summary>
    /// Collects the runes on every post near a portal.
    /// </summary>
    /// <param name="portalPosition">Position of the portal.</param>
    /// <param name="everything">True if one post near the portal holds every rune.</param>
    /// <returns>Bit mask of the runes on all posts in range.</returns>
    public static int GetRunes(Vector3 portalPosition, out bool everything)
    {
        int mask = 0;
        everything = false;
        float range = Range;
        if (range <= 0f)
        {
            return mask;
        }

        foreach (RuneRackComponent rack in RuneRackComponent.Instances)
        {
            if (rack == null || Vector3.Distance(rack.transform.position, portalPosition) > range)
            {
                continue;
            }

            int rackMask = rack.Mask;
            mask |= rackMask;
            everything |= rackMask == WhiteHiltRuneBase.FullMask;
        }

        return mask;
    }

    /// <summary>
    /// Checks whether an inventory may go through a portal with the given runes.
    /// </summary>
    /// <param name="inventory">The traveller's inventory.</param>
    /// <param name="mask">Runes near the portal.</param>
    /// <param name="everything">True if a full set of runes is near the portal. It also lets through the items vanilla blocks for every portal.</param>
    /// <returns>True if every item may go through.</returns>
    public static bool IsTeleportable(Inventory inventory, int mask, bool everything)
    {
        if (everything)
        {
            return true;
        }

        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            ItemDrop.ItemData.SharedData shared = item.m_shared;
            if (shared.m_toolTier >= 1000 || (!shared.m_teleportable && !WhiteHiltRuneBase.Unlocks(mask, shared.m_name)))
            {
                return false;
            }
        }

        return true;
    }
}
