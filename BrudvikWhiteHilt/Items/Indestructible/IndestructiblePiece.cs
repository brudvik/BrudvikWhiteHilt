using Jotunn.Configs;
using Jotunn.Entities;

namespace BrudvikWhiteHilt.Items.Indestructible;

/// <summary>
/// This class defines an indestructible piece.
/// </summary>
public class IndestructiblePiece : CustomPiece
{
    /// <summary>
    /// Constructor for the IndestructiblePiece class.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="basePrefabName"></param>
    /// <param name="itemConfig"></param>
    public IndestructiblePiece(string name, string basePrefabName, PieceConfig itemConfig) : base(name, basePrefabName, itemConfig)
    {
        var wearNTear = Piece.GetComponent<WearNTear>();
        if (wearNTear != null)
        {
            wearNTear.m_health = 999;
            wearNTear.m_ashDamageImmune = true;
            wearNTear.m_ashDamageResist = true;
            wearNTear.m_damages.m_fire = HitData.DamageModifier.Immune;
            wearNTear.m_damages.m_blunt = HitData.DamageModifier.Immune;
            wearNTear.m_damages.m_slash = HitData.DamageModifier.Immune;
            wearNTear.m_damages.m_chop = HitData.DamageModifier.Immune;
            wearNTear.m_damages.m_pierce = HitData.DamageModifier.Immune;
            wearNTear.m_damages.m_spirit = HitData.DamageModifier.Immune;
            wearNTear.m_damages.m_frost = HitData.DamageModifier.Immune;
            wearNTear.m_damages.m_lightning = HitData.DamageModifier.Immune;
            wearNTear.m_damages.m_poison = HitData.DamageModifier.Immune;
            wearNTear.m_onDamaged += () => AlterDamage(wearNTear);
        }
    }

    /// <summary>
    /// This method alters the damage of the WearNTear component to make it indestructible.
    /// </summary>
    /// <param name="wearNTear"></param>
    private void AlterDamage(WearNTear wearNTear)
    {
        wearNTear.m_health = 999;
    }

}
