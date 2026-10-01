using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Progression;

/// <summary>
/// Enables, disables and prices White Hilt recipes according to the selected progression mode.
/// Recipes are gated instead of unregistering items, so switching mode never deletes items players already own.
/// </summary>
public static class ProgressionManager
{
    // Hud.m_requirementItems in the game's build HUD; Hud.SetupPieceInfo throws on a piece that needs more.
    private const int HudRequirementSlots = 6;

    private static readonly (ProgressionTier Tier, string Material, int Amount)[] tierMaterials =
    {
        (ProgressionTier.BlackForest, "Bronze", 5),
        (ProgressionTier.Swamp, "Iron", 5),
        (ProgressionTier.Mountain, "Silver", 5),
        (ProgressionTier.Plains, "BlackMetal", 5),
        (ProgressionTier.Mistlands, "Eitr", 3),
        (ProgressionTier.Ashlands, "FlametalNew", 3)
    };

    private static readonly Dictionary<string, IWhiteHiltProgressionEntry> itemEntries = new();
    private static readonly Dictionary<string, IWhiteHiltProgressionEntry> pieceEntries = new();
    private static readonly Dictionary<string, Piece.Requirement[]> originalRequirements = new();
    private static readonly Dictionary<string, (string Text, Piece.Requirement[] Requirements)> configuredRequirements = new();
    private static readonly Dictionary<string, (ProgressionTier Tier, Piece.Requirement[] Base, Piece.Requirement[] Requirements)> linearRequirements = new();
    private static Player trackedPlayer;
    private static ProgressionTier trackedTier;

    /// <summary>
    /// Set while the player is discovering a new item, so tier unlocks are announced only when they actually happen.
    /// </summary>
    public static bool AnnounceUnlocks { get; set; }

    /// <summary>
    /// Registers the English unlock messages. Must run in Awake, before Valheim loads its languages.
    /// </summary>
    public static void RegisterTranslations()
    {
        foreach (ProgressionTier tier in System.Enum.GetValues(typeof(ProgressionTier)))
        {
            Translations.AddEnglish(GetUnlockKey(tier), $"The White Hilt answers: {GetTierDisplayName(tier)} gear unlocked");
        }
    }

    /// <summary>
    /// Registers an item whose crafting recipe is gated.
    /// </summary>
    /// <param name="entry">The item.</param>
    public static void RegisterItem(IWhiteHiltProgressionEntry entry)
    {
        itemEntries[entry.GatedPrefabName] = entry;
        WhiteHiltConfig.BindEntry(entry);
    }

    /// <summary>
    /// Registers a piece whose build recipe is gated.
    /// </summary>
    /// <param name="entry">The piece.</param>
    public static void RegisterPiece(IWhiteHiltProgressionEntry entry)
    {
        pieceEntries[entry.GatedPrefabName] = entry;
        WhiteHiltConfig.BindEntry(entry);
    }

    /// <summary>
    /// Re-evaluates the local player's recipes, e.g. after a config change.
    /// </summary>
    public static void Refresh()
    {
        if (Player.m_localPlayer != null)
        {
            Player.m_localPlayer.UpdateKnownRecipesList();
        }
    }

