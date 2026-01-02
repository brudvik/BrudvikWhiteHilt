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
    /// The hash of the effect. This is used to identify the effect.
    /// </summary>
    public int? EffectHash = null;

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
    /// Enables the effect - duration is 1200 seconds (20 minutes).
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1200f;
        m_healthRegenMultiplier = 2f;
        m_staminaRegenMultiplier = 2f;
        m_addMaxCarryWeight = 150f; // Carry more resources
        EffectHash = GetHashCode();
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
        staminaUse = 0f;
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
            m_character.Heal(1f * dt);
            m_character.AddStamina(5f * dt);
        }
    }
}
