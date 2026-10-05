using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BrudvikWhiteHilt.Chests;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Lets the local player craft, build, fuel and cook with what lies in the chests around them, as if it were in their
/// inventory. Only containers the player may open are used (private chests, wards), and never one another player has open.
/// In multiplayer only chests handed over to the player (<see cref="ContainerHandoff"/>) count and are taken from, so
/// two players never write the same chest at once. They are asked for when the player gets ready to use them: takes
/// out the hammer, opens the crafting panel or looks at a smelter, fire or cooking station. Alone or as the host the
/// player owns every chest, and nothing waits.
/// </summary>
public static class NearbyContainers
{
    /// <summary>
    /// What the items are wanted for; each can be switched off in the config.
    /// </summary>
    public enum Use
    {
        /// <summary>Crafting at a crafting station.</summary>
        Crafting,

        /// <summary>Building with the hammer and other build tools.</summary>
        Building,

        /// <summary>Fuel and ore for smelters, kilns and fires.</summary>
        FuelAndOre,

        /// <summary>Raw food for cooking stations.</summary>
        Cooking
    }

    private const string Section = "ChestCrafting";
    private const string AzuCraftyBoxesGuid = "Azumatt.AzuCraftyBoxes";
    private const float ListLifetime = 0.5f;
    private const float ListMoveTolerance = 0.5f;

    // The build menu checks every piece each frame, so counts are reused for a moment; taking from a chest clears them.
    private const float CountLifetime = 0.25f;

    // How often the chests around a player who is about to use them are looked at, and how long before a chest that is
    // not handed over is asked for again. The wait keeps two busy players from passing chests back and forth.
    private const float WarmInterval = 0.5f;
    private const float RequestCooldown = 10f;
    private const int RequestedLimit = 500;

    private static readonly HashSet<Container> all = new();
    private static readonly List<(Container Container, float SqrDistance)> nearby = new();
    private static readonly Dictionary<(string Name, int Quality, bool Building), int> countCache = new();
    private static readonly Dictionary<(string Name, bool Building), bool> unlimitedCache = new();
    private static readonly Dictionary<Container, float> requested = new();

    private static ConfigEntry<bool> enabled;
    private static ConfigEntry<float> range;
    private static ConfigEntry<float> buildRange;
    private static ConfigEntry<bool> leaveOne;
    private static ConfigEntry<bool> forCrafting;
    private static ConfigEntry<bool> forBuilding;
    private static ConfigEntry<bool> forFuelAndOre;
    private static ConfigEntry<bool> forCooking;
    private static ConfigEntry<string> excludedContainers;
    private static ConfigEntry<string> excludedItems;
    private static ConfigEntry<KeyboardShortcut> toggleKey;
    private static ConfigEntry<KeyCode> fillAllKey;
    private static ConfigEntry<bool> showTakenChests;

    private static HashSet<string> excludedContainerSet = new();
    private static HashSet<string> excludedItemSet;
    private static bool pausedByPlayer;
    private static bool? otherModInstalled;
    private static float listTime = float.NegativeInfinity;
    private static Vector3 listPosition;
    private static float countTime = float.NegativeInfinity;
    private static float nextWarm;

    /// <summary>
    /// True while the fill-all key (Shift by default) is held: fuel and ore are filled up in one go.
    /// </summary>
    public static bool FillAllHeld => fillAllKey != null && Input.GetKey(fillAllKey.Value);

    /// <summary>
    /// True if the chests should show when something is taken from them.
    /// </summary>
    public static bool ShowTakenChests => showTakenChests == null || showTakenChests.Value;

