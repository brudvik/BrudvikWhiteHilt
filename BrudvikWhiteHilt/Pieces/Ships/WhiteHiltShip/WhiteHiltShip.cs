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
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's piece manager, which the piece is added to.</param>
    public WhiteHiltShip(PieceManager instance) : base(instance)
    {
        Translations.AddEnglish("whitehilt_ship_take", "Take off the last upgrade");
        Translations.AddEnglish("whitehilt_ship_add", "Add an upgrade");
        Translations.AddEnglish("whitehilt_ship_none", "No upgrades");
        Translations.AddEnglish("whitehilt_ship_upgrades", "Upgrades");
        Translations.AddEnglish("msg_whitehilt_ship_added", "Upgrade added");
        Translations.AddEnglish("msg_whitehilt_ship_already", "The ship already has that upgrade");
        Translations.AddEnglish("msg_whitehilt_ship_barrels_full", "Empty the extra cargo slots before taking the barrels off");
        Translations.AddEnglish("msg_whitehilt_ship_chest_full", "Empty the sea chest before taking it off");
        Translations.AddEnglish("whitehilt_ship_anchor_lower", "Lower the anchor");
        Translations.AddEnglish("whitehilt_ship_lantern_on", "Light the lantern");
        Translations.AddEnglish("whitehilt_ship_lantern_off", "Put out the lantern");
        Translations.AddEnglish("whitehilt_ship_lantern_blocked", "Kraken keeps the lantern dark");
        Translations.AddEnglish("whitehilt_ship_anchor_raise", "Raise the anchor");
        Translations.AddEnglish("whitehilt_ship_anchored", "At anchor");
        Translations.AddEnglish("msg_whitehilt_ship_anchor_lowered", "The anchor is lowered");
        Translations.AddEnglish("msg_whitehilt_ship_anchor_raised", "The anchor is raised");
        Translations.AddEnglish("msg_whitehilt_ship_anchor_down", "Raise the anchor at the mast before you sail");
        Translations.AddEnglish("msg_whitehilt_ship_net_catch", "The net caught");
        Translations.AddEnglish("whitehilt_shipportal_unnamed", "Unnamed ship portal");
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltShip";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Ship";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Ship of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "VikingShip";

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 20, Recover = false },
        new() { Item = "IronNails", Amount = 100, Recover = false },
        new() { Item = "BronzeNails", Amount = 100, Recover = false },
        new() { Item = "DeerHide", Amount = 15, Recover = false },
        new() { Item = "LeatherScraps", Amount = 15, Recover = false }
    };
}
