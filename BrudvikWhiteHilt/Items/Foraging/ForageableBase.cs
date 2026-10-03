using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Patches.Foraging;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Foraging;

/// <summary>
/// Base class for ingredients that grow in the world and can be picked.
/// Each forageable adds an item and a pickable plant that spawns in new zones. An extra drop on a vanilla plant or creature
/// makes the ingredient available in areas that were explored before the mod was installed.
/// </summary>
public abstract class ForageableBase
{
    private static readonly Dictionary<Texture2D, Material> plantMaterials = new();

    private readonly ConfigEntry<bool> spawn;
    private readonly ConfigEntry<float> spawnPerZone;
    private readonly ConfigEntry<float> extraDropChance;
    private readonly ConfigEntry<float> creatureDropChance;
    private readonly ConfigEntry<float> respawnMinutes;
    private readonly ConfigEntry<int> pickAmount;
    private readonly ConfigEntry<int> groupSizeMin;
    private readonly ConfigEntry<int> groupSizeMax;
    private ZoneSystem.ZoneVegetation vegetation;
    private Pickable pickable;
    private float vanillaRespawnMinutes;
    private int vanillaAmount;

    /// <summary>
    /// Prefab name of the ingredient item.
    /// </summary>
    public abstract string BaseName { get; }

    /// <summary>
    /// Name shown to players, in English. Other languages come from the embedded translation files.
    /// </summary>
    protected abstract string FullName { get; }

    /// <summary>
    /// Item description, in English.
    /// </summary>
    protected abstract string Description { get; }

    /// <summary>
    /// Vanilla item to clone.
    /// </summary>
    protected abstract string CopyItemFrom { get; }

    /// <summary>
    /// Vanilla pickable to clone.
    /// </summary>
    protected abstract string CopyPickableFrom { get; }

    /// <summary>
    /// Where and how often the pickable spawns in newly generated zones.
    /// </summary>
    protected abstract VegetationConfig Vegetation { get; }

    /// <summary>
    /// Indicates whether the forageable is enabled.
    /// </summary>
    public abstract bool Enabled { get; }

    /// <summary>
    /// Vanilla pickable that also gives the ingredient, or null for none.
    /// </summary>
    protected virtual string ExtraDropFrom => CopyPickableFrom;

    /// <summary>
    /// Biomes where <see cref="ExtraDropFrom"/> gives the ingredient. Defaults to the biome the ingredient grows in.
    /// </summary>
    protected virtual Heightmap.Biome ExtraDropBiome => Vegetation.Biome;

    /// <summary>
    /// Chance that picking the vanilla pickable also gives the ingredient.
    /// </summary>
    protected virtual float ExtraDropChance => 0.3f;

    /// <summary>
    /// Vanilla creature that sometimes drops the ingredient, or null for none.
    /// </summary>
    protected virtual string CreatureDropFrom => null;

    /// <summary>
    /// Chance that <see cref="CreatureDropFrom"/> drops the ingredient when killed.
    /// </summary>
    protected virtual float CreatureDropChance => 0.2f;

    /// <summary>
    /// Prefab name of the pickable plant.
    /// </summary>
    public string PickableName => $"Pickable_{BaseName}";

    private string NameKey => $"item_{BaseName.ToLowerInvariant()}";

    /// <summary>
    /// Binds the config entries and registers the English text. Runs in the plugin's Awake.
    /// </summary>
    protected ForageableBase()
    {
        string section = $"Foraging.{FullName.Replace(" ", string.Empty)}";
        WhiteHiltConfig.SetSectionLabel(section, Translations.Token(NameKey));
        AcceptableValueRange<float> chance = new(0f, 1f);

        spawn = WhiteHiltConfig.BindAdminOnly(section, "Spawn", true,
            $"Let {FullName} grow in zones that are generated from now on. Zones that already exist are not changed.");
        spawnPerZone = WhiteHiltConfig.BindAdminOnly(section, "SpawnPerZone", Vegetation.Max,
            "Maximum number of groups per zone (64 x 64 m). Values below 1 are a chance to place one group.",
            new AcceptableValueRange<float>(0f, 20f));
        groupSizeMin = WhiteHiltConfig.BindAdminOnly(section, "GroupSizeMin", Vegetation.GroupSizeMin,
            "Fewest plants in a group, in zones generated from now on.", new AcceptableValueRange<int>(1, 20));
        groupSizeMax = WhiteHiltConfig.BindAdminOnly(section, "GroupSizeMax", Vegetation.GroupSizeMax,
            "Most plants in a group, in zones generated from now on.", new AcceptableValueRange<int>(1, 20));
        respawnMinutes = WhiteHiltConfig.BindAdminOnly(section, "RegrowMinutes", 0f,
            $"In-game minutes before a picked {FullName} grows back. 0 = the same as the vanilla {CopyPickableFrom}. Applies to plants loaded after the change.",
            new AcceptableValueRange<float>(0f, 10000f));
        pickAmount = WhiteHiltConfig.BindAdminOnly(section, "PickAmount", 0,
            $"How many {FullName} one plant gives. 0 = the same as the vanilla {CopyPickableFrom}. Applies to plants loaded after the change.",
            new AcceptableValueRange<int>(0, 20));

        if (ExtraDropFrom != null)
        {
            extraDropChance = WhiteHiltConfig.BindAdminOnly(section, "ExtraDropChance", ExtraDropChance,
                $"Chance that picking a vanilla {ExtraDropFrom} also gives {FullName}. 0 turns it off.", chance);
        }

        if (CreatureDropFrom != null)
        {
            creatureDropChance = WhiteHiltConfig.BindAdminOnly(section, "CreatureDropChance", CreatureDropChance,
                $"Chance that a {CreatureDropFrom} drops 1-2 {FullName} when killed. 0 turns it off.", chance);
        }

        Translations.AddEnglish(NameKey, FullName);
        Translations.AddEnglish($"{NameKey}_description", Description);
    }

