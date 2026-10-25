using BrudvikWhiteHilt.Darkness;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.SeaBattles;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Brings Ragnarok's host to the local player while it rages, outside its eye: the dead rise from the earth now and
/// then, close by; Surt's host comes for a base the player is in; Fenrir is loosed on one who stays out in the open,
/// and is heard howling as he comes; and Naglfar, the ship of nails, sails out of the storm for a ship at sea, or runs
/// ashore by a player on the coast and its dead wade out of the sea. Each client brings them for its own player and
/// owns them; how many of a kind are already near keeps a party together from drawing more. On the
/// <see cref="Game"/> object.
/// </summary>
public class RagnarokSpawner : MonoBehaviour
{
    private const float TickSeconds = 1f;
    private const int Attempts = 30;
    private const float BaseMargin = 4f;
    private const float DeadMin = 16f;
    private const float DeadMax = 30f;
    private const float DeadNear = 50f;
    private const int DeadCap = 8;
    private const float SurtMin = 32f;
    private const float SurtMax = 48f;
    private const float SurtNear = 70f;
    private const int SurtCap = 6;
    private const float FenrirMin = 70f;
    private const float FenrirMax = 90f;
    private const float FenrirSeen = 25f;
    private const float FenrirHeard = 160f;
    private const float CoastReach = 45f;
    private const float NaglfarDelay = 20f;

    private int storm;
    private float nextTick;
    private float nextDead;
    private float nextSurt;
    private float openSeconds;
    private float nextHowl;
    private bool fenrirLoosed;
    private bool fenrirHunting;
    private bool naglfarCame;
    private bool toldDead;
    private float ragingSince;

    private void Update()
    {
        if (Time.time < nextTick)
        {
            return;
        }

        float seconds = TickSeconds;
        nextTick = Time.time + TickSeconds;
        Player player = Player.m_localPlayer;
        if (StormState.Number != storm)
        {
            storm = StormState.Number;
            fenrirLoosed = false;
            naglfarCame = false;
            toldDead = false;
            openSeconds = 0f;
            ragingSince = -1f;
        }

        bool raging = player != null && !player.IsDead() && StormSettings.Enabled.Value && StormState.Ragnarok && StormState.Raging
            && !StormState.InEye && StormSettings.HasStorms(player.GetCurrentBiome()) && !player.InInterior();
        if (!raging)
        {
            openSeconds = 0f;
            fenrirHunting = false;
            return;
        }

        if (ragingSince < 0f)
        {
            ragingSince = Time.time;
            nextDead = Time.time + Random.Range(10f, 25f);
            nextSurt = Time.time + Random.Range(30f, 60f);
        }

        if (StormSettings.RagnarokDead.Value && Time.time >= nextDead)
        {
            nextDead = Time.time + StormSettings.RagnarokDeadSeconds.Value * Random.Range(0.75f, 1.25f);
            RaiseDead(player);
        }

        if (StormSettings.RagnarokSurt.Value && Time.time >= nextSurt)
        {
            nextSurt = Time.time + StormSettings.RagnarokSurtSeconds.Value * Random.Range(0.75f, 1.25f);
            BringSurt(player);
        }

        if (StormSettings.RagnarokFenrir.Value)
        {
            Fenrir(player, seconds);
        }

        if (StormSettings.RagnarokNaglfar.Value && !naglfarCame && Time.time - ragingSince >= NaglfarDelay)
        {
            Naglfar(player);
        }
    }

    // Two or three of the dead claw their way up close by; a draugr now and then an archer or an elite.
    private void RaiseDead(Player player)
    {
        Vector3 origin = player.transform.position;
        if (RagnarokHost.CountNear(RagnarokHostKind.Dead, origin, DeadNear) >= DeadCap)
        {
            return;
        }

        int count = Random.Range(2, 4);
        int raised = 0;
        for (int i = 0; i < count; i++)
        {
            if (TryFindGround(origin, DeadMin, DeadMax, out Vector3 point) && Spawn(DeadPrefab(), point, origin, "vfx_MudDestroyed"))
            {
                raised++;
            }
        }

        if (raised > 0 && !toldDead)
        {
            toldDead = true;
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ragnarok_dead");
        }
    }

