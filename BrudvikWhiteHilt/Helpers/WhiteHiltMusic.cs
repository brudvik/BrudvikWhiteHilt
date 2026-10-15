using BrudvikWhiteHilt.Difficulty;
using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Kraken;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.SeaBattles;
using BrudvikWhiteHilt.TradingPosts;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// The mod's own fight music, played by the game's music manager in place of its combat music: a huge monster's when
/// one of the mod's great beasts fights you, the sea battle's when a Nail Ship's crew is near, the blood moon's under a
/// blood moon. Outside a fight the game's music plays as before. The pieces are registered with the game's music
/// manager, so its fading, its volume and its continuous music setting apply to them.
/// </summary>
public static class WhiteHiltMusic
{
    /// <summary>A great beast fights you.</summary>
    public const string BigBeast = "music_bigbeast";

    /// <summary>A Nail Ship's crew is near.</summary>
    public const string SeaBattle = "music_seabattle";

    /// <summary>A blood moon is up.</summary>
    public const string BloodMoon = "music_bloodmoon";

    private static readonly string[] pieces = { BigBeast, SeaBattle, BloodMoon };

    // How near the fight has to be.
    private const float BeastRange = 70f;
    private const float RaiderRange = 150f;
    private const float ChooseInterval = 1f;

    private static MusicMan registeredWith;
    private static string chosen;
    private static float chosenAt = float.NegativeInfinity;

    /// <summary>
    /// Whether a piece of music is one of the mod's.
    /// </summary>
    /// <param name="name">The music's name.</param>
    /// <returns>True for the mod's.</returns>
    public static bool IsOurs(string name) => pieces.Contains(name);

    /// <summary>
    /// Registers the pieces with the game's music manager, once for each.
    /// </summary>
    /// <param name="music">The game's music manager.</param>
    public static void Register(MusicMan music)
    {
        if (music == null || music == registeredWith || VisualHelper.IsHeadless)
        {
            return;
        }

        registeredWith = music;
        float volume = SoundSettings.MusicVolume.Value;
        foreach (string name in pieces)
        {
            AudioClip clip = AudioAssets.TryLoad(name);
            if (clip == null || volume <= 0f || music.m_musicHashes.ContainsKey(name.GetStableHashCode()))
            {
                continue;
            }

            MusicMan.NamedMusic piece = new()
            {
                m_name = name,
                m_clips = new[] { clip },
                m_volume = volume,
                m_fadeInTime = 2f,
                m_loop = true,
                m_resume = true,
                m_alwaysFadeout = true
            };
            music.m_music.Add(piece);
            music.m_musicHashes[name.GetStableHashCode()] = piece;
        }
    }

    /// <summary>
    /// The fight music for the local player now, or null for the game's own combat music.
    /// </summary>
    /// <returns>The music's name.</returns>
    public static string Choose()
    {
        if (Time.time - chosenAt < ChooseInterval)
        {
            return chosen;
        }

        chosenAt = Time.time;
        Player player = Player.m_localPlayer;
        chosen = player == null ? null
            : NearGreatBeast(player) ? BigBeast
            : NearRaiders(player) ? SeaBattle
            : DifficultyState.BloodMoon ? BloodMoon
            : null;
        return chosen != null && registeredWith != null && registeredWith.m_musicHashes.ContainsKey(chosen.GetStableHashCode()) ? chosen : null;
    }

    // One of the mod's great beasts, alive and hostile, close by: the Kraken, a Lindorm, a Desert Dragon, a Black Beast
    // or a plague.
    private static bool NearGreatBeast(Player player)
    {
        Vector3 here = player.transform.position;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (character == null || character.IsDead() || character.IsPlayer() || character.IsTamed()
                || (character.transform.position - here).sqrMagnitude > BeastRange * BeastRange)
            {
                continue;
            }

            if (character.GetComponent<KrakenBody>() != null || character.GetComponent<LindormBurrow>() != null
                || character.GetComponent<DragonFire>() != null || character.GetComponent<BeastBehaviour>() != null
                || character.GetComponent<TerrorBehaviour>() != null)
            {
                return true;
            }
        }

        return false;
    }

    // A living Nail Ship crew member or draugr harbour guard close by.
    private static bool NearRaiders(Player player)
    {
        Vector3 here = player.transform.position;
        return RaiderCrew.Count > 0 && RaiderCrew.All.Any(crew => crew != null && crew.Alive
            && (crew.transform.position - here).sqrMagnitude < RaiderRange * RaiderRange);
    }
}

/// <summary>
/// The blood moon heard: slow war drums like a heartbeat and wolves far away, fading in under a blood moon at night
/// outdoors and out when it sets or the player goes inside. On the <see cref="Game"/> object of every machine with a
/// player.
/// </summary>
public class BloodMoonAmbience : MonoBehaviour
{
    private const string Clip = "amb_bloodmoon";
    private const float Fade = 4f;

    private AudioSource source;

    private void Start()
    {
        AudioClip clip = VisualHelper.IsHeadless ? null : AudioAssets.TryLoad(Clip);
        if (clip == null)
        {
            enabled = false;
            return;
        }

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = true;
        source.clip = clip;
        source.volume = 0f;
        if (AudioMan.instance != null)
        {
            source.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
        }
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool on = player != null && DifficultyState.BloodMoon && EnvMan.IsNight() && !player.InInterior();
        float target = on ? SoundSettings.AmbienceVolume.Value : 0f;
        source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime / Fade);
        if (source.volume > 0f && !source.isPlaying)
        {
            source.Play();
        }
        else if (source.volume <= 0f && source.isPlaying)
        {
            source.Stop();
        }
    }
}
