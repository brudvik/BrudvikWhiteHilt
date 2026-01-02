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
        m_startMessage = $"Youth flows through you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enhanced regeneration and vitality";
    }

    /// <summary>
    /// Enables the effect - duration is 2400 seconds (40 minutes) - extra long!
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 2400f; // 40 minutes - this potion lasts longer!
        m_healthRegenModifier = 5f;   // 5x health regen
        m_staminaRegenModifier = 5f;  // 5x stamina regen
        m_eitrRegenModifier = 5f;     // 5x eitr regen
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
    /// Continuously regenerates a small amount of health.
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        if (m_character != null)
        {
            // Small continuous heal
            m_character.Heal(1f * dt);
        }
    }
}
