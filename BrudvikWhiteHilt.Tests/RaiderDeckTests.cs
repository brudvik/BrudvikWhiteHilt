using BrudvikWhiteHilt.Patches.SeaBattles;
using BrudvikWhiteHilt.SeaBattles;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Xunit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace BrudvikWhiteHilt.Tests;

public class RaiderDeckTests
{
    private static readonly Vector3[] square =
    {
        new(0f, 1f, 0f), new(10f, 1f, 0f), new(0f, 1f, 10f), new(10f, 1f, 10f)
    };

    [Fact]
    public void SpreadStartsForwardAndThenGoesFarAway()
    {
        List<Vector3> picked = RaiderDeck.Spread(square, 2);

        // The most forward point, the one with the lowest x among equals, then the one farthest from it.
        Assert.Equal(new Vector3(0f, 1f, 10f), picked[0]);
        Assert.Equal(new Vector3(10f, 1f, 0f), picked[1]);
    }

    [Fact]
    public void SpreadBreaksTiesByTheOrderOfThePoints()
    {
        List<Vector3> picked = RaiderDeck.Spread(square, 3);

        Assert.Equal(new[] { new Vector3(0f, 1f, 10f), new Vector3(10f, 1f, 0f), new Vector3(0f, 1f, 0f) }, picked);
    }

    [Fact]
    public void SpreadIsTheSameEveryTime()
    {
        List<Vector3> grid = Grid(7, 15, 1.2f);

        Assert.Equal(RaiderDeck.Spread(grid, 9), RaiderDeck.Spread(grid, 9));
    }

    [Fact]
    public void SpreadPicksDistinctPointsAtSomeDistance()
    {
        List<Vector3> picked = RaiderDeck.Spread(Grid(7, 15, 1.2f), 8);

        Assert.Equal(8, picked.Distinct().Count());
        for (int i = 0; i < picked.Count; i++)
        {
            for (int j = i + 1; j < picked.Count; j++)
            {
                Assert.True(Vector3.Distance(picked[i], picked[j]) >= 3f, $"{picked[i]} and {picked[j]} are close");
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void SpreadOfNothingIsEmpty(int count)
    {
        Assert.Empty(RaiderDeck.Spread(square, count));
        Assert.Empty(RaiderDeck.Spread(null, 2));
    }

    [Fact]
    public void SpreadGivesEveryPointWhenThereAreNoMoreThanWanted()
    {
        Assert.Equal(square, RaiderDeck.Spread(square, 4));
        Assert.Equal(square, RaiderDeck.Spread(square, 40));
    }

    [Fact]
    public void ObstaclesClearTheDeckAroundThemAcrossNotUp()
    {
        Vector3[] points = { new(0f, 3f, 0.5f), new(0f, 1f, 2f), new(0.9f, 1f, 0f), new(1f, 1f, 0f) };

        List<Vector3> kept = RaiderDeck.RemoveNear(points, new[] { Vector3.zero }, 1f);

        // The first and third are within one metre sideways (the first is high above, which does not count); the last
        // is exactly one metre away, which is clear.
        Assert.Equal(new[] { points[1], points[3] }, kept);
    }

    [Fact]
    public void NoObstaclesKeepEverything()
    {
        Assert.Equal(square, RaiderDeck.RemoveNear(square, new Vector3[0], 1f));
    }

    [Fact]
    public void TheNearestPointIsFoundInThreeDimensions()
    {
        Vector3[] points = { new(0f, 0f, 0f), new(5f, 0f, 0f), new(5f, 3f, 0f) };

        Assert.Equal(1, RaiderDeck.Nearest(points, new Vector3(4f, 0.5f, 0f)));
        Assert.Equal(2, RaiderDeck.Nearest(points, new Vector3(5f, 2.5f, 0f)));
        Assert.Equal(0, RaiderDeck.Nearest(points, new Vector3(-9f, 0f, 0f)));
    }

    [Fact]
    public void EquallyNearPointsGiveTheFirst()
    {
        Assert.Equal(0, RaiderDeck.Nearest(new[] { new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f) }, Vector3.zero));
    }

    [Fact]
    public void ArchersGetThePostsFarthestFromTheMiddle()
    {
        Vector3[] posts = { new(0f, 1f, 0f), new(-2f, 1f, 3f), new(2f, 1f, -3f), new(1f, 1f, 5f) };

        Assert.Equal(new[] { 1, 2 }, RaiderDeck.OuterIndexes(posts, 2));
        Assert.Equal(new[] { 1, 2, 3, 0 }, RaiderDeck.OuterIndexes(posts, 9));
        Assert.Empty(RaiderDeck.OuterIndexes(posts, 0));
    }

    [Theory]
    [InlineData(0f, 0f, 0f, true)]
    [InlineData(4.4f, 0f, 8.9f, true)]
    [InlineData(4.6f, 0f, 0f, false)]
    [InlineData(0f, 0f, 9.1f, false)]
    [InlineData(-4.5f, 0f, -9f, true)]
    public void TheDeckReachesHalfTheSizeAndTheMarginBeyondTheCentre(float x, float y, float z, bool inside)
    {
        Assert.Equal(inside, RaiderDeck.InsideDeck(new Vector3(x, y, z), Vector3.zero, new Vector3(8f, 1f, 17f), 0.5f));
    }

    [Fact]
    public void TheDeckFollowsItsCentre()
    {
        Assert.True(RaiderDeck.InsideDeck(new Vector3(0f, 0f, 20f), new Vector3(0f, 0f, 20f), new Vector3(2f, 1f, 2f), 0f));
        Assert.False(RaiderDeck.InsideDeck(Vector3.zero, new Vector3(0f, 0f, 20f), new Vector3(2f, 1f, 2f), 0f));
    }

    private static List<Vector3> Grid(int across, int along, float step)
    {
        List<Vector3> points = new();
        for (int x = 0; x < across; x++)
        {
            for (int z = 0; z < along; z++)
            {
                points.Add(new Vector3(x * step - across * step / 2f, 1f, z * step - along * step / 2f));
            }
        }

        return points;
    }
}

public class SailingShipTests
{
    [Fact]
    public void AnEmptyListThatSailsCountsOnePlayer()
    {
        List<int> sailor = new();
        List<int> parked = new();
        SailingShips.Add(sailor);
        try
        {
            Assert.Equal(1, SailingShips.Count(sailor, 0));
            // Lists are told apart by reference: another empty list is not a sailing ship's.
            Assert.Equal(0, SailingShips.Count(parked, 0));
        }
        finally
        {
            SailingShips.Remove(sailor);
        }

        Assert.Equal(0, SailingShips.Count(sailor, 0));
    }

    [Fact]
    public void PlayersAboardAreCountedAsTheyAre()
    {
        List<int> sailor = new();
        SailingShips.Add(sailor);
        try
        {
            Assert.Equal(3, SailingShips.Count(sailor, 3));
        }
        finally
        {
            SailingShips.Remove(sailor);
        }
    }

    [Fact]
    public void AShipThatIsRemovedIsNotFoundAgain()
    {
        List<int> sailor = new();
        SailingShips.Add(sailor);
        Assert.True(SailingShips.Contains(sailor));

        SailingShips.Remove(sailor);

        Assert.False(SailingShips.Contains(sailor));
    }
}

public class RaiderShipPatchTests
{
    // Stand-ins for the ship's list field and the list's count: the game's own types cannot be loaded here.
    private sealed class Holder
    {
        public List<int> Players = new();

        public static int Replacement(List<int> list)
        {
            return list.Count;
        }
    }

    private static readonly FieldInfo players = typeof(Holder).GetField(nameof(Holder.Players));
    private static readonly MethodInfo count = typeof(List<int>).GetProperty(nameof(List<int>.Count)).GetGetMethod();
    private static readonly MethodInfo replacement = typeof(Holder).GetMethod(nameof(Holder.Replacement));

    [Fact]
    public void OnlyTheCountOfTheShipsPlayersIsReplaced()
    {
        List<CodeInstruction> code = new()
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, players),
            new CodeInstruction(OpCodes.Callvirt, count),
            new CodeInstruction(OpCodes.Pop),
            // Another list's count, with no read of the ship's list before it, stays as it is.
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Callvirt, count),
            new CodeInstruction(OpCodes.Pop)
        };

