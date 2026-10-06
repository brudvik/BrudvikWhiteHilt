using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfLoki;

/// <summary>
/// The status effect of the Gift of Loki potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// </summary>
public class GiftOfLokiEffect : SE_Stats
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
        m_startMessage = $"The power of {effectName} has arrived!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = effectName;
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Loki.DurationMinutes.Value * 60f;
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
    /// Setups the effect for the character. This is called when the effect is applied to a character.
    /// </summary>
    /// <param name="character">The character the effect is put on.</param>
    public override void Setup(Character character)
    {
        base.Setup(character);

        // Boost the current Eitr.
        character.AddEitr(PotionSettings.Loki.BonusEitr.Value);
    }

    /// <summary>
    /// Modifies the Eitr regen. This is called when the character is regenerating Eitr.
    /// </summary>
    /// <param name="staminaRegen">Multiplier of eitr regeneration (the game names it after stamina); changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyEitrRegen(ref float staminaRegen)
    {
        staminaRegen += PotionSettings.Loki.EitrRegenBonus.Value;
    }

}
