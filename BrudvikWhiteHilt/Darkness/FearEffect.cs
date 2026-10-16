using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// Fear of the dark on the local player: the meter in the icon, less stamina regeneration from
/// <see cref="DarknessClient.Afraid"/> and failing legs from <see cref="DarknessClient.Panic"/>.
/// <see cref="DarknessClient"/> keeps the meter; this only shows and applies it.
/// </summary>
public class FearEffect : StatusEffect
{
    private const string EffectKey = "se_whitehiltfear";
    private const string IconTrophy = "TrophyGhost";
    private const float AfraidStamina = 0.5f;
    private const float PanicSpeed = 0.8f;

    private static FearEffect template;

    /// <summary>The registered effect, which the player gets a copy of.</summary>
    public static FearEffect Template => template;

    /// <summary>
    /// Creates and registers the effect. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        template = CreateInstance<FearEffect>();
        template.name = "SE_WhiteHiltFear";
        template.m_name = Translations.Token(EffectKey);
        template.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
        template.m_ttl = 0f;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(template, fixReference: false));
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        // The ghost's trophy, once the game's items are there.
        if (m_icon == null && ObjectDB.instance != null)
        {
            m_icon = ObjectDB.instance.GetItemPrefab(IconTrophy)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        }

        base.Setup(character);
    }

    /// <inheritdoc/>
    public override string GetIconText()
    {
        return $"{Mathf.RoundToInt(DarknessClient.Fear)}%";
    }

    /// <inheritdoc/>
    public override string GetTooltipString()
    {
        return $"{base.GetTooltipString()}\n{Translations.Word(DarknessClient.Lit ? "whitehilt_dark_lit" : "whitehilt_dark_unlit")}";
    }

    /// <inheritdoc/>
    public override void ModifyStaminaRegen(ref float staminaRegen)
    {
        if (DarknessClient.Fear >= DarknessClient.Afraid)
        {
            staminaRegen *= AfraidStamina;
        }
    }

    /// <inheritdoc/>
    public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
    {
        if (DarknessClient.Fear >= DarknessClient.Panic)
        {
            speed *= PanicSpeed;
        }
    }
}
