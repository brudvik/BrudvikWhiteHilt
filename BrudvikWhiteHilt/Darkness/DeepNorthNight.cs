using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Storms;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// The night out in the Deep North, on the <see cref="Game"/> object of every machine with a player. Out in the open
/// the cold bites through any frost resistance and drains health and stamina until the player finds a fire, a roof or
/// a warm place; now and then a white storm sweeps in, with whispers on the wind and wolves howling in it; and a pack
/// of Black Wolves that lost the player to a fire finds them again once they are back out in the open. The fear of the
/// dark and its shadows follow the player out too (<see cref="DarknessClient"/>).
/// </summary>
public class DeepNorthNight : MonoBehaviour
{
    private const float TickSeconds = 0.5f;
    private const float HealthFloor = 0.1f;
    private const float PackHeard = 150f;
    private const float PackSeen = 25f;
    private const float HowlDistance = 20f;
    private const float ReturnCooldown = 90f;
    private const string Howl = "sfx_wolf_haul";
    private const string PackKey = "BlackWolf";

    private static long huntedNight = -1;

    private float nextTick;
    private float lastTick;
    private float owed;
    private long warnedNight = -1;
    private bool shownWhiteout;
    private float nextWhisper;
    private float nextHowl;
    private bool wasSheltered;
    private float openSeconds;
    private float nextReturn;

    /// <summary>
    /// Whether it is night, as the White Hilt clock tells it.
    /// </summary>
    /// <returns>True from 18:00 to 06:00.</returns>
    public static bool IsNight()
    {
        return DifficultyState.IsNightHour(DifficultyState.Hours());
    }

    /// <summary>
    /// Whether a player is out in the night of the Deep North: not below ground, and the night is worse there.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True out in the night of the Deep North.</returns>
    public static bool Out(Player player)
    {
        return player != null && DeepNorthNightSettings.Enabled != null && DeepNorthNightSettings.Enabled.Value && !player.InInterior()
            && WorldGenerator.instance != null && WorldGenerator.instance.GetBiome(player.transform.position) == Heightmap.Biome.DeepNorth
            && IsNight();
    }

    /// <summary>
    /// Whether a player is out of the cold and out of a pack's reach: below ground, by a fire or another warm place, or
    /// under a roof.
    /// </summary>
    /// <param name="player">The player, local or not.</param>
    /// <returns>True when sheltered.</returns>
    public static bool Sheltered(Player player)
    {
        if (player.InInterior() || EffectArea.IsPointInsideArea(player.transform.position, EffectArea.Type.Heat) != null)
        {
            return true;
        }

        Cover.GetCoverForPoint(player.GetCenterPoint(), out float cover, out bool underRoof);
        return underRoof && cover >= 0.8f;
    }

    /// <summary>
    /// Whether a white storm blows in a stretch of time. Every machine works it out from the world time alone, so
    /// everyone has the same storm.
    /// </summary>
    /// <param name="stretch">The number of the stretch of time.</param>
    /// <param name="share">Share of the stretches a storm blows in.</param>
    /// <returns>True when it blows.</returns>
    public static bool WhiteoutAt(long stretch, float share)
    {
        unchecked
        {
            uint hash = (uint)stretch * 2654435761u;
            hash ^= hash >> 16;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return hash % 1000u < share * 1000f;
        }
    }

    /// <summary>
    /// Whether a white storm blows in the Deep North now.
    /// </summary>
    /// <returns>True when it blows.</returns>
    public static bool Whiteout()
    {
        if (ZNet.instance == null || DeepNorthNightSettings.WhiteoutShare == null || DeepNorthNightSettings.WhiteoutShare.Value <= 0f || !IsNight())
        {
            return false;
        }

        long stretch = (long)(ZNet.instance.GetTimeSeconds() / (DeepNorthNightSettings.WhiteoutMinutes.Value * 60f));
        return WhiteoutAt(stretch, DeepNorthNightSettings.WhiteoutShare.Value);
    }

    /// <summary>
    /// The weather a white storm forces where the local player is, if any. Called after the game's own forced weathers
    /// and the storms'.
    /// </summary>
    /// <param name="env">The environment manager.</param>
    /// <returns>The weather's name, or null to leave the weather alone.</returns>
    public static string Override(EnvMan env)
    {
        return Out(Player.m_localPlayer) && Whiteout() ? StormWeather.Whiteout(env) : null;
    }

