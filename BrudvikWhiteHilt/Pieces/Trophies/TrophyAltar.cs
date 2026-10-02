using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Trophies;

/// <summary>
/// The Trophy Altar: a small stone altar with Eikthyr in miniature. A boss trophy and a Swamp Key give a full stack of
/// that trophy, so every extra copy costs another kill of The Elder. Any other trophy of the game does the same with a
/// Hard Antler from Eikthyr.
/// </summary>
public class TrophyAltar : IWhiteHiltCustomPiece
{
    /// <summary>Prefab name, also the crafting station of the copies.</summary>
    public const string PrefabName = "piece_whitehilt_trophyaltar";

    private const string FullName = "Trophy Altar";
    private const string Description = "A small stone altar with Eikthyr in miniature. Offer a boss trophy and a Swamp Key, or any other trophy and a Hard Antler, to get a full stack of that trophy.";
    private const string KeyPrefab = "CryptKey";
    private const string AntlerPrefab = "HardAntler";
    private const string VanillaTrophyPrefix = "Trophy";
    private const string StatuetteCreature = "Eikthyr";
    private const string StatuetteClip = "Idle";
    private const string StatuetteBody = "Deer";
    private const float Size = 1.2f;

    // Where Eikthyr stands on the slab, in the altar model's units: the altar is 1 high (0.33 m) and 3.6 long along x.
    private static readonly Vector3 statuetteBase = new(0f, 0.98f, 0f);
    private const float StatuetteHeight = 1.2f;
    private const float StatuetteYaw = 90f;

    private static readonly string[] looks = { "New", "Worn", "Broken" };
    private static readonly Dictionary<string, Recipe> recipes = new();
    private static readonly HashSet<string> ordinary = new();

    private readonly PieceManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public string Id => PrefabName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(PrefabName);

    /// <inheritdoc/>
    public string GatedPrefabName => PrefabName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public TrophyAltar(PieceManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(PrefabName, FullName, Description);
    }

    /// <summary>
    /// Adds the altar to the hammer and one copy recipe per trophy to the altar.
    /// </summary>
    public void Add()
    {
        try
        {
            PieceConfig pieceConfig = new()
            {
                Name = NameToken,
                Description = Translations.Token($"{PrefabName}_description"),
                PieceTable = PieceTables.Hammer,
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "Stone", Amount = 10, Recover = true },
                    new() { Item = "FineWood", Amount = 4, Recover = true },
                    new() { Item = "Iron", Amount = 2, Recover = true },
                    new() { Item = "ElderBark", Amount = 2, Recover = true }
                }
            };

            CustomPiece piece = new(PrefabName, "piece_workbench", pieceConfig);

            // Recipes match their station by name, so a unique name keeps the workbench recipes off the altar.
            CraftingStation station = piece.PiecePrefab.GetComponent<CraftingStation>();
            station.m_name = NameToken;
            station.m_craftRequireRoof = false;
            station.m_craftRequireFire = false;
            station.m_showBasicRecipies = false;

            TryApplyVisual(piece, station);
            instance.AddPiece(piece);
            AddRecipes();
            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Every trophy in the default list and the config gets a recipe with the key, every other trophy of the game one with
    // the antler; the config then switches them on and off.
    private static void AddRecipes()
    {
        List<string> bosses = TrophyAltarSettings.BossTrophies.Split(',').Concat(TrophyAltarSettings.TrophyNames()).Distinct().ToList();
        foreach (string trophy in bosses)
        {
            AddRecipe(trophy, KeyPrefab, TrophyAltarSettings.KeysPerCraft.Value);
        }

        foreach (string trophy in OrdinaryTrophyNames().Where(name => !bosses.Contains(name)))
        {
            if (AddRecipe(trophy, AntlerPrefab, TrophyAltarSettings.AntlersPerCraft.Value))
            {
                ordinary.Add(trophy);
            }
        }

        TrophyAltarSettings.Trophies.SettingChanged += (_, _) => ApplyRecipes();
        TrophyAltarSettings.TrophiesPerCraft.SettingChanged += (_, _) => ApplyRecipes();
        TrophyAltarSettings.KeysPerCraft.SettingChanged += (_, _) => ApplyRecipes();
        TrophyAltarSettings.TrophiesMade.SettingChanged += (_, _) => ApplyRecipes();
        TrophyAltarSettings.OrdinaryTrophies.SettingChanged += (_, _) => ApplyRecipes();
        TrophyAltarSettings.AntlersPerCraft.SettingChanged += (_, _) => ApplyRecipes();
        TrophyAltarSettings.OrdinaryTrophiesMade.SettingChanged += (_, _) => ApplyRecipes();
        TrophyAltarSettings.ExcludedTrophies.SettingChanged += (_, _) => ApplyRecipes();
        ApplyRecipes();
    }

