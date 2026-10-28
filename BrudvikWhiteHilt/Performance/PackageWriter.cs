namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Writes one network package into another without the copy vanilla makes. <c>ZPackage.Write(ZPackage)</c> asks the
/// inner package for <c>GetArray()</c>, which is <c>MemoryStream.ToArray()</c>: a new array as large as the whole inner
/// package, written out and dropped at once. The game nests packages for every batch of world data it sends a player and
/// for every routed call, so a server makes that garbage once per player per send. Writing the length and then the
/// bytes straight from the inner stream's own buffer gives the same bytes.
/// </summary>
public static class PackageWriter
{
    /// <summary>
    /// Writes <paramref name="inner"/> into <paramref name="outer"/> exactly as vanilla does: its length as an int, then
    /// its bytes.
    /// </summary>
    /// <param name="outer">The package written to.</param>
    /// <param name="inner">The package written.</param>
    /// <returns>False when this cannot be done without the copy, and vanilla should write it.</returns>
    public static bool WriteNested(ZPackage outer, ZPackage inner)
    {
        // A package written into itself would read from the buffer it is growing; leave that odd case to vanilla.
        if (outer == null || inner == null || ReferenceEquals(outer, inner))
        {
            return false;
        }

        // GetArray flushes the writer before it copies; the length is only right after the flush. A package whose writer
        // was swapped (SaveFileHelper) is flushed the same way, and like GetArray only the stream's own bytes are written.
        inner.m_writer.Flush();
        inner.m_stream.Flush();
        if (!inner.m_stream.TryGetBuffer(out System.ArraySegment<byte> bytes))
        {
            return false;
        }

        outer.m_writer.Write(bytes.Count);
        outer.m_writer.Write(bytes.Array, bytes.Offset, bytes.Count);
        return true;
    }
}