    private static string DeadPrefab()
    {
        float roll = Random.value;
        return roll < 0.12f ? RagnarokRegistry.DraugrElite : roll < 0.4f ? RagnarokRegistry.DraugrArcher : RagnarokRegistry.Draugr;
    }

    // Surt's host comes out of the fire for a base: two warriors and an archer, just outside it.
    private static void BringSurt(Player player)
    {
        Vector3 origin = player.transform.position;
        if (EffectArea.IsPointInsideArea(origin, EffectArea.Type.PlayerBase, 0f) == null
            || RagnarokHost.CountNear(RagnarokHostKind.Surt, origin, SurtNear) >= SurtCap
            || !TryFindGround(origin, SurtMin, SurtMax, out Vector3 point))
        {
            return;
        }

        int brought = 0;
        foreach (string prefab in new[] { RagnarokRegistry.SurtWarrior, RagnarokRegistry.SurtWarrior, RagnarokRegistry.SurtArcher })
        {
            Vector2 offset = Random.insideUnitCircle * 3f;
            Vector3 at = point + new Vector3(offset.x, 0f, offset.y);
            if (ZoneSystem.instance.GetSolidHeight(at, out float height))
            {
                at.y = height + 0.2f;
                brought += Spawn(prefab, at, origin, "vfx_RockDestroyed_large") ? 1 : 0;
            }
        }

        if (brought > 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ragnarok_surt");
            StormAmbience.PlayEffect("sfx_storm_quake");
        }
    }

    // Out in the open too long, and Fenrir is loosed; he is heard as he comes, and loses the scent at a roof or a fire.
    private void Fenrir(Player player, float seconds)
    {
        bool sheltered = DeepNorthNight.Sheltered(player);
        openSeconds = sheltered ? 0f : openSeconds + seconds;
        RagnarokHost fenrir = RagnarokHost.NearestFenrir(player.transform.position, FenrirHeard);
        if (fenrir != null)
        {
            float distance = Vector3.Distance(fenrir.transform.position, player.transform.position);
            if (distance > FenrirSeen && Time.time >= nextHowl)
            {
                nextHowl = Time.time + Random.Range(8f, 14f);
                Howl(player, fenrir.transform.position);
            }

            if (sheltered && fenrirHunting)
            {
                player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_ragnarok_fenrir_lost");
            }

            fenrirHunting = !sheltered;
            return;
        }

        fenrirHunting = false;
        if (fenrirLoosed || openSeconds < StormSettings.RagnarokFenrirSeconds.Value
            || RagnarokHost.NearestFenrir(player.transform.position, 400f) != null
            || !TryFindGround(player.transform.position, FenrirMin, FenrirMax, out Vector3 point))
        {
            return;
        }

        if (Spawn(RagnarokRegistry.Fenrir, point, player.transform.position, null))
        {
            fenrirLoosed = true;
            fenrirHunting = true;
            nextHowl = Time.time + 2f;
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ragnarok_fenrir");
            if (GameCamera.instance != null)
            {
                GameCamera.instance.AddShake(player.transform.position, 100f, 1.5f, false);
            }
        }
    }

    // A howl 20 m off towards him, so it is heard from where he comes.
    private static void Howl(Player player, Vector3 from)
    {
        GameObject howl = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("sfx_wolf_haul") : null;
        if (howl != null)
        {
            Vector3 toward = (from - player.transform.position).normalized;
            Instantiate(howl, player.transform.position + toward * 20f, Quaternion.identity);
        }
    }

