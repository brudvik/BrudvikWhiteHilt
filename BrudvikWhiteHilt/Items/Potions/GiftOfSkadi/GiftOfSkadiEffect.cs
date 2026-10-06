using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSkadi;

/// <summary>
/// The status effect of the Gift of Skadi potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// Grants immunity to frost and freezing effects.
/// </summary>
public class GiftOfSkadiEffect : SE_Stats
{
    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName">Name of the potion: the effect's name and the subject of its start and stop messages.</param>
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
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Skadi.DurationMinutes.Value * 60f;
        
        // Set damage modifier for frost immunity
        m_mods = new System.Collections.Generic.List<HitData.DamageModPair>
        {
            new HitData.DamageModPair { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Immune }
        };
        
    }

    /// <summary>
    /// Sets the icon for the effect.
    /// </summary>
    /// <param name="path">Embedded resource name of the icon image.</param>
    public void SetIcon(string path)
    {
        m_icon = AssetUtilsExtended.LoadTextureFromEmbeddedResource(path).ConvertToSprite();
    }

    /// <summary>
    /// Sets up the effect - grants freezing immunity.
    /// </summary>
    /// <param name="character">The character the effect is put on.</param>
    public override void Setup(Character character)
    {
        base.Setup(character);
        
        // Remove any existing freezing effect
        character.GetSEMan().RemoveStatusEffect(SEMan.s_statusEffectFreezing);
        character.GetSEMan().RemoveStatusEffect(SEMan.s_statusEffectCold);
    }

    /// <summary>
    /// Updates the status effect to continuously remove freezing.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        // Continuously remove freezing effects
        if (m_character != null)
        {
            m_character.GetSEMan().RemoveStatusEffect(SEMan.s_statusEffectFreezing);
            m_character.GetSEMan().RemoveStatusEffect(SEMan.s_statusEffectCold);
        }
    }
}
