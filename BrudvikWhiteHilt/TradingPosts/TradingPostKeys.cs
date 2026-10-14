namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The names the trading posts use in the network data.
/// </summary>
public static class TradingPostKeys
{
    /// <summary>Prefab name of the invisible marker every post has at its origin, which carries the dome.</summary>
    public const string MarkerPrefabName = "WhiteHilt_TradingPost";

    /// <summary>The post a piece or marker belongs to: the post's id, the same on all its pieces.</summary>
    public static readonly int PostId = "whitehilt_post".GetStableHashCode();

    /// <summary>On the marker: the biome the post serves (<see cref="Heightmap.Biome"/> as an int).</summary>
    public static readonly int Biome = "whitehilt_post_biome".GetStableHashCode();

    /// <summary>Prefab hash of the marker.</summary>
    public static readonly int MarkerHash = MarkerPrefabName.GetStableHashCode();

    /// <summary>
    /// Whether an object belongs to a trading post.
    /// </summary>
    /// <param name="zdo">The object's network data, or null.</param>
    /// <returns>True for a part of a post.</returns>
    public static bool IsPostPart(ZDO zdo)
    {
        return zdo != null && !string.IsNullOrEmpty(zdo.GetString(PostId));
    }
}
