using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltCrystalAxe;

/// <summary>
/// The White Hilt Crystal Axe, a one-handed axe cloned from the vanilla <c>AxeIron</c>, so a shield can be carried with
/// it. It shows the White Hilt Battleaxe's model scaled down (<c>whaxe</c>, with a lilac head), carries the Crystal
/// Battleaxe's glow and deals spirit damage like it. In linear progression it unlocks with the Mountain tier. Not to be
/// confused with the White Hilt Axe, the tool (Items/Tools/WhiteHiltAxe).
/// </summary>
public class WhiteHiltCrystalAxe : WhiteHiltWeaponBase
{
    // Centre of the axe head in attach space, from AssetSource/Models/whaxe.weapon.json.
    private static readonly Vector3 headCentre = new(0f, 0f, 0.65f);
    private static readonly string[] crystalGlow = { "Point Light", "flare" };

    private static ConfigEntry<float> spiritDamage;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
    public WhiteHiltCrystalAxe(ItemManager instance) : base(instance)
    {
        spiritDamage ??= WhiteHiltConfig.BindAdminOnly("Gear.Weapons", "CrystalAxeSpiritDamage", 20f,
            "Spirit damage of the White Hilt Crystal Axe, as the Crystal Battleaxe deals; hurts the undead.", new AcceptableValueRange<float>(0f, 100f));
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltCrystalAxe";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Crystal Axe";

    /// <inheritdoc/>
    protected override string Description => "The Indestructible Crystal Axe of Dyrnwyn";

    /// <inheritdoc/>
    protected override string CopyFrom => "AxeIron";

    /// <inheritdoc/>
    protected override float BonusSpiritDamage => spiritDamage?.Value ?? 0f;

    /// <summary>
    /// Nordic Axe - Cloudcleaver by Peter Nox, the White Hilt Battleaxe's model at one-handed size, with a white grip and
    /// a lilac head.
    /// </summary>
    protected override string ModelName => "whaxe";

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <summary>
    /// Crystal is a Mountain material, an exception to the White Hilt gear rule, since the axe is the Crystal
    /// Battleaxe's one-handed kin.
    /// </summary>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Iron", Amount = 20, Recover = false },
        new() { Item = "Crystal", Amount = 10, Recover = false },
        new() { Item = "AxeIron", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Puts the Crystal Battleaxe's lilac light and glint on the axe head.
    /// </summary>
    /// <param name="model">The axe model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        Transform source = PrefabManager.Cache.GetPrefab<GameObject>("BattleaxeCrystal")?.transform.Find("attach");
        if (source == null)
        {
            Jotunn.Logger.LogWarning("BattleaxeCrystal has no attach child; the White Hilt Crystal Axe gets no glow.");
            return;
        }

        foreach (string name in crystalGlow)
        {
            Transform glow = source.Find(name);
            if (glow == null)
            {
                Jotunn.Logger.LogWarning($"BattleaxeCrystal has no attach/{name}; the White Hilt Crystal Axe goes without it.");
                continue;
            }

            GameObject copy = Object.Instantiate(glow.gameObject, model.transform.parent, false);
            copy.name = name;
            copy.transform.localPosition = headCentre;
        }
    }
}
