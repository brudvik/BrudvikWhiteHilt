using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Binding;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// The Rune Etching Table: a rune used on it is etched into the trophy-bound White Hilt weapon in hand, together with
/// the materials of its infusion. The rune and the materials are used up, and a rune etched before is replaced.
/// </summary>
public class RuneEtchingTableComponent : RuneForgeExtensionComponent
{
    /// <inheritdoc/>
    protected override string UsageMessage => "$msg_whitehilt_etch_usage";

    /// <summary>
    /// Registers the English texts.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_etch_hint", "Etch a rune into your bound White Hilt weapon");
        Translations.AddEnglish("whitehilt_etch_target", "In hand: {0}");
        Translations.AddEnglish("whitehilt_etch_none", "no rune");
        Translations.AddEnglish("whitehilt_etch_unbound", "Bind a black trophy to it at the Binding Stone first");
        Translations.AddEnglish("msg_whitehilt_etch_usage", "Hold a trophy-bound White Hilt weapon, and use a rune from the hotbar on the table");
        Translations.AddEnglish("msg_whitehilt_etch_noweapon", "Hold a White Hilt weapon");
        Translations.AddEnglish("msg_whitehilt_etch_unbound", "The weapon must be bound with a black trophy at the Binding Stone first");
        Translations.AddEnglish("msg_whitehilt_etch_same", "That rune is already etched into it");
        Translations.AddEnglish("msg_whitehilt_etch_need", "You need {0}");
        Translations.AddEnglish("msg_whitehilt_etch_done", "{0} is etched into your {1}");
    }

    /// <inheritdoc/>
    public override string GetHoverText()
    {
        Localization localization = Localization.instance;
        string text = localization.Localize(HoverHeader() + "\n[<color=yellow><b>1-8</b></color>] $whitehilt_etch_hint");
        foreach (Infusion infusion in Infusion.All)
        {
            string rune = ItemName(infusion.Rune);
            string cost = CostText(BindingSettings.Cost(infusion.Kind));
            text += $"\n<color=#a0a0a0>{rune}{(cost.Length > 0 ? " + " + cost : string.Empty)}: {localization.Localize(Translations.Token(infusion.NameKey))}</color>";
        }

        Player player = Player.m_localPlayer;
        ItemDrop.ItemData weapon = player != null ? HeldGear(player, shields: false) : null;
        if (weapon != null)
        {
            Infusion current = GearBinding.GetInfusion(weapon);
            string state = GearBinding.GetBound(weapon) == null ? "$whitehilt_etch_unbound"
                : current != null ? Translations.Token(current.NameKey) : "$whitehilt_etch_none";
            text += "\n" + string.Format(localization.Localize("$whitehilt_etch_target"), localization.Localize($"{weapon.m_shared.m_name}: {state}"));
        }

        return text;
    }

    /// <inheritdoc/>
    public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        Infusion infusion = item?.m_dropPrefab != null ? Infusion.FromRune(item.m_dropPrefab.name) : null;
        if (infusion == null || user is not Player player)
        {
            return false;
        }

        if (!CanUse(player, out CraftingStation forge))
        {
            return true;
        }

        ItemDrop.ItemData weapon = HeldGear(player, shields: false);
        BeastDefinition bound = GearBinding.GetBound(weapon);
        if (weapon == null || bound == null)
        {
            player.Message(MessageHud.MessageType.Center, weapon == null ? "$msg_whitehilt_etch_noweapon" : "$msg_whitehilt_etch_unbound");
            return true;
        }

        if (GearBinding.GetInfusion(weapon) == infusion)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_etch_same");
            return true;
        }

        Inventory inventory = player.GetInventory();
        List<(string SharedName, int Amount)> cost = BindingSettings.Cost(infusion.Kind);
        if (cost.Any(part => inventory.CountItems(part.SharedName) < part.Amount))
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_etch_need"), CostText(cost)));
            return true;
        }

        inventory.RemoveOneItem(item);
        foreach ((string sharedName, int amount) in cost)
        {
            inventory.RemoveItem(sharedName, amount);
        }

        GearBinding.Infuse(weapon, infusion);
        PlayEffect(forge);
        player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_etch_done"),
            Localization.instance.Localize(Translations.Token(infusion.NameKey)), Localization.instance.Localize(weapon.m_shared.m_name)));
        return true;
    }

    private static string CostText(List<(string SharedName, int Amount)> cost)
    {
        return string.Join(", ", cost.Select(part => $"{part.Amount} {Localization.instance.Localize(part.SharedName)}"));
    }

    private static string ItemName(string prefabName)
    {
        ItemDrop item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>() : null;
        return item != null ? Localization.instance.Localize(item.m_itemData.m_shared.m_name) : prefabName;
    }
}
