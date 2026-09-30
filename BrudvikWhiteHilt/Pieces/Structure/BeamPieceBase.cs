using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure;

/// <summary>
/// How a vanilla beam or pole is turned into a new length or angle.
/// </summary>
public enum BeamShape
{
    /// <summary>Stretched along its length, as vanilla makes its 1 m and 2 m wooden poles from one block.</summary>
    Stretch,

    /// <summary>Two of the vanilla model end to end, so iron fittings keep their size.</summary>
    Double,

    /// <summary>Stretched to span 2 m across and tilted up, like the vanilla 26° and 45° wooden beams.</summary>
    Tilt
}

/// <summary>
/// A vanilla beam or pole in another length or at an angle. Keeps the vanilla material, strength, station and
/// category, costs the vanilla recipe times <see cref="CostFactor"/> and sits right after its original in the hammer.
/// </summary>
public abstract class BeamPieceBase : IWhiteHiltCustomPiece
{
    private const string SnapPrefix = "$hud_snappoint";

    private static readonly List<BeamPieceBase> all = new();
    private static bool orderHooked;

    private readonly PieceManager instance;

    /// <summary>Prefab name of the new piece.</summary>
    protected abstract string PrefabName { get; }

    /// <summary>Name shown to players.</summary>
    protected abstract string FullName { get; }

    /// <summary>Description shown to players.</summary>
    protected abstract string Description { get; }

    /// <summary>The vanilla piece it is made from.</summary>
    protected abstract string CopyFrom { get; }

    /// <summary>How the vanilla piece is reshaped.</summary>
    protected abstract BeamShape Shape { get; }

    /// <summary>The piece's length axis in its own space: right for beams, up for poles.</summary>
    protected abstract Vector3 Axis { get; }

    /// <summary>New length over the vanilla length, for <see cref="BeamShape.Stretch"/> and <see cref="BeamShape.Tilt"/>.</summary>
    protected virtual float LengthFactor => 2f;

    /// <summary>Degrees the beam is tilted up, for <see cref="BeamShape.Tilt"/>.</summary>
    protected virtual float TiltDegrees => 0f;

