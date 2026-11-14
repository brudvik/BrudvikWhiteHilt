namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// The coverings of the White Hilt roofs.
/// </summary>
public enum RoofCovering
{
    /// <summary>Sod over birch bark (torvtak).</summary>
    Turf,

    /// <summary>Tarred pine shingles (spontak).</summary>
    Shingle,

    /// <summary>Tarred shingles with rounded ends, as on the stave churches.</summary>
    ScaleShingle,

    /// <summary>Stone slabs (hellertak).</summary>
    Slate,

    /// <summary>Thatch of barley and flax straw (stråtak).</summary>
    Straw,

    /// <summary>Thatch of swamp reed (rørtak).</summary>
    Reed,

    /// <summary>Black marble slabs, as the dvergr build.</summary>
    Marble,

    /// <summary>Tiles of grey ash stone.</summary>
    Grausten,

    /// <summary>The red tiles of the game's own Grausten Roof, in the pitches and shapes the game lacks.</summary>
    RedGrausten
}

/// <summary>
/// How each covering is built and which textures it uses. Only uses UnityEngine, so the offline previews share it.
/// </summary>
public static class RoofCoverings
{
    /// <summary>All coverings, in the order they appear in the hammer.</summary>
    public static readonly RoofCovering[] All =
    {
        RoofCovering.Turf, RoofCovering.Reed, RoofCovering.Shingle, RoofCovering.ScaleShingle, RoofCovering.Slate, RoofCovering.Straw,
        RoofCovering.Marble, RoofCovering.Grausten, RoofCovering.RedGrausten
    };

    /// <summary>
    /// Whether a covering comes in a shape and pitch. Most come in the five shapes of the vanilla wooden roof at every
    /// pitch. The red Grausten Roof fills in what the game's own lacks: every shape at 26° and 67°, and at 45° all but the
    /// plain slope (the game's own) and the ridge (made from the game's own tiles), and only it has the joints of the
    /// ridges (corner, tee, cross), the hip end and the pyramid top.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <param name="shape">The shape.</param>
    /// <param name="pitch">The pitch.</param>
    /// <returns>True if the piece exists.</returns>
    public static bool Has(RoofCovering covering, RoofShape shape, RoofPitch pitch)
    {
        if (covering != RoofCovering.RedGrausten)
        {
            return !RoofMeshBuilder.IsTopJoint(shape);
        }

        return pitch != RoofPitch.Medium || (shape != RoofShape.Slope && shape != RoofShape.Ridge);
    }

    /// <summary>
    /// The build of a covering.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <returns>A new style.</returns>
    public static RoofStyle Style(RoofCovering covering)
    {
        return covering switch
        {
            RoofCovering.Turf => new RoofStyle
            {
                Thickness = 0.24f, TileWidth = 2f, TileHeight = 2f, Overhang = 0.15f, EaveLog = true,
                Crest = RoofCrest.None, EdgeStrip = true, EdgeTile = 2f
            },
            RoofCovering.Shingle or RoofCovering.ScaleShingle => new RoofStyle
            {
                Thickness = 0.06f, CourseLength = 0.25f, Step = 0.025f, CoursesPerTile = 4, TileWidth = 1f, TileHeight = 1f,
                Overhang = 0.2f, Crest = RoofCrest.Boards, EdgeTile = 1f
            },
            RoofCovering.Slate => new RoofStyle
            {
                Thickness = 0.09f, TileWidth = 3f, TileHeight = 3f, Overhang = 0.18f, Crest = RoofCrest.Cap, EdgeTile = 3f
            },
            RoofCovering.Straw => new RoofStyle
            {
                Thickness = 0.32f, TileWidth = 2.3f, TileHeight = 2.3f, Overhang = 0.3f, Crest = RoofCrest.Roll, EdgeTile = 2.3f
            },
            RoofCovering.Marble => new RoofStyle
            {
                Thickness = 0.08f, TileWidth = 1.5f, TileHeight = 1.5f, Overhang = 0.18f, Crest = RoofCrest.Cap, EdgeTile = 1.5f
            },
            RoofCovering.Grausten => new RoofStyle
            {
                Thickness = 0.12f, TileWidth = 1.8f, TileHeight = 1.8f, Overhang = 0.18f, Crest = RoofCrest.Cap, EdgeTile = 1.8f
            },
            RoofCovering.RedGrausten => new RoofStyle
            {
                Thickness = 0.3f, CourseLength = 0.354f, TileWidth = 2f, TileHeight = 2f, Overhang = 0.2f, Crest = RoofCrest.Tiles, EdgeTile = 2f,
                Tiled = true, TileLift = 0.15f
            },
            _ => new RoofStyle
            {
                Thickness = 0.28f, TileWidth = 0.8f, TileHeight = 0.8f, Overhang = 0.3f, Crest = RoofCrest.Roll, EdgeTile = 0.8f
            }
        };
    }

    /// <summary>
    /// Base name of the covering's textures in the bundle (<c>&lt;name&gt;_albedo</c>, <c>&lt;name&gt;_normal</c>).
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <returns>The texture name.</returns>
    public static string Texture(RoofCovering covering)
    {
        return covering switch
        {
            RoofCovering.Turf => "roof_turf",
            RoofCovering.Shingle => "roof_shingle",
            RoofCovering.ScaleShingle => "roof_scale",
            RoofCovering.Slate => "roof_slate",
            RoofCovering.Straw => "roof_straw",
            RoofCovering.Marble => "roof_marble",
            RoofCovering.Grausten or RoofCovering.RedGrausten => "roof_grausten",
            _ => "roof_reed"
        };
    }

    /// <summary>
    /// Base name of the texture on the covering's cut edges, or null for wood.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <returns>The texture name, or null.</returns>
    public static string EdgeTexture(RoofCovering covering)
    {
        return covering switch
        {
            RoofCovering.Turf => "roof_turfedge",
            RoofCovering.Shingle or RoofCovering.ScaleShingle => null,
            _ => Texture(covering)
        };
    }

    /// <summary>
    /// Lower-case id used in prefab names and config sections.
    /// </summary>
    /// <param name="covering">The covering.</param>
    /// <returns>The id.</returns>
    public static string Id(RoofCovering covering)
    {
        return covering switch
        {
            RoofCovering.ScaleShingle => "scaleshingle",
            _ => covering.ToString().ToLowerInvariant()
        };
    }
}
