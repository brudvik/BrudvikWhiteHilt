using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Structure;

/// <summary>Single buildable iron panels assembled from unscaled vanilla grate models.</summary>
public abstract class IronGratePieceBase : IWhiteHiltCustomPiece
{
    private readonly PieceManager instance;

    /// <summary>Panel width in metres.</summary>
    protected abstract int Width { get; }

    /// <summary>Panel height in metres.</summary>
    protected abstract int Height { get; }

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public string Id => $"piece_whitehilt_iron_grate_{Width}x{Height}";

    /// <inheritdoc/>
    public string DisplayName => $"Iron grate {Width}x{Height}m";

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Id);

    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>Registers the panel text.</summary>
    /// <param name="instance">The piece manager.</param>
    protected IronGratePieceBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Id, DisplayName,
            $"An iron grate panel {Width} m wide and {Height} m high.");
    }

    /// <inheritdoc/>
    public void Add()
    {
        try
        {
            int tileSize = Height == 1 ? 1 : 2;
            string sourceName = $"iron_wall_{tileSize}x{tileSize}";
            GameObject source = PrefabManager.Instance.GetPrefab(sourceName)
                ?? throw new InvalidOperationException($"{sourceName} not found");
            int tiles = Width * Height / (tileSize * tileSize);
            PieceConfig config = new()
            {
                Name = NameToken,
                Description = Translations.Token($"{Id}_description"),
                PieceTable = PieceTables.Hammer,
                Requirements = source.GetComponent<Piece>().m_resources
                    .Where(requirement => requirement.m_resItem != null)
                    .Select(requirement => new RequirementConfig
                    {
                        Item = requirement.m_resItem.name,
                        Amount = requirement.m_amount * tiles,
                        Recover = requirement.m_recover
                    }).ToArray()
            };
            CustomPiece piece = new(Id, sourceName, config);
            Assemble(piece.PiecePrefab, Width, Height, tileSize);
            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
            }

            instance.AddPiece(piece);
            Jotunn.Logger.LogInfo($"{DisplayName} added!");
        }
        catch (Exception exception)
        {
            Jotunn.Logger.LogError($"{DisplayName} failed to load!");
            Jotunn.Logger.LogError(exception);
        }
    }

    private static void Assemble(GameObject prefab, int width, int height, int tileSize)
    {
        Transform root = prefab.transform;
        WearNTear wear = prefab.GetComponent<WearNTear>();
        wear.m_new = Tile(wear.m_new, root, width, height, tileSize);
        wear.m_worn = Tile(wear.m_worn, root, width, height, tileSize);
        wear.m_broken = Tile(wear.m_broken, root, width, height, tileSize);
        wear.m_fragmentRoots = wear.m_fragmentRoots
            .Select(fragment => Tile(fragment, root, width, height, tileSize)).ToArray();

        BoxCollider collider = prefab.GetComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = new Vector3(width, height, collider.size.z);
        foreach (Transform snap in root.Cast<Transform>()
            .Where(child => child.CompareTag("snappoint")).ToArray())
        {
            UnityEngine.Object.DestroyImmediate(snap.gameObject);
        }

        for (int column = 0; column <= width; column++)
        {
            AddSnap(root, new Vector3(column - width * 0.5f, -height * 0.5f, 0f));
            AddSnap(root, new Vector3(column - width * 0.5f, height * 0.5f, 0f));
        }

        for (int row = 1; row < height; row++)
        {
            AddSnap(root, new Vector3(-width * 0.5f, row - height * 0.5f, 0f));
            AddSnap(root, new Vector3(width * 0.5f, row - height * 0.5f, 0f));
        }
    }

    private static GameObject Tile(GameObject source, Transform root, int width, int height, int tileSize)
    {
        GameObject state = new($"WhiteHilt{source.name}");
        state.SetActive(source.activeSelf);
        state.layer = source.layer;
        state.transform.SetParent(root, false);
        source.transform.SetParent(state.transform, false);
        Vector3 originalPosition = source.transform.localPosition;
        for (int column = 0; column < width / tileSize; column++)
        {
            for (int row = 0; row < height / tileSize; row++)
            {
                GameObject tile = column == 0 && row == 0
                    ? source : UnityEngine.Object.Instantiate(source, state.transform);
                tile.SetActive(true);
                tile.transform.localPosition = originalPosition + new Vector3(
                    -width * 0.5f + tileSize * (column + 0.5f),
                    -height * 0.5f + tileSize * (row + 0.5f) - (tileSize == 1 ? 0.5f : 0f), 0f);
            }
        }

        return state;
    }

    private static void AddSnap(Transform root, Vector3 position)
    {
        GameObject snap = new("$hud_snappoint");
        snap.layer = root.gameObject.layer;
        snap.tag = "snappoint";
        snap.transform.SetParent(root, false);
        snap.transform.localPosition = position;
        snap.SetActive(false);
    }
}

/// <summary>Iron grate two metres wide and one metre high.</summary>
public sealed class IronGrate2x1 : IronGratePieceBase
{
    /// <summary>Registers the panel text.</summary>
    /// <param name="instance">The piece manager.</param>
    public IronGrate2x1(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override int Width => 2;

    /// <inheritdoc/>
    protected override int Height => 1;
}

/// <summary>Iron grate four metres wide and four metres high.</summary>
public sealed class IronGrate4x4 : IronGratePieceBase
{
    /// <summary>Registers the panel text.</summary>
    /// <param name="instance">The piece manager.</param>
    public IronGrate4x4(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override int Width => 4;

    /// <inheritdoc/>
    protected override int Height => 4;
}

/// <summary>Iron grate four metres wide and one metre high.</summary>
public sealed class IronGrate4x1 : IronGratePieceBase
{
    /// <summary>Registers the panel text.</summary>
    /// <param name="instance">The piece manager.</param>
    public IronGrate4x1(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override int Width => 4;

    /// <inheritdoc/>
    protected override int Height => 1;
}