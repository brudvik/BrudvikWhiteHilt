using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renders the White Hilt darkwood doors, walls, gable walls and double gate offline with the mod's own cutting code
/// (MeshData and GateJoinery, copied in by render_darkwood.ps1), from the vanilla darkwood gate read straight out of the
/// game's asset bundle. Writes a check sheet per piece (front, back, and opened from the side) and, with -dwDocs, the
/// pictures for the documentation. Run with <c>-executeMethod DarkwoodPreview.Render -dwGame &lt;valheim&gt; -dwOut &lt;dir&gt;</c>.
/// </summary>
public static class DarkwoodPreview
{
    private static readonly (string Name, JoineryKind Kind, float Height)[] pieces =
    {
        ("darkwood_door_3", JoineryKind.Door, 3f),
        ("darkwood_door_4", JoineryKind.Door, 4f),
        ("darkwood_gate_double", JoineryKind.DoubleGate, 4f),
        ("darkwood_wall_2x1", JoineryKind.Wall, 1f),
        ("darkwood_wall_2x2", JoineryKind.Wall, 2f),
        ("darkwood_wall_2x3", JoineryKind.Wall, 3f),
        ("darkwood_wall_2x4", JoineryKind.Wall, 4f),
        ("darkwood_wall_26", JoineryKind.Gable, 1f),
        ("darkwood_wall_45", JoineryKind.Gable, 2f),
        ("darkwood_wall_67", JoineryKind.Gable, 4f)
    };

    private static readonly Dictionary<GateSlot, Material> materials = new();

    /// <summary>
    /// Renders every piece.
    /// </summary>
    public static void Render()
    {
        string game = Argument("-dwGame");
        string outFolder = Argument("-dwOut");
        string docsFolder = TryArgument("-dwDocs");
        Directory.CreateDirectory(outFolder);
        string bundles = Path.Combine(game, @"valheim_Data\StreamingAssets\SoftRef\Bundles");
        AssetBundle items = AssetBundle.LoadFromFile(Path.Combine(bundles, "c4210710"));
        List<AssetBundle> others = Directory.GetFiles(bundles).Where(path => !path.EndsWith("c4210710")).Select(AssetBundle.LoadFromFile).Where(b => b != null).ToList();
        try
        {
            GameObject gatePrefab = items.LoadAllAssets<GameObject>().First(g => g.name == "darkwood_gate");
            foreach ((string name, JoineryKind kind, float height) in pieces)
            {
                // A new scene unloads materials nothing in it uses, so the gate is read again for every scene.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                SetUpStage(true);
                GateSource source = ReadGate(gatePrefab);
                JoineryModel model = GateJoinery.Build(kind, height, source);
                GameObject piece = Place(model, name, Vector3.zero, 0f);
                Bounds bounds = MeasureBounds(piece);
                Texture2D[] views =
                {
                    RenderView(bounds, Quaternion.Euler(8f, 0f, 0f), 700, 900),
                    RenderView(bounds, Quaternion.Euler(8f, 180f, 0f), 700, 900),
                    OpenAndRender(piece, bounds)
                };
                Texture2D sheet = new(700 * views.Length, 900, TextureFormat.RGB24, false);
                for (int i = 0; i < views.Length; i++)
                {
                    sheet.SetPixels(700 * i, 0, 700, 900, views[i].GetPixels());
                }

                sheet.Apply();
                File.WriteAllBytes(Path.Combine(outFolder, $"{name}.png"), sheet.EncodeToPNG());
                Debug.Log($"[Darkwood] {name}: {piece.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.vertexCount)} vertices");

                if (docsFolder != null)
                {
                    SetLeaves(piece, 0f);
                    GameObject.Find("Ground")?.SetActive(false);
                    Texture2D picture = RenderView(MeasureBounds(piece), Quaternion.Euler(12f, 30f, 0f), 900, 1000, 1.1f, default, true);
                    File.WriteAllBytes(Path.Combine(docsFolder, $"{name}.png"), picture.EncodeToPNG());
                }
            }

            if (docsFolder != null)
            {
                RenderFacade(gatePrefab, docsFolder);
            }
        }
        finally
        {
            items.Unload(true);
            others.ForEach(bundle => bundle.Unload(true));
        }
    }

    // A darkwood hall front: a house with the 3 m door under two gable walls, then the 4 m door and the double gate.
    private static void RenderFacade(GameObject gatePrefab, string docsFolder)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        SetUpStage(false);
        GateSource source = ReadGate(gatePrefab);
        GameObject root = new("facade");
        void Put(JoineryKind kind, float height, float x, float y = 0f, float yaw = 0f)
        {
            Place(GateJoinery.Build(kind, height, source), $"{kind}_{height}", new Vector3(x, y, 0f), yaw).transform.SetParent(root.transform, true);
        }

