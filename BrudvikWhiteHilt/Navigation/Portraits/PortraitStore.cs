using BepInEx;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Portraits;

/// <summary>
/// Portrait pixels on disk and on the wire: 96 x 96 RGBA, deflated, named by the SHA-1 of the raw pixels. Your own
/// portrait is kept per character with the look it was taken of; portraits from others are cached by hash.
/// </summary>
public static class PortraitStore
{
    /// <summary>Width and height of a portrait in pixels.</summary>
    public const int Size = 96;

    /// <summary>Length of the raw RGBA pixels.</summary>
    public const int RawLength = Size * Size * 4;

    /// <summary>Largest compressed portrait accepted.</summary>
    public const int MaxCompressed = 24 * 1024;

    private const int OwnMagic = 0x31504857;
    private const int MaxCacheFiles = 200;

    // The portrait is clipped to this share of the disc, so a thin dark rim shows around it.
    private const float ClipRadius = 0.92f;

    private static string Folder => Path.Combine(Path.Combine(Paths.ConfigPath, "WhiteHilt"), "portraits");

    private static string CacheFolder => Path.Combine(Folder, "cache");

    /// <summary>
    /// Whether a string is a SHA-1 in lower-case hex, the only form used in file names.
    /// </summary>
    /// <param name="hash">The text to check.</param>
    /// <returns>True if it is a valid hash.</returns>
    public static bool IsHash(string hash)
    {
        return hash != null && hash.Length == 40 && hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
    }

    /// <summary>
    /// The SHA-1 of raw pixels in lower-case hex.
    /// </summary>
    /// <param name="raw">The raw pixels.</param>
    /// <returns>The hash.</returns>
    public static string Hash(byte[] raw)
    {
        using SHA1 sha = SHA1.Create();
        byte[] digest = sha.ComputeHash(raw);
        StringBuilder text = new(40);
        foreach (byte b in digest)
        {
            text.Append(b.ToString("x2"));
        }

        return text.ToString();
    }

    /// <summary>
    /// Deflates raw pixels.
    /// </summary>
    /// <param name="raw">The raw pixels.</param>
    /// <returns>The compressed bytes.</returns>
    public static byte[] Compress(byte[] raw)
    {
        using MemoryStream output = new();
        using (DeflateStream deflate = new(output, CompressionMode.Compress))
        {
            deflate.Write(raw, 0, raw.Length);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Inflates a portrait, refusing anything that is not exactly <see cref="RawLength"/> bytes.
    /// </summary>
    /// <param name="data">The compressed bytes.</param>
    /// <returns>The raw pixels, or null if the data is invalid.</returns>
    public static byte[] Decompress(byte[] data)
    {
        if (data == null || data.Length == 0 || data.Length > MaxCompressed)
        {
            return null;
        }

        try
        {
            using MemoryStream input = new(data);
            using DeflateStream deflate = new(input, CompressionMode.Decompress);
            byte[] raw = new byte[RawLength];
            int read = 0;
            while (read < RawLength)
            {
                int count = deflate.Read(raw, read, RawLength - read);
                if (count <= 0)
                {
                    break;
                }

                read += count;
            }

            return read == RawLength && deflate.ReadByte() == -1 ? raw : null;
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Makes a UI texture from raw pixels, clipped to a soft-edged circle.
    /// </summary>
    /// <param name="raw">The raw pixels.</param>
    /// <returns>The texture.</returns>
    public static Texture2D ToTexture(byte[] raw)
    {
        byte[] pixels = (byte[])raw.Clone();
        float centre = (Size - 1) / 2f;
        float radius = Size / 2f * ClipRadius;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                float edge = Mathf.Clamp01(radius - distance + 0.5f);
                int alpha = (y * Size + x) * 4 + 3;
                pixels[alpha] = (byte)(pixels[alpha] * edge);
            }
        }

        Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "WhiteHiltPortrait",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        texture.LoadRawTextureData(pixels);
        texture.Apply(false, true);
        return texture;
    }

    /// <summary>
    /// Reads your own portrait for a character.
    /// </summary>
    /// <param name="playerId">The character's player ID.</param>
    /// <param name="look">The look the portrait was taken of.</param>
    /// <param name="hash">The hash of the raw pixels.</param>
    /// <param name="data">The compressed pixels.</param>
    /// <returns>True if a valid portrait was found.</returns>
    public static bool TryLoadOwn(long playerId, out string look, out string hash, out byte[] data)
    {
        look = null;
        hash = null;
        data = null;
        string path = OwnPath(playerId);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using BinaryReader reader = new(File.OpenRead(path));
            if (reader.ReadInt32() != OwnMagic)
            {
                return false;
            }

            look = reader.ReadString();
            hash = reader.ReadString();
            int length = reader.ReadInt32();
            if (!IsHash(hash) || length <= 0 || length > MaxCompressed)
            {
                return false;
            }

            data = reader.ReadBytes(length);
            byte[] raw = Decompress(data);
            return raw != null && Hash(raw) == hash;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Jotunn.Logger.LogWarning($"Portrait: could not read {path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Writes your own portrait for a character.
    /// </summary>
    /// <param name="playerId">The character's player ID.</param>
    /// <param name="look">The look the portrait was taken of.</param>
    /// <param name="hash">The hash of the raw pixels.</param>
    /// <param name="data">The compressed pixels.</param>
    public static void SaveOwn(long playerId, string look, string hash, byte[] data)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            using BinaryWriter writer = new(File.Create(OwnPath(playerId)));
            writer.Write(OwnMagic);
            writer.Write(look);
            writer.Write(hash);
            writer.Write(data.Length);
            writer.Write(data);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Jotunn.Logger.LogWarning($"Portrait: could not save your portrait: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads another player's portrait from the cache.
    /// </summary>
    /// <param name="hash">The hash of the raw pixels.</param>
    /// <returns>The raw pixels, or null if they are missing or do not match the hash.</returns>
    public static byte[] LoadCached(string hash)
    {
        if (!IsHash(hash))
        {
            return null;
        }

        string path = Path.Combine(CacheFolder, hash + ".bin");
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] raw = Decompress(File.ReadAllBytes(path));
            return raw != null && Hash(raw) == hash ? raw : null;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Caches another player's portrait, keeping the newest <see cref="MaxCacheFiles"/> files.
    /// </summary>
    /// <param name="hash">The hash of the raw pixels.</param>
    /// <param name="data">The compressed pixels.</param>
    public static void SaveCached(string hash, byte[] data)
    {
        if (!IsHash(hash))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(CacheFolder);
            File.WriteAllBytes(Path.Combine(CacheFolder, hash + ".bin"), data);
            FileInfo[] files = new DirectoryInfo(CacheFolder).GetFiles("*.bin");
            foreach (FileInfo old in files.OrderByDescending(file => file.LastWriteTimeUtc).Skip(MaxCacheFiles))
            {
                old.Delete();
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Jotunn.Logger.LogWarning($"Portrait: could not cache a portrait: {ex.Message}");
        }
    }

    private static string OwnPath(long playerId)
    {
        return Path.Combine(Folder, $"own_{playerId}.bin");
    }
}
