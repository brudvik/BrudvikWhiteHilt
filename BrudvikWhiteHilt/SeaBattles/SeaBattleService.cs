using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Summoning;
using BrudvikWhiteHilt.Kraken;
using BrudvikWhiteHilt.Pieces.Ships;
using BrudvikWhiteHilt.SeaBattles.Escorts;
using BrudvikWhiteHilt.SeaBattles.Fleets;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// Runs on the <see cref="Game"/> object and decides when a Nail Ship comes. The client that owns a ship at sea asks the
/// server once a minute while every condition holds; the server checks the request again, rolls the chance, keeps the
/// world-wide cooldown, picks the crew from the global keys and the fleet size from the fleet tiers; the client then raises
/// the ship, or the whole fleet. Admins can call one with whitehilt_seabattle summon, or a fleet with fleet, which skip the
/// chance and the cooldown. Every number is a setting.
/// </summary>
public class SeaBattleService : MonoBehaviour
{
    private const string RequestRpc = "WhiteHiltSeaBattleRequest";
    private const string SpawnRpc = "WhiteHiltSeaBattleSpawn";
    private const string AdminRpc = "WhiteHiltSeaBattleAdmin";
    private const string FleetRpc = "WhiteHiltSeaBattleFleet";
    private const string ReplyRpc = "WhiteHiltSeaBattleReply";
    private const string SightedRpc = "WhiteHiltSeaBattleSighted";
    private const float CheckInterval = 60f;

    // The server only believes a request from a player this close to the ship it names, and counts the players aboard
    // from those this close to it.
    internal const float AboardRange = 60f;

    // The sighting is shown at most this often, so a client that repeats it cannot flood the horn.
    private const float SightedInterval = 5f;

    // A sea whose weakest wind is this strong is a storm, and one whose day fog is this thick is foggy.
    private const float StormWind = 0.8f;
    private const float FogDensity = 0.01f;

    private float nextCheck;
    private float lastEncounter = float.NegativeInfinity;
    private float lastSighted = float.NegativeInfinity;
    private Minimap.PinData sightingPin;
    private float sightingPinUntil;

