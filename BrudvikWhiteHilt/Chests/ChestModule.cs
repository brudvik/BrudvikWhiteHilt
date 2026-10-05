#nullable enable annotations

using BepInEx.Bootstrap;
using BrudvikWhiteHilt.Chests.Commands;
using BrudvikWhiteHilt.Chests.Configuration;
using BrudvikWhiteHilt.Chests.Constants;
using BrudvikWhiteHilt.Chests.Events;
using BrudvikWhiteHilt.Chests.Extensions;
using BrudvikWhiteHilt.Chests.Helpers;
using BrudvikWhiteHilt.Chests.Models;
using BrudvikWhiteHilt.Patches.Chests;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using BrudvikWhiteHilt.Chests.Piece;
using BrudvikWhiteHilt.Chests.Utils;
using BrudvikWhiteHilt.Progression;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests
{
    /// <summary>
    /// The chests of the former BrudvikStackedChest mod: chests that restock their items automatically. The prefab
    /// names, the ZDO key and the world progress folder are the old mod's, so chests placed with it keep their contents.
    /// </summary>
    internal class ChestModule : global::BrudvikWhiteHilt.Crafting.IUnlimitedItems
    {
        /// <summary>
        /// GUID of the former stand-alone mod. While it is loaded it keeps its own chests and this module stays off.
        /// </summary>
        public const string OldModGuid = "com.jotunn.BrudvikStackedChest";

        // The old mod's name: its world progress folder and RPC prefix.
        private const string LegacyName = "BrudvikStackedChest";

        // Root namespace of the embedded icons.
        private const string ResourceRoot = "BrudvikWhiteHilt";

        // Remembers the mode cargo was last restocked in; the White Hilt Ship's chest shares the hold's ZDO.
        private const string CargoModeKey = "whitehilt_cargo_mode";
        private const string ShipChestModeKey = "whitehilt_cargo_mode_chest";

        /// <summary>
        /// List to store custom pieces (chests) added by the plugin.
        /// </summary>
        private List<CustomPieceExtended> customPieces = new();

        /// <summary>
        /// Manager to handle custom chests, initialized with the PieceManager instance and a SpriteLoader.
        /// </summary>
        private CustomChestManager customChestManager = new(PieceManager.Instance, new SpriteLoader(ResourceRoot), ResourceRoot);

        // Created in Initialize, after the config file is bound.
        private ChestSettings settings = null!;
        private ItemCatalog itemCatalog = null!;
        private WorldProgress worldProgress = null!;
        private ChestSupply chestSupply = null!;
        private ChestProgressUi progressUi = null!;
        private ChestHoverPanel hoverPanel = null!;
        private BiomeCatalog biomeCatalog = null!;
        private ChestLearnUi learnUi = null!;
        private GatheringPanel gatheringPanel = null!;
        private readonly SpriteLoader spriteLoader = new(ResourceRoot);
        private Sprite? gatheringIcon;

        // Inventories do not know their container; the inventory patches need to recognize our chests.
        private readonly ConditionalWeakTable<Inventory, Container> chestInventories = new();

        // Whether a container belongs to a ship or cart; it never changes, and the UI asks every frame.
        private readonly ConditionalWeakTable<Container, StrongBox<bool>> vehicleContainers = new();

        // Containers dropping their items: refilling them now would drop the refills, and Take all must empty them.
        private readonly ConditionalWeakTable<Container, object> destroyedContainers = new();

        /// <summary>
        /// Starts the chests, unless the former stand-alone mod is loaded. Call from the plugin's Awake.
        /// </summary>
        /// <returns>The module, or null while the old mod is loaded.</returns>
        public static ChestModule? Start()
        {
            if (Chainloader.PluginInfos.ContainsKey(OldModGuid))
            {
                Jotunn.Logger.LogWarning("BrudvikStackedChest is installed. Its chests are now part of White Hilt, which leaves them to the old mod " +
                    "while it is loaded. Remove BrudvikStackedChest to switch over, but keep its config file and the BepInEx/config/BrudvikStackedChest folder.");
                return null;
            }

            var module = new ChestModule();
            module.Initialize();
            return module;
        }

        private void Initialize()
        {
            Texts.Register($"{ResourceRoot}.Chests");

            settings = new ChestSettings();
            itemCatalog = new ItemCatalog(settings);
            worldProgress = new WorldProgress(LegacyName);
            chestSupply = new ChestSupply(settings, itemCatalog, worldProgress);
            progressUi = new ChestProgressUi(chestSupply, FindPiece, IsCargo);
            hoverPanel = new ChestHoverPanel(chestSupply, FindPiece, () => settings.ShowHoverPanel.Value);
            biomeCatalog = new BiomeCatalog(settings);
            learnUi = new ChestLearnUi(FindPiece, () => settings.LearnAll.Value, () => settings.LearnTrophies.Value);
            gatheringPanel = new GatheringPanel(itemCatalog, biomeCatalog, chestSupply, worldProgress, GetChestName, GetChestLook, HighlightChests,
                LoadGatheringIcon);

            WhiteHiltConfig.File.SettingChanged += (_, _) => HandleSettingsChanged();
            SynchronizationManager.OnConfigurationSynchronized += (_, _) => HandleSettingsChanged();
            worldProgress.ItemUnlocked += HandleItemUnlockedByOthers;
            learnUi.ItemsLearned += HandleItemsLearned;
            CommandManager.Instance.AddConsoleCommand(new ProgressCommand(chestSupply, progressUi, () => customPieces));

            // Register a callback to add cloned items when prefabs are registered
            PrefabManager.OnPrefabsRegistered += AddClonedItems;

            ContainerPatch.ContainerCheckForChangesPatched += HandleContainerCheckForChanges;
            ContainerPatch.ContainerChangedPatched += HandleContainerChanged;
            InventoryPatch.InventoryAddingItemPatched += HandleInventoryAddingItem;
            InventoryPatch.InventoryMoveAllPatched += HandleInventoryMoveAll;
            ContainerPatch.ContainerDropAllItemsPatched += HandleContainerDropAllItemsPatched;
            ContainerPatch.ContainerHoverTextPatched += progressUi.HandleContainerHoverText;
            InventoryGuiPatch.InventoryGridUpdatedPatched += progressUi.HandleGridUpdated;
            InventoryGuiPatch.ItemTooltipPatched += progressUi.HandleItemTooltip;
            InventoryGuiPatch.ContainerPanelUpdatedPatched += progressUi.HandleContainerPanelUpdated;
            PlayerPatch.PlayerSpawnedPatched += HandlePlayerSpawned;
            PlayerPatch.PlayerKnownItemPatched += HandlePlayerKnownItem;
            InventoryGuiPatch.InventoryGridUpdatedPatched += learnUi.HandleGridUpdated;
            InventoryGuiPatch.ItemTooltipPatched += learnUi.HandleItemTooltip;
            InventoryGuiPatch.ContainerPanelUpdatedPatched += learnUi.HandleContainerPanelUpdated;
            MessageHudPatch.UnlockMessagePatched += learnUi.HandleUnlockMessage;
            InventoryGuiPatch.InventoryShownPatched += gatheringPanel.HandleInventoryShown;
            InventoryGuiPatch.InventoryHiddenPatched += gatheringPanel.HandleClose;
            InventoryGuiPatch.InventoryPanelOpenedPatched += gatheringPanel.HandleClose;
            global::BrudvikWhiteHilt.Crafting.NearbyContainers.Unlimited = this;

            Jotunn.Logger.LogInfo("White Hilt chests have loaded!");
        }

        /// <summary>
        /// Updates the hover and gathering panels. Call every frame from the plugin.
        /// </summary>
        public void Update()
        {
            hoverPanel?.Update();
            gatheringPanel?.Update();
        }

        /// <inheritdoc/>
        public bool IsUnlimitedIn(Container container, ItemDrop.ItemData item)
        {
            if (!chestSupply.IsReady || IsDestroyed(container)) return false;

            var piece = FindPiece(container);
            if (piece != null) return chestSupply.IsSupplied(chestSupply.Mode, piece.CustomPieceConfig.ItemCategory, item);

            return IsCargo(container) && chestSupply.IsCargoSupplied(item);
        }

        /// <inheritdoc/>
        public string? GetUnlimitedChest(string prefabName, ItemDrop.ItemData.SharedData shared)
        {
            if (!chestSupply.IsReady || !chestSupply.IsUnlimited(prefabName, shared)) return null;

            return GetChestName(itemCatalog.GetCategory(prefabName));
        }

        /// <inheritdoc/>
        public bool TryGetUnlockProgress(string prefabName, ItemDrop.ItemData.SharedData shared, out int stored, out int required)
        {
            stored = 0;
            required = 0;
            if (chestSupply.Mode != ChestMode.Linear || !chestSupply.IsReady) return false;

            var category = itemCatalog.GetCategory(prefabName);
            if (category == ChestCategory.None || !chestSupply.CanUnlock(category, prefabName, shared)) return false;

            required = chestSupply.GetUnlockAmount(shared);
            stored = Math.Min(worldProgress.GetBestStored(prefabName), required);
            return required > 0;
        }

        internal bool IsCollectionChest(Container container) => !IsDestroyed(container) && FindPiece(container) != null;

        // The name of the White Hilt chest a container is, or null for any other container.
        internal string? GetChestLabel(Container container)
        {
            var piece = FindPiece(container);
            if (piece == null) return null;
            var category = piece.CustomPieceConfig.ItemCategory;
            return category == ChestCategory.None ? Texts.Localize(piece.Tooltip) : GetChestName(category);
        }

        internal IReadOnlyList<string> GetGatherableItems(Heightmap.Biome biome) => biomeCatalog.GetItems(biome);

        internal int CollectionPriority(Container container, ItemDrop.ItemData item)
        {
            if (!chestSupply.IsReady || IsDestroyed(container) || item.m_dropPrefab == null) return -1;
            var piece = FindPiece(container);
            if (piece == null) return -1;
            var category = piece.CustomPieceConfig.ItemCategory;
            if (category == ChestCategory.None) return 1;
            return itemCatalog.Contains(category, item.m_dropPrefab.name) ? 0 : -1;
        }

        // Whether the chest would take the whole stack without using a slot, as it already holds the item without limit.
        internal bool AbsorbsCollected(Container container, ItemDrop.ItemData item)
        {
            if (CollectionPriority(container, item) < 0 || container.GetInventory() == null) return false;
            return chestSupply.AbsorbsDeposit(FindPiece(container).CustomPieceConfig.ItemCategory, container.GetInventory(), item);
        }

        internal int DepositCollected(Container container, ItemDrop.ItemData source)
        {
            if (CollectionPriority(container, source) < 0 || container.m_nview == null || !container.m_nview.IsValid()
                || !container.m_nview.IsOwner() || container.IsInUse()) return 0;

            var piece = FindPiece(container);
            var category = piece.CustomPieceConfig.ItemCategory;
            container.Load();
            RegisterChest(container);
            container.Restock(category, chestSupply, settings.SortContents.Value);
            var inventory = container.GetInventory();
            if (chestSupply.AbsorbsDeposit(category, inventory, source)) return source.m_stack;

            var incoming = source.Clone();
            var amount = incoming.m_stack;
            if (ChestSupply.IsPlain(incoming) && ChestSupply.IsStackable(incoming.m_shared))
            {
                foreach (var stored in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
                {
                    if (incoming.m_stack <= 0) break;
                    if (!ChestSupply.IsPlain(stored) || !stored.IsSameType(incoming) || stored.m_quality != incoming.m_quality
                        || stored.m_variant != incoming.m_variant || stored.m_worldLevel != incoming.m_worldLevel
                        || stored.m_crafterID != incoming.m_crafterID || stored.m_crafterName != incoming.m_crafterName
                        || stored.m_durability != incoming.m_durability || stored.m_cheated != incoming.m_cheated) continue;
                    var space = stored.m_shared.m_maxStackSize - stored.m_stack;
                    if (space > 0) inventory.AddItem(incoming, Math.Min(space, incoming.m_stack), stored.m_gridPos.x, stored.m_gridPos.y);
                }
            }

            while (incoming.m_stack > 0)
            {
                var slot = inventory.FindEmptySlot(true);
                if (slot.x < 0) break;
                inventory.AddItem(incoming, Math.Min(incoming.m_stack, incoming.m_shared.m_maxStackSize), slot.x, slot.y);
            }

            var accepted = amount - incoming.m_stack;
            if (accepted > 0)
            {
                container.Restock(category, chestSupply, settings.SortContents.Value);
                container.Save();
                ReportStock(container, category);
            }
            return accepted;
        }

        private void HandleSettingsChanged()
        {
            itemCatalog.Invalidate();
            RegisterDiscoveries(Player.m_localPlayer);
        }

        /// <summary>
        /// Tells every player when someone else in the world makes an item unlimited. The player who did it already
        /// got a message in the middle of the screen.
        /// </summary>
        private void HandleItemUnlockedByOthers(string key)
        {
            if (Player.m_localPlayer == null) return;

            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Texts.Get("bsc_msg_unlimited", chestSupply.GetDisplayName(key)));
        }

        /// <summary>
        /// Before a chest drops its contents, removes the stacks it supplies without limit, so only items players
        /// stored themselves are dropped.
        /// </summary>
        private void HandleContainerDropAllItemsPatched(object sender, ContainerDropAllItemsPatchEvent e)
        {
            if (e?.Container == null) return;

            destroyedContainers.GetValue(e.Container, _ => new object());
            var piece = FindPiece(e.Container);
            if (piece == null)
            {
                if (IsCargo(e.Container)) e.Container.RemoveSuppliedItems(ChestCategory.None, chestSupply);
                return;
            }

            e.Container.RemoveSuppliedItems(piece.CustomPieceConfig.ItemCategory, chestSupply);

            var chestId = e.Container.GetChestId();
            if (chestId != null && e.Container.IsOwnedByMe()) worldProgress.ReportStock(chestId, new Dictionary<string, int>());
        }

        /// <summary>
        /// Tells the server how close the chest is to unlocking each of its items, for the gathering panel.
        /// </summary>
        private void ReportStock(Container container, ChestCategory category)
        {
            if (settings.Mode.Value != ChestMode.Linear || !chestSupply.IsReady || !container.IsOwnedByMe()) return;

            var inventory = container.GetInventory();
            var chestId = container.GetChestId();
            if (inventory == null || chestId == null) return;

            worldProgress.ReportStock(chestId, chestSupply.GetUnlockableAmounts(category, inventory));
        }

        /// <summary>
        /// Shares items learned from a chest as discovered in Discovered mode, like items the player picks up.
        /// </summary>
        private void HandleItemsLearned(List<string> itemTokens)
        {
            if (settings.Mode.Value == ChestMode.Discovered) worldProgress.Discover(itemTokens);
        }

        private string? GetChestName(ChestCategory category)
        {
            if (category == ChestCategory.None) return null;

            var piece = customPieces.Find(candidate => candidate.CustomPieceConfig.ItemCategory == category);
            return piece == null ? null : Texts.Localize(piece.Tooltip);
        }

        // The chest's own build icon (its category sign) and the colour it glows in, for the gathering panel.
        private (Sprite? Icon, Color Color) GetChestLook(ChestCategory category)
        {
            var piece = customPieces.Find(candidate => candidate.CustomPieceConfig.ItemCategory == category);
            return piece == null ? (null, Color.white) : (piece.Piece?.m_icon, ChestEffects.GetGlowColor(piece.Color));
        }

        /// <summary>
        /// Makes the loaded chests and wall drawers of the given categories near the local player light up for a while.
        /// </summary>
        /// <returns>How many light up.</returns>
        private int HighlightChests(IReadOnlyCollection<ChestCategory> categories)
        {
            var player = Player.m_localPlayer;
            if (player == null || categories.Count == 0) return 0;

            var count = 0;
            var range = ChestHighlight.Range * ChestHighlight.Range;
            foreach (var container in global::BrudvikWhiteHilt.Crafting.NearbyContainers.Registered())
            {
                var piece = FindPiece(container);
                if (piece == null || IsDestroyed(container) || !categories.Contains(piece.CustomPieceConfig.ItemCategory)
                    || (container.transform.position - player.transform.position).sqrMagnitude > range) continue;

                ChestHighlight.Flash(container, ChestEffects.GetGlowColor(piece.Color));
                count++;
            }
            return count;
        }

        private Sprite? LoadGatheringIcon()
        {
            try
            {
                if (gatheringIcon == null) gatheringIcon = spriteLoader.Load("strg_081_round.png");
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Could not load the gathering panel icon: {ex.Message}");
            }
            return gatheringIcon;
        }

        /// <summary>
        /// Handles the ContainerCheckForChangesPatched event.
        /// Keeps the unlimited stacks in one of our chests full and adds the unlimited items that are missing.
        /// </summary>
        private void HandleContainerCheckForChanges(object sender, ContainerCheckForChangesPatchEvent e)
        {
            if (IsDestroyed(e?.Container)) return;

            var piece = FindPiece(e?.Container);
            if (piece == null)
            {
                RestockCargo(e?.Container);
                return;
            }

            RegisterChest(e!.Container);
            ChestEffects.UpdateGlow(e.Container, piece.Color, piece.CustomPieceConfig.ItemCategory, chestSupply);
            e.Container.Restock(piece.CustomPieceConfig.ItemCategory, chestSupply, settings.SortContents.Value);
            UpdateIndicator(e.Container, piece);
            ReportStock(e.Container, piece.CustomPieceConfig.ItemCategory);
        }

        /// <summary>
        /// Restocks one of our chests as soon as its contents change, so building several pieces in a row from a
        /// chest does not run it dry before the next periodic check.
        /// </summary>
        private void HandleContainerChanged(object sender, ContainerChangedPatchEvent e)
        {
            if (IsDestroyed(e?.Container)) return;

            var piece = FindPiece(e?.Container);
            if (piece == null)
            {
                RestockCargo(e?.Container);
                return;
            }

            RegisterChest(e!.Container);
            e.Container.Restock(piece.CustomPieceConfig.ItemCategory, chestSupply, settings.SortContents.Value);
            UpdateIndicator(e.Container, piece);
        }

        /// <summary>
        /// Keeps the stacks of unlimited items in a cart or ship hold full, so cargo brought along works like an
        /// Everlasting Chest wherever the cart or ship goes.
        /// </summary>
        private void RestockCargo(Container? container)
        {
            if (!IsCargo(container)) return;

            RegisterChest(container!);
            container!.RestockCargo(chestSupply, container.GetComponent<ShipChest>() != null ? ShipChestModeKey : CargoModeKey);
        }

        private bool IsDestroyed(Container? container)
        {
            return container != null && destroyedContainers.TryGetValue(container, out _);
        }

        private bool IsCargo(Container? container)
        {
            if (container == null || !settings.UnlimitedCargo.Value) return false;

            return vehicleContainers.GetValue(container, held => new StrongBox<bool>(
                held.GetComponentInParent<Ship>() != null || held.GetComponentInParent<Vagon>() != null)).Value;
        }

        private void UpdateIndicator(Container container, CustomPieceExtended piece)
        {
            var inventory = container.GetInventory();
            if (inventory == null || (ZNet.instance != null && ZNet.instance.IsDedicated())) return;

            if (!container.TryGetComponent(out ChestIndicator indicator)) indicator = container.gameObject.AddComponent<ChestIndicator>();
            indicator.Refresh(inventory, piece.CustomPieceConfig.ItemCategory, chestSupply, settings.ShowIndicators.Value);
        }

        /// <summary>
        /// Lets an item put into one of our chests disappear when the chest already holds it without limit, instead
        /// of forming yet another stack.
        /// </summary>
        private void HandleInventoryAddingItem(object sender, InventoryAddingItemPatchEvent e)
        {
            if (!TryGetChest(e.Inventory, out _, out var category)) return;

            e.Absorb = chestSupply.AbsorbsDeposit(category, e.Inventory, e.Item);
        }

        /// <summary>
        /// Makes Take all on one of our chests take only the items players stored themselves.
        /// </summary>
        private void HandleInventoryMoveAll(object sender, InventoryMoveAllPatchEvent e)
        {
            if (!TryGetChest(e.FromInventory, out var container, out var category)) return;

            e.Handled = true;

            // Until the world progress is known, unlimited stacks cannot be told apart from stored items.
            if (chestSupply.IsReady) container.MoveStoredItemsTo(e.Inventory, category, chestSupply);
        }

        private void RegisterChest(Container container)
        {
            var inventory = container.GetInventory();
            if (inventory != null) chestInventories.GetValue(inventory, _ => container);
        }

        private bool TryGetChest(Inventory inventory, out Container container, out ChestCategory category)
        {
            category = ChestCategory.None;
            if (!chestInventories.TryGetValue(inventory, out container) || container == null || IsDestroyed(container)) return false;

            var piece = FindPiece(container);
            if (piece == null) return IsCargo(container);

            category = piece.CustomPieceConfig.ItemCategory;
            return true;
        }

        private void HandlePlayerSpawned(object sender, PlayerSpawnedPatchEvent e)
        {
            worldProgress.RequestSync();
            RegisterDiscoveries(e.Player);
        }

        private void HandlePlayerKnownItem(object sender, PlayerKnownItemPatchEvent e)
        {
            if (settings.Mode.Value == ChestMode.Discovered) worldProgress.Discover(new[] { e.ItemToken });
        }

        /// <summary>
        /// Shares everything the player already knows, so items discovered before joining or before the mode was
        /// switched count as well.
        /// </summary>
        private void RegisterDiscoveries(Player? player)
        {
            if (player == null || settings.Mode.Value != ChestMode.Discovered) return;

            worldProgress.Discover(player.m_knownMaterial);
        }

        private CustomPieceExtended? FindPiece(Container? container)
        {
            if (container == null || container.name == null) return null;

            foreach (var piece in customPieces)
            {
                if (global::Utils.GetPrefabName(container.gameObject) == piece.PrefabName) return piece;
            }
            return null;
        }

        /// <summary>
        /// Adds cloned items to the custom pieces list.
        /// This method creates a new custom chest model for various prefabs and adds it to the custom pieces list.
        /// </summary>
        private void AddClonedItems()
        {
            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSWoodChest",
                    DisplayName = "$bsc_chest_wood",
                    Description = "$bsc_chest_wood_desc",
                    Icon = "strg_049_round.png",
                    Color = SharedUtils.ColorFromRGB(0, 0, 0, 0.8f),
                    Category = ChestCategory.Wood
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSStoneChest",
                    DisplayName = "$bsc_chest_stone",
                    Description = "$bsc_chest_stone_desc",
                    Icon = "strg_009_round.png",
                    Color = SharedUtils.ColorFromRGB(135, 135, 135, 0.8f),
                    Category = ChestCategory.Stone
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSMetalChest",
                    DisplayName = "$bsc_chest_metal",
                    Description = "$bsc_chest_metal_desc",
                    Icon = "strg_082_round.png",
                    Color = SharedUtils.ColorFromRGB(163, 34, 24, 0.8f),
                    Category = ChestCategory.Metal
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSFoodChest",
                    DisplayName = "$bsc_chest_food",
                    Description = "$bsc_chest_food_desc",
                    Icon = "strg_046_round.png",
                    Color = SharedUtils.ColorFromRGB(112, 81, 44, 0.8f),
                    Rows = 10,
                    Category = ChestCategory.Food
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSMaterialChest",
                    DisplayName = "$bsc_chest_material",
                    Description = "$bsc_chest_material_desc",
                    Icon = "strg_088_round.png",
                    Color = SharedUtils.ColorFromRGB(44, 47, 112, 0.8f),
                    Category = ChestCategory.Material
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSAnimalChest",
                    DisplayName = "$bsc_chest_animal",
                    Description = "$bsc_chest_animal_desc",
                    Icon = "strg_012_round.png",
                    Color = SharedUtils.ColorFromRGB(181, 178, 27, 0.8f),
                    Category = ChestCategory.Animal
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSSeedChest",
                    DisplayName = "$bsc_chest_seed",
                    Description = "$bsc_chest_seed_desc",
                    Icon = "strg_029_round.png",
                    Color = SharedUtils.ColorFromRGB(27, 181, 89, 0.8f),
                    Category = ChestCategory.Seed
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSTrophyChest",
                    DisplayName = "$bsc_chest_trophy",
                    Description = "$bsc_chest_trophy_desc",
                    Icon = "strg_091_round.png",
                    Color = SharedUtils.ColorFromRGB(21, 122, 117, 0.8f),
                    Category = ChestCategory.Trophy
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSTreasureChest",
                    DisplayName = "$bsc_chest_treasure",
                    Description = "$bsc_chest_treasure_desc",
                    Icon = "strg_098_round.png",
                    Color = SharedUtils.ColorFromRGB(228, 237, 95, 0.8f),
                    Category = ChestCategory.Treasure
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSToolsChest",
                    DisplayName = "$bsc_chest_tools",
                    Description = "$bsc_chest_tools_desc",
                    Icon = "strg_039_round.png",
                    Color = SharedUtils.ColorFromRGB(51, 21, 122, 0.8f),
                    Category = ChestCategory.Tools
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSArmorChest",
                    DisplayName = "$bsc_chest_armor",
                    Description = "$bsc_chest_armor_desc",
                    Icon = "strg_014_round.png",
                    Color = SharedUtils.ColorFromRGB(120, 80, 40, 0.8f),
                    Category = ChestCategory.Armor
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSWeaponChest",
                    DisplayName = "$bsc_chest_weapon",
                    Description = "$bsc_chest_weapon_desc",
                    Icon = "strg_032_round.png",
                    Color = SharedUtils.ColorFromRGB(180, 50, 50, 0.8f),
                    Rows = 10,
                    Columns = 8,
                    Category = ChestCategory.Weapon
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSPotionChest",
                    DisplayName = "$bsc_chest_potion",
                    Description = "$bsc_chest_potion_desc",
                    Icon = "strg_004_round.png",
                    Color = SharedUtils.ColorFromRGB(200, 100, 200, 0.8f),
                    Category = ChestCategory.Potion
                }
            ));

            customPieces.Add(customChestManager.AddCustomChest(
                new CustomChestModel()
                {
                    Name = "BSEmptyChest",
                    DisplayName = "$bsc_chest_empty",
                    Description = "$bsc_chest_empty_desc",
                    Icon = "strg_010_round.png",
                    Color = SharedUtils.ColorFromRGB(45, 45, 79, 0.8f)
                }
            ));

            global::BrudvikWhiteHilt.Helpers.Translations.AddEnglish("whitehilt_wall_drawer", "Wall drawer");
            global::BrudvikWhiteHilt.Helpers.Translations.AddEnglish("whitehilt_wall_drawer_description", "A compact drawer for a player-built wooden or stone wall. Shares the corresponding chest's storage rules.");
            foreach (var source in customPieces.ToArray())
            {
                try
                {
                    customPieces.Add(customChestManager.AddWallDrawer(source));
                }
                catch (Exception error)
                {
                    Jotunn.Logger.LogError($"Wall drawer for {source.PrefabName} failed: {error}");
                }
            }

            // Unregister the callback to prevent duplicate items
            PrefabManager.OnPrefabsRegistered -= AddClonedItems;
        }

    }
}

