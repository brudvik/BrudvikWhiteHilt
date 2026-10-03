using BrudvikWhiteHilt.Items.Meads.BogBeanBitter;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfEir;

/// <summary>
/// The effect of the Gift of Eir: heals at once, cleanses poison, fire and frost and keeps them off, and speeds up
/// health regeneration.
/// </summary>
public class GiftOfEirEffect : SE_Stats
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
        m_startMessage = $"Eir tends your wounds with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Heals at once, ends poison, fire and frost and keeps them off, faster health regeneration";
    }

    /// <summary>
    /// Sets the configured duration and regeneration.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = GiftOfEir.DurationMinutes.Value * 60f;
        m_healthRegenMultiplier = GiftOfEir.HealthRegenMultiplier.Value;
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        base.Setup(character);
        if (m_character != null)
        {
            m_character.Heal(m_character.GetMaxHealth() * GiftOfEir.HealShare.Value);
            CleansingEffect.Cleanse(m_character);
        }
    }

    /// <inheritdoc/>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        CleansingEffect.Cleanse(m_character);
    }
}
