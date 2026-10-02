using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Patches.Foraging;
using BrudvikWhiteHilt.Pieces.Roofs;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Roofing;

/// <summary>
/// The materials of the White Hilt roofs: birch bark, turf, pine tar, slate, soapstone and straw (reed is a forageable),
/// and the slate outcrops in the Mountains. Where each comes from is set up here and in Patches/Roofing.
/// </summary>
public static class RoofMaterials
{
    /// <summary>Prefab name of birch bark.</summary>
    public const string BirchBark = "WhiteHilt_BirchBark";

    /// <summary>Prefab name of turf.</summary>
    public const string Turf = "WhiteHilt_Turf";

    /// <summary>Prefab name of pine tar.</summary>
    public const string PineTar = "WhiteHilt_PineTar";

    /// <summary>Prefab name of slate.</summary>
    public const string Slate = "WhiteHilt_Slate";

    /// <summary>Prefab name of soapstone.</summary>
    public const string Soapstone = "WhiteHilt_Soapstone";

    /// <summary>Prefab name of straw.</summary>
    public const string Straw = "WhiteHilt_Straw";

    /// <summary>Prefab name of the slate outcrop.</summary>
    public const string OutcropName = "WhiteHilt_SlateOutcrop";

    private const string OutcropFracName = "WhiteHilt_SlateOutcrop_frac";

    private static ZoneSystem.ZoneVegetation outcropVegetation;

