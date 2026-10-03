using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Binding;

/// <summary>
/// Dread, the status effect of the Obsidian Rune: a creature that has it runs from the nearest player.
/// </summary>
public class DreadEffect : SE_Stats
{
    /// <summary>
    /// Name of the status effect.
    /// </summary>
    public const string EffectName = "WhiteHilt_Dread";

    /// <summary>
    /// Hash of the status effect's name.
    /// </summary>
    public static readonly int Hash = EffectName.GetStableHashCode();

    /// <summary>
    /// Registers the status effect and its English text. Call from the plugin's Awake, after the binding settings.
    /// </summary>
    public static void Register()
    {
        Translations.AddEnglish("se_whitehilt_dread", "Dread");
        Translations.AddEnglish("se_whitehilt_dread_tooltip", "Runs in terror");
        DreadEffect dread = ScriptableObject.CreateInstance<DreadEffect>();
        dread.name = EffectName;
        dread.m_name = Translations.Token("se_whitehilt_dread");
        dread.m_tooltip = Translations.Token("se_whitehilt_dread_tooltip");
        dread.m_ttl = BindingSettings.DreadSeconds.Value;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(dread, fixReference: false));
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        m_ttl = BindingSettings.DreadSeconds.Value;
        base.Setup(character);
    }
}
