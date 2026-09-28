using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the asset bundle with the meshes and textures used by White Hilt items.
/// Every <c>*.obj</c>, <c>*_albedo</c> and <c>*_emission</c> texture (.png / .jpg) in Assets/Foraging is included.
/// Run from the command line with <c>-executeMethod BuildForagingBundle.Build</c>.
/// </summary>
public static class BuildForagingBundle
{
    private const string BundleName = "whitehilt_foraging";
    private const string SourceFolder = "Assets/Foraging";

    /// <summary>
    /// Configures the importers and writes the bundle to the project's AssetBundles folder.
    /// </summary>
    public static void Build()
    {
        AssetDatabase.Refresh();

        string[] models = FindAssets("*.obj");
        string[] textures = new[] { "*_albedo.png", "*_albedo.jpg", "*_emission.png", "*_emission.jpg" }
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

        string outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "AssetBundles");
        Directory.CreateDirectory(outputPath);

        AssetBundleBuild build = new()
        {
            assetBundleName = BundleName,
            assetNames = models.Concat(textures).ToArray()
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

        Debug.Log($"[WhiteHilt] Built {BundleName} with {models.Length} models and {textures.Length} textures");
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
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        // Atlases from convert_glb.py are one tile per part side by side, so give each tile 512 px.
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        importer.maxTextureSize = width > height ? 1024 : 512;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.isReadable = false;
        importer.SaveAndReimport();
    }
}