        Put(JoineryKind.Wall, 3f, -5f);
        Put(JoineryKind.Door, 3f, -3f);
        Put(JoineryKind.Gable, 2f, -5f, 3f, 0f);
        Put(JoineryKind.Gable, 2f, -3f, 3f, 180f);
        Put(JoineryKind.Wall, 4f, 0f);
        Put(JoineryKind.Door, 4f, 2f);
        Put(JoineryKind.DoubleGate, 4f, 5f);
        Put(JoineryKind.Wall, 4f, 8f);
        Texture2D picture = RenderView(MeasureBounds(root), Quaternion.Euler(10f, 20f, 0f), 1600, 800, 0.95f, default, true);
        File.WriteAllBytes(Path.Combine(docsFolder, "darkwood_facade.png"), picture.EncodeToPNG());
    }

    private static Texture2D OpenAndRender(GameObject piece, Bounds bounds)
    {
        SetLeaves(piece, 70f);
        Texture2D image = RenderView(bounds, Quaternion.Euler(25f, 150f, 0f), 700, 900, 1.2f);
        SetLeaves(piece, 0f);
        return image;
    }

    // Turns the leaves as GateLeafDriver does: the leaf with the vanilla door's angle, the mirrored one against it.
    private static void SetLeaves(GameObject piece, float angle)
    {
        foreach (Transform pivot in piece.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("leaf")))
        {
            pivot.localRotation = Quaternion.Euler(0f, pivot.name == "leaf_left" ? -angle : angle, 0f);
        }
    }

    private static GameObject Place(JoineryModel model, string name, Vector3 position, float yaw)
    {
        GameObject root = new(name);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        AddModel(root.transform, "New", model.Static["New"]);
        foreach (JoineryLeaf leaf in model.Leaves)
        {
            GameObject pivot = new(leaf.Mirrored ? "leaf_left" : "leaf");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = leaf.Pivot;
            AddModel(pivot.transform, "high", leaf.High);
        }

        return root;
    }

    private static void AddModel(Transform parent, string name, List<JoineryPart> parts)
    {
        if (parts.Count == 0)
        {
            return;
        }

        GameObject model = new(name);
        model.transform.SetParent(parent, false);
        model.AddComponent<MeshFilter>().sharedMesh = MeshData.ToMesh(name, parts.Select(part => part.Mesh).ToArray());
        model.AddComponent<MeshRenderer>().sharedMaterials = parts.Select(part => materials[part.Slot]).ToArray();
    }

    // As DarkwoodJoineryBase.ReadGate in the mod, with the game's materials redrawn in the Standard shader, which the
    // editor has (the game's Custom/Piece shader is not in the bundle).
    private static GateSource ReadGate(GameObject prefab)
    {
        GameObject gate = UnityEngine.Object.Instantiate(prefab);
        Transform root = gate.transform;
        Transform door = root.Find("door");
        GateSource source = new() { DoorPivot = door.localPosition };
        foreach ((GateSlot slot, Transform holder, string child) in new[]
        {
            (GateSlot.Leaf, door, "high"), (GateSlot.LeafLow, door, "low"),
            (GateSlot.New, root.Find("New"), "high"), (GateSlot.NewLow, root.Find("New"), "low"),
            (GateSlot.Worn, root.Find("Worn"), "high"), (GateSlot.WornLow, root.Find("Worn"), "low")
        })
        {
            Transform part = holder.Find(child);
            Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
            bool leaf = slot == GateSlot.Leaf || slot == GateSlot.LeafLow;
            Matrix4x4 matrix = leaf ? Matrix4x4.identity : root.worldToLocalMatrix * part.localToWorldMatrix;
            source.Meshes[slot] = MeshData.FromMesh(mesh, 0, matrix);
            Material game = part.GetComponent<MeshRenderer>().sharedMaterial;
            Material material = new(Shader.Find("Standard")) { mainTexture = game.mainTexture, color = game.HasProperty("_Color") ? game.color : Color.white };
            material.SetFloat("_Glossiness", 0.2f);
            if (game.HasProperty("_BumpMap") && game.GetTexture("_BumpMap") != null)
            {
                material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_BumpMap", game.GetTexture("_BumpMap"));
            }

            materials[slot] = material;
            if (slot == GateSlot.Leaf)
            {
                source.LeafOffset = part.localPosition;
            }
        }

        UnityEngine.Object.DestroyImmediate(gate);
        return source;
    }

    private static void SetUpStage(bool ground)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.6f, 0.64f, 0.7f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.5f, 0.47f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.25f);
        Light sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.2f;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(35f, 200f, 0f);
        Light fill = new GameObject("Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.6f;
        fill.transform.rotation = Quaternion.Euler(30f, 20f, 0f);
        if (!ground)
        {
            return;
        }

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Ground";
        plane.transform.localScale = Vector3.one * 3f;
        Material grass = new(Shader.Find("Standard")) { color = new Color(0.33f, 0.4f, 0.22f) };
        grass.SetFloat("_Glossiness", 0f);
        plane.GetComponent<MeshRenderer>().sharedMaterial = grass;
    }

    private static Texture2D RenderView(Bounds bounds, Quaternion rotation, int width, int height, float zoom = 1f, Vector3 shift = default, bool transparent = false)
    {
        GameObject cameraObject = new("Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 30f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = transparent ? new Color(0f, 0f, 0f, 0f) : new Color(0.78f, 0.82f, 0.86f);
        float distance = bounds.extents.magnitude / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * zoom;
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
}