    /// <summary>
    /// Remembers that a pack came for the local player tonight, so it comes back when they leave the warmth.
    /// </summary>
    public static void MarkHunted()
    {
        huntedNight = DifficultyState.NightId();
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (player == null || ZNet.instance == null || DeepNorthNightSettings.Enabled == null || Time.time < nextTick)
        {
            return;
        }

        float seconds = Mathf.Clamp(Time.time - lastTick, 0f, 2f);
        lastTick = Time.time;
        nextTick = Time.time + TickSeconds;

        bool outside = Out(player) && !player.IsDead();
        bool sheltered = outside && Sheltered(player);
        Freeze(player, outside && !sheltered, seconds);
        Storm(player, outside);
        Track(player, outside, sheltered, seconds);
    }

    // Out in the open the cold drains health and stamina, frost resistance or not; the health is gathered until it is
    // at least 1, and it never takes the last tenth.
    private void Freeze(Player player, bool cold, float seconds)
    {
        BitingColdEffect.Show(player, cold);
        if (!cold || player.InGodMode())
        {
            owed = 0f;
            return;
        }

        long night = DifficultyState.NightId();
        if (warnedNight != night)
        {
            warnedNight = night;
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_north_cold");
        }

        player.m_stamina = Mathf.Max(0f, player.m_stamina - DeepNorthNightSettings.ColdStaminaDrain.Value * seconds);
        owed += player.GetMaxHealth() * DeepNorthNightSettings.ColdHealthDrain.Value * seconds;
        float room = player.GetHealth() - player.GetMaxHealth() * HealthFloor;
        if (owed < 1f || room < 1f)
        {
            owed = Mathf.Min(owed, 1f);
            return;
        }

        HitData hit = new() { m_point = player.GetCenterPoint() };
        hit.m_damage.m_damage = Mathf.Min(owed, room);
        owed = 0f;
        player.ApplyDamage(hit, showDamageText: false, triggerEffects: false);
    }

    // The white storm: told when it comes and goes, with whispers on the wind while it blows.
    private void Storm(Player player, bool outside)
    {
        bool blowing = outside && Whiteout();
        if (blowing != shownWhiteout)
        {
            shownWhiteout = blowing;
            player.Message(MessageHud.MessageType.TopLeft, blowing ? "$msg_whitehilt_north_whiteout" : "$msg_whitehilt_north_whiteout_ends");
        }

        if (blowing && Time.time >= nextWhisper)
        {
            nextWhisper = Time.time + Random.Range(10f, 20f);
            Vector2 side = Random.insideUnitCircle.normalized * 4f;
            DarknessSounds.Play(DarknessSounds.Whispers, player.GetHeadPoint() + new Vector3(side.x, 0f, side.y));
        }
    }

    // The pack: heard howling before it is seen, and back on the player's scent once they have been out in the open a
    // while after it lost them.
    private void Track(Player player, bool outside, bool sheltered, float seconds)
    {
        Transform wolf = BeastBehaviour.NearestPack(player.transform.position, PackHeard);
        bool near = wolf != null;
        if (near && outside && sheltered && !wasSheltered)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_north_pack_lost");
        }

        wasSheltered = sheltered;
        if (near && Time.time >= nextHowl && Vector3.Distance(wolf.position, player.transform.position) > PackSeen)
        {
            nextHowl = Time.time + Random.Range(12f, 20f);
            Vector3 toward = (wolf.position - player.transform.position).normalized;
            GameObject howl = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Howl) : null;
            if (howl != null)
            {
                Instantiate(howl, player.transform.position + toward * HowlDistance, Quaternion.identity);
            }
        }

        if (near || !outside || sheltered || huntedNight != DifficultyState.NightId() || !PackMayCome())
        {
            openSeconds = 0f;
            return;
        }

        openSeconds += seconds;
        if (openSeconds < DeepNorthNightSettings.PackReturnSeconds.Value || Time.time < nextReturn)
        {
            return;
        }

        openSeconds = 0f;
        nextReturn = Time.time + ReturnCooldown;
        BeastDefinition pack = BeastDefinition.Find(PackKey);
        if (pack != null && BeastSpawner.Spawn(player, pack))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_north_pack_returns");
        }
    }

    private static bool PackMayCome()
    {
        return DifficultySettings.Enabled.Value && DifficultySettings.BeastsEnabled.Value && DifficultySettings.IsBeastEnabled(PackKey);
    }
}