    /// <summary>The vanilla recipe is multiplied by this, rounded, at least 1 of each.</summary>
    protected virtual float CostFactor => 2f;

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected BeamPieceBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the piece to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            GameObject vanilla = PrefabManager.Instance.GetPrefab(CopyFrom) ?? throw new InvalidOperationException($"{CopyFrom} not found");
            Piece vanillaPiece = vanilla.GetComponent<Piece>();
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Requirements = vanillaPiece.m_resources
                    .Where(requirement => requirement.m_resItem != null)
                    .Select(requirement => new RequirementConfig
                    {
                        Item = requirement.m_resItem.name,
                        Amount = Mathf.Max(1, Mathf.RoundToInt(requirement.m_amount * CostFactor)),
                        Recover = requirement.m_recover
                    })
                    .ToArray()
            };

            CustomPiece piece = new(PrefabName, CopyFrom, pieceConfig);
            Reshape(piece.PiecePrefab.transform);

            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }

            instance.AddPiece(piece);
            all.Add(this);
            if (!orderHooked)
            {
                orderHooked = true;
                PieceManager.OnPiecesRegistered += PlaceAfterOriginals;
            }

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private void Reshape(Transform root)
    {
        Vector3 axis = Axis.normalized;
        switch (Shape)
        {
            case BeamShape.Stretch:
                Stretch(root, axis, LengthFactor, false);
                break;
            case BeamShape.Double:
                Double(root, axis);
                Stretch(root, axis, 2f, true);
                break;
            case BeamShape.Tilt:
                Stretch(root, axis, LengthFactor, false);
                Tilt(root);
                break;
        }
    }

    // Keeps spanning 2 m across and rises by tan(angle) * 2 m. The centre follows the vanilla wooden beams, so the
    // snap points land where theirs do: the 26° beam starts level with its origin, the 45° beam is centred on it.
    private void Tilt(Transform root)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, TiltDegrees);
        Vector3 center = TiltDegrees < 45f ? Vector3.up * Mathf.Tan(TiltDegrees * Mathf.Deg2Rad) : Vector3.zero;
        foreach (Transform child in root)
        {
            child.localPosition = center + rotation * child.localPosition;
            child.localRotation = rotation * child.localRotation;
        }
    }

    // Moves every child out along the axis, and lengthens those that run along it.
    private static void Stretch(Transform root, Vector3 axis, float factor, bool skipModels)
    {
        foreach (Transform child in root)
        {
            Vector3 position = child.localPosition;
            child.localPosition = position + axis * (Vector3.Dot(position, axis) * (factor - 1f));
            bool model = IsModelHolder(child);
            if (child.name.StartsWith(SnapPrefix) || child.name.IndexOf("top", StringComparison.OrdinalIgnoreCase) >= 0 || (skipModels && model))
            {
                continue;
            }

            ScaleAlong(child, axis, factor);
        }
    }

    // Scales through whichever of the child's own axes lies along the root axis.
    private static void ScaleAlong(Transform child, Vector3 axis, float factor)
    {
        Vector3 scale = child.localScale;
        for (int i = 0; i < 3; i++)
        {
            Vector3 local = Vector3.zero;
            local[i] = 1f;
            if (Mathf.Abs(Vector3.Dot(child.localRotation * local, axis)) > 0.9f)
            {
                scale[i] *= factor;
                child.localScale = scale;
                return;
            }
        }
    }

    // Two copies of each model part inside the new and worn holders, so the piece's wear still switches them.
    private static void Double(Transform root, Vector3 axis)
    {
        Dictionary<Renderer, Renderer> copies = new();
        foreach (Transform holder in root.Cast<Transform>().Where(IsModelHolder).ToList())
        {
            Vector3 shift = Quaternion.Inverse(holder.localRotation) * axis;
            Vector3 scale = holder.localScale;
            shift = new Vector3(shift.x / scale.x, shift.y / scale.y, shift.z / scale.z);
            foreach (Transform part in holder.Cast<Transform>().ToList())
            {
                GameObject copy = UnityEngine.Object.Instantiate(part.gameObject, holder);
                copy.name = part.name;
                copy.transform.localPosition = part.localPosition + shift;
                copy.transform.localRotation = part.localRotation;
                copy.transform.localScale = part.localScale;
                part.localPosition -= shift;

                Renderer[] originals = part.GetComponentsInChildren<Renderer>(true);
                Renderer[] clones = copy.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < originals.Length && i < clones.Length; i++)
                {
                    copies[originals[i]] = clones[i];
                }
            }
        }

        LODGroup group = root.GetComponent<LODGroup>();
        if (group == null)
        {
            return;
        }

        LOD[] lods = group.GetLODs();
        for (int i = 0; i < lods.Length; i++)
        {
            Renderer[] renderers = lods[i].renderers;
            lods[i].renderers = renderers
                .Concat(renderers.Where(renderer => renderer != null && copies.ContainsKey(renderer)).Select(renderer => copies[renderer]))
                .ToArray();
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
    }

    private static bool IsModelHolder(Transform child)
    {
        string name = child.name.ToLowerInvariant();
        return (name == "new" || name == "worn" || name == "wornbroken") && child.childCount > 0;
    }

    private static void PlaceAfterOriginals()
    {
        PieceTable table = PieceManager.Instance.GetPieceTable(PieceTables.Hammer);
        if (table == null)
        {
            return;
        }

        List<GameObject> pieces = table.m_pieces;
        foreach (BeamPieceBase beam in all)
        {
            int own = pieces.FindIndex(prefab => prefab != null && prefab.name == beam.PrefabName);
            if (own < 0)
            {
                continue;
            }

            GameObject prefab = pieces[own];
            pieces.RemoveAt(own);
            int after = LastIndexOfFamily(pieces, beam.CopyFrom);
            pieces.Insert(after < 0 ? pieces.Count : after + 1, prefab);
        }
    }

    // After the original and any of ours already placed behind it.
    private static int LastIndexOfFamily(List<GameObject> pieces, string copyFrom)
    {
        int index = pieces.FindIndex(prefab => prefab != null && prefab.name == copyFrom);
        if (index < 0)
        {
            return -1;
        }

        while (index + 1 < pieces.Count && all.Any(beam => beam.CopyFrom == copyFrom && pieces[index + 1] != null && pieces[index + 1].name == beam.PrefabName))
        {
            index++;
        }

        return index;
    }
}
