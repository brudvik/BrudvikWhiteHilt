#nullable enable annotations

using BrudvikWhiteHilt.Chests.Helpers;
using BrudvikWhiteHilt.Chests.Models;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Chests.Piece
{
    /// <summary>
    /// Manages the creation and configuration of custom chests.
    /// </summary>
    public class CustomChestManager
    {
        // Reference to the PieceManager for adding custom pieces
        private readonly PieceManager pieceManager;

        // Reference to the SpriteLoader for loading icons
        private readonly SpriteLoader spriteLoader;

        // The name of the plugin, used for resource identification
        private readonly string pluginName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomChestManager"/> class.
        /// </summary>
        /// <param name="pieceManager">The PieceManager for adding custom pieces.</param>
        /// <param name="spriteLoader">The SpriteLoader for loading icons.</param>
        /// <param name="pluginName">The name of the plugin, used for resource identification.</param>
        public CustomChestManager(PieceManager pieceManager, SpriteLoader spriteLoader, string pluginName)
        {
            this.pieceManager = pieceManager;
            this.spriteLoader = spriteLoader;
            this.pluginName = pluginName;
        }

        /// <summary>
        /// Adds a custom chest piece based on the provided model.
        /// </summary>
        /// <param name="model">The model containing the configuration for the custom chest.</param>
        /// <returns>The created <see cref="CustomPieceExtended"/> object.</returns>
        public CustomPieceExtended AddCustomChest(CustomChestModel model)
        {
            // Create a new configuration for the custom chest piece
            var pieceConfig = new CustomPieceConfigExtended
            {
                Name = model.DisplayName, 
                Description = model.Description, 
                PieceTable = "Hammer", 
                Category = "Chests", 
                PluginName = pluginName, 
                Icon = LoadIcon(model.Icon), 
                Requirements = new[]
                {
                    new RequirementConfig
                    {
                        Item = "Wood", 
                        Amount = 10, 
                        Recover = true 
                    }
                },
                ItemCategory = model.Category
            };

            // Create a new custom piece with the specified name and base piece
            var piece = new CustomPieceExtended(model.Name, "piece_chest", pieceConfig)
            {
                Color = model.Color,
                Icon = model.Icon,
                Tooltip = model.DisplayName,
                Rows = model.Rows,
                Columns = model.Columns
            };

            // Apply the properties to the custom piece
            piece.ApplyProperties();
            if (model.Name.EndsWith("Drawer", System.StringComparison.Ordinal)) WallDrawer.Configure(piece.PiecePrefab);

            // Add the custom piece to the PieceManager
            pieceManager.AddPiece(piece);

            // Log the initiation of the custom chest piece
            Jotunn.Logger.LogInfo($"{model.Name} chest piece is initiated");
            return piece; 
        }

        /// <summary>Registers a wall-mounted variant with the source chest's category and inventory dimensions.</summary>
        /// <param name="source">The original chest descriptor.</param>
        /// <returns>The independent drawer descriptor.</returns>
        public CustomPieceExtended AddWallDrawer(CustomPieceExtended source)
        {
            var drawer = AddCustomChest(new CustomChestModel
            {
                Name = source.PrefabName + "Drawer",
                DisplayName = source.Tooltip + " ($whitehilt_wall_drawer)",
                Description = source.CustomPieceConfig.Description + "\n$whitehilt_wall_drawer_description",
                Icon = source.Icon,
                Color = source.Color,
                Category = source.CustomPieceConfig.ItemCategory,
                Rows = source.Rows,
                Columns = source.Columns
            });
            return drawer;
        }

        // The icons are licensed and not in the public source; a chest must register without them, or placed ones vanish.
        private UnityEngine.Sprite? LoadIcon(string icon)
        {
            try
            {
                return spriteLoader.Load(icon);
            }
            catch (System.Exception ex)
            {
                Jotunn.Logger.LogWarning($"Chest icon {icon} not loaded, using the vanilla chest icon: {ex.Message}");
                return null;
            }
        }
    }
}
