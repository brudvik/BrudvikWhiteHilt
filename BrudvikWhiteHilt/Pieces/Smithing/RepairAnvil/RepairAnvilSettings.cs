using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Pieces.Smithing.RepairAnvil;

/// <summary>
/// Config for the Repair Anvil, section "RepairAnvil". Server-synced.
/// </summary>
public static class RepairAnvilSettings
{
    private const string Section = "RepairAnvil";

    private static string costText;
    private static List<(string Prefab, int Amount)> cost = new();

    /// <summary>What each repaired item costs, e.g. "Resin:1,Iron:1"; empty for free.</summary>
    public static ConfigEntry<string> CostPerItem { get; private set; }

    /// <summary>Whether everything in the inventory is repaired, not only what is worn.</summary>
    public static ConfigEntry<bool> WholeInventory { get; private set; }

    /// <summary>Whether repairing raises the Crafting skill, as at a workbench.</summary>
    public static ConfigEntry<bool> RaiseCraftingSkill { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        CostPerItem = WhiteHiltConfig.BindAdminOnly(Section, "CostPerItem", string.Empty,
            "What each repaired item costs, as prefab:amount pairs, e.g. Resin:1,Iron:1. Empty makes repairing free.");
        WholeInventory = WhiteHiltConfig.BindAdminOnly(Section, "WholeInventory", false,
            "Repair everything in the inventory, not only what you wear and the shield in its slot.");
        RaiseCraftingSkill = WhiteHiltConfig.BindAdminOnly(Section, "RaiseCraftingSkill", true,
            "Repairing raises the Crafting skill, as it does at a workbench.");

        Translations.AddEnglish("whitehilt_anvil_repair", "Repair everything you wear");
        Translations.AddEnglish("whitehilt_anvil_repair_all", "Repair everything you carry");
        Translations.AddEnglish("whitehilt_anvil_worn", "{0} worn items");
        Translations.AddEnglish("whitehilt_anvil_cost", "Cost per item");
        Translations.AddEnglish("msg_whitehilt_anvil_nothing", "Nothing needs repairing");
        Translations.AddEnglish("msg_whitehilt_anvil_repaired", "Repaired {0} items");
        Translations.AddEnglish("msg_whitehilt_anvil_missing", "You need {0} {1}");
    }

    /// <summary>
    /// The cost of repairing one item, parsed from <see cref="CostPerItem"/>. Unknown prefabs are left out.
    /// </summary>
    /// <returns>Prefab names and amounts.</returns>
    public static IReadOnlyList<(string Prefab, int Amount)> Cost()
    {
        string text = CostPerItem.Value ?? string.Empty;
        if (text == costText)
        {
            return cost;
        }

        costText = text;
        cost = new List<(string, int)>();
        foreach (string pair in text.Split(',').Select(part => part.Trim()).Where(part => part.Length > 0))
        {
            string[] parts = pair.Split(':');
            string prefab = parts[0].Trim();
            int amount = parts.Length > 1 && int.TryParse(parts[1].Trim(), out int parsed) ? parsed : 1;
            if (amount <= 0)
            {
                continue;
            }

            if (ObjectDB.instance != null && ObjectDB.instance.GetItemPrefab(prefab) == null)
            {
                Jotunn.Logger.LogWarning($"Repair Anvil: unknown item '{prefab}' in CostPerItem is ignored");
                continue;
            }

            cost.Add((prefab, amount));
        }

        return cost;
    }
}
