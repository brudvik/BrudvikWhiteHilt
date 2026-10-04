using BrudvikWhiteHilt.Helpers;
using System;

namespace BrudvikWhiteHilt.Bestiary;

/// <summary>A material preparation tied to the eligible weapon held when it is applied.</summary>
public sealed class BeastCounterCoating : SE_Stats
{
    private ItemDrop.ItemData weapon;
    private int left;

    /// <summary>Stable identifier of the preparation.</summary>
    public string CounterKey;

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        base.Setup(character);
        weapon = (character as Humanoid)?.GetCurrentWeapon();
        left = BeastCounter.Get(CounterKey).Attacks;
    }

    /// <summary>Whether this weapon can take this material preparation.</summary>
    /// <param name="counter">The preparation.</param>
    /// <param name="item">The held item.</param>
    /// <returns>True only for a melee weapon with the required damage channel.</returns>
    public static bool Accepts(BeastCounter counter, ItemDrop.ItemData item)
    {
        if (counter == null || counter.IsArrow || item?.m_shared == null
            || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow
            || item.m_shared.m_attack?.m_attackType == Attack.AttackType.Projectile)
        {
            return false;
        }
        HitData.DamageTypes damage = item.m_shared.m_damages;
        return counter.DamageType switch
        {
            HitData.DamageType.Pickaxe => damage.m_pickaxe > 0f,
            HitData.DamageType.Blunt => damage.m_blunt > 0f,
            HitData.DamageType.Slash => damage.m_slash > 0f,
            _ => false
        };
    }

    /// <inheritdoc/>
    public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
    {
        BeastCounter counter = BeastCounter.Get(CounterKey);
        if (left <= 0 || hitData == null || (m_character as Humanoid)?.GetCurrentWeapon() != weapon
            || !Accepts(counter, weapon) || hitData.m_ranged || skill == Skills.SkillType.None
            || (skill == Skills.SkillType.WoodCutting && counter.DamageType != HitData.DamageType.Slash))
        {
            return;
        }
        if (hitData.m_statusEffectHash != 0)
        {
            return;
        }
        hitData.m_statusEffectHash = counter.Marker;
        left--;
    }

    /// <inheritdoc/>
    public override bool IsDone() => left <= 0 || base.IsDone();

    /// <inheritdoc/>
    public override string GetIconText() => left.ToString();

    /// <inheritdoc/>
    public override string GetTooltipString() => string.Format(Localization.instance.Localize("$whitehilt_counter_treatment"),
        Localization.instance.Localize(Translations.Token(BeastCounter.Get(CounterKey).Beast.NameKey)), left);
}