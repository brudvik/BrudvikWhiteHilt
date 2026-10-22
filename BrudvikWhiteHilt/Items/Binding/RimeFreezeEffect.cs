using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Binding;

/// <summary>
/// Frozen fast, the status effect of the Rime Rune: a creature that has it cannot move, and wears the game's frost.
/// </summary>
public class RimeFreezeEffect : SE_Stats
{
    /// <summary>
    /// Name of the status effect.
    /// </summary>
    public const string EffectName = "WhiteHilt_RimeFreeze";

    /// <summary>
    /// Hash of the status effect's name.
    /// </summary>
    public static readonly int Hash = EffectName.GetStableHashCode();

    /// <summary>
    /// Registers the status effect and its English text. Call from the plugin's Awake, after the binding settings.
    /// </summary>
    public static void Register()
    {
        Translations.AddEnglish("se_whitehilt_rimefreeze", "Frozen fast");
        Translations.AddEnglish("se_whitehilt_rimefreeze_tooltip", "Cannot move");
        RimeFreezeEffect freeze = ScriptableObject.CreateInstance<RimeFreezeEffect>();
        freeze.name = EffectName;
        freeze.m_name = Translations.Token("se_whitehilt_rimefreeze");
        freeze.m_tooltip = Translations.Token("se_whitehilt_rimefreeze_tooltip");
        freeze.m_ttl = BindingSettings.RimeFreezeSeconds.Value;
        freeze.m_speedModifier = -1f;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(freeze, fixReference: false));
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        m_ttl = BindingSettings.RimeFreezeSeconds.Value;

        // The game's own frost, ice on the body, is the look of it.
        if (m_startEffects == null || m_startEffects.m_effectPrefabs.Length == 0)
        {
            StatusEffect frost = ObjectDB.instance != null ? ObjectDB.instance.GetStatusEffect(SEMan.s_statusEffectFrost) : null;
            if (frost != null)
            {
                m_startEffects = frost.m_startEffects;
            }
        }

        base.Setup(character);
    }
}
