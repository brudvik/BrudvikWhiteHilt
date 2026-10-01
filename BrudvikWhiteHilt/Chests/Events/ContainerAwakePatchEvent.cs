#nullable enable annotations

using System;

namespace BrudvikWhiteHilt.Chests.Events
{
    public class ContainerAwakePatchEvent : EventArgs
    {
        public Container Container { get; set; }
    }
}
