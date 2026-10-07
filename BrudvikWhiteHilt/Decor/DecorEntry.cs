using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BrudvikWhiteHilt.Decor;

/// <summary>
/// What a decor piece's look comes from.
/// </summary>
public enum DecorLook
{
    /// <summary>The visible parts of a vanilla prefab, with its own materials (vegetation keeps its wind).</summary>
    Vanilla,

    /// <summary>A model from the decor bundle (AssetSource/Decor/Models).</summary>
    Model,

    /// <summary>A model the mod's main bundle already holds for another piece.</summary>
    Bundle
}

/// <summary>
/// The light a decor piece gives.
/// </summary>
public enum DecorLight
{
    /// <summary>No light.</summary>
    None,

    /// <summary>A small flame with a short, warm light.</summary>
    Candle,

    /// <summary>A flame behind glass, with a wider light.</summary>
    Lantern,

    /// <summary>An open fire with flames and a crackle.</summary>
    Fire
}

/// <summary>
/// One piece of the Decor Hammer, as listed in AssetSource/Decor/decor.json (embedded in the mod).
/// </summary>
/// <remarks>
/// The pieces are data, not code: a new decoration is one more line in the file, and prepare_decor.py makes its model.
/// The catalogue is read with Jotunn's SimpleJson into plain dictionaries rather than typed fields, so an entry can
/// leave out everything it does not need.
/// </remarks>
public sealed class DecorEntry
{
    private const string ResourceName = "BrudvikWhiteHilt.Decor.json";

    private static List<DecorEntry> all;

    /// <summary>Short id, unique in the catalogue; the prefab, mesh and translation keys are made from it.</summary>
    public string Id { get; private set; }

    /// <summary>The build menu tab the piece is shown under.</summary>
    public string Category { get; private set; }

    /// <summary>Where the look comes from.</summary>
    public DecorLook Look { get; private set; }

    /// <summary>The vanilla prefab, for <see cref="DecorLook.Vanilla"/>.</summary>
    public string VanillaPrefab { get; private set; }

    /// <summary>The mesh name in the main bundle, for <see cref="DecorLook.Bundle"/>.</summary>
    public string BundleMesh { get; private set; }

    /// <summary>Height in metres, for models; 0 keeps a vanilla prefab's own size.</summary>
    public float Height { get; private set; }

    /// <summary>Uniform scale applied on top, mostly to shrink large vanilla props.</summary>
    public float Scale { get; private set; } = 1f;

    /// <summary>Whether it sways in the wind: models get Valheim's vegetation material.</summary>
    public bool Wind { get; private set; }

    /// <summary>Whether it blocks players; plants, cloth and small things on tables do not.</summary>
    public bool Solid { get; private set; } = true;

    /// <summary>What it is made of, for its health and the sound and dust when it breaks.</summary>
    public string Material { get; private set; } = "wood";

    /// <summary>The light it gives.</summary>
    public DecorLight Light { get; private set; }

    /// <summary>
    /// Where the flame sits, as a share of the piece's height from its foot, or a negative number for the light's
    /// usual place (the top of a candle, the middle of a lantern, the bottom of a fire).
    /// </summary>
    public float LightAt { get; private set; } = -1f;

    /// <summary>Height of the seat in metres, or 0 for a piece that cannot be sat on.</summary>
    public float Seat { get; private set; }

    /// <summary>
    /// Which way one sits, in degrees about the vertical from the model's +z; 180 for a chair whose back is on +z.
    /// </summary>
    public float SeatYaw { get; private set; }

    /// <summary>What it costs.</summary>
    public RequirementConfig[] Requirements { get; private set; }

    /// <summary>English name.</summary>
    public string Name { get; private set; }

    /// <summary>English description.</summary>
    public string Description { get; private set; }

    /// <summary>Attribution for a CC BY model, or null.</summary>
    public string Credit { get; private set; }

