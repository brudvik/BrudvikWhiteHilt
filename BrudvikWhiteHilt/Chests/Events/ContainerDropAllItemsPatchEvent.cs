#nullable enable annotations

using System;

namespace BrudvikWhiteHilt.Chests.Events
{
    /// <summary>
    /// Raised when a container drops all it holds, as when it is destroyed. The chest module uses it to drop only what
    /// players put in, not the unlimited stock it made itself.
    /// </summary>
    /// <remarks>
    /// The chest code does not patch the game itself: the Harmony patches in Patches/Chests only raise events like this
    /// one, and the chest module subscribes to them. That keeps the patches small and the logic testable apart from the
    /// game's classes.
    /// </remarks>
    public class ContainerDropAllItemsPatchEvent : EventArgs
    {
        /// <summary>
        /// The container dropping its items.
        /// </summary>
        public Container Container { get; set; }
    }
}
