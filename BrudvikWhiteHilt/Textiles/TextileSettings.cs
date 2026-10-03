using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System;
using System.Linq;

namespace BrudvikWhiteHilt.Textiles;

/// <summary>
/// Config and texts for weaving and dyeing: what dyeing a cape, a banner or a sail costs, how near the loom a cape is
/// dyed, and which materials count as cloth.
/// </summary>
public static class TextileSettings
{
    private const string Section = "Textiles";

    private static string[] clothNames = Array.Empty<string>();

    /// <summary>How near a loom a cape can be dyed, in metres.</summary>
    public static ConfigEntry<float> LoomRange { get; private set; }

    /// <summary>Paint pot uses to dye a cape.</summary>
    public static ConfigEntry<int> CapeUses { get; private set; }

    /// <summary>Paint pot uses to dye a banner.</summary>
    public static ConfigEntry<int> BannerUses { get; private set; }

    /// <summary>Paint pot uses to dye a sail.</summary>
    public static ConfigEntry<int> SailUses { get; private set; }

    /// <summary>Linen cloth to dye a sail.</summary>
    public static ConfigEntry<int> SailCloth { get; private set; }

    /// <summary>Material names (parts) that count as cloth on pieces and ships.</summary>
    public static ConfigEntry<string> ClothMaterials { get; private set; }

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        LoomRange = WhiteHiltConfig.BindAdminOnly(Section, "LoomRange", 5f, "How near a loom a cape can be dyed, in metres.",
            new AcceptableValueRange<float>(1f, 20f));
        CapeUses = WhiteHiltConfig.BindAdminOnly(Section, "CapeUses", 2, "Paint pot uses to dye a cape.", new AcceptableValueRange<int>(0, 20));
        BannerUses = WhiteHiltConfig.BindAdminOnly(Section, "BannerUses", 1, "Paint pot uses to dye the cloth of a banner.", new AcceptableValueRange<int>(0, 20));
        SailUses = WhiteHiltConfig.BindAdminOnly(Section, "SailUses", 5, "Paint pot uses to dye a sail.", new AcceptableValueRange<int>(0, 20));
        SailCloth = WhiteHiltConfig.BindAdminOnly(Section, "SailCloth", 4, "Linen cloth to dye a sail.", new AcceptableValueRange<int>(0, 20));
        ClothMaterials = WhiteHiltConfig.BindAdminOnly(Section, "ClothMaterials", "sail,banner,curtain,tapestry",
            "Comma-separated parts of material names that count as cloth, for dyeing banners and sails.");
        ClothMaterials.SettingChanged += (_, _) => ReadClothNames();
        ReadClothNames();

        Translations.AddEnglish("msg_whitehilt_dye_done", "Dyed {0}");
        Translations.AddEnglish("msg_whitehilt_dye_pot", "The pot does not hold enough paint: {0} uses needed");
        Translations.AddEnglish("msg_whitehilt_dye_cloth", "Not enough linen cloth: {0} needed");
        Translations.AddEnglish("whitehilt_dye_tooltip", "Dyed");
    }

    /// <summary>
    /// True if a material is cloth.
    /// </summary>
    /// <param name="materialName">The material's name.</param>
    /// <returns>True if so.</returns>
    public static bool IsCloth(string materialName)
    {
        if (string.IsNullOrEmpty(materialName))
        {
            return false;
        }

        string lower = materialName.ToLowerInvariant();
        return clothNames.Any(lower.Contains);
    }

    private static void ReadClothNames()
    {
        clothNames = ClothMaterials.Value.Split(',').Select(name => name.Trim().ToLowerInvariant()).Where(name => name.Length > 0).ToArray();
    }
}
