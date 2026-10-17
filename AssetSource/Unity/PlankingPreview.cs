using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrudvikWhiteHilt.Pieces.Structure.Darkwood;
using BrudvikWhiteHilt.Pieces.Structure.Planking;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renders the White Hilt wood floors, plank walls, lattices and braces offline with the mod's own cutting code
/// (MeshData, Sawing, FloorCutting, WallCutting and Joinery, copied in by render_planking.ps1), from the vanilla wood floor, wall and window read
/// straight out of the game's asset bundles. Writes a check sheet per piece (floors from above, the side and below;
/// walls from the front, the back and askew) and a tower of vanilla walls on the octagon; with -plDocs also the
/// pictures for the documentation. Run with
/// <c>-executeMethod PlankingPreview.Render -plGame &lt;valheim&gt; -plOut &lt;dir&gt;</c>.
/// </summary>
public static class PlankingPreview
{
    private static readonly (string Name, FloorShape Shape)[] floors =
    {
        ("wood_floor_triangle", FloorShape.Triangle(2f)),
        ("wood_floor_triangle_1x1", FloorShape.Triangle(1f)),
        ("wood_floor_angled", FloorShape.AngledEnd(false)),
        ("wood_floor_angled_mirrored", FloorShape.AngledEnd(true)),
        ("wood_floor_octagon", FloorShape.Octagon(2f)),
        ("wood_floor_octagon_4", FloorShape.Octagon(4f))
    };

    private static readonly (string Name, Func<MeshData, MeshData, MeshData> Shape, bool Window)[] walls =
    {
        ("woodwall_4x2", (tile, plank) => WallCutting.Lay(tile, 4, 2), false),
        ("woodwall_4x4", (tile, plank) => WallCutting.Lay(tile, 4, 4), false),
        ("woodwall_window", (tile, plank) => WallCutting.CutOpening(tile, WallCutting.Window), true),
        ("wood_lattice_2x2", (tile, plank) => Joinery.Lattice(plank, 2f, 2f), false),
        ("wood_lattice_2x1", (tile, plank) => Joinery.Lattice(plank, 2f, 1f), false),
        ("wood_brace_05", (tile, plank) => Joinery.Brace(plank, 0.5f, 0.12f), false),
        ("wood_brace_1", (tile, plank) => Joinery.Brace(plank, 1f, 0.14f), false),
        ("wood_brace_2", (tile, plank) => Joinery.Brace(plank, 2f, 0.16f), false)
    };

