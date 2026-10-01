using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.SweetGaleAle;

/// <summary>
/// A gruit ale bittered with sweet gale that raises the carry weight.
/// </summary>
public class SweetGaleAle : WhiteHiltMeadBase
{
    private readonly ConfigEntry<float> carryWeight;

    /// <summary>
    /// Constructor for the SweetGaleAle class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public SweetGaleAle(ItemManager instance) : base(instance)
    {
        carryWeight = WhiteHiltConfig.BindAdminOnly(ConfigSection, "CarryWeight", 75f,
            "Carry weight added while the ale is active.", new AcceptableValueRange<float>(0f, 500f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltSweetGaleAle";

    /// <inheritdoc/>
    protected override string FullName => "Sweet Gale Ale";

    /// <inheritdoc/>
    protected override string Description => "A strong, bitter ale brewed with sweet gale, the way it was done before hops. Carriers swear by it.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Carry weight increased by 75";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStrength";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStrength";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Honey", Amount = 10, Recover = false },
        new() { Item = Foraging.SweetGale.SweetGale.PrefabName, Amount = 10, Recover = false },
        new() { Item = "Blueberries", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.8f, 0.85f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
        effect.m_addMaxCarryWeight = carryWeight.Value;
    }
}
