using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Portraits;

/// <summary>
/// Runs on the <see cref="Game"/> object and shares portraits between players. Each client tells every other player the
/// hash of its own portrait; a client that does not have that picture cached asks for it once and gets about 10 KB back.
/// Everything is checked on arrival: the hash must be the one the sender announced, and the pixels must inflate to
/// exactly one portrait and hash to it.
/// </summary>
public class PortraitNetwork : MonoBehaviour
{
    private const string HashRpc = "WhiteHiltPortraitHash";
    private const string RequestRpc = "WhiteHiltPortraitRequest";
    private const string DataRpc = "WhiteHiltPortraitData";
    private const float ScanInterval = 2f;
    private const float Cooldown = 10f;

    private static PortraitNetwork instance;

    private readonly Dictionary<long, string> peerHashes = new();
    private readonly Dictionary<string, Texture2D> textures = new();
    private readonly HashSet<long> announced = new();
    private readonly HashSet<long> heard = new();
    private readonly HashSet<string> requested = new();
    private readonly Dictionary<long, float> lastReply = new();
    private readonly Dictionary<long, float> lastData = new();
    private readonly Dictionary<string, long> uidByName = new();

    private HashSet<long> seen = new();
    private string ownHash;
    private byte[] ownData;
    private Texture2D ownTexture;
    private float nextScan;

    /// <summary>
    /// The portrait of a player, by the session ID that owns their character.
    /// </summary>
    /// <param name="uid">The player's session ID.</param>
    /// <returns>The portrait, or null if it is not known (yet).</returns>
    public static Texture2D Get(long uid)
    {
        if (instance == null || uid == 0L)
        {
            return null;
        }

        if (uid == ZDOMan.GetSessionID())
        {
            return instance.ownTexture;
        }

        return instance.peerHashes.TryGetValue(uid, out string hash) && instance.textures.TryGetValue(hash, out Texture2D texture) ? texture : null;
    }

    /// <summary>
    /// Describes the known portraits, for the console.
    /// </summary>
    /// <returns>One line per player.</returns>
    public static string Describe()
    {
        if (instance == null)
        {
            return "Portraits: join a world first.";
        }

        StringBuilder text = new();
        text.Append($"Portraits: enabled {PortraitSettings.Enabled.Value}, yours {(instance.ownHash != null ? instance.ownHash.Substring(0, 8) : "missing (taken in the main menu)")}");
        foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
        {
            long uid = player.m_characterID.UserID;
            if (uid == 0L || uid == ZDOMan.GetSessionID())
            {
                continue;
            }

            string state = !instance.peerHashes.TryGetValue(uid, out string hash) ? "no portrait announced"
                : instance.textures.ContainsKey(hash) ? $"shown ({hash.Substring(0, 8)})"
                : $"waiting ({hash.Substring(0, 8)})";
            text.Append($"\n  {player.m_name}: {state}");
        }

        return text.ToString();
    }

    private void Start()
    {
        instance = this;
        LoadOwn();
        ZRoutedRpc.instance?.Register<string>(HashRpc, RPC_Hash);
        ZRoutedRpc.instance?.Register(RequestRpc, RPC_Request);
        ZRoutedRpc.instance?.Register<ZPackage>(DataRpc, RPC_Data);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        foreach (Texture2D texture in textures.Values)
        {
            Destroy(texture);
        }

        if (ownTexture != null)
        {
            Destroy(ownTexture);
        }
    }

    private void LoadOwn()
    {
        PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
        if (profile == null || !PortraitSettings.Enabled.Value
            || !PortraitStore.TryLoadOwn(profile.GetPlayerID(), out _, out string hash, out byte[] data))
        {
            return;
        }

        ownHash = hash;
        ownData = data;
        ownTexture = PortraitStore.ToTexture(PortraitStore.Decompress(data));
    }

