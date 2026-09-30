using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renders preview images of pieces put together from exported vanilla meshes, so a layout can be tried without the game.
/// Run with <c>-executeMethod PreviewRender.Render -previewLayout &lt;layout.json&gt; -previewMeshes &lt;dir&gt; -previewOut &lt;dir&gt;</c>.
/// With <c>-previewTransparent</c> every piece is rendered from its first view only, without ground, on a transparent
/// background (<c>-previewSize</c> pixels square), for the documentation.
/// </summary>
public static class PreviewRender
{
    private const int ViewWidth = 640;
    private const int ViewHeight = 480;

    private static readonly Dictionary<string, MeshData> meshes = new();
    private static readonly Dictionary<string, Material> materials = new();
    private static string meshFolder;
    private static bool transparent;

    /// <summary>
    /// Builds every piece in the layout and writes one PNG per piece with three views.
    /// </summary>
    public static void Render()
    {
        string layoutPath = Argument("-previewLayout");
        meshFolder = Argument("-previewMeshes");
        string outFolder = Argument("-previewOut");
        Directory.CreateDirectory(outFolder);

        Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(layoutPath));
        Dictionary<string, PieceData> pieces = layout.pieces.ToDictionary(piece => piece.name);
        string[] only = (TryArgument("-previewOnly") ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        transparent = Environment.GetCommandLineArgs().Contains("-previewTransparent");
        int size = int.TryParse(TryArgument("-previewSize"), out int parsed) ? parsed : 1024;

        foreach (PieceData piece in layout.pieces.Where(piece => !piece.template && (only.Length == 0 || only.Contains(piece.name))))
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            // A new scene destroys meshes and materials made in the last one, so every piece loads its own.
            meshes.Clear();
            materials.Clear();
            SetUpStage();
            GameObject root = new(piece.name);
            Build(piece, root.transform, pieces, 0);
            Bounds bounds = MeasureBounds(root);

            View[] views = piece.views != null && piece.views.Length > 0 ? piece.views : DefaultViews;
            string file = Path.Combine(outFolder, piece.name + ".png");
            if (transparent)
            {
                Texture2D single = RenderView(bounds, views[0], size, size);
                File.WriteAllBytes(file, single.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(single);
                Debug.Log($"[Preview] {piece.name}: {Stats(root)} -> {file}");
                continue;
            }

            Texture2D sheet = new(ViewWidth * views.Length, ViewHeight, TextureFormat.RGB24, false);
            for (int i = 0; i < views.Length; i++)
            {
                Texture2D image = RenderView(bounds, views[i], ViewWidth, ViewHeight);
                sheet.SetPixels(i * ViewWidth, 0, ViewWidth, ViewHeight, image.GetPixels());
                UnityEngine.Object.DestroyImmediate(image);
            }

            sheet.Apply();
            File.WriteAllBytes(file, sheet.EncodeToPNG());
            Debug.Log($"[Preview] {piece.name}: {Stats(root)} -> {file}");
        }
    }

    private static readonly View[] DefaultViews =
    {
        new() { label = "outside", yaw = 180f, pitch = 12f },
        new() { label = "inside", yaw = 0f, pitch = 20f },
        new() { label = "angle", yaw = -40f, pitch = 30f }
    };

    private static void Build(PieceData piece, Transform parent, Dictionary<string, PieceData> pieces, int depth)
    {
        if (depth > 8)
        {
            throw new InvalidOperationException($"{piece.name} nests too deep");
        }

        foreach (PartData part in piece.parts)
        {
            for (int copy = 0; copy < Math.Max(1, part.repeat); copy++)
            {
                BuildPart(part, ToVector(part.step, Vector3.zero) * copy, parent, pieces, depth);
            }
        }
    }

    private static void BuildPart(PartData part, Vector3 offset, Transform parent, Dictionary<string, PieceData> pieces, int depth)
    {
        GameObject node = new(part.mesh ?? part.piece);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = ToVector(part.position, Vector3.zero) + offset;
        node.transform.localRotation = Quaternion.Euler(ToVector(part.rotation, Vector3.zero));
        node.transform.localScale = ToVector(part.scale, Vector3.one);

        if (!string.IsNullOrEmpty(part.piece))
        {
            Build(pieces[part.piece], node.transform, pieces, depth + 1);
            return;
        }

        MeshData data = LoadMesh(part.mesh);
        foreach (MeshPart meshPart in data.parts)
        {
            GameObject child = new(meshPart.texture);
            child.transform.SetParent(node.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = meshPart.mesh;
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LoadMaterial(string.IsNullOrEmpty(part.texture) ? meshPart.texture : part.texture, part.tint);
        }
    }

    // In game every piece is combined into one mesh per material, so the material count is the draw call count.
    private static string Stats(GameObject root)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        int triangles = renderers.Sum(renderer => renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3);
        int materials = renderers.Select(renderer => renderer.sharedMaterial).Distinct().Count();
        return $"{renderers.Length} parts, {triangles} triangles, {materials} materials after combining";
    }

