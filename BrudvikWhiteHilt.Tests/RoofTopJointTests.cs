using BrudvikWhiteHilt.Pieces.Roofs;
using UnityEngine;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

public class RoofTopJointTests
{
    public static TheoryData<RoofShape, RoofPitch> Joints()
    {
        TheoryData<RoofShape, RoofPitch> data = new();
        foreach (RoofShape shape in new[] { RoofShape.RidgeCorner, RoofShape.RidgeTee, RoofShape.RidgeCross, RoofShape.HipEnd, RoofShape.Pyramid })
        {
            foreach (RoofPitch pitch in new[] { RoofPitch.Low, RoofPitch.Medium, RoofPitch.Steep })
            {
                data.Add(shape, pitch);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Joints))]
    public void SnapPointsLieOnTheRoof(RoofShape shape, RoofPitch pitch)
    {
        foreach (Vector3 point in RoofMeshBuilder.TopJointSnapPoints(shape, pitch))
        {
            Assert.Equal(point.y, RoofMeshBuilder.Height(shape, pitch, point.x, point.z), 3);
        }
    }

    [Theory]
    [MemberData(nameof(Joints))]
    public void CornersAreLowAsARidgesAndTheCentreIsAtTheRidge(RoofShape shape, RoofPitch pitch)
    {
        float g = RoofMeshBuilder.Rise(pitch) / 2f;
        foreach (Vector2 corner in new[] { new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f) })
        {
            Assert.Equal(0f, RoofMeshBuilder.Height(shape, pitch, corner.x, corner.y), 3);
        }

        Assert.Equal(g, RoofMeshBuilder.Height(shape, pitch, 0f, 0f), 3);
    }

    // Each slope falls at the pitch, so no step between two neighbouring points may be steeper than that, and the roof
    // has no gaps or cliffs where its facets meet.
    [Theory]
    [MemberData(nameof(Joints))]
    public void TheRoofIsContinuous(RoofShape shape, RoofPitch pitch)
    {
        float g = RoofMeshBuilder.Rise(pitch) / 2f;
        const int Steps = 40;
        const float Step = 2f / Steps;
        for (int i = 0; i <= Steps; i++)
        {
            for (int j = 0; j < Steps; j++)
            {
                float x = -1f + i * Step;
                float z = -1f + j * Step;
                float here = RoofMeshBuilder.Height(shape, pitch, x, z);
                Assert.InRange(Mathf.Abs(RoofMeshBuilder.Height(shape, pitch, x, z + Step) - here), 0f, g * Step * 1.01f + 1e-4f);
                Assert.InRange(Mathf.Abs(RoofMeshBuilder.Height(shape, pitch, z, x) - RoofMeshBuilder.Height(shape, pitch, z + Step, x)), 0f, g * Step * 1.01f + 1e-4f);
            }
        }
    }

    [Theory]
    [InlineData(RoofShape.RidgeCorner, 0.5f, -0.2f, 0.5f)]
    [InlineData(RoofShape.RidgeCorner, -0.5f, 0.2f, 0.8f)]
    [InlineData(RoofShape.RidgeTee, 0.5f, 0.2f, 0.8f)]
    [InlineData(RoofShape.RidgeCross, -0.5f, -0.2f, 0.8f)]
    [InlineData(RoofShape.HipEnd, 0.6f, 0.1f, 0.4f)]
    [InlineData(RoofShape.Pyramid, 0.6f, -0.1f, 0.4f)]
    public void HipsTakeTheLowerSlopeAndValleysTheHigher(RoofShape shape, float x, float z, float height)
    {
        Assert.Equal(height, RoofMeshBuilder.Height(shape, RoofPitch.Medium, x, z), 3);
    }

    [Fact]
    public void TheRedGraustenRoofFillsInWhatTheGameLacks()
    {
        Assert.False(RoofCoverings.Has(RoofCovering.RedGrausten, RoofShape.Slope, RoofPitch.Medium));
        Assert.False(RoofCoverings.Has(RoofCovering.RedGrausten, RoofShape.Ridge, RoofPitch.Medium));
        Assert.True(RoofCoverings.Has(RoofCovering.RedGrausten, RoofShape.Slope, RoofPitch.Low));
        Assert.True(RoofCoverings.Has(RoofCovering.RedGrausten, RoofShape.Ridge, RoofPitch.Steep));
        Assert.True(RoofCoverings.Has(RoofCovering.RedGrausten, RoofShape.OuterCorner, RoofPitch.Medium));
        Assert.True(RoofCoverings.Has(RoofCovering.RedGrausten, RoofShape.SmokeHole, RoofPitch.Medium));
        Assert.True(RoofCoverings.Has(RoofCovering.RedGrausten, RoofShape.RidgeCross, RoofPitch.Medium));
        Assert.False(RoofCoverings.Has(RoofCovering.Slate, RoofShape.RidgeCross, RoofPitch.Medium));
        Assert.True(RoofCoverings.Has(RoofCovering.Slate, RoofShape.Slope, RoofPitch.Medium));
    }
}
