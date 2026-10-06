using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Stonework;

/// <summary>
/// A raised memorial stone, a bauta, with an inscription of your own on its face, like the runestones raised for the
/// dead
/// and for great deeds. It works as the vanilla sign: use it to carve the text, and the hover text shows it.
/// </summary>
public class MemorialStone : StoneworkPieceBase
{
    // The part of the stone's flatter face the inscription fills, in the piece's space (build_defenses.py memorial_stone):
    // the face is about 0.9 m wide there and bulges forward along a ridge near x 0.1.
    private static readonly Rect TextArea = Rect.MinMaxRect(-0.3f, 1.0f, 0.46f, 1.95f);

    // Cap height of the letters in metres, at most and at least: smaller when the inscription is long.
    private const float LargestLetter = 0.07f;
    private const float SmallestLetter = 0.03f;

    // Letters in the shade of a cut in the stone, with a pale lower edge where the light falls into the cut.
    private static readonly Color CutColor = new(0.16f, 0.14f, 0.13f, 0.92f);
    private static readonly Color CutEdgeColor = new(0.78f, 0.76f, 0.72f, 0.55f);

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public MemorialStone(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "bautastein";

    /// <inheritdoc/>
    protected override string FullName => "Memorial Stone";

    /// <inheritdoc/>
    protected override string Description => "A tall raised stone, a bauta, to remember the dead or a great deed. Use it to carve an inscription of your own on its face.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 20, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 2000f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;

    /// <summary>
    /// Lays the sign's text into the stone's face: sized to the face, following its curve, and cut into it.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    protected override void CustomizeStone(GameObject prefab)
    {
        Sign sign = prefab.GetComponent<Sign>();
        sign.m_name = Translations.Token(PrefabName);
        sign.m_defaultText = string.Empty;
        sign.m_characterLimit = 100;

        Transform canvas = prefab.transform.Find("Canvas");
        TextMeshProUGUI text = canvas != null ? canvas.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (text == null)
        {
            return;
        }

        StoneFace face = StoneFace.Measure(prefab.transform, canvas, TextArea);
        canvas.localPosition = new Vector3(TextArea.center.x, TextArea.center.y, face.Front);

        // The text's rectangle and letter sizes in its own units, from its size in the world.
        float unit = Mathf.Abs(text.transform.lossyScale.x / prefab.transform.lossyScale.x);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = TextArea.size / unit;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        // A capital stands about 0.7 of the font size.
        text.fontSizeMax = LargestLetter / 0.7f / unit;
        text.fontSizeMin = SmallestLetter / 0.7f / unit;
        text.color = CutColor;
        text.fontSharedMaterial = CutMaterial(text.fontSharedMaterial);

        text.gameObject.AddComponent<CarvedText>().Face = face;
    }

    // The font's material with an underlay: a pale copy of each letter just below it, the lit lower side of the cut.
    private static Material CutMaterial(Material font)
    {
        Material cut = new(font) { name = $"{font.name}_carved" };
        cut.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        cut.SetColor(ShaderUtilities.ID_UnderlayColor, CutEdgeColor);
        cut.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        cut.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
        cut.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
        cut.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
        return cut;
    }
}

/// <summary>
/// The front of a stone over an area, as a grid of how far forward (+z) its surface stands, in the piece's space.
/// </summary>
[Serializable]
public class StoneFace
{
    private const float Step = 0.04f;
    private const float Margin = 0.12f;

    /// <summary>Lower left corner of the grid.</summary>
    public Vector2 Origin;

    /// <summary>Grid points across.</summary>
    public int Columns;

    /// <summary>Grid points up.</summary>
    public int Rows;

    /// <summary>Front of the surface at each grid point, row by row.</summary>
    public float[] Depth = Array.Empty<float>();

    /// <summary>Foremost point of the surface over the area.</summary>
    public float Front;

    /// <summary>
    /// Measures the front of the meshes under a piece over an area, a little beyond it on every side.
    /// </summary>
    /// <param name="root">The piece.</param>
    /// <param name="skip">A part whose meshes are not the stone, e.g. the text.</param>
    /// <param name="area">The area, in the piece's space.</param>
    /// <returns>The measured face.</returns>
    public static StoneFace Measure(Transform root, Transform skip, Rect area)
    {
        List<Vector3> triangles = new();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || (skip != null && filter.transform.IsChildOf(skip)) || !filter.sharedMesh.isReadable)
            {
                continue;
            }

            Matrix4x4 toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Vector3[] vertices = filter.sharedMesh.vertices;
            foreach (int index in filter.sharedMesh.triangles)
            {
                triangles.Add(toRoot.MultiplyPoint3x4(vertices[index]));
            }
        }

