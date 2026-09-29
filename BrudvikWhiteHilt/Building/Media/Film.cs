using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Media;

/// <summary>
/// One camera point in a film: where the camera is, how it looks, and how long it takes to reach the next point.
/// </summary>
public class FilmPoint
{
    /// <summary>Width of the thumbnail in pixels.</summary>
    public const int ThumbWidth = 160;

    /// <summary>Height of the thumbnail in pixels.</summary>
    public const int ThumbHeight = 90;

    /// <summary>Camera position.</summary>
    public Vector3 Position;

    /// <summary>Camera heading in degrees.</summary>
    public float Yaw;

    /// <summary>Camera pitch in degrees.</summary>
    public float Pitch;

    /// <summary>Field of view in degrees.</summary>
    public float Fov;

    /// <summary>Seconds to fly to the next point.</summary>
    public float Seconds = 4f;

    /// <summary>Seconds to stand still at this point first.</summary>
    public float Hold;

    /// <summary>Eases in and out of this leg instead of flying at an even speed.</summary>
    public bool Smooth = true;

    /// <summary>Small picture of the view, or null.</summary>
    public Texture2D Thumb;

    /// <summary>The camera rotation.</summary>
    public Quaternion Rotation => Quaternion.Euler(Pitch, Yaw, 0f);
}

/// <summary>
/// A film: an ordered list of camera points plus title card and fade settings.
/// </summary>
public class Film
{
    /// <summary>File id.</summary>
    public string Id = Guid.NewGuid().ToString("N");

    /// <summary>Title shown on the title card.</summary>
    public string Title = string.Empty;

    /// <summary>Name shown under the title.</summary>
    public string Author = string.Empty;

    /// <summary>Date shown under the title.</summary>
    public string Date = string.Empty;

    /// <summary>Fades in from and out to black.</summary>
    public bool Fade = true;

    /// <summary>Shows the title card first.</summary>
    public bool TitleCard = true;

    /// <summary>The camera points in order.</summary>
    public List<FilmPoint> Points = new();
}

/// <summary>
/// Saves and loads films per world under BepInEx/config/BrudvikWhiteHilt/media/&lt;world&gt;/.
/// </summary>
public static class FilmStore
{
    private const int Version = 1;
    private const string Extension = ".whfilm";

    /// <summary>
    /// Loads every film saved for the current world, oldest first.
    /// </summary>
    /// <returns>The films.</returns>
    public static List<Film> LoadAll()
    {
        List<Film> films = new();
        string folder = Folder();
        if (folder == null || !Directory.Exists(folder))
        {
            return films;
        }

        foreach (string file in Directory.GetFiles(folder, "*" + Extension).OrderBy(File.GetCreationTimeUtc))
        {
            try
            {
                films.Add(Read(new ZPackage(File.ReadAllBytes(file)), Path.GetFileNameWithoutExtension(file)));
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Could not read film {file}: {ex.Message}");
            }
        }

        return films;
    }

    /// <summary>
    /// Saves a film.
    /// </summary>
    /// <param name="film">The film.</param>
    public static void Save(Film film)
    {
        string folder = Folder();
        if (folder == null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, film.Id + Extension), Write(film).GetArray());
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Could not save film {film.Title}: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a film's file.
    /// </summary>
    /// <param name="film">The film.</param>
    public static void Delete(Film film)
    {
        string folder = Folder();
        string file = folder == null ? null : Path.Combine(folder, film.Id + Extension);
        if (file != null && File.Exists(file))
        {
            File.Delete(file);
        }
    }

    private static string Folder()
    {
        string world = ZNet.instance != null ? ZNet.instance.GetWorldName() : null;
        if (string.IsNullOrEmpty(world))
        {
            return null;
        }

        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            world = world.Replace(invalid, '_');
        }

        return Path.Combine(Paths.ConfigPath, "BrudvikWhiteHilt", "media", world);
    }

    private static ZPackage Write(Film film)
    {
        ZPackage package = new();
        package.Write(Version);
        package.Write(film.Title);
        package.Write(film.Author);
        package.Write(film.Date);
        package.Write(film.Fade);
        package.Write(film.TitleCard);
        package.Write(film.Points.Count);
        foreach (FilmPoint point in film.Points)
        {
            package.Write(point.Position);
            package.Write(point.Yaw);
            package.Write(point.Pitch);
            package.Write(point.Fov);
            package.Write(point.Seconds);
            package.Write(point.Hold);
            package.Write(point.Smooth);
            package.Write(ThumbBytes(point.Thumb));
        }

        return package;
    }

    private static Film Read(ZPackage package, string id)
    {
        package.ReadInt();
        Film film = new()
        {
            Id = id,
            Title = package.ReadString(),
            Author = package.ReadString(),
            Date = package.ReadString(),
            Fade = package.ReadBool(),
            TitleCard = package.ReadBool()
        };

        int count = package.ReadInt();
        for (int i = 0; i < count; i++)
        {
            film.Points.Add(new FilmPoint
            {
                Position = package.ReadVector3(),
                Yaw = package.ReadSingle(),
                Pitch = package.ReadSingle(),
                Fov = package.ReadSingle(),
                Seconds = package.ReadSingle(),
                Hold = package.ReadSingle(),
                Smooth = package.ReadBool(),
                Thumb = ThumbFromBytes(package.ReadByteArray())
            });
        }

        return film;
    }

    private static byte[] ThumbBytes(Texture2D thumb)
    {
        if (thumb == null)
        {
            return Array.Empty<byte>();
        }

        Color32[] pixels = thumb.GetPixels32();
        byte[] bytes = new byte[pixels.Length * 3];
        for (int i = 0; i < pixels.Length; i++)
        {
            bytes[i * 3] = pixels[i].r;
            bytes[i * 3 + 1] = pixels[i].g;
            bytes[i * 3 + 2] = pixels[i].b;
        }

        return bytes;
    }

    private static Texture2D ThumbFromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length != FilmPoint.ThumbWidth * FilmPoint.ThumbHeight * 3)
        {
            return null;
        }

        Color32[] pixels = new Color32[FilmPoint.ThumbWidth * FilmPoint.ThumbHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(bytes[i * 3], bytes[i * 3 + 1], bytes[i * 3 + 2], 255);
        }

        Texture2D thumb = new(FilmPoint.ThumbWidth, FilmPoint.ThumbHeight, TextureFormat.RGB24, false);
        thumb.SetPixels32(pixels);
        thumb.Apply();
        return thumb;
    }
}
