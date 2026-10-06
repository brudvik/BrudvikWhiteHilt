#nullable enable annotations

using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Constants
{
    /// <summary>
    /// Shared defaults of the restocking chests.
    /// </summary>
    public static class DefaultValues
    {
        /// <summary>
        /// Scale of the category sign on the front of a chest, so the icon textures (whatever their pixel size) come
        /// out
        /// the same size on every chest.
        /// </summary>
        public static Vector3 ChestIconScale { get { return new Vector3(0.15f, 0.15f, 0.15f); } }
    }
}