    /// <summary>
    /// Tells which items the restocking chests keep without limit, or null while the chest module is off.
    /// </summary>
    public static IUnlimitedItems Unlimited { get; set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("msg_whitehilt_chests_on", "Using nearby chests");
        Translations.AddEnglish("msg_whitehilt_chests_off", "Not using nearby chests");
        Translations.AddEnglish("whitehilt_chests_inventory", "in your inventory");
        Translations.AddEnglish("whitehilt_chests_chests", "in chests");

        enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true, "Use the items in nearby chests, carts and ship holds for crafting, building, fuel, ore and cooking.");
        range = WhiteHiltConfig.BindAdminOnly(Section, "Range", 30f, "How far away a chest may be when crafting, fuelling and cooking, in metres.",
            new AcceptableValueRange<float>(1f, 100f));
        buildRange = WhiteHiltConfig.BindAdminOnly(Section, "BuildRange", 30f, "How far away a chest may be when building, in metres.",
            new AcceptableValueRange<float>(1f, 100f));
        leaveOne = WhiteHiltConfig.BindAdminOnly(Section, "LeaveOne", false, "Leave one of each item in a chest, so it keeps its place there.");
        forCrafting = WhiteHiltConfig.BindAdminOnly(Section, "Crafting", true, "Use chests when crafting at a crafting station.");
        forBuilding = WhiteHiltConfig.BindAdminOnly(Section, "Building", true, "Use chests when building.");
        forFuelAndOre = WhiteHiltConfig.BindAdminOnly(Section, "FuelAndOre", true, "Use chests when adding fuel or ore to smelters, kilns and fires.");
        forCooking = WhiteHiltConfig.BindAdminOnly(Section, "Cooking", true, "Use chests when putting raw food on a cooking station.");
        excludedContainers = WhiteHiltConfig.BindAdminOnly(Section, "ExcludedContainers", string.Empty,
            "Comma-separated prefab names of containers never taken from, e.g. piece_chest_private,Karve.");
        excludedItems = WhiteHiltConfig.BindAdminOnly(Section, "ExcludedItems", string.Empty,
            "Comma-separated prefab names of items never taken from chests, e.g. Coins,Ruby.");
        toggleKey = WhiteHiltConfig.BindLocal(Section, "ToggleKey", new KeyboardShortcut(KeyCode.O, KeyCode.LeftAlt),
            "Switches the use of nearby chests off and on for you.");
        fillAllKey = WhiteHiltConfig.BindLocal(Section, "FillAllKey", KeyCode.LeftShift,
            "Hold while adding fuel or ore to fill it up at once from your inventory and nearby chests.");
        showTakenChests = WhiteHiltConfig.BindLocal(Section, "ShowTakenChests", true,
            "Chests open their lid and glow briefly when something is taken from them.");

