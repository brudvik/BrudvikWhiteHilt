using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Tools;

/// <summary>An everlasting hammer with a separate menu for compact ship workshops.</summary>
public sealed class WhiteHiltShipHammer : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    /// <summary>Stable item prefab identifier.</summary>
    public const string PrefabName = "WhiteHiltShipHammer";
    /// <summary>Separate workshop build table.</summary>
    public const string TableName = "_WhiteHiltShipHammerPieceTable";

    private readonly ItemManager instance;
    private IndestructibleItem added;

    /// <inheritdoc/>
    public bool Enabled => true;
    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;
    /// <inheritdoc/>
    public string Id => PrefabName;
    /// <inheritdoc/>
    public string DisplayName => "White Hilt Ship Hammer";
    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));
    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>Registers the hammer's text and shared durability settings.</summary>
    /// <param name="instance">Item registration manager.</param>
    public WhiteHiltShipHammer(ItemManager instance)
    {
        this.instance = instance;
        IndestructibleItem.BindConfig();
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), DisplayName,
            "An everlasting shipwright's hammer. Builds compact workshops aboard Skidbladnir.");
    }

    /// <summary>Applies the same durability settings as other White Hilt tools.</summary>
    public void ApplyConfig() => added?.ApplyConfig();

    /// <summary>Registers the tool and its independent repair/build menu.</summary>
    public void Add()
    {
        try
        {
            PieceManager.Instance.AddPieceTable(new CustomPieceTable(TableName, new PieceTableConfig { CanRemovePieces = true }));
            GameObject repair = PrefabManager.Instance.GetPrefab("Hammer").GetComponent<ItemDrop>()
                .m_itemData.m_shared.m_buildPieces.m_pieces.First(prefab => prefab.GetComponent<Piece>().m_repairPiece);
            PieceManager.Instance.AddPiece(new CustomPiece("WhiteHiltShipRepair", repair.name, new PieceConfig
            {
                PieceTable = TableName,
                Name = repair.GetComponent<Piece>().m_name,
                Description = repair.GetComponent<Piece>().m_description
            }));
            IndestructibleItem hammer = new(PrefabName, "Hammer", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token(Translations.ItemKey(PrefabName) + "_description"),
                CraftingStation = CraftingStations.Workbench,
                PieceTable = TableName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Wood", Amount = 3 },
                    new() { Item = "Iron", Amount = 2 },
                    new() { Item = "Resin", Amount = 2 }
                }
            });
            if (!VisualHelper.IsHeadless)
            {
                foreach (Renderer renderer in hammer.ItemPrefab.GetComponentsInChildren<Renderer>(true))
                    if (renderer.material.HasProperty("_Color")) renderer.material.color = new Color(0.85f, 0.95f, 1f);
                Sprite icon = VisualHelper.RenderIcon(hammer.ItemPrefab);
                if (icon != null)
                {
                    hammer.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
                }
            }
            instance.AddItem(hammer);
            added = hammer;
        }
        catch (Exception exception)
        {
            Jotunn.Logger.LogError($"Ship hammer failed to load: {exception}");
        }
    }
}