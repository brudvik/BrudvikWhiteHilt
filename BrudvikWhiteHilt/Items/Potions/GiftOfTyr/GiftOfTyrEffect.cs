using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfTyr;

/// <summary>
/// This class defines the effect of the Gift of Tyr potion.
/// Grants enhanced blocking, parrying, and knockback resistance.
/// </summary>
public class GiftOfTyrEffect : SE_Stats
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
        m_startMessage = $"You stand unshakeable with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enhanced blocking, no knockback";
    }

    /// <summary>
    /// Enables the effect - duration is 1200 seconds (20 minutes).
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1200f;
        m_addMaxCarryWeight = 100f; // Stand your ground with heavy loads
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
    /// Modifies block stamina to be nearly zero.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = 0f;
    }

    /// <summary>
    /// Modifies dodge stamina to be minimal.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= 0.25f;
    }

    /// <summary>
    /// Reduces all damage taken significantly.
    /// </summary>
    /// <param name="hit"></param>
    public override void OnDamaged(HitData hit, Character attacker)
    {
        base.OnDamaged(hit, attacker);
        
        // Reduce pushback force
        hit.m_pushForce *= 0.1f;
    }
}
