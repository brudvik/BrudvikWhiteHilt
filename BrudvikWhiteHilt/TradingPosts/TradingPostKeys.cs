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

    /// <summary>On the marker: set once the post's trader has been put out.</summary>
    public static readonly int HasTrader = "whitehilt_post_trader".GetStableHashCode();

    /// <summary>On the marker: the version of the layout the post was built from (0 before versions).</summary>
    public static readonly int LayoutVersion = "whitehilt_post_layout".GetStableHashCode();

    /// <summary>On the marker: the world time, in whole seconds, the hunter's coming was last decided.</summary>
    public static readonly int HunterDecided = "whitehilt_post_hunter_time".GetStableHashCode();

    /// <summary>On the marker: whether the hunter is at the post, as last decided.</summary>
    public static readonly int HunterHere = "whitehilt_post_hunter_here".GetStableHashCode();

    /// <summary>On the marker: the network id of the hunter put out, in two parts.</summary>
    public static readonly int HunterUser = "whitehilt_post_hunter_user".GetStableHashCode();

    /// <summary>On the marker: the second part of the hunter's network id.</summary>
    public static readonly int HunterObject = "whitehilt_post_hunter_id".GetStableHashCode();

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
