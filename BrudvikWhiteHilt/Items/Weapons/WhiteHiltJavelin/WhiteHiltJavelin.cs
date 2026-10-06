using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltJavelin;

/// <summary>
/// The White Hilt Javelin, a light spear cloned from the vanilla <c>SpearElderbark</c> with the White Hilt model
/// <c>whjavelin</c>. Made for throwing: it flies faster and hits harder when thrown, costs less stamina to throw, and
/// stabs a little weaker than the White Hilt Spear.
/// </summary>
public class WhiteHiltJavelin : WhiteHiltWeaponBase
{
    private const float ThrowSpeed = 1.4f;
    private const float ThrowDamage = 1.3f;
    private const float ThrowStamina = 0.75f;
    private const float StabDamage = 0.8f;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltJavelin(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltJavelin";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Javelin";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Javelin of Dyrnwyn. Light, and made to be thrown.";

    /// <inheritdoc/>
    protected override string CopyFrom => "SpearElderbark";

    /// <summary>
    /// Javelin by beyondmatter, with a white grip.
    /// </summary>
    protected override string ModelName => "whjavelin";

    /// <inheritdoc/>
    protected override bool ThrownAsModel => true;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 10, Recover = false },
        new() { Item = "ElderBark", Amount = 5, Recover = false },
        new() { Item = "Feathers", Amount = 10, Recover = false }
    };

    /// <summary>
    /// A faster, harder and cheaper throw, and a weaker stab.
    /// </summary>
    /// <param name="shared">The weapon's shared data.</param>
    protected override void AdjustStats(ItemDrop.ItemData.SharedData shared)
    {
        shared.m_attack.m_damageMultiplier *= StabDamage;
        Attack throwAttack = shared.m_secondaryAttack;
        throwAttack.m_damageMultiplier *= ThrowDamage;
        throwAttack.m_projectileVel *= ThrowSpeed;
        throwAttack.m_projectileVelMin *= ThrowSpeed;
        throwAttack.m_attackStamina *= ThrowStamina;
    }
}
