using BrudvikWhiteHilt.Companions;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Pieces.Farming;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using BrudvikWhiteHilt.Ranching;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Texts whose numbers come from the config, so they stay right when an admin changes it.
/// Meads and potions register their own.
/// </summary>
public static class DynamicTexts
{
    // Vanilla explore radius, which the Navigator's Table and Pathfinder's Amulet bonuses are shares of.
    private const float VanillaExploreRadius = 100f;

    /// <summary>
    /// Registers the dynamic texts. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        Translations.AddDynamic("piece_whitehilt_feedingtrough_description", () => new object[] { Translations.Number(RanchingSettings.TroughRange.Value) });
        Translations.AddDynamic("piece_whitehilt_dogbowl_description", () => new object[] { Translations.Number(RanchingSettings.TroughRange.Value) });
        Translations.AddDynamic("piece_whitehilt_tetherpost_description", () => new object[] { Translations.Number(RanchingSettings.TetherRange.Value) });
        Translations.AddDynamic("piece_whitehilt_compostbin_description", () => new object[]
        {
            CompostSettings.WastePerCompost.Value,
            Translations.Number(CompostSettings.Range.Value),
            Translations.Percent(1f - CompostSettings.GrowTime.Value),
            CompostSettings.CompostPerDay.Value
        });

        Translations.AddDynamic("item_whitehiltcharttable_description", () => new object[] { Translations.Number(ExploreRadius(NavigationSettings.TableBonus.Value)) });
        Translations.AddDynamic("item_whitehiltpathfinder_description", () => new object[]
        {
            Translations.Number(ExploreRadius(NavigationSettings.AmuletBonus.Value)),
            Translations.Number(NavigationSettings.RavenSightRadius.Value)
        });
        Translations.AddDynamic("whitehilt_skill_exploration_description", () => new object[] { NavigationSettings.SharedMapRevealLevel.Value });

        Translations.AddDynamic("whitehilt_mapextension_inactive", () => new object[] { Translations.Number(PortalSettings.MapTableExtensionRange) });
        Translations.AddDynamic("piece_whitehilt_valkyriestone_description", () => new object[]
        {
            ValkyrieCostPhrase(),
            PortalSettings.ValkyrieOncePerDeath ? Translations.Word("whitehilt_valkyrie_once") : string.Empty
        });
        Translations.AddDynamic("whitehilt_valkyrie_confirm", () => new object[] { ValkyrieCostPhrase() });

        Translations.AddDynamic("item_whitehiltdogbandage_description", () => new object[] { Translations.Percent(DogSettings.BandageHeal.Value) });

        Translations.AddDynamic("whitehilt_valkyrie_cost_many", () => new object[] { PortalSettings.ValkyrieCost });

        Translations.AddEnglish("whitehilt_valkyrie_free", "nothing");
        Translations.AddEnglish("whitehilt_valkyrie_one", "one Surtling Core");
        Translations.AddEnglish("whitehilt_valkyrie_cost_many", "{0} Surtling Cores");
        Translations.AddEnglish("whitehilt_valkyrie_once", ", once for each death");
    }

    private static float ExploreRadius(float bonus)
    {
        return NavigationSettings.ExploreRadiusBonus.Value ? VanillaExploreRadius * (1f + bonus) : VanillaExploreRadius;
    }

    private static string ValkyrieCostPhrase()
    {
        int cost = PortalSettings.ValkyrieCost;
        return cost switch
        {
            <= 0 => Translations.Word("whitehilt_valkyrie_free"),
            1 => Translations.Word("whitehilt_valkyrie_one"),
            _ => string.Format(Translations.Word("whitehilt_valkyrie_cost_many"), cost)
        };
    }
}
