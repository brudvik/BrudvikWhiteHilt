using BepInEx.Configuration;
using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Pieces.Navigation;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Navigation;

/// <summary>
/// The Pathfinder's Amulet, a trinket: a valknut pendant worn on the chest. It widens the circle of map the wearer
/// uncovers, and uncovering new land fills its adrenaline; at full adrenaline the ravens reveal 500 m around
/// (<see cref="RavenSightEffect"/>). One gem lights up on it per twenty levels of Exploration.
/// <see cref="RubyPathfinderAmulet"/> is the same amulet with a ruby and the target tools.
/// </summary>
public class PathfinderAmulet : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    /// <summary>
    /// Prefab name of the amulet.
    /// </summary>
    public const string PrefabName = "WhiteHiltPathfinder";

    private const string Description = "A valknut pendant on a leather cord, set with a gem for every twenty levels of Exploration. Worn as a trinket, the map uncovers further around you, up to {0} m. Uncovering new land fills its adrenaline; when it is full, Odin's ravens show you the land for {1} m around.";
    private const string CopyFrom = "TrinketBronzeHealth";
    private const string Section = "Gear.WhiteHiltPathfinder";

    // The chest bone; VisEquipment puts a child named attach_<bone> of the item on that bone.
    private const string WornName = "attach_Spine2";
    private const string GroundName = "WhiteHiltAmuletModel";
    private const float GroundScale = 2f;

    // Shared names of every Pathfinder amulet; all of them widen the map and fill the adrenaline.
    private static readonly HashSet<string> amuletNames = new();
    private static ConfigEntry<float> maxAdrenaline;
    private static ConfigEntry<float> adrenalinePerSquareMetre;
    private static RavenSightEffect ravenSight;

    private readonly ItemManager instance;
    private ItemDrop.ItemData.SharedData shared;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public virtual ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    public virtual string Id => PrefabName;

    /// <inheritdoc/>
    public virtual string DisplayName => "Pathfinder's Amulet";

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(Id));

    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>English description; {0} is the explore radius and {1} the Raven Sight radius.</summary>
    protected virtual string EnglishDescription => Description;

    /// <summary>Translation key of the worn status effect.</summary>
    protected virtual string EffectKey => "se_whitehiltpathfinder";

    /// <summary>English name of the worn status effect.</summary>
    protected virtual string EffectName => "Pathfinder";

    /// <summary>English tooltip of the worn status effect.</summary>
    protected virtual string EffectTooltip => "The map uncovers further around you. New land fills your adrenaline.";

    /// <summary>Layout in defenses.json the worn and dropped model is built from.</summary>
    protected virtual string Layout => "stifinner";

    /// <summary>The recipe at the Cartographer's Desk.</summary>
    protected virtual RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Bronze", Amount = 3 },
        new() { Item = "SilverNecklace", Amount = 1 },
        new() { Item = "Ruby", Amount = 1 }
    };

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public PathfinderAmulet(ItemManager instance)
    {
        this.instance = instance;
        if (maxAdrenaline == null)
        {
            maxAdrenaline = WhiteHiltConfig.BindAdminOnly(Section, "MaxAdrenaline", 50f,
                "Adrenaline needed before the ravens reveal the land.", new AcceptableValueRange<float>(1f, 1000f));
            // The default fills it in about 2.5 minutes of sailing along new coast.
            adrenalinePerSquareMetre = WhiteHiltConfig.BindAdminOnly(Section, "AdrenalinePerSquareMetre", 0.0001f,
                "Adrenaline gained per square metre of newly uncovered map.", new AcceptableValueRange<float>(0f, 0.01f));
            RavenSightEffect.RegisterEnglish();
        }

        Translations.AddEnglishNameAndDescription(Translations.ItemKey(Id), DisplayName, EnglishDescription);
        Translations.AddEnglish(EffectKey, EffectName);
        Translations.AddEnglish($"{EffectKey}_tooltip", EffectTooltip);
        amuletNames.Add(NameToken);
        UtilitySlots.AllowInExtraSlots(NameToken);
    }

    /// <summary>
    /// Returns true if the player wears a Pathfinder amulet, as trinket or in an extra accessory slot.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True while one is worn.</returns>
    public static bool IsWorn(Player player)
    {
        return IsWornAsTrinket(player) || (player != null && amuletNames.Any(name => UtilitySlots.IsWornExtra(player, name)));
    }

    /// <summary>
    /// Returns true if the player wears a Pathfinder amulet as trinket; only then does it fill the adrenaline.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True while one is the worn trinket.</returns>
    public static bool IsWornAsTrinket(Player player)
    {
        return player != null && player.m_trinketItem != null && amuletNames.Contains(player.m_trinketItem.m_shared.m_name);
    }

    /// <summary>
    /// Fills the wearer's adrenaline for newly uncovered map.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="squareMetres">Area of the newly uncovered map.</param>
    public static void OnExplored(Player player, float squareMetres)
    {
        if (IsWornAsTrinket(player))
        {
            player.AddAdrenaline(squareMetres * adrenalinePerSquareMetre.Value);
        }
    }

    /// <summary>
    /// Adds the amulet and its recipe at the Cartographer's Desk.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem amulet = new(Id, CopyFrom, new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(Id)}_description"),
                CraftingStation = CartographerDesk.StationPrefabName,
                Requirements = Requirements
            });

            ItemDrop.ItemData.SharedData shared = amulet.ItemDrop.m_itemData.m_shared;
            SE_Stats equipEffect = ScriptableObject.CreateInstance<SE_Stats>();
            equipEffect.name = $"SE_{Id}";
            equipEffect.m_name = Translations.Token(EffectKey);
            equipEffect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
            bool newRavenSight = ravenSight == null;
            ravenSight ??= RavenSightEffect.Create();

            shared.m_equipStatusEffect = equipEffect;
            shared.m_fullAdrenalineSE = ravenSight;
            this.shared = shared;
            ApplyConfig();

            Sprite icon = TryApplyVisual(amulet);
            equipEffect.m_icon = icon ?? shared.m_icons.FirstOrDefault();

            instance.AddStatusEffect(new CustomStatusEffect(equipEffect, fixReference: false));
            if (newRavenSight)
            {
                ravenSight.m_icon = equipEffect.m_icon;
                instance.AddStatusEffect(new CustomStatusEffect(ravenSight, fixReference: false));
            }

            instance.AddItem(amulet);
            Jotunn.Logger.LogInfo($"{DisplayName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{DisplayName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies the configured adrenaline maximum.
    /// </summary>
    public void ApplyConfig()
    {
        if (shared != null)
        {
            shared.m_maxAdrenaline = maxAdrenaline.Value;
        }
    }

    // Replaces the vanilla trinket with the amulet: worn on the chest bone, and larger on the ground and in the icon.
    private Sprite TryApplyVisual(CustomItem amulet)
    {
        if (VisualHelper.IsHeadless)
        {
            return null;
        }

        try
        {
            Transform root = amulet.ItemPrefab.transform;
            // The vanilla trinket is worn through attach_skin and lies on the ground as attach; its sparkle stays.
            foreach (Transform child in root.Cast<Transform>().Where(child => child.name.StartsWith("attach_")).ToList())
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
            {
                renderer.enabled = false;
            }

            DefenseLayout layout = DefenseLayout.Load();
            List<DefenseModelBuilder.Placement> placements = DefenseModelBuilder.Flatten(layout, layout.Get(Layout));

            GameObject ground = BuildModel(root, GroundName, placements);
            ground.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ground.transform.localScale = Vector3.one * GroundScale;

            GameObject worn = BuildModel(root, WornName, placements);
            worn.AddComponent<AmuletGems>();
            worn.SetActive(false);

            Sprite icon = VisualHelper.RenderIcon(amulet.ItemPrefab);
            if (icon != null)
            {
                amulet.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
            }

            return icon;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{DisplayName}: keeping the vanilla look: {ex.Message}");
            return null;
        }
    }

    private static GameObject BuildModel(Transform root, string name, List<DefenseModelBuilder.Placement> placements)
    {
        GameObject model = new(name) { layer = root.gameObject.layer };
        model.transform.SetParent(root, false);
        Dictionary<string, Transform> gems = new();
        for (int i = 1; i <= AmuletGems.Count; i++)
        {
            GameObject gem = new(AmuletGems.GemName(i)) { layer = model.layer };
            gem.transform.SetParent(model.transform, false);
            gems[gem.name] = gem.transform;
        }

        DefenseModelBuilder.Build(model.transform, gems, placements, levelOfDetail: false);
        return model;
    }
}
