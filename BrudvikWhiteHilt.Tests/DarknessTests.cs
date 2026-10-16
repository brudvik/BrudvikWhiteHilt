using BrudvikWhiteHilt.Darkness;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// The creeping darkness: when it comes below ground, how dark it is, and the fear it brings (docs/darkness.md).
/// </summary>
public class DarknessTests
{
    // The defaults: 3 minutes calm, gaps from 4 down to 1.5 minutes over 10 minutes, 15 to 45 seconds of black.
    private static readonly DarknessSchedule.Timing timing = new(180f, 240f, 90f, 600f, 15f, 45f);

    /// <summary>The first darkness flickers in after the calm, give or take a fifth, and not before.</summary>
    [Fact]
    public void TheCalmComesFirst()
    {
        int seed = DarknessSchedule.Seed("1200:-340");
        double first = DarknessSchedule.FirstDarkness(seed, timing);
        Assert.InRange(first, 144.0, 216.0);
        Assert.Equal(DarkPhase.Calm, DarknessSchedule.At(first - 1.0, seed, timing, out _, out float left));
        Assert.Equal(1f, left, 3);
        Assert.Equal(DarkPhase.Flicker, DarknessSchedule.At(first + 1.0, seed, timing, out float into, out float length));
        Assert.Equal(1f, into, 3);
        Assert.Equal(DarknessSchedule.FlickerSeconds, length, 3);
    }

    /// <summary>A darkness flickers, fades, goes black and returns, and then it is calm again.</summary>
    [Fact]
    public void ADarknessRunsItsCourse()
    {
        int seed = DarknessSchedule.Seed("cave");
        double first = DarknessSchedule.FirstDarkness(seed, timing);
        double fading = first + DarknessSchedule.FlickerSeconds + 0.5;
        double black = fading + DarknessSchedule.FadeSeconds;
        Assert.Equal(DarkPhase.Fading, DarknessSchedule.At(fading, seed, timing, out _, out _));
        Assert.Equal(DarkPhase.Black, DarknessSchedule.At(black, seed, timing, out _, out float blackLength));
        Assert.InRange(blackLength, 12f, 18f);
        double returning = black - 0.5 + blackLength + 0.5;
        Assert.Equal(DarkPhase.Returning, DarknessSchedule.At(returning, seed, timing, out _, out _));
        Assert.Equal(DarkPhase.Calm, DarknessSchedule.At(returning + DarknessSchedule.ReturnSeconds + 1.0, seed, timing, out _, out _));
    }

    /// <summary>The longer a place has been in use, the more often the darkness comes and the longer it stays black.</summary>
    [Fact]
    public void TheDarknessCreepsIn()
    {
        int seed = DarknessSchedule.Seed("crypt");
        double first = DarknessSchedule.FirstDarkness(seed, timing);
        int earlyEvents = Count(seed, first - 1.0, first + 599.0, out _);
        Count(seed, first - 1.0, first + 40.0, out float earlyBlack);
        int lateEvents = Count(seed, 1800.0, 2400.0, out float lateBlack);
        Assert.True(lateEvents > earlyEvents, $"{lateEvents} later against {earlyEvents} early");
        Assert.True(lateBlack > earlyBlack * 1.8f, $"{lateBlack} later against {earlyBlack} early");
    }

    // Darknesses that start between two times, and the longest black among them.
    private static int Count(int seed, double from, double to, out float longestBlack)
    {
        int events = 0;
        longestBlack = 0f;
        DarkPhase was = DarknessSchedule.At(from, seed, timing, out _, out _);
        for (double at = from; at < to; at += 0.5)
        {
            DarkPhase phase = DarknessSchedule.At(at, seed, timing, out _, out float length);
            if (phase == DarkPhase.Flicker && was != DarkPhase.Flicker)
            {
                events++;
            }

            if (phase == DarkPhase.Black)
            {
                longestBlack = System.Math.Max(longestBlack, length);
            }

            was = phase;
        }

        return events;
    }

    /// <summary>Every machine works out the same darkness for the same place; another place has its own.</summary>
    [Fact]
    public void EveryoneInAPlaceHasTheSameDarkness()
    {
        Assert.Equal(DarknessSchedule.Seed("812:-2044"), DarknessSchedule.Seed("812:-2044"));
        Assert.NotEqual(DarknessSchedule.Seed("812:-2044"), DarknessSchedule.Seed("813:-2044"));
        Assert.NotEqual(DarknessSchedule.FirstDarkness(DarknessSchedule.Seed("a"), timing), DarknessSchedule.FirstDarkness(DarknessSchedule.Seed("b"), timing));
    }

