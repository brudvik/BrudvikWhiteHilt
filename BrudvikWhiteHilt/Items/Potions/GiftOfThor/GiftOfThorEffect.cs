using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfThor;

/// <summary>
/// The status effect of the Gift of Thor potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// Grants increased mining/chopping power and faster attack speed.
/// </summary>
public class GiftOfThorEffect : SE_Stats
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
    /// <param name="path">Embedded resource name of the icon image.</param>
    public void SetIcon(string path)
    {
        m_icon = AssetUtilsExtended.LoadTextureFromEmbeddedResource(path).ConvertToSprite();
    }

    /// <summary>
    /// Modifies home item (tool) stamina usage to be minimal.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Thor.HomeItemStaminaMultiplier.Value;
    }

    /// <summary>
    /// Modifies attack stamina usage.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Thor.AttackStaminaMultiplier.Value;
    }

    /// <summary>
    /// Doubles chopping and mining damage.
    /// </summary>
    /// <param name="skill">The skill of the attacking weapon.</param>
    /// <param name="hitData">The hit the attack deals; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
    {
        base.ModifyAttack(skill, ref hitData);
        hitData.m_damage.m_chop *= PotionSettings.Thor.ChopDamageMultiplier.Value;
        hitData.m_damage.m_pickaxe *= PotionSettings.Thor.PickaxeDamageMultiplier.Value;
    }
}
