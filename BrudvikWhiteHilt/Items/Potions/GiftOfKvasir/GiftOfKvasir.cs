using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.Foraging.Angelica;
using BrudvikWhiteHilt.Items.Foraging.Meadowsweet;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfKvasir;

/// <summary>
/// The Gift of Kvasir, the mead of poetry brewed from the wisest of beings: every skill rises faster while it lasts.
/// Its icon is the rendered, tinted mead.
/// </summary>
public class GiftOfKvasir : PotionBase
{
    private const string Name = "GiftOfKvasir";

    // Bound before the base constructor, which creates the effect once to register its text.
    static GiftOfKvasir()
    {
        DurationMinutes = PotionSettings.BindDuration(Name, 20f);
        SkillGain = PotionSettings.BindSetting(Name, "SkillGain", 0.5f, 0f, 5f, "Extra skill experience while the gift lasts (0.5 = 50% more).");
    }

    /// <summary>
    /// Constructor for the GiftOfKvasir class.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GiftOfKvasir(ItemManager instance) : base(instance) { }

    /// <summary>Duration in minutes.</summary>
    public static ConfigEntry<float> DurationMinutes { get; private set; }

    /// <summary>Extra skill experience, as a share.</summary>
    public static ConfigEntry<float> SkillGain { get; private set; }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Gift of Kvasir";

    /// <inheritdoc/>
    protected override string Description => "Grants you a sip of the mead of poetry, brewed from the blood of wise Kvasir";

    /// <inheritdoc/>
    protected override Color Tint => new(0.95f, 0.55f, 0.4f);

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = Meadowsweet.PrefabName, Amount = 10, Recover = false },
        new RequirementConfig { Item = "Honey", Amount = 10, Recover = false },
        new RequirementConfig { Item = Angelica.PrefabName, Amount = 3, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfKvasirEffect>();
        effect.Initialize(FullName);
        return effect;
    }
}

/// <summary>
/// The effect of the Gift of Kvasir: more skill experience.
/// </summary>
public class GiftOfKvasirEffect : SE_Stats
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
        m_startMessage = $"The words and wisdom of Kvasir fill you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Every skill rises faster";
    }

    /// <summary>
    /// Sets the configured duration and skill gain.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = GiftOfKvasir.DurationMinutes.Value * 60f;
        m_raiseSkill = Skills.SkillType.All;
        m_raiseSkillModifier = GiftOfKvasir.SkillGain.Value;
    }
}
