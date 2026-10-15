using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the asset bundle with the meshes and textures used by White Hilt items; the sounds are in their own bundle
/// (<see cref="BuildAudioBundle"/>). Every <c>*.obj</c>, <c>*_albedo</c> and <c>*_emission</c> texture (.png / .jpg) in Assets/Foraging is included,
/// and the animated creature prefabs from Assets/Creatures (see <see cref="BuildCreatures"/>) and the attack clips from
/// Assets/Animations (see <see cref="BuildAttackClips"/>).
/// Run from the command line with <c>-executeMethod BuildForagingBundle.Build</c>.
/// </summary>
public static class BuildForagingBundle
{
    private const string BundleName = "whitehilt_foraging";
    private const string SourceFolder = "Assets/Foraging";

    // Models the mod reads on the CPU: merged with Mesh.CombineMeshes (navigation pieces, Pathfinder amulet) or bent (White Hilt Bow).
    // Models the mod reads at run time: combined into one mesh, or (the wall drawer and the ship workshops, made of flat
    // palette swatches) given surfaces of vanilla wood, iron and stone by Helpers/PaletteSurfaces.
    private static readonly string[] CombinedModels =
    {
        "cartodesk", "sextant", "mapscroll", "seachart", "amulet", "whbow", "walldrawer", "shipworkbench", "shipforge", "shipstonecutter"
    };

    // Tiling roof textures whose one tile covers 2-3 m of roof.
    private static readonly string[] LargeTiles = { "roof_slate_albedo", "roof_straw_albedo", "roof_turf_albedo", "roof_marble_albedo", "roof_grausten_albedo" };

    /// <summary>
    /// Configures the importers and writes the bundle to the project's AssetBundles folder.
    /// </summary>
    public static void Build()
    {
        AssetDatabase.Refresh();

        string[] models = FindAssets("*.obj");
        string[] textures = new[] { "*_albedo.png", "*_albedo.jpg", "*_emission.png", "*_emission.jpg", "*_normal.png", "*_normal.jpg" }
            .SelectMany(FindAssets)
            .OrderBy(path => path)
            .ToArray();
        if (models.Length == 0)
        {
            throw new InvalidOperationException($"No models found in {SourceFolder}.");
        }

        foreach (string model in models)
        {
            ConfigureModel(model);
        }

        foreach (string texture in textures)
        {
            ConfigureTexture(texture);
        }

        string[] creatures = BuildCreatures.Prepare();
        string[] animations = BuildAttackClips.Prepare();

        string outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "AssetBundles");
        Directory.CreateDirectory(outputPath);

        AssetBundleBuild build = new()
        {
            assetBundleName = BundleName,
            assetNames = models.Concat(textures).Concat(creatures).Concat(animations).ToArray()
        };

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            BuildTarget.StandaloneWindows64);

        if (manifest == null)
        {
            throw new InvalidOperationException("Asset bundle build failed.");
        }

        foreach (string model in models)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(model);
            Debug.Log($"[WhiteHilt] Mesh '{mesh.name}': {mesh.vertexCount} vertices, bounds {mesh.bounds}");
        }

        foreach (string texture in textures)
        {
            Debug.Log($"[WhiteHilt] Texture '{Path.GetFileNameWithoutExtension(texture)}'");
        }

        Debug.Log($"[WhiteHilt] Built {BundleName} with {models.Length} models, {textures.Length} textures, {creatures.Length} creatures and {animations.Length} animations");
    }

    private static string[] FindAssets(string pattern)
    {
        return Directory.GetFiles(SourceFolder, pattern)
            .Select(path => path.Replace('\\', '/'))
            .OrderBy(path => path)
            .ToArray();
    }

    private static void ConfigureModel(string path)
    {
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importAnimation = false;
        importer.importNormals = ModelImporterNormals.Import;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        // Mesh.CombineMeshes reads the vertices on the CPU, so the models the mod merges into combined meshes stay readable.
        importer.isReadable = CombinedModels.Contains(Path.GetFileNameWithoutExtension(path));
        importer.SaveAndReimport();
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        string name = Path.GetFileNameWithoutExtension(path);
        bool normal = name.EndsWith("_normal", StringComparison.Ordinal);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        // Atlases from convert_glb.py are one tile per part side by side, so give each tile 512 px (at most 4096 in all).
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        int tiles = Mathf.Max(1, Mathf.RoundToInt((float)width / height));
        // Roof textures with a large tile (2-3 m) keep 1024 px, so they stay sharp up close.
        importer.maxTextureSize = LargeTiles.Contains(name) ? 1024 : tiles > 1 ? Mathf.Min(4096, Mathf.NextPowerOfTwo(512 * tiles)) : 512;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.isReadable = false;
        importer.SaveAndReimport();
    }
}