    private static void SetUpStage()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.2f);

        Light sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.1f;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(45f, 150f, 0f);

        if (transparent)
        {
            return;
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.localScale = Vector3.one * 10f;
        Material grass = new(Shader.Find("Standard")) { color = new Color(0.33f, 0.4f, 0.22f) };
        grass.SetFloat("_Glossiness", 0f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = grass;
    }

    private static Texture2D RenderView(Bounds bounds, View view, int width, int height)
    {
        GameObject cameraObject = new("Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 35f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = transparent ? new Color(0f, 0f, 0f, 0f) : new Color(0.62f, 0.72f, 0.82f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 500f;

        Quaternion rotation = Quaternion.Euler(view.pitch, view.yaw, 0f);
        bool focused = view.focus != null && view.focus.Length == 3;
        Vector3 center = focused ? new Vector3(view.focus[0], view.focus[1], view.focus[2]) : bounds.center;
        float radius = focused && view.radius > 0f ? view.radius : bounds.extents.magnitude;
        float distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.85f * (view.zoom > 0f ? 1f / view.zoom : 1f);
        camera.nearClipPlane = Mathf.Min(0.1f, distance * 0.1f);
        camera.transform.position = center - rotation * Vector3.forward * distance;
        camera.transform.rotation = rotation;
        camera.aspect = (float)width / height;

        RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        camera.targetTexture = target;
        camera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D image = new(width, height, transparent ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        if (transparent)
        {
            Unpremultiply(image);
        }

        image.Apply();
        RenderTexture.active = previous;

        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        return image;
    }

    // Edges were blended over a black clear colour; dividing by alpha keeps them from turning dark on a light page.
    private static void Unpremultiply(Texture2D image)
    {
        Color32[] pixels = image.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.a > 0 && pixel.a < 255)
            {
                pixels[i] = new Color32(
                    (byte)Math.Min(255, pixel.r * 255 / pixel.a),
                    (byte)Math.Min(255, pixel.g * 255 / pixel.a),
                    (byte)Math.Min(255, pixel.b * 255 / pixel.a),
                    pixel.a);
            }
        }

        image.SetPixels32(pixels);
    }

    private static Bounds MeasureBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private static MeshData LoadMesh(string name)
    {
        if (meshes.TryGetValue(name, out MeshData cached))
        {
            return cached;
        }

        MeshData data = JsonUtility.FromJson<MeshData>(File.ReadAllText(Path.Combine(meshFolder, name + ".json")));
        foreach (MeshPart part in data.parts)
        {
            Mesh mesh = new() { name = $"{name}_{part.texture}", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = ToVectors(part.vertices);
            mesh.normals = ToVectors(part.normals);
            mesh.uv = Enumerable.Range(0, part.uvs.Length / 2).Select(i => new Vector2(part.uvs[i * 2], part.uvs[i * 2 + 1])).ToArray();
            mesh.triangles = part.triangles;
            mesh.RecalculateBounds();
            part.mesh = mesh;
        }

        meshes[name] = data;
        return data;
    }

    private static Material LoadMaterial(string texture, float[] tint)
    {
        string key = texture + (tint != null ? string.Join(",", tint) : string.Empty);
        if (materials.TryGetValue(key, out Material cached))
        {
            return cached;
        }

        Material material = new(Shader.Find("Standard"));
        material.SetFloat("_Glossiness", 0.05f);
        string file = Path.Combine(meshFolder, "textures", texture + ".png");
        if (File.Exists(file))
        {
            Texture2D image = new(2, 2);
            image.LoadImage(File.ReadAllBytes(file));
            material.mainTexture = image;
        }

        if (tint != null && tint.Length >= 3)
        {
            material.color = new Color(tint[0], tint[1], tint[2]);
        }

        materials[key] = material;
        return material;
    }

    private static Vector3[] ToVectors(float[] values)
    {
        return Enumerable.Range(0, values.Length / 3).Select(i => new Vector3(values[i * 3], values[i * 3 + 1], values[i * 3 + 2])).ToArray();
    }

    private static Vector3 ToVector(float[] values, Vector3 fallback)
    {
        return values != null && values.Length == 3 ? new Vector3(values[0], values[1], values[2]) : fallback;
    }

    private static string Argument(string name)
    {
        return TryArgument(name) ?? throw new ArgumentException($"Missing command line argument {name}");
    }

    private static string TryArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    [Serializable]
    private class Layout
    {
        public PieceData[] pieces;
    }

    [Serializable]
    private class PieceData
    {
        public string name;
        public bool template;
        public PartData[] parts;
        public View[] views;
    }

    [Serializable]
    private class PartData
    {
        public string mesh;
        public string piece;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public float[] tint;
        public string texture;
        public int repeat;
        public float[] step;
    }

    [Serializable]
    private class View
    {
        public string label;
        public float yaw;
        public float pitch;
        public float zoom;
        public float[] focus;
        public float radius;
    }

    [Serializable]
    private class MeshData
    {
        public string name;
        public MeshPart[] parts;
        public float[] snapPoints;
    }

    [Serializable]
    private class MeshPart
    {
        public string texture;
        public float[] vertices;
        public float[] normals;
        public float[] uvs;
        public int[] triangles;

        [NonSerialized]
        public Mesh mesh;
    }
}