    private static bool AddRecipe(string trophy, string offering, int offeringAmount)
    {
        if (PrefabManager.Instance.GetPrefab(trophy)?.GetComponent<ItemDrop>() == null)
        {
            Jotunn.Logger.LogWarning($"{FullName}: unknown trophy '{trophy}' is left out");
            return false;
        }

        CustomRecipe recipe = new(new RecipeConfig
        {
            Name = $"Recipe_WhiteHiltTrophyAltar_{trophy}",
            Item = trophy,
            Amount = 1,
            CraftingStation = PrefabName,
            Requirements = new RequirementConfig[]
            {
                new() { Item = trophy, Amount = TrophyAltarSettings.TrophiesPerCraft.Value },
                new() { Item = offering, Amount = offeringAmount }
            }
        });
        ItemManager.Instance.AddRecipe(recipe);
        recipes[trophy] = recipe.Recipe;
        return true;
    }

    // The game's own trophies, by their "Trophy" names: White Hilt's own trophies, other mods' items and the beasts' black
    // trophies are not copied this way.
    private static IEnumerable<string> OrdinaryTrophyNames()
    {
        IEnumerable<GameObject> items = ObjectDB.instance != null && ObjectDB.instance.m_items.Count > 0
            ? ObjectDB.instance.m_items
            : PrefabManager.Cache.GetPrefabs(typeof(ItemDrop)).Values.OfType<ItemDrop>().Select(item => item.gameObject);
        return items
            .Where(prefab => prefab != null && prefab.name.StartsWith(VanillaTrophyPrefix, StringComparison.Ordinal))
            .Where(prefab => prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
            .Where(prefab => ItemManager.Instance.GetItem(prefab.name) == null && !Chests.Helpers.ChestSupply.IsEarnedOnly(prefab.name))
            .Select(prefab => prefab.name)
            .Distinct()
            .ToList();
    }

    // The recipe objects stay registered with the game, so changing them in place reaches the altar without a restart.
    private static void ApplyRecipes()
    {
        IReadOnlyList<string> enabled = TrophyAltarSettings.TrophyNames();
        foreach (string missing in enabled.Where(name => !recipes.ContainsKey(name)))
        {
            Jotunn.Logger.LogWarning($"{FullName}: '{missing}' has no recipe until the game is restarted");
        }

        IReadOnlyList<string> excluded = TrophyAltarSettings.ExcludedNames();
        foreach (KeyValuePair<string, Recipe> entry in recipes)
        {
            Recipe recipe = entry.Value;
            bool isOrdinary = ordinary.Contains(entry.Key);
            recipe.m_enabled = isOrdinary
                ? TrophyAltarSettings.OrdinaryTrophies.Value && !excluded.Contains(entry.Key)
                : enabled.Contains(entry.Key);
            recipe.m_amount = MadeAmount(entry.Key, isOrdinary ? TrophyAltarSettings.OrdinaryTrophiesMade.Value : TrophyAltarSettings.TrophiesMade.Value);
            if (recipe.m_resources != null && recipe.m_resources.Length >= 2)
            {
                recipe.m_resources[0].m_amount = TrophyAltarSettings.TrophiesPerCraft.Value;
                recipe.m_resources[1].m_amount = isOrdinary ? TrophyAltarSettings.AntlersPerCraft.Value : TrophyAltarSettings.KeysPerCraft.Value;
            }
        }
    }

    private static int MadeAmount(string trophy, int configured)
    {
        if (configured > 0)
        {
            return configured;
        }

        ItemDrop item = PrefabManager.Instance.GetPrefab(trophy)?.GetComponent<ItemDrop>();
        return item != null ? Mathf.Max(1, item.m_itemData.m_shared.m_maxStackSize) : 1;
    }

    private static void TryApplyVisual(CustomPiece piece, CraftingStation station)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            Transform root = piece.PiecePrefab.transform;
            Mesh altar = ForagingAssets.LoadMesh("trophyaltar");
            Texture2D altarTexture = ForagingAssets.LoadTexture("trophyaltar_albedo");
            (Mesh statuette, Material statuetteMaterial) = TryBakeStatuette();

            GameObject newLook = null;
            foreach (string look in looks)
            {
                Transform lookRoot = root.Find(look) ?? throw new InvalidOperationException($"the look {look} was not found");
                GameObject model = VisualHelper.ReplaceMesh(lookRoot.gameObject, altar, altarTexture, size: Size);
                newLook ??= model;
                if (statuette != null)
                {
                    GameObject figure = VisualHelper.AddMesh(model, statuette, null, statuetteBase, StatuetteHeight,
                        Quaternion.Euler(0f, StatuetteYaw, 0f));
                    figure.GetComponent<MeshRenderer>().sharedMaterial = statuetteMaterial;
                }
            }

            VisualHelper.FitBoxColliders(root, newLook);
            PieceFragments.Apply(piece.PiecePrefab);
            Sprite icon = VisualHelper.RenderIcon(piece.PiecePrefab);
            if (icon != null)
            {
                piece.Piece.m_icon = icon;
                station.m_icon = icon;
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the vanilla look: {ex.Message}");
        }
    }

