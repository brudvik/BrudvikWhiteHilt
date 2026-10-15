using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Plays the mod's own sounds (<see cref="AudioAssets"/>) through the game's mixer, so the game's volume sliders apply:
/// recorded voice lines from a speaker, effect sounds in the world or for the local player alone.
/// </summary>
public static class WhiteHiltSound
{
    private const string VoicePrefix = "voice_";
    private static readonly string[] tokenPrefixes = { "$whitehilt_trader_", "$whitehilt_" };

    private static AudioMixerGroup effects;

    /// <summary>The game's mixer group for effect sounds, once the game has its audio up.</summary>
    public static AudioMixerGroup EffectsMixer
    {
        get
        {
            if (effects == null && AudioMan.instance != null && AudioMan.instance.m_masterMixer != null)
            {
                effects = AudioMan.instance.m_masterMixer.FindMatchingGroups("SFX").FirstOrDefault() ?? AudioMan.instance.m_ambientMixer;
            }

            return effects;
        }
    }

    /// <summary>
    /// The clip a speaker says a line with, in the player's language, or in English when that is not recorded.
    /// </summary>
    /// <param name="speaker">The speaker: a biome's trader in lower case (meadows, blackforest...), hunter or skipper.</param>
    /// <param name="token">The line's translation token, such as $whitehilt_trader_greet2.</param>
    /// <returns>The clip, or null when the line is not recorded.</returns>
    public static AudioClip VoiceClip(string speaker, string token)
    {
        string line = LineName(token);
        if (string.IsNullOrEmpty(speaker) || line == null)
        {
            return null;
        }

        string language = Localization.instance != null && Localization.instance.GetSelectedLanguage() == "Norwegian" ? "no" : "en";
        return AudioAssets.TryLoad(ClipName(speaker, language, line)) ?? (language != "en" ? AudioAssets.TryLoad(ClipName(speaker, "en", line)) : null);
    }

    /// <summary>
    /// The name of a recorded line's clip, as AssetSource/Audio/generate_audio.py names it.
    /// </summary>
    /// <param name="speaker">The speaker.</param>
    /// <param name="language">en or no.</param>
    /// <param name="line">The line's name (<see cref="LineName"/>).</param>
    /// <returns>voice_{speaker}_{language}_{line}.</returns>
    public static string ClipName(string speaker, string language, string line) => $"{VoicePrefix}{speaker}_{language}_{line}";

    /// <summary>
    /// A line's name from its translation token: the token without $whitehilt_trader_ or $whitehilt_.
    /// </summary>
    /// <param name="token">The token, such as $whitehilt_trader_greet2 or $whitehilt_skipper_depart1.</param>
    /// <returns>The name, such as greet2 or skipper_depart1; null for anything but a token of the mod.</returns>
    public static string LineName(string token)
    {
        string prefix = token == null ? null : tokenPrefixes.FirstOrDefault(candidate => token.StartsWith(candidate, System.StringComparison.Ordinal));
        return prefix == null ? null : token.Substring(prefix.Length);
    }

    /// <summary>
    /// Gives an object a voice: a sound source in the world, heard within a few metres.
    /// </summary>
    /// <param name="speaker">The object that speaks.</param>
    /// <returns>The voice, or null on a dedicated server.</returns>
    public static AudioSource AddVoice(GameObject speaker)
    {
        if (VisualHelper.IsHeadless || speaker == null)
        {
            return null;
        }

        AudioSource voice = speaker.AddComponent<AudioSource>();
        Place(voice, 2f, 22f);
        return voice;
    }

    /// <summary>
    /// Says a recorded line, cutting off the one before; nothing when the line is not recorded or voices are off.
    /// </summary>
    /// <param name="voice">The speaker's voice (<see cref="AddVoice"/>).</param>
    /// <param name="speaker">Whose voice it is.</param>
    /// <param name="token">The line's translation token.</param>
    public static void Speak(AudioSource voice, string speaker, string token)
    {
        float volume = SoundSettings.VoiceVolume?.Value ?? 1f;
        AudioClip clip = voice != null && volume > 0f ? VoiceClip(speaker, token) : null;
        if (clip == null)
        {
            return;
        }

        if (voice.outputAudioMixerGroup == null)
        {
            voice.outputAudioMixerGroup = EffectsMixer;
        }

        voice.Stop();
        voice.clip = clip;
        voice.volume = volume;
        voice.Play();
    }

    /// <summary>
    /// Plays an effect sound in the world from a source, at the effects volume.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="name">The sound's name.</param>
    public static void PlayFrom(AudioSource source, string name)
    {
        float volume = SoundSettings.EffectsVolume?.Value ?? 1f;
        AudioClip clip = source != null && volume > 0f ? AudioAssets.TryLoad(name) : null;
        if (clip == null)
        {
            return;
        }

        if (source.outputAudioMixerGroup == null)
        {
            source.outputAudioMixerGroup = EffectsMixer;
        }

        source.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Plays an effect sound for the local player alone, not placed in the world.
    /// </summary>
    /// <param name="name">The sound's name.</param>
    public static void PlayLocal(string name)
    {
        float volume = SoundSettings.EffectsVolume?.Value ?? 1f;
        AudioClip clip = volume > 0f && !VisualHelper.IsHeadless ? AudioAssets.TryLoad(name) : null;
        if (clip == null)
        {
            return;
        }

        GameObject holder = new($"whitehilt_sound_{name}");
        AudioSource source = holder.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = EffectsMixer;
        source.PlayOneShot(clip, volume);
        Object.Destroy(holder, clip.length + 0.5f);
    }

    /// <summary>
    /// Sets a source up as a sound in the world: full within <paramref name="near"/> metres, gone beyond <paramref name="far"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="near">Distance it is heard at full volume within.</param>
    /// <param name="far">Distance it is not heard beyond.</param>
    public static void Place(AudioSource source, float near, float far)
    {
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = near;
        source.maxDistance = far;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = EffectsMixer;
    }
}
