using System.Reflection;
using HarmonyLib;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Stand-ins for three small vanilla methods that make garbage or use reflection every frame on every client: the
/// hotbar's key names, the worn gear's modifiers and the distance test of the active area. Each gives exactly the
/// answer vanilla gives.
/// </summary>
public static class FrameGarbage
{
    private const string HotbarFormat = "Hotbar{0}";
    private const string HotbarAltFormat = "Hotbar{0}Alt";
    private const int HotbarCacheSize = 10;

    private static readonly string[] hotbarNames = new string[HotbarCacheSize];
    private static readonly string[] hotbarAltNames = new string[HotbarCacheSize];

    private static FieldInfo[] modifierFields;
    private static AccessTools.FieldRef<ItemDrop.ItemData.SharedData, float>[] modifierReaders;

    /// <summary>
    /// The name of a hotbar key, as <c>string.Format(format, index)</c> would make it. <c>Player.Update</c> formats
    /// "Hotbar{0}" and "Hotbar{0}Alt" for keys 1 to 8 every frame, 16 new strings and 16 boxed ints; these are made once.
    /// </summary>
    /// <param name="format">The format vanilla passes.</param>
    /// <param name="index">The key's number.</param>
    /// <returns>The key's name.</returns>
    public static string HotbarKeyName(string format, int index)
    {
        string[] cache = format == HotbarFormat ? hotbarNames : format == HotbarAltFormat ? hotbarAltNames : null;
        if (cache == null || index < 0 || index >= HotbarCacheSize || !VanillaFixSettings.HotbarKeys.Active)
        {
            return string.Format(format, index);
        }

        return cache[index] ??= string.Format(format, index);
    }

    /// <summary>
    /// Sums the worn gear's modifiers (movement, stamina use, heat resistance...) into the player's table, as
    /// <c>Player.UpdateModifiers</c> does. Vanilla reads each of 11 fields of up to 8 worn items through
    /// <c>FieldInfo.GetValue</c>, a slow reflection call that boxes the float, every physics step. Here each field is read
    /// through a typed accessor made once. The items are added in vanilla's order, so the sums are the same to the bit.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>False when the table could not be filled this way and vanilla should do it.</returns>
    public static bool UpdateModifiers(Player player)
    {
        FieldInfo[] fields = Player.s_equipmentModifierSourceFields;
        float[] values = player.m_equipmentModifierValues;
        if (fields == null || values == null || values.Length > fields.Length || !Readers(fields))
        {
            return false;
        }

        for (int i = 0; i < values.Length; i++)
        {
            AccessTools.FieldRef<ItemDrop.ItemData.SharedData, float> read = modifierReaders[i];
            float sum = 0f;
            if (player.m_rightItem != null)
            {
                sum += read(player.m_rightItem.m_shared);
            }

            if (player.m_leftItem != null)
            {
                sum += read(player.m_leftItem.m_shared);
            }

            if (player.m_chestItem != null)
            {
                sum += read(player.m_chestItem.m_shared);
            }

            if (player.m_legItem != null)
            {
                sum += read(player.m_legItem.m_shared);
            }

            if (player.m_helmetItem != null)
            {
                sum += read(player.m_helmetItem.m_shared);
            }

            if (player.m_shoulderItem != null)
            {
                sum += read(player.m_shoulderItem.m_shared);
            }

            if (player.m_utilityItem != null)
            {
                sum += read(player.m_utilityItem.m_shared);
            }

            if (player.m_trinketItem != null)
            {
                sum += read(player.m_trinketItem.m_shared);
            }

            values[i] = sum;
        }

        return true;
    }

    /// <summary>
    /// The largest of the three axis distances between two points, as <c>Utils.ChebyshevDistance</c> gives it. Vanilla
    /// passes the three to <c>Mathf.Max(params float[])</c>, which makes a new array for every call, and the game calls
    /// it for many objects every frame. This compares them in the same order as <c>Mathf.Max</c>, so a NaN gives the
    /// same answer too.
    /// </summary>
    /// <param name="a">One point.</param>
    /// <param name="b">The other point.</param>
    /// <returns>The distance.</returns>
    public static float ChebyshevDistance(UnityEngine.Vector3 a, UnityEngine.Vector3 b)
    {
        float max = System.Math.Abs(a.x - b.x);
        float y = System.Math.Abs(a.y - b.y);
        if (y > max)
        {
            max = y;
        }

        float z = System.Math.Abs(a.z - b.z);
        if (z > max)
        {
            max = z;
        }

        return max;
    }

    // Makes a typed reader for each modifier field, again only if the game's field list was replaced. False, and
    // logged once, when a field is missing or not a float, so vanilla keeps doing it.
    private static bool Readers(FieldInfo[] fields)
    {
        if (ReferenceEquals(fields, modifierFields))
        {
            return modifierReaders != null;
        }

        modifierFields = fields;
        modifierReaders = null;
        var readers = new AccessTools.FieldRef<ItemDrop.ItemData.SharedData, float>[fields.Length];
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] == null || fields[i].FieldType != typeof(float) || fields[i].IsStatic)
            {
                Jotunn.Logger.LogWarning($"Performance: equipment modifier {i} is not a float field; the game's own way is used.");
                return false;
            }

            readers[i] = AccessTools.FieldRefAccess<ItemDrop.ItemData.SharedData, float>(fields[i]);
        }

        modifierReaders = readers;
        return true;
    }
}
