using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds <c>whitehilt_decor</c>, the asset bundle of the Decor Hammer's models, from Assets/Decor (every
/// <c>decor_*.obj</c> and its <c>_albedo</c> texture, written by convert_glb.py from AssetSource/Decor/Models).
/// Run from the command line with <c>-executeMethod BuildDecorBundle.Build</c>.
/// </summary>
/// <remarks>
/// A bundle of its own, shipped as a file next to the mod's DLL rather than embedded in it, so the many decorations
/// neither grow the DLL nor sit in memory twice. Textures are kept small: 512 px for a model with one texture, 256 px
/// per tile of an atlas, as most decorations are seen at arm's length or further.
/// </remarks>
public static class BuildDecorBundle
{
    private const string BundleName = "whitehilt_decor";
    private const string SourceFolder = "Assets/Decor";

    /// <summary>
    /// Configures the importers and writes the bundle to the project's AssetBundlesDecor folder.
    /// </summary>
    public static void Build()
    {
        AssetDatabase.Refresh();
        string[] models = Find("*.obj");
        string[] textures = new[] { "*_albedo.png", "*_albedo.jpg" }.SelectMany(Find).OrderBy(path => path).ToArray();
        if (models.Length == 0)
        {
            throw new InvalidOperationException($"No models found in {SourceFolder}.");
        }

        foreach (string model in models)
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(model);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        foreach (string texture in textures)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texture);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            // Cut-out leaves keep their edges in the smaller mip levels.
            importer.alphaIsTransparency = texture.EndsWith(".png", StringComparison.Ordinal);
            importer.mipMapsPreserveCoverage = importer.alphaIsTransparency;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int tiles = Mathf.Max(1, Mathf.RoundToInt((float)width / height));
            importer.maxTextureSize = tiles > 1 ? Mathf.Min(1024, Mathf.NextPowerOfTwo(256 * tiles)) : 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        string output = Path.Combine(Path.GetDirectoryName(Application.dataPath), "AssetBundlesDecor");
        Directory.CreateDirectory(output);
        AssetBundleBuild build = new() { assetBundleName = BundleName, assetNames = models.Concat(textures).ToArray() };
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(output, new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows64);
        if (manifest == null)
        {
            throw new InvalidOperationException("Decor asset bundle build failed.");
        }

        Debug.Log($"[WhiteHilt] Built {BundleName} with {models.Length} models and {textures.Length} textures");
    }

    private static string[] Find(string pattern)
    {
        return Directory.GetFiles(SourceFolder, pattern).Select(path => path.Replace('\\', '/')).OrderBy(path => path).ToArray();
    }
}
