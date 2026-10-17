using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.SeaBuckthornMead;

/// <summary>
/// A sour orange mead of sea buckthorn that makes stamina come back faster.
/// </summary>
public class SeaBuckthornMead : WhiteHiltMeadBase
{
    private readonly ConfigEntry<float> staminaRegenBonus;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SeaBuckthornMead(ItemManager instance) : base(instance)
    {
        staminaRegenBonus = WhiteHiltConfig.BindAdminOnly(ConfigSection, "StaminaRegenBonus", 0.3f,
            "How much faster stamina comes back (0.3 = 30%).", new AcceptableValueRange<float>(0f, 2f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSeaBuckthornMead";

    /// <inheritdoc/>
    protected override string FullName => "Sea Buckthorn Mead";

    /// <inheritdoc/>
    protected override string Description => "A sour, sunny mead of sea buckthorn berries. Breath comes back to you before you have missed it.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Stamina regenerates {0}% faster";

    /// <inheritdoc/>
    protected override object[] TooltipValues => new object[] { Helpers.Translations.Percent(staminaRegenBonus.Value) };

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStaminaMedium";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStaminaMedium";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 8, Recover = false },
        new() { Item = Foraging.SeaBuckthorn.SeaBuckthorn.PrefabName, Amount = 10, Recover = false },
        new() { Item = "Vineberry", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.65f, 0.25f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_staminaRegenMultiplier = 1f + staminaRegenBonus.Value;
    }
}
