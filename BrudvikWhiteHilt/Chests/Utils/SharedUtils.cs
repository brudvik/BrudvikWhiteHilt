#nullable enable annotations

using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Utils
{
    /// <summary>
    /// Small helpers shared by the chest code.
    /// </summary>
    public static class SharedUtils
    {
        /// <summary>
        /// Makes a colour from 0-255 channel values, as colours are usually written in image editors, with an alpha of
        /// 0-1, as Unity's <see cref="Color"/> takes it.
        /// </summary>
        /// <param name="r">Red, 0-255.</param>
        /// <param name="g">Green, 0-255.</param>
        /// <param name="b">Blue, 0-255.</param>
        /// <param name="a">Alpha, 0-1.</param>
        /// <returns>The colour.</returns>
        public static Color ColorFromRGB(byte r, byte g, byte b, float a = 1.0f)
        {
            byte alpha = (byte)(a * 255);
            return new Color32(r, g, b, alpha);
        }
    }
}