    // Now and then looks who is in the world and greets those who are new with the hash of our portrait. A player
    // waiting to respawn has no character but is still there.
    private void Update()
    {
        if (Time.time < nextScan || ZNet.instance == null || ZRoutedRpc.instance == null)
        {
            return;
        }

        nextScan = Time.time + ScanInterval;
        long self = ZDOMan.GetSessionID();
        HashSet<long> present = new();
        foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
        {
            long uid = player.m_characterID.UserID;

            // A player waiting to respawn has no character (vanilla sets ZDOID.None) but has not left.
            if (uid == 0L)
            {
                if (player.m_name != null && uidByName.TryGetValue(player.m_name, out long known))
                {
                    present.Add(known);
                }

                continue;
            }

            if (uid == self)
            {
                continue;
            }

            if (player.m_name != null)
            {
                uidByName[player.m_name] = uid;
            }

            present.Add(uid);
            if (ownHash != null && announced.Add(uid))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(uid, HashRpc, ownHash);
            }
        }

        // Players who left are forgotten, so they are greeted again if they come back. A greeting can arrive before the
        // player list shows the sender, so only players seen in an earlier scan count as gone.
        foreach (long gone in seen.Where(uid => !present.Contains(uid)))
        {
            if (peerHashes.TryGetValue(gone, out string hash))
            {
                requested.Remove(hash);
            }

            peerHashes.Remove(gone);
            announced.Remove(gone);
            heard.Remove(gone);
            lastReply.Remove(gone);
            lastData.Remove(gone);
            foreach (string name in uidByName.Where(entry => entry.Value == gone).Select(entry => entry.Key).ToList())
            {
                uidByName.Remove(name);
            }
        }

        seen = present;
    }

    // Hears another player's portrait hash, and fetches the portrait only if it is neither loaded nor in the local
    // cache, so portraits are sent once and not every session.
    private void RPC_Hash(long sender, string hash)
    {
        if (!PortraitStore.IsHash(hash))
        {
            return;
        }

        // A player who joined after our last scan may not have been listening yet; answer the first greeting.
        if (heard.Add(sender) && ownHash != null)
        {
            announced.Add(sender);
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, HashRpc, ownHash);
        }

        peerHashes[sender] = hash;
        if (textures.ContainsKey(hash))
        {
            PortraitPins.Refresh();
            return;
        }

        byte[] cached = PortraitStore.LoadCached(hash);
        if (cached != null)
        {
            textures[hash] = PortraitStore.ToTexture(cached);
            PortraitPins.Refresh();
        }
        else if (requested.Add(hash))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, RequestRpc);
        }
    }

    private void RPC_Request(long sender)
    {
        if (ownData == null || !CooledDown(lastReply, sender))
        {
            return;
        }

        ZPackage package = new();
        package.Write(ownHash);
        package.Write(ownData);
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, DataRpc, package);
    }

    // Takes a portrait sent by another player, only if it is the one they announced and its contents match the hash,
    // and caches it on disk.
    private void RPC_Data(long sender, ZPackage package)
    {
        if (!CooledDown(lastData, sender) || !peerHashes.TryGetValue(sender, out string expected))
        {
            return;
        }

        try
        {
            string hash = package.ReadString();
            byte[] data = package.ReadByteArray();
            if (hash != expected || textures.ContainsKey(hash))
            {
                return;
            }

            byte[] raw = PortraitStore.Decompress(data);
            if (raw == null || PortraitStore.Hash(raw) != hash)
            {
                Jotunn.Logger.LogWarning($"Portrait: ignored an invalid portrait from {sender}.");
                return;
            }

            PortraitStore.SaveCached(hash, data);
            textures[hash] = PortraitStore.ToTexture(raw);
            PortraitPins.Refresh();
        }
        catch (Exception)
        {
            Jotunn.Logger.LogWarning($"Portrait: ignored a malformed portrait from {sender}.");
        }
    }

    private static bool CooledDown(Dictionary<long, float> last, long sender)
    {
        if (last.TryGetValue(sender, out float time) && Time.time - time < Cooldown)
        {
            return false;
        }

        last[sender] = Time.time;
        return true;
    }
}