    /// <summary>
    /// Applies the progression rules to all White Hilt recipes and pieces for the given player.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <returns>True if the enabled state of any piece changed.</returns>
    public static bool Apply(Player player)
    {
        if (ObjectDB.instance == null)
        {
            return false;
        }

        bool linear = WhiteHiltConfig.Mode.Value == ProgressionMode.Linear;
        ProgressionTier unlockedTier = GetUnlockedTier(player);
        AnnounceNewTier(player, unlockedTier, linear);

        foreach (Recipe recipe in ObjectDB.instance.m_recipes)
        {
            if (recipe == null || recipe.m_item == null || !itemEntries.TryGetValue(recipe.m_item.name, out var entry))
            {
                continue;
            }

            ProgressionTier? tier = ResolveTier(entry, linear);
            recipe.m_enabled = tier.HasValue && tier.Value <= unlockedTier;
            recipe.m_resources = GetRequirements(entry, recipe.m_resources, linear ? tier : null, int.MaxValue);
        }

        bool piecesChanged = false;
        foreach (var pair in pieceEntries)
        {
            Piece piece = ZNetScene.instance?.GetPrefab(pair.Key)?.GetComponent<Piece>();
            if (piece == null)
            {
                continue;
            }

            ProgressionTier? tier = ResolveTier(pair.Value, linear);
            bool enabled = tier.HasValue && tier.Value <= unlockedTier;
            piecesChanged |= piece.m_enabled != enabled;
            piece.m_enabled = enabled;
            // The build HUD shows 6 slots, and the crafting station takes one of them.
            int room = HudRequirementSlots - (piece.m_craftingStation != null ? 1 : 0);
            piece.m_resources = GetRequirements(pair.Value, piece.m_resources, linear ? tier : null, room);
        }

        return piecesChanged;
    }

    private static ProgressionTier? ResolveTier(IWhiteHiltProgressionEntry entry, bool linear)
    {
        TierOverride tierOverride = WhiteHiltConfig.GetTierOverride(entry.Id);
        if (tierOverride == TierOverride.Never || !WhiteHiltConfig.IsEnabled(entry.Id))
        {
            return null;
        }

        if (!linear)
        {
            return ProgressionTier.Start;
        }

        return tierOverride switch
        {
            TierOverride.Start => ProgressionTier.Start,
            TierOverride.BlackForest => ProgressionTier.BlackForest,
            TierOverride.Swamp => ProgressionTier.Swamp,
            TierOverride.Mountain => ProgressionTier.Mountain,
            TierOverride.Plains => ProgressionTier.Plains,
            TierOverride.Mistlands => ProgressionTier.Mistlands,
            TierOverride.Ashlands => ProgressionTier.Ashlands,
            _ => entry.DefaultTier
        };
    }

    private static ProgressionTier GetUnlockedTier(Player player)
    {
        // Tiers are cumulative: knowing a later material also unlocks every earlier tier.
        for (int i = tierMaterials.Length - 1; i >= 0; i--)
        {
            ItemDrop material = GetItemDrop(tierMaterials[i].Material);
            if (material != null && player.IsMaterialKnown(material.m_itemData.m_shared.m_name))
            {
                return tierMaterials[i].Tier;
            }
        }

        return ProgressionTier.Start;
    }

    private static Piece.Requirement[] GetRequirements(IWhiteHiltProgressionEntry entry, Piece.Requirement[] current, ProgressionTier? linearTier, int maxRequirements)
    {
        string prefabName = entry.GatedPrefabName;
        if (!originalRequirements.TryGetValue(prefabName, out var builtIn))
        {
            builtIn = current ?? new Piece.Requirement[0];
            originalRequirements[prefabName] = builtIn;
        }

        Piece.Requirement[] original = GetConfiguredRequirements(entry, builtIn, maxRequirements);
        if (!linearTier.HasValue)
        {
            return original;
        }

        if (linearRequirements.TryGetValue(prefabName, out var cached) && cached.Tier == linearTier.Value && cached.Base == original)
        {
            return cached.Requirements;
        }

        Piece.Requirement[] requirements = original;
        foreach (var tierMaterial in tierMaterials)
        {
            if (tierMaterial.Tier != linearTier.Value)
            {
                continue;
            }

            ItemDrop material = GetItemDrop(tierMaterial.Material);
            bool alreadyRequired = original.Any(requirement => requirement.m_resItem != null && requirement.m_resItem.name == tierMaterial.Material);
            if (material != null && !alreadyRequired && original.Length >= maxRequirements)
            {
                Jotunn.Logger.LogWarning($"{prefabName}: no room for the tier cost {tierMaterial.Material}, the build menu shows at most {maxRequirements} requirements");
            }
            else if (material != null && !alreadyRequired)
            {
                requirements = original
                    .Append(new Piece.Requirement { m_resItem = material, m_amount = tierMaterial.Amount, m_recover = false })
                    .ToArray();
            }
        }

        linearRequirements[prefabName] = (linearTier.Value, original, requirements);
        return requirements;
    }

