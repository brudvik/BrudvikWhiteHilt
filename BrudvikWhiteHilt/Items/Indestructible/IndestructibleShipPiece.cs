using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Indestructible;

/// <summary>
/// This class defines an indestructible ship piece.
/// Ships require special handling because they have unique damage systems
/// and child components that must remain functional.
/// </summary>
public class IndestructibleShipPiece : CustomPiece
{
    /// <summary>
    /// Constructor for the IndestructibleShipPiece class.
    /// </summary>
    /// <param name="name">The name of the ship piece.</param>
    /// <param name="basePrefabName">The base prefab to clone from.</param>
    /// <param name="itemConfig">The piece configuration.</param>
    public IndestructibleShipPiece(string name, string basePrefabName, PieceConfig itemConfig) : base(name, basePrefabName, itemConfig)
    {
        // Configure ship WearNTear with ship-specific settings
        var wearNTear = Piece.GetComponent<WearNTear>();
        MakeShipIndestructible(wearNTear);
    }

    /// <summary>
    /// Configures a ship's WearNTear to be highly resistant but still functional.
    /// Ships need special handling to maintain their float and sailing mechanics.
    /// </summary>
    /// <param name="wearNTear">The WearNTear component.</param>
    private static void MakeShipIndestructible(WearNTear wearNTear)
    {
        if (wearNTear == null) return;

        // Set very high health but don't use extreme values that might interfere with ship systems
        wearNTear.m_health = 100000f;
        
        // Ashlands protection
        wearNTear.m_ashDamageImmune = true;
        wearNTear.m_ashDamageResist = true;
        
        // Disable support and roof wear checks
        wearNTear.m_noSupportWear = true;
        wearNTear.m_noRoofWear = true;
        
        // Make immune to all damage types
        wearNTear.m_damages.m_fire = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_blunt = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_slash = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_chop = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_pickaxe = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_pierce = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_spirit = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_frost = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_lightning = HitData.DamageModifier.Immune;
        wearNTear.m_damages.m_poison = HitData.DamageModifier.Immune;
        
        Jotunn.Logger.LogDebug("Ship configured as indestructible");
    }
}
