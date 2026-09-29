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
/// </summary>
public class PathfinderAmulet : IWhiteHiltCustomItem
{
    /// <summary>
    /// Prefab name of the amulet.
    /// </summary>
    public const string PrefabName = "WhiteHiltPathfinder";

    private const string FullName = "Pathfinder's Amulet";
    private const string Description = "A valknut pendant on a leather cord, set with a gem for every twenty levels of Exploration. Worn as a trinket, the map uncovers further around you, up to 200 m. Uncovering new land fills its adrenaline; when it is full, Odin's ravens show you the land for 500 m around.";
    private const string CopyFrom = "TrinketBronzeHealth";
    private const string EffectKey = "se_whitehiltpathfinder";
    private const string AmuletLayout = "stifinner";

    // The chest bone; VisEquipment puts a child named attach_<bone> of the item on that bone.
    private const string WornName = "attach_Spine2";
    private const string GroundName = "WhiteHiltAmuletModel";
    private const float GroundScale = 2f;

    private const float MaxAdrenaline = 50f;

    // About 2.5 minutes of sailing along new coast fill it.
    private const float AdrenalinePerSquareMetre = 0.0001f;

    private readonly ItemManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(PrefabName));

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Constructor for the PathfinderAmulet class. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public PathfinderAmulet(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PrefabName), FullName, Description);
        Translations.AddEnglish(EffectKey, "Pathfinder");
        Translations.AddEnglish($"{EffectKey}_tooltip", "The map uncovers further around you. New land fills your adrenaline.");
        RavenSightEffect.RegisterEnglish();
    }

    /// <summary>
    /// Returns true if the player wears the amulet.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True while it is worn.</returns>
    public static bool IsWorn(Player player)
    {
        return player != null && player.m_trinketItem != null && player.m_trinketItem.m_shared.m_name == Translations.Token(Translations.ItemKey(PrefabName));
    }

    /// <summary>
    /// Fills the wearer's adrenaline for newly uncovered map.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="squareMetres">Area of the newly uncovered map.</param>
    public static void OnExplored(Player player, float squareMetres)
    {
        if (IsWorn(player))
        {
            player.AddAdrenaline(squareMetres * AdrenalinePerSquareMetre);
        }
    }

    /// <summary>
    /// Adds the amulet and its recipe at the Cartographer's Desk.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem amulet = new(PrefabName, CopyFrom, new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token($"{Translations.ItemKey(PrefabName)}_description"),
                CraftingStation = CartographerDesk.StationPrefabName,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Bronze", Amount = 3 },
                    new() { Item = "Amber", Amount = 2 },
                    new() { Item = "Ruby", Amount = 1 }
                }
            });

            ItemDrop.ItemData.SharedData shared = amulet.ItemDrop.m_itemData.m_shared;
            SE_Stats equipEffect = ScriptableObject.CreateInstance<SE_Stats>();
            equipEffect.name = "SE_WhiteHiltPathfinder";
            equipEffect.m_name = Translations.Token(EffectKey);
            equipEffect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
            RavenSightEffect ravenSight = RavenSightEffect.Create();

            shared.m_maxAdrenaline = MaxAdrenaline;
            shared.m_equipStatusEffect = equipEffect;
            shared.m_fullAdrenalineSE = ravenSight;

            Sprite icon = TryApplyVisual(amulet);
            equipEffect.m_icon = icon ?? shared.m_icons.FirstOrDefault();
            ravenSight.m_icon = equipEffect.m_icon;

            instance.AddStatusEffect(new CustomStatusEffect(equipEffect, fixReference: false));
            instance.AddStatusEffect(new CustomStatusEffect(ravenSight, fixReference: false));
            instance.AddItem(amulet);
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Replaces the vanilla trinket with the amulet: worn on the chest bone, and larger on the ground and in the icon.
    private static Sprite TryApplyVisual(CustomItem amulet)
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
            List<DefenseModelBuilder.Placement> placements = DefenseModelBuilder.Flatten(layout, layout.Get(AmuletLayout));

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
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
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
