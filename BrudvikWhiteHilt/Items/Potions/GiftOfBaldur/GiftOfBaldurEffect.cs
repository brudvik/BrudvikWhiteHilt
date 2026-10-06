using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfBaldur;

/// <summary>
/// The status effect of the Gift of Baldur potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// Grants stealth/reduced enemy detection.
/// </summary>
public class GiftOfBaldurEffect : SE_Stats
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
        m_startMessage = $"You shimmer with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enemies cannot detect you easily";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Baldur.DurationMinutes.Value * 60f;
        m_stealthModifier = PotionSettings.Baldur.StealthModifier.Value;
        m_noiseModifier = PotionSettings.Baldur.NoiseModifier.Value;
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
    /// Modifies the stealth to be nearly invisible.
    /// </summary>
    /// <param name="baseStealth">The stealth without any effect.</param>
    /// <param name="stealth">The stealth so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyStealth(float baseStealth, ref float stealth)
    {
        stealth = PotionSettings.Baldur.Stealth.Value;
    }

    /// <summary>
    /// Modifies sneak stamina to be zero.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Baldur.SneakStaminaMultiplier.Value;
    }
}
