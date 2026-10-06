using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// Broken armour, given by White Hilt weapons that break armour (<see cref="WeaponTrait.BreaksArmor"/>): the target
/// takes more damage from every hit for a while. A new hit starts the time again.
/// </summary>
public class ArmorBreakEffect : StatusEffect
{
    /// <summary>
    /// Name of the status effect.
    /// </summary>
    public const string EffectName = "WhiteHilt_ArmorBreak";

    /// <summary>
    /// Hash of the status effect's name.
    /// </summary>
    public static readonly int Hash = EffectName.GetStableHashCode();

    private const string Section = "Gear.Weapons";

    private static ConfigEntry<float> extraDamage;
    private static ConfigEntry<float> seconds;

    /// <summary>
    /// Share of extra damage the target takes, e.g. 0.15 for 15% more.
    /// </summary>
    public static float ExtraDamage => extraDamage?.Value ?? 0f;

    /// <summary>
    /// Seconds the armour stays broken.
    /// </summary>
    public static float Seconds => seconds?.Value ?? 0f;

    /// <summary>
    /// Binds the config and registers the status effect and its English text. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        extraDamage = WhiteHiltConfig.BindAdminOnly(Section, "ArmorBreakExtraDamage", 0.15f,
            "Share of extra damage a target with broken armour takes from every hit (0.15 = 15% more), from White Hilt weapons that break armour (e.g. the Morning Star).",
            new AcceptableValueRange<float>(0f, 2f));
        seconds = WhiteHiltConfig.BindAdminOnly(Section, "ArmorBreakSeconds", 8f,
            "Seconds the armour stays broken; a new hit starts it again.", new AcceptableValueRange<float>(1f, 60f));

        Translations.AddEnglish("se_whitehilt_armorbreak", "Broken armour");
        Translations.AddEnglish("se_whitehilt_armorbreak_tooltip", "Takes more damage");
        ArmorBreakEffect effect = ScriptableObject.CreateInstance<ArmorBreakEffect>();
        effect.name = EffectName;
        effect.m_name = Translations.Token("se_whitehilt_armorbreak");
        effect.m_tooltip = Translations.Token("se_whitehilt_armorbreak_tooltip");
        effect.m_ttl = Seconds;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        m_ttl = Seconds;
        base.Setup(character);
    }

    /// <inheritdoc/>
    public override void ResetTime()
    {
        m_ttl = Seconds;
        base.ResetTime();
    }

    /// <inheritdoc/>
    public override void OnDamaged(HitData hit, Character attacker)
    {
        base.OnDamaged(hit, attacker);
        hit.m_damage.Modify(1f + ExtraDamage);
    }
}
