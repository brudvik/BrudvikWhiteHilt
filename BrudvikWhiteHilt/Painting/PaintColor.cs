using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Painting;

/// <summary>
/// How a painted piece looks.
/// </summary>
public enum PaintMode
{
    /// <summary>The colour over the texture: the grain shows, and it can only darken.</summary>
    Stain = 0,

    /// <summary>The texture bleached first, then coloured: true colours, also white.</summary>
    Paint = 1
}

/// <summary>
/// Paint colours as hex text and as the int kept in a piece's ZDO: bit 25 set when painted, bit 24 the mode, then RGB.
/// </summary>
public static class PaintColor
{
    private const int PaintedBit = 1 << 25;
    private const int ModeBit = 1 << 24;

    /// <summary>
    /// The ZDO value for a colour and mode.
    /// </summary>
    /// <param name="color">The colour.</param>
    /// <param name="mode">Stain or paint.</param>
    /// <returns>The packed value, never 0.</returns>
    public static int Pack(Color32 color, PaintMode mode)
    {
        return PaintedBit | (mode == PaintMode.Paint ? ModeBit : 0) | (color.r << 16) | (color.g << 8) | color.b;
    }

    /// <summary>
    /// Reads a ZDO value.
    /// </summary>
    /// <param name="value">The packed value.</param>
    /// <param name="color">The colour.</param>
    /// <param name="mode">Stain or paint.</param>
    /// <returns>False when the piece is not painted.</returns>
    public static bool Unpack(int value, out Color32 color, out PaintMode mode)
    {
        color = new Color32((byte)(value >> 16), (byte)(value >> 8), (byte)value, 255);
        mode = (value & ModeBit) != 0 ? PaintMode.Paint : PaintMode.Stain;
        return (value & PaintedBit) != 0;
    }

    /// <summary>
    /// The colour as six hex digits, e.g. "8E2B22".
    /// </summary>
    /// <param name="color">The colour.</param>
    /// <returns>The hex text.</returns>
    public static string ToHex(Color32 color)
    {
        return $"{color.r:X2}{color.g:X2}{color.b:X2}";
    }

    /// <summary>
    /// Reads six hex digits, with or without a leading #.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="color">The colour.</param>
    /// <returns>True if it was a colour.</returns>
    public static bool TryParseHex(string text, out Color32 color)
    {
        color = new Color32(0, 0, 0, 255);
        string hex = (text ?? string.Empty).Trim().TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            return false;
        }

        color = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        return true;
    }

    /// <summary>
    /// A coloured block for rich text, e.g. in tooltips and messages.
    /// </summary>
    /// <param name="color">The colour.</param>
    /// <returns>Rich text showing the colour and its hex.</returns>
    public static string Swatch(Color32 color)
    {
        return $"<color=#{ToHex(color)}>■■■</color> #{ToHex(color)}";
    }
}
