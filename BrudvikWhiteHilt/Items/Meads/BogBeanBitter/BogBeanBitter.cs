using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.BogBeanBitter;

/// <summary>
/// A bitter fever draught that ends poison, fire, frost, lightning, tar and smoke, and keeps them off for a while.
/// </summary>
public class BogBeanBitter : WhiteHiltMeadBase
{
    /// <summary>
    /// Constructor for the BogBeanBitter class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public BogBeanBitter(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltBogBeanBitter";

    /// <inheritdoc/>
    protected override string FullName => "Bog Bean Bitter";

    /// <inheritdoc/>
    protected override string Description => "A green-black draught so bitter it makes your eyes water. Whatever burns, freezes or poisons you, it burns out first.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Ends poison, burning, frost, shock, tar and smoke, and keeps them off";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadPoisonResist";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBasePoisonResist";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.BogBean.BogBean.PrefabName, Amount = 10, Recover = false },
        new() { Item = Foraging.SweetGale.SweetGale.PrefabName, Amount = 3, Recover = false },
        new() { Item = "Honey", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.5f, 0.62f, 0.38f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override float DurationSeconds => 180f;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffectInstance()
    {
        return ScriptableObject.CreateInstance<CleansingEffect>();
    }

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
    }
}
