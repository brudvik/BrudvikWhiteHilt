using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfSleipnir;

/// <summary>
/// This class defines the effect of the Gift of Sleipnir potion.
/// Grants increased movement speed, no fall damage, and higher jumps.
/// </summary>
public class GiftOfSleipnirEffect : SE_Stats
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
    /// <param name="path"></param>
    public void SetIcon(string path)
    {
        m_icon = AssetUtilsExtended.LoadTextureFromEmbeddedResource(path).ConvertToSprite();
    }

    /// <summary>
    /// Modifies the fall damage taken by the character.
    /// </summary>
    /// <param name="baseDamage"></param>
    /// <param name="damage"></param>
    public override void ModifyFallDamage(float baseDamage, ref float damage)
    {
        damage *= PotionSettings.Sleipnir.FallDamageMultiplier.Value;
    }
}
