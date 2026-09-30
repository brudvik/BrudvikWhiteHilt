using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renders every creature prefab in Assets/Creatures (built by <see cref="BuildCreatures"/>) as a contact sheet: one row
/// per animator state, four moments of its clip, each seen from the front (+z looks at the camera) and from the side.
/// Checks orientation, materials and animations before the game. Run with
/// <c>-executeMethod CreaturePreview.Render -previewOut &lt;dir&gt;</c> (no -nographics); see AssetSource/Preview/render_creatures.ps1.
/// </summary>
public static class CreaturePreview
{
    private const int Tile = 256;
    private const int Moments = 4;

    /// <summary>
    /// Renders the sheets to &lt;previewOut&gt;/&lt;creature&gt;_anim.png.
    /// </summary>
    public static void Render()
    {
        string outFolder = Argument("-previewOut");
        Directory.CreateDirectory(outFolder);
        foreach (string path in Directory.GetFiles("Assets/Creatures", "*_visual.prefab"))
        {
            RenderCreature(path.Replace('\\', '/'), outFolder);
        }
    }

    private static void RenderCreature(string path, string outFolder)
    {
        GameObject instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        Animator animator = instance.GetComponentInChildren<Animator>();
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();

        GameObject lightObject = new("light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

        GameObject cameraObject = new("camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.16f, 0.2f, 0.26f);
        RenderTexture target = new(Tile, Tile, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        camera.targetTexture = target;

        Texture2D sheet = new(Tile * Moments * 2, Tile * clips.Length, TextureFormat.RGB24, false);
        for (int row = 0; row < clips.Length; row++)
        {
            AnimationClip clip = clips[row];
            for (int moment = 0; moment < Moments; moment++)
            {
                clip.SampleAnimation(animator.gameObject, clip.length * moment / (Moments - 1));
                // Skinning does not update between renders in batch mode, so the sampled pose is baked into plain meshes.
                GameObject baked = Bake(instance);
                Bounds bounds = Measure(baked);
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 1.1f;
                camera.orthographicSize = size / 2f;
                camera.farClipPlane = size * 6f;
                for (int view = 0; view < 2; view++)
                {
                    Vector3 direction = view == 0 ? Vector3.forward : Vector3.right;
                    cameraObject.transform.position = bounds.center + direction * size * 2f;
                    cameraObject.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
                    camera.Render();
                    RenderTexture.active = target;
                    Texture2D tile = new(Tile, Tile, TextureFormat.RGB24, false);
                    tile.ReadPixels(new Rect(0, 0, Tile, Tile), 0, 0);
                    tile.Apply();
                    sheet.SetPixels((moment * 2 + view) * Tile, (clips.Length - 1 - row) * Tile, Tile, Tile, tile.GetPixels());
                    UnityEngine.Object.DestroyImmediate(tile);
                }

                UnityEngine.Object.DestroyImmediate(baked);
            }

            Debug.Log($"[WhiteHilt] Preview row {row}: {clip.name} ({clip.length:F2} s, loop {clip.isLooping}, events {clip.events.Length})");
        }

        RenderTexture.active = null;
        string name = Path.GetFileNameWithoutExtension(path).Replace("_visual", string.Empty);
        File.WriteAllBytes(Path.Combine(outFolder, $"{name}_anim.png"), sheet.EncodeToPNG());
        RenderPortrait(instance, animator, clips, camera, Path.Combine(outFolder, $"{name}.png"));
        UnityEngine.Object.DestroyImmediate(instance);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        UnityEngine.Object.DestroyImmediate(lightObject);
        Debug.Log($"[WhiteHilt] Preview of {name}: {clips.Length} rows");
    }

    // A three-quarter view on a transparent background, mid-attack if the creature has one, for the documentation.
    private static void RenderPortrait(GameObject instance, Animator animator, AnimationClip[] clips, Camera camera, string file)
    {
        const int size = 480;
        AnimationClip clip = clips.FirstOrDefault(each => each.events.Length > 0) ?? clips[0];
        clip.SampleAnimation(animator.gameObject, clip.length * (clip.events.Length > 0 ? clip.events[0].time / clip.length * 0.8f : 0.4f));
        GameObject baked = Bake(instance);
        Bounds bounds = Measure(baked);
        float extent = bounds.extents.magnitude;
        RenderTexture target = new(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        camera.targetTexture = target;
        camera.orthographic = false;
        camera.fieldOfView = 30f;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        Vector3 direction = new Vector3(0.8f, 0.35f, 1f).normalized;
        camera.transform.position = bounds.center + direction * extent / Mathf.Sin(15f * Mathf.Deg2Rad);
        camera.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
        camera.farClipPlane = extent * 10f;
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new(size, size, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        image.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(file, image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(baked);
    }

    private static GameObject Bake(GameObject instance)
    {
        GameObject baked = new("baked");
        foreach (SkinnedMeshRenderer skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skinned.enabled = false;
            Mesh mesh = new();
            skinned.BakeMesh(mesh, true);
            GameObject part = new(skinned.name);
            part.transform.SetParent(baked.transform, false);
            part.transform.SetPositionAndRotation(skinned.transform.position, skinned.transform.rotation);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = skinned.sharedMaterials;
        }

        return baked;
    }

    private static Bounds Measure(GameObject root)
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
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : throw new ArgumentException($"Missing {name}");
    }
}
