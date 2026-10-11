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
/// A darkwood roof cross: the vanilla wooden roof cross (two beams crossed in an X, as wide as a wall and as high as
/// the roof's rise) built from two darkwood beams instead, as Valheim has no darkwood one. A clone of the wooden
/// cross keeps its snap points, colliders, support and place in the build menu; only its look is the darkwood beam's,
/// with its carved ends. Costs what a darkwood beam costs, as the wooden cross costs what a wooden beam does.
/// </summary>
public abstract class DarkwoodCrossBase : IWhiteHiltCustomPiece
{
    private const string Beam = "darkwood_beam";
    private const string After = "darkwood_beam_67";
    private const string SnapPrefix = "$hud_snappoint";

    // The two beams sit this far in front of and behind the cross's middle, so their faces do not draw in one place.
    private const float ArmOffset = 0.01f;

    // Half the width of a board of the cross, wooden and darkwood alike.
    private const float BoardHalf = 0.2f;

    private static readonly List<DarkwoodCrossBase> all = new();
    private static bool orderHooked;

    private readonly PieceManager instance;

    /// <summary>Prefab name of the new piece.</summary>
    protected abstract string PrefabName { get; }

    /// <summary>Name shown to players.</summary>
    protected abstract string FullName { get; }

    /// <summary>The vanilla wooden cross it is made from.</summary>
    protected abstract string CopyFrom { get; }

    /// <summary>How high the cross rises over its 2 m width: 1, 2 or 4 m, as the 26°, 45° and 67° roofs.</summary>
    protected abstract float Rise { get; }

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Plains;

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
    protected DarkwoodCrossBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName,
            "Two darkwood beams crossed under the gable, carved like the rest of the darkwood, for the roofs of a hall.");
    }

    /// <summary>
    /// Adds the piece to the hammer.
    /// </summary>
    public void Add()
    {
        try
        {
            GameObject beam = PrefabManager.Instance.GetPrefab(Beam) ?? throw new InvalidOperationException($"{Beam} not found");
            if (PrefabManager.Instance.GetPrefab(CopyFrom) == null)
            {
                throw new InvalidOperationException($"{CopyFrom} not found");
            }

            PieceConfig config = new()
            {
                Name = Translations.Token(PrefabName),
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                Requirements = beam.GetComponent<Piece>().m_resources
                    .Where(requirement => requirement.m_resItem != null)
                    .Select(requirement => new RequirementConfig
                    {
                        Item = requirement.m_resItem.name,
                        Amount = requirement.m_amount,
                        Recover = requirement.m_recover
                    })
                    .ToArray()
            };

            CustomPiece piece = new(PrefabName, CopyFrom, config);
            Rebuild(piece.PiecePrefab.transform, beam.transform);

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
                PieceManager.OnPiecesRegistered += PlaceAfterDarkwoodBeams;
            }

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Replaces the wooden boards in each wear state (new, worn, broken) with two darkwood beams from the same wear state,
    // laid corner to corner through the middle of the cross and as long as the wooden boards reach.
    private void Rebuild(Transform root, Transform beam)
    {
        // The wooden boards' outer corners reach the half width, so their centre lines end half a board short of it.
        float angle = Mathf.Atan2(Rise, 2f);
        float reach = (HalfWidth(root) - BoardHalf * Mathf.Sin(angle)) / Mathf.Cos(angle);
        BeamAxis(beam, out float centerline, out float halfLength);
        Vector3 middle = new(0f, Rise / 2f, 0f);

        foreach (Transform holder in root.Cast<Transform>().Where(IsModelHolder).ToList())
        {
            Transform source = beam.Find(holder.name) ?? beam.Find("New") ?? throw new InvalidOperationException($"{Beam} has no model");
            foreach (Transform old in holder.Cast<Transform>().ToList())
            {
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            Dictionary<Renderer, List<Renderer>> copies = new();
            foreach (float side in new[] { 1f, -1f })
            {
                GameObject arm = new("arm") { layer = holder.gameObject.layer };
                arm.transform.SetParent(holder, false);
                arm.transform.localPosition = middle + Vector3.forward * (ArmOffset * side);
                arm.transform.localRotation = Quaternion.Euler(0f, 0f, side * angle * Mathf.Rad2Deg);
                arm.transform.localScale = new Vector3(reach / halfLength, 1f, 1f);

                GameObject lowered = new("beam") { layer = holder.gameObject.layer };
                lowered.transform.SetParent(arm.transform, false);
                lowered.transform.localPosition = Vector3.down * centerline;
                CopyModel(source, lowered.transform, copies);
            }

            Relink(source, holder, copies);
        }
    }

    // The model parts of one wear state of the beam, without their colliders: the cross keeps its own.
    private static void CopyModel(Transform source, Transform parent, Dictionary<Renderer, List<Renderer>> copies)
    {
        foreach (Transform part in source)
        {
            GameObject copy = UnityEngine.Object.Instantiate(part.gameObject, parent);
            copy.name = part.name;
            copy.transform.localPosition = part.localPosition;
            copy.transform.localRotation = part.localRotation;
            copy.transform.localScale = part.localScale;
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer[] originals = part.GetComponentsInChildren<Renderer>(true);
            Renderer[] clones = copy.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < originals.Length && i < clones.Length; i++)
            {
                if (!copies.TryGetValue(originals[i], out List<Renderer> list))
                {
                    copies[originals[i]] = list = new List<Renderer>();
                }

                list.Add(clones[i]);
            }
        }
    }

    // Gives the wear state the beam's levels of detail with both beams in them, so its near and far models do not
    // draw at once.
    private static void Relink(Transform source, Transform holder, Dictionary<Renderer, List<Renderer>> copies)
    {
        LODGroup group = holder.GetComponent<LODGroup>();
        LODGroup template = source.GetComponent<LODGroup>();
        if (group == null)
        {
            return;
        }

        if (template == null)
        {
            UnityEngine.Object.DestroyImmediate(group);
            return;
        }

        LOD[] lods = template.GetLODs();
        for (int i = 0; i < lods.Length; i++)
        {
            lods[i].renderers = lods[i].renderers
                .Where(renderer => renderer != null && copies.ContainsKey(renderer))
                .SelectMany(renderer => copies[renderer])
                .ToArray();
        }

        group.SetLODs(lods);
        group.RecalculateBounds();
    }

    // How far the wooden boards reach out from the middle, read from the new model's mesh.
    private static float HalfWidth(Transform root)
    {
        float half = 0f;
        Transform model = root.Cast<Transform>().FirstOrDefault(child => child.name.Equals("New", StringComparison.OrdinalIgnoreCase));
        foreach (MeshFilter filter in (model ?? root).GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }

            Bounds bounds = filter.sharedMesh.bounds;
            Vector3 scale = filter.transform.lossyScale;
            Vector3 center = root.InverseTransformPoint(filter.transform.TransformPoint(bounds.center));
            half = Mathf.Max(half, Mathf.Abs(center.x) + bounds.extents.x * Mathf.Abs(scale.x));
        }

        return half > 0.1f ? half : 1.26f;
    }

    // The beam's centre line height and half length, from its snap points at its two ends.
    private static void BeamAxis(Transform beam, out float centerline, out float halfLength)
    {
        Transform[] snaps = beam.Cast<Transform>().Where(child => child.name.StartsWith(SnapPrefix, StringComparison.Ordinal)).ToArray();
        centerline = snaps.Length > 0 ? snaps.Average(snap => snap.localPosition.y) : 0.25f;
        halfLength = snaps.Length > 0 ? snaps.Max(snap => Mathf.Abs(snap.localPosition.x)) : 1f;
        if (halfLength < 0.1f)
        {
            halfLength = 1f;
        }
    }

    private static bool IsModelHolder(Transform child)
    {
        string name = child.name.ToLowerInvariant();
        return (name == "new" || name == "worn" || name == "broken" || name == "wornbroken") && child.childCount > 0;
    }

    // The crosses follow the darkwood beams in the hammer, 26° to 67°.
    private static void PlaceAfterDarkwoodBeams()
    {
        PieceTable table = PieceManager.Instance.GetPieceTable(PieceTables.Hammer);
        if (table == null)
        {
            return;
        }

        List<GameObject> pieces = table.m_pieces;
        List<GameObject> ours = all.Select(cross => pieces.FirstOrDefault(prefab => prefab != null && prefab.name == cross.PrefabName))
            .Where(prefab => prefab != null)
            .ToList();
        if (ours.Count == 0)
        {
            return;
        }

        pieces.RemoveAll(ours.Contains);
        int after = pieces.FindIndex(prefab => prefab != null && prefab.name == After);
        pieces.InsertRange(after < 0 ? pieces.Count : after + 1, ours);
    }
}

