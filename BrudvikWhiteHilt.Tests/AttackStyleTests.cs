using BrudvikWhiteHilt.Items.Weapons.Styles;
using System;
using System.Linq;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class AttackStyleTests
{
    [Fact]
    public void EveryStyleHasItsOwnTrigger()
    {
        AttackStyle[] styles = (AttackStyle[])Enum.GetValues(typeof(AttackStyle));
        Assert.Equal(styles.Length, styles.Select(AttackStyles.Trigger).Distinct().Count());
        foreach (AttackStyle style in styles)
        {
            Assert.True(AttackStyles.TryFromTrigger(AttackStyles.Trigger(style), out AttackStyle back));
            Assert.Equal(style, back);
        }
    }

    [Theory]
    [InlineData("swing_longsword")]
    [InlineData("swing_axe")]
    [InlineData("knife_stab")]
    [InlineData("spear_poke")]
    [InlineData("battleaxe_attack")]
    [InlineData("greatsword")]
    [InlineData("swing_sledge")]
    [InlineData("atgeir_attack")]
    public void ADefaultPoolStartsWithItsOwnStyleAndKeepsTheGrip(string trigger)
    {
        AttackStyle[] pool = AttackStyles.DefaultPool(trigger);
        Assert.True(AttackStyles.TryFromTrigger(trigger, out AttackStyle native));
        Assert.Equal(native, pool[0]);
        Assert.All(pool, style => Assert.Equal(AttackStyles.IsTwoHanded(native), AttackStyles.IsTwoHanded(style)));
    }

    [Theory]
    [InlineData("bow_fire")]
    [InlineData("crossbow_fire")]
    [InlineData("staff_fireball")]
    public void RangedAndMagicAttacksDoNotVary(string trigger)
    {
        Assert.Null(AttackStyles.DefaultPool(trigger));
    }

    [Theory]
    [InlineData(AttackStyle.Slash, AttackStyle.Stab, 2f)]
    [InlineData(AttackStyle.Slam, AttackStyle.Cleave, 2f)]
    [InlineData(AttackStyle.Stab, AttackStyle.Slash, 1f)]
    [InlineData(AttackStyle.Lunge, AttackStyle.Stab, 2f)]
    [InlineData(AttackStyle.Slash, AttackStyle.Rune, 2f)]
    public void EvenedOutStylesDealTheSameDamagePerSecond(AttackStyle native, AttackStyle style, float finisher)
    {
        float factor = AttackStyles.DamageFactor(native, style, finisher, 1f);
        Assert.Equal(DamagePerSecond(native, finisher), DamagePerSecond(style, finisher) * factor, 3);
    }

    [Fact]
    public void WithoutEvenOutTheDamageAndStaminaPerHitStay()
    {
        Assert.Equal(1f, AttackStyles.DamageFactor(AttackStyle.Slash, AttackStyle.Slam, 2f, 0f), 5);
        Assert.Equal(1f, AttackStyles.StaminaFactor(AttackStyle.Slash, AttackStyle.Slam, 0f), 5);
    }

    [Fact]
    public void AWeaponInItsOwnStyleIsUnchanged()
    {
        Assert.Equal(1f, AttackStyles.DamageFactor(AttackStyle.Cleave, AttackStyle.Cleave, 2f, 1f), 5);
        Assert.Equal(1f, AttackStyles.StaminaFactor(AttackStyle.Cleave, AttackStyle.Cleave, 1f), 5);
    }

    [Fact]
    public void ASlowerStyleCostsMoreStaminaPerHit()
    {
        Assert.True(AttackStyles.StaminaFactor(AttackStyle.Slash, AttackStyle.Slam, 1f) > 1f);
        Assert.True(AttackStyles.StaminaFactor(AttackStyle.Slash, AttackStyle.Stab, 1f) < 1f);
    }

    [Fact]
    public void TheSignatureTakesItsShareAndTheOthersSplitTheRest()
    {
        AttackStyle[] styles = { AttackStyle.Slash, AttackStyle.Chop, AttackStyle.Stab };
        Assert.Equal(AttackStyle.Slash, AttackStyles.Pick(styles, 0.5f, 0.49f));
        Assert.Equal(AttackStyle.Chop, AttackStyles.Pick(styles, 0.5f, 0.5f));
        Assert.Equal(AttackStyle.Chop, AttackStyles.Pick(styles, 0.5f, 0.74f));
        Assert.Equal(AttackStyle.Stab, AttackStyles.Pick(styles, 0.5f, 0.76f));
        Assert.Equal(AttackStyle.Stab, AttackStyles.Pick(styles, 0.5f, 0.99999f));
        Assert.Equal(AttackStyle.Slash, AttackStyles.Pick(styles, 1f, 0.99f));
        Assert.Equal(AttackStyle.Chop, AttackStyles.Pick(styles, 0f, 0f));
    }

    [Fact]
    public void ASingleStyleIsAlwaysPicked()
    {
        Assert.Equal(AttackStyle.Lunge, AttackStyles.Pick(new[] { AttackStyle.Lunge }, 0f, 0.9f));
    }

    [Fact]
    public void TheRuneSwordCutsAreACombinationOfThree()
    {
        Assert.Equal(3, AttackStyles.ChainLevels(AttackStyle.Rune));
        Assert.Equal(AttackStyles.RuneTrigger, AttackStyles.Trigger(AttackStyle.Rune));
        Assert.False(AttackStyles.IsTwoHanded(AttackStyle.Rune));
    }

    private static float DamagePerSecond(AttackStyle style, float finisher)
    {
        int hits = AttackStyles.ChainLevels(style);
        float averageHit = hits > 1 ? (hits - 1 + finisher) / hits : 1f;
        return averageHit / AttackStyles.SecondsPerHit(style);
    }
}
