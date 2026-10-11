using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Loads meshes and textures from the main asset bundle, <c>whitehilt_foraging</c>, a file next to the mod's DLL.
/// </summary>
/// <remarks>
/// The bundle used to be embedded in the DLL. Loading it from memory meant a managed copy and a native copy of the
/// whole bundle; from a file, Unity reads only what it needs. Assets of a kind are listed once and then looked up by
/// name, as LoadAllAssets is slow and the mod asks for a few hundred of them while it starts.
/// </remarks>
public static class ForagingAssets
{
    /// <summary>File name of the bundle.</summary>
    public const string BundleName = "whitehilt_foraging";

    private static AssetBundle bundle;
    private static ILookup<string, Mesh> fragments;
    private static readonly Dictionary<Type, Dictionary<string, Object>> byName = new();

    /// <summary>
    /// Loads a mesh from the bundle.
    /// </summary>
    /// <param name="name">Mesh name.</param>
    /// <returns>The mesh.</returns>
    public static Mesh LoadMesh(string name)
    {
        // Meshes are sub-assets of the imported model, so they are not found by LoadAsset.
        return Find<Mesh>(name) ?? throw new InvalidOperationException($"Mesh '{name}' not found in {BundleName}.");
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
        return TryLoadTexture(name) ?? throw new InvalidOperationException($"Texture '{name}' not found in {BundleName}.");
    }

    /// <summary>
    /// Loads a texture from the bundle if it has one of that name, such as the normal map a model may have.
    /// </summary>
    /// <param name="name">Texture name.</param>
    /// <returns>The texture, or null.</returns>
    public static Texture2D TryLoadTexture(string name)
    {
        return Find<Texture2D>(name);
    }

    /// <summary>
    /// Loads a sound from the bundle.
    /// </summary>
    /// <param name="name">Sound name (the .wav file name in lower case, without extension).</param>
    /// <returns>The sound.</returns>
    public static AudioClip LoadAudio(string name)
    {
        return Find<AudioClip>(name) ?? throw new InvalidOperationException($"Sound '{name}' not found in {BundleName}.");
    }

    /// <summary>
    /// Loads a prefab from the bundle, such as an animated creature visual (&lt;name&gt;_visual).
    /// </summary>
    /// <param name="name">Prefab name.</param>
    /// <returns>The prefab.</returns>
    public static GameObject LoadPrefab(string name)
    {
        return GetBundle().LoadAsset<GameObject>(name)
            ?? throw new InvalidOperationException($"Prefab '{name}' not found in {BundleName}.");
    }

    /// <summary>
    /// Loads an animation clip from the bundle, such as the Rune Sword's cuts made by BuildAttackClips.cs.
    /// </summary>
    /// <param name="name">Clip name.</param>
    /// <returns>The clip.</returns>
    public static AnimationClip LoadAnimation(string name)
    {
        return Find<AnimationClip>(name) ?? throw new InvalidOperationException($"Animation '{name}' not found in {BundleName}.");
    }

    // The first asset of a kind with the name, as LoadAllAssets lists them; the list is made on the first request.
    private static T Find<T>(string name) where T : Object
    {
        if (!byName.TryGetValue(typeof(T), out Dictionary<string, Object> assets))
        {
            assets = new Dictionary<string, Object>();
            foreach (T asset in GetBundle().LoadAllAssets<T>())
            {
                if (!assets.ContainsKey(asset.name))
                {
                    assets.Add(asset.name, asset);
                }
            }

            byName[typeof(T)] = assets;
        }

        return assets.TryGetValue(name, out Object found) ? (T)found : null;
    }

    private static AssetBundle GetBundle()
    {
        if (bundle == null)
        {
            BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(BrudvikWhiteHilt.PluginGUID, out PluginInfo plugin);
            string folder = plugin != null ? Path.GetDirectoryName(plugin.Location) : Paths.PluginPath;
            string path = Path.Combine(folder, BundleName);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"{path} is missing: copy every file in the release's plugins folder, not only the DLL.");
            }

            bundle = AssetBundle.LoadFromFile(path)
                ?? throw new InvalidOperationException($"Asset bundle {path} could not be loaded.");
        }

        return bundle;
    }
}
