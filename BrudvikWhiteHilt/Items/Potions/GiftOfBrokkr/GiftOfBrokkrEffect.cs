using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfBrokkr;

/// <summary>
/// The status effect of the Gift of Brokkr potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// Grants enhanced crafting abilities and workstation bonuses.
/// </summary>
public class GiftOfBrokkrEffect : SE_Stats
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
        m_startMessage = $"The craft of the dwarves flows through you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enhanced crafting abilities";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Brokkr.DurationMinutes.Value * 60f;
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
    /// Modifies skill level to boost crafting-related skills.
    /// </summary>
    /// <param name="skill">The skill asked about.</param>
    /// <param name="level">Its level so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifySkillLevel(Skills.SkillType skill, ref float level)
    {
        base.ModifySkillLevel(skill, ref level);
        
        // Boost all skills while active (simulates master craftsman)
        level += PotionSettings.Brokkr.SkillBonus.Value;
        float maxLevel = PotionSettings.Brokkr.MaxSkillLevel.Value;
        if (level > maxLevel) level = maxLevel;
    }

    /// <summary>
    /// No stamina cost for home/building items.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Brokkr.HomeItemStaminaMultiplier.Value;
    }
}