    /// <summary>
    /// Adds the item, the pickable and its vegetation. Must run before the first world is loaded.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem item = new(BaseName, CopyItemFrom);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_name = Translations.Token(NameKey);
            shared.m_description = Translations.Token($"{NameKey}_description");
            TryApplyVisual(item.ItemPrefab);

            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }

            ItemManager.Instance.AddItem(item);

            GameObject pickablePrefab = PrefabManager.Instance.CreateClonedPrefab(PickableName, CopyPickableFrom);
            Pickable pickable = pickablePrefab.GetComponent<Pickable>();
            pickable.m_itemPrefab = item.ItemPrefab;
            pickable.m_overrideName = string.Empty;
            pickable.m_extraDrops = new DropTable();
            this.pickable = pickable;
            vanillaRespawnMinutes = pickable.m_respawnTimeMinutes;
            vanillaAmount = pickable.m_amount;
            ApplyPickableConfig();
            TryApplyPickableVisual(pickablePrefab, pickable);

            CustomVegetation customVegetation = new(pickablePrefab, false, Vegetation);
            ZoneManager.Instance.AddCustomVegetation(customVegetation);
            vegetation = customVegetation.Vegetation;
            ApplyVegetationConfig();

            if (ExtraDropFrom != null)
            {
                ForagingDropPatch.Register(ExtraDropFrom, ExtraDropBiome, BaseName, () => extraDropChance.Value);
            }

            OnAdded();
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Applies changed or server-synced config values.
    /// </summary>
    public void ApplyConfig()
    {
        ApplyVegetationConfig();
        ApplyPickableConfig();
        if (ZNetScene.instance != null)
        {
            AddCreatureDrop();
        }
    }

    /// <summary>
    /// Lets the vanilla creature drop the ingredient, or updates its chance. Must run for every new <see cref="ZNetScene"/>,
    /// since it edits a vanilla prefab.
    /// </summary>
    public void AddCreatureDrop()
    {
        if (CreatureDropFrom == null)
        {
            return;
        }

        try
        {
            GameObject itemPrefab = PrefabManager.Instance.GetPrefab(BaseName);
            CharacterDrop characterDrop = ZNetScene.instance?.GetPrefab(CreatureDropFrom)?.GetComponent<CharacterDrop>();
            if (itemPrefab == null || characterDrop == null)
            {
                Jotunn.Logger.LogWarning($"{FullName}: could not add a drop to {CreatureDropFrom}.");
                return;
            }

            CharacterDrop.Drop existing = characterDrop.m_drops.Find(drop => drop.m_prefab == itemPrefab);
            if (existing != null)
            {
                existing.m_chance = creatureDropChance.Value;
                return;
            }

            characterDrop.m_drops.Add(new CharacterDrop.Drop
            {
                m_prefab = itemPrefab,
                m_amountMin = 1,
                m_amountMax = 2,
                m_chance = creatureDropChance.Value,
                m_levelMultiplier = false
            });
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName}: failed to add a drop to {CreatureDropFrom}!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Changes the look of the cloned item and pickable.
    /// </summary>
    /// <param name="visualRoot">The item prefab, or the visible part of the pickable.</param>
    protected abstract void ApplyVisual(GameObject visualRoot);

    /// <summary>
    /// Registers what else the forageable brings, such as a smelter conversion. Runs after the item and pickable exist.
    /// </summary>
    protected virtual void OnAdded()
    {
    }

    /// <summary>
    /// Changes the look of the cloned pickable. By default <see cref="ApplyVisual"/> on the part hidden when picked.
    /// </summary>
    /// <param name="pickablePrefab">The pickable prefab.</param>
    /// <param name="pickable">Its pickable component.</param>
    protected virtual void ApplyPickableVisual(GameObject pickablePrefab, Pickable pickable)
    {
        ApplyVisual(pickable.m_hideWhenPicked != null ? pickable.m_hideWhenPicked : pickablePrefab);
    }

    /// <summary>
    /// Whether a visual root is the item prefab rather than the pickable.
    /// </summary>
    /// <param name="visualRoot">The object passed to <see cref="ApplyVisual"/>.</param>
    /// <returns>True for the item.</returns>
    protected static bool IsItem(GameObject visualRoot)
    {
        return visualRoot.GetComponent<ItemDrop>() != null;
    }

    /// <summary>
    /// Shows a bundle model in place of the vanilla look, with a static plant material. On an item it takes the height
    /// of the vanilla item; on a pickable it stands on the ground, <paramref name="height"/> metres high.
    /// </summary>
    /// <param name="visualRoot">The object passed to <see cref="ApplyVisual"/>.</param>
    /// <param name="modelName">Bundle mesh name; its texture is <c>&lt;modelName&gt;_albedo</c>.</param>
    /// <param name="height">Height on the ground, in metres.</param>
    /// <returns>The new model.</returns>
    protected static GameObject ReplacePlantMesh(GameObject visualRoot, string modelName, float height)
    {
        Mesh mesh = ForagingAssets.LoadMesh(modelName);
        Texture2D texture = ForagingAssets.LoadTexture($"{modelName}_albedo");
        if (IsItem(visualRoot))
        {
            GameObject itemModel = VisualHelper.ReplaceMesh(visualRoot, mesh, texture);
            itemModel.GetComponent<MeshRenderer>().sharedMaterial = PlantMaterial(texture);
            return itemModel;
        }

        Vector3 size = mesh.bounds.size;
        float longest = height * Mathf.Max(size.x, size.y, size.z) / size.y / visualRoot.transform.lossyScale.y;
        GameObject model = VisualHelper.ReplaceMesh(visualRoot, mesh, texture, size: longest);
        model.GetComponent<MeshRenderer>().sharedMaterial = PlantMaterial(texture);

        // Vanilla plants reach below the ground, so the base goes to the pickable's origin instead.
        Transform ground = visualRoot.GetComponentInParent<Pickable>(true)?.transform ?? visualRoot.transform;
        Vector3 position = model.transform.position;
        model.transform.position = new Vector3(position.x, ground.position.y - 0.02f, position.z);
        return model;
    }

    /// <summary>
    /// Shows a bush from the bundle on a pickable bush: <c>&lt;modelName&gt;</c> stays, <c>&lt;modelName&gt;fruit</c>
    /// (berries or flowers, made with the same bounds) hides when picked.
    /// </summary>
    /// <param name="pickablePrefab">The pickable prefab.</param>
    /// <param name="pickable">Its pickable component.</param>
    /// <param name="modelName">Bundle mesh name of the bush.</param>
    /// <param name="height">Height on the ground, in metres.</param>
    protected static void ReplaceBushMesh(GameObject pickablePrefab, Pickable pickable, string modelName, float height)
    {
        GameObject model = ReplacePlantMesh(pickablePrefab, modelName, height);
        MeshRenderer renderer = model.GetComponent<MeshRenderer>();
        Mesh fruitMesh = ForagingAssets.LoadMesh($"{modelName}fruit");
        GameObject fruit = VisualHelper.AddMesh(model, fruitMesh, null, Vector3.zero, model.GetComponent<MeshFilter>().sharedMesh.bounds.size.y);
        fruit.GetComponent<MeshRenderer>().sharedMaterial = renderer.sharedMaterial;
        if (pickable.m_hideWhenPicked != null)
        {
            fruit.transform.SetParent(pickable.m_hideWhenPicked.transform, true);
        }
    }

    /// <summary>
    /// Removes the glow of a cloned thistle: its point light and its flare and bee particles.
    /// </summary>
    /// <param name="pickablePrefab">The pickable prefab.</param>
    protected static void RemoveGlow(GameObject pickablePrefab)
    {
        foreach (Light light in pickablePrefab.GetComponentsInChildren<Light>(true))
        {
            UnityEngine.Object.DestroyImmediate(light.gameObject);
        }

        foreach (ParticleSystem particles in pickablePrefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            UnityEngine.Object.DestroyImmediate(particles.gameObject);
        }
    }

    // The mushroom's material does not sway in the wind, so models made of many parts keep together.
    private static Material PlantMaterial(Texture2D texture)
    {
        if (plantMaterials.TryGetValue(texture, out Material material))
        {
            return material;
        }

        Material template = PrefabManager.Cache.GetPrefab<GameObject>("Pickable_Mushroom").GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
        material = VisualHelper.CreateTexturedMaterial(template, texture, $"{texture.name}_plant");
        plantMaterials[texture] = material;
        return material;
    }

    // The ZoneSystem keeps this same object, so changes apply to zones generated afterwards.
    private void ApplyVegetationConfig()
    {
        if (vegetation == null)
        {
            return;
        }

        vegetation.m_enable = spawn.Value;
        vegetation.m_max = spawnPerZone.Value;
        vegetation.m_groupSizeMin = groupSizeMin.Value;
        vegetation.m_groupSizeMax = Mathf.Max(groupSizeMin.Value, groupSizeMax.Value);
    }

    // Edits the prefab, so plants already loaded keep their values until they load again.
    private void ApplyPickableConfig()
    {
        if (pickable == null)
        {
            return;
        }

        pickable.m_respawnTimeMinutes = respawnMinutes.Value > 0f ? respawnMinutes.Value : vanillaRespawnMinutes;
        pickable.m_amount = pickAmount.Value > 0 ? pickAmount.Value : vanillaAmount;
    }

    private void TryApplyVisual(GameObject visualRoot)
    {
        TryApplyVisual(visualRoot.name, () => ApplyVisual(visualRoot));
    }

    private void TryApplyPickableVisual(GameObject pickablePrefab, Pickable pickable)
    {
        TryApplyVisual(pickablePrefab.name, () => ApplyPickableVisual(pickablePrefab, pickable));
    }

    private void TryApplyVisual(string name, Action apply)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        // A broken look must not remove the item, or players would lose it from their inventories.
        try
        {
            apply();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look of {name}: {ex.Message}");
        }
    }
}
