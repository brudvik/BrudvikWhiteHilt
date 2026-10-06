#nullable enable annotations

using System;

namespace BrudvikWhiteHilt.Chests.Events
{
    /// <summary>
    /// Raised when a container checks whether its contents changed, which the game does now and then for every loaded
    /// container. The chest module uses it to restock unlimited chests and to learn what lies in them.
    /// </summary>
    /// <remarks>
    /// The chest code does not patch the game itself: the Harmony patches in Patches/Chests only raise events like this
    /// one, and the chest module subscribes to them. That keeps the patches small and the logic testable apart from the
    /// game's classes.
    /// </remarks>
    public class ContainerCheckForChangesPatchEvent : EventArgs
    {
        /// <summary>
        /// The container being checked.
        /// </summary>
        public Container Container { get; set; }
    }
}
