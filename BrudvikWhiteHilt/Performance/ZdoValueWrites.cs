using System;
using System.Collections.Generic;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Sets a ZDO value without boxing it. Every <c>ZDO.Set</c> ends in <c>BinarySearchDictionary.SetValue</c>, which asks
/// <c>m_values[i].Equals((object)value)</c> to skip a value that did not change; for a float, int, long, Vector3 or
/// Quaternion that cast makes a heap object, thrown away on the same line. Animations, velocities and timers set
/// values every frame for every object a machine owns, so this is a steady trickle of garbage. The methods here do the
/// same test with the value type's own <c>Equals</c>, which gives the same answer (each type's <c>Equals(object)</c>
/// checks the type and then calls it), and leave new keys to vanilla.
/// </summary>
public static class ZdoValueWrites
{
    /// <summary>
    /// Stands in for <c>ZDOHelper.InitAndSet</c>: sets a value and says whether it changed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="container">One of ZDOExtraData's value tables.</param>
    /// <param name="zid">The object.</param>
    /// <param name="hash">The value's key.</param>
    /// <param name="value">The new value.</param>
    /// <returns>True when the value was new or changed, as vanilla.</returns>
    public static bool InitAndSet<T>(Dictionary<ZDOID, BinarySearchDictionary<int, T>> container, ZDOID zid, int hash, T value)
        where T : struct, IEquatable<T>
    {
        if (VanillaFixSettings.ZdoWrite.Active && TrySetExisting(container, zid, hash, value, out bool changed))
        {
            return changed;
        }

        return container.InitAndSet(zid, hash, value);
    }

    /// <summary>
    /// Stands in for <c>ZDOHelper.Update</c>: sets a value of an object that already has values of this type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="container">One of ZDOExtraData's value tables.</param>
    /// <param name="zid">The object.</param>
    /// <param name="hash">The value's key.</param>
    /// <param name="value">The new value.</param>
    /// <returns>True when the value was new or changed, as vanilla.</returns>
    public static bool Update<T>(Dictionary<ZDOID, BinarySearchDictionary<int, T>> container, ZDOID zid, int hash, T value)
        where T : struct, IEquatable<T>
    {
        if (VanillaFixSettings.ZdoWrite.Active && TrySetExisting(container, zid, hash, value, out bool changed))
        {
            return changed;
        }

        return container.Update(zid, hash, value);
    }

    /// <summary>
    /// Sets a value whose key the object already has, with one lookup per table and no box.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="container">One of ZDOExtraData's value tables.</param>
    /// <param name="zid">The object.</param>
    /// <param name="hash">The value's key.</param>
    /// <param name="value">The new value.</param>
    /// <param name="changed">Whether the value differed from the old one.</param>
    /// <returns>False when the object has no such key yet; vanilla then inserts it in order.</returns>
    public static bool TrySetExisting<T>(Dictionary<ZDOID, BinarySearchDictionary<int, T>> container, ZDOID zid, int hash, T value, out bool changed)
        where T : struct, IEquatable<T>
    {
        changed = false;
        if (!container.TryGetValue(zid, out BinarySearchDictionary<int, T> values) || values == null
            || !values.TryGetValue(hash, out T old))
        {
            return false;
        }

        if (old.Equals(value))
        {
            return true;
        }

        values[hash] = value;
        changed = true;
        return true;
    }
}
