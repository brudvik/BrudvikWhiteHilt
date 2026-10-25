using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// The biting cold of the Deep North's night on the local player, shown with the game's freezing icon while
/// <see cref="DeepNorthNight"/> drains them. It only shows; the drain is done there.
/// </summary>
public class BitingColdEffect : StatusEffect
{
    private const string EffectKey = "se_whitehiltbitingcold";
    private const string IconEffect = "Freezing";

    private static BitingColdEffect template;

    /// <summary>
    /// Creates and registers the effect. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        template = CreateInstance<BitingColdEffect>();
        template.name = "SE_WhiteHiltBitingCold";
        template.m_name = Translations.Token(EffectKey);
        template.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
        template.m_ttl = 0f;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(template, fixReference: false));
    }

    /// <summary>
    /// Puts the effect on the player or takes it off.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="cold">Whether the cold bites now.</param>
    public static void Show(Player player, bool cold)
    {
        SEMan seman = player.GetSEMan();
        if (seman == null || template == null)
        {
            return;
        }

        bool has = seman.HaveStatusEffect(template.NameHash());
        if (cold && !has)
        {
            seman.AddStatusEffect(template);
        }
        else if (!cold && has)
        {
            seman.RemoveStatusEffect(template.NameHash(), quiet: true);
        }
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        // The game's own freezing icon, once its effects are there.
        if (m_icon == null && ObjectDB.instance != null)
        {
            m_icon = ObjectDB.instance.GetStatusEffect(IconEffect.GetStableHashCode())?.m_icon;
        }

        base.Setup(character);
    }
}
