namespace BrudvikWhiteHilt.Items.Coatings;

/// <summary>
/// A whetstone edge or an oil on the weapon: extra damage for a number of hits, then it wears off.
/// </summary>
public class WeaponCoatingEffect : SE_Stats
{
    /// <summary>Hits it lasts.</summary>
    public int Charges = 30;

    /// <summary>Extra slash and pierce damage, as a share (whetstone).</summary>
    public float EdgeBonus;

    /// <summary>Kind of damage added as a share of the physical damage (oils), or <see cref="HitData.DamageType.Physical"/> for none.</summary>
    public HitData.DamageType AddedType = HitData.DamageType.Physical;

    /// <summary>Share of the physical damage added as <see cref="AddedType"/>.</summary>
    public float AddedShare;

    private int left;

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        base.Setup(character);
        left = Charges;
    }

    /// <inheritdoc/>
    public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
    {
        if (left <= 0 || skill == Skills.SkillType.WoodCutting || skill == Skills.SkillType.Pickaxes || skill == Skills.SkillType.None)
        {
            return;
        }

        HitData.DamageTypes damage = hitData.m_damage;
        float physical = damage.m_blunt + damage.m_slash + damage.m_pierce;
        damage.m_slash *= 1f + EdgeBonus;
        damage.m_pierce *= 1f + EdgeBonus;
        float added = physical * AddedShare;
        switch (AddedType)
        {
            case HitData.DamageType.Spirit:
                damage.m_spirit += added;
                break;
            case HitData.DamageType.Poison:
                damage.m_poison += added;
                break;
            case HitData.DamageType.Fire:
                damage.m_fire += added;
                break;
        }

        hitData.m_damage = damage;
        left--;
        if (left <= 0)
        {
            m_time = m_ttl;
        }
    }

    /// <inheritdoc/>
    public override string GetIconText()
    {
        return left.ToString();
    }
}