    /// <summary>It fades from half to black, stays black, and the light comes back evenly; calm is light.</summary>
    [Theory]
    [InlineData((int)DarkPhase.Calm, 0f, 10f, 0f)]
    [InlineData((int)DarkPhase.Fading, 0f, 5f, 0.5f)]
    [InlineData((int)DarkPhase.Fading, 5f, 5f, 1f)]
    [InlineData((int)DarkPhase.Black, 7f, 20f, 1f)]
    [InlineData((int)DarkPhase.Returning, 1.5f, 3f, 0.5f)]
    public void HowDarkItIs(int phase, float into, float length, float level)
    {
        Assert.Equal(level, DarknessSchedule.Level((DarkPhase)phase, into, length, 0f), 3);
    }

    /// <summary>The sounds fall away while the lights flicker, stay away in the dark and come back with the light.</summary>
    [Theory]
    [InlineData((int)DarkPhase.Calm, 0f, 10f, 0f)]
    [InlineData((int)DarkPhase.Flicker, 4f, 8f, 0.5f)]
    [InlineData((int)DarkPhase.Fading, 1f, 5f, 1f)]
    [InlineData((int)DarkPhase.Black, 10f, 20f, 1f)]
    [InlineData((int)DarkPhase.Returning, 2.25f, 3f, 0.25f)]
    public void SilenceComesBeforeTheDark(int phase, float into, float length, float silence)
    {
        Assert.Equal(silence, DarknessSchedule.Silence((DarkPhase)phase, into, length), 3);
    }

    /// <summary>The dark creeps in from the first darkness to its worst over the ramp, and no further.</summary>
    [Theory]
    [InlineData(100.0, 0f)]
    [InlineData(180.0, 0f)]
    [InlineData(480.0, 0.5f)]
    [InlineData(780.0, 1f)]
    [InlineData(5000.0, 1f)]
    public void TheDarkCreepsIn(double elapsed, float creep)
    {
        Assert.Equal(creep, DarknessSchedule.Creep(elapsed, timing), 3);
    }

    /// <summary>The flicker dips, but never all the way to black.</summary>
    [Fact]
    public void TheFlickerDipsButIsNotBlack()
    {
        for (float time = 0f; time < 20f; time += 0.05f)
        {
            Assert.InRange(DarknessSchedule.Level(DarkPhase.Flicker, 7.9f, 8f, time), 0f, 0.9f);
        }
    }

    /// <summary>
    /// Fear fills in the black without a light, holds while the dark comes and goes, falls once the place is lit and
    /// twice as fast with a light of your own.
    /// </summary>
    [Fact]
    public void FearGrowsInTheDarkAndFallsInTheLight()
    {
        Assert.Equal(40f, DarknessClient.StepFear(0f, 30f, true, false, false, 75f), 3);
        Assert.Equal(100f, DarknessClient.StepFear(90f, 30f, true, false, false, 75f), 3);
        Assert.Equal(20f, DarknessClient.StepFear(40f, 7.5f, true, false, true, 75f), 3);
        Assert.Equal(40f, DarknessClient.StepFear(40f, 7.5f, false, false, false, 75f), 3);
        Assert.Equal(20f, DarknessClient.StepFear(40f, 15f, false, true, false, 75f), 3);
        Assert.Equal(0f, DarknessClient.StepFear(10f, 60f, false, true, false, 75f), 3);
    }

    /// <summary>Afraid from 25, terrified from 50, panic from 75, dying at the top.</summary>
    [Theory]
    [InlineData(0f, 0)]
    [InlineData(25f, 1)]
    [InlineData(50f, 2)]
    [InlineData(75f, 3)]
    [InlineData(100f, 4)]
    public void FearHasItsStages(float fear, int stage)
    {
        Assert.Equal(stage, DarknessClient.Stage(fear));
    }

    /// <summary>A place keeps its start while someone has been there in the last five minutes, else starts over.</summary>
    [Theory]
    [InlineData(true, 100.0, 350.0, 600.0, 100.0)]
    [InlineData(true, 100.0, 250.0, 600.0, 600.0)]
    [InlineData(false, 0.0, 0.0, 600.0, 600.0)]
    public void APlaceForgetsWhenEmpty(bool known, double start, double lastSeen, double now, double expected)
    {
        Assert.Equal(expected, DarknessService.StartFor(known, start, lastSeen, now), 3);
    }
}
