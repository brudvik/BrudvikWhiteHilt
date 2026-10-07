using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Loads meshes and textures from the embedded foraging asset bundle.
/// </summary>
public static class ForagingAssets
{
    private const string ResourceName = "BrudvikWhiteHilt.Assets.whitehilt_foraging";

    private static AssetBundle bundle;
    private static ILookup<string, Mesh> fragments;

    /// <summary>
    /// Loads a mesh from the bundle.
    /// </summary>
    /// <param name="name">Mesh name.</param>
    /// <returns>The mesh.</returns>
    public static Mesh LoadMesh(string name)
    {
        // Meshes are sub-assets of the imported model, so they are not found by LoadAsset.
        return GetBundle().LoadAllAssets<Mesh>().FirstOrDefault(mesh => mesh.name == name)
            ?? throw new InvalidOperationException($"Mesh '{name}' not found in {ResourceName}.");
    }

    /// <summary>
    /// Loads the chunks a model breaks into (<c>&lt;name&gt;_frag0</c>, ...), written by convert_glb.py for a model with a <c>.fragments.json</c>.
    /// </summary>
    /// <param name="name">Mesh name of the whole model.</param>
    /// <returns>The chunks, or none if the model has none.</returns>
    public static Mesh[] LoadFragments(string name)
    {
        fragments ??= GetBundle().LoadAllAssets<Mesh>()
            .Where(mesh => mesh.name.Contains("_frag"))
            .ToLookup(mesh => mesh.name.Substring(0, mesh.name.LastIndexOf("_frag", StringComparison.Ordinal)));
        return fragments[name].ToArray();
    }

    /// <summary>
    /// Loads a texture from the bundle.
    /// </summary>
    /// <param name="name">Texture name.</param>
    /// <returns>The texture.</returns>
    public static Texture2D LoadTexture(string name)
    {
        return GetBundle().LoadAllAssets<Texture2D>().FirstOrDefault(texture => texture.name == name)
            ?? throw new InvalidOperationException($"Texture '{name}' not found in {ResourceName}.");
    }

    /// <summary>
    /// Loads a sound from the bundle.
    /// </summary>
    /// <param name="name">Sound name (the .wav file name in lower case, without extension).</param>
    /// <returns>The sound.</returns>
    public static AudioClip LoadAudio(string name)
    {
        return GetBundle().LoadAllAssets<AudioClip>().FirstOrDefault(clip => clip.name == name)
            ?? throw new InvalidOperationException($"Sound '{name}' not found in {ResourceName}.");
    }

    /// <summary>
    /// Loads a prefab from the bundle, such as an animated creature visual (&lt;name&gt;_visual).
    /// </summary>
    /// <param name="name">Prefab name.</param>
    /// <returns>The prefab.</returns>
    public static GameObject LoadPrefab(string name)
    {
        return GetBundle().LoadAsset<GameObject>(name)
            ?? throw new InvalidOperationException($"Prefab '{name}' not found in {ResourceName}.");
    }

    /// <summary>
    /// Loads an animation clip from the bundle, such as the Rune Sword's cuts made by BuildAttackClips.cs.
    /// </summary>
    /// <param name="name">Clip name.</param>
    /// <returns>The clip.</returns>
    public static AnimationClip LoadAnimation(string name)
    {
        return GetBundle().LoadAllAssets<AnimationClip>().FirstOrDefault(clip => clip.name == name)
            ?? throw new InvalidOperationException($"Animation '{name}' not found in {ResourceName}.");
    }

    private static AssetBundle GetBundle()
    {
        if (bundle == null)
        {
            // Not Jotunn's LoadAssetBundleFromResources: it disposes the stream, but Unity reads bundle data lazily from it.
            using Stream stream = typeof(ForagingAssets).Assembly.GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
            using MemoryStream memory = new();
            stream.CopyTo(memory);

            bundle = AssetBundle.LoadFromMemory(memory.ToArray())
                ?? throw new InvalidOperationException($"Embedded asset bundle '{ResourceName}' could not be loaded.");
        }

        return bundle;
    }
}
