using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.TradingPosts;
using UnityEngine;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// How a member of the household looks, as agreed in the preview pictures (AssetSource/Preview/build_household.py,
/// round A of docs/plans/household.md): farmhands in plain wool and linen in the dyes of a farm (undyed, woad, madder,
/// onion skin), huskarls in iron mail and helmet. Body, hair, beard and colours come from a seed and are written into
/// the network data, where the game's equipment display reads them on every machine.
/// </summary>
public static class HouseholdLooks
{
    private const string TunicKey = "item_whitehilt_householdtunic";
    private const string TrousersKey = "item_whitehilt_householdtrousers";

    // The four sets of farm clothes: the tunic's cut, its colour and trim, and the trousers' colour. The same values as
    // FARMHANDS in build_household.py.
    private static readonly Outfit[] outfits =
    {
        new("ArmorTunic1", new Color(0.52f, 0.46f, 0.38f), new Color(0.72f, 0.66f, 0.55f), new Color(0.33f, 0.27f, 0.2f)),
        new("ArmorTunic2", new Color(0.3f, 0.4f, 0.56f), new Color(0.85f, 0.82f, 0.72f), new Color(0.36f, 0.3f, 0.22f)),
        new("ArmorTunic3", new Color(0.56f, 0.24f, 0.16f), new Color(0.8f, 0.7f, 0.45f), new Color(0.28f, 0.24f, 0.2f)),
        new("ArmorTunic1", new Color(0.7f, 0.58f, 0.3f), new Color(0.4f, 0.3f, 0.2f), new Color(0.32f, 0.26f, 0.18f)),
    };

    private static readonly string[] menHair = { "Hair3", "Hair8", "Hair18", "Hair24", "Hair5", "Hair11" };
    private static readonly string[] womenHair = { "Hair14", "Hair20", "Hair16", "Hair12", "Hair31" };
    private static readonly string[] beards = { "Beard2", "Beard9", "Beard13", "Beard4", "Beard5", "Beard12", "" };

    // Hair from flaxen to near black, and the red of the madder-haired.
    private static readonly Color[] hairColours =
    {
        new(0.82f, 0.66f, 0.4f), new(0.55f, 0.38f, 0.2f), new(0.3f, 0.2f, 0.12f), new(0.5f, 0.2f, 0.08f),
        new(0.15f, 0.12f, 0.1f), new(0.86f, 0.72f, 0.48f),
    };

    /// <summary>The gear a huskarl comes with: iron mail and helmet, a sword and a shield.</summary>
    public static readonly string[] HuskarlGear = { "ArmorIronChest", "ArmorIronLegs", "HelmetIron", "SwordIron", "ShieldBanded" };

    /// <summary>
    /// Registers the clothes' names. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglishNameAndDescription(TunicKey, "Farmhand's Tunic", "A plain wool tunic, dyed on the farm.");
        Translations.AddEnglishNameAndDescription(TrousersKey, "Farmhand's Trousers", "Plain trousers for work on the farm.");
    }

    /// <summary>
    /// Makes the farm clothes. Call when the vanilla prefabs are available.
    /// </summary>
    public static void CreateClothes()
    {
        for (int i = 0; i < outfits.Length; i++)
        {
            Outfit outfit = outfits[i];
            TraderClothes.Add(TunicName(i), outfit.Tunic, TunicKey, TraderClothes.Dye(outfit.Cloth, outfit.Trim, keepLeather: true));
            TraderClothes.Add(TrousersName(i), "ArmorTrollLeatherLegs", TrousersKey, TraderClothes.Dye(outfit.Legs, null, keepLeather: true));
        }
    }

    /// <summary>
    /// The clothes a new farmhand comes with, picked from the seed.
    /// </summary>
    /// <param name="seed">The member's seed.</param>
    /// <returns>The tunic's and the trousers' prefab names.</returns>
    public static string[] FarmhandClothes(int seed)
    {
        int outfit = Pick(seed, 3, outfits.Length);
        return new[] { TunicName(outfit), TrousersName(outfit) };
    }

    /// <summary>
    /// Writes body, hair, beard and colours into the member's network data.
    /// </summary>
    /// <param name="zdo">The member's network data.</param>
    /// <param name="seed">The member's seed.</param>
    /// <param name="female">Whether the member is a woman.</param>
    public static void WriteBody(ZDO zdo, int seed, bool female)
    {
        string hair = female ? womenHair[Pick(seed, 5, womenHair.Length)] : menHair[Pick(seed, 5, menHair.Length)];
        string beard = female ? string.Empty : beards[Pick(seed, 7, beards.Length)];
        Color colour = hairColours[Pick(seed, 11, hairColours.Length)];
        zdo.Set(ZDOVars.s_modelIndex, female ? 1 : 0);
        zdo.Set(ZDOVars.s_skinColor, Vector3.one);
        zdo.Set(ZDOVars.s_hairColor, new Vector3(colour.r, colour.g, colour.b));
        zdo.Set(ZDOVars.s_hairItem, hair.GetStableHashCode());
        zdo.Set(ZDOVars.s_beardItem, string.IsNullOrEmpty(beard) ? 0 : beard.GetStableHashCode());
        zdo.Set(ZDOVars.s_noBeard, string.IsNullOrEmpty(beard));
    }

    private static string TunicName(int outfit) => $"WhiteHilt_HouseholdTunic_{outfit}";

    private static string TrousersName(int outfit) => $"WhiteHilt_HouseholdTrousers_{outfit}";

    // One choice from the seed; each kind of choice mixes the seed with its own salt, so hair and clothes do not go
    // together.
    private static int Pick(int seed, int salt, int count)
    {
        uint mixed = unchecked((uint)seed * 2654435761u + (uint)salt * 40503u);
        return (int)((mixed >> 13) % (uint)count);
    }

    private sealed class Outfit
    {
        public Outfit(string tunic, Color cloth, Color trim, Color legs)
        {
            Tunic = tunic;
            Cloth = cloth;
            Trim = trim;
            Legs = legs;
        }

        public string Tunic { get; }

        public Color Cloth { get; }

        public Color Trim { get; }

        public Color Legs { get; }
    }
}