    // A missing statuette leaves the bare altar.
    private static (Mesh, Material) TryBakeStatuette()
    {
        try
        {
            return BakeStatuette();
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: no Eikthyr on the altar: {ex.Message}");
            return (null, null);
        }
    }

    // Eikthyr baked from the game's own boss in his idle pose, so nothing of the game is shipped. His shedding moss is left out.
    private static (Mesh, Material) BakeStatuette()
    {
        Transform visual = PrefabManager.Instance.GetPrefab(StatuetteCreature)?.transform.Find("Visual")
            ?? throw new InvalidOperationException("Eikthyr's Visual was not found");
        GameObject holder = new("whitehilt_eikthyr_bake");
        holder.SetActive(false);
        try
        {
            GameObject copy = UnityEngine.Object.Instantiate(visual.gameObject, holder.transform, false);
            AnimationClip idle = copy.GetComponent<Animator>()?.runtimeAnimatorController?.animationClips
                .FirstOrDefault(clip => clip.name == StatuetteClip);
            if (idle != null)
            {
                idle.SampleAnimation(copy, 0f);
            }
            else
            {
                Jotunn.Logger.LogWarning($"{FullName}: Eikthyr has no {StatuetteClip} clip, the statuette keeps the prefab pose");
            }

            Material material = copy.transform.Find(StatuetteBody)?.GetComponent<SkinnedMeshRenderer>()?.sharedMaterial
                ?? throw new InvalidOperationException("Eikthyr's body was not found");
            List<CombineInstance> parts = new();
            foreach (SkinnedMeshRenderer skin in copy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                // Only the active parts directly under Visual: the inactive OLD model has a body of its own.
                if (skin.transform.parent != copy.transform || !skin.gameObject.activeSelf || skin.sharedMaterial != material)
                {
                    continue;
                }

                Mesh baked = new() { name = $"whitehilt_eikthyr_{skin.name}" };
                skin.BakeMesh(baked, true);
                Matrix4x4 toRoot = copy.transform.worldToLocalMatrix * Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one);
                parts.Add(new CombineInstance { mesh = baked, transform = toRoot });
            }

            if (parts.Count == 0 || parts.All(part => part.mesh.vertexCount == 0))
            {
                throw new InvalidOperationException("baking Eikthyr gave no vertices");
            }

            Mesh statuette = new() { name = "whitehilt_eikthyr_statuette", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            statuette.CombineMeshes(parts.ToArray(), true, true);
            statuette.RecalculateBounds();
            parts.ForEach(part => UnityEngine.Object.Destroy(part.mesh));
            return (statuette, material);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(holder);
        }
    }
}
