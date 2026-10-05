using BrudvikWhiteHilt.Crafting;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Patches.Crafting;

/// <summary>
/// Makes the items in nearby chests count as the local player's own while crafting and building, takes what the
/// inventory lacks from the chests, and marks the requirements that come from chests.
/// </summary>
[HarmonyPatch]
public static class NearbyContainerPatches
{
    private const string ChestIconName = "WhiteHiltChestIcon";
    private static readonly Color fromChestsColor = new(1f, 0.78f, 0.3f);

    // Set while vanilla counts the player's items for a requirement check, so the chests are added to the count.
    private static NearbyContainers.Use? counting;
    private static Sprite chestIcon;

    /// <summary>
    /// Keeps track of the containers in the world.
    /// </summary>
    /// <param name="__instance">The container.</param>
    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    [HarmonyPostfix]
    public static void RegisterContainer(Container __instance)
    {
        if (__instance.m_nview != null && __instance.m_nview.GetZDO() != null)
        {
            NearbyContainers.Register(__instance);
        }
    }

    /// <summary>
    /// Switches the use of chests off and on with the toggle key, and asks for the chests around the player to be
    /// handed over when the player gets ready to use them.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    public static void CheckToggleKey(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            NearbyContainers.CheckToggleKey(__instance);
            NearbyContainers.Warm(__instance);
        }
    }

    /// <summary>
    /// Counts the chests while vanilla checks whether a recipe can be crafted.
    /// </summary>
    /// <param name="__state">The previous counting state.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
    [HarmonyPrefix]
    public static void BeginCraftingCheck(out NearbyContainers.Use? __state)
    {
        __state = counting;
        counting = NearbyContainers.Use.Crafting;
    }

    /// <summary>
    /// Stops counting the chests.
    /// </summary>
    /// <param name="__state">The previous counting state.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
    [HarmonyPostfix]
    public static void EndCraftingCheck(NearbyContainers.Use? __state)
    {
        counting = __state;
    }

    /// <summary>
    /// Counts the chests while vanilla checks whether a piece can be built.
    /// </summary>
    /// <param name="__state">The previous counting state.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
    [HarmonyPrefix]
    public static void BeginBuildingCheck(out NearbyContainers.Use? __state)
    {
        __state = counting;
        counting = NearbyContainers.Use.Building;
    }

    /// <summary>
    /// Stops counting the chests.
    /// </summary>
    /// <param name="__state">The previous counting state.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
    [HarmonyPostfix]
    public static void EndBuildingCheck(NearbyContainers.Use? __state)
    {
        counting = __state;
    }

    /// <summary>
    /// Counts the chests while a requirement is shown, so it is not flashed red when the chests have enough.
    /// </summary>
    /// <param name="__state">The previous counting state.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    [HarmonyPrefix]
    public static void BeginRequirementDisplay(out NearbyContainers.Use? __state)
    {
        __state = counting;
        counting = CurrentUse();
    }

    /// <summary>
    /// Stops counting the chests and marks a requirement that is partly taken from chests: amber amount, a chest on the
    /// icon, and the split in the tooltip.
    /// </summary>
    /// <param name="elementRoot">The requirement element.</param>
    /// <param name="req">The requirement.</param>
    /// <param name="player">The player.</param>
    /// <param name="quality">Quality level being crafted.</param>
    /// <param name="craftMultiplier">Number of items being crafted.</param>
    /// <param name="__result">False if the requirement was hidden.</param>
    /// <param name="__state">The previous counting state.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    [HarmonyPostfix]
    public static void EndRequirementDisplay(Transform elementRoot, Piece.Requirement req, Player player, int quality, int craftMultiplier, bool __result,
        NearbyContainers.Use? __state)
    {
        counting = __state;
        if (!__result || req.m_resItem == null || player != Player.m_localPlayer)
        {
            ShowChestIcon(elementRoot, false);
            RequirementOverlay.Hide(elementRoot);
            return;
        }

        NearbyContainers.Use use = CurrentUse();
        bool chestsActive = NearbyContainers.IsActive(use);
        bool overlay = RequirementOverlay.IsOn(use);
        string name = req.m_resItem.m_itemData.m_shared.m_name;
        int need = req.GetAmount(quality) * craftMultiplier;
        int own = player.GetInventory().CountItems(name);
        int inChests = chestsActive && (overlay || own < need) ? NearbyContainers.Count(use, name) : 0;
        bool fromChests = own < need && own + inChests >= need;
        if (fromChests)
        {
            elementRoot.Find("res_amount").GetComponent<TMP_Text>().color = fromChestsColor;
        }

        if (overlay)
        {
            RequirementOverlay.Show(elementRoot, req.m_resItem, own, inChests, need, use, chestsActive);
        }
        else
        {
            RequirementOverlay.Hide(elementRoot);
            if (fromChests)
            {
                elementRoot.GetComponent<UITooltip>().m_text += Localization.instance.Localize($"\n{own} $whitehilt_chests_inventory + {inChests} $whitehilt_chests_chests");
            }
        }

        ShowChestIcon(elementRoot, fromChests);
    }

    /// <summary>
    /// Hides the chest icon and the overlay of a requirement slot vanilla empties.
    /// </summary>
    /// <param name="elementRoot">The requirement element.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.HideRequirement))]
    [HarmonyPostfix]
    public static void HideRequirementExtras(Transform elementRoot)
    {
        ShowChestIcon(elementRoot, false);
        RequirementOverlay.Hide(elementRoot);
    }

    /// <summary>
    /// Hides the chest icon and the overlay on the build menu slot that shows the crafting station, which vanilla
    /// fills without <see cref="InventoryGui.SetupRequirement"/>.
    /// </summary>
    /// <param name="__instance">The HUD.</param>
    /// <param name="piece">The selected piece.</param>
    [HarmonyPatch(typeof(Hud), nameof(Hud.SetupPieceInfo))]
    [HarmonyPostfix]
    public static void HideStationSlotExtras(Hud __instance, Piece piece)
    {
        if (piece == null || piece.m_craftingStation == null || piece.m_resources.Length >= __instance.m_requirementItems.Length)
        {
            return;
        }

        Transform station = __instance.m_requirementItems[piece.m_resources.Length].transform;
        ShowChestIcon(station, false);
        RequirementOverlay.Hide(station);
    }

    /// <summary>
    /// Adds the chests to the local player's item count during a requirement check.
    /// </summary>
    /// <param name="__instance">The inventory counted.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="quality">Item quality, or -1 for any.</param>
    /// <param name="__result">The count.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
    [HarmonyPostfix]
    public static void AddChestsToCount(Inventory __instance, string name, int quality, ref int __result)
    {
        if (IsCountingFor(__instance))
        {
            __result += NearbyContainers.Count(counting.Value, name, quality);
        }
    }

    /// <summary>
    /// Lets an item in a nearby chest count as had, for the "can almost build" check.
    /// </summary>
    /// <param name="__instance">The inventory checked.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="__result">True if the item is had.</param>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveItem), typeof(string), typeof(bool))]
    [HarmonyPostfix]
    public static void AddChestsToHaveItem(Inventory __instance, string name, ref bool __result)
    {
        if (!__result && IsCountingFor(__instance))
        {
            __result = NearbyContainers.Count(counting.Value, name) > 0;
        }
    }

    /// <summary>
    /// Takes what the inventory lacks from the nearby chests before vanilla takes the rest from the inventory.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="requirements">The requirements.</param>
    /// <param name="qualityLevel">Quality level being crafted.</param>
    /// <param name="itemQuality">Required item quality, or -1.</param>
    /// <param name="multiplier">Number of items being crafted.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    [HarmonyPrefix]
    public static void TakeFromChests(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
    {
        NearbyContainers.Use use = CurrentUse();
        if (__instance != Player.m_localPlayer || !NearbyContainers.IsActive(use))
        {
            return;
        }

        // The same requirements vanilla consumes: upgrade-only resources only at an upgrading station.
        CraftingStation station = __instance.GetCurrentCraftingStation();
        foreach (Piece.Requirement requirement in requirements)
        {
            bool skip = station != null ? station.m_upgrader != requirement.m_upgraderResource : requirement.m_upgraderResource;
            if (skip || requirement.m_resItem == null)
            {
                continue;
            }

            string name = requirement.m_resItem.m_itemData.m_shared.m_name;
            int missing = requirement.GetAmount(qualityLevel) * multiplier - __instance.GetInventory().CountItems(name, itemQuality);
            if (missing > 0)
            {
                NearbyContainers.Take(use, name, missing, itemQuality);
            }
        }
    }

    // Crafting happens with the inventory open; building with it closed.
    private static NearbyContainers.Use CurrentUse()
    {
        return InventoryGui.IsVisible() ? NearbyContainers.Use.Crafting : NearbyContainers.Use.Building;
    }

    private static bool IsCountingFor(Inventory inventory)
    {
        return counting.HasValue && Player.m_localPlayer != null && inventory == Player.m_localPlayer.GetInventory() && NearbyContainers.IsActive(counting.Value);
    }

    private static void ShowChestIcon(Transform elementRoot, bool show)
    {
        Transform icon = elementRoot.Find($"res_icon/{ChestIconName}");
        if (icon == null)
        {
            if (!show)
            {
                return;
            }

            icon = CreateChestIcon(elementRoot.Find("res_icon"));
            if (icon == null)
            {
                return;
            }
        }

        if (icon.gameObject.activeSelf != show)
        {
            icon.gameObject.SetActive(show);
        }
    }

    // A small chest in the lower right corner of the item icon.
    private static Transform CreateChestIcon(Transform itemIcon)
    {
        chestIcon ??= ZNetScene.instance?.GetPrefab("piece_chest_wood")?.GetComponent<Piece>()?.m_icon;
        if (itemIcon == null || chestIcon == null)
        {
            return null;
        }

        GameObject icon = new(ChestIconName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)icon.transform;
        rect.SetParent(itemIcon, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(1.05f, 0.55f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = icon.GetComponent<Image>();
        image.sprite = chestIcon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return rect;
    }
}
