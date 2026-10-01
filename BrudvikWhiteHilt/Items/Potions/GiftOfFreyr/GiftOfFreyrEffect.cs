using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfFreyr;

/// <summary>
/// This class defines the effect of the Gift of Freyr potion.
/// Grants bonuses related to farming, comfort, and peaceful activities.
/// </summary>
public class GiftOfFreyrEffect : SE_Stats
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
        m_startMessage = $"Peace and prosperity flow through you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enhanced comfort and peaceful activities";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Freyr.DurationMinutes.Value * 60f;
        m_healthRegenMultiplier = PotionSettings.Freyr.HealthRegenMultiplier.Value;
        m_staminaRegenMultiplier = PotionSettings.Freyr.StaminaRegenMultiplier.Value;
        m_addMaxCarryWeight = PotionSettings.Freyr.CarryWeight.Value;
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
    /// No stamina cost for farming activities.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Freyr.HomeItemStaminaMultiplier.Value;
    }

    /// <summary>
    /// Continuously heals when near a workbench (building area).
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        if (m_character != null)
        {
            // Passive comfort bonus effect
            m_character.Heal(PotionSettings.Freyr.HealPerSecond.Value * dt, showText: false);
            m_character.AddStamina(PotionSettings.Freyr.StaminaPerSecond.Value * dt);
        }
    }
}
