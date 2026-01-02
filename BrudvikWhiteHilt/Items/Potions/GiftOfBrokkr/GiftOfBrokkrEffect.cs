using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfBrokkr;

/// <summary>
/// This class defines the effect of the Gift of Brokkr potion.
/// Grants enhanced crafting abilities and workstation bonuses.
/// </summary>
public class GiftOfBrokkrEffect : SE_Stats
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
        m_startMessage = $"The craft of the dwarves flows through you with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Enhanced crafting abilities";
    }

    /// <summary>
    /// Enables the effect - duration is 1200 seconds (20 minutes).
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
    /// Modifies skill level to boost crafting-related skills.
    /// </summary>
    /// <param name="skill"></param>
    /// <param name="level"></param>
    public override void ModifySkillLevel(Skills.SkillType skill, ref float level)
    {
        base.ModifySkillLevel(skill, ref level);
        
        // Boost all skills while active (simulates master craftsman)
        level += 25f;
        if (level > 100f) level = 100f;
    }

    /// <summary>
    /// No stamina cost for home/building items.
    /// </summary>
    /// <param name="baseStaminaUse"></param>
    /// <param name="staminaUse"></param>
    public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse = 0f;
    }
}
