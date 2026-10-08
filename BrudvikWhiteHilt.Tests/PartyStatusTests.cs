using BrudvikWhiteHilt.Party;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class PartyStatusTests
{
    private static PartyStatus Sample() => new()
    {
        Name = "Kari",
        Health = 184.4f,
        MaxHealth = 250f,
        Stamina = 96f,
        MaxStamina = 110f,
        Eitr = 0f,
        MaxEitr = 0f,
        Dead = false,
        Effects = new[] { 11, -22, 33 }
    };

    [Fact]
    public void StatusReadsBackTheSame()
    {
        ZPackage package = new();
        Sample().Write(package);
        package.SetPos(0);

        PartyStatus read = PartyStatus.Read(package);

        Assert.NotNull(read);
        Assert.Equal("Kari", read.Name);
        Assert.Equal(184.4f, read.Health);
        Assert.Equal(250f, read.MaxHealth);
        Assert.Equal(96f, read.Stamina);
        Assert.Equal(110f, read.MaxStamina);
        Assert.Equal(new[] { 11, -22, 33 }, read.Effects);
        Assert.False(read.DiffersFrom(Sample()));
    }

    [Fact]
    public void BrokenOrNewerStatusIsIgnored()
    {
        ZPackage newer = new();
        newer.Write((byte)99);
        newer.SetPos(0);
        Assert.Null(PartyStatus.Read(newer));

        ZPackage cut = new();
        cut.Write((byte)1);
        cut.Write("Kari");
        cut.SetPos(0);
        Assert.Null(PartyStatus.Read(cut));
    }

    [Fact]
    public void OnlyChangesWorthSeeingAreSent()
    {
        PartyStatus sent = Sample();
        PartyStatus now = Sample();

        now.Health += 0.5f;
        Assert.False(now.DiffersFrom(sent));

        now.Health += 1f;
        Assert.True(now.DiffersFrom(sent));

        now = Sample();
        now.Effects = new[] { 11, -22 };
        Assert.True(now.DiffersFrom(sent));

        now = Sample();
        now.Dead = true;
        Assert.True(now.DiffersFrom(sent));
        Assert.True(now.DiffersFrom(null));
    }

    [Theory]
    [InlineData(184.4f, 250f, "185/250")]
    [InlineData(0.2f, 250f, "1/250")]
    [InlineData(0f, 250f, "0/250")]
    [InlineData(-3f, 110f, "0/110")]
    [InlineData(110f, 109.6f, "110/110")]
    public void AmountRoundsTheCurrentValueUp(float value, float max, string expected)
    {
        Assert.Equal(expected, PartyStatus.Amount(value, max));
    }
}
