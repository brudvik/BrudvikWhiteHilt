using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Binding;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// The Binding Stone: a black beast trophy used on it is bound to the White Hilt weapon in hand, or else the shield.
/// The trophy is used up, and a trophy bound before is replaced.
/// </summary>
public class BindingStoneComponent : RuneForgeExtensionComponent
{
    /// <inheritdoc/>
    protected override string UsageMessage => "$msg_whitehilt_bind_usage";

    /// <summary>
    /// Registers the English texts.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_bind_hint", "Bind a black beast trophy to your White Hilt weapon, or to the shield");
        Translations.AddEnglish("whitehilt_bind_target", "In hand: {0}");
        Translations.AddEnglish("whitehilt_bind_none", "not bound");
        Translations.AddEnglish("msg_whitehilt_bind_usage", "Hold a White Hilt weapon or shield, and use a black beast trophy from the hotbar on the stone");
        Translations.AddEnglish("msg_whitehilt_bind_nogear", "Hold a White Hilt weapon or shield");
        Translations.AddEnglish("msg_whitehilt_bind_same", "That trophy is already bound to it");
        Translations.AddEnglish("msg_whitehilt_bind_done", "The {0} is bound to your {1}");
    }

    /// <inheritdoc/>
    public override string GetHoverText()
    {
        string text = HoverHeader() + "\n[<color=yellow><b>1-8</b></color>] $whitehilt_bind_hint";
        Player player = Player.m_localPlayer;
        ItemDrop.ItemData gear = player != null ? HeldGear(player, shields: true) : null;
        if (gear != null)
        {
            BeastDefinition bound = GearBinding.GetBound(gear);
            string state = bound != null ? Translations.Token(bound.TrophyKey) : "$whitehilt_bind_none";
            text += "\n" + string.Format(Localization.instance.Localize("$whitehilt_bind_target"), Localization.instance.Localize($"{gear.m_shared.m_name}: {state}"));
        }

        return Localization.instance.Localize(text);
    }

    /// <inheritdoc/>
    public override bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        BeastDefinition beast = GearBinding.BeastOfTrophy(item);
        if (beast == null || user is not Player player)
        {
            return false;
        }

        if (!CanUse(player, out CraftingStation forge))
        {
            return true;
        }

        ItemDrop.ItemData gear = HeldGear(player, shields: true);
        if (gear == null)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_bind_nogear");
            return true;
        }

        if (GearBinding.GetBound(gear) == beast)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_bind_same");
            return true;
        }

        player.GetInventory().RemoveOneItem(item);
        GearBinding.Bind(gear, beast);
        PlayEffect(forge);
        player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_bind_done"),
            Localization.instance.Localize(Translations.Token(beast.TrophyKey)), Localization.instance.Localize(gear.m_shared.m_name)));
        return true;
    }
}
