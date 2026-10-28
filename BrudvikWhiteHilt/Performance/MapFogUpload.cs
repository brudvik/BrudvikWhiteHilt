using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Uncovers the map around the player as the game's <c>Minimap.Explore(Vector3, float)</c> does, but sends only the
/// changed rectangle of the fog texture to the graphics card. The game calls <c>Texture2D.Apply</c> on the whole
/// 2048 x 2048 fog texture (8 MB) whenever a pixel is uncovered, every two seconds while exploring.
/// <para>
/// The pixels are still uncovered one by one through the game's <c>Explore(int, int)</c>, so its patches (the
/// Exploration skill counts new pixels there) and the texture's own copy in memory stay as they were. Only the upload
/// changes: the rectangle is copied from that copy into a small texture of the same format, which is uploaded and then
/// copied into place on the graphics card. Every other full upload (loading the map, map tables) still sends it all.
/// A device that cannot copy between textures, or any failure, gets the game's full upload.
/// </para>
/// </summary>
public static class MapFogUpload
{
    // Larger rectangles (a very wide explore radius) are uploaded whole, as the game does.
    private const int MaxStageSize = 256;

    private static Texture2D stage;
    private static Texture2D checkedFog;
    private static bool fogUsable;
    private static int bytesPerPixel;
    private static bool failed;
    private static bool standDownLogged;

    /// <summary>
    /// Uncovers the circle around a point, in place of the game's method.
    /// </summary>
    /// <param name="map">The minimap.</param>
    /// <param name="point">World position.</param>
    /// <param name="radius">Radius in metres.</param>
    /// <returns>True when it was done here; false to let the game's method run.</returns>
    public static bool TryExplore(Minimap map, Vector3 point, float radius)
    {
        if (!StreamingSettings.MapFogUpload.Value || failed || map.m_fogTexture == null || map.m_explored == null)
        {
            return false;
        }

        if (CommunityPatchCoexistence.HasActiveFix("MinimapFogUploadPatch"))
        {
            if (!standDownLogged)
            {
                standDownLogged = true;
                Jotunn.Logger.LogInfo("Map fog upload: Valheim Community Patch has its own fix for this, so it is used.");
            }

            return false;
        }

        Texture2D fog = map.m_fogTexture;
        int size = map.m_textureSize;
        int reach = (int)Mathf.Ceil(radius / map.m_pixelSize);
        map.WorldToPixel(point, out int px, out int py);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        try
        {
            // The game's loop: the square around the pixel, minus the corners outside the circle.
            for (int y = py - reach; y <= py + reach; y++)
            {
                if (y < 0 || y >= size)
                {
                    continue;
                }

                for (int x = px - reach; x <= px + reach; x++)
                {
                    if (x < 0 || x >= size || new Vector2(x - px, y - py).magnitude > reach || !map.Explore(x, y))
                    {
                        continue;
                    }

                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }

            if (minX > maxX)
            {
                return true;
            }

            Upload(fog, size, minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        catch (Exception ex)
        {
            failed = true;
            Jotunn.Logger.LogWarning("Map fog upload: the partial upload failed, so the whole texture is sent from now on: "
                + ex.Message);
            fog.Apply();
        }

        return true;
    }

    // Sends one rectangle of the texture's memory copy to the graphics card.
    private static void Upload(Texture2D fog, int size, int x0, int y0, int width, int height)
    {
        if (width > MaxStageSize || height > MaxStageSize || !Usable(fog, size) || !EnsureStage(fog, Math.Max(width, height)))
        {
            fog.Apply();
            return;
        }

        NativeArray<byte> source = fog.GetPixelData<byte>(0);
        NativeArray<byte> target = stage.GetPixelData<byte>(0);
        int rowBytes = width * bytesPerPixel;
        for (int row = 0; row < height; row++)
        {
            NativeArray<byte>.Copy(source, ((y0 + row) * size + x0) * bytesPerPixel, target, row * stage.width * bytesPerPixel,
                rowBytes);
        }

        stage.Apply(false, false);
        Graphics.CopyTexture(stage, 0, 0, 0, 0, width, height, fog, 0, 0, x0, y0);
    }

    // Decided once per fog texture; each world makes a new one.
    private static bool Usable(Texture2D fog, int size)
    {
        if (ReferenceEquals(fog, checkedFog))
        {
            return fogUsable;
        }

        checkedFog = fog;
        fogUsable = false;
        string reason = null;
        if ((SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) == 0)
        {
            reason = "the graphics device cannot copy between textures";
        }
        else if (fog.mipmapCount != 1 || !fog.isReadable || fog.width != size || fog.height != size)
        {
            reason = "the fog texture is not the plain readable texture expected";
        }
        else
        {
            int bytes = fog.GetPixelData<byte>(0).Length;
            int pixels = size * size;
            if (pixels <= 0 || bytes <= 0 || bytes % pixels != 0)
            {
                reason = "the fog texture's format is not a whole number of bytes per pixel";
            }
            else
            {
                bytesPerPixel = bytes / pixels;
            }
        }

        if (reason != null)
        {
            Jotunn.Logger.LogInfo($"Map fog upload: {reason}, so the whole texture is sent as the game does.");
            return false;
        }

        fogUsable = true;
        return true;
    }

    // A square staging texture in the fog's format, grown to the next power of two that fits.
    private static bool EnsureStage(Texture2D fog, int needed)
    {
        if (stage != null && stage.graphicsFormat == fog.graphicsFormat && stage.width >= needed)
        {
            return true;
        }

        int side = 16;
        while (side < needed)
        {
            side *= 2;
        }

        if (stage != null)
        {
            UnityEngine.Object.Destroy(stage);
        }

        stage = new Texture2D(side, side, fog.graphicsFormat, TextureCreationFlags.None)
        {
            name = "WhiteHilt map fog stage",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        if (stage.graphicsFormat == fog.graphicsFormat && stage.mipmapCount == 1)
        {
            return true;
        }

        failed = true;
        Jotunn.Logger.LogInfo("Map fog upload: no staging texture in the fog's format, so the whole texture is sent.");
        return false;
    }
}
