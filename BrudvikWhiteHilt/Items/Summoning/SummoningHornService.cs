using BepInEx.Configuration;
using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Kraken;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Summoning;

/// <summary>Validates horn calls on the server and spawns the encounter there.</summary>
public class SummoningHornService : MonoBehaviour
{
    /// <summary>The horn's prefab name.</summary>
    public const string HornName = "WhiteHiltSummoningHorn";
    /// <summary>The synchronized start time of a horn call.</summary>
    public const string CallKey = "whitehilt_horn_call";
    /// <summary>Marks an encounter deliberately called by a horn.</summary>
    public const string SummonedKey = "whitehilt_horn_summoned";
    private const string RequestRpc = "WhiteHiltHornCall";
    private const string ReplyRpc = "WhiteHiltHornReply";
    private float nextCall;
    private readonly Dictionary<long, long> handledCalls = new();

    /// <summary>Whether the horn can summon encounters.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }
    /// <summary>World-wide delay between successful calls, in seconds.</summary>
    public static ConfigEntry<float> CooldownSeconds { get; private set; }
    /// <summary>Length of a horn call, in seconds.</summary>
    public static ConfigEntry<float> BlowSeconds { get; private set; }
    /// <summary>Distance within which an existing encounter blocks a call.</summary>
    public static ConfigEntry<float> EncounterRange { get; private set; }
    /// <summary>Distance over which the horn is audible.</summary>
    public static ConfigEntry<float> SoundRange { get; private set; }
    /// <summary>Allowed network delay after a completed horn call.</summary>
    public static ConfigEntry<float> NetworkGraceSeconds { get; private set; }

    /// <summary>Binds the server-synchronized settings.</summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "Enabled", true, "Allow the horn to summon Kraken and biome beasts.");
        CooldownSeconds = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "CooldownSeconds", 300f, "World-wide delay between successful horn calls.", new AcceptableValueRange<float>(1f, 86400f));
        BlowSeconds = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "BlowSeconds", 5f, "Seconds of uninterrupted blowing before a summon.", new AcceptableValueRange<float>(2f, 10f));
        EncounterRange = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "EncounterRange", 150f, "A nearby black beast or Kraken prevents another call.", new AcceptableValueRange<float>(25f, 1000f));
        SoundRange = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "SoundRange", 150f, "Maximum distance at which the horn can be heard.", new AcceptableValueRange<float>(10f, 500f));
        NetworkGraceSeconds = WhiteHiltConfig.BindAdminOnly("SummoningHorn", "NetworkGraceSeconds", 5f, "Allowed network delay after finishing a horn call.", new AcceptableValueRange<float>(1f, 30f));
    }

    /// <summary>Requests an encounter after the local player finishes blowing.</summary>
    public static void Request()
    {
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc);
    }

    private void Start()
    {
        ZRoutedRpc.instance.Register(RequestRpc, RPC_Call);
        ZRoutedRpc.instance.Register<string>(ReplyRpc, RPC_Reply);
    }

    private void RPC_Call(long sender)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || !Enabled.Value)
            return;
        Player player = Player.GetAllPlayers().FirstOrDefault(candidate => candidate.m_nview != null
            && candidate.m_nview.IsValid() && candidate.m_nview.GetZDO().GetOwner() == sender);
        if (player == null || player.IsDead() || player.InInterior())
            return;
        ZDO zdo = player.m_nview.GetZDO();
        if (zdo.GetInt(ZDOVars.s_rightItem) != HornName.GetStableHashCode())
            return;
        long started = zdo.GetLong(CallKey);
        double elapsed = (ZNet.instance.GetTime().Ticks - started) / (double)System.TimeSpan.TicksPerSecond;
        if (started == 0 || elapsed < BlowSeconds.Value || elapsed > BlowSeconds.Value + NetworkGraceSeconds.Value
            || (handledCalls.TryGetValue(sender, out long previous) && started <= previous))
            return;
        handledCalls[sender] = started;
        if (Time.time < nextCall)
        {
            Reply(sender, "$whitehilt_horn_wait");
            return;
        }
        if (Character.GetAllCharacters().Any(character => character != null && !character.IsDead()
            && Vector3.Distance(character.transform.position, player.transform.position) < EncounterRange.Value
            && (character.GetComponent<KrakenBody>() != null || character.GetComponent<BeastBehaviour>() != null)))
        {
            Reply(sender, "$whitehilt_horn_nearby");
            return;
        }
        Heightmap.Biome biome = Heightmap.FindBiome(player.transform.position);
        bool spawned;
        if (biome == Heightmap.Biome.Ocean)
        {
            Ship ship = Ship.Instances.OfType<Ship>().FirstOrDefault(candidate => candidate.m_players.Contains(player));
            if (ship == null || !KrakenSettings.Enabled.Value || !KrakenSpawner.DeepEnough(ship.transform.position, KrakenSettings.MinDepth.Value))
            {
                Reply(sender, "$whitehilt_horn_ship");
                return;
            }
            if (!HasKey(KrakenSettings.RequiredKey.Value))
            {
                Reply(sender, "$whitehilt_horn_locked");
                return;
            }
            spawned = KrakenSpawner.Spawn(ship, summoned: true);
        }
        else
        {
            var candidates = BeastDefinition.Candidates(biome, false, false)
                .Where(beast => DifficultySettings.BeastsEnabled.Value && DifficultySettings.IsBeastEnabled(beast.Key)).ToList();
            if (candidates.Count == 0)
            {
                Reply(sender, "$whitehilt_horn_empty");
                return;
            }
            var unlocked = candidates.Where(beast => HasKey(beast.BossKey)).ToList();
            if (unlocked.Count == 0)
            {
                Reply(sender, "$whitehilt_horn_locked");
                return;
            }
            spawned = BeastSpawner.Spawn(player, unlocked[Random.Range(0, unlocked.Count)], summoned: true);
        }
        if (spawned)
            nextCall = Time.time + CooldownSeconds.Value;
        Reply(sender, spawned ? "$whitehilt_horn_answer" : "$whitehilt_horn_space");
    }

    private static bool HasKey(string key) => string.IsNullOrEmpty(key) || ZoneSystem.instance.GetGlobalKey(key);

    private static void Reply(long sender, string text) => ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, text);

    private static void RPC_Reply(long sender, string text)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, text);
    }
}