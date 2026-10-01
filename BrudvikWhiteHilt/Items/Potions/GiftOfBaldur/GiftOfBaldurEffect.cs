using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfBaldur;

/// <summary>
/// This class defines the effect of the Gift of Baldur potion.
/// Grants stealth/reduced enemy detection.
/// </summary>
public class GiftOfBaldurEffect : SE_Stats
{
    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName"></param>
    public void Initialize(string effectName)
    {
        base.name = effectName;
        m_name = effectName;
        m_startMessageType = MessageHud.MessageType.Center;
        m_startMessage = $"You shimmer with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enemies cannot detect you easily";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Baldur.DurationMinutes.Value * 60f;
        m_stealthModifier = PotionSettings.Baldur.StealthModifier.Value;
        m_noiseModifier = PotionSettings.Baldur.NoiseModifier.Value;
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
    /// Modifies the stealth to be nearly invisible.
    /// </summary>
    /// <param name="stealth"></param>
    public override void ModifyStealth(float baseStealth, ref float stealth)
    {
        stealth = PotionSettings.Baldur.Stealth.Value;
    }

    /// <summary>
    /// Modifies sneak stamina to be zero.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Baldur.SneakStaminaMultiplier.Value;
    }
}
