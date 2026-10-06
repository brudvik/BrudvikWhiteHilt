using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Kraken;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfOdin;

/// <summary>
/// The status effect of the Gift of Odin potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// </summary>
public class GiftOfOdinEffect : SE_Stats
{
    /// <summary>
    /// Max health added while the effect is active.
    /// </summary>
    public static float BonusMaxHealth => PotionSettings.Odin.BonusMaxHealth.Value;

    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName">Name of the potion: the effect's name and the subject of its start and stop messages.</param>
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
    /// Enables the effect - configurable duration, 10 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Odin.DurationMinutes.Value * 60f;
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

        // The food update adds the bonus from now on; raise it at once so the heal below fills it.
        character.SetMaxHealth(character.GetMaxHealth() + BonusMaxHealth * KrakenBody.PotionFactor(character, KrakenSettings.OdinBonusShare.Value));
        character.Heal(character.GetMaxHealth() * HealingFactor(character));
    }

    /// <summary>
    /// Modifies the fall damage taken by the character. This is called when the character takes fall damage.
    /// </summary>
    /// <param name="baseDamage">The fall damage without any effect.</param>
    /// <param name="damage">The fall damage so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyFallDamage(float baseDamage, ref float damage)
    {
        damage *= Mathf.Lerp(1f, PotionSettings.Odin.FallDamageMultiplier.Value,
            KrakenBody.PotionFactor(m_character, KrakenSettings.OdinBonusShare.Value));
    }

    /// <summary>
    /// Modifies the health regen. This is called when the character is regenerating health.
    /// </summary>
    /// <param name="regenMultiplier">Multiplier of health regeneration; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyHealthRegen(ref float regenMultiplier)
    {
        regenMultiplier += PotionSettings.Odin.HealthRegenBonus.Value * HealingFactor(m_character);
    }

    /// <summary>
    /// Updates the status effect. This is called every frame while the effect is active.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        m_character.Heal(PotionSettings.Odin.HealPerSecond.Value * dt
            * HealingFactor(m_character), showText: false);
    }

    private static float HealingFactor(Character character)
    {
        return Mathf.Min(KrakenBody.PotionFactor(character, KrakenSettings.OdinHealingShare.Value),
            BeastBehaviour.OdinHealingFactor(character));
    }

}
