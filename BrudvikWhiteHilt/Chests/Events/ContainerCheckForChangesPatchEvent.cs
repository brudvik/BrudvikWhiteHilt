#nullable enable annotations

using System;

namespace BrudvikWhiteHilt.Chests.Events
{
    public class ContainerCheckForChangesPatchEvent : EventArgs
    {
        public Container Container { get; set; }
    }
}
