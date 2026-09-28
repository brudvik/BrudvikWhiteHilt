using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// Base class for White Hilt ships.
/// </summary>
public abstract class WhiteHiltShipBase : IWhiteHiltCustomPiece
{
    /// <summary>
    /// The base name of the ship.
    /// </summary>
    protected abstract string BaseName { get; }

    /// <summary>
    /// The full name of the ship.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// The description of the ship.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected abstract string CopyFrom { get; }

    /// <summary>
    /// The requirements for crafting the ship.
    /// </summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 10, Recover = false }
    };

    /// <summary>
    /// Indicates whether the weapon is enabled or not.
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
    /// Constructor for the WhiteHiltShipBase class.
    /// </summary>
    /// <param name="instance"></param>
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
            item.Piece.m_comfort = 5;

            // Set the ship to be ready for Ashlands
            if (item.Piece != null)
            {
                var shipSettings = item.Piece.GetComponent<Ship>();
                if (shipSettings != null)
                {
                    shipSettings.m_ashlandsReady = true;
                    shipSettings.m_sailForceFactor = 0.5f;
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
