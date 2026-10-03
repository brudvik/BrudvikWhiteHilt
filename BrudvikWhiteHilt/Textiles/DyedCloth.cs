using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Painting;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Textiles;

/// <summary>
/// Dyed cloth: the cloth parts of banners and the sails of ships, and whole capes, shown in a dye colour. A banner's or
/// ship's dye lives in its ZDO and is sent to everyone by RPC; a cape's is on the item and its wearer's ZDO.
/// The vanilla texture is bleached and the colour baked into a material copy, so only the cloth changes.
/// </summary>
public static class DyedCloth
{
    private const string Rpc = "WhiteHiltDye";

    private static readonly int dyeKey = "whitehilt_dye".GetStableHashCode();
    private static readonly Dictionary<(Material, int), Material> tinted = new();

    /// <summary>
    /// The dye of a piece or ship.
    /// </summary>
    /// <param name="nview">Its network view.</param>
    /// <returns>The packed colour, 0 when undyed.</returns>
    public static int Get(ZNetView nview)
    {
        return nview != null && nview.IsValid() ? nview.GetZDO().GetInt(dyeKey) : 0;
    }

    /// <summary>
    /// Dyes a piece or ship for everyone.
    /// </summary>
    /// <param name="nview">Its network view.</param>
    /// <param name="value">The packed colour.</param>
    public static void Set(ZNetView nview, int value)
    {
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        nview.ClaimOwnership();
        nview.GetZDO().Set(dyeKey, value);
        nview.InvokeRPC(ZNetView.Everybody, Rpc, value);
    }

    /// <summary>
    /// Listens for dye and shows the saved dye. Called when a piece or ship wakes up.
    /// </summary>
    /// <param name="piece">The piece.</param>
    public static void OnAwake(WearNTear piece)
    {
        ZNetView nview = piece.m_nview;
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<int>(Rpc, (_, value) => Apply(piece.gameObject, value, false));
        int saved = nview.GetZDO().GetInt(dyeKey);
        if (saved != 0)
        {
            Apply(piece.gameObject, saved, false);
        }
    }

    /// <summary>
    /// True if an object has cloth to dye.
    /// </summary>
    /// <param name="root">The object.</param>
    /// <returns>True if so.</returns>
    public static bool HasCloth(GameObject root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && TextileSettings.IsCloth(material.name))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Shows a dye locally; 0 shows the cloth as it was.
    /// </summary>
    /// <param name="root">The object.</param>
    /// <param name="value">The packed colour.</param>
    /// <param name="everyRenderer">Dye every renderer, as for a cape, instead of the cloth materials only.</param>
    public static void Apply(GameObject root, int value, bool everyRenderer)
    {
        if (root == null || VisualHelper.IsHeadless)
        {
            return;
        }

        ClothDye dye = root.GetComponent<ClothDye>();
        if (dye == null)
        {
            if (value == 0)
            {
                return;
            }

            dye = root.AddComponent<ClothDye>();
        }

        dye.Apply(value, everyRenderer);
    }

    /// <summary>
    /// A bleached copy of a material in a colour, shared by everything dyed that colour.
    /// </summary>
    /// <param name="source">The vanilla material.</param>
    /// <param name="color">The colour.</param>
    /// <returns>The copy.</returns>
    internal static Material Tinted(Material source, Color32 color)
    {
        int rgb = (color.r << 16) | (color.g << 8) | color.b;
        if (tinted.TryGetValue((source, rgb), out Material cached) && cached != null)
        {
            return cached;
        }

        Material copy = new(PaintedPieces.Bleached(source)) { name = source.name + "_whitehilt_dye" };
        if (copy.HasProperty("_Color"))
        {
            copy.color = color;
        }

        tinted[(source, rgb)] = copy;
        return copy;
    }
}

/// <summary>
/// Remembers an object's vanilla cloth materials and shows its dye.
/// </summary>
public class ClothDye : MonoBehaviour
{
    private readonly Dictionary<Renderer, Material[]> originals = new();

    /// <summary>
    /// Shows a dye, or the vanilla look for 0.
    /// </summary>
    /// <param name="value">The packed colour.</param>
    /// <param name="everyRenderer">Dye every renderer instead of the cloth materials only.</param>
    public void Apply(int value, bool everyRenderer)
    {
        if (originals.Count == 0)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    originals[renderer] = renderer.sharedMaterials;
                }
            }
        }

        bool dyed = PaintColor.Unpack(value, out Color32 color, out _);
        foreach (KeyValuePair<Renderer, Material[]> entry in originals)
        {
            if (entry.Key == null)
            {
                continue;
            }

            Material[] materials = new Material[entry.Value.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = entry.Value[i];
                materials[i] = dyed && source != null && (everyRenderer || TextileSettings.IsCloth(source.name)) ? DyedCloth.Tinted(source, color) : source;
            }

            entry.Key.sharedMaterials = materials;
        }
    }
}
