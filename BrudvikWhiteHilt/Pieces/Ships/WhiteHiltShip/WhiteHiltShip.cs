using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// The Indestructible Ship of Dyrnwyn
/// </summary>
public class WhiteHiltShip : WhiteHiltShipBase
{
    /// <summary>
    /// Constructor for the WhiteHiltShip class.
    /// </summary>
    /// <param name="instance"></param>
    public WhiteHiltShip(PieceManager instance) : base(instance)
    {
        Translations.AddEnglish("whitehilt_ship_take", "Take off the last upgrade");
        Translations.AddEnglish("whitehilt_ship_add", "Add an upgrade");
        Translations.AddEnglish("whitehilt_ship_none", "No upgrades");
        Translations.AddEnglish("whitehilt_ship_upgrades", "Upgrades");
        Translations.AddEnglish("msg_whitehilt_ship_added", "Upgrade added");
        Translations.AddEnglish("msg_whitehilt_ship_already", "The ship already has that upgrade");
        Translations.AddEnglish("msg_whitehilt_ship_barrels_full", "Empty the extra cargo slots before taking the barrels off");
        Translations.AddEnglish("whitehilt_ship_anchor_lower", "Lower the anchor");
        Translations.AddEnglish("whitehilt_ship_anchor_raise", "Raise the anchor");
        Translations.AddEnglish("whitehilt_ship_anchored", "At anchor");
        Translations.AddEnglish("msg_whitehilt_ship_anchor_lowered", "The anchor is lowered");
        Translations.AddEnglish("msg_whitehilt_ship_anchor_raised", "The anchor is raised");
        Translations.AddEnglish("msg_whitehilt_ship_anchor_down", "Raise the anchor at the mast before you sail");
        Translations.AddEnglish("msg_whitehilt_ship_net_catch", "The net caught");
    }

    /// <summary>
    /// The base name of the ship.
    /// </summary>
    protected override string BaseName => "WhiteHiltShip";

    /// <summary>
    /// The full name of the ship.
    /// </summary>
    protected override string FullName => "White Hilt Ship";

    /// <summary>
    /// The description of the ship.
    /// </summary>
    protected override string Description => "The Indestructible Ship of Dyrnwyn";

    /// <summary>
    /// The name of the item to copy from.
    /// </summary>
    protected override string CopyFrom => "VikingShip";

    /// <summary>
    /// Indicates whether the White Hilt ship is enabled.
    /// </summary>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject ship)
    {
        try
        {
            WhiteHiltShipUpgradeSetup.Prepare(ship);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName}: upgrades are not available: {ex.Message}");
        }

        WhiteHiltShipLook.Apply(ship);
    }

    /// <summary>
    /// The name of the ship prefab.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 20, Recover = false },
        new() { Item = "IronNails", Amount = 100, Recover = false },
        new() { Item = "BronzeNails", Amount = 100, Recover = false },
        new() { Item = "DeerHide", Amount = 15, Recover = false },
        new() { Item = "LeatherScraps", Amount = 15, Recover = false }
    };
}
