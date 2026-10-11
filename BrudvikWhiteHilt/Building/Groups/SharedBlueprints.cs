using BepInEx;
using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// Blueprints shared on the server, one set per world. Runs on the <see cref="Game"/> object of every machine: the
/// server keeps the shared blueprints as files in its config folder and answers requests; a client asks for the list,
/// shares one of its own blueprints, fetches a copy of a shared one into its own folder, or unshares one it shared.
/// The server checks every request: who sent it (see <see cref="RoutedRpcSender"/>), its size, its pieces, the
/// sharer's quota and pace. Only the player who shared a blueprint can unshare it.
/// </summary>
public class SharedBlueprints : MonoBehaviour
{
    private const string IndexRequestRpc = "WhiteHilt_BlueprintIndexRequest";
    private const string IndexRpc = "WhiteHilt_BlueprintIndex";
    private const string ShareRpc = "WhiteHilt_BlueprintShare";
    private const string FetchRpc = "WhiteHilt_BlueprintFetch";
    private const string DataRpc = "WhiteHilt_BlueprintData";
    private const string UnshareRpc = "WhiteHilt_BlueprintUnshare";
    private const string ResultRpc = "WhiteHilt_BlueprintResult";
    private const string Extension = ".blueprint";
    private const string CreatorIdTag = "#CreatorId:";
    private const int MaxNameLength = 60;
    private const float AnswerSeconds = 5f;

    // A packed blueprint unpacks to at most this many times its size, and never more than this in total.
    private const int UnpackFactor = 20;
    private const int MaxUnpacked = 8 * 1024 * 1024;

    private static readonly List<Entry> index = new();
    private static SharedBlueprints instance;
    private static float requestedAt = -1f;
    private static bool answered;

    private readonly Dictionary<string, float> lastShare = new();
    private Dictionary<string, Stored> stored;

    /// <summary>Raised on the client when the list of shared blueprints has changed.</summary>
    public static event Action Changed;

    /// <summary>The shared blueprints, as the server last told this client.</summary>
    public static IReadOnlyList<Entry> Index => index;

    /// <summary>True once the server has answered since the list was last asked for.</summary>
    public static bool Answered => answered;

    /// <summary>True while an answer is awaited and has not taken too long.</summary>
    public static bool Waiting => !answered && requestedAt >= 0f && Time.time - requestedAt < AnswerSeconds;

