using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// Norse names for the household, picked from a seed so every machine that knows the seed gets the same name. The
/// traders' names (Asbjørn, Gunnar, Ragnhild, Hallvard, Sigrid, Ylva, Brand, Frøydis) are left out, so a farmhand is
/// never mistaken for a trader.
/// </summary>
public static class HouseholdNames
{
    /// <summary>The men's names.</summary>
    public static readonly string[] Men =
    {
        "Arne", "Ivar", "Torstein", "Eirik", "Bjørn", "Halvard", "Ketil", "Leif", "Odd", "Ragnar", "Sigurd", "Steinar",
        "Svein", "Trygve", "Ulv", "Vidar", "Asgeir", "Einar", "Frode", "Geir", "Grim", "Hauk", "Kolbein", "Orm", "Rolv",
        "Toke", "Torgils", "Åsmund",
    };

    /// <summary>The women's names.</summary>
    public static readonly string[] Women =
    {
        "Tora", "Gyda", "Hild", "Astrid", "Gudrun", "Ingrid", "Solveig", "Torunn", "Åse", "Bergljot", "Dagny",
        "Gunnhild", "Helga", "Inga", "Liv", "Ragna", "Signy", "Tove", "Unn", "Vigdis", "Aud", "Borghild", "Eldrid",
        "Halldis", "Jorunn", "Ranveig",
    };

    /// <summary>
    /// Picks a name not yet taken: the seed's own name, or the next free one after it. When every name is taken, the
    /// seed's own name is given again.
    /// </summary>
    /// <param name="seed">The seed; any number, negative too.</param>
    /// <param name="female">Whether to pick a woman's name.</param>
    /// <param name="taken">Names already used on the farm.</param>
    /// <returns>The name.</returns>
    public static string Pick(int seed, bool female, IEnumerable<string> taken)
    {
        string[] names = female ? Women : Men;
        HashSet<string> used = new(taken ?? Enumerable.Empty<string>());
        int start = (int)((uint)seed % (uint)names.Length);
        for (int step = 0; step < names.Length; step++)
        {
            string name = names[(start + step) % names.Length];
            if (!used.Contains(name))
            {
                return name;
            }
        }

        return names[start];
    }
}
