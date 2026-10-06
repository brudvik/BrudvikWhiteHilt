using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfRatatoskr;

/// <summary>
/// The status effect of the Gift of Ratatoskr potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// Grants increased sprint speed and agility.
/// </summary>
public class GiftOfRatatoskrEffect : SE_Stats
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
        m_startMessage = $"You have been blessed with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Increased agility and sprint speed";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Ratatoskr.DurationMinutes.Value * 60f;
        m_speedModifier = PotionSettings.Ratatoskr.SpeedModifier.Value;
        m_runStaminaDrainModifier = PotionSettings.Ratatoskr.RunStaminaDrainModifier.Value;
        m_jumpModifier = new Vector3(0, PotionSettings.Ratatoskr.JumpModifier.Value, 0);
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
    /// Modifies the sneak stamina usage.
    /// </summary>
    /// <param name="baseStaminaUse">What the action costs without any effect.</param>
    /// <param name="staminaUse">What it costs so far; changed in place, as the game passes it on to the other effects and then uses it.</param>
    public override void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
    {
        staminaUse *= PotionSettings.Ratatoskr.SneakStaminaMultiplier.Value;
    }
}
