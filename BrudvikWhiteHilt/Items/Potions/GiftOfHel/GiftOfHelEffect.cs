using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfHel;

/// <summary>
/// The status effect of the Gift of Hel potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// Provides protection from death - heals to full when taking fatal damage.
/// </summary>
public class GiftOfHelEffect : SE_Stats
{
    /// <summary>
    /// Whether the resurrection has been used.
    /// </summary>
    private bool m_resurrectionUsed = false;

    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName">Name of the potion: the effect's name and the subject of its start and stop messages.</param>
    public void Initialize(string effectName)
    {
        base.name = effectName;
        m_name = effectName;
        m_startMessageType = MessageHud.MessageType.Center;
        m_startMessage = $"Hel watches over you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Protects from death once";
    }

    /// <summary>
    /// Enables the effect - lasts until death or the configured duration, 30 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Hel.DurationMinutes.Value * 60f; // or until triggered
        m_resurrectionUsed = false;
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
    /// Monitors health and triggers resurrection when health gets critically low.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        if (m_character != null && !m_resurrectionUsed)
        {
            // If health drops below the configured share, trigger resurrection
            if (m_character.GetHealth() < m_character.GetMaxHealth() * PotionSettings.Hel.TriggerHealthFraction.Value)
            {
                TriggerResurrection();
            }
        }
    }

    /// <summary>
    /// Triggers the resurrection effect.
    /// </summary>
    private void TriggerResurrection()
    {
        if (m_resurrectionUsed || m_character == null) return;
        
        m_resurrectionUsed = true;
        
        // Heal to full
        m_character.Heal(m_character.GetMaxHealth());
        
        // Add stamina
        m_character.AddStamina(m_character.GetMaxStamina());
        
        // Show message
        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, Translations.Token(GiftOfHel.SparedMessageKey));
        
        // End the effect after use
        m_ttl = 1f;
    }

    /// <summary>
    /// Called when the character takes damage - can prevent fatal damage.
    /// </summary>
    public override void OnDamaged(HitData hit, Character attacker)
    {
        base.OnDamaged(hit, attacker);
        
        if (!m_resurrectionUsed && m_character != null)
        {
            float healthAfterHit = m_character.GetHealth() - hit.GetTotalDamage();
            if (healthAfterHit <= 0)
            {
                // This would be fatal - reduce the damage to leave 1 HP
                hit.m_damage.m_damage = 0;
                hit.m_damage.m_blunt = 0;
                hit.m_damage.m_slash = 0;
                hit.m_damage.m_pierce = 0;
                hit.m_damage.m_fire = 0;
                hit.m_damage.m_frost = 0;
                hit.m_damage.m_lightning = 0;
                hit.m_damage.m_poison = 0;
                hit.m_damage.m_spirit = 0;
                
                TriggerResurrection();
            }
        }
    }
}
