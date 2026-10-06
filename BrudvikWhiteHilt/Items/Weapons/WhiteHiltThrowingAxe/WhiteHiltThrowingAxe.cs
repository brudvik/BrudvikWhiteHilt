using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltThrowingAxe;

/// <summary>
/// The White Hilt Throwing Axe, cloned from the vanilla <c>SpearElderbark</c> for its throw, with the swing of the
/// vanilla <c>AxeIron</c> as its primary attack and the White Hilt model <c>whthrowingaxe</c>. It hews like an axe,
/// is thrown like a spear, spinning end over end, and lands where it hits to be picked up again.
/// </summary>
public class WhiteHiltThrowingAxe : WhiteHiltWeaponBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltThrowingAxe(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltThrowingAxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Throwing Axe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Throwing Axe of Dyrnwyn. Hew with it, or throw it.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SpearElderbark";

    /// <summary>
    /// Viking Throwing Axe by Sam (rawpuwnzl), with a white grip.
    /// </summary>
    protected override string ModelName => "whthrowingaxe";

    /// <inheritdoc/>
    protected override bool ThrownAsModel => true;

    /// <inheritdoc/>
    protected override float ThrownSpin => 2.5f;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 15, Recover = false },
        new() { Item = "Feathers", Amount = 5, Recover = false },
        new() { Item = "AxeFlint", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Swings like the Iron Axe, cuts and chops instead of piercing, and trains the axe skill.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        ItemDrop axe = PrefabManager.Cache.GetPrefab<ItemDrop>("AxeIron");
        if (axe != null)
        {
            shared.m_attack = axe.m_itemData.m_shared.m_attack.Clone();
        }
        else
        {
            Jotunn.Logger.LogWarning("AxeIron not found; the White Hilt Throwing Axe stabs like a spear.");
        }

        shared.m_skillType = Skills.SkillType.Axes;
        shared.m_damages = new HitData.DamageTypes { m_slash = 50f, m_chop = 30f };
        shared.m_damagesPerLevel = new HitData.DamageTypes { m_slash = 4f, m_chop = 2f };
        shared.m_toolTier = 2;
    }
}
