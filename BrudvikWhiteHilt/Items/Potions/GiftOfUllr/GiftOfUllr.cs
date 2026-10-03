using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.Foraging.Angelica;
using BrudvikWhiteHilt.Items.Foraging.Juniper;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfUllr;

/// <summary>
/// The Gift of Ullr, the hunter god of bow and ski: arrows hit harder, and you move quicker and quieter.
/// Its icon is the rendered, tinted mead.
/// </summary>
public class GiftOfUllr : PotionBase
{
    private const string Name = "GiftOfUllr";

    // Bound before the base constructor, which creates the effect once to register its text.
    static GiftOfUllr()
    {
        DurationMinutes = PotionSettings.BindDuration(Name, 20f);
        BowDamage = PotionSettings.Bind(Name, "BowDamage", 0.25f, 0f, 2f, "Extra damage with bows (0.25 = 25%).");
        SpeedModifier = PotionSettings.Bind(Name, "SpeedModifier", 0.15f, 0f, 1f, "Extra movement speed (0.15 = 15%).");
        StealthModifier = PotionSettings.Bind(Name, "StealthModifier", -0.3f, -1f, 0f, "Change to how easily you are noticed; -1 makes you nearly impossible to notice.");
    }

    /// <summary>
    /// Constructor for the GiftOfUllr class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GiftOfUllr(ItemManager instance) : base(instance) { }

    /// <summary>Duration in minutes.</summary>
    public static ConfigEntry<float> DurationMinutes { get; private set; }

    /// <summary>Extra bow damage, as a share.</summary>
    public static ConfigEntry<float> BowDamage { get; private set; }

    /// <summary>Extra movement speed, as a share.</summary>
    public static ConfigEntry<float> SpeedModifier { get; private set; }

    /// <summary>Stealth modifier.</summary>
    public static ConfigEntry<float> StealthModifier { get; private set; }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Gift of Ullr";

    /// <inheritdoc/>
    protected override string Description => "Grants you the eye and stride of Ullr, the hunter god of bow and ski";

    /// <inheritdoc/>
    protected override Color Tint => new(0.6f, 0.75f, 1f);

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = Juniper.PrefabName, Amount = 5, Recover = false },
        new RequirementConfig { Item = Angelica.PrefabName, Amount = 5, Recover = false },
        new RequirementConfig { Item = "Feathers", Amount = 10, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfUllrEffect>();
        effect.Initialize(FullName);
        return effect;
    }
}

/// <summary>
/// The effect of the Gift of Ullr: more bow damage, faster and quieter movement.
/// </summary>
public class GiftOfUllrEffect : SE_Stats
{
    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName">Name of the potion.</param>
    public void Initialize(string effectName)
    {
        name = effectName;
        m_name = effectName;
        m_startMessageType = MessageHud.MessageType.Center;
        m_startMessage = $"Ullr guides your hunt with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Bows hit harder, you move faster and are harder to notice";
    }

    /// <summary>
    /// Sets the configured duration and stats.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = GiftOfUllr.DurationMinutes.Value * 60f;
        m_modifyAttackSkill = Skills.SkillType.Bows;
        m_damageModifier = 1f + GiftOfUllr.BowDamage.Value;
        m_speedModifier = GiftOfUllr.SpeedModifier.Value;
        m_stealthModifier = GiftOfUllr.StealthModifier.Value;
    }
}