        List<CodeInstruction> patched = RaiderShipPatches.ReplaceCount(code, players, count, replacement, out int replaced);

        Assert.Equal(1, replaced);
        Assert.Equal(OpCodes.Call, patched[2].opcode);
        Assert.Equal(replacement, patched[2].operand);
        Assert.Equal(OpCodes.Callvirt, patched[5].opcode);
        Assert.Equal(count, patched[5].operand);
    }

    [Fact]
    public void EveryReadIsReplaced()
    {
        List<CodeInstruction> code = new();
        for (int i = 0; i < 3; i++)
        {
            code.Add(new CodeInstruction(OpCodes.Ldarg_0));
            code.Add(new CodeInstruction(OpCodes.Ldfld, players));
            code.Add(new CodeInstruction(OpCodes.Callvirt, count));
        }

        RaiderShipPatches.ReplaceCount(code, players, count, replacement, out int replaced);

        Assert.Equal(3, replaced);
    }

    [Fact]
    public void CodeWithoutTheReadIsLeftAlone()
    {
        List<CodeInstruction> code = new() { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ret) };

        List<CodeInstruction> patched = RaiderShipPatches.ReplaceCount(code, players, count, replacement, out int replaced);

        Assert.Equal(0, replaced);
        Assert.Equal(2, patched.Count);
    }

    [Fact]
    public void TheGamesPhysicsReadsThePlayerCountWhereThePatchLooks()
    {
        // The game's own code, read without loading it. If a game update changes it, this fails; the patch itself would
        // only log a warning and the Nail Ship would stop at sea.
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assembly_valheim.dll");
        using AssemblyDefinition game = AssemblyDefinition.ReadAssembly(path);
        MethodDefinition physics = game.MainModule.GetType("Ship").Methods.Single(method => method.Name == "CustomFixedUpdate");
        Instruction[] code = physics.Body.Instructions.ToArray();

        int reads = 0;
        for (int i = 1; i < code.Length; i++)
        {
            if (code[i].OpCode.Code is Code.Callvirt or Code.Call && code[i].Operand is MethodReference called && called.Name == "get_Count"
                && called.DeclaringType.Name == "List`1" && code[i - 1].OpCode.Code == Code.Ldfld
                && code[i - 1].Operand is FieldReference field && field.Name == "m_players")
            {
                reads++;
            }
        }

        // Once to stop the ship that has no players, once to brake it.
        Assert.Equal(2, reads);
    }
}
