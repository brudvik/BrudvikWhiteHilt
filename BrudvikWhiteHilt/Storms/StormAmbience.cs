using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The storm heard: the wind, rain, sand, snow or fire of the storm where the player is, fading in as it comes and out
/// in shelter, and thunder cracking overhead with a flash of lightning. On the <see cref="Game"/> object of every
/// machine with a player.
/// </summary>
public class StormAmbience : MonoBehaviour
{
    private const float Fade = 3f;
    private const float WarningShare = 0.25f;
    private const float RoofShare = 0.45f;
    private const float InteriorShare = 0.12f;

    private static readonly string[] nearThunder = { "sfx_storm_thunder", "sfx_storm_thunder_2", "sfx_storm_thunder_3" };
    private static readonly string[] farThunder = { "sfx_storm_thunder_far", "sfx_storm_thunder_far_2" };

    private static StormAmbience instance;

    private AudioSource loop;
    private AudioSource effects;
    private string playing;
    private string wanted;
    private float nextThunder;

    /// <summary>
    /// The warning: distant thunder, a gust and a horn far away. Before Ragnarok, Gjallarhorn: Heimdall's horn, heard
    /// as loud under a roof as out in the open.
    /// </summary>
    public static void PlayWarning()
    {
        if (StormState.Ragnarok && StormSettings.RagnarokHorn.Value)
        {
            PlayEffect("sfx_storm_gjallarhorn", false);
            return;
        }

        PlayEffect("sfx_storm_warning");
    }

    /// <summary>
    /// Plays a storm sound for the local player, quieter in shelter.
    /// </summary>
    /// <param name="name">The sound.</param>
    public static void PlayEffect(string name)
    {
        PlayEffect(name, true);
    }

    private static void PlayEffect(string name, bool muffled)
    {
        if (instance == null || instance.effects == null)
        {
            return;
        }

        AudioClip clip = AudioAssets.TryLoad(name);
        float volume = (SoundSettings.EffectsVolume?.Value ?? 1f) * StormSettings.Volume.Value * (muffled ? Muffle() : 1f);
        if (clip != null && volume > 0f)
        {
            instance.effects.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// A thunderclap, near or far; a near one flashes when the player is outside.
    /// </summary>
    /// <param name="near">Right overhead.</param>
    public static void Thunder(bool near)
    {
        string[] clips = near ? nearThunder : farThunder;
        PlayEffect(clips[Random.Range(0, clips.Length)]);
        if (near && Player.m_localPlayer != null && !Player.m_localPlayer.InInterior())
        {
            StormVision.Flash(Player.m_localPlayer.InShelter() ? 0.4f : 1f);
        }
    }

    // In shelter the storm is heard through the walls.
    private static float Muffle()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return 1f;
        }

        return player.InInterior() ? InteriorShare : player.InShelter() ? RoofShare : 1f;
    }

    private void Start()
    {
        if (VisualHelper.IsHeadless)
        {
            enabled = false;
            return;
        }

        instance = this;
        loop = gameObject.AddComponent<AudioSource>();
        loop.playOnAwake = false;
        loop.spatialBlend = 0f;
        loop.loop = true;
        loop.volume = 0f;
        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.spatialBlend = 0f;
        effects.outputAudioMixerGroup = WhiteHiltSound.EffectsMixer;
        if (AudioMan.instance != null)
        {
            loop.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool here = player != null && StormSettings.Enabled.Value && StormState.Visible
            && StormSettings.HasStorms(player.GetCurrentBiome());
        StormProfile profile = here ? StormProfile.Here(player.GetCurrentBiome()) : null;
        wanted = profile?.Ambience ?? wanted;

        // In the eye the wind dies down to what it was while the storm gathered.
        float share = !here ? 0f : StormState.Coming || StormState.InEye ? WarningShare : 1f;
        float target = share * Muffle() * SoundSettings.AmbienceVolume.Value * StormSettings.Volume.Value;

        // A new storm's sound fades the old one out first.
        if (playing != wanted)
        {
            target = 0f;
            if (loop.volume <= 0.001f)
            {
                SwapClip();
            }
        }

        loop.volume = Mathf.MoveTowards(loop.volume, target, Time.deltaTime / Fade);
        if (loop.volume > 0f && !loop.isPlaying && loop.clip != null)
        {
            loop.Play();
        }
        else if (loop.volume <= 0f && loop.isPlaying)
        {
            loop.Stop();
        }

        RollThunder(profile);
    }

    private void SwapClip()
    {
        playing = wanted;
        loop.Stop();
        loop.clip = string.IsNullOrEmpty(wanted) ? null : AudioAssets.TryLoad(wanted);
    }

    // Thunder now and then while the storm rages, far off while it gathers.
    private void RollThunder(StormProfile profile)
    {
        if (profile == null || !profile.Thunders || Time.time < nextThunder)
        {
            return;
        }

        if (StormState.Raging && !StormState.InEye)
        {
            nextThunder = Time.time + Random.Range(8f, 25f) / Mathf.Max(1f, StormClient.Fury);
            Thunder(Random.value < 0.6f);
        }
        else
        {
            nextThunder = Time.time + Random.Range(25f, 50f);
            Thunder(false);
        }
    }
}
