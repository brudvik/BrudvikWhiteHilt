using BrudvikWhiteHilt.Pieces.Roofs;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Sound;

/// <summary>
/// Makes wind, rain, sea and thunder quieter and muffled under a roof, by how well the player is covered: a little under
/// an open roof, most in a closed house, some under the White Hilt Ship's tent. Only the weather's own sound sources are
/// touched, so fires, cooking and crafting sound as before.
/// </summary>
public static class IndoorSound
{
    private const float OpenCutoff = 22000f;
    private const float MinCutoff = 200f;

    // Under a roof with open sides the weather is this far towards indoors; the game counts 80 % cover as shelter.
    private const float OpenRoof = 0.4f;
    private const float LeastCover = 0.3f;
    private const float ShelterCover = 0.8f;
    private const float ShipTent = 0.5f;

    private static readonly List<AudioLowPassFilter> filters = new();
    private static readonly HashSet<AudioSource> handled = new();

    private static volatile float windGain = 1f;
    private static volatile float rainGain = 1f;
    private static float indoor;
    private static AudioMan attachedAudio;
    private static EnvMan attachedEnv;

    /// <summary>Gain of the wind now, 0 to 1. Read on the audio thread.</summary>
    public static float WindGain => windGain;

    /// <summary>Gain of rain, sea, thunder and the weather's ambience now, 0 to 1. Read on the audio thread.</summary>
    public static float RainGain => rainGain;

    /// <summary>True while a thunder clap is being spawned, so its sound gets the filters.</summary>
    public static bool CapturingThunder { get; set; }

    /// <summary>
    /// Finds the weather's sound sources and fades the gain and muffling towards how covered the player is. Called after
    /// the game's audio update.
    /// </summary>
    /// <param name="audio">The game's audio manager.</param>
    public static void Tick(AudioMan audio)
    {
        if (audio != attachedAudio)
        {
            attachedAudio = audio;
            Attach(audio.m_windLoopSource, wind: true);
            Attach(audio.m_ambientLoopSource, wind: false);
            Attach(audio.m_oceanAmbientSource, wind: false);
        }

        if (EnvMan.instance != null && EnvMan.instance != attachedEnv)
        {
            attachedEnv = EnvMan.instance;
            AttachEnvironments(attachedEnv);
        }

        float fade = Mathf.Max(0.05f, IndoorSoundSettings.FadeSeconds.Value);
        indoor = Mathf.MoveTowards(indoor, Target(), Time.unscaledDeltaTime / fade);
        windGain = Mathf.Lerp(1f, Mathf.Clamp01(IndoorSoundSettings.WindVolume.Value), indoor);
        rainGain = Mathf.Lerp(1f, Mathf.Clamp01(IndoorSoundSettings.RainVolume.Value), indoor);

        // Pitch is heard logarithmically, so the cutoff moves evenly in log space.
        float cutoff = OpenCutoff;
        if (IndoorSoundSettings.Muffle.Value && indoor > 0.001f)
        {
            float closed = Mathf.Clamp(IndoorSoundSettings.MuffleCutoffHz.Value, MinCutoff, OpenCutoff);
            cutoff = Mathf.Exp(Mathf.Lerp(Mathf.Log(OpenCutoff), Mathf.Log(closed), indoor));
        }

        bool muffled = cutoff < OpenCutoff - 1f;
        filters.RemoveAll(filter => filter == null);
        foreach (AudioLowPassFilter filter in filters)
        {
            filter.enabled = muffled;
            filter.cutoffFrequency = cutoff;
        }
    }

    /// <summary>
    /// Gives the sound of a thunder clap the filters.
    /// </summary>
    /// <param name="spawned">The effect objects the clap spawned.</param>
    public static void AttachSpawned(GameObject[] spawned)
    {
        if (spawned == null)
        {
            return;
        }

        foreach (GameObject go in spawned)
        {
            if (go == null)
            {
                continue;
            }

            foreach (AudioSource source in go.GetComponentsInChildren<AudioSource>(true))
            {
                Attach(source, wind: false);
            }
        }
    }

    // 0 outdoors, 1 in a closed house. Dungeons have their own sounds and are left alone.
    private static float Target()
    {
        Player player = Player.m_localPlayer;
        if (!IndoorSoundSettings.Enabled.Value || player == null || player.IsDead() || player.InInterior())
        {
            return 0f;
        }

        float target = player.m_underRoof
            ? Mathf.Lerp(OpenRoof, 1f, Mathf.InverseLerp(LeastCover, ShelterCover, player.m_coverPercentage))
            : 0f;
        if (player.m_underRoof && RoofInfo.IsQuietRoofAbove(player.GetHeadPoint()))
        {
            target = Mathf.Min(1f, target + RoofSettings.QuietRoofBonus.Value);
        }
        if (IndoorSoundSettings.ShipTentCounts.Value && WhiteHiltShipUpgrades.IsUnderTent(player.GetCenterPoint()))
        {
            target = Mathf.Max(target, ShipTent);
        }

        return target;
    }

    // Rain and other weather particles can carry their own sound.
    private static void AttachEnvironments(EnvMan env)
    {
        foreach (EnvSetup setup in env.m_environments)
        {
            if (setup?.m_psystems == null)
            {
                continue;
            }

            foreach (GameObject system in setup.m_psystems)
            {
                if (system == null)
                {
                    continue;
                }

                foreach (AudioSource source in system.GetComponentsInChildren<AudioSource>(true))
                {
                    Attach(source, wind: false);
                }
            }
        }
    }

    private static void Attach(AudioSource source, bool wind)
    {
        handled.RemoveWhere(known => known == null);
        if (source == null || !handled.Add(source) || source.GetComponent<IndoorAudioFilter>() != null)
        {
            return;
        }

        GameObject go = source.gameObject;
        go.AddComponent<IndoorAudioFilter>().Wind = wind;
        if (go.GetComponent<AudioLowPassFilter>() != null)
        {
            return;
        }

        AudioLowPassFilter lowPass = go.AddComponent<AudioLowPassFilter>();
        lowPass.cutoffFrequency = OpenCutoff;
        lowPass.enabled = false;
        filters.Add(lowPass);
    }
}
