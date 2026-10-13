using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BrudvikWhiteHilt.Diagnostics;

/// <summary>
/// Keeps one oversized network message from cutting a player off. Steam refuses a message over 512 KB, and the game
/// then tries that same message again every frame, so nothing queued behind it is ever sent: the player gets no more
/// world, and is dropped a little later. The guard drops such a message before it is queued, and logs what it was.
/// </summary>
public static class MessageSizeGuard
{
    /// <summary>Largest message Steam sends (k_cbMaxSteamNetworkingSocketsMessageSizeSend).</summary>
    public const int SteamLimit = 512 * 1024;

    private const float RepeatLogSeconds = 60f;

    private static readonly int routedHash = "RoutedRPC".GetStableHashCode();
    private static readonly int zdoDataHash = "ZDOData".GetStableHashCode();
    private static readonly Dictionary<string, float> lastLogged = new();
    private static Dictionary<int, string> names;

    /// <summary>
    /// Whether a message may be sent. A message that may not is logged.
    /// </summary>
    /// <param name="package">The message.</param>
    /// <param name="receiver">Who it was for, for the log.</param>
    /// <returns>False if Steam would refuse it.</returns>
    public static bool Allow(ZPackage package, string receiver)
    {
        if (package == null || package.Size() <= SteamLimit)
        {
            return true;
        }

        string what;
        try
        {
            what = Describe(package);
        }
        catch (Exception ex)
        {
            what = $"unreadable ({ex.GetType().Name})";
        }

        if (!lastLogged.TryGetValue(what, out float last) || Time.realtimeSinceStartup - last >= RepeatLogSeconds)
        {
            lastLogged[what] = Time.realtimeSinceStartup;
            Jotunn.Logger.LogError($"Network: dropped a message of {package.Size():N0} bytes to {receiver}, larger than Steam " +
                                   $"allows ({SteamLimit:N0}); sent, it would have stopped every later message. It was {what}.");
        }

        return false;
    }

    // Reads the call the message carries: for a routed call the call inside it, for world data the largest object in
    // it. Vanilla writes world data in small batches, so a batch this big holds one object that is too big by itself.
    private static string Describe(ZPackage package)
    {
        ZPackage copy = new(package.GetArray());
        int method = copy.ReadInt();
        if (method == routedHash)
        {
            ZPackage routed = copy.ReadPackage();
            routed.ReadLong();
            routed.ReadLong();
            long target = routed.ReadLong();
            routed.ReadZDOID();
            return $"the routed call {NameOf(routed.ReadInt())} for peer {target}";
        }

        return method == zdoDataHash ? DescribeZdos(copy) : $"the call {NameOf(method)}";
    }

    private static string DescribeZdos(ZPackage package)
    {
        int invalid = package.ReadInt();
        for (int i = 0; i < invalid; i++)
        {
            package.ReadZDOID();
        }

        ZDOID biggest = ZDOID.None;
        Vector3 position = Vector3.zero;
        int biggestSize = -1;
        int count = 0;
        while (true)
        {
            ZDOID id = package.ReadZDOID();
            if (id.IsNone())
            {
                break;
            }

            count++;
            package.ReadUShort();
            package.ReadUInt();
            package.ReadLong();
            Vector3 at = package.ReadVector3();
            int size = package.ReadPackage().Size();
            if (size > biggestSize)
            {
                biggest = id;
                position = at;
                biggestSize = size;
            }
        }

        int prefab = ZDOMan.instance?.GetZDO(biggest)?.GetPrefab() ?? 0;
        string name = ZNetScene.instance?.GetPrefab(prefab)?.name ?? prefab.ToString();
        return $"world data for {count} objects; the largest is {name} {biggest} at {position} ({biggestSize:N0} bytes)";
    }

    // The names of the mod's own calls, found from its string constants, since the game only sends their hashes.
    private static string NameOf(int hash)
    {
        if (names == null)
        {
            names = new Dictionary<int, string>();
            Type[] types;
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type != null).ToArray();
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (FieldInfo field in types.SelectMany(type => type.GetFields(flags)))
            {
                if (field.IsLiteral && field.FieldType == typeof(string) && field.GetRawConstantValue() is string value && value.Length > 0)
                {
                    names[value.GetStableHashCode()] = value;
                }
            }
        }

        return names.TryGetValue(hash, out string name) ? name : $"#{hash}";
    }
}
