using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfTyr;

/// <summary>
/// The status effect of the Gift of Tyr potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// Grants enhanced blocking, parrying, and knockback resistance.
/// </summary>
public class GiftOfTyrEffect : SE_Stats
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
        m_startMessage = $"You stand unshakeable with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enhanced blocking, no knockback";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Tyr.DurationMinutes.Value * 60f;
        m_addMaxCarryWeight = PotionSettings.Tyr.CarryWeight.Value;
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
    /// Modifies block stamina to be nearly zero.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Tyr.BlockStaminaMultiplier.Value;
    }

    /// <summary>
    /// Modifies dodge stamina to be minimal.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Tyr.DodgeStaminaMultiplier.Value;
    }

    /// <summary>
    /// Reduces all damage taken significantly.
    /// </summary>
    /// <param name="hit">The incoming hit, before its damage is applied; changed in place.</param>
    /// <param name="attacker">Who dealt it, or null for damage without an attacker.</param>
    public override void OnDamaged(HitData hit, Character attacker)
    {
        base.OnDamaged(hit, attacker);
        
        // Reduce pushback force
        hit.m_pushForce *= PotionSettings.Tyr.PushForceMultiplier.Value;
    }
}