/// <summary>
/// Darkwood roof cross for the 26° roofs.
/// </summary>
public class DarkwoodCross26 : DarkwoodCrossBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodCross26(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_darkwood_cross_26";

    /// <inheritdoc/>
    protected override string FullName => "Darkwood Roof Cross 26°";

    /// <inheritdoc/>
    protected override string CopyFrom => "wood_wall_roof_top";

    /// <inheritdoc/>
    protected override float Rise => 1f;
}

/// <summary>
/// Darkwood roof cross for the 45° roofs.
/// </summary>
public class DarkwoodCross45 : DarkwoodCrossBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodCross45(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_darkwood_cross_45";

    /// <inheritdoc/>
    protected override string FullName => "Darkwood Roof Cross 45°";

    /// <inheritdoc/>
    protected override string CopyFrom => "wood_wall_roof_top_45";

    /// <inheritdoc/>
    protected override float Rise => 2f;
}

/// <summary>
/// Darkwood roof cross for the 67° roofs.
/// </summary>
public class DarkwoodCross67 : DarkwoodCrossBase
{
    /// <summary>
    /// Registers the piece's text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DarkwoodCross67(PieceManager instance) : base(instance)
    {
        // Valheim's own darkwood beam 67° has no name in its translations and shows its token in the hammer.
        Translations.AddEnglish("piece_darkwoodbeam67", "Darkwood Beam 67°");
    }

    /// <inheritdoc/>
    protected override string PrefabName => "piece_whitehilt_darkwood_cross_67";

    /// <inheritdoc/>
    protected override string FullName => "Darkwood Roof Cross 67°";

    /// <inheritdoc/>
    protected override string CopyFrom => "wood_wall_roof_top_67";

    /// <inheritdoc/>
    protected override float Rise => 4f;
}
