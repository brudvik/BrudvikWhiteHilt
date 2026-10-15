using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The bounty notice a plagued post's trader hands out (<see cref="TradingPostTerror"/>). What it shows is kept in the
/// item itself, so it holds wherever the player goes: while the player carries it, a circle on the map shows where the
/// monster was last seen, and once the player is close to the lair the fresh tracks narrow the circle to it. Runs on
/// the <see cref="Game"/> object of every machine with a player.
/// </summary>
public class TerrorNotice : MonoBehaviour
{
    private const string PostKey = "whitehilt_terror_post";
    private const string BiomeKey = "whitehilt_terror_biome";
    private const string CentreKey = "whitehilt_terror_centre";
    private const string LairKey = "whitehilt_terror_lair";
    private const string UntilKey = "whitehilt_terror_until";
    private const string TrackedKey = "whitehilt_terror_tracked";

    private const float CheckInterval = 2f;

    // Close enough to the lair to find the fresh tracks, and the circle they narrow the search to.
    private const float TrackRange = 110f;
    private const float TrackedRadius = 45f;

    private readonly Dictionary<string, (Minimap.PinData Area, Minimap.PinData Icon, bool Tracked)> pins = new();
    private float nextCheck;

    /// <summary>
    /// What a notice shows.
    /// </summary>
    public struct Data
    {
        /// <summary>The plagued post's id.</summary>
        public string Post;

        /// <summary>The post's biome index.</summary>
        public int Biome;

        /// <summary>The middle of the search circle.</summary>
        public Vector3 Centre;

        /// <summary>The monster's lair.</summary>
        public Vector3 Lair;

        /// <summary>The world time the plague lasts until.</summary>
        public long Until;

        /// <summary>Whether the player has found the fresh tracks.</summary>
        public bool Tracked;
    }

    /// <summary>
    /// Whether an item is a bounty notice.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for a notice.</returns>
    public static bool Is(ItemDrop.ItemData item) => item?.m_dropPrefab != null && item.m_dropPrefab.name == TradingPostTerrors.NoticeName;

    /// <summary>
    /// Writes what a notice shows into it.
    /// </summary>
    /// <param name="item">The notice.</param>
    /// <param name="data">What it shows.</param>
    public static void Write(ItemDrop.ItemData item, Data data)
    {
        item.m_customData[PostKey] = data.Post ?? string.Empty;
        item.m_customData[BiomeKey] = data.Biome.ToString(CultureInfo.InvariantCulture);
        item.m_customData[CentreKey] = Vector(data.Centre);
        item.m_customData[LairKey] = Vector(data.Lair);
        item.m_customData[UntilKey] = data.Until.ToString(CultureInfo.InvariantCulture);
        if (data.Tracked)
        {
            item.m_customData[TrackedKey] = "1";
        }
    }

    /// <summary>
    /// Reads what a notice shows.
    /// </summary>
    /// <param name="item">The notice.</param>
    /// <param name="data">What it shows.</param>
    /// <returns>False for a notice without anything written in it.</returns>
    public static bool TryRead(ItemDrop.ItemData item, out Data data)
    {
        data = default;
        if (item == null || !item.m_customData.TryGetValue(PostKey, out string post) || string.IsNullOrEmpty(post))
        {
            return false;
        }

        item.m_customData.TryGetValue(BiomeKey, out string biome);
        item.m_customData.TryGetValue(CentreKey, out string centre);
        item.m_customData.TryGetValue(LairKey, out string lair);
        item.m_customData.TryGetValue(UntilKey, out string until);
        data = new Data
        {
            Post = post,
            Biome = int.TryParse(biome, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) ? index : 0,
            Centre = Vector(centre),
            Lair = Vector(lair),
            Until = long.TryParse(until, NumberStyles.Integer, CultureInfo.InvariantCulture, out long time) ? time : 0L,
            Tracked = item.m_customData.ContainsKey(TrackedKey)
        };
        return true;
    }

    /// <summary>
    /// Whether a player carries a notice for a post.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="postId">The post's id.</param>
    /// <returns>True with one.</returns>
    public static bool Carries(Player player, string postId)
    {
        return player != null && player.GetInventory().GetAllItems().Any(item => Is(item) && TryRead(item, out Data data) && data.Post == postId);
    }

