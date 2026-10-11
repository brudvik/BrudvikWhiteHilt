using System.IO;
using System.IO.Compression;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Unpacks data packed with the game's <c>Utils.Compress</c> (gzip), but stops at a size limit. Data sent by another
/// player is unpacked this way, so a small package that would unpack into gigabytes cannot stall the game.
/// </summary>
public static class BoundedGzip
{
    /// <summary>
    /// Unpacks gzip data.
    /// </summary>
    /// <param name="data">The packed data.</param>
    /// <param name="maxBytes">Most bytes it may unpack to.</param>
    /// <returns>The unpacked data, or null if it is broken or larger than <paramref name="maxBytes"/>.</returns>
    public static byte[] Decompress(byte[] data, int maxBytes)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        try
        {
            using MemoryStream input = new(data);
            using GZipStream gzip = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();
            byte[] buffer = new byte[16384];
            int read;
            while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + read > maxBytes)
                {
                    return null;
                }

                output.Write(buffer, 0, read);
            }

            return output.ToArray();
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
