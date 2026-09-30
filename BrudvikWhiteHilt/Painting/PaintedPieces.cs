using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Painting;

/// <summary>
/// The colour of painted building pieces. It is kept in the piece's ZDO, so everyone sees it and it stays after a
/// restart; a change is sent to everyone by RPC. Stain tints the vanilla material; paint swaps in a copy with a
/// bleached texture and tints that. Without the mod, pieces simply look vanilla again.
/// </summary>
public static class PaintedPieces
{
    private const string Rpc = "WhiteHiltPaint";

    private static readonly int paintKey = "whitehilt_paint".GetStableHashCode();
    private static readonly Dictionary<Material, Material> bleached = new();
    private static readonly Dictionary<Texture, Texture2D> bleachedTextures = new();

    /// <summary>
    /// The paint of a piece.
    /// </summary>
    /// <param name="piece">The piece.</param>
    /// <returns>The packed value, 0 when unpainted.</returns>
    public static int Get(WearNTear piece)
    {
        ZDO zdo = piece != null && piece.m_nview != null ? piece.m_nview.GetZDO() : null;
        return zdo != null ? zdo.GetInt(paintKey) : 0;
    }

    /// <summary>
    /// Paints a piece, or clears it with 0, for everyone.
    /// </summary>
    /// <param name="piece">The piece.</param>
    /// <param name="value">The packed value.</param>
    public static void Set(WearNTear piece, int value)
    {
        ZNetView nview = piece.m_nview;
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        nview.ClaimOwnership();
        nview.GetZDO().Set(paintKey, value);
        nview.InvokeRPC(ZNetView.Everybody, Rpc, value);
    }

    /// <summary>
    /// Listens for paint and shows the saved paint. Called when a piece wakes up.
    /// </summary>
    /// <param name="piece">The piece.</param>
    public static void OnAwake(WearNTear piece)
    {
        ZNetView nview = piece.m_nview;
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<int>(Rpc, (_, value) => Show(piece, value));
        int saved = nview.GetZDO().GetInt(paintKey);
        if (saved != 0)
        {
            Show(piece, saved);
        }
    }

    /// <summary>
    /// Puts the tint back after something else cleared it, e.g. the build highlight.
    /// </summary>
    /// <param name="go">The piece's game object.</param>
    public static void Reapply(GameObject go)
    {
        if (go != null && go.TryGetComponent(out PaintedPiece painted))
        {
            painted.ApplyTint();
        }
    }

    /// <summary>
    /// Shows a paint on a piece locally, without saving it; 0 shows the piece as it is.
    /// </summary>
    /// <param name="piece">The piece.</param>
    /// <param name="value">The packed value.</param>
    public static void Show(WearNTear piece, int value)
    {
        if (piece == null || VisualHelper.IsHeadless)
        {
            return;
        }

        PaintedPiece painted = piece.GetComponent<PaintedPiece>();
        if (painted == null)
        {
            if (value == 0)
            {
                return;
            }

            painted = piece.gameObject.AddComponent<PaintedPiece>();
        }

        painted.Apply(value);
    }

    /// <summary>
    /// A copy of a material with its texture bleached to light grey, so a tint gives the true colour.
    /// </summary>
    /// <param name="source">The vanilla material.</param>
    /// <returns>The copy, or the material itself if it has no texture.</returns>
    internal static Material Bleached(Material source)
    {
        if (source == null || !source.HasProperty("_MainTex") || source.mainTexture == null)
        {
            return source;
        }

        if (bleached.TryGetValue(source, out Material cached) && cached != null)
        {
            return cached;
        }

        Texture texture = source.mainTexture;
        if (!bleachedTextures.TryGetValue(texture, out Texture2D grey) || grey == null)
        {
            // Light grey keeps the grain and shading faint, so white paint stays white.
            grey = VisualHelper.RecolorTexture(texture, pixel =>
            {
                float luminance = (0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b) / 255f;
                byte value = (byte)(Mathf.Lerp(0.72f, 1f, luminance) * 255f);
                return new Color32(value, value, value, pixel.a);
            });
            bleachedTextures[texture] = grey;
        }

        Material copy = new(source) { name = source.name + "_whitehilt_paint", mainTexture = grey };
        bleached[source] = copy;
        return copy;
    }
}

/// <summary>
/// Remembers a painted piece's vanilla materials and keeps its tint.
/// </summary>
public class PaintedPiece : MonoBehaviour
{
    private readonly Dictionary<Renderer, Material[]> originals = new();
    private int value;

    /// <summary>
    /// Shows a paint, or the vanilla look for 0.
    /// </summary>
    /// <param name="packed">The packed value.</param>
    public void Apply(int packed)
    {
        value = packed;
        bool painted = PaintColor.Unpack(packed, out _, out PaintMode mode);
        CaptureOriginals();
        foreach (KeyValuePair<Renderer, Material[]> entry in originals)
        {
            if (entry.Key == null)
            {
                continue;
            }

            if (painted && mode == PaintMode.Paint)
            {
                Material[] swapped = new Material[entry.Value.Length];
                for (int i = 0; i < swapped.Length; i++)
                {
                    swapped[i] = PaintedPieces.Bleached(entry.Value[i]);
                }

                entry.Key.sharedMaterials = swapped;
            }
            else
            {
                entry.Key.sharedMaterials = entry.Value;
            }
        }

        if (painted)
        {
            ApplyTint();
        }
        else if (MaterialMan.instance != null)
        {
            MaterialMan.instance.ResetValue(gameObject, ShaderProps._Color);
        }
    }

    /// <summary>
    /// Sets the tint again, e.g. after the build highlight reset it.
    /// </summary>
    public void ApplyTint()
    {
        if (MaterialMan.instance != null && PaintColor.Unpack(value, out Color32 color, out _))
        {
            MaterialMan.instance.SetValue(gameObject, ShaderProps._Color, (Color)color);
        }
    }

    // Once, before the first swap; every look (new, worn, broken) is included.
    private void CaptureOriginals()
    {
        if (originals.Count > 0)
        {
            return;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            {
                originals[renderer] = renderer.sharedMaterials;
            }
        }
    }
}
