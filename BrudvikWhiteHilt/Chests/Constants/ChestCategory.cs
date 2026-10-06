#nullable enable annotations

namespace BrudvikWhiteHilt.Chests.Constants
{
    /// <summary>
    /// The item category a chest is filled with: one restocking chest (and its wall drawer) per category. The items of
    /// each are not listed by hand but sorted into these at runtime from what the game knows about them (item type,
    /// crafting station, drops and so on), see <see cref="Helpers.ItemSorter"/>, so items added by game updates and
    /// other
    /// mods find their chest too. Each category can still take extra items or leave some out through the config.
    /// </summary>
    public enum ChestCategory
    {
        /// <summary>No automatic contents; the chest only restocks what players put into it.</summary>
        None,

        /// <summary>Wood of every kind, from Wood to Yggdrasil and Ashwood.</summary>
        Wood,

        /// <summary>Stone, flint and the other quarried materials.</summary>
        Stone,

        /// <summary>Ores, metal bars and scrap.</summary>
        Metal,

        /// <summary>Food, fish and what is cooked.</summary>
        Food,

        /// <summary>Crafting materials that fit no narrower category.</summary>
        Material,

        /// <summary>Materials creatures drop: hides, pelts, feathers, bones and the like.</summary>
        Animal,

        /// <summary>Seeds and saplings for planting.</summary>
        Seed,

        /// <summary>Trophies.</summary>
        Trophy,

        /// <summary>Valuables: everything with a trade value, such as coins, gems and the bosses' drops.</summary>
        Treasure,

        /// <summary>Tools, torches and utility gear such as belts, and weapons whose skill is a tool skill (the pickaxe, the fishing rod).</summary>
        Tools,

        /// <summary>Helmets, chest and leg pieces, capes, gloves and trinkets.</summary>
        Armor,

        /// <summary>Weapons and shields.</summary>
        Weapon,

        /// <summary>Meads and potions: consumables without food value, and what is fermented.</summary>
        Potion
    }
}
