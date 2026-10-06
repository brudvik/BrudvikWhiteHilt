using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Binding;
using BrudvikWhiteHilt.Items.Runes.GlowRune;
using BrudvikWhiteHilt.Items.Weapons;
using BrudvikWhiteHilt.Painting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// The Rune Etching Table: a rune used on it is etched into the trophy-bound White Hilt weapon in hand, together with
/// the materials of its infusion. The rune and the materials are used up, and a rune etched before is replaced. A
/// coloured Glow Rune is etched into any White Hilt weapon or shield in hand, bound or not, and makes it glow in its
/// colour beside any infusion.
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
        Translations.AddEnglish("whitehilt_etch_hint", "Etch a rune into your bound White Hilt weapon or shield");
        Translations.AddEnglish("whitehilt_etch_target", "In hand: {0}");
        Translations.AddEnglish("whitehilt_etch_none", "no rune");
        Translations.AddEnglish("whitehilt_etch_unbound", "Bind a black trophy to it at the Binding Stone first");
        Translations.AddEnglish("msg_whitehilt_etch_usage", "Hold a trophy-bound White Hilt weapon or shield, and use a rune from the hotbar on the table");
        Translations.AddEnglish("msg_whitehilt_etch_noweapon", "Hold a White Hilt weapon");
        Translations.AddEnglish("msg_whitehilt_etch_noshield", "Hold a White Hilt shield");
        Translations.AddEnglish("msg_whitehilt_etch_unbound", "The weapon must be bound with a black trophy at the Binding Stone first");
        Translations.AddEnglish("msg_whitehilt_etch_same", "That rune is already etched into it");
        Translations.AddEnglish("msg_whitehilt_etch_need", "You need {0}");
        Translations.AddEnglish("msg_whitehilt_etch_done", "{0} is etched into your {1}");
        Translations.AddEnglish("whitehilt_etch_glow", "Glow Rune: makes any White Hilt weapon or shield glow in its colour");
        Translations.AddEnglish("msg_whitehilt_glow_nogear", "Hold a White Hilt weapon or shield");
        Translations.AddEnglish("msg_whitehilt_glow_blank", "Colour the Glow Rune at the Paint Bench first");
        Translations.AddEnglish("msg_whitehilt_glow_done", "Your {0} glows {1}");
        Translations.AddEnglish("msg_whitehilt_glow_out", "The glow in your {0} goes out");
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

        text += "\n<color=#a0a0a0>" + localization.Localize("$whitehilt_etch_glow") + "</color>";

        Player player = Player.m_localPlayer;
        ItemDrop.ItemData weapon = player != null ? HeldGear(player, shields: false) : null;
        text += TargetLine(weapon);
        text += TargetLine(player != null ? HeldShield(player) : null);
        return text;
    }

    /// <inheritdoc/>
    public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (GlowRune.IsGlowRune(item))
        {
            return user is Player glowPlayer && EtchGlow(glowPlayer, item);
        }

        Infusion infusion = item?.m_dropPrefab != null ? Infusion.FromRune(item.m_dropPrefab.name) : null;
        if (infusion == null || user is not Player player)
        {
            return false;
        }

        if (!CanUse(player, out CraftingStation forge))
        {
            return true;
        }

        ItemDrop.ItemData weapon = infusion.ForShield ? HeldShield(player) : HeldGear(player, shields: false);
        BeastDefinition bound = GearBinding.GetBound(weapon);
        if (weapon == null || bound == null)
        {
            string missing = infusion.ForShield ? "$msg_whitehilt_etch_noshield" : "$msg_whitehilt_etch_noweapon";
            player.Message(MessageHud.MessageType.Center, weapon == null ? missing : "$msg_whitehilt_etch_unbound");
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

    // Etches a glow rune's colour into the White Hilt weapon in hand, or else the shield; a black rune puts a glow out.
    private bool EtchGlow(Player player, ItemDrop.ItemData rune)
    {
        if (!CanUse(player, out CraftingStation forge))
        {
            return true;
        }

        ItemDrop.ItemData gear = HeldGear(player, shields: true);
        if (gear == null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_glow_nogear");
            return true;
        }

        if (!GlowRune.TryGetColor(rune, out Color32 color))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_glow_blank");
            return true;
        }

        player.GetInventory().RemoveOneItem(rune);
        WeaponGlow.SetColor(gear, color);
        PlayEffect(forge);
        Localization localization = Localization.instance;
        string name = localization.Localize(gear.m_shared.m_name);
        player.Message(MessageHud.MessageType.Center, WeaponGlow.IsDark(color)
            ? string.Format(localization.Localize("$msg_whitehilt_glow_out"), name)
            : string.Format(localization.Localize("$msg_whitehilt_glow_done"), name, PaintColor.Swatch(color)));
        return true;
    }

    private static string CostText(List<(string SharedName, int Amount)> cost)
    {
        return string.Join(", ", cost.Select(part => $"{part.Amount} {Localization.instance.Localize(part.SharedName)}"));
    }

    // The White Hilt shield on the player's arm.
    private static ItemDrop.ItemData HeldShield(Player player)
    {
        return GearBinding.IsShield(player.m_leftItem) ? player.m_leftItem : null;
    }

    // "In hand: <item>: <rune>" for a held piece of gear, or nothing.
    private static string TargetLine(ItemDrop.ItemData gear)
    {
        if (gear == null)
        {
            return string.Empty;
        }

        Localization localization = Localization.instance;
        Infusion current = GearBinding.GetInfusion(gear);
        string state = GearBinding.GetBound(gear) == null ? "$whitehilt_etch_unbound"
            : current != null ? Translations.Token(current.NameKey) : "$whitehilt_etch_none";
        return "\n" + string.Format(localization.Localize("$whitehilt_etch_target"), localization.Localize($"{gear.m_shared.m_name}: {state}"));
    }

    private static string ItemName(string prefabName)
    {
        ItemDrop item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>() : null;
        return item != null ? Localization.instance.Localize(item.m_itemData.m_shared.m_name) : prefabName;
    }
}
