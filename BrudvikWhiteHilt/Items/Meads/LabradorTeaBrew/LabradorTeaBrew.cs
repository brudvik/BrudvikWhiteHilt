using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Meads.LabradorTeaBrew;

/// <summary>
/// A sharp-smelling brew that biting insects cannot stand: leeches, deathsquitoes and ticks do not notice the drinker.
/// </summary>
public class LabradorTeaBrew : WhiteHiltMeadBase
{
    private static ConfigEntry<string> ignoredBy;
    private static string parsedFrom;
    private static HashSet<string> ignoringCreatures = new();

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public LabradorTeaBrew(ItemManager instance) : base(instance)
    {
        ignoredBy = WhiteHiltConfig.BindAdminOnly(ConfigSection, "IgnoredBy", "Leech,Leech_cave,Deathsquito,Tick",
            "Creatures (prefab names, comma separated) that do not notice someone who drank the brew.");
    }

    /// <summary>
    /// Whether a creature leaves alone someone who drank the brew.
    /// </summary>
    /// <param name="prefabName">Prefab name of the creature.</param>
    /// <returns>True if the creature ignores the drinker.</returns>
    public static bool Ignores(string prefabName)
    {
        string value = ignoredBy?.Value ?? string.Empty;
        if (!ReferenceEquals(value, parsedFrom))
        {
            parsedFrom = value;
            ignoringCreatures = new HashSet<string>(value.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }

        return ignoringCreatures.Contains(prefabName);
    }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltLabradorTeaBrew";

    /// <inheritdoc/>
    protected override string FullName => "Labrador Tea Brew";

    /// <inheritdoc/>
    protected override string Description => "A resinous, eye-watering brew of Labrador tea. Drink it and your sweat smells so sharp that leeches, deathsquitoes and ticks leave you be.";

    /// <inheritdoc/>
    protected override string EffectTooltip => "Leeches, deathsquitoes and ticks do not notice you";

    /// <inheritdoc/>
    protected override string CopyMeadFrom => "MeadStaminaMinor";

    /// <inheritdoc/>
    protected override string CopyMeadBaseFrom => "MeadBaseStaminaMinor";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = Foraging.LabradorTea.LabradorTea.PrefabName, Amount = 8, Recover = false },
        new() { Item = Foraging.SweetGale.SweetGale.PrefabName, Amount = 2, Recover = false },
        new() { Item = "Honey", Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    protected override Color Tint => new(0.72f, 0.8f, 0.55f);

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffectInstance()
    {
        return ScriptableObject.CreateInstance<InsectWardEffect>();
    }

    /// <inheritdoc/>
    protected override void ConfigureEffect(SE_Stats effect)
    {
    }
}