    /// <summary>
    /// Asks the server, as an admin, to send a Nail Ship after the ship the local player is aboard now.
    /// </summary>
    /// <param name="fireShip">Whether a fire ship comes along.</param>
    /// <param name="ram">Whether the Nail Ship carries a ram.</param>
    public static void Summon(bool fireShip = false, bool ram = false)
    {
        Ship ship = Ship.GetLocalShip();
        if (ZRoutedRpc.instance == null || ship == null || ship.m_nview == null || !ship.m_nview.IsValid() || RaiderShip.Is(ship) || TradeShip.Is(ship))
        {
            Print("Board a ship at sea first.");
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), AdminRpc, ship.m_nview.GetZDO().m_uid,
            EncounterRules.PackShips(1, fireShip, ram));
    }

    /// <summary>
    /// Asks the server, as an admin, to send a whole fleet of Nail Ships after the ship the local player is aboard now.
    /// </summary>
    /// <param name="size">The number of ships, 2 or 3.</param>
    /// <param name="fireShip">Whether a fire ship comes along.</param>
    /// <param name="ram">Whether the flagship carries a ram.</param>
    public static void SummonFleet(int size, bool fireShip = false, bool ram = false)
    {
        Ship ship = Ship.GetLocalShip();
        if (ZRoutedRpc.instance == null || ship == null || ship.m_nview == null || !ship.m_nview.IsValid() || RaiderShip.Is(ship) || TradeShip.Is(ship))
        {
            Print("Board a ship at sea first.");
            return;
        }

        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), FleetRpc, ship.m_nview.GetZDO().m_uid,
            EncounterRules.PackShips(size, fireShip, ram));
    }

    /// <summary>
    /// Describes the conditions for a Nail Ship where the local player is, for the console.
    /// </summary>
    /// <returns>One line per condition.</returns>
    public static string Describe()
    {
        Ship ship = Ship.GetLocalShip();
        Vector3 position = ship != null ? ship.transform.position : Vector3.zero;
        int aboard = ship != null ? ship.m_players.Count : 0;
        string key = SeaBattleSettings.RequiredKey.Value;
        float land = ship != null ? NearestLand(position) : float.PositiveInfinity;
        float chance = EncounterRules.AttackChance(SeaBattleSettings.ChancePerMinute.Value, SeaBattleSettings.ChancePerExtraPlayer.Value, aboard);
        int weather = LocalWeather();
        chance = Mathf.Min(100f, chance * EncounterRules.WeatherFactor(weather, SeaBattleSettings.ChanceAtNight.Value, SeaBattleSettings.ChanceInStorm.Value,
            SeaBattleSettings.ChanceInFog.Value));
        return $"Nail Ship: enabled {SeaBattleSettings.Enabled.Value}\n" +
            $"  on a ship: {ship != null}, own the ship: {ship != null && ship.m_nview.IsOwner()}, a Nail Ship itself: {RaiderShip.Is(ship)}\n" +
            $"  players aboard: {aboard}, chance per minute: {chance:0.##}% (night {(weather & EncounterRules.WeatherNight) != 0}, " +
            $"storm {(weather & EncounterRules.WeatherStorm) != 0}, fog {(weather & EncounterRules.WeatherFog) != 0})\n" +
            $"  ocean: {ship != null && IsOcean(position)}, deep ({SeaBattleSettings.MinDepth.Value} m): {ship != null && DeepWater(position.x, position.z, SeaBattleSettings.MinDepth.Value)}\n" +
            $"  nearest land: {(float.IsPositiveInfinity(land) ? "none in range" : land.ToString("0") + " m")} (needs {SeaBattleSettings.MinShoreDistance.Value} m clear)\n" +
            $"  key {(string.IsNullOrEmpty(key) ? "(none)" : key)}: {HasKey(key)}\n" +
            $"  Nail Ships loaded: {RaiderShip.Loaded}, Krakens loaded: {KrakenBody.Instances.Count}";
    }

    /// <summary>
    /// Whether the sea is deeper than a depth at a place, from the world generator. Unlike a ray at the seabed this works
    /// for a place where no zone is loaded, and on the server.
    /// </summary>
    /// <param name="x">World x.</param>
    /// <param name="z">World z.</param>
    /// <param name="depth">Least depth of water, in metres.</param>
    /// <returns>True when the seabed lies deeper than that.</returns>
    public static bool DeepWater(float x, float z, float depth)
    {
        return WorldGenerator.instance != null && ZoneSystem.instance != null
            && WorldGenerator.instance.GetHeight(x, z) < ZoneSystem.instance.m_waterLevel - depth;
    }

    /// <summary>
    /// Whether a point lies in the near zones this machine keeps its own objects alive in. A Nail Ship is not persistent:
    /// raised outside them, the game destroys it the moment it is made. How far that reaches is the simulation distance
    /// the server and this machine agree on, the lower of the two, so on a server it may be far less than alone.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="margin">Zones to keep clear of the edge, so a ship is not lost the moment the player moves on.</param>
    /// <returns>True when a ship raised there lives.</returns>
    public static bool InNearArea(Vector3 point, int margin)
    {
        if (ZNet.instance == null || ZoneSystem.instance == null)
        {
            return true;
        }

        SimulationDistance distance = ZNet.instance.GetSyncedSimulationDistance();
        Vector2s centre = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
        Vector2s zone = ZoneSystem.GetZone(point);
        return EncounterRules.InNearZones(zone.x - centre.x, zone.y - centre.y, distance.NearSimulationDistance - margin, distance.IsClassic);
    }

    internal static bool IsOcean(Vector3 position)
    {
        return WorldGenerator.instance != null && WorldGenerator.instance.GetBiome(position) == Heightmap.Biome.Ocean;
    }

    private static float NearestLand(Vector3 position)
    {
        return WorldGenerator.instance == null || ZoneSystem.instance == null ? float.PositiveInfinity
            : EncounterRules.NearestLand(position.x, position.z, SeaBattleSettings.MinShoreDistance.Value,
                (x, z) => WorldGenerator.instance.GetHeight(x, z), ZoneSystem.instance.m_waterLevel);
    }

    internal static bool HasKey(string key)
    {
        return string.IsNullOrEmpty(key) || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key));
    }

    // The weather where the local player is, as flags for an encounter request: night, a storm (by the environment's name
    // or its strong wind) and fog or mist (by its name or thick fog). The server only reads it to raise the chance.
    private static int LocalWeather()
    {
        int weather = EnvMan.instance != null && EnvMan.IsNight() ? EncounterRules.WeatherNight : 0;
        EnvSetup environment = EnvMan.instance != null ? EnvMan.instance.GetCurrentEnvironment() : null;
        if (environment == null)
        {
            return weather;
        }

        string name = environment.m_name ?? string.Empty;
        if (name.IndexOf("storm", System.StringComparison.OrdinalIgnoreCase) >= 0 || environment.m_windMin >= StormWind)
        {
            weather |= EncounterRules.WeatherStorm;
        }

        if (name.IndexOf("mist", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("fog", System.StringComparison.OrdinalIgnoreCase) >= 0
            || environment.m_fogDensityDay >= FogDensity)
        {
            weather |= EncounterRules.WeatherFog;
        }

        return weather;
    }

    // Whether a fleet is joined by a fire ship this time: the setting's chance, once the world has reached its key.
    internal static bool PickFireShip()
    {
        return Weapons.WeaponRules.KeyReached(Weapons.WeaponSettings.FireShipKey.Value, key => ZoneSystem.instance.GetGlobalKey(key))
            && Random.value < Weapons.WeaponSettings.FireShipChance.Value;
    }

    private static void Print(string message)
    {
        if (global::Console.instance != null)
        {
            global::Console.instance.Print(message);
        }
    }

    private void Start()
    {
        ZRoutedRpc.instance?.Register<ZDOID, int, int>(RequestRpc, RPC_Request);
        ZRoutedRpc.instance?.Register<ZDOID, int, int, int>(SpawnRpc, RPC_Spawn);
        ZRoutedRpc.instance?.Register<ZDOID, int>(AdminRpc, RPC_Admin);
        ZRoutedRpc.instance?.Register<ZDOID, int>(FleetRpc, RPC_Fleet);
        ZRoutedRpc.instance?.Register<string>(ReplyRpc, RPC_Reply);
        ZRoutedRpc.instance?.Register<ZDOID, int, Vector3>(SightedRpc, RPC_Sighted);
        nextCheck = Time.time + CheckInterval;
    }

    private void Update()
    {
        if (sightingPin != null && Time.time >= sightingPinUntil)
        {
            DropSightingPin();
        }

        if (Time.time < nextCheck || ZRoutedRpc.instance == null)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        Ship ship = Ship.GetLocalShip();
        if (Conditions(ship))
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RequestRpc, ship.m_nview.GetZDO().m_uid, ship.m_players.Count, LocalWeather());
        }
    }

    private void OnDestroy()
    {
        DropSightingPin();
    }

    // On the client that owns the ship: whether it is worth asking the server. The server checks the same things again,
    // as a modified client could ask at will; this only keeps the usual client from asking for nothing every minute.
    private static bool Conditions(Ship ship)
    {
        Player player = Player.m_localPlayer;
        if (!SeaBattleSettings.Enabled.Value || player == null || player.IsDead() || ship == null || ship.m_nview == null
            || !ship.m_nview.IsValid() || !ship.m_nview.IsOwner() || RaiderShip.Is(ship) || TradeShip.Is(ship) || RaiderShip.Hunting > 0
            || KrakenBody.Instances.Count > 0 || !ship.m_players.Contains(player))
        {
            return false;
        }

        Vector3 position = ship.transform.position;
        if (TradeShip.AnyNear(position, EscortSettings.NoEncounterRange.Value))
        {
            return false;
        }

        return EncounterRules.AttackChance(SeaBattleSettings.ChancePerMinute.Value, SeaBattleSettings.ChancePerExtraPlayer.Value, ship.m_players.Count) > 0f
            && HasKey(SeaBattleSettings.RequiredKey.Value) && OpenSea(position);
    }

    // Deep ocean with no land near: the same test on the client and on the server.
    private static bool OpenSea(Vector3 position)
    {
        return IsOcean(position) && DeepWater(position.x, position.z, SeaBattleSettings.MinDepth.Value)
            && float.IsPositiveInfinity(NearestLand(position));
    }

    // On the server: checks a request from a client and rolls for a Nail Ship. What the client says is only a claim: the
    // sender must be a real connection, standing near a ship that exists, that is no Nail Ship, in deep ocean away from
    // land, and the number of players aboard is counted from the players near it, not taken from the client. The weather
    // the client names only raises the chance, by at most the settings' factors; the server adds the night it knows itself.
    private void RPC_Request(long sender, ZDOID shipId, int playersAboard, int weather)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer() || !SeaBattleSettings.Enabled.Value
            || !HasKey(SeaBattleSettings.RequiredKey.Value) || !EncounterRules.CooldownOver(Time.time, lastEncounter, SeaBattleSettings.CooldownMinutes.Value))
        {
            return;
        }

        if (RoutedRpcSender.Verify(sender, out _) == null)
        {
            Jotunn.Logger.LogWarning($"Nail Ship: a request from {sender} could not be tied to a connection and was ignored");
            return;
        }

        ZDO ship = TargetShip(shipId);
        ZDO player = PlayerZdo(sender);
        if (ship == null || player == null || Vector3.Distance(player.GetPosition(), ship.GetPosition()) > AboardRange
            || !OpenSea(ship.GetPosition()))
        {
            return;
        }

        // A merchant on its way has the sea to itself: the escort has its own waves.
        if (EscortService.Quiet(ship.GetPosition()))
        {
            return;
        }

        int aboard = Mathf.Clamp(playersAboard, 1, Mathf.Max(1, PlayersNear(ship.GetPosition())));
        float chance = EncounterRules.AttackChance(SeaBattleSettings.ChancePerMinute.Value, SeaBattleSettings.ChancePerExtraPlayer.Value, aboard);
        weather |= EnvMan.instance != null && EnvMan.IsNight() ? EncounterRules.WeatherNight : 0;
        chance *= EncounterRules.WeatherFactor(weather, SeaBattleSettings.ChanceAtNight.Value, SeaBattleSettings.ChanceInStorm.Value,
            SeaBattleSettings.ChanceInFog.Value);
        if (!EncounterRules.Rolled(chance, Random.value))
        {
            return;
        }

        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        CrewTier tier = CrewTable.Pick(tiers, key => ZoneSystem.instance.GetGlobalKey(key));
        if (tier == null && string.IsNullOrEmpty(SeaBattleSettings.RequiredKey.Value) && tiers.Count > 0)
        {
            // No key required means from the start, before any tier's own key is set.
            tier = tiers[0];
        }

        if (tier == null)
        {
            return;
        }

        lastEncounter = Time.time;
        int bonus = EncounterRules.CrewBonus(aboard, SeaBattleSettings.CrewPerExtraPlayer.Value);
        int size = PickFleetSize();
        bool fireShip = PickFireShip();
        Jotunn.Logger.LogInfo($"Nail Ship: sending {(size > 1 ? $"a fleet of {size}" : "one")}{(fireShip ? " and a fire ship" : string.Empty)} after {sender}'s ship " +
            $"({aboard} aboard, tier {tier.Key}, {bonus} extra crew, chance {chance:0.##}%)");
        ZRoutedRpc.instance.InvokeRoutedRPC(SpawnPeer(ship, sender), SpawnRpc, shipId, tier.Index, bonus, EncounterRules.PackShips(size, fireShip));
    }

    // On the server: the size of the fleet for one encounter, from the fleet tiers and the world's keys. One ship when
    // fleets are off or no tier applies.
    internal static int PickFleetSize()
    {
        if (!FleetSettings.Enabled.Value)
        {
            return 1;
        }

        return FleetRules.PickSize(FleetRules.ParseTiers(FleetSettings.FleetTiers.Value), key => ZoneSystem.instance.GetGlobalKey(key), Random.value);
    }

    // On the server: an admin asks for a Nail Ship at once, with no roll and no cooldown; the order may bring a fire ship
    // and a ram.
    private void RPC_Admin(long sender, ZDOID shipId, int packed)
    {
        EncounterRules.UnpackShips(packed, out bool fireShip, out bool ram);
        SendForAdmin(sender, shipId, EncounterRules.PackShips(1, fireShip, ram));
    }

    // On the server: an admin asks for a fleet at once, with no roll and no cooldown.
    private void RPC_Fleet(long sender, ZDOID shipId, int packed)
    {
        if (ZNet.instance != null && ZNet.instance.IsServer() && IsAdmin(sender) && !FleetSettings.Enabled.Value)
        {
            Reply(sender, "Fleets are switched off in the settings ([SeaBattles.Fleets] Enabled).");
            return;
        }

        int size = EncounterRules.UnpackShips(packed, out bool fireShip, out bool ram);
        SendForAdmin(sender, shipId, EncounterRules.PackShips(Mathf.Clamp(size, 2, FleetRules.MaxSize), fireShip, ram));
    }

    // On the server: checks that the sender is an admin and orders the Nail Ship, or the fleet, at once. A world with no
    // crew key set gets the first tier, so the ship can be tried before any boss is slain.
    private void SendForAdmin(long sender, ZDOID shipId, int packed)
    {
        if (ZNet.instance == null || !ZNet.instance.IsServer())
        {
            return;
        }

        if (!IsAdmin(sender))
        {
            Reply(sender, "Only admins can do that.");
            return;
        }

        if (!SeaBattleSettings.Enabled.Value)
        {
            Reply(sender, "Nail Ships are switched off in the settings ([SeaBattles] Enabled).");
            return;
        }

        ZDO ship = TargetShip(shipId);
        if (ship == null)
        {
            Reply(sender, "Board a ship at sea first.");
            return;
        }

        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        CrewTier tier = CrewTable.Pick(tiers, key => ZoneSystem.instance.GetGlobalKey(key));
        string note = string.Empty;
        if (tier == null && tiers.Count > 0)
        {
            tier = tiers[0];
            note = " No crew key is set in this world, so the first tier is used.";
        }

        if (tier == null)
        {
            Reply(sender, "[SeaBattles] CrewTiers has no valid tier.");
            return;
        }

        lastEncounter = Time.time;
        int bonus = EncounterRules.CrewBonus(PlayersNear(ship.GetPosition()), SeaBattleSettings.CrewPerExtraPlayer.Value);
        int size = EncounterRules.UnpackShips(packed, out bool fireShip, out bool ram);
        ZRoutedRpc.instance.InvokeRoutedRPC(SpawnPeer(ship, sender), SpawnRpc, shipId, tier.Index, bonus, packed);
        Reply(sender, (size > 1 ? $"A fleet of {size} Nail Ships is sent." : "A Nail Ship is sent.")
            + (ram ? " It carries a ram." : string.Empty) + (fireShip ? " A fire ship comes along." : string.Empty) + note);
    }

    // Only the ship's owner may raise the Nail Ship beside it, and that need not be the one who asked.
    private static long SpawnPeer(ZDO ship, long sender)
    {
        long owner = ship.GetOwner();
        return owner != 0L ? owner : sender;
    }

    // On the client that was told to: raises the Nail Ship, or the fleet, ahead of its ship, with a fire ship when the order
    // brings one, and sounds the horn for everyone near. Only the server may order this, and the ship must still be one this
    // machine owns with the player aboard.
    private void RPC_Spawn(long sender, ZDOID shipId, int tierIndex, int crewBonus, int packed)
    {
        if (sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            return;
        }

        GameObject found = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(shipId) : null;
        Ship ship = found != null ? found.GetComponent<Ship>() : null;
        IReadOnlyList<CrewTier> tiers = CrewTable.Parse(SeaBattleSettings.CrewTiers.Value);
        if (ship == null || RaiderShip.Is(ship) || RaiderShip.Hunting > 0 || !ship.m_nview.IsValid() || !ship.m_nview.IsOwner()
            || tierIndex < 0 || tierIndex >= tiers.Count)
        {
            return;
        }

        int size = EncounterRules.UnpackShips(packed, out bool fireShip, out bool ram);
        RaiderShip lead = RaiderShip.SpawnFleet(ship, Mathf.Max(0, crewBonus), tiers[tierIndex], Mathf.Clamp(size, 1, FleetRules.MaxSize), fireShip, ram);
        if (lead != null)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, SightedRpc, shipId, lead.IsFlagship ? 2 : 1, lead.transform.position);
        }
    }

    // On every client: the horn of the Nail Ship is heard near the hunted ship, and those aboard it are told from which
    // bearing it comes; a fleet has its own message. The place it was sighted is pinned on their map for a while.
    private void RPC_Sighted(long sender, ZDOID shipId, int size, Vector3 position)
    {
        if (Time.time - lastSighted < SightedInterval || ZNetScene.instance == null)
        {
            return;
        }

        GameObject found = ZNetScene.instance.FindInstance(shipId);
        if (found == null)
        {
            return;
        }

        lastSighted = Time.time;
        HornCaller.PlayAt(found.transform.position, SeaBattleSettings.SpawnDistance.Value * 2f);
        Ship local = Ship.GetLocalShip();
        if (local == null || local.gameObject != found || Player.m_localPlayer == null)
        {
            return;
        }

        Vector2 from = new(local.transform.position.x, local.transform.position.z);
        float course = RaiderTactics.CourseTo(from, new Vector2(position.x, position.z), 0f);
        string bearing = EncounterRules.BearingNames[EncounterRules.Bearing(course - ShipAssist.Heading(local.transform))];
        string text = string.Format(Localization.instance.Localize(size > 1 ? "$msg_whitehilt_fleet_sighted" : "$msg_whitehilt_raider_sighted"),
            Localization.instance.Localize("$whitehilt_bearing_" + bearing));
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, text);
        PinSighting(position);
    }

    // A pin on the local map where the Nail Ships were sighted, taken off after the setting's seconds; none with 0.
    private void PinSighting(Vector3 position)
    {
        float seconds = SeaBattleSettings.SightingPinSeconds.Value;
        if (seconds <= 0f || Minimap.instance == null)
        {
            return;
        }

        DropSightingPin();
        sightingPin = Minimap.instance.AddPin(position, Minimap.PinType.RandomEvent, Translations.Word("whitehilt_raider_sighted_pin"), save: false, isChecked: false);
        sightingPinUntil = Time.time + seconds;
    }

    private void DropSightingPin()
    {
        if (sightingPin != null && Minimap.instance != null)
        {
            Minimap.instance.RemovePin(sightingPin);
        }

        sightingPin = null;
    }

    private static void RPC_Reply(long sender, string message)
    {
        if (sender == ZRoutedRpc.instance.GetServerPeerID())
        {
            Print(message);
        }
    }

    internal static void Reply(long sender, string message)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(sender, ReplyRpc, message);
    }

    // The network data of a ship the server knows: it exists and its prefab is a ship that is not a Nail Ship.
    internal static ZDO TargetShip(ZDOID id)
    {
        ZDO zdo = id.IsNone() || ZDOMan.instance == null ? null : ZDOMan.instance.GetZDO(id);
        if (zdo == null || NailShipRegistry.IsNailShip(zdo.GetPrefab()) || TradeShipRegistry.IsTradeShip(zdo.GetPrefab()))
        {
            return null;
        }

        GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
        return prefab != null && prefab.GetComponent<Ship>() != null ? zdo : null;
    }

    // The network data of the sender's player, as the horn service finds it: the host is its own session.
    internal static ZDO PlayerZdo(long sender)
    {
        ZNetPeer peer = ZNet.instance.GetPeer(sender);
        ZDOID playerId = peer != null ? peer.m_characterID
            : sender == ZDOMan.GetSessionID() && Player.m_localPlayer != null ? Player.m_localPlayer.GetZDOID() : ZDOID.None;
        ZDO zdo = playerId.IsNone() ? null : ZDOMan.instance.GetZDO(playerId);
        return zdo != null && zdo.GetOwner() == sender && zdo.GetFloat(ZDOVars.s_health, 1f) > 0f ? zdo : null;
    }

    internal static int PlayersNear(Vector3 position)
    {
        int count = 0;
        foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
        {
            ZDO zdo = info.m_characterID.IsNone() ? null : ZDOMan.instance.GetZDO(info.m_characterID);
            if (zdo != null && Vector3.Distance(zdo.GetPosition(), position) <= AboardRange)
            {
                count++;
            }
        }

        return count;
    }

    // Admins are known by their platform id, taken from the connection the RPC came in on, not from the sender id the
    // client wrote into it; the host is an admin.
    internal static bool IsAdmin(long sender)
    {
        string identity = RoutedRpcSender.Verify(sender, out _);
        if (identity == null)
        {
            return false;
        }

        return sender == ZRoutedRpc.instance.m_id || ZNet.instance.IsAdmin(identity);
    }
}