        StoneFace face = new()
        {
            Origin = new Vector2(area.xMin - Margin, area.yMin - Margin),
            Columns = Mathf.CeilToInt((area.width + Margin * 2f) / Step) + 1,
            Rows = Mathf.CeilToInt((area.height + Margin * 2f) / Step) + 1
        };
        face.Depth = new float[face.Columns * face.Rows];
        face.Front = float.MinValue;
        for (int row = 0; row < face.Rows; row++)
        {
            for (int column = 0; column < face.Columns; column++)
            {
                Vector2 point = face.Origin + new Vector2(column, row) * Step;
                float depth = Foremost(triangles, point);
                face.Depth[row * face.Columns + column] = depth;
                if (!float.IsNaN(depth) && area.Contains(point))
                {
                    face.Front = Mathf.Max(face.Front, depth);
                }
            }
        }

        if (face.Front == float.MinValue)
        {
            face.Front = 0f;
        }

        face.FillGaps();
        return face;
    }

    /// <summary>
    /// How far forward the surface stands at a point, between the grid points; clamped to the grid at its edges.
    /// </summary>
    /// <param name="x">Across, in the piece's space.</param>
    /// <param name="y">Up, in the piece's space.</param>
    /// <returns>The front of the surface.</returns>
    public float At(float x, float y)
    {
        if (Depth.Length == 0)
        {
            return Front;
        }

        float u = Mathf.Clamp((x - Origin.x) / Step, 0f, Columns - 1.001f);
        float v = Mathf.Clamp((y - Origin.y) / Step, 0f, Rows - 1.001f);
        int column = (int)u;
        int row = (int)v;
        float fu = u - column;
        float fv = v - row;
        float bottom = Mathf.Lerp(Depth[row * Columns + column], Depth[row * Columns + column + 1], fu);
        float top = Mathf.Lerp(Depth[(row + 1) * Columns + column], Depth[(row + 1) * Columns + column + 1], fu);
        return Mathf.Lerp(bottom, top, fv);
    }

    // The foremost surface the triangles have straight in front of or behind a point, or NaN where there is none.
    private static float Foremost(List<Vector3> triangles, Vector2 point)
    {
        float best = float.NaN;
        for (int i = 0; i + 2 < triangles.Count; i += 3)
        {
            Vector3 a = triangles[i];
            Vector3 b = triangles[i + 1];
            Vector3 c = triangles[i + 2];
            float area = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(area) < 1e-9f)
            {
                continue;
            }

            float wa = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) / area;
            float wb = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) / area;
            float wc = 1f - wa - wb;
            if (wa < -1e-5f || wb < -1e-5f || wc < -1e-5f)
            {
                continue;
            }

            float z = wa * a.z + wb * b.z + wc * c.z;
            if (float.IsNaN(best) || z > best)
            {
                best = z;
            }
        }

        return best;
    }

    // Points beside the stone take the depth of the nearest measured point in their row, or the front.
    private void FillGaps()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                int index = row * Columns + column;
                if (!float.IsNaN(Depth[index]))
                {
                    continue;
                }

                float found = Front;
                for (int reach = 1; reach < Columns; reach++)
                {
                    if (column - reach >= 0 && !float.IsNaN(Depth[index - reach])) { found = Depth[index - reach]; break; }
                    if (column + reach < Columns && !float.IsNaN(Depth[index + reach])) { found = Depth[index + reach]; break; }
                }

                Depth[index] = found;
            }
        }
    }
}

/// <summary>
/// Lays the letters of a text onto a curved stone face, each corner of each letter on the surface, every time the text
/// is drawn anew.
/// </summary>
public class CarvedText : MonoBehaviour
{
    // Just in front of the surface, so the stone does not show through the letters.
    private const float Lift = 0.004f;

    /// <summary>The face the letters lie on.</summary>
    public StoneFace Face;

    private TMP_Text text;
    private Transform piece;
    private bool laying;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        piece = GetComponentInParent<Piece>()?.transform ?? transform.root;
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        if (text != null)
        {
            text.ForceMeshUpdate();
        }
    }

    private void OnDisable() => TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);

    // Each time TextMeshPro rebuilds the text, moves every vertex onto the stone's curved face, so the letters follow
    // the stone instead of floating flat in front of it. The guard keeps the vertex update from calling this again.
    private void OnTextChanged(UnityEngine.Object changed)
    {
        if (laying || changed != text || Face == null || piece == null)
        {
            return;
        }

        laying = true;
        try
        {
            Matrix4x4 toPiece = piece.worldToLocalMatrix * text.transform.localToWorldMatrix;
            Matrix4x4 fromPiece = toPiece.inverse;
            TMP_TextInfo info = text.textInfo;
            for (int m = 0; m < info.meshInfo.Length; m++)
            {
                Vector3[] vertices = info.meshInfo[m].vertices;
                if (vertices == null)
                {
                    continue;
                }

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 point = toPiece.MultiplyPoint3x4(vertices[i]);
                    point.z = Face.At(point.x, point.y) + Lift;
                    vertices[i] = fromPiece.MultiplyPoint3x4(point);
                }
            }

            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
        finally
        {
            laying = false;
        }
    }
}