    /// <summary>Prefab name of the piece.</summary>
    public string PrefabName => $"piece_whitehilt_decor_{Id}";

    /// <summary>Mesh and texture name in the decor bundle.</summary>
    public string MeshName => $"decor_{Id}";

    /// <summary>
    /// Every entry of the catalogue, read once from the embedded file.
    /// </summary>
    public static IReadOnlyList<DecorEntry> All => all ??= Load();

    private static List<DecorEntry> Load()
    {
        using Stream stream = typeof(DecorEntry).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>
    /// Reads a catalogue. Separate from loading so the tests can read the file in the repository.
    /// </summary>
    /// <param name="json">The catalogue's text.</param>
    /// <returns>Its entries, in order.</returns>
    internal static List<DecorEntry> Parse(string json)
    {
        IDictionary<string, object> root = (IDictionary<string, object>)SimpleJson.SimpleJson.DeserializeObject(json);
        return ((IList<object>)root["pieces"]).Cast<IDictionary<string, object>>().Select(FromJson).ToList();
    }

    private static DecorEntry FromJson(IDictionary<string, object> json)
    {
        DecorEntry entry = new()
        {
            Id = Text(json, "id") ?? throw new InvalidDataException("a decor entry has no id"),
            Category = Text(json, "category") ?? "Home",
            VanillaPrefab = Text(json, "vanilla"),
            BundleMesh = Text(json, "bundle"),
            Height = Number(json, "height", 0f),
            Scale = Number(json, "scale", 1f),
            Wind = Flag(json, "wind", false),
            Material = Text(json, "material") ?? "wood",
            Seat = Number(json, "seat", 0f),
            SeatYaw = Number(json, "seatYaw", 0f),
            LightAt = Number(json, "lightAt", -1f),
            Name = Text(json, "name"),
            Description = Text(json, "description"),
            Credit = Text(json, "credit"),
            Requirements = ParseCost(Text(json, "cost") ?? string.Empty)
        };
        entry.Look = entry.VanillaPrefab != null ? DecorLook.Vanilla : entry.BundleMesh != null ? DecorLook.Bundle : DecorLook.Model;
        // Plants and loose cloth let players through, so walking through a flower bed bends the flowers.
        entry.Solid = Flag(json, "solid", !entry.Wind && entry.Material != "cloth");
        entry.Light = (Text(json, "light") ?? "none") switch
        {
            "candle" => DecorLight.Candle,
            "lantern" => DecorLight.Lantern,
            "fire" => DecorLight.Fire,
            _ => DecorLight.None
        };
        return entry;
    }

    // "Wood:2,Resin:1". The materials come back when a decoration is removed, so rearranging a room costs nothing.
    private static RequirementConfig[] ParseCost(string cost)
    {
        return cost.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split(':'))
            .Select(parts => new RequirementConfig
            {
                Item = parts[0].Trim(),
                Amount = parts.Length > 1 ? int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture) : 1,
                Recover = true
            })
            .ToArray();
    }

    private static string Text(IDictionary<string, object> json, string key)
    {
        return json.TryGetValue(key, out object value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
    }

    private static float Number(IDictionary<string, object> json, string key, float fallback)
    {
        return json.TryGetValue(key, out object value) && value != null ? Convert.ToSingle(value, CultureInfo.InvariantCulture) : fallback;
    }

    private static bool Flag(IDictionary<string, object> json, string key, bool fallback)
    {
        return json.TryGetValue(key, out object value) && value is bool flag ? flag : fallback;
    }

    /// <summary>
    /// Registers the English name and description of every entry.
    /// </summary>
    public static void RegisterTranslations()
    {
        foreach (DecorEntry entry in All)
        {
            Translations.AddEnglish(entry.PrefabName, entry.Name ?? entry.Id);
            Translations.AddEnglish($"{entry.PrefabName}_description", entry.Description ?? string.Empty);
        }
    }
}
