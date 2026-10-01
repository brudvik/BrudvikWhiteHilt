#nullable enable annotations

using System;

namespace BrudvikWhiteHilt.Chests.Events
{
    public class ContainerDropAllItemsPatchEvent : EventArgs
    {
        public Container Container { get; set; }
    }
}