    // Naglfar sails out of the storm for a ship at sea, or runs ashore by one on the coast.
    private void Naglfar(Player player)
    {
        Ship ship = Ship.GetLocalShip();
        if (ship != null)
        {
            if (ship.m_nview != null && ship.m_nview.IsValid() && ship.m_nview.IsOwner() && !RaiderShip.Is(ship) && RaiderShip.Hunting == 0
                && RaiderShip.SpawnFleet(ship, 0, null, Random.Range(2, 4)) != null)
            {
                naglfarCame = true;
                player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ragnarok_naglfar");
                StormAmbience.PlayEffect("sfx_storm_gjallarhorn");
            }

            return;
        }

        if (!TryFindShore(player.transform.position, out Vector3 shore))
        {
            return;
        }

        naglfarCame = true;
        for (int i = 0; i < 4; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 4f;
            Vector3 at = shore + new Vector3(offset.x, 0f, offset.y);
            at.y = Mathf.Max(ZoneSystem.instance.m_waterLevel, ZoneSystem.instance.GetGroundHeight(at)) + 0.2f;
            Spawn(DeadPrefab(), at, player.transform.position, "vfx_water_surface");
        }

        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ragnarok_naglfar_ashore");
    }

    // The water's edge between the player and the sea: from a point out in deep water, back towards the player to the
    // first shallows.
    private static bool TryFindShore(Vector3 origin, out Vector3 shore)
    {
        ZoneSystem zones = ZoneSystem.instance;
        float water = zones.m_waterLevel;
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            Vector3 direction = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            if (zones.GetGroundHeight(origin + direction * CoastReach) > water - 2f)
            {
                continue;
            }

            for (float distance = CoastReach; distance > 8f; distance -= 2f)
            {
                Vector3 point = origin + direction * distance;
                if (zones.GetGroundHeight(point) > water - 0.6f)
                {
                    shore = point;
                    return true;
                }
            }
        }

        shore = Vector3.zero;
        return false;
    }

    // Dry ground at a distance from the player, outside any base.
    private static bool TryFindGround(Vector3 origin, float min, float max, out Vector3 point)
    {
        ZoneSystem zones = ZoneSystem.instance;
        float water = zones.m_waterLevel;
        for (int i = 0; i < Attempts; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            point = origin + new Vector3(direction.x, 0f, direction.y) * Random.Range(min, max);
            if (!zones.GetSolidHeight(point, out float height) || height < water + 0.5f)
            {
                continue;
            }

            point.y = height + 0.2f;
            if (EffectArea.IsPointInsideArea(point, EffectArea.Type.PlayerBase, BaseMargin) == null)
            {
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }

    // One of the host, facing the player and hunting, with a burst where it comes up.
    private static bool Spawn(string name, Vector3 point, Vector3 facing, string effect)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
        if (prefab == null)
        {
            Jotunn.Logger.LogWarning($"Ragnarok: prefab {name} not found");
            return false;
        }

        Vector3 look = facing - point;
        look.y = 0f;
        Quaternion rotation = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity;
        Difficulty.CreatureStars.BeginSpawn(true);
        GameObject instance;
        try
        {
            instance = Instantiate(prefab, point, rotation);
        }
        finally
        {
            Difficulty.CreatureStars.EndSpawn();
        }

        Character character = instance.GetComponent<Character>();
        RagnarokHost.Mark(character);
        if (character != null && character.GetBaseAI() is BaseAI ai)
        {
            ai.SetHuntPlayer(true);
        }

        if (name == RagnarokRegistry.Fenrir && character != null)
        {
            character.SetLevel(1);
            ZDO zdo = character.m_nview.GetZDO();
            zdo.Set(Difficulty.CreatureStars.DamageKey, StormSettings.RagnarokFenrirDamage.Value);
            ArrivalSound.Mark(zdo);
        }

        GameObject burst = effect != null && ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(effect) : null;
        if (burst != null)
        {
            Instantiate(burst, point, Quaternion.identity);
        }

        return true;
    }
}
