using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Binding;
using BrudvikWhiteHilt.Items.Runes;
using BrudvikWhiteHilt.Items.Runes.FlameRune;
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
        Translations.AddEnglish("whitehilt_etch_flame", "Flame Rune: sets any White Hilt weapon or shield burning in its colour");
        Translations.AddEnglish("whitehilt_etch_remove_glow", "Remove the glow, free");
        Translations.AddEnglish("whitehilt_etch_remove_flame", "Put the flame out, or bring it back as it was, free");
        Translations.AddEnglish("msg_whitehilt_glow_nogear", "Hold a White Hilt weapon or shield");
        Translations.AddEnglish("msg_whitehilt_glow_blank", "Colour the rune at the Paint Bench first");
        Translations.AddEnglish("msg_whitehilt_glow_done", "Your {0} glows {1}");
        Translations.AddEnglish("msg_whitehilt_glow_out", "The glow in your {0} goes out");
        Translations.AddEnglish("msg_whitehilt_glow_none", "Your {0} has no glow");
        Translations.AddEnglish("msg_whitehilt_flame_done", "Your {0} burns {1}");
        Translations.AddEnglish("msg_whitehilt_flame_out", "The flame of your {0} goes out");
        Translations.AddEnglish("msg_whitehilt_flame_back", "Your {0} burns as it was made to again");
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
        text += "\n<color=#a0a0a0>" + localization.Localize("$whitehilt_etch_flame") + "</color>";
        text += "\n" + localization.Localize("[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_etch_remove_glow");
        text += "\n" + localization.Localize("[<color=yellow><b>Alt + $KEY_Use</b></color>] $whitehilt_etch_remove_flame");

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
            return user is Player glowPlayer && EtchColour(glowPlayer, item, GlowRune.Name);
        }

        if (FlameRune.IsFlameRune(item))
        {
            return user is Player flamePlayer && EtchColour(flamePlayer, item, FlameRune.Name);
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

    /// <summary>
    /// Shift + Use takes the glow off the White Hilt gear in hand, Alt + Use puts its flame out or brings it back as
    /// it was; both are free. Use alone tells how the table works.
    /// </summary>
    /// <param name="user">The one using the table.</param>
    /// <param name="hold">Whether the key is held.</param>
    /// <param name="alt">Whether Shift (the alternative place key) is held.</param>
    /// <returns>True when handled.</returns>
    public override bool Interact(Humanoid user, bool hold, bool alt)
    {
        bool altKey = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (hold || user is not Player player || (!alt && !altKey))
        {
            return base.Interact(user, hold, alt);
        }

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

        Localization localization = Localization.instance;
        string name = localization.Localize(gear.m_shared.m_name);
        if (altKey)
        {
            bool wasOff = WeaponFlame.IsOff(gear);
            if (wasOff)
            {
                WeaponFlame.Restore(gear);
            }
            else
            {
                WeaponFlame.PutOut(gear);
            }

            player.Message(MessageHud.MessageType.Center, string.Format(localization.Localize(wasOff ? "$msg_whitehilt_flame_back" : "$msg_whitehilt_flame_out"), name));
        }
        else if (WeaponGlow.TryGetColor(gear, out _))
        {
            WeaponGlow.Clear(gear);
            player.Message(MessageHud.MessageType.Center, string.Format(localization.Localize("$msg_whitehilt_glow_out"), name));
        }
        else
        {
            player.Message(MessageHud.MessageType.Center, string.Format(localization.Localize("$msg_whitehilt_glow_none"), name));
            return true;
        }

        PlayEffect(forge);
        return true;
    }

    // Etches a glow or flame rune's colour into the White Hilt weapon in hand, or else the shield. A black glow rune
    // puts a glow out.
    private bool EtchColour(Player player, ItemDrop.ItemData rune, string runeName)
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

        if (!ColourRunes.TryGetColor(rune, runeName, out Color32 color))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_glow_blank");
            return true;
        }

        player.GetInventory().RemoveOneItem(rune);
        Localization localization = Localization.instance;
        string name = localization.Localize(gear.m_shared.m_name);
        string message;
        if (runeName == FlameRune.Name)
        {
            WeaponFlame.SetColor(gear, color);
            message = string.Format(localization.Localize("$msg_whitehilt_flame_done"), name, PaintColor.Swatch(color));
        }
        else
        {
            WeaponGlow.SetColor(gear, color);
            message = WeaponGlow.IsDark(color)
                ? string.Format(localization.Localize("$msg_whitehilt_glow_out"), name)
                : string.Format(localization.Localize("$msg_whitehilt_glow_done"), name, PaintColor.Swatch(color));
        }

        PlayEffect(forge);
        player.Message(MessageHud.MessageType.Center, message);
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