    private static Piece.Requirement[] GetConfiguredRequirements(IWhiteHiltProgressionEntry entry, Piece.Requirement[] builtIn, int maxRequirements)
    {
        string text = WhiteHiltConfig.GetRecipeOverride(entry.Id);
        if (text.Length == 0)
        {
            return builtIn;
        }

        if (configuredRequirements.TryGetValue(entry.GatedPrefabName, out var cached) && cached.Text == text)
        {
            return cached.Requirements;
        }

        List<Piece.Requirement> requirements = new();
        foreach (string part in text.Split(','))
        {
            string[] fields = part.Split(':');
            string name = fields[0].Trim();
            if (name.Length == 0)
            {
                continue;
            }

            ItemDrop material = GetItemDrop(name);
            int amountPerLevel = 0;
            if (material == null || fields.Length < 2 || fields.Length > 3
                || !int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount < 0
                || (fields.Length == 3 && !int.TryParse(fields[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out amountPerLevel)))
            {
                Jotunn.Logger.LogWarning($"{entry.Id}: skipping \"{part.Trim()}\" in the recipe config");
                continue;
            }

            requirements.Add(new Piece.Requirement { m_resItem = material, m_amount = amount, m_amountPerLevel = amountPerLevel, m_recover = true });
        }

        if (requirements.Count > maxRequirements)
        {
            Jotunn.Logger.LogWarning($"{entry.Id}: the build menu shows at most {maxRequirements} requirements, the rest of the recipe config is left out");
            requirements = requirements.Take(maxRequirements).ToList();
        }

        // A config with nothing usable would make it free, so the built-in recipe stays.
        Piece.Requirement[] result = requirements.Count > 0 ? requirements.ToArray() : builtIn;
        configuredRequirements[entry.GatedPrefabName] = (text, result);
        return result;
    }

    private static void AnnounceNewTier(Player player, ProgressionTier unlockedTier, bool linear)
    {
        // The first evaluation per player is a baseline, so loading a character never announces old unlocks.
        if (trackedPlayer != player)
        {
            trackedPlayer = player;
            trackedTier = unlockedTier;
            return;
        }

        if (linear && AnnounceUnlocks && unlockedTier > trackedTier && WhiteHiltConfig.ShowUnlockMessages.Value)
        {
            ProgressionTier previousTier = trackedTier;
            List<string> unlockedNames = itemEntries.Values
                .Concat(pieceEntries.Values)
                .Where(entry => ResolveTier(entry, linear: true) is ProgressionTier tier && tier > previousTier && tier <= unlockedTier)
                .OrderBy(entry => entry.DisplayName)
                .Select(entry => entry.NameToken)
                .ToList();

            string message = Translations.Token(GetUnlockKey(unlockedTier));
            if (unlockedNames.Count > 0)
            {
                message += "\n" + string.Join(", ", unlockedNames);
            }

            player.Message(MessageHud.MessageType.Center, message);
        }

        trackedTier = unlockedTier;
    }

    private static ItemDrop GetItemDrop(string prefabName)
    {
        return ObjectDB.instance?.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>();
    }

    private static string GetUnlockKey(ProgressionTier tier)
    {
        return $"whitehilt_unlock_{tier.ToString().ToLowerInvariant()}";
    }

    private static string GetTierDisplayName(ProgressionTier tier)
    {
        return tier switch
        {
            ProgressionTier.BlackForest => "Black Forest",
            _ => tier.ToString()
        };
    }
}
