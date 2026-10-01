using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfThor;

/// <summary>
/// This class defines the effect of the Gift of Thor potion.
/// Grants increased mining/chopping power and faster attack speed.
/// </summary>
public class GiftOfThorEffect : SE_Stats
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
        m_startMessage = $"Thunder courses through you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "{0}x chopping and {1}x mining damage, less building and attack stamina, immune to lightning";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Thor.DurationMinutes.Value * 60f;
        
        // Add lightning resistance as a bonus
        m_mods = new System.Collections.Generic.List<HitData.DamageModPair>
        {
            new HitData.DamageModPair { m_type = HitData.DamageType.Lightning, m_modifier = HitData.DamageModifier.Immune }
        };
        
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
    /// Modifies home item (tool) stamina usage to be minimal.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Thor.HomeItemStaminaMultiplier.Value;
    }

    /// <summary>
    /// Modifies attack stamina usage.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Thor.AttackStaminaMultiplier.Value;
    }

    /// <summary>
    /// Doubles chopping and mining damage.
    /// </summary>
    /// <param name="skill"></param>
    /// <param name="hitData"></param>
    public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
    {
        base.ModifyAttack(skill, ref hitData);
        hitData.m_damage.m_chop *= PotionSettings.Thor.ChopDamageMultiplier.Value;
        hitData.m_damage.m_pickaxe *= PotionSettings.Thor.PickaxeDamageMultiplier.Value;
    }
}
