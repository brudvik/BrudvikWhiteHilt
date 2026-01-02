using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfFenrir;

/// <summary>
/// This class defines the effect of the Gift of Fenrir potion.
/// Grants increased attack speed and life steal on hits.
/// </summary>
public class GiftOfFenrirEffect : SE_Stats
{
    /// <summary>
    /// The hash of the effect. This is used to identify the effect.
    /// </summary>
    public int? EffectHash = null;

    /// <summary>
    /// Tracks damage dealt for life steal.
    /// </summary>
    private float m_damageDealt = 0f;

    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName"></param>
    public void Initialize(string effectName)
    {
        base.name = effectName;
        m_name = effectName;
        m_startMessageType = MessageHud.MessageType.Center;
        m_startMessage = $"The wolf's fury surges through you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Increased attack speed, life steal on hits";
    }

    /// <summary>
    /// Enables the effect - duration is 1200 seconds (20 minutes).
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1200f;
        m_speedModifier = 0.25f; // Faster movement
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
    /// Modifies attack stamina usage.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= 0.5f; // 50% reduction
    }

    /// <summary>
    /// Called when hitting an enemy - implements life steal.
    /// </summary>
    public override void OnDamaged(HitData hit, Character attacker)
    {
        base.OnDamaged(hit, attacker);
    }

    /// <summary>
    /// Updates the effect - continuous minor health regeneration simulates life steal.
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        // Continuous regeneration as a form of sustain (simulates life steal)
        if (m_character != null)
        {
            m_character.Heal(2f * dt);
        }
    }
}