    /// <summary>
    /// Renders every floor.
    /// </summary>
    public static void Render()
    {
        string game = Argument("-plGame");
        string outFolder = Argument("-plOut");
        string docsFolder = TryArgument("-plDocs");
        Directory.CreateDirectory(outFolder);
        string bundles = Path.Combine(game, @"valheim_Data\StreamingAssets\SoftRef\Bundles");
        List<AssetBundle> loaded = Directory.GetFiles(bundles).Select(AssetBundle.LoadFromFile).Where(b => b != null).ToList();
        try
        {
            List<GameObject> prefabs = loaded.Where(b => !b.isStreamedSceneAssetBundle).SelectMany(b => b.LoadAllAssets<GameObject>()).ToList();
            GameObject floorPrefab = prefabs.First(g => g.name == "wood_floor");
            GameObject wallPrefab = prefabs.First(g => g.name == "woodwall");
            GameObject windowPrefab = prefabs.First(g => g.name == "wood_window");
            foreach ((string name, FloorShape shape) in floors)
            {
                // A new scene unloads materials nothing in it uses, so the floor is read again for every scene.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                SetUpStage(true);
                (MeshData tile, Material material) = ReadModel(floorPrefab);
                GameObject piece = Place(FloorCutting.Cut(shape, tile), material, name, Vector3.up * 0.02f, 0f);
                Bounds bounds = MeasureBounds(piece);
                Texture2D[] views =
                {
                    RenderView(bounds, Quaternion.Euler(60f, 20f, 0f), 700, 700),
                    RenderView(bounds, Quaternion.Euler(12f, 160f, 0f), 700, 700, 0.7f),
                    RenderView(bounds, Quaternion.Euler(-35f, 200f, 0f), 700, 700, 1f, default, false, true)
                };
                Texture2D sheet = new(700 * views.Length, 700, TextureFormat.RGB24, false);
                for (int i = 0; i < views.Length; i++)
                {
                    sheet.SetPixels(700 * i, 0, 700, 700, views[i].GetPixels());
                }

                sheet.Apply();
                File.WriteAllBytes(Path.Combine(outFolder, $"{name}.png"), sheet.EncodeToPNG());
                Debug.Log($"[Planking] {name}: {piece.GetComponent<MeshFilter>().sharedMesh.vertexCount} vertices");

                if (docsFolder != null)
                {
                    GameObject.Find("Ground")?.SetActive(false);
                    Texture2D picture = RenderView(MeasureBounds(piece), Quaternion.Euler(45f, 25f, 0f), 900, 800, 1.1f, default, true);
                    File.WriteAllBytes(Path.Combine(docsFolder, $"{name}.png"), picture.EncodeToPNG());
                }
            }

            foreach ((string name, Func<MeshData, MeshData, MeshData> shape, bool window) in walls)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                SetUpStage(true);
                (MeshData tile, Material material) = ReadModel(wallPrefab);
                GameObject piece = Place(shape(tile, ReadPlank(wallPrefab)), material, name, Vector3.zero, 0f);
                piece.transform.position = Vector3.up * (MeasureBounds(piece).extents.y - MeasureBounds(piece).center.y);
                if (window)
                {
                    HangShutter(piece, windowPrefab);
                }

                Bounds bounds = MeasureBounds(piece);
                Texture2D[] views =
                {
                    RenderView(bounds, Quaternion.Euler(8f, 0f, 0f), 700, 700),
                    RenderView(bounds, Quaternion.Euler(8f, 180f, 0f), 700, 700),
                    RenderView(bounds, Quaternion.Euler(20f, 40f, 0f), 700, 700)
                };
                Texture2D sheet = new(700 * views.Length, 700, TextureFormat.RGB24, false);
                for (int i = 0; i < views.Length; i++)
                {
                    sheet.SetPixels(700 * i, 0, 700, 700, views[i].GetPixels());
                }

                sheet.Apply();
                File.WriteAllBytes(Path.Combine(outFolder, $"{name}.png"), sheet.EncodeToPNG());
                Debug.Log($"[Planking] {name}: {piece.GetComponent<MeshFilter>().sharedMesh.vertexCount} vertices");

                if (docsFolder != null)
                {
                    GameObject.Find("Ground")?.SetActive(false);
                    Texture2D picture = RenderView(MeasureBounds(piece), Quaternion.Euler(12f, 25f, 0f), 900, 900, 1.1f, default, true);
                    File.WriteAllBytes(Path.Combine(docsFolder, $"{name}.png"), picture.EncodeToPNG());
                }
            }

            RenderTower(floorPrefab, wallPrefab, outFolder, docsFolder);
            RenderFrame(prefabs, wallPrefab, outFolder, docsFolder);
        }
        finally
        {
            loaded.ForEach(bundle => bundle.Unload(true));
        }
    }

    // The 2 m octagon with a vanilla wall on every side and a triangle bay beside a square floor.
    private static void RenderTower(GameObject floorPrefab, GameObject wallPrefab, string outFolder, string docsFolder)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        SetUpStage(docsFolder == null);
        (MeshData tile, Material material) = ReadModel(floorPrefab);
        GameObject root = new("tower");
        FloorShape octagon = FloorShape.Octagon(2f);
        Place(FloorCutting.Cut(octagon, tile), material, "octagon", Vector3.zero, 0f).transform.SetParent(root.transform, true);
        Vector2[] corners = octagon.Corners;
        for (int i = 0; i < corners.Length; i++)
        {
            // Every side but the front one, so the floor shows.
            if (i == 0)
            {
                continue;
            }

            Vector2 a = corners[i], b = corners[(i + 1) % corners.Length];
            Vector2 middle = (a + b) / 2f;
            float yaw = -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            GameObject wall = Instantiate(wallPrefab, new Vector3(middle.x, 1f, middle.y), Quaternion.Euler(0f, yaw, 0f));
            wall.transform.SetParent(root.transform, true);
        }

        Place(FloorCutting.Cut(FloorShape.Triangle(2f), tile), material, "bay", new Vector3(5f, 0f, -1f), 0f).transform.SetParent(root.transform, true);
        Place(FloorCutting.Cut(FloorShape.Triangle(2f), tile), material, "bay2", new Vector3(5f, 0f, -1f), 180f).transform.SetParent(root.transform, true);
        Place(FloorCutting.Cut(FloorShape.AngledEnd(false), tile), material, "angled", new Vector3(7.5f, 0f, -1f), 0f).transform.SetParent(root.transform, true);
        Place(FloorCutting.Cut(FloorShape.AngledEnd(true), tile), material, "angled2", new Vector3(8.5f, 0f, -1f), 0f).transform.SetParent(root.transform, true);
        Texture2D picture = RenderView(MeasureBounds(root), Quaternion.Euler(40f, 15f, 0f), 1600, 900, 0.85f, default, docsFolder != null);
        File.WriteAllBytes(Path.Combine(docsFolder ?? outFolder, "wood_floor_tower.png"), picture.EncodeToPNG());
    }

    // Two vanilla poles under a vanilla beam, a brace in each corner and a lattice railing between them.
    private static void RenderFrame(List<GameObject> prefabs, GameObject wallPrefab, string outFolder, string docsFolder)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        SetUpStage(docsFolder == null);
        (_, Material material) = ReadModel(wallPrefab);
        MeshData plank = ReadPlank(wallPrefab);
        GameObject root = new("frame");
        GameObject pole = prefabs.First(g => g.name == "wood_pole2");
        GameObject beam = prefabs.First(g => g.name == "wood_beam");
        foreach (float x in new[] { 0f, 2f, 4f })
        {
            Instantiate(pole, new Vector3(x, 1f, 0f), Quaternion.identity).transform.SetParent(root.transform, true);
        }

        foreach (float x in new[] { 1f, 3f })
        {
            Instantiate(beam, new Vector3(x, 2f, 0f), Quaternion.identity).transform.SetParent(root.transform, true);
            Place(Joinery.Lattice(plank, 2f, 1f), material, "lattice", new Vector3(x, 0.5f, 0f), 0f).transform.SetParent(root.transform, true);
        }

        Place(Joinery.Brace(plank, 0.5f, 0.12f), material, "knee", new Vector3(0f, 2f, 0f), 0f).transform.SetParent(root.transform, true);
        Place(Joinery.Brace(plank, 0.5f, 0.12f), material, "knee2", new Vector3(2f, 2f, 0f), 180f).transform.SetParent(root.transform, true);
        Place(Joinery.Brace(plank, 0.5f, 0.12f), material, "knee3", new Vector3(2f, 2f, 0f), 0f).transform.SetParent(root.transform, true);
        Place(Joinery.Brace(plank, 0.5f, 0.12f), material, "knee4", new Vector3(4f, 2f, 0f), 180f).transform.SetParent(root.transform, true);
        Texture2D picture = RenderView(MeasureBounds(root), Quaternion.Euler(15f, 25f, 0f), 1400, 800, 0.85f, default, docsFolder != null);
        File.WriteAllBytes(Path.Combine(docsFolder ?? outFolder, "wood_frame.png"), picture.EncodeToPNG());
    }

    // As PlankWallPieceBase.ReadWall in the mod: the plank of the vanilla wall, a unit block with the grain along z.
    private static MeshData ReadPlank(GameObject wallPrefab)
    {
        return MeshData.FromMesh(wallPrefab.transform.Find("New/high/plank").GetComponent<MeshFilter>().sharedMesh, 0, Matrix4x4.identity);
    }

    // As PlankWallWindow.Finish in the mod: the vanilla window's shutter and post moved to the opening's right side.
    private static void HangShutter(GameObject piece, GameObject windowPrefab)
    {
        GameObject window = Instantiate(windowPrefab, Vector3.zero, Quaternion.identity, "door");
        Transform shutter = window.transform.Find("door");
        Vector3 hinge = new(WallCutting.Window.xMax, WallCutting.Window.center.y, 0f);
        foreach (Transform part in new[] { shutter, window.transform.Find("New") })
        {
            part.SetParent(piece.transform, false);
            part.localPosition += hinge;
        }

        foreach (Transform far in piece.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Low").ToList())
        {
            UnityEngine.Object.DestroyImmediate(far.gameObject);
        }

        UnityEngine.Object.DestroyImmediate(window);
    }

    // An instance of a vanilla piece with its materials redrawn in the Standard shader, and nothing but its new look.
    private static GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, params string[] keep)
    {
        GameObject copy = UnityEngine.Object.Instantiate(prefab);
        foreach (Transform child in copy.transform.Cast<Transform>().Where(child => !child.name.Equals("New", StringComparison.OrdinalIgnoreCase) && !keep.Contains(child.name)).ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (MeshRenderer renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(Redraw).ToArray();
        }

        foreach (LODGroup group in copy.GetComponentsInChildren<LODGroup>(true))
        {
            group.enabled = false;
        }

        // Only the near model: the far one would be drawn on top of it without the LOD group.
        foreach (Transform far in copy.GetComponentsInChildren<Transform>(true).Where(t => t.name.ToLowerInvariant().Contains("lod")).ToList())
        {
            UnityEngine.Object.DestroyImmediate(far.gameObject);
        }

        copy.transform.SetPositionAndRotation(position, rotation * copy.transform.rotation);
        return copy;
    }

    private static GameObject Place(MeshData mesh, Material material, string name, Vector3 position, float yaw)
    {
        GameObject root = new(name);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        root.AddComponent<MeshFilter>().sharedMesh = MeshData.ToMesh(name, mesh);
        root.AddComponent<MeshRenderer>().sharedMaterial = material;
        return root;
    }

    // As WoodFloorPieceBase.ReadFloor and PlankWallPieceBase.ReadWall in the mod, the new model only, with the game's
    // material redrawn in the Standard shader.
    private static (MeshData Tile, Material Material) ReadModel(GameObject prefab)
    {
        Transform root = prefab.transform;
        MeshFilter filter = root.Find("New").Cast<Transform>()
            .Where(child => child.gameObject.activeSelf && child.name.StartsWith("_Combined"))
            .Select(child => child.GetComponent<MeshFilter>())
            .First(found => found != null && found.sharedMesh != null);
        Matrix4x4 matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        return (MeshData.FromMesh(filter.sharedMesh, 0, matrix), Redraw(filter.GetComponent<MeshRenderer>().sharedMaterial));
    }

    private static Material Redraw(Material game)
    {
        if (game == null)
        {
            return null;
        }

        Material material = new(Shader.Find("Standard")) { mainTexture = game.mainTexture, color = game.HasProperty("_Color") ? game.color : Color.white };
        material.SetFloat("_Glossiness", 0.15f);
        if (game.HasProperty("_BumpMap") && game.GetTexture("_BumpMap") != null)
        {
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", game.GetTexture("_BumpMap"));
        }

        return material;
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
        sun.transform.rotation = Quaternion.Euler(50f, 200f, 0f);
        Light fill = new GameObject("Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.6f;
        fill.transform.rotation = Quaternion.Euler(-40f, 20f, 0f);
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

    private static Texture2D RenderView(Bounds bounds, Quaternion rotation, int width, int height, float zoom = 1f, Vector3 shift = default, bool transparent = false, bool hideGround = false)
    {
        GameObject ground = GameObject.Find("Ground");
        if (hideGround && ground != null)
        {
            ground.SetActive(false);
        }

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
        if (hideGround && ground != null)
        {
            ground.SetActive(true);
        }

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
