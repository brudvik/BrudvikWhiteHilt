using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSkadi;

/// <summary>
/// This class defines the effect of the Gift of Skadi potion.
/// Grants immunity to frost and freezing effects.
/// </summary>
public class GiftOfSkadiEffect : SE_Stats
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
        m_startMessage = $"You embrace the cold with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Immune to frost and freezing";
    }

    /// <summary>
    /// Enables the effect - duration is 1200 seconds (20 minutes).
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1200f;
        
        // Set damage modifier for frost immunity
        m_mods = new System.Collections.Generic.List<HitData.DamageModPair>
        {
            new HitData.DamageModPair { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Immune }
        };
        
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
    /// Sets up the effect - grants freezing immunity.
    /// </summary>
    /// <param name="character"></param>
    public override void Setup(Character character)
    {
        base.Setup(character);
        
        // Remove any existing freezing effect
        character.GetSEMan().RemoveStatusEffect("Freezing".GetHashCode());
        character.GetSEMan().RemoveStatusEffect("Cold".GetHashCode());
    }

    /// <summary>
    /// Updates the status effect to continuously remove freezing.
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        // Continuously remove freezing effects
        if (m_character != null)
        {
            m_character.GetSEMan().RemoveStatusEffect("Freezing".GetHashCode());
            m_character.GetSEMan().RemoveStatusEffect("Cold".GetHashCode());
        }
    }
}
