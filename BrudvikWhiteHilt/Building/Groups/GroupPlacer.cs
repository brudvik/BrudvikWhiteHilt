using BrudvikWhiteHilt.Crafting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// Places and tears down groups of pieces with the same rules as building one by one: known recipes, crafting stations,
/// wards, no-build zones, biomes and the full cost, counted together and taken from inventory and nearby chests.
/// Placing is all or nothing.
/// </summary>
public static class GroupPlacer
{
    /// <summary>
    /// One piece to place.
    /// </summary>
    public struct Item
    {
        /// <summary>The piece prefab.</summary>
        public Piece Piece;

        /// <summary>World position.</summary>
        public Vector3 Position;

        /// <summary>World rotation.</summary>
        public Quaternion Rotation;

        /// <summary>Sign text or portal name, or null.</summary>
        public string Text;
    }

    /// <summary>
    /// Finds a buildable piece prefab by name. Terrain pieces are left out.
    /// </summary>
    /// <param name="prefabName">The prefab name.</param>
    /// <returns>The piece, or null.</returns>
    public static Piece Resolve(string prefabName)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
        Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
        if (piece == null || piece.GetComponent<TerrainModifier>() != null || piece.GetComponent<TerrainOp>() != null)
        {
            return null;
        }

        return piece;
    }

    /// <summary>
    /// Adds up what a group of pieces costs.
    /// </summary>
    /// <param name="pieces">The piece prefabs.</param>
    /// <returns>Amount per resource.</returns>
    public static Dictionary<ItemDrop, int> Cost(IEnumerable<Piece> pieces)
    {
        Dictionary<ItemDrop, int> cost = new();
        foreach (Piece piece in pieces)
        {
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
            {
                continue;
            }

            foreach (Piece.Requirement requirement in piece.m_resources)
            {
                if (requirement.m_resItem != null && requirement.m_amount > 0)
                {
                    cost.TryGetValue(requirement.m_resItem, out int amount);
                    cost[requirement.m_resItem] = amount + requirement.m_amount;
                }
            }
        }

        return cost;
    }

    /// <summary>
    /// How many of a resource the player has in the inventory and in nearby chests.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The resource.</param>
    /// <returns>The count.</returns>
    public static int Have(Player player, ItemDrop item)
    {
        string name = item.m_itemData.m_shared.m_name;
        int count = player.GetInventory().CountItems(name);
        if (NearbyContainers.IsActive(NearbyContainers.Use.Building))
        {
            count += NearbyContainers.Count(NearbyContainers.Use.Building, name);
        }

        return count;
    }

    /// <summary>
    /// Checks whether a group can be placed.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="items">The pieces.</param>
    /// <param name="pay">Whether the pieces cost anything.</param>
    /// <returns>Null if it can, otherwise a message.</returns>
    public static string Check(Player player, IList<Item> items, bool pay)
    {
        int max = GroupSettings.MaxPieces.Value;
        if (max > 0 && items.Count > max)
        {
            return string.Format(Localization.instance.Localize("$msg_whitehilt_group_too_many"), items.Count, max);
        }

        bool free = player.m_noPlacementCost;
        HashSet<Piece> checkedPieces = new();
        foreach (Item item in items)
        {
            Piece piece = item.Piece;
            if (!free && checkedPieces.Add(piece))
            {
                if (!player.HaveRequirements(piece, Player.RequirementMode.IsKnown))
                {
                    return string.Format(Localization.instance.Localize("$msg_whitehilt_group_unknown_piece"), Localization.instance.Localize(piece.m_name));
                }

                if (piece.m_craftingStation != null && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench)
                    && !CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position))
                {
                    return Localization.instance.Localize("$msg_missingstation") + ": " + Localization.instance.Localize(piece.m_craftingStation.m_name);
                }
            }

            if (Location.IsInsideNoBuildLocation(item.Position))
            {
                return Localization.instance.Localize("$msg_nobuildzone");
            }

            PrivateArea ward = piece.GetComponent<PrivateArea>();
            if (!PrivateArea.CheckAccess(item.Position, ward != null ? ward.m_radius : 0f, false, ward != null))
            {
                return Localization.instance.Localize("$msg_privatezone");
            }

            if (piece.m_onlyInBiome != Heightmap.Biome.None && (Heightmap.FindBiome(item.Position) & piece.m_onlyInBiome) == 0)
            {
                return Localization.instance.Localize("$msg_wrongbiome");
            }
        }

        if (!pay || free)
        {
            return null;
        }

        List<string> missing = new();
        foreach (KeyValuePair<ItemDrop, int> cost in Cost(items.Select(item => item.Piece)))
        {
            int lacking = cost.Value - Have(player, cost.Key);
            if (lacking > 0)
            {
                missing.Add(Localization.instance.Localize(cost.Key.m_itemData.m_shared.m_name) + " " + lacking);
            }
        }

        return missing.Count == 0 ? null : string.Format(Localization.instance.Localize("$whitehilt_group_missing"), string.Join(", ", missing));
    }

    /// <summary>
    /// Places a group from the bottom up, after <see cref="Check"/> said yes. Records it as one undo step.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="items">The pieces.</param>
    /// <param name="pay">Whether the pieces cost anything.</param>
    /// <param name="removed">Pieces torn down as part of the same step (a move), or null.</param>
    /// <returns>The placed pieces.</returns>
    public static List<Piece> Place(Player player, IList<Item> items, bool pay, List<PieceSnapshot> removed = null)
    {
        List<Piece> placed = new();
        BuildUndo.BeginGroup(free: !pay, removed);
        try
        {
            foreach (Item item in items.OrderBy(item => item.Position.y))
            {
                player.PlacePiece(item.Piece, item.Position, item.Rotation, doAttack: false);
                Piece piece = BuildUndo.LastRecorded;
                if (piece != null)
                {
                    placed.Add(piece);
                    if (!string.IsNullOrEmpty(item.Text))
                    {
                        new PieceSnapshot { Text = item.Text }.ApplyText(piece);
                    }
                }

                if (pay && !player.m_noPlacementCost && !ZoneSystem.instance.GetGlobalKey(item.Piece.FreeBuildKey()))
                {
                    player.ConsumeResources(item.Piece.m_resources, 0);
                }
            }
        }
        finally
        {
            BuildUndo.EndGroup();
        }

        player.UseStamina(player.GetBuildStamina());
        return placed;
    }

    /// <summary>
    /// Checks whether a piece may be torn down: not in a ward or no-build zone, its station near, and nothing in it.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="piece">The piece.</param>
    /// <returns>True if it may.</returns>
    public static bool CanRemove(Player player, Piece piece)
    {
        if (piece == null || !piece.m_canBeRemoved || piece.m_nview == null || !piece.m_nview.IsValid()
            || Location.IsInsideNoBuildLocation(piece.transform.position) || !PrivateArea.CheckAccess(piece.transform.position, 0f, false)
            || !piece.CanBeRemoved())
        {
            return false;
        }

        ItemStand stand = piece.GetComponentInChildren<ItemStand>();
        if (stand != null && stand.HaveAttachment())
        {
            return false;
        }

        return player.m_noPlacementCost || piece.m_craftingStation == null || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench)
            || CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position);
    }

    /// <summary>
    /// Tears down pieces without drops.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="pieces">The pieces; check them with <see cref="CanRemove"/> first.</param>
    /// <param name="refund">Full: everything back, Recoverable: what the hammer gives back, None: nothing.</param>
    /// <returns>Snapshots of the torn down pieces.</returns>
    public static List<PieceSnapshot> Remove(Player player, IEnumerable<Piece> pieces, Refund refund)
    {
        List<PieceSnapshot> removed = new();
        foreach (Piece piece in pieces.Where(piece => piece != null).ToList())
        {
            removed.Add(PieceSnapshot.Of(piece));
            Piece.Requirement[] cost = piece.m_resources;
            piece.m_nview.ClaimOwnership();
            piece.GetComponent<IRemoved>()?.OnRemoved();
            WearNTear wearNTear = piece.GetComponent<WearNTear>();
            if (wearNTear != null)
            {
                wearNTear.Remove(blockDrop: true);
            }
            else
            {
                ZNetScene.instance.Destroy(piece.gameObject);
            }

            if (refund != Refund.None && !player.m_noPlacementCost)
            {
                Give(player, cost, refund == Refund.Recoverable);
            }
        }

        if (removed.Count > 0)
        {
            player.m_removeEffects.Create(removed[0].Position, Quaternion.identity);
        }

        return removed;
    }

    /// <summary>
    /// Puts resources in the player's inventory; what does not fit is dropped at the player's feet.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="cost">The requirements to give back.</param>
    /// <param name="onlyRecoverable">Only the requirements the hammer gives back.</param>
    public static void Give(Player player, Piece.Requirement[] cost, bool onlyRecoverable)
    {
        Inventory inventory = player.GetInventory();
        foreach (Piece.Requirement requirement in cost)
        {
            if (requirement.m_resItem == null || requirement.m_amount <= 0 || (onlyRecoverable && !requirement.m_recover))
            {
                continue;
            }

            GameObject prefab = requirement.m_resItem.gameObject;
            int maxStack = Mathf.Max(1, requirement.m_resItem.m_itemData.m_shared.m_maxStackSize);
            for (int left = requirement.m_amount; left > 0; left -= maxStack)
            {
                int amount = Mathf.Min(left, maxStack);
                if (inventory.CanAddItem(prefab, amount))
                {
                    inventory.AddItem(prefab, amount);
                    continue;
                }

                ItemDrop.ItemData item = requirement.m_resItem.m_itemData.Clone();
                item.m_dropPrefab = prefab;
                item.m_stack = amount;
                ItemDrop.DropItem(item, amount, player.transform.position + player.transform.forward + Vector3.up, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// What is given back when pieces are torn down.
    /// </summary>
    public enum Refund
    {
        /// <summary>Nothing, as when pieces are moved.</summary>
        None,

        /// <summary>What the hammer gives back.</summary>
        Recoverable,

        /// <summary>Everything, as when undoing a placement.</summary>
        Full
    }
}
