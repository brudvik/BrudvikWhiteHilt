using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfFenrir;

/// <summary>
/// This class defines the effect of the Gift of Fenrir potion.
/// Grants faster attacks, cheaper attacks, faster movement and life steal.
/// </summary>
public class GiftOfFenrirEffect : SE_Stats
{
    /// <summary>
    /// Animation speed multiplier while attacking.
    /// </summary>
    public static float AttackSpeed => PotionSettings.Fenrir.AttackSpeed.Value;

    /// <summary>
    /// Share of damage dealt that is returned as health.
    /// </summary>
    public static float LifeSteal => PotionSettings.Fenrir.LifeSteal.Value;

    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName"></param>
    public void Initialize(string effectName)
    {
        base.name = effectName;
        m_name = effectName;
        m_startMessageType = MessageHud.MessageType.Center;
        m_startMessage = $"The wolf's fury surges through you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Faster attacks, less attack stamina, faster movement, life steal";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Fenrir.DurationMinutes.Value * 60f;
        m_speedModifier = PotionSettings.Fenrir.SpeedModifier.Value;
    }

    /// <summary>
    /// Sets the icon for the effect.
    /// </summary>
    /// <param name="path"></param>
    public void SetIcon(string path)
    {
        m_icon = AssetUtilsExtended.LoadTextureFromEmbeddedResource(path).ConvertToSprite();
    }

    /// <summary>
    /// Modifies attack stamina usage.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Fenrir.AttackStaminaMultiplier.Value;
    }
}
