using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Kraken;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfFreya;

/// <summary>
/// The status effect of the Gift of Freya potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// </summary>
public class GiftOfFreyaEffect : SE_Stats
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
        m_startMessage = $"You have been bestowed the {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} is no more!";
        m_tooltip = effectName;
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Freya.DurationMinutes.Value * 60f;
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
    /// Sets up the effect for the character. This is called when the effect is applied to a character.
    /// </summary>
    /// <param name="character">The character the effect is put on.</param>
    public override void Setup(Character character)
    {
        base.Setup(character);
        character.AddStamina(character.GetMaxStamina() + PotionSettings.Freya.BonusStamina.Value
            * KrakenBody.PotionFactor(character, KrakenSettings.FreyaShare.Value));
    }

    /// <summary>
    /// Modifies the run stamina drain. This is called when the character is running.
    /// </summary>
    /// <param name="baseDrain">Stamina drain of running without any effect.</param>
    /// <param name="drain">The drain so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    /// <param name="dir">Direction of the run.</param>
    public override void ModifyRunStaminaDrain(float baseDrain, ref float drain, Vector3 dir)
    {
        drain = StaminaCost(baseDrain);
    }

    /// <summary>
    /// Modifies the jump stamina usage. This is called when the character jumps.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = StaminaCost(baseStaminaUse);
    }

    /// <summary>
    /// Modifies the attack stamina usage. This is called when the character attacks.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = StaminaCost(baseStaminaUse);
    }

    /// <summary>
    /// Modifies the block stamina usage. This is called when the character blocks.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = StaminaCost(baseStaminaUse);
    }

    /// <summary>
    /// Modifies the dodge stamina usage. This is called when the character dodges.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = StaminaCost(baseStaminaUse);
    }

    /// <summary>
    /// Modifies the swim stamina usage. This is called when the character swims.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = StaminaCost(baseStaminaUse);
    }

    /// <summary>
    /// Modifies the home item stamina usage. This is called when the character uses a home item.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = StaminaCost(baseStaminaUse);
    }

    /// <summary>
    /// Modifies the sneak stamina usage. This is called when the character sneaks.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = StaminaCost(baseStaminaUse);
    }

    /// <summary>
    /// Modifies the stamina regen. This is called when the character regenerates stamina.
    /// </summary>
    /// <param name="staminaRegen">Multiplier of stamina regeneration; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyStaminaRegen(ref float staminaRegen)
    {
        staminaRegen += PotionSettings.Freya.StaminaRegenBonus.Value * KrakenBody.PotionFactor(m_character, KrakenSettings.FreyaShare.Value);
    }

    private float StaminaCost(float baseCost)
    {
        float share = KrakenBody.PotionFactor(m_character, KrakenSettings.FreyaShare.Value);
        float cost = Mathf.Lerp(baseCost, PotionSettings.Freya.StaminaUse.Value, share);
        return share < 1f ? Mathf.Max(0f, cost) : cost;
    }

}
