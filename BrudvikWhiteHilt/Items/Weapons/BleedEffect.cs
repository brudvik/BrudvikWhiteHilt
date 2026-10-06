using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// Bleeding, given by White Hilt weapons that bleed (<see cref="WeaponTrait.Bleeds"/>): damage every second for a
/// while, which no armour or resistance lessens and which does not stagger. A new hit starts the time again. It grows
/// with the quality of the weapon that struck.
/// </summary>
public class BleedEffect : StatusEffect
{
    /// <summary>
    /// Name of the status effect.
    /// </summary>
    public const string EffectName = "WhiteHilt_Bleed";

    /// <summary>
    /// Hash of the status effect's name.
    /// </summary>
    public static readonly int Hash = EffectName.GetStableHashCode();

    private const string Section = "Gear.Weapons";

    private static ConfigEntry<float> damagePerSecond;
    private static ConfigEntry<float> damagePerLevel;
    private static ConfigEntry<float> seconds;

    private float tickTimer;
    private int itemLevel = 1;

    /// <summary>
    /// Damage per second of a bleeding from a weapon of quality 1.
    /// </summary>
    public static float DamagePerSecond => damagePerSecond?.Value ?? 0f;

    /// <summary>
    /// Seconds a bleeding lasts.
    /// </summary>
    public static float Seconds => seconds?.Value ?? 0f;

    /// <summary>
    /// Damage per second of a bleeding from a weapon of a quality.
    /// </summary>
    /// <param name="quality">The weapon's quality, from 1.</param>
    /// <returns>Damage per second.</returns>
    public static float DamageAtQuality(int quality) => DamagePerSecond + (damagePerLevel?.Value ?? 0f) * Mathf.Max(0, quality - 1);

    /// <summary>
    /// Binds the config and registers the status effect and its English text. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        damagePerSecond = WhiteHiltConfig.BindAdminOnly(Section, "BleedDamagePerSecond", 4f,
            "Damage per second of the bleeding from a White Hilt weapon that bleeds (e.g. the Seax), at quality 1. Armour and resistances do not lessen it.",
            new AcceptableValueRange<float>(0f, 100f));
        damagePerLevel = WhiteHiltConfig.BindAdminOnly(Section, "BleedDamagePerLevel", 1f,
            "Bleeding damage per second added per quality level of the weapon above 1.", new AcceptableValueRange<float>(0f, 50f));
        seconds = WhiteHiltConfig.BindAdminOnly(Section, "BleedSeconds", 6f,
            "Seconds a bleeding lasts; a new hit starts it again.", new AcceptableValueRange<float>(1f, 60f));

        Translations.AddEnglish("se_whitehilt_bleed", "Bleeding");
        Translations.AddEnglish("se_whitehilt_bleed_tooltip", "Losing health every second");
        BleedEffect bleed = ScriptableObject.CreateInstance<BleedEffect>();
        bleed.name = EffectName;
        bleed.m_name = Translations.Token("se_whitehilt_bleed");
        bleed.m_tooltip = Translations.Token("se_whitehilt_bleed_tooltip");
        bleed.m_ttl = Seconds;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(bleed, fixReference: false));
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        m_ttl = Seconds;
        base.Setup(character);
    }

    /// <inheritdoc/>
    public override void SetLevel(int itemLevel, float skillLevel)
    {
        base.SetLevel(itemLevel, skillLevel);
        this.itemLevel = Mathf.Max(1, itemLevel);
    }

    /// <inheritdoc/>
    public override void ResetTime()
    {
        m_ttl = Seconds;
        base.ResetTime();
    }

    /// <inheritdoc/>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        tickTimer += dt;
        // Only the owner deals the damage, or every machine that sees the target would.
        if (tickTimer < 1f || m_character == null || m_character.IsDead() || !m_character.IsOwner())
        {
            return;
        }

        tickTimer -= 1f;
        HitData hit = new() { m_point = m_character.GetCenterPoint(), m_hitType = HitData.HitType.Poisoned };
        hit.m_damage.m_damage = DamageAtQuality(itemLevel);
        m_character.Damage(hit);
    }
}
