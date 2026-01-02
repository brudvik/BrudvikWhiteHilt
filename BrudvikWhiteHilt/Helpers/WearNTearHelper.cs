namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Helper class for configuring WearNTear components to be indestructible.
/// </summary>
public static class WearNTearHelper
{
    /// <summary>
    /// Configures a WearNTear component to be indestructible.
    /// </summary>
    /// <param name="wearNTear">The WearNTear component to configure.</param>
    /// <param name="health">The health value to set (default 999).</param>
    public static void MakeIndestructible(WearNTear wearNTear, float health = 999f)
    {
        if (wearNTear == null) return;

        wearNTear.m_health = health;
        wearNTear.m_ashDamageImmune = true;
        wearNTear.m_ashDamageResist = true;
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
        wearNTear.m_onDamaged += () => ResetHealth(wearNTear, health);
    }

    /// <summary>
    /// Resets the health of the WearNTear component when damaged.
    /// </summary>
    /// <param name="wearNTear">The WearNTear component.</param>
    /// <param name="health">The health value to reset to.</param>
    private static void ResetHealth(WearNTear wearNTear, float health)
    {
        wearNTear.m_health = health;
    }
}
