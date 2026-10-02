using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Roofs;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renders the White Hilt roofs offline with the mod's own mesh code (RoofMeshBuilder, copied in by render_roofs.ps1):
/// one image per covering with a small gable roof (with a smoke hole and the dragon gable), corner pieces and the steep
/// pitches. Run with <c>-executeMethod RoofPreview.Render -roofTextures &lt;dir&gt; -roofWood &lt;jpg&gt; -roofOut &lt;dir&gt;</c>
/// and optionally <c>-roofMeshes &lt;dir&gt;</c> with an exported <c>dragon_head.json</c>.
/// </summary>
public static class RoofPreview
{
    private static string textureFolder;
    private static readonly Dictionary<string, Texture2D> textures = new();

    /// <summary>
    /// Renders every covering.
    /// </summary>
    public static void Render()
    {
        textureFolder = Argument("-roofTextures");
        string wood = Argument("-roofWood");
        string outFolder = Argument("-roofOut");
        string meshFolder = TryArgument("-roofMeshes");
        string docsFolder = TryArgument("-roofDocs");
        Directory.CreateDirectory(outFolder);

        foreach (RoofCovering covering in RoofCoverings.All)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            textures.Clear();
            SetUpStage();

            Material woodMaterial = MakeMaterial(LoadFile(wood), null);
            Material top = MakeMaterial(LoadTexture(RoofCoverings.Texture(covering) + "_albedo"), LoadTexture(RoofCoverings.Texture(covering) + "_normal"));
            string edgeName = RoofCoverings.EdgeTexture(covering);
            Material edge = edgeName == null ? woodMaterial : MakeMaterial(LoadTexture(edgeName + "_albedo"), LoadTexture(edgeName + "_normal"));
            Material[] materials = { top, woodMaterial, edge, woodMaterial };
            RoofStyle style = RoofCoverings.Style(covering);
            GameObject root = new(covering.ToString());

            // A 6 m long gable roof at 26°, one slope with a smoke hole.
            for (int x = 0; x <= 4; x += 2)
            {
                Place(root, RoofShape.Ridge, RoofPitch.Low, style, materials, new Vector3(x, 2f, 0f), 0f);
                Place(root, x == 2 ? RoofShape.SmokeHole : RoofShape.Slope, RoofPitch.Low, style, materials, new Vector3(x, 1f, 2f), 0f, false);
                Place(root, RoofShape.Slope, RoofPitch.Low, style, materials, new Vector3(x, 0f, 4f), 0f);
                Place(root, RoofShape.Slope, RoofPitch.Low, style, materials, new Vector3(x, 1f, -2f), 180f, false);
                Place(root, RoofShape.Slope, RoofPitch.Low, style, materials, new Vector3(x, 0f, -4f), 180f);
            }

            PlaceHatch(root, RoofPitch.Low, woodMaterial, new Vector3(2f, 1f, 2f));
            PlaceGable(root, RoofPitch.Low, woodMaterial, meshFolder, new Vector3(-1.03f, 2.5f, 0f));

            // Corners and the steeper pitches beside it.
            GameObject samples = new("samples");
            samples.transform.position = new Vector3(14f, 0f, 0f);
            Place(samples, RoofShape.OuterCorner, RoofPitch.Low, style, materials, new Vector3(0f, 0f, 3f), 0f);
            Place(samples, RoofShape.InnerCorner, RoofPitch.Low, style, materials, new Vector3(0f, 0f, -1f), 0f);
            Place(samples, RoofShape.OuterCorner, RoofPitch.Medium, style, materials, new Vector3(3.5f, 0f, 3f), 0f);
            Place(samples, RoofShape.InnerCorner, RoofPitch.Medium, style, materials, new Vector3(3.5f, 0f, -1f), 0f);
            Place(samples, RoofShape.Slope, RoofPitch.Steep, style, materials, new Vector3(7f, 1f, 2f), 0f);
            Place(samples, RoofShape.Ridge, RoofPitch.Steep, style, materials, new Vector3(7f, 3f, 0f), 0f);
            Place(samples, RoofShape.Slope, RoofPitch.Steep, style, materials, new Vector3(7f, 1f, -2f), 180f);
            Place(samples, RoofShape.Slope, RoofPitch.Medium, style, materials, new Vector3(10f, 1f, 2f), 0f);
            Place(samples, RoofShape.Ridge, RoofPitch.Medium, style, materials, new Vector3(10f, 2f, 0f), 0f);
            Place(samples, RoofShape.SmokeHole, RoofPitch.Medium, style, materials, new Vector3(10f, 1f, -2f), 180f);
            PlaceHatch(samples, RoofPitch.Medium, woodMaterial, new Vector3(10f, 1f, -2f), 180f);

            const int Width = 1200;
            const int Height = 800;
            Texture2D[] views =
            {
                RenderView(MeasureBounds(root), Quaternion.Euler(30f, 235f, 0f), Width, Height),
                RenderView(MeasureBounds(samples), Quaternion.Euler(35f, 200f, 0f), Width, Height),
                RenderView(MeasureBounds(root), Quaternion.Euler(-25f, 60f, 0f), Width, Height, 0.75f)
            };
            Texture2D sheet = new(Width * views.Length, Height, TextureFormat.RGB24, false);
            for (int i = 0; i < views.Length; i++)
            {
                sheet.SetPixels(Width * i, 0, Width, Height, views[i].GetPixels());
            }

            sheet.Apply();
            string file = Path.Combine(outFolder, $"roof_{RoofCoverings.Id(covering)}.png");
            File.WriteAllBytes(file, sheet.EncodeToPNG());
            Debug.Log($"[Roofs] {covering} -> {file}");

            if (docsFolder != null)
            {
                // The house alone on a transparent background, for the documentation.
                samples.SetActive(false);
                GameObject.Find("Ground")?.SetActive(false);
                Texture2D picture = RenderView(MeasureBounds(root), Quaternion.Euler(30f, 235f, 0f), 1200, 800, 1.25f, default, true);
                File.WriteAllBytes(Path.Combine(docsFolder, $"roof_{RoofCoverings.Id(covering)}.png"), picture.EncodeToPNG());
            }
        }
    }

    private static void Place(GameObject root, RoofShape shape, RoofPitch pitch, RoofStyle style, Material[] materials, Vector3 position, float yaw, bool eaves = true)
    {
        GameObject piece = new($"{shape}_{pitch}");
        piece.transform.SetParent(root.transform, false);
        piece.transform.localPosition = position;
        piece.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        piece.AddComponent<MeshFilter>().sharedMesh = RoofMeshBuilder.Build(shape, pitch, style);
        piece.AddComponent<MeshRenderer>().sharedMaterials = materials;
        if (!eaves)
        {
            return;
        }

        foreach (RoofPart part in RoofMeshBuilder.Eaves(shape))
        {
            GameObject eave = new(part.ToString());
            eave.transform.SetParent(piece.transform, false);
            eave.AddComponent<MeshFilter>().sharedMesh = RoofMeshBuilder.Build(shape, pitch, style, part);
            eave.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }
    }

    private static void PlaceHatch(GameObject root, RoofPitch pitch, Material wood, Vector3 position, float yaw = 0f)
    {
        HatchPlacement hatch = RoofMeshBuilder.Hatch(pitch);
        GameObject pivot = new("hatch");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = position + Quaternion.Euler(0f, yaw, 0f) * hatch.Hinge;
        pivot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * hatch.Rotation * Quaternion.Euler(-70f, 0f, 0f);
        GameObject lid = new("lid");
        lid.transform.SetParent(pivot.transform, false);
        lid.AddComponent<MeshFilter>().sharedMesh = RoofMeshBuilder.Box(
            new Vector3(0f, RoofMeshBuilder.HatchThickness / 2f, hatch.Size / 2f),
            new Vector3(hatch.Size, RoofMeshBuilder.HatchThickness, hatch.Size),
            1f);
        lid.AddComponent<MeshRenderer>().sharedMaterial = wood;
    }

    private static void PlaceGable(GameObject root, RoofPitch pitch, Material wood, string meshFolder, Vector3 position)
    {
        GameObject gable = new("gable");
        gable.transform.SetParent(root.transform, false);
        gable.transform.localPosition = position;
        gable.AddComponent<MeshFilter>().sharedMesh = RoofMeshBuilder.GableBoards(pitch, out (Vector3 Position, Vector3 Outward, Vector3 Up)[] tips);
        gable.AddComponent<MeshRenderer>().sharedMaterial = wood;

        string headFile = meshFolder != null ? Path.Combine(meshFolder, "dragon_head.json") : null;
        if (headFile == null || !File.Exists(headFile))
        {
            return;
        }

        MeshJson data = JsonUtility.FromJson<MeshJson>(File.ReadAllText(headFile));
        foreach ((Vector3 tip, Vector3 outward, Vector3 up) in tips)
        {
            foreach (MeshPartJson part in data.parts)
            {
                Mesh mesh = new();
                mesh.vertices = ToVectors(part.vertices);
                mesh.normals = ToVectors(part.normals);
                mesh.uv = Enumerable.Range(0, part.uvs.Length / 2).Select(i => new Vector2(part.uvs[i * 2], part.uvs[i * 2 + 1])).ToArray();
                mesh.triangles = part.triangles;
                mesh.RecalculateBounds();
                GameObject head = new("dragon_head");
                head.transform.SetParent(gable.transform, false);
                head.AddComponent<MeshFilter>().sharedMesh = mesh;
                head.AddComponent<MeshRenderer>().sharedMaterial = wood;
                RoofMeshBuilder.PlaceGableHead(head.transform, mesh.bounds, tip, outward, up);
            }
        }
    }

    private static void SetUpStage()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.2f);
        Light sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(40f, 160f, 0f);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(6f, -2.5f, 0f);
        ground.transform.localScale = Vector3.one * 6f;
        Material grass = new(Shader.Find("Standard")) { color = new Color(0.33f, 0.4f, 0.22f) };
        grass.SetFloat("_Glossiness", 0f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = grass;
    }

    private static Material MakeMaterial(Texture2D albedo, Texture2D normal)
    {
        Material material = new(Shader.Find("Standard")) { mainTexture = albedo };
        material.SetFloat("_Glossiness", 0.08f);
        if (normal != null)
        {
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", normal);
        }

        return material;
    }

    private static Texture2D LoadTexture(string name)
    {
        string file = Path.Combine(textureFolder, name + ".jpg");
        return File.Exists(file) ? LoadFile(file) : null;
    }

    private static Texture2D LoadFile(string file)
    {
        if (textures.TryGetValue(file, out Texture2D cached))
        {
            return cached;
        }

        Texture2D image = new(2, 2, TextureFormat.RGBA32, true, file.Contains("_normal"));
        image.LoadImage(File.ReadAllBytes(file));
        image.wrapMode = TextureWrapMode.Repeat;
        textures[file] = image;
        return image;
    }

    private static Texture2D RenderView(Bounds bounds, Quaternion rotation, int width, int height, float zoom = 1f, Vector3 shift = default, bool transparent = false)
    {
        GameObject cameraObject = new("Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 35f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = transparent ? new Color(0f, 0f, 0f, 0f) : new Color(0.62f, 0.72f, 0.82f);
        float distance = bounds.extents.magnitude / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.62f * zoom;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 500f;
        camera.transform.position = bounds.center + shift - rotation * Vector3.forward * distance;
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
            // Edges were blended over a black clear colour; dividing by alpha keeps them from turning dark.
            Color32[] pixels = image.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.a > 0 && pixel.a < 255)
                {
                    pixels[i] = new Color32((byte)Math.Min(255, pixel.r * 255 / pixel.a), (byte)Math.Min(255, pixel.g * 255 / pixel.a), (byte)Math.Min(255, pixel.b * 255 / pixel.a), pixel.a);
                }
            }

            image.SetPixels32(pixels);
        }

        image.Apply();
        RenderTexture.active = previous;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        return image;
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

    private static Vector3[] ToVectors(float[] values)
    {
        return Enumerable.Range(0, values.Length / 3).Select(i => new Vector3(values[i * 3], values[i * 3 + 1], values[i * 3 + 2])).ToArray();
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
    private class MeshJson
    {
        public MeshPartJson[] parts;
    }

    [Serializable]
    private class MeshPartJson
    {
        public float[] vertices;
        public float[] normals;
        public float[] uvs;
        public int[] triangles;
    }
}