    /// <summary>
    /// Asks the server for the list of shared blueprints.
    /// </summary>
    public static void RequestIndex()
    {
        if (instance == null || ZRoutedRpc.instance == null)
        {
            return;
        }

        answered = false;
        requestedAt = Time.time;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), IndexRequestRpc, new ZPackage());
    }

    /// <summary>
    /// Shares one of the local player's blueprints. A blueprint of the same name shared before by the same player is
    /// replaced.
    /// </summary>
    /// <param name="blueprint">The blueprint.</param>
    public static void Share(Blueprint blueprint)
    {
        if (instance == null || ZRoutedRpc.instance == null || blueprint == null)
        {
            return;
        }

        byte[] packed = Utils.Compress(Encoding.UTF8.GetBytes(BlueprintStore.Format(blueprint)));
        if (packed.Length > SharedBlueprintSettings.MaxKilobytes.Value * 1024)
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$msg_whitehilt_blueprint_toobig");
            return;
        }

        ZPackage package = new();
        package.Write(blueprint.Name ?? string.Empty);
        package.Write(packed);
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), ShareRpc, package);
    }

    /// <summary>
    /// Fetches a copy of a shared blueprint into the local player's own blueprints.
    /// </summary>
    /// <param name="entry">The shared blueprint.</param>
    public static void Fetch(Entry entry)
    {
        if (instance != null && ZRoutedRpc.instance != null && entry != null)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), FetchRpc, entry.Id);
        }
    }

    /// <summary>
    /// Stops sharing a blueprint the local player shared.
    /// </summary>
    /// <param name="entry">The shared blueprint.</param>
    public static void Unshare(Entry entry)
    {
        if (instance != null && ZRoutedRpc.instance != null && entry != null)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), UnshareRpc, entry.Id);
        }
    }

    /// <summary>
    /// Cleans a name for a blueprint: one line, no control characters, not too long.
    /// </summary>
    /// <param name="name">The name as given.</param>
    /// <returns>The name to use.</returns>
    public static string CleanName(string name)
    {
        string clean = new((name ?? string.Empty).Where(character => !char.IsControl(character)).ToArray());
        clean = clean.Trim();
        if (clean.Length > MaxNameLength)
        {
            clean = clean.Substring(0, MaxNameLength).Trim();
        }

        return clean.Length > 0 ? clean : "Blueprint";
    }

    private void Start()
    {
        instance = this;
        index.Clear();
        answered = false;
        requestedAt = -1f;
        ZRoutedRpc.instance?.Register<ZPackage>(IndexRequestRpc, RPC_IndexRequest);
        ZRoutedRpc.instance?.Register<ZPackage>(IndexRpc, RPC_Index);
        ZRoutedRpc.instance?.Register<ZPackage>(ShareRpc, RPC_Share);
        ZRoutedRpc.instance?.Register<string>(FetchRpc, RPC_Fetch);
        ZRoutedRpc.instance?.Register<ZPackage>(DataRpc, RPC_Data);
        ZRoutedRpc.instance?.Register<string>(UnshareRpc, RPC_Unshare);
        ZRoutedRpc.instance?.Register<string, string>(ResultRpc, RPC_Result);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

    private static bool FromServer(long sender) => ZRoutedRpc.instance != null && sender == ZRoutedRpc.instance.GetServerPeerID();

    // The server's folder for this world: its name and id, so two worlds of the same name keep apart.
    private static string Folder()
    {
        string world = ZNet.instance.GetWorldName() ?? "world";
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            world = world.Replace(invalid, '_');
        }

        return Path.Combine(Paths.ConfigPath, "BrudvikWhiteHilt", "blueprints", "shared", $"{world}_{ZNet.instance.GetWorldUID()}");
    }

    private void RPC_IndexRequest(long sender, ZPackage package)
    {
        if (IsServer)
        {
            SendIndex(sender, RoutedRpcSender.Verify(sender, out _));
        }
    }

    // On the server: checks and saves a shared blueprint. The blueprint is read and written again, so the file holds
    // only what the parser understood, with the pieces this server does not know left out.
    private void RPC_Share(long sender, ZPackage package)
    {
        if (!IsServer)
        {
            return;
        }

        string identity = RoutedRpcSender.Verify(sender, out string creator);
        if (identity == null)
        {
            return;
        }

        if (!SharedBlueprintSettings.Enabled.Value)
        {
            Reply(sender, "msg_whitehilt_blueprint_disabled", string.Empty);
            return;
        }

        if (lastShare.TryGetValue(identity, out float last) && Time.time - last < SharedBlueprintSettings.ShareCooldownSeconds.Value)
        {
            Reply(sender, "msg_whitehilt_blueprint_wait", string.Empty);
            return;
        }

        string name = CleanName(package.ReadString());
        byte[] packed = package.ReadByteArray();
        int limit = SharedBlueprintSettings.MaxKilobytes.Value * 1024;
        byte[] text = packed != null && packed.Length <= limit ? BoundedGzip.Decompress(packed, Math.Min(MaxUnpacked, limit * UnpackFactor)) : null;
        if (text == null)
        {
            Reply(sender, "msg_whitehilt_blueprint_toobig", string.Empty);
            return;
        }

        Blueprint blueprint = BlueprintStore.ParseBlueprint(Encoding.UTF8.GetString(text).Split('\n'), name);
        blueprint.Pieces.RemoveAll(piece => GroupPlacer.Resolve(piece.Prefab) == null);
        if (blueprint.Pieces.Count == 0)
        {
            Reply(sender, "msg_whitehilt_blueprint_empty", string.Empty);
            return;
        }

        if (blueprint.Pieces.Count > SharedBlueprintSettings.MaxPieces.Value)
        {
            Reply(sender, "msg_whitehilt_blueprint_toobig", string.Empty);
            return;
        }

        LoadStored();
        Stored existing = stored.Values.FirstOrDefault(entry => entry.CreatorId == identity && string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        int max = SharedBlueprintSettings.MaxPerPlayer.Value;
        if (existing == null && stored.Values.Count(entry => entry.CreatorId == identity) >= max)
        {
            Reply(sender, "msg_whitehilt_blueprint_toomany", max.ToString());
            return;
        }

        blueprint.Name = name;
        blueprint.Creator = string.IsNullOrEmpty(creator) ? "?" : creator.Replace('\n', ' ');
        string id = existing?.Id ?? Guid.NewGuid().ToString("N").Substring(0, 12);
        string file = Path.Combine(Folder(), id + Extension);
        try
        {
            Directory.CreateDirectory(Folder());
            File.WriteAllText(file, CreatorIdTag + identity + "\n" + BlueprintStore.Format(blueprint));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Shared blueprints: could not write {file}: {ex.Message}");
            Reply(sender, "msg_whitehilt_blueprint_failed", string.Empty);
            return;
        }

        stored[id] = new Stored(id, name, blueprint.Creator, identity, blueprint.Pieces.Count, file);
        lastShare[identity] = Time.time;
        Jotunn.Logger.LogInfo($"Shared blueprints: {blueprint.Creator} shared \"{name}\" ({blueprint.Pieces.Count} pieces) as {id}");
        Reply(sender, "msg_whitehilt_blueprint_shared", name);
        BroadcastIndex();
    }

    private void RPC_Fetch(long sender, string id)
    {
        if (!IsServer)
        {
            return;
        }

        LoadStored();
        if (!SharedBlueprintSettings.Enabled.Value)
        {
            Reply(sender, "msg_whitehilt_blueprint_disabled", string.Empty);
            return;
        }

        if (id == null || !stored.TryGetValue(id, out Stored entry) || !File.Exists(entry.File))
        {
            Reply(sender, "msg_whitehilt_blueprint_gone", string.Empty);
            return;
        }

        // The sharer's platform id stays on the server.
        string text = string.Join("\n", File.ReadAllLines(entry.File).Where(line => !line.StartsWith(CreatorIdTag, StringComparison.Ordinal)));
        ZPackage reply = new();
        reply.Write(entry.Name);
        reply.Write(entry.Creator);
        reply.Write(Utils.Compress(Encoding.UTF8.GetBytes(text)));
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, DataRpc, reply);
    }

    private void RPC_Unshare(long sender, string id)
    {
        if (!IsServer)
        {
            return;
        }

        string identity = RoutedRpcSender.Verify(sender, out _);
        LoadStored();
        if (identity == null || id == null || !stored.TryGetValue(id, out Stored entry))
        {
            Reply(sender, "msg_whitehilt_blueprint_gone", string.Empty);
            return;
        }

        if (entry.CreatorId != identity)
        {
            Reply(sender, "msg_whitehilt_blueprint_notyours", string.Empty);
            return;
        }

        try
        {
            File.Delete(entry.File);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Shared blueprints: could not delete {entry.File}: {ex.Message}");
            Reply(sender, "msg_whitehilt_blueprint_failed", string.Empty);
            return;
        }

        stored.Remove(id);
        Reply(sender, "msg_whitehilt_blueprint_unshared", entry.Name);
        BroadcastIndex();
    }

    private void RPC_Index(long sender, ZPackage package)
    {
        if (!FromServer(sender))
        {
            return;
        }

        index.Clear();
        int count = package.ReadInt();
        for (int i = 0; i < count; i++)
        {
            index.Add(new Entry(package.ReadString(), package.ReadString(), package.ReadString(), package.ReadInt(), package.ReadBool()));
        }

        answered = true;
        Changed?.Invoke();
    }

    // On the client: saves the fetched copy among the player's own blueprints.
    private void RPC_Data(long sender, ZPackage package)
    {
        if (!FromServer(sender))
        {
            return;
        }

        string name = CleanName(package.ReadString());
        string creator = package.ReadString();
        byte[] text = BoundedGzip.Decompress(package.ReadByteArray(), MaxUnpacked);
        if (text == null)
        {
            return;
        }

        Blueprint blueprint = BlueprintStore.ParseBlueprint(Encoding.UTF8.GetString(text).Split('\n'), name);
        blueprint.Name = name;
        blueprint.Creator = creator;
        try
        {
            BlueprintStore.Save(blueprint);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Shared blueprints: could not save the fetched {name}: {ex.Message}");
            return;
        }

        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_blueprint_fetched"), name));
        BlueprintPanel.Reload();
    }

    private void RPC_Result(long sender, string token, string argument)
    {
        if (FromServer(sender) && token.StartsWith("msg_whitehilt_blueprint_", StringComparison.Ordinal))
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$" + token), argument));
        }
    }

    private static void Reply(long target, string token, string argument)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(target, ResultRpc, token, argument ?? string.Empty);
    }

    // Sends each player the list, with their own blueprints marked.
    private void BroadcastIndex()
    {
        foreach (ZNetPeer peer in ZNet.instance.GetPeers().Where(peer => peer.IsReady()))
        {
            SendIndex(peer.m_uid, RoutedRpcSender.IdentityOf(peer.m_uid));
        }

        if (!ZNet.instance.IsDedicated())
        {
            SendIndex(ZDOMan.GetSessionID(), RoutedRpcSender.IdentityOf(ZDOMan.GetSessionID()));
        }
    }

    private void SendIndex(long target, string identity)
    {
        LoadStored();
        ZPackage package = new();
        List<Stored> entries = stored.Values.OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        package.Write(entries.Count);
        foreach (Stored entry in entries)
        {
            package.Write(entry.Id);
            package.Write(entry.Name);
            package.Write(entry.Creator);
            package.Write(entry.Pieces);
            package.Write(identity != null && entry.CreatorId == identity);
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(target, IndexRpc, package);
    }

    // Reads the world's shared blueprints from disk the first time they are needed.
    private void LoadStored()
    {
        if (stored != null)
        {
            return;
        }

        stored = new Dictionary<string, Stored>();
        string folder = Folder();
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (string file in Directory.GetFiles(folder, "*" + Extension))
        {
            try
            {
                string[] lines = File.ReadAllLines(file);
                string creatorId = lines.FirstOrDefault(line => line.StartsWith(CreatorIdTag, StringComparison.Ordinal))?.Substring(CreatorIdTag.Length).Trim();
                string id = Path.GetFileNameWithoutExtension(file);
                Blueprint blueprint = BlueprintStore.ParseBlueprint(lines, id);
                stored[id] = new Stored(id, blueprint.Name, blueprint.Creator, creatorId ?? string.Empty, blueprint.Pieces.Count, file);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Shared blueprints: could not read {file}: {ex.Message}");
            }
        }

        Jotunn.Logger.LogInfo($"Shared blueprints: {stored.Count} in {folder}");
    }

    /// <summary>
    /// A shared blueprint as a client sees it.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>
        /// Creates an entry.
        /// </summary>
        /// <param name="id">The server's id for it.</param>
        /// <param name="name">Its name.</param>
        /// <param name="creator">Who shared it.</param>
        /// <param name="pieces">How many pieces it has.</param>
        /// <param name="mine">Whether the local player shared it.</param>
        public Entry(string id, string name, string creator, int pieces, bool mine)
        {
            Id = id;
            Name = name;
            Creator = creator;
            Pieces = pieces;
            Mine = mine;
        }

        /// <summary>The server's id for it.</summary>
        public string Id { get; }

        /// <summary>Its name.</summary>
        public string Name { get; }

        /// <summary>Who shared it.</summary>
        public string Creator { get; }

        /// <summary>How many pieces it has.</summary>
        public int Pieces { get; }

        /// <summary>Whether the local player shared it.</summary>
        public bool Mine { get; }
    }

    // A shared blueprint as the server keeps it.
    private sealed class Stored
    {
        public Stored(string id, string name, string creator, string creatorId, int pieces, string file)
        {
            Id = id;
            Name = name;
            Creator = creator;
            CreatorId = creatorId;
            Pieces = pieces;
            File = file;
        }

        public string Id { get; }

        public string Name { get; }

        public string Creator { get; }

        public string CreatorId { get; }

        public int Pieces { get; }

        public string File { get; }
    }
}
