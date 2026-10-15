using BrudvikWhiteHilt.Clock;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Storm exposure on the local player: the meter in the icon, less stamina regeneration as it climbs, and the slowing
/// wind of the snowstorms out in the open. <see cref="StormClient"/> keeps the meter; this only shows and applies it.
/// </summary>
public class StormExposureEffect : StatusEffect
{
    private const string EffectKey = "se_whitehiltstorm";

    private static StormExposureEffect template;

    /// <summary>The registered effect, which the player gets a copy of.</summary>
    public static StormExposureEffect Template => template;

    /// <summary>
    /// Creates and registers the effect. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        template = CreateInstance<StormExposureEffect>();
        template.name = "SE_WhiteHiltStorm";
        template.m_name = Translations.Token(EffectKey);
        template.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
        template.m_ttl = 0f;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(template, fixReference: false));
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        // The icon is loaded here, as the clock's icons need the game's texture loading.
        if (m_icon == null)
        {
            m_icon = GameClock.LoadIcon("Storm");
        }

        base.Setup(character);
    }

    /// <inheritdoc/>
    public override string GetIconText()
    {
        return $"{Mathf.RoundToInt(StormClient.Exposure)}%";
    }

    /// <inheritdoc/>
    public override string GetTooltipString()
    {
        string cover = Translations.Word($"whitehilt_storm_cover_{StormClient.Cover.ToString().ToLowerInvariant()}");
        return $"{base.GetTooltipString()}\n{cover}";
    }

    /// <inheritdoc/>
    public override void ModifyStaminaRegen(ref float staminaRegen)
    {
        staminaRegen *= StormExposure.StaminaRegen(StormClient.Exposure);
    }

    /// <inheritdoc/>
    public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
    {
        speed *= StormClient.SpeedFactor;
    }
}
