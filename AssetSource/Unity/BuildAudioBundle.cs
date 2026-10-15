using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds <c>whitehilt_audio</c>, the asset bundle of every sound of the mod, from Assets/Audio: the effect sounds of
/// AssetSource/Sounds and the voices, effects, ambience and music AssetSource/Audio/generate_audio.py makes (every
/// <c>*.wav</c> and <c>*.mp3</c>). Run from the command line with <c>-executeMethod BuildAudioBundle.Build</c>.
/// </summary>
/// <remarks>
/// A bundle of its own, shipped as a file next to the mod's DLL, so the many voice lines neither grow the main bundle
/// nor need it rebuilt. Short sounds are decompressed when loaded so they play at once; the long loops of ambience and
/// music (amb_*, music_*) stay compressed in memory and are decoded as they play.
/// </remarks>
public static class BuildAudioBundle
{
    private const string BundleName = "whitehilt_audio";
    private const string SourceFolder = "Assets/Audio";

    /// <summary>
    /// Configures the importers and writes the bundle to the project's AssetBundlesAudio folder.
    /// </summary>
    public static void Build()
    {
        AssetDatabase.Refresh();
        string[] sounds = new[] { "*.wav", "*.mp3" }.SelectMany(Find).OrderBy(path => path).ToArray();
        if (sounds.Length == 0)
        {
            throw new InvalidOperationException($"No sounds found in {SourceFolder}.");
        }

        foreach (string sound in sounds)
        {
            string name = Path.GetFileNameWithoutExtension(sound);
            bool loop = name.StartsWith("amb_", StringComparison.Ordinal) || name.StartsWith("music_", StringComparison.Ordinal);
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(sound);
            // Music keeps its stereo; everything else is placed in the world and needs only one channel.
            importer.forceToMono = !name.StartsWith("music_", StringComparison.Ordinal);
            importer.loadInBackground = loop;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = name.StartsWith("voice_", StringComparison.Ordinal) ? 0.5f : 0.7f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "AssetBundlesAudio");
        Directory.CreateDirectory(output);
        AssetBundleBuild build = new() { assetBundleName = BundleName, assetNames = sounds };
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(output, new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows64);
        if (manifest == null)
        {
            throw new InvalidOperationException("Audio asset bundle build failed.");
        }

        float seconds = sounds.Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(clip => clip != null).Sum(clip => clip.length);
        Debug.Log($"[WhiteHilt] Built {BundleName} with {sounds.Length} sounds, {seconds / 60f:0.0} minutes");
    }

    private static string[] Find(string pattern)
    {
        return Directory.GetFiles(SourceFolder, pattern).Select(path => path.Replace('\\', '/')).ToArray();
    }
}