        excludedContainers.SettingChanged += (_, _) => excludedContainerSet = ParseList(excludedContainers.Value);
        excludedItems.SettingChanged += (_, _) => excludedItemSet = null;
        range.SettingChanged += (_, _) => ForgetNearby();
        buildRange.SettingChanged += (_, _) => ForgetNearby();
        excludedContainerSet = ParseList(excludedContainers.Value);
    }

    /// <summary>
    /// True if chests may be used for this purpose right now.
    /// </summary>
    /// <param name="use">What the items are wanted for.</param>
    /// <returns>True if chests count.</returns>
    public static bool IsActive(Use use)
    {
        if (enabled == null || !enabled.Value || pausedByPlayer || Player.m_localPlayer == null || IsOtherModInstalled())
        {
            return false;
        }

        return use switch
        {
            Use.Crafting => forCrafting.Value,
            Use.Building => forBuilding.Value,
            Use.FuelAndOre => forFuelAndOre.Value,
            Use.Cooking => forCooking.Value,
            _ => false
        };
    }

    /// <summary>
    /// Registers a container; called when it wakes up. Destroyed containers are dropped when the list is next refreshed.
    /// </summary>
    /// <param name="container">The container.</param>
    public static void Register(Container container)
    {
        all.Add(container);
    }

    /// <summary>
    /// Every container in the loaded world.
    /// </summary>
    /// <returns>The containers.</returns>
    public static IEnumerable<Container> Registered()
    {
        all.RemoveWhere(container => container == null);
        return all;
    }

    /// <summary>
    /// Switches the use of chests off and on when the toggle key is pressed. Call once a frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void CheckToggleKey(Player player)
    {
        if (toggleKey == null || !toggleKey.Value.IsDown() || !player.TakeInput())
        {
            return;
        }

        pausedByPlayer = !pausedByPlayer;
        player.Message(MessageHud.MessageType.Center, pausedByPlayer ? "$msg_whitehilt_chests_off" : "$msg_whitehilt_chests_on");
    }

    /// <summary>
    /// Asks for the chests around the local player to be handed over while the player gets ready to use them, so
    /// they count and can be taken from by the time the player clicks. Call once a frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Warm(Player player)
    {
        if (Time.time < nextWarm)
        {
            return;
        }

        nextWarm = Time.time + WarmInterval;
        Use? use = UpcomingUse(player);
        if (use == null || !IsActive(use.Value))
        {
            return;
        }

        if (requested.Count > RequestedLimit)
        {
            requested.Clear();
        }

        long playerId = player.GetPlayerID();
        foreach (Container container in InRange(use.Value))
        {
            if (ContainerHandoff.Held(container) || (requested.TryGetValue(container, out float at) && Time.time - at < RequestCooldown))
            {
                continue;
            }

            requested[container] = Time.time;
            ContainerHandoff.Ready(container, playerId);
        }
    }

    /// <summary>
    /// How many of an item lie in the chests around the local player.
    /// </summary>
    /// <param name="use">What the items are wanted for; building has its own range.</param>
    /// <param name="name">Shared item name, e.g. $item_wood.</param>
    /// <param name="quality">Item quality, or -1 for any.</param>
    /// <returns>The number available.</returns>
    public static int Count(Use use, string name, int quality = -1)
    {
        if (IsExcluded(name))
        {
            return 0;
        }

        ExpireCounts();
        bool building = use == Use.Building;
        if (countCache.TryGetValue((name, quality, building), out int cached))
        {
            return cached;
        }

        int count = 0;
        foreach (Container container in GetNearby(use))
        {
            count += Available(container, name, quality);
        }

        countCache[(name, quality, building)] = count;
        return count;
    }

    /// <summary>
    /// Checks whether a chest around the local player keeps an item without limit, so it never runs out here.
    /// </summary>
    /// <param name="use">What the items are wanted for; building has its own range.</param>
    /// <param name="name">Shared item name.</param>
    /// <returns>True if a chest in range refills the item.</returns>
    public static bool IsUnlimitedNearby(Use use, string name)
    {
        if (Unlimited == null || IsExcluded(name))
        {
            return false;
        }

        ExpireCounts();
        bool building = use == Use.Building;
        if (unlimitedCache.TryGetValue((name, building), out bool cached))
        {
            return cached;
        }

        bool unlimited = GetNearby(use).Any(container => container.GetInventory().GetAllItems()
            .Any(item => item.m_shared.m_name == name && Unlimited.IsUnlimitedIn(container, item)));
        unlimitedCache[(name, building)] = unlimited;
        return unlimited;
    }

    /// <summary>
    /// Takes up to <paramref name="amount"/> of an item out of the nearest chests.
    /// </summary>
    /// <param name="use">What the items are wanted for; building has its own range.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="amount">How many to take.</param>
    /// <param name="quality">Item quality, or -1 for any.</param>
    /// <returns>How many were taken.</returns>
    public static int Take(Use use, string name, int amount, int quality = -1)
    {
        return TakeFrom(GetNearby(use).ToList(), name, amount, quality);
    }

    /// <summary>
    /// Takes up to <paramref name="amount"/> of an item out of the given containers, in their order.
    /// </summary>
    /// <param name="containers">The containers, e.g. from <see cref="Around"/>.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="amount">How many to take.</param>
    /// <param name="quality">Item quality, or -1 for any.</param>
    /// <returns>How many were taken.</returns>
    public static int TakeFrom(IEnumerable<Container> containers, string name, int amount, int quality = -1)
    {
        if (amount <= 0 || IsExcluded(name))
        {
            return 0;
        }

        int taken = 0;
        foreach (Container container in containers)
        {
            if (container == null || !container.m_nview.IsValid() || Available(container, name, quality) <= 0)
            {
                continue;
            }

            // Only a chest this player owns is written; it was handed over with its newest contents.
            if (!container.m_nview.IsOwner())
            {
                continue;
            }

            int take = Mathf.Min(amount - taken, Available(container, name, quality));
            if (take <= 0)
            {
                continue;
            }

            container.GetInventory().RemoveItem(name, take, quality);
            container.Save();
            if (ShowTakenChests)
            {
                ContainerPulse.Play(container);
            }

            taken += take;
            if (taken >= amount)
            {
                break;
            }
        }

        countCache.Clear();
        unlimitedCache.Clear();
        return taken;
    }

    // Both would take the same items from the chests. Checked in game, when every plugin has loaded.
    private static bool IsOtherModInstalled()
    {
        if (!otherModInstalled.HasValue)
        {
            otherModInstalled = Chainloader.PluginInfos.ContainsKey(AzuCraftyBoxesGuid);
            if (otherModInstalled.Value)
            {
                Jotunn.Logger.LogWarning("AzuCraftyBoxes is installed, so White Hilt does not use nearby chests.");
            }
        }

        return otherModInstalled.Value;
    }

    private static int Available(Container container, string name, int quality)
    {
        int count = container.GetInventory()?.CountItems(name, quality) ?? 0;
        return leaveOne.Value ? Mathf.Max(0, count - 1) : count;
    }

    // The containers in range for a use that the player holds, nearest first: the ones that count and are taken from.
    private static IEnumerable<Container> GetNearby(Use use)
    {
        return InRange(use).Where(ContainerHandoff.Held);
    }

    // The containers in range for a use, nearest first, held or not.
    private static IEnumerable<Container> InRange(Use use)
    {
        float reach = use == Use.Building ? buildRange.Value : range.Value;
        float maxDistance = reach * reach;
        return RefreshNearby().TakeWhile(entry => entry.SqrDistance <= maxDistance).Select(entry => entry.Container);
    }

    // What the player is getting ready to do with the chests: building with a build tool out, crafting with the
    // inventory open, or fuelling or cooking while looking at a station.
    private static Use? UpcomingUse(Player player)
    {
        if (player.InPlaceMode())
        {
            return Use.Building;
        }

        if (InventoryGui.IsVisible())
        {
            return Use.Crafting;
        }

        GameObject hovered = player.GetHoverObject();
        if (hovered == null)
        {
            return null;
        }

        if (hovered.GetComponentInParent<CookingStation>() != null)
        {
            return IsActive(Use.Cooking) ? Use.Cooking : Use.FuelAndOre;
        }

        return hovered.GetComponentInParent<Smelter>() != null || hovered.GetComponentInParent<Fireplace>() != null ? Use.FuelAndOre : null;
    }

    private static void ForgetNearby()
    {
        listTime = float.NegativeInfinity;
        countCache.Clear();
        unlimitedCache.Clear();
    }

    private static void ExpireCounts()
    {
        if (Time.time - countTime > CountLifetime)
        {
            countTime = Time.time;
            countCache.Clear();
            unlimitedCache.Clear();
        }
    }

    // The containers within the larger of both ranges, nearest first, refreshed at most twice a second or when the player moves.
    private static List<(Container Container, float SqrDistance)> RefreshNearby()
    {
        Player player = Player.m_localPlayer;
        Vector3 position = player.transform.position;
        if (Time.time - listTime < ListLifetime && (position - listPosition).sqrMagnitude < ListMoveTolerance * ListMoveTolerance)
        {
            return nearby;
        }

        listTime = Time.time;
        listPosition = position;
        nearby.Clear();
        float reach = Mathf.Max(range.Value, buildRange.Value);
        float maxDistance = reach * reach;
        CollectUsable(position, maxDistance, nearby);
        return nearby;
    }

    /// <summary>
    /// The containers the local player may take from within <paramref name="reach"/> of a point, nearest first.
    /// </summary>
    /// <param name="position">The centre, e.g. a quartermaster's table.</param>
    /// <param name="reach">The range in metres.</param>
    /// <returns>The containers.</returns>
    public static List<Container> Around(Vector3 position, float reach)
    {
        List<(Container Container, float SqrDistance)> found = new();
        CollectUsable(position, reach * reach, found);
        return found.Select(entry => entry.Container).ToList();
    }

    // Adds the containers within range that the local player may use, nearest first.
    private static void CollectUsable(Vector3 position, float maxDistance, List<(Container Container, float SqrDistance)> found)
    {
        all.RemoveWhere(container => container == null);
        long playerId = Game.instance.GetPlayerProfile().GetPlayerID();
        foreach (Container container in all)
        {
            if (container.m_nview == null || !container.m_nview.IsValid() || container.GetInventory() == null)
            {
                continue;
            }

            Vector3 containerPosition = container.transform.position;
            float sqrDistance = (containerPosition - position).sqrMagnitude;
            bool openByOther = !container.m_nview.IsOwner() && container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
            if (sqrDistance > maxDistance
                || openByOther
                || !container.CheckAccess(playerId)
                || (container.m_checkGuardStone && !PrivateArea.CheckAccess(containerPosition, 0f, flash: false))
                || excludedContainerSet.Contains(Utils.GetPrefabName(container.m_nview.gameObject))
                || container.GetComponent<Pieces.Waste.WasteWellComponent>() != null)
            {
                continue;
            }

            found.Add((container, sqrDistance));
        }

        found.Sort((a, b) => a.SqrDistance.CompareTo(b.SqrDistance));
    }

    /// <summary>
    /// Checks whether an item may never be taken from chests (<c>ExcludedItems</c>).
    /// </summary>
    /// <param name="name">Shared item name.</param>
    /// <returns>True if the item is excluded.</returns>
    public static bool IsExcluded(string name)
    {
        if (excludedItemSet == null)
        {
            if (ObjectDB.instance == null)
            {
                return false;
            }

            excludedItemSet = new HashSet<string>(ParseList(excludedItems.Value)
                .Select(prefab => ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name)
                .Where(shared => shared != null));
        }

        return excludedItemSet.Contains(name);
    }

    private static HashSet<string> ParseList(string value)
    {
        return new HashSet<string>(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).Where(part => part.Length > 0));
    }
}
