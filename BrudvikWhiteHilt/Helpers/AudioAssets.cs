using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Loads the mod's sounds from <c>whitehilt_audio</c>, an asset bundle of its own kept as a file next to the mod's DLL:
/// the effect sounds, the traders' recorded voices, the trading posts' ambience and music, and the plagues' roars.
/// </summary>
/// <remarks>
/// A bundle of its own, so the many voice lines neither grow the main bundle nor need it rebuilt when a line changes.
/// The clips are listed once and looked up by name, as LoadAllAssets is slow.
/// </remarks>
public static class AudioAssets
{
    /// <summary>File name of the bundle.</summary>
    public const string BundleName = "whitehilt_audio";

    private static AssetBundle bundle;
    private static bool tried;
    private static Dictionary<string, AudioClip> clips;

    /// <summary>
    /// Loads a sound from the bundle.
    /// </summary>
    /// <param name="name">Sound name (the file name in lower case, without extension).</param>
    /// <returns>The sound.</returns>
    public static AudioClip LoadAudio(string name)
    {
        return TryLoad(name) ?? throw new InvalidOperationException($"Sound '{name}' not found in {BundleName}.");
    }

    /// <summary>
    /// Loads a sound from the bundle if it has one of that name, such as a voice line that may not be recorded.
    /// </summary>
    /// <param name="name">Sound name.</param>
    /// <returns>The sound, or null.</returns>
    public static AudioClip TryLoad(string name)
    {
        if (string.IsNullOrEmpty(name) || Load() == null)
        {
            return null;
        }

        clips ??= bundle.LoadAllAssets<AudioClip>().GroupBy(clip => clip.name).ToDictionary(group => group.Key, group => group.First());
        return clips.TryGetValue(name, out AudioClip clip) ? clip : null;
    }

    private static AssetBundle Load()
    {
        // Tried once: a missing file is reported once, not for every sound.
        if (bundle != null || tried)
        {
            return bundle;
        }

        tried = true;
        BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(BrudvikWhiteHilt.PluginGUID, out PluginInfo plugin);
        string folder = plugin != null ? Path.GetDirectoryName(plugin.Location) : Paths.PluginPath;
        string path = Path.Combine(folder, BundleName);
        if (!File.Exists(path))
        {
            Jotunn.Logger.LogError($"{path} is missing: copy every file in the release's plugins folder, not only the DLL. The mod is silent without it.");
            return null;
        }

        bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            Jotunn.Logger.LogError($"Asset bundle {path} could not be loaded.");
        }

        return bundle;
    }
}
