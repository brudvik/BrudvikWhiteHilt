using System;
using System.Collections;
using System.Diagnostics;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Makes saving the character's map cheap. The game's <c>Minimap.GetMapData</c> writes both explored maps one bool at
/// a time (eight million calls on a 2048 map) and compresses the result, on every save, even when nothing changed.
/// Here the body is built from the packed bits (<see cref="MapDataFormat"/>) and compressed by the game's own code, so
/// the saved bytes are the same. When both maps and the pins are exactly as at the last save, that save's bytes are
/// handed back without building anything.
/// <para>
/// The fast body is checked once, the first time a world loads, against the game's own method, behind the loading screen; if the two differ
/// (a game update changed the format), the fast body is not used again and the game's own method runs, still with the
/// unchanged-map reuse. Idea from HitchFix's save hitch fix (MIT, Valheim Performance Fixes contributors).
/// </para>
/// </summary>
public static class MapSaveCache
{
    private static int[] lastOwn;
    private static int[] lastShared;
    private static byte[] lastTail;
    private static int lastSize;
    private static byte[] lastResult;

    // Set while the check calls the game's own method, so the prefix lets it through.
    private static bool callingVanilla;
    private static bool fastChecked;
    private static bool fastMatches;

    // A snapshot of the map taken in the prefix and kept for the postfix.
    private struct Snapshot
    {
        public int Size;
        public int Count;
        public int[] Own;
        public int[] Shared;
        public byte[] Tail;
    }

    private static Snapshot pending;
    private static bool havePending;

    /// <summary>
    /// Instead of the game's GetMapData: returns the last result for an unchanged map, or builds it from the packed bits.
    /// </summary>
    /// <param name="map">The minimap.</param>
    /// <param name="result">The map data, when this returns true.</param>
    /// <returns>True when <paramref name="result"/> replaces the game's method; false to let the game's method run.</returns>
    public static bool TryGetMapData(Minimap map, out byte[] result)
    {
        result = null;
        havePending = false;
        if (callingVanilla || !StreamingSettings.MapSave.Value || map.m_explored == null || map.m_exploredOthers == null
            || map.m_exploredOthers.Length < map.m_explored.Length)
        {
            return false;
        }

        try
        {
            Snapshot snapshot = Take(map);
            if (lastResult != null && snapshot.Size == lastSize && MapDataFormat.Same(snapshot.Own, lastOwn)
                && MapDataFormat.Same(snapshot.Shared, lastShared) && MapDataFormat.Same(snapshot.Tail, lastTail))
            {
                result = (byte[])lastResult.Clone();
                ZLog.Log("Minimap: map unchanged since the last save, reusing its " + result.Length + " bytes");
                return true;
            }

            if (fastMatches)
            {
                Stopwatch watch = Stopwatch.StartNew();
                byte[] body = MapDataFormat.Body(snapshot.Size, snapshot.Own, snapshot.Shared, snapshot.Count, snapshot.Tail);
                result = Wrap(body);
                ZLog.Log("Minimap: compressed mapData " + body.Length + " => " + result.Length + " bytes in "
                    + watch.ElapsedMilliseconds + " ms");
                Remember(snapshot, result);
                result = (byte[])result.Clone();
                return true;
            }

            // The game's method runs; the postfix keeps its result for the next save.
            pending = snapshot;
            havePending = true;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning("Map save: could not build the map data, the game's own method saves it: " + ex.Message);
            result = null;
        }

        return false;
    }

    /// <summary>
    /// After the game's own GetMapData: keeps its result for an unchanged map next time.
    /// </summary>
    /// <param name="result">What the game's method returned.</param>
    public static void AfterVanilla(byte[] result)
    {
        if (!havePending || result == null)
        {
            return;
        }

        havePending = false;
        Remember(pending, (byte[])result.Clone());
    }

    /// <summary>
    /// The first time a world loads, just after the map is loaded and while the loading screen is up: runs the game's own GetMapData
    /// and compares it with the fast body. Equal turns the fast body on; different keeps the game's method.
    /// </summary>
    /// <param name="map">The minimap.</param>
    public static void CheckAgainstGame(Minimap map)
    {
        if (fastChecked || !StreamingSettings.MapSave.Value || map.m_explored == null || map.m_exploredOthers == null
            || ZNet.instance == null)
        {
            return;
        }

        fastChecked = true;
        try
        {
            Stopwatch watch = Stopwatch.StartNew();
            Snapshot snapshot = Take(map);
            byte[] vanilla;
            callingVanilla = true;
            try
            {
                vanilla = map.GetMapData();
            }
            finally
            {
                callingVanilla = false;
            }

            long vanillaMs = watch.ElapsedMilliseconds;
            byte[] fast = Wrap(MapDataFormat.Body(snapshot.Size, snapshot.Own, snapshot.Shared, snapshot.Count, snapshot.Tail));
            fastMatches = MapDataFormat.Same(vanilla, fast);
            Remember(snapshot, (byte[])vanilla.Clone());
            if (fastMatches)
            {
                Jotunn.Logger.LogInfo($"Map save: the fast map data matches the game's ({vanilla.Length} bytes; the game's "
                    + $"own method took {vanillaMs} ms).");
            }
            else
            {
                Jotunn.Logger.LogWarning($"Map save: the fast map data differs from the game's ({fast.Length} vs "
                    + $"{vanilla.Length} bytes), so the game's own method saves the map.");
            }
        }
        catch (Exception ex)
        {
            fastMatches = false;
            Jotunn.Logger.LogWarning("Map save: could not check the fast map data, the game's own method saves the map: "
                + ex.Message);
        }
    }

    // The game's wrapper: the version, then the compressed body with its length, through the game's own compression.
    private static byte[] Wrap(byte[] body)
    {
        ZPackage outer = new ZPackage();
        outer.Write((int)global::Version.c_MapVersion);
        outer.WriteCompressed(new ZPackage(body));
        return outer.GetArray();
    }

    private static Snapshot Take(Minimap map)
    {
        int count = map.m_explored.Length;
        int[] own = MapDataFormat.Words(map.m_explored);
        int[] shared = MapDataFormat.Words(map.m_exploredOthers);
        if (map.m_exploredOthers.Length > count)
        {
            // The game writes only as many shared pixels as it has own ones.
            Array.Resize(ref shared, own.Length);
            if ((count & 31) != 0)
            {
                shared[shared.Length - 1] &= (int)((1u << (count & 31)) - 1u);
            }
        }

        return new Snapshot { Size = map.m_textureSize, Count = count, Own = own, Shared = shared, Tail = Tail(map) };
    }

    // Everything after the explored maps, written as the game writes it.
    private static byte[] Tail(Minimap map)
    {
        ZPackage tail = new ZPackage();
        int saved = 0;
        foreach (Minimap.PinData pin in map.m_pins)
        {
            if (pin.m_save)
            {
                saved++;
            }
        }

        tail.Write(saved);
        foreach (Minimap.PinData pin in map.m_pins)
        {
            if (pin.m_save)
            {
                tail.Write(pin.m_name);
                tail.Write(pin.m_pos);
                tail.Write((int)pin.m_type);
                tail.Write(pin.m_checked);
                tail.Write(pin.m_ownerID);
                tail.Write(pin.m_author.ToString());
            }
        }

        tail.Write(ZNet.instance.IsReferencePositionPublic());
        return tail.GetArray();
    }

    private static void Remember(Snapshot snapshot, byte[] result)
    {
        lastOwn = snapshot.Own;
        lastShared = snapshot.Shared;
        lastTail = snapshot.Tail;
        lastSize = snapshot.Size;
        lastResult = result;
    }
}
