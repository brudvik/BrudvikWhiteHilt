using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfNjord;

/// <summary>
/// This class defines the effect of the Gift of Njord potion.
/// Grants the ability to breathe underwater and swim effortlessly.
/// </summary>
public class GiftOfNjordEffect : SE_Stats
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
        m_startMessage = $"You have been blessed with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Swim faster, no swim stamina drain, cannot drown";
    }

    /// <summary>
    /// Enables the effect - duration is 1200 seconds (20 minutes).
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1200f;
        m_swimSpeedModifier = 1.0f;
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
    /// Modifies the swim stamina usage to zero.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = 0f;
    }

    /// <summary>
    /// Updates the status effect to prevent drowning.
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        // Keep stamina above minimum while swimming to prevent drowning
        if (m_character != null && m_character.IsSwimming() && m_character is Player player)
        {
            if (player.GetStamina() < 20f)
            {
                player.AddStamina(50f);
            }
        }
    }
}
