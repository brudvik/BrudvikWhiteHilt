using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BrudvikWhiteHilt.Navigation.Portraits;

/// <summary>
/// Takes your own portrait in the main menu: when a character is shown, a hidden copy of it is loaded far below the
/// preview, stripped of helmet and weapons, and rendered once with its own camera and lights. A new picture is only
/// taken when the look (body, hair, beard, colours) has changed since the last one.
/// </summary>
public static class PortraitCapture
{
    // Raise when the framing or lighting changes, so every portrait is taken again.
    private const int Version = 1;
    private const int Supersample = 2;
    private const int SettleFrames = 4;
    private const float CloneDepth = 300f;
    private const float Distance = 1.05f;
    private const float FieldOfView = 24f;
    private const float FaceHeight = 0.09f;

    private static Coroutine running;
    private static GameObject clone;

    /// <summary>
    /// Starts a portrait for the character shown in the menu. Called after the game sets up the character preview.
    /// </summary>
    /// <param name="startup">The main menu.</param>
    /// <param name="profile">The selected character, or null while a new one is created.</param>
    public static void OnPreview(FejdStartup startup, PlayerProfile profile)
    {
        Stop(startup);
        if (profile == null || VisualHelper.IsHeadless || !PortraitSettings.Enabled.Value || startup.m_playerPrefab == null)
        {
            return;
        }

        running = startup.StartCoroutine(Capture(startup, profile));
    }

    private static void Stop(FejdStartup startup)
    {
        if (running != null)
        {
            startup.StopCoroutine(running);
            running = null;
        }

        if (clone != null)
        {
            Object.Destroy(clone);
            clone = null;
        }
    }

    private static IEnumerator Capture(FejdStartup startup, PlayerProfile profile)
    {
        VisEquipment visual = Spawn(startup, profile);
        if (visual == null)
        {
            running = null;
            Stop(startup);
            yield break;
        }

        // Equipment and hair are built by VisEquipment's own update, and the animator needs a frame to pose.
        for (int i = 0; i < SettleFrames; i++)
        {
            yield return null;
        }

        try
        {
            TakeIfChanged(profile.GetPlayerID(), visual);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Portrait: could not take your portrait: {ex}");
        }

        running = null;
        Stop(startup);
    }

    private static VisEquipment Spawn(FejdStartup startup, PlayerProfile profile)
    {
        Transform point = startup.m_characterPreviewPoint;
        ZNetView.m_forceDisableInit = true;
        try
        {
            clone = Object.Instantiate(startup.m_playerPrefab, point.position + Vector3.down * CloneDepth, point.rotation);
        }
        finally
        {
            ZNetView.m_forceDisableInit = false;
        }

        clone.name = "WhiteHiltPortraitModel";
        Object.Destroy(clone.GetComponent<Rigidbody>());
        try
        {
            profile.LoadPlayerData(clone.GetComponent<Player>());
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Portrait: could not load {profile.GetName()}: {ex.Message}");
            return null;
        }

        return clone.GetComponent<VisEquipment>();
    }

    private static void TakeIfChanged(long playerId, VisEquipment visual)
    {
        string look = Look(visual);
        if (PortraitStore.TryLoadOwn(playerId, out string savedLook, out _, out _) && savedLook == look)
        {
            return;
        }

        visual.m_helmetItem = 0;
        visual.m_leftItem = 0;
        visual.m_rightItem = 0;
        visual.m_leftBackItem = 0;
        visual.m_rightBackItem = 0;
        visual.UpdateVisuals();

        byte[] raw = Render(visual);
        string hash = PortraitStore.Hash(raw);
        PortraitStore.SaveOwn(playerId, look, hash, PortraitStore.Compress(raw));
        Jotunn.Logger.LogInfo($"Portrait: took a new portrait ({hash.Substring(0, 8)}).");
    }

    private static string Look(VisEquipment visual)
    {
        return string.Join("|", Version.ToString(CultureInfo.InvariantCulture), visual.m_modelIndex.ToString(CultureInfo.InvariantCulture),
            visual.m_hairItem.ToString(CultureInfo.InvariantCulture), visual.m_beardItem.ToString(CultureInfo.InvariantCulture),
            Colour(visual.m_skinColor), Colour(visual.m_hairColor));
    }

