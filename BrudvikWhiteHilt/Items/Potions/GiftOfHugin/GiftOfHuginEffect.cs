using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using System;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfHugin;

/// <summary>
/// This class defines the effect of the Gift of Hugin potion.
/// </summary>
public class GiftOfHuginEffect : SE_Stats
{
    /// <summary>
    /// The player character that the effect is applied to.
    /// </summary>
    private Player player;

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
        m_tooltip = effectName;
    }

    /// <summary>
    /// Enables the effect - this is an instant effect with no duration.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1f; // Instant effect, just need a brief duration
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
        player = character as Player;

        player.m_skills.GetSkillList().ForEach(skill =>
        {
            try
            {
                skill.m_level = 100;
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error setting skill level {skill.m_info.m_skill}: {ex}");
            }
        });

        var package = new ZPackage();

        try
        {
            player.m_skills.Save(package);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"Error saving skills: {ex}");
        }
        
    }

}
