using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Dowsing;

/// <summary>
/// Runs on the <see cref="Game"/> object. While the Stone Dowser is worn, the client asks the server now and then for the
/// nearest clearing that still has rocks; only the server knows every location, also those nobody has been near. A
/// clearing nobody has visited counts as full; in a visited one the rocks not yet picked are counted. The client marks
/// the answer on the map and tells the direction when it changes.
/// </summary>
public class StoneDowsingService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltDowseRequest";
    private const string ReplyRpc = "WhiteHiltDowseReply";

    // A location's exterior radius can be unset; its rocks still lie within this many metres.
    private const float MinScanRadius = 20f;

    private static readonly string[] directions = { "n", "ne", "e", "se", "s", "sw", "w", "nw" };

    private static bool active;
    private static float nextRequest;
    private static Minimap.PinData pin;
    private static Vector3? lastTarget;
    private static bool toldNone;

    private string hashedItems;
    private readonly HashSet<int> trackedPrefabs = new();

    /// <summary>
    /// Asks the server for the nearest clearing when it is time. Called every frame while the dowser is worn.
    /// </summary>
    /// <param name="position">The local player's position.</param>
    public static void Tick(Vector3 position)
    {
        active = true;
        if (Time.time < nextRequest || ZRoutedRpc.instance == null)
        {
            return;
        }

        nextRequest = Time.time + StoneDowsingSettings.RefreshSeconds.Value;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, position);
    }

    /// <summary>
    /// Forgets the clearing and takes its pin off the map. Called when the dowser is taken off.
    /// </summary>
    public static void Clear()
    {
        active = false;
        nextRequest = 0f;
        lastTarget = null;
        toldNone = false;
        RemovePin();
    }

    private void Start()
    {
        Clear();
        pin = null;
        ZRoutedRpc.instance?.Register<Vector3>(RequestRpc, RPC_Request);
        ZRoutedRpc.instance?.Register<ZPackage>(ReplyRpc, RPC_Reply);
    }

    private void RPC_Request(long sender, Vector3 position)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || ZoneSystem.instance == null || ZDOMan.instance == null)
        {
            return;
        }

        ZPackage reply = new();
        bool found = TryFindClearing(position, out Vector3 clearing, out int rocksLeft);
        reply.Write(found);
        if (found)
        {
            reply.Write(clearing);
            reply.Write(rocksLeft);
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, reply);
    }

    // The nearest tracked location within the search radius that has rocks left; -1 rocks for one nobody has visited.
    private bool TryFindClearing(Vector3 position, out Vector3 clearing, out int rocksLeft)
    {
        clearing = Vector3.zero;
        rocksLeft = 0;
        float radius = StoneDowsingSettings.SearchRadius.Value;
        float best = radius * radius;
        bool found = false;
        HashSet<int> prefabs = TrackedPrefabs();
        foreach (ZoneSystem.LocationInstance instance in ZoneSystem.instance.m_locationInstances.Values)
        {
            ZoneSystem.ZoneLocation location = instance.m_location;
            if (location == null || !(StoneDowsingSettings.IsTrackedLocation(location.m_prefabName) || StoneDowsingSettings.IsTrackedLocation(location.m_name)))
            {
                continue;
            }

            float dx = instance.m_position.x - position.x;
            float dz = instance.m_position.z - position.z;
            float distance = dx * dx + dz * dz;
            if (distance >= best)
            {
                continue;
            }

            int left = instance.m_placed ? CountRocks(instance.m_position, Mathf.Max(location.m_exteriorRadius, MinScanRadius), prefabs) : -1;
            if (left == 0)
            {
                continue;
            }

            best = distance;
            clearing = instance.m_position;
            rocksLeft = left;
            found = true;
        }

        return found;
    }

    private static int CountRocks(Vector3 centre, float radius, HashSet<int> prefabs)
    {
        Vector2s min = ZoneSystem.GetZone(centre - new Vector3(radius, 0f, radius));
        Vector2s max = ZoneSystem.GetZone(centre + new Vector3(radius, 0f, radius));
        List<ZDO>[] sectors = ZDOMan.instance.m_objectsBySector;
        int count = 0;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                uint index = ZoneSystem.SectorToIndex(x, y).Sector;
                List<ZDO> objects = index < sectors.Length ? sectors[index] : null;
                if (objects == null)
                {
                    continue;
                }

                foreach (ZDO zdo in objects)
                {
                    if (prefabs.Contains(zdo.GetPrefab()) && Utils.DistanceXZ(zdo.GetPosition(), centre) <= radius && !zdo.GetBool(ZDOVars.s_picked))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    // Hashes of the pickable prefabs that give a tracked item, rebuilt when the item list changes.
    private HashSet<int> TrackedPrefabs()
    {
        string items = StoneDowsingSettings.Items.Value;
        if (items == hashedItems || ZNetScene.instance == null)
        {
            return trackedPrefabs;
        }

        hashedItems = items;
        trackedPrefabs.Clear();
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            Pickable pickable = prefab != null ? prefab.GetComponent<Pickable>() : null;
            if (pickable != null && pickable.m_itemPrefab != null && StoneDowsingSettings.IsTrackedItem(pickable.m_itemPrefab.name))
            {
                trackedPrefabs.Add(prefab.name.GetStableHashCode());
            }
        }

        return trackedPrefabs;
    }

    private void RPC_Reply(long sender, ZPackage reply)
    {
        Player player = Player.m_localPlayer;
        if (!active || player == null || sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        if (!reply.ReadBool())
        {
            lastTarget = null;
            RemovePin();
            if (!toldNone)
            {
                toldNone = true;
                player.Message(MessageHud.MessageType.TopLeft,
                    string.Format(Translations.Word("msg_whitehilt_dowser_none"), Mathf.RoundToInt(StoneDowsingSettings.SearchRadius.Value)));
            }

            return;
        }

        Vector3 clearing = reply.ReadVector3();
        int rocksLeft = reply.ReadInt();
        toldNone = false;
        ShowPin(clearing, rocksLeft);
        if (lastTarget.HasValue && Utils.DistanceXZ(lastTarget.Value, clearing) < 1f)
        {
            return;
        }

        lastTarget = clearing;
        Vector3 toward = clearing - player.transform.position;
        float distance = Mathf.Round(new Vector2(toward.x, toward.z).magnitude / 10f) * 10f;
        player.Message(MessageHud.MessageType.TopLeft,
            string.Format(Translations.Word("msg_whitehilt_dowser_found"), Direction(toward), distance.ToString("0", CultureInfo.CurrentCulture)));
    }

    private static void ShowPin(Vector3 clearing, int rocksLeft)
    {
        RemovePin();
        if (!StoneDowsingSettings.ShowPin.Value || Minimap.instance == null)
        {
            return;
        }

        string label = Translations.Word("whitehilt_dowser_pin");
        if (rocksLeft > 0)
        {
            label += $" ({rocksLeft})";
        }

        pin = Minimap.instance.AddPin(clearing, Minimap.PinType.Icon3, label, save: false, isChecked: false);
    }

    private static void RemovePin()
    {
        if (pin != null && Minimap.instance != null)
        {
            Minimap.instance.RemovePin(pin);
        }

        pin = null;
    }

    private static string Direction(Vector3 toward)
    {
        float angle = Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg;
        int index = (Mathf.RoundToInt(angle / 45f) % 8 + 8) % 8;
        return Translations.Word($"whitehilt_dowser_dir_{directions[index]}");
    }
}
