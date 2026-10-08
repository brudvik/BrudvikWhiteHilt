using BrudvikWhiteHilt.Crafting;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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

    // How long after the items have arrived the replayed click to place a piece may come.
    private const float ReplayWindow = 1f;
    private static readonly Color fromChestsColor = new(1f, 0.78f, 0.3f);

    // Set while vanilla counts the player's items for a requirement check, so the chests are added to the count.
    private static NearbyContainers.Use? counting;

    // Set while a crafting or a piece is paid for: then only the chests handed over count, the ones that are taken from.
    // Otherwise every chest the player may use counts, so what is shown is what lies there.
    private static bool paying;
    private static Sprite chestIcon;

    // The piece whose placing is clicked again once the items for it arrived; it is placed without fetching again.
    private static Piece replaying;
    private static float replayAt;

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
    /// Switches the use of chests off and on with the toggle key, asks for the chests around a station the player looks
    /// at to be handed over, and goes on with a crafting or placing whose chests did not answer in time.
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
            ChestWithdrawal.Tick();
        }
    }

    /// <summary>
    /// Before a crafting is done: fetches what the inventory lacks from the chests nearby. If other players hold some of
    /// them, the crafting stops here and is done again by itself once the items have arrived. Then vanilla checks and
    /// takes the requirements, with only the chests handed over counting.
    /// </summary>
    /// <param name="__instance">The inventory screen.</param>
    /// <param name="player">The crafting player.</param>
    /// <param name="__state">Whether paying was already on.</param>
    /// <returns>False to wait for the chests.</returns>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    [HarmonyPrefix]
    public static bool BeginCrafting(InventoryGui __instance, Player player, out bool __state)
    {
        __state = paying;
        Recipe recipe = __instance.m_craftRecipe;
        if (player != Player.m_localPlayer || recipe == null || player.NoCostCheat())
        {
            return true;
        }

        int quality = __instance.m_craftUpgradeItem != null ? __instance.m_craftUpgradeItem.m_quality + 1 : 1;
        int multiplier = __instance.m_multiCrafting ? Mathf.Max(1, __instance.m_multiCraftAmount) : 1;
        CraftingStation station = player.GetCurrentCraftingStation();
        if (!ChestWithdrawal.Continuing)
        {
            if (ChestWithdrawal.Busy)
            {
                player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_chests_waiting");
                return false;
            }

            List<(string Name, int Amount)> needs = CraftingNeeds(player, recipe, station, quality, multiplier);
            if (ChestWithdrawal.Fetch(player, NearbyContainers.Use.Crafting, needs, ResumeCrafting(__instance, player, station)))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_chests_waiting");
                return false;
            }
        }

        paying = true;

        // Vanilla gives up without a word when the requirements are not met; say so, as the chests are often why.
        if (NearbyContainers.IsActive(NearbyContainers.Use.Crafting) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost)
            && !player.HaveRequirements(recipe, false, quality, multiplier))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_chests_notenough");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Counts every chest again after a crafting.
    /// </summary>
    /// <param name="__state">Whether paying was already on.</param>
    /// <param name="__exception">The original error, if any.</param>
    /// <returns>The unchanged original error.</returns>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    [HarmonyFinalizer]
    public static Exception EndCrafting(bool __state, Exception __exception)
    {
        paying = __state;
        return __exception;
    }

    /// <summary>
    /// Before a piece is placed: as <see cref="BeginCrafting"/>, for the piece's resources. A placing that waited for
    /// the chests is clicked again by itself, and then refused if the items still did not suffice, so a piece is never
    /// placed half paid for.
    /// </summary>
    /// <param name="__instance">The building player.</param>
    /// <param name="piece">The piece.</param>
    /// <param name="__state">Whether paying was already on.</param>
    /// <returns>False to wait for the chests.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    [HarmonyPrefix]
    public static bool BeginPlacing(Player __instance, Piece piece, out bool __state)
    {
        __state = paying;
        if (__instance != Player.m_localPlayer || piece == null || __instance.NoCostCheat())
        {
            return true;
        }

        bool replayed = replaying == piece && Time.time - replayAt <= ReplayWindow;
        replaying = null;
        if (!replayed)
        {
            if (ChestWithdrawal.Busy)
            {
                __instance.Message(MessageHud.MessageType.Center, "$msg_whitehilt_chests_waiting");
                return false;
            }

            // The hoe's big brush costs more than the piece itself; that is fetched along with it.
            float extra = Building.TerrainPatches.BrushExtra(__instance, piece);
            List<(string Name, int Amount)> needs = piece.m_resources.Where(requirement => requirement.m_resItem != null)
                .Select(requirement => (requirement.m_resItem.m_itemData.m_shared.m_name,
                    requirement.m_amount + Mathf.CeilToInt(requirement.m_amount * extra))).ToList();
            if (ChestWithdrawal.Fetch(__instance, NearbyContainers.Use.Building, needs, ResumePlacing(__instance, piece)))
            {
                __instance.Message(MessageHud.MessageType.Center, "$msg_whitehilt_chests_waiting");
                return false;
            }
        }

        paying = true;
        if (NearbyContainers.IsActive(NearbyContainers.Use.Building) && !ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())
            && !__instance.HaveRequirements(piece, Player.RequirementMode.CanBuild))
        {
            __instance.Message(MessageHud.MessageType.Center, "$msg_whitehilt_chests_notenough");
            return false;
        }

        return true;
    }

    private static float nextRefusalLog;

    /// <summary>
    /// When a click to build is refused for its requirements, writes to the log which one is missing: the station, or
    /// each resource as the inventory holds it, as it is counted, and in the chests. Vanilla shows one message for all.
    /// </summary>
    /// <param name="__instance">The building player.</param>
    /// <param name="piece">The piece.</param>
    /// <param name="mode">What is checked.</param>
    /// <param name="__result">Whether the requirements are met.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void ExplainRefusal(Player __instance, Piece piece, Player.RequirementMode mode, bool __result)
    {
        if (__result || mode != Player.RequirementMode.CanBuild || __instance != Player.m_localPlayer || piece == null
            || Time.time < nextRefusalLog || !(ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace")))
        {
            return;
        }

        nextRefusalLog = Time.time + 1f;
        string station = piece.m_craftingStation == null ? "none"
            : $"{piece.m_craftingStation.m_name} known={__instance.m_knownStations.ContainsKey(piece.m_craftingStation.m_name)} "
              + $"inRange={CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, __instance.transform.position) != null}";
        Inventory inventory = __instance.GetInventory();
        string resources = string.Join(", ", piece.m_resources.Where(requirement => requirement.m_resItem != null).Select(requirement =>
        {
            string name = requirement.m_resItem.m_itemData.m_shared.m_name;
            int held = inventory.GetAllItems().Where(item => item.m_shared.m_name == name).Sum(item => item.m_stack);
            int kept = global::BrudvikWhiteHilt.Backpack.UtilitySlots.CountKept(inventory, name, -1, true);
            return $"{name} need={requirement.m_amount} inInventory={held} counted={inventory.CountItems(name)} "
                + $"kept={kept} "
                + $"chestsHeld={NearbyContainers.Count(NearbyContainers.Use.Building, name)} chestsAll={NearbyContainers.CountAll(NearbyContainers.Use.Building, name)}";
        }));
        Jotunn.Logger.LogWarning($"Cannot build {piece.name}: station {station}; {resources}; paying={paying}; at {__instance.transform.position}");
    }

    /// <summary>
    /// Counts every chest again after a piece is placed.
    /// </summary>
    /// <param name="__state">Whether paying was already on.</param>
    /// <param name="__exception">The original error, if any.</param>
    /// <returns>The unchanged original error.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    [HarmonyFinalizer]
    public static Exception EndPlacing(bool __state, Exception __exception)
    {
        paying = __state;
        return __exception;
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
        int inChests = chestsActive && (overlay || own < need) ? NearbyContainers.CountAll(use, name) : 0;
        int usable = chestsActive && own < need ? NearbyContainers.CountForCheck(use, name, -1, false) : 0;
        bool fromChests = own < need && own + usable >= need;
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
            __result += NearbyContainers.CountForCheck(counting.Value, name, quality, paying);
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
            __result = NearbyContainers.CountForCheck(counting.Value, name, -1, paying) > 0;
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

    // What a crafting takes: each requirement times the amount crafted, without the upgrade-only ones at a plain
    // station. A recipe that takes any one of its ingredients fetches nothing when the inventory holds one, and else
    // only the first one the chests can make up.
    private static List<(string Name, int Amount)> CraftingNeeds(Player player, Recipe recipe, CraftingStation station, int quality,
        int multiplier)
    {
        List<(string Name, int Amount)> needs = recipe.m_resources
            .Where(requirement => requirement.m_resItem != null
                && (station != null ? station.m_upgrader == requirement.m_upgraderResource : !requirement.m_upgraderResource))
            .Select(requirement => (requirement.m_resItem.m_itemData.m_shared.m_name, requirement.GetAmount(quality) * multiplier))
            .ToList();
        if (!recipe.m_requireOnlyOneIngredient)
        {
            return needs;
        }

        Inventory inventory = player.GetInventory();
        if (needs.Any(need => inventory.CountItems(need.Name) >= need.Amount))
        {
            return new List<(string Name, int Amount)>();
        }

        return needs.Where(need => inventory.CountItems(need.Name)
                + NearbyContainers.CountForCheck(NearbyContainers.Use.Crafting, need.Name, -1, false) >= need.Amount)
            .Take(1).ToList();
    }

    // Crafts once more when the items fetched for it have arrived. Vanilla reads what to craft from fields a new click
    // overwrites, so they are captured now, set back for the crafting and restored after it. Nothing is crafted if the
    // player has left the station meanwhile.
    private static Action ResumeCrafting(InventoryGui gui, Player player, CraftingStation station)
    {
        Recipe recipe = gui.m_craftRecipe;
        ItemDrop.ItemData upgrade = gui.m_craftUpgradeItem;
        int variant = gui.m_craftVariant;
        bool multiCrafting = gui.m_multiCrafting;
        int multiCraftAmount = gui.m_multiCraftAmount;
        return () =>
        {
            if (gui == null || player == null || player != Player.m_localPlayer || player.IsDead()
                || player.GetCurrentCraftingStation() != station)
            {
                Jotunn.Logger.LogInfo($"Did not craft {recipe.name} after fetching from the chests, as the player left the station.");
                return;
            }

            Recipe savedRecipe = gui.m_craftRecipe;
            ItemDrop.ItemData savedUpgrade = gui.m_craftUpgradeItem;
            int savedVariant = gui.m_craftVariant;
            bool savedMultiCrafting = gui.m_multiCrafting;
            int savedMultiCraftAmount = gui.m_multiCraftAmount;
            gui.m_craftRecipe = recipe;
            gui.m_craftUpgradeItem = upgrade;
            gui.m_craftVariant = variant;
            gui.m_multiCrafting = multiCrafting;
            gui.m_multiCraftAmount = multiCraftAmount;
            try
            {
                gui.DoCrafting(player);
            }
            finally
            {
                gui.m_craftRecipe = savedRecipe;
                gui.m_craftUpgradeItem = savedUpgrade;
                gui.m_craftVariant = savedVariant;
                gui.m_multiCrafting = savedMultiCrafting;
                gui.m_multiCraftAmount = savedMultiCraftAmount;
            }
        };
    }

    // Clicks to place the piece again when the items fetched for it have arrived: vanilla places a piece within a moment
    // of the click time, and pays for it and trains the skill around the placing, so setting the time does it all the
    // vanilla way, wherever the piece is aimed now. Nothing happens if the player put the hammer away or chose another
    // piece meanwhile.
    private static Action ResumePlacing(Player player, Piece piece)
    {
        return () =>
        {
            if (player == null || player != Player.m_localPlayer || !player.InPlaceMode() || player.GetSelectedPiece() != piece)
            {
                Jotunn.Logger.LogInfo($"Did not place {piece.name} after fetching from the chests, as the player stopped building it.");
                return;
            }

            replaying = piece;
            replayAt = Time.time;
            player.m_placePressedTime = Time.time;
        };
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

    // Shows or hides the small chest mark on a requirement that is (partly) taken from chests.
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