    private static string Colour(Vector3 colour)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:F3},{1:F3},{2:F3}", colour.x, colour.y, colour.z);
    }

    private static byte[] Render(VisEquipment visual)
    {
        Transform root = visual.transform;
        int layer = FreeLayer();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }

        Transform head = FindHead(visual);
        Vector3 face = head.position + root.up * FaceHeight;
        int big = PortraitStore.Size * Supersample;

        RenderTexture target = RenderTexture.GetTemporary(big, big, 24, RenderTextureFormat.ARGB32);
        GameObject rig = new("WhiteHiltPortraitRig");
        List<Light> silenced = new();
        AmbientMode ambientMode = RenderSettings.ambientMode;
        Color ambientLight = RenderSettings.ambientLight;
        float ambientIntensity = RenderSettings.ambientIntensity;
        bool fog = RenderSettings.fog;
        RenderTexture active = RenderTexture.active;
        try
        {
            Camera camera = rig.AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << layer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = FieldOfView;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 10f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.targetTexture = target;
            camera.transform.position = face + root.forward * Distance;
            camera.transform.LookAt(face, root.up);

            // The menu's own lights (campfire, moon) would tint the portrait; they are off only for these two renders.
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.enabled)
                {
                    light.enabled = false;
                    silenced.Add(light);
                }
            }

            AddLight(rig, layer, root.forward * 1f - root.right * 0.6f + root.up * 0.8f, 1.1f, new Color(1f, 0.96f, 0.9f));
            AddLight(rig, layer, root.forward * 1f + root.right * 0.8f + root.up * 0.2f, 0.45f, new Color(0.85f, 0.9f, 1f));
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = false;

            Texture2D readback = new(big, big, TextureFormat.RGBA32, false);
            Color32[] onBlack = Shoot(camera, target, readback, Color.black);
            Color32[] onWhite = Shoot(camera, target, readback, Color.white);
            Object.Destroy(readback);
            return Combine(onBlack, onWhite, big);
        }
        finally
        {
            foreach (Light light in silenced)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }

            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.fog = fog;
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target);
            Object.Destroy(rig);
        }
    }

    private static Transform FindHead(VisEquipment visual)
    {
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Head")
            {
                return child;
            }
        }

        return visual.m_helmet != null ? visual.m_helmet : visual.transform;
    }

    private static int FreeLayer()
    {
        for (int layer = 31; layer >= 8; layer--)
        {
            if (string.IsNullOrEmpty(LayerMask.LayerToName(layer)))
            {
                return layer;
            }
        }

        return 31;
    }

    private static void AddLight(GameObject rig, int layer, Vector3 towardLight, float intensity, Color colour)
    {
        GameObject holder = new("Light");
        holder.transform.SetParent(rig.transform, false);
        holder.transform.rotation = Quaternion.LookRotation(-towardLight.normalized);
        Light light = holder.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = colour;
        light.shadows = LightShadows.None;
        light.cullingMask = 1 << layer;
    }

    private static Color32[] Shoot(Camera camera, RenderTexture target, Texture2D readback, Color background)
    {
        camera.backgroundColor = background;
        camera.Render();
        RenderTexture.active = target;
        readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
        return readback.GetPixels32();
    }

    // Shader alpha is not reliable, so coverage is measured as how much the white background shows through.
    private static byte[] Combine(Color32[] onBlack, Color32[] onWhite, int big)
    {
        int size = PortraitStore.Size;
        byte[] raw = new byte[PortraitStore.RawLength];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int sy = 0; sy < Supersample; sy++)
                {
                    for (int sx = 0; sx < Supersample; sx++)
                    {
                        int i = (y * Supersample + sy) * big + x * Supersample + sx;
                        Color32 black = onBlack[i];
                        Color32 white = onWhite[i];
                        float through = (white.r - black.r + white.g - black.g + white.b - black.b) / (3f * 255f);
                        a += Mathf.Clamp01(1f - through);
                        r += black.r;
                        g += black.g;
                        b += black.b;
                    }
                }

                int samples = Supersample * Supersample;
                int o = (y * size + x) * 4;
                float coverage = a / samples;
                float scale = a > 0.001f ? 1f / a : 0f;
                raw[o] = (byte)Mathf.Clamp(r * scale, 0f, 255f);
                raw[o + 1] = (byte)Mathf.Clamp(g * scale, 0f, 255f);
                raw[o + 2] = (byte)Mathf.Clamp(b * scale, 0f, 255f);
                raw[o + 3] = (byte)Mathf.RoundToInt(coverage * 255f);
            }
        }

        return raw;
    }
}
