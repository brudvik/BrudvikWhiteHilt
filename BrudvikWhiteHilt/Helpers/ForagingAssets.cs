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
