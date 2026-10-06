using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSleipnir;

/// <summary>
/// The status effect of the Gift of Sleipnir potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// Grants increased movement speed, no fall damage, and higher jumps.
/// </summary>
public class GiftOfSleipnirEffect : SE_Stats
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
        m_startMessage = $"You have been blessed with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Increased speed, no fall damage, higher jumps";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Sleipnir.DurationMinutes.Value * 60f;
        m_speedModifier = PotionSettings.Sleipnir.SpeedModifier.Value;
        m_jumpModifier = new UnityEngine.Vector3(0, PotionSettings.Sleipnir.JumpModifier.Value, 0);
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
    /// Modifies the fall damage taken by the character.
    /// </summary>
    /// <param name="baseDamage">The fall damage without any effect.</param>
    /// <param name="damage">The fall damage so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyFallDamage(float baseDamage, ref float damage)
    {
        damage *= PotionSettings.Sleipnir.FallDamageMultiplier.Value;
    }
}