    /// <summary>
    /// Writes the post's plague into the notices the player has just been given there, the ones with nothing in them yet.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="postId">The post's id.</param>
    public static void FillNew(Player player, string postId)
    {
        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems().Where(item => Is(item) && !TryRead(item, out _)).ToList())
        {
            TradingPostTerror.Fill(item, postId);
        }
    }

    /// <summary>
    /// Reading a notice opens the map on its circle and says how long is left.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The notice.</param>
    public static void Read(Player player, ItemDrop.ItemData item)
    {
        if (!TryRead(item, out Data data) || ZNet.instance == null || !TradingPostTerror.Lasts(data.Until, ZNet.instance.GetTimeSeconds()))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_terror_lapsed");
            return;
        }

        Helpers.WhiteHiltSound.PlayLocal("sfx_parchment");
        TerrorDefinition terror = TerrorDefinition.Of(data.Biome);
        double days = (data.Until - ZNet.instance.GetTimeSeconds()) / 1800.0;
        string name = terror != null ? Localization.instance.Localize("$" + terror.NameKey) : string.Empty;
        player.Message(MessageHud.MessageType.Center,
            Localization.instance.Localize("$msg_whitehilt_terror_notice", name, Mathf.Max(1, Mathf.CeilToInt((float)days)).ToString()));
        // The notice is read from the inventory. The game takes no map input while the inventory shows, so with both
        // open neither could be closed: the inventory goes first.
        if (InventoryGui.instance != null && InventoryGui.IsVisible())
        {
            InventoryGui.instance.Hide();
        }

        Minimap.instance?.ShowPointOnMap(data.Tracked ? data.Lair : data.Centre);
    }

    private void OnDestroy()
    {
        foreach (string post in pins.Keys.ToList())
        {
            RemovePins(post);
        }
    }

    // Keeps a circle on the map for every notice the player carries while its plague lasts, and finds the tracks.
    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        Player player = Player.m_localPlayer;
        if (player == null || Minimap.instance == null || ZNet.instance == null)
        {
            return;
        }

        double now = ZNet.instance.GetTimeSeconds();
        HashSet<string> shown = new();
        foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems().Where(Is))
        {
            if (!TryRead(item, out Data data) || !TradingPostTerror.Lasts(data.Until, now) || !shown.Add(data.Post))
            {
                continue;
            }

            TerrorDefinition terror = TerrorDefinition.Of(data.Biome);
            if (!data.Tracked && Utils.DistanceXZ(player.transform.position, data.Lair) < TrackRange)
            {
                data.Tracked = true;
                Write(item, data);
                Helpers.WhiteHiltSound.PlayLocal("sfx_tracks");
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_whitehilt_terror_tracks", terror != null ? Localization.instance.Localize("$" + terror.NameKey) : string.Empty));
            }

            if (!pins.TryGetValue(data.Post, out var pin) || pin.Tracked != data.Tracked)
            {
                RemovePins(data.Post);
                AddPins(data, terror);
            }
        }

        foreach (string post in pins.Keys.Where(post => !shown.Contains(post)).ToList())
        {
            RemovePins(post);
        }
    }

    private void AddPins(Data data, TerrorDefinition terror)
    {
        Vector3 centre = data.Tracked ? data.Lair : data.Centre;
        string name = terror != null ? Localization.instance.Localize("$whitehilt_terror_pin", Localization.instance.Localize("$" + terror.NameKey)) : string.Empty;
        Minimap.PinData area = Minimap.instance.AddPin(centre, Minimap.PinType.EventArea, string.Empty, save: false, isChecked: false);
        area.m_worldSize = (data.Tracked ? TrackedRadius : TradingPostTerror.CircleRadius) * 2f;
        Minimap.PinData icon = Minimap.instance.AddPin(centre, Minimap.PinType.RandomEvent, name, save: false, isChecked: false);
        icon.m_animate = true;
        pins[data.Post] = (area, icon, data.Tracked);
    }

    private void RemovePins(string post)
    {
        if (pins.TryGetValue(post, out var pin) && Minimap.instance != null)
        {
            Minimap.instance.RemovePin(pin.Area);
            Minimap.instance.RemovePin(pin.Icon);
        }

        pins.Remove(post);
    }

    private static string Vector(Vector3 value) =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.##};{1:0.##};{2:0.##}", value.x, value.y, value.z);

    private static Vector3 Vector(string text)
    {
        string[] parts = text?.Split(';');
        if (parts == null || parts.Length != 3)
        {
            return Vector3.zero;
        }

        float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
        float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
        float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
        return new Vector3(x, y, z);
    }
}
