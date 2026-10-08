using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Crafting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.RepairAnvil;

/// <summary>
/// Using the anvil repairs everything the player wears at once, and the shield in its slot, with the forge's repair
/// sound. Free unless the server sets a cost per item.
/// </summary>
public class RepairAnvilComponent : MonoBehaviour, Hoverable, Interactable
{
    private static EffectList repairEffects;

    private Piece piece;

    /// <inheritdoc/>
    public string GetHoverText()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return string.Empty;
        }

        string action = RepairAnvilSettings.WholeInventory.Value ? "$whitehilt_anvil_repair_all" : "$whitehilt_anvil_repair";
        string text = $"{GetHoverName()}\n[<color=yellow><b>$KEY_Use</b></color>] {action}";
        int worn = Repairable(player).Count;
        if (worn > 0)
        {
            text += $"\n{string.Format(Localization.instance.Localize("$whitehilt_anvil_worn"), worn)}";
        }

        IReadOnlyList<(string Prefab, int Amount)> cost = RepairAnvilSettings.Cost();
        if (cost.Count > 0)
        {
            text += "\n$whitehilt_anvil_cost: " + string.Join(", ", cost.Select(part => $"{part.Amount} {ItemName(part.Prefab)}"));
        }

        return Localization.instance.Localize(text);
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
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user is not Player player || !PrivateArea.CheckAccess(transform.position))
        {
            return false;
        }

        List<ItemDrop.ItemData> worn = Repairable(player);
        if (worn.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_anvil_nothing");
            return true;
        }

        if (!Pay(player, worn.Count, () =>
            {
                if (this != null)
                {
                    Interact(player, false, false);
                }
            }))
        {
            return true;
        }

        foreach (ItemDrop.ItemData item in worn)
        {
            if (RepairAnvilSettings.RaiseCraftingSkill.Value)
            {
                player.RaiseSkill(Skills.SkillType.Crafting, 1f - item.m_durability / item.GetMaxDurability());
            }

            item.m_durability = item.GetMaxDurability();
        }

        RepairEffects()?.Create(transform.position, Quaternion.identity);
        player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_anvil_repaired"), worn.Count));
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void Awake()
    {
        piece = GetComponent<Piece>();
    }

    // Worn out and either worn, the shield in its slot, or anything carried when the server allows it.
    private static List<ItemDrop.ItemData> Repairable(Player player)
    {
        List<ItemDrop.ItemData> worn = new();
        player.GetInventory().GetWornItems(worn);
        if (!RepairAnvilSettings.WholeInventory.Value)
        {
            worn.RemoveAll(item => !player.IsItemEquiped(item) && item.m_gridPos != HandSlots.ShieldSlot);
        }

        return worn;
    }

    // Takes the cost of repairing a number of items from the player and nearby chests, as crafting does, checking all
    // of it first so nothing is taken when something is missing; again repairs once what lay in other players' chests
    // has arrived.
    private static bool Pay(Player player, int items, System.Action again)
    {
        List<(string Name, int Amount)> needs = RepairAnvilSettings.Cost().Select(part => (ItemName(part.Prefab), part.Amount * items)).ToList();
        (string Name, int Amount) missing = needs.FirstOrDefault(need => ChestCost.Have(player, NearbyContainers.Use.Crafting, need.Name) < need.Amount);
        string lacking = missing.Name == null ? "$msg_whitehilt_chests_notenough" : string.Format(
            Localization.instance.Localize("$msg_whitehilt_anvil_missing"), missing.Amount, Localization.instance.Localize(missing.Name));
        return ChestCost.Pay(player, NearbyContainers.Use.Crafting, needs, again, lacking);
    }

    private static string ItemName(string prefab)
    {
        return ObjectDB.instance?.GetItemPrefab(prefab)?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name ?? prefab;
    }

    private static EffectList RepairEffects()
    {
        if (repairEffects == null && ZNetScene.instance != null)
        {
            repairEffects = ZNetScene.instance.GetPrefab("forge")?.GetComponent<CraftingStation>()?.m_repairItemDoneEffects;
        }

        return repairEffects;
    }
}
