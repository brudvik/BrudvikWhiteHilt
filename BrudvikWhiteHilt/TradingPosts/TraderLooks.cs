using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The look of each biome's trader as agreed in the preview pictures (AssetSource/Preview/build_traders.py writes
/// traders.json from the same table it draws them from): name, body, hair, beard, colours and the vanilla clothes copied.
/// </summary>
[Serializable]
public class TraderLooks
{
    private const string ResourceName = "BrudvikWhiteHilt.Traders.json";

    private static TraderLooks loaded;

    /// <summary>Where the file comes from.</summary>
    public string note;

    /// <summary>One look per biome.</summary>
    public TraderLook[] traders;

    /// <summary>
    /// Every look, loaded once from the embedded file.
    /// </summary>
    public static TraderLook[] All => Load().traders;

    /// <summary>
    /// The look of a biome's trader; the Meadows' for a biome without one.
    /// </summary>
    /// <param name="biome">The biome.</param>
    /// <returns>The look.</returns>
    public static TraderLook Get(Heightmap.Biome biome)
    {
        string name = biome.ToString();
        return All.FirstOrDefault(look => string.Equals(look.biome, name, StringComparison.OrdinalIgnoreCase)) ?? All[0];
    }

    private static TraderLooks Load()
    {
        if (loaded != null)
        {
            return loaded;
        }

        using Stream stream = typeof(TraderLooks).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
        using StreamReader reader = new(stream);
        loaded = SimpleJson.SimpleJson.DeserializeObject<TraderLooks>(reader.ReadToEnd());
        if (loaded?.traders == null || loaded.traders.Length == 0)
        {
            throw new InvalidOperationException($"Embedded resource '{ResourceName}' has no traders.");
        }

        return loaded;
    }
}

/// <summary>
/// One biome's trader.
/// </summary>
[Serializable]
public class TraderLook
{
    /// <summary>The biome, as <see cref="Heightmap.Biome"/> names it (case aside).</summary>
    public string biome;

    /// <summary>The trader's name.</summary>
    public string name;

    /// <summary>The female body; else the male.</summary>
    public bool female;

    /// <summary>Hair prefab from the character creator.</summary>
    public string hair;

    /// <summary>Beard prefab, or empty for none.</summary>
    public string beard;

    /// <summary>Hair and beard colour.</summary>
    public float[] hairColour;

    /// <summary>The tunic's cloth colour.</summary>
    public float[] main;

    /// <summary>The trim's colour.</summary>
    public float[] accent;

    /// <summary>The trousers' colour.</summary>
    public float[] legs;

    /// <summary>The vanilla tunic copied.</summary>
    public string tunic;

    /// <summary>The vanilla trousers copied.</summary>
    public string trousers;

    /// <summary>The vanilla cape copied, or empty for none.</summary>
    public string cape;

    /// <summary>The cape's colour.</summary>
    public float[] capeColour;

    /// <summary>Prefab name of this trader's tunic.</summary>
    public string TunicPrefab => $"WhiteHilt_TraderTunic_{biome}";

    /// <summary>Prefab name of this trader's trousers.</summary>
    public string TrousersPrefab => $"WhiteHilt_TraderTrousers_{biome}";

    /// <summary>Prefab name of this trader's cape, or null for none.</summary>
    public string CapePrefab => string.IsNullOrEmpty(cape) ? null : $"WhiteHilt_TraderCape_{biome}";

    /// <summary>Translation key of the trader's name with the land they trade in.</summary>
    public string NameKey => $"npc_whitehilt_trader_{biome.ToLowerInvariant()}";

    /// <summary>
    /// Turns three numbers from the file into a colour.
    /// </summary>
    /// <param name="values">Red, green and blue.</param>
    /// <returns>The colour, white for a missing value.</returns>
    public static Color ToColour(float[] values)
    {
        return values != null && values.Length >= 3 ? new Color(values[0], values[1], values[2]) : Color.white;
    }

    /// <summary>
    /// Writes the look into a trader's network data, where the game's own equipment display reads it on every machine.
    /// </summary>
    /// <param name="zdo">The trader's network data.</param>
    public void WriteTo(ZDO zdo)
    {
        zdo.Set(ZDOVars.s_modelIndex, female ? 1 : 0);
        zdo.Set(ZDOVars.s_skinColor, Vector3.one);
        Color colour = ToColour(hairColour);
        zdo.Set(ZDOVars.s_hairColor, new Vector3(colour.r, colour.g, colour.b));
        zdo.Set(ZDOVars.s_hairItem, hair.GetStableHashCode());
        if (string.IsNullOrEmpty(beard))
        {
            zdo.Set(ZDOVars.s_beardItem, 0);
            zdo.Set(ZDOVars.s_noBeard, true);
        }
        else
        {
            zdo.Set(ZDOVars.s_beardItem, beard.GetStableHashCode());
        }

        zdo.Set(ZDOVars.s_chestItem, TunicPrefab.GetStableHashCode());
        zdo.Set(ZDOVars.s_legItem, TrousersPrefab.GetStableHashCode());
        zdo.Set(ZDOVars.s_shoulderItem, CapePrefab?.GetStableHashCode() ?? 0);
        zdo.Set(ZDOVars.s_shoulderItemVariant, 0);
        zdo.Set(ZDOVars.s_shoulderItemQuality, 1);
    }
}
