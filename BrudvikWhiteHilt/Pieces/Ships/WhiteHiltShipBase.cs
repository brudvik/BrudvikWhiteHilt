using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Base class for the White Hilt ships, built with the hammer. Each is a clone of a vanilla ship
/// (<see cref="CopyFrom"/>), which brings its physics, sail and controls; this class makes it indestructible, ready for
/// the Ashlands' fiery water and gives it comfort, and lets the subclass change the prefab
/// (<see cref="CustomizePrefab"/>). The plugin finds every subclass by reflection and calls <see cref="Add"/>.
/// </summary>
public abstract class WhiteHiltShipBase : IWhiteHiltCustomPiece
{
    /// <summary>
    /// Prefab name of the ship. Saved worlds refer to the ship by it, so it must never change once released.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// Name shown to players, in English. Other languages come from the embedded translation files
    /// (Translations/*.json), keyed by the prefab name.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Item description, in English; translated the same way as the name.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Vanilla ship it is cloned from; it brings the physics, sail, controls and hold.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// Ingredients of the recipe. The progression tier may add its own cost on top (see ProgressionManager).
    /// </summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 10, Recover = false }
    };

    /// <summary>
    /// Whether the ship is added to the game at all. A disabled one is skipped when the plugin starts, so it never
    /// reaches ObjectDB or a recipe.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PieceKey);

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    private string PieceKey => $"piece_{BaseName.ToLowerInvariant()}";

    private readonly PieceManager instance;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's piece manager, which the piece is added to.</param>
    protected WhiteHiltShipBase(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PieceKey, FullName, Description);
    }

    /// <summary>
    /// Adds the ship piece to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = Translations.Token(PieceKey),
                Description = Translations.Token($"{PieceKey}_description"),
                PieceTable = PieceTables.Hammer,
                Requirements = Requirements
            };

            IndestructiblePiece item = new(BaseName, CopyFrom, pieceConfig);

            // Set the ship to be ready for Ashlands
            if (item.Piece != null)
            {
                item.Piece.m_comfort = 5;
                var shipSettings = item.Piece.GetComponent<Ship>();
                if (shipSettings != null)
                {
                    shipSettings.m_ashlandsReady = true;
                }
            }
            else
            {
                Jotunn.Logger.LogWarning($"{FullName} failed to load ship configuration, ship is not set to be ready for Ashlands!");
            }

            CustomizePrefab(item.PiecePrefab);
            Sprite icon = VisualHelper.RenderIcon(item.PiecePrefab);
            if (icon != null)
            {
                item.Piece.m_icon = icon;
            }

            instance.AddPiece(item);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Changes the cloned ship prefab, e.g. its look or extra components. Runs on servers too. Does nothing by default.
    /// </summary>
    /// <param name="ship">The cloned ship prefab.</param>
    protected virtual void CustomizePrefab(GameObject ship) { }
}
