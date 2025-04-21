using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfFreya;

/// <summary>
/// This class defines the effect of the Gift of Freya potion.
/// </summary>
public class GiftOfFreyaEffect : StatusEffect
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
        m_startMessage = $"You have been bestowed the {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} is no more!";
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
    /// Sets up the effect for the character. This is called when the effect is applied to a character.
    /// </summary>
    /// <param name="character"></param>
    public override void Setup(Character character)
    {
        base.Setup(character);
        character.AddStamina(character.GetMaxStamina() + 400f);
    }

    /// <summary>
    /// Modifies the run stamina drain. This is called when the character is running.
    /// </summary>
    /// <param name="baseDrain"></param>
    /// <param name="drain"></param>
    /// <param name="dir"></param>
    public override void ModifyRunStaminaDrain(float baseDrain, ref float drain, Vector3 dir)
    {
        drain = -0.9f;
    }

    /// <summary>
    /// Modifies the jump stamina usage. This is called when the character jumps.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = -0.9f;
    }

    /// <summary>
    /// Modifies the attack stamina usage. This is called when the character attacks.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = -0.9f;
    }

    /// <summary>
    /// Modifies the block stamina usage. This is called when the character blocks.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = -0.9f;
    }

    /// <summary>
    /// Modifies the dodge stamina usage. This is called when the character dodges.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = -0.9f;
    }

    /// <summary>
    /// Modifies the swim stamina usage. This is called when the character swims.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = -0.9f;
    }

    /// <summary>
    /// Modifies the home item stamina usage. This is called when the character uses a home item.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = -0.9f;
    }

    /// <summary>
    /// Modifies the sneak stamina usage. This is called when the character sneaks.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = -0.9f;
    }

    /// <summary>
    /// Modifies the stamina regen. This is called when the character regenerates stamina.
    /// </summary>
    /// <param name="staminaRegen"></param>
    public override void ModifyStaminaRegen(ref float staminaRegen)
    {
        staminaRegen += 40f;
    }

}
