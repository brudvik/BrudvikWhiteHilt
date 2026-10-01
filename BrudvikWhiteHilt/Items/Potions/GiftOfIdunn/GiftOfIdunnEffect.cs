using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfIdunn;

/// <summary>
/// This class defines the effect of the Gift of Idunn potion.
/// Grants greatly enhanced health and stamina regeneration.
/// </summary>
public class GiftOfIdunnEffect : SE_Stats
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
        m_startMessage = $"Youth flows through you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enhanced regeneration and vitality";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 40 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Idunn.DurationMinutes.Value * 60f;
        m_healthRegenMultiplier = PotionSettings.Idunn.HealthRegenMultiplier.Value;
        m_staminaRegenMultiplier = PotionSettings.Idunn.StaminaRegenMultiplier.Value;
        m_eitrRegenMultiplier = PotionSettings.Idunn.EitrRegenMultiplier.Value;
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
    /// Continuously regenerates a small amount of health.
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        if (m_character != null)
        {
            // Small continuous heal
            m_character.Heal(PotionSettings.Idunn.HealPerSecond.Value * dt, showText: false);
        }
    }
}
