using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.Foraging.BogBean;
using BrudvikWhiteHilt.Items.Foraging.SphagnumMoss;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfEir;

/// <summary>
/// The Gift of Eir, the healer among the gods: heals and cleanses once when drunk, then speeds up health regeneration.
/// Brewed from Swamp herbs; its icon is the rendered, tinted mead.
/// </summary>
public class GiftOfEir : PotionBase
{
    private const string Name = "GiftOfEir";

    // Bound before the base constructor, which creates the effect once to register its text.
    static GiftOfEir()
    {
        DurationMinutes = PotionSettings.BindDuration(Name, 2f);
        HealShare = PotionSettings.BindSetting(Name, "HealShare", 0.25f, 0f, 1f, "Share of max health healed at once (0.25 = a quarter).");
        HealthRegenMultiplier = PotionSettings.BindSetting(Name, "HealthRegenMultiplier", 1.5f, 1f, 10f, "Health regeneration multiplier.");
    }

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GiftOfEir(ItemManager instance) : base(instance) { }

    /// <summary>Duration in minutes.</summary>
    public static ConfigEntry<float> DurationMinutes { get; private set; }

    /// <summary>Share of max health healed when drunk.</summary>
    public static ConfigEntry<float> HealShare { get; private set; }

    /// <summary>Health regeneration multiplier.</summary>
    public static ConfigEntry<float> HealthRegenMultiplier { get; private set; }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Gift of Eir";

    /// <inheritdoc/>
    protected override string Description => "Grants you the care of Eir, the healer among the gods";

    /// <inheritdoc/>
    protected override Color Tint => new(0.55f, 0.85f, 0.6f);

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = SphagnumMoss.PrefabName, Amount = 10, Recover = false },
        new RequirementConfig { Item = BogBean.PrefabName, Amount = 5, Recover = false },
        new RequirementConfig { Item = "Honey", Amount = 10, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfEirEffect>();
        effect.Initialize(FullName);
        return effect;
    }
}
