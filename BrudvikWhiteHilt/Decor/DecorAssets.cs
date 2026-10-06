using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Decor;

/// <summary>
/// Loads the decor models from <c>whitehilt_decor</c>, an asset bundle kept as a file next to the mod's DLL.
/// </summary>
/// <remarks>
/// The main bundle is embedded in the DLL. The decor bundle has many more models, so it is a separate file loaded
/// straight from disk: Unity then reads what it needs from the file instead of keeping a copy of the whole bundle in
/// memory. Meshes and textures are looked up by name once and kept in dictionaries, as LoadAllAssets is slow.
/// </remarks>
public static class DecorAssets
{
    /// <summary>File name of the bundle.</summary>
    public const string BundleName = "whitehilt_decor";

    private static AssetBundle bundle;
    private static bool tried;
    private static Dictionary<string, Mesh> meshes;
    private static Dictionary<string, Texture2D> textures;

    /// <summary>
    /// Whether the bundle file is there. Without it the hammer offers only the vanilla-look pieces.
    /// </summary>
    public static bool Available => Load() != null;

    /// <summary>
    /// A mesh from the bundle.
    /// </summary>
    /// <param name="name">Mesh name (<c>decor_&lt;id&gt;</c>).</param>
    /// <returns>The mesh, or null if it is not in the bundle.</returns>
    public static Mesh LoadMesh(string name)
    {
        if (Load() == null)
        {
            return null;
        }

        meshes ??= bundle.LoadAllAssets<Mesh>().GroupBy(mesh => mesh.name).ToDictionary(group => group.Key, group => group.First());
        return meshes.TryGetValue(name, out Mesh mesh) ? mesh : null;
    }

    /// <summary>
    /// A texture from the bundle.
    /// </summary>
    /// <param name="name">Texture name (<c>decor_&lt;id&gt;_albedo</c>).</param>
    /// <returns>The texture, or null if it is not in the bundle.</returns>
    public static Texture2D LoadTexture(string name)
    {
        if (Load() == null)
        {
            return null;
        }

        textures ??= bundle.LoadAllAssets<Texture2D>().GroupBy(texture => texture.name).ToDictionary(group => group.Key, group => group.First());
        return textures.TryGetValue(name, out Texture2D texture) ? texture : null;
    }

    private static AssetBundle Load()
    {
        // Tried once: a missing file is reported once, not for every piece.
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
            Jotunn.Logger.LogWarning($"Decor: {path} is missing, so only the decorations with a vanilla look are offered.");
            return null;
        }

        bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            Jotunn.Logger.LogError($"Decor: {path} could not be loaded.");
        }

        return bundle;
    }
}