    /// <summary>
    /// Registers the translations and the extra drops, and the items once the vanilla prefabs can be cloned.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BirchBark), "Birch Bark",
            "White bark peeled from a birch in sheets. Laid in layers under the sod, it keeps a roof dry for a lifetime.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(Turf), "Turf",
            "A square of sod cut from good grassland, roots and all. On a roof it holds the birch bark down and keeps the cold out.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(PineTar), "Pine Tar",
            "Thick, sweet-smelling tar burnt out of resinous pine in a tar kiln. Shingles and ships last for generations with it.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(Slate), "Slate",
            "Dark stone that splits into thin, flat slabs. Laid in courses, it makes a roof that neither burns nor rots.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(Soapstone), "Soapstone",
            "Soft, grey-green stone that a knife can carve. It takes the heat of a fire and gives it back for hours.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(Straw), "Straw",
            "Dry stalks of barley and flax, left when the grain is gathered. Bound in bundles, it thatches a roof.");
        Translations.AddEnglish("whitehilt_slateoutcrop", "Slate outcrop");

        ForagingDropPatch.Register("Pickable_Barley", Heightmap.Biome.All, Straw, () => RoofSettings.StrawChance.Value);
        ForagingDropPatch.Register("Pickable_Barley_Wild", Heightmap.Biome.All, Straw, () => RoofSettings.StrawChance.Value);
        ForagingDropPatch.Register("Pickable_Flax", Heightmap.Biome.All, Straw, () => RoofSettings.StrawChance.Value);
        ForagingDropPatch.Register("Pickable_Flax_Wild", Heightmap.Biome.All, Straw, () => RoofSettings.StrawChance.Value);

        PrefabManager.OnVanillaPrefabsAvailable += Add;
    }

    /// <summary>
    /// Applies changed or server-synced config values to the outcrop vegetation.
    /// </summary>
    public static void ApplyConfig()
    {
        if (outcropVegetation != null)
        {
            outcropVegetation.m_enable = RoofSettings.SlateOutcropPerZone.Value > 0f;
            outcropVegetation.m_max = RoofSettings.SlateOutcropPerZone.Value;
        }
    }

    private static void Add()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= Add;
        Try("Roof materials", () =>
        {
            AddItem(BirchBark, "ElderBark", pixel => Shade(pixel, new Color(0.9f, 0.88f, 0.82f), 0.25f));
            AddItem(Turf, "Stone", pixel => Earth(pixel));
            AddItem(PineTar, "Tar", pixel => Shade(pixel, new Color(0.55f, 0.3f, 0.12f), 0f));
            AddItem(Slate, "Stone", pixel => Shade(pixel, new Color(0.42f, 0.46f, 0.52f), 0f));
            AddItem(Soapstone, "Stone", pixel => Shade(pixel, new Color(0.62f, 0.68f, 0.62f), 0f));
            AddItem(Straw, "Flax", pixel => Shade(pixel, new Color(0.95f, 0.78f, 0.42f), 0f));
        });
        Try("Slate outcrop", AddOutcrop);
    }

    private static void Try(string what, Action action)
    {
        try
        {
            action();
            Jotunn.Logger.LogInfo($"{what} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{what} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void AddItem(string name, string copyFrom, Func<Color32, Color32> recolor)
    {
        CustomItem item = new(name, copyFrom);
        ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
        string key = Translations.ItemKey(name);
        shared.m_name = Translations.Token(key);
        shared.m_description = Translations.Token($"{key}_description");
        shared.m_teleportable = true;
        if (!VisualHelper.IsHeadless)
        {
            VisualHelper.Recolor(item.ItemPrefab, recolor);
            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }

        ItemManager.Instance.AddItem(item);
    }

    // Keeps the light and dark of the pixel and gives it the tint; with streaks > 0, the darkest pixels stay dark.
    private static Color32 Shade(Color32 pixel, Color tint, float streaks)
    {
        Color.RGBToHSV(pixel, out _, out _, out float value);
        if (value < streaks)
        {
            return new Color32(30, 26, 22, pixel.a);
        }

        Color result = tint * Mathf.Clamp01(0.45f + value * 0.75f);
        result.a = pixel.a / 255f;
        return result;
    }

    // Dark soil in the shadows, grass where the stone is light.
    private static Color32 Earth(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out _, out _, out float value);
        Color soil = new(0.3f, 0.22f, 0.14f);
        Color grass = new(0.36f, 0.5f, 0.2f);
        Color result = Color.Lerp(soil, grass, Mathf.SmoothStep(0f, 1f, (value - 0.3f) / 0.4f)) * (0.7f + value * 0.5f);
        result.a = pixel.a / 255f;
        return result;
    }

    // A copper deposit turned to slate: the whole rock and its broken pieces, which drop slate, stone and soapstone.
    private static void AddOutcrop()
    {
        GameObject slate = PrefabManager.Instance.GetPrefab(Slate) ?? throw new InvalidOperationException($"{Slate} not found");
        GameObject soapstone = PrefabManager.Instance.GetPrefab(Soapstone) ?? throw new InvalidOperationException($"{Soapstone} not found");
        GameObject stone = PrefabManager.Instance.GetPrefab("Stone");

        GameObject frac = PrefabManager.Instance.CreateClonedPrefab(OutcropFracName, "rock4_copper_frac");
        MineRock5 mineRock = frac.GetComponent<MineRock5>() ?? throw new InvalidOperationException("rock4_copper_frac has no MineRock5");
        mineRock.m_name = Translations.Token("whitehilt_slateoutcrop");
        List<DropTable.DropData> drops = new()
        {
            new() { m_item = slate, m_stackMin = 1, m_stackMax = 2, m_weight = 3f },
            new() { m_item = soapstone, m_stackMin = 1, m_stackMax = 1, m_weight = 1f }
        };
        if (stone != null)
        {
            drops.Add(new DropTable.DropData { m_item = stone, m_stackMin = 1, m_stackMax = 2, m_weight = 2f });
        }

        mineRock.m_dropItems = new DropTable { m_drops = drops, m_dropMin = 2, m_dropMax = 4, m_dropChance = 1f };

        GameObject outcrop = PrefabManager.Instance.CreateClonedPrefab(OutcropName, "rock4_copper");
        Destructible destructible = outcrop.GetComponent<Destructible>() ?? throw new InvalidOperationException("rock4_copper has no Destructible");
        destructible.m_spawnWhenDestroyed = frac;
        HoverText hover = outcrop.GetComponent<HoverText>();
        if (hover != null)
        {
            hover.m_text = Translations.Token("whitehilt_slateoutcrop");
        }

        if (!VisualHelper.IsHeadless)
        {
            Dictionary<Material, Material> slateMaterials = new();
            Recolor(outcrop, slateMaterials);
            Recolor(frac, slateMaterials);
        }

        CustomVegetation vegetation = new(outcrop, false, new VegetationConfig
        {
            Biome = Heightmap.Biome.Mountain,
            Min = 1,
            Max = RoofSettings.SlateOutcropPerZone.Value,
            GroupSizeMin = 1,
            GroupSizeMax = 1,
            MinAltitude = 40f,
            MaxTilt = 35f,
            BlockCheck = true
        });
        ZoneManager.Instance.AddCustomVegetation(vegetation);
        outcropVegetation = vegetation.Vegetation;
        ApplyConfig();
    }

    // The copper veins become dark, blue-grey slate. Materials are shared, so each is recoloured once.
    private static void Recolor(GameObject root, Dictionary<Material, Material> cache)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
            {
                if (source == null || source.mainTexture == null)
                {
                    return source;
                }

                if (!cache.TryGetValue(source, out Material material))
                {
                    material = new Material(source)
                    {
                        mainTexture = VisualHelper.RecolorTexture(source.mainTexture, pixel => Shade(pixel, new Color(0.4f, 0.44f, 0.5f), 0f))
                    };
                    cache[source] = material;
                }

                return material;
            }).ToArray();
        }
    }
}
