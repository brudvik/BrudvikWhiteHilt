using BrudvikWhiteHilt.Backpack;
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

        if (!Pay(player, worn.Count))
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

    private static bool Pay(Player player, int items)
    {
        IReadOnlyList<(string Prefab, int Amount)> cost = RepairAnvilSettings.Cost();
        Inventory inventory = player.GetInventory();
        foreach ((string prefab, int amount) in cost)
        {
            string name = ItemName(prefab);
            if (inventory.CountItems(name) < amount * items)
            {
                player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_anvil_missing"),
                    amount * items, Localization.instance.Localize(name)));
                return false;
            }
        }

        foreach ((string prefab, int amount) in cost)
        {
            inventory.RemoveItem(ItemName(prefab), amount * items);
        }

        return true;
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
