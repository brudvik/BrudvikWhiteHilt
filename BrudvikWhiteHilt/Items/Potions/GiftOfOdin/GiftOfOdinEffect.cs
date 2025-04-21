using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfOdin;

/// <summary>
/// This class defines the effect of the Gift of Odin potion.
/// </summary>
public class GiftOfOdinEffect : StatusEffect
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
        m_startMessage = $"You feel immensely powerful with {effectName}!";
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

        // Boost the maximum health.
        if (character.GetMaxHealth() < 500f)
        {
            character.SetMaxHealth(500f);
        }

        // Boost the current health.
        character.Heal(character.GetMaxHealth());
    }

    /// <summary>
    /// Modifies the fall damage taken by the character. This is called when the character takes fall damage.
    /// </summary>
    /// <param name="baseDamage"></param>
    /// <param name="damage"></param>
    public override void ModifyFallDamage(float baseDamage, ref float damage)
    {
        damage += 0.05f;
        if (damage < 0f)
        {
            damage = 0f;
        }
    }

    /// <summary>
    /// Modifies the health regen. This is called when the character is regenerating health.
    /// </summary>
    /// <param name="regenMultiplier"></param>
    public override void ModifyHealthRegen(ref float regenMultiplier)
    {
        regenMultiplier += 20f;
    }

    /// <summary>
    /// Updates the status effect. This is called every frame while the effect is active.
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        m_character.Heal(20f);
    }

}
