using Jotunn.Managers;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// Sets the household up when the game starts: its settings, the householder's prefab, the farm clothes and the console command
/// whitehilt_household. The plan is docs/plans/household.md.
/// </summary>
public static class HouseholdRegistry
{
    /// <summary>
    /// Registers the texts, the prefab and the command. Call once from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        HouseholdSettings.Initialize();
        HouseholdBody.RegisterTranslations();
        HouseholdPanel.RegisterTranslations();
        HouseholdMember.RegisterTranslations();
        HouseholdLooks.RegisterTranslations();
        PrefabManager.OnVanillaPrefabsAvailable += CreatePrefab;
        HouseholdCommands.Register();
    }

    private static void CreatePrefab()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= CreatePrefab;
        HouseholdLooks.CreateClothes();
        HouseholdBody.CreatePrefab();
    }
}
