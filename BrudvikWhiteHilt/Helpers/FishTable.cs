using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Which vanilla fish live in which waters, for the nets that catch them without a rod.
/// </summary>
public static class FishTable
{
    // Fish1 perch, Fish2 pike, Fish3 tuna, Fish5 trollfish, Fish6 giant herring, Fish7 grouper, Fish8 coral cod,
    // Fish9 anglerfish, Fish10 northern salmon, Fish11 magmafish, Fish12 pufferfish.
    private static readonly Dictionary<Heightmap.Biome, (string Prefab, float Weight)[]> fishByBiome = new()
    {
        [Heightmap.Biome.Meadows] = new[] { ("Fish1", 0.7f), ("Fish2", 0.3f) },
        [Heightmap.Biome.BlackForest] = new[] { ("Fish2", 0.6f), ("Fish1", 0.25f), ("Fish5", 0.15f) },
        [Heightmap.Biome.Swamp] = new[] { ("Fish6", 0.7f), ("Fish1", 0.3f) },
        [Heightmap.Biome.Mountain] = new[] { ("Fish1", 1f) },
        [Heightmap.Biome.Plains] = new[] { ("Fish7", 0.7f), ("Fish1", 0.3f) },
        [Heightmap.Biome.Ocean] = new[] { ("Fish3", 0.5f), ("Fish8", 0.35f), ("Fish12", 0.15f) },
        [Heightmap.Biome.Mistlands] = new[] { ("Fish9", 0.6f), ("Fish12", 0.4f) },
        [Heightmap.Biome.DeepNorth] = new[] { ("Fish10", 1f) },
        [Heightmap.Biome.AshLands] = new[] { ("Fish11", 1f) }
    };

    /// <summary>
    /// Picks a fish that lives in the biome, weighted by how common it is there. Unknown biomes count as ocean.
    /// </summary>
    /// <param name="biome">Biome of the water.</param>
    /// <returns>Prefab name of the fish.</returns>
    public static string Pick(Heightmap.Biome biome)
    {
        if (!fishByBiome.TryGetValue(biome, out (string Prefab, float Weight)[] fish))
        {
            fish = fishByBiome[Heightmap.Biome.Ocean];
        }

        float roll = Random.value * fish.Sum(entry => entry.Weight);
        foreach ((string prefab, float weight) in fish)
        {
            roll -= weight;
            if (roll <= 0f)
            {
                return prefab;
            }
        }

        return fish[fish.Length - 1].Prefab;
    }
}
