using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfLoki;

/// <summary>
/// This class defines the effect of the Gift of Loki potion.
/// </summary>
public class GiftOfLokiEffect : StatusEffect
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
        m_startMessage = $"The power of {effectName} has arrived!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = effectName;
    }

    /// <summary>
    /// Enables the effect - default is 1200f which is 20 minutes (seconds)
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1200f;
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
    /// Setups the effect for the character. This is called when the effect is applied to a character.
    /// </summary>
    /// <param name="character"></param>
    public override void Setup(Character character)
    {
        base.Setup(character);

        // Boost the current Eitr.
        character.AddEitr(500f);
    }

    /// <summary>
    /// Modifies the Eitr regen. This is called when the character is regenerating Eitr.
    /// </summary>
    /// <param name="staminaRegen"></param>
    public override void ModifyEitrRegen(ref float staminaRegen)
    {
        staminaRegen += 80f;
    }

}
