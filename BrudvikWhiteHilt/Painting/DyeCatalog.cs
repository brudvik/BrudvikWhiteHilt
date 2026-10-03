using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Painting;

/// <summary>
/// The food and animal products that colour paint, and the coloured White Hilt forageables. The colour of each is measured once
/// from its icon; a mix of up to three of them, one to three of each, is searched for the colour closest to the one wanted.
/// </summary>
public static class DyeCatalog
{
    /// <summary>The binder every pot needs, besides its dyes.</summary>
    public const string Binder = "Resin";

    private const int IconSize = 32;
    private const int MaxOfEach = 3;
    private const int MaxKinds = 3;

    private static readonly string[] dyePrefabs =
    {
        // Meadows
        "Raspberry", "Mushroom", "Dandelion", "Honey", "RawMeat", "NeckTail", "DeerMeat", "DeerHide", "LeatherScraps",
        "Feathers", "BoneFragments", "Resin", "Coal",

        // Black Forest
        "Blueberries", "Carrot", "MushroomYellow", "Thistle", "TrollHide", "GreydwarfEye", "QueensJam", "CarrotSoup",

        // Swamp
        "Turnip", "Bloodbag", "Guck", "Ooze", "Entrails", "WitheredBone", "Root", "BlackSoup", "TurnipStew",
        Items.Foraging.SphagnumMoss.SphagnumMoss.PrefabName, Items.Foraging.BogBean.BogBean.PrefabName, Items.Foraging.LabradorTea.LabradorTea.PrefabName,
        Items.Foraging.Cattail.Cattail.PrefabName, Items.Foraging.Meadowsweet.Meadowsweet.PrefabName, Items.Foraging.BogIron.BogIron.PrefabName,
        Items.Foraging.Peat.Peat.PrefabName,

        // Mountains
        Items.Foraging.Juniper.Juniper.PrefabName, Items.Foraging.Angelica.Angelica.PrefabName, Items.Foraging.IcelandMoss.IcelandMoss.PrefabName,
        Items.Foraging.WolfLichen.WolfLichen.PrefabName, Items.Foraging.MountainSorrel.MountainSorrel.PrefabName,
        Items.Foraging.RockLichen.RockLichen.PrefabName,

        // Plains
        Items.Foraging.Rosehips.Rosehips.PrefabName, Items.Foraging.Yarrow.Yarrow.PrefabName, Items.Foraging.Caraway.Caraway.PrefabName,

        // Ocean, once Bonemass is slain
        Kraken.KrakenRegistry.InkName
    };

    private static List<Dye> dyes;

    /// <summary>
    /// A dye and the colour it gives.
    /// </summary>
    public sealed class Dye
    {
        /// <summary>Prefab name.</summary>
        public string Prefab;

        /// <summary>Shared item name, e.g. $item_raspberries.</summary>
        public string Name;

        /// <summary>The colour it gives, in linear space.</summary>
        public Color Linear;
    }

    /// <summary>
    /// A mix of dyes.
    /// </summary>
    public sealed class Mix
    {
        /// <summary>The dyes and how many of each.</summary>
        public List<(Dye Dye, int Amount)> Parts = new();

        /// <summary>The colour the mix gives.</summary>
        public Color32 Result;

        /// <summary>How close it is, 0 to 100.</summary>
        public float Match;

        /// <summary>True when the player has everything, the binder included.</summary>
        public bool Available;
    }

    /// <summary>
    /// The dyes, measured on first use. Empty on a server without graphics.
    /// </summary>
    public static IReadOnlyList<Dye> Dyes
    {
        get
        {
            dyes ??= Measure();
            return dyes;
        }
    }

    /// <summary>
    /// How many of an item the player has, in the inventory and in nearby chests.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="name">Shared item name.</param>
    /// <returns>The count.</returns>
    public static int Have(Player player, string name)
    {
        int count = player.GetInventory().CountItems(name);
        if (NearbyContainers.IsActive(NearbyContainers.Use.Crafting))
        {
            count += NearbyContainers.Count(NearbyContainers.Use.Crafting, name);
        }

        return count;
    }

    /// <summary>
    /// Takes an item from the inventory first, then from nearby chests.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="name">Shared item name.</param>
    /// <param name="amount">How many.</param>
    public static void Take(Player player, string name, int amount)
    {
        Inventory inventory = player.GetInventory();
        int own = Mathf.Min(amount, inventory.CountItems(name));
        if (own > 0)
        {
            inventory.RemoveItem(name, own);
        }

        if (amount > own && NearbyContainers.IsActive(NearbyContainers.Use.Crafting))
        {
            NearbyContainers.Take(NearbyContainers.Use.Crafting, name, amount - own);
        }
    }

    /// <summary>
    /// The best mix for a colour: among what the player has if that comes close enough, else among all dyes.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="target">The colour wanted.</param>
    /// <returns>The mix, or null if there are no dyes at all.</returns>
    public static Mix Suggest(Player player, Color32 target)
    {
        if (Dyes.Count == 0)
        {
            return null;
        }

        string binderName = BinderName();
        bool haveBinder = binderName != null && Have(player, binderName) >= 1;
        Dictionary<Dye, int> have = Dyes.ToDictionary(dye => dye, dye => Have(player, dye.Name) - (dye.Name == binderName ? 1 : 0));
        Mix own = haveBinder ? Search(target, Dyes.Where(dye => have[dye] > 0).ToList(), have) : null;
        if (own != null && own.Match >= 80f)
        {
            own.Available = true;
            return own;
        }

        Mix any = Search(target, Dyes.ToList(), null);
        if (own != null && own.Match >= any.Match - 2f)
        {
            own.Available = true;
            return own;
        }

        return any;
    }

    /// <summary>
    /// The shared name of the binder, e.g. $item_resin.
    /// </summary>
    /// <returns>The name, or null before the game has loaded.</returns>
    public static string BinderName()
    {
        GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(Binder) : null;
        return prefab != null ? prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name : null;
    }

    // Tries every mix of up to three dyes with one to three of each; with a stock, only what the player has.
    private static Mix Search(Color32 target, List<Dye> candidates, Dictionary<Dye, int> stock)
    {
        Color want = ((Color)target).linear;
        Mix best = null;
        float bestDistance = float.MaxValue;
        int count = candidates.Count;
        int[] amounts = new int[MaxKinds];
        List<int> picks = new(MaxKinds);
        for (int a = 0; a < count; a++)
        {
            picks.Clear();
            picks.Add(a);
            TryAmounts(picks, 0, amounts, candidates, stock, want, ref best, ref bestDistance);
            for (int b = a + 1; b < count; b++)
            {
                picks.Clear();
                picks.Add(a);
                picks.Add(b);
                TryAmounts(picks, 0, amounts, candidates, stock, want, ref best, ref bestDistance);
                for (int c = b + 1; c < count; c++)
                {
                    picks.Clear();
                    picks.Add(a);
                    picks.Add(b);
                    picks.Add(c);
                    TryAmounts(picks, 0, amounts, candidates, stock, want, ref best, ref bestDistance);
                }
            }
        }

        if (best != null)
        {
            best.Match = Mathf.Clamp(100f - bestDistance * 100f, 0f, 100f);
        }

        return best;
    }

    private static void TryAmounts(List<int> picks, int index, int[] amounts, List<Dye> candidates, Dictionary<Dye, int> stock, Color want,
        ref Mix best, ref float bestDistance)
    {
        if (index == picks.Count)
        {
            Color sum = Color.clear;
            int total = 0;
            for (int i = 0; i < picks.Count; i++)
            {
                sum += candidates[picks[i]].Linear * amounts[i];
                total += amounts[i];
            }

            Color mixed = sum / total;
            float distance = Distance(mixed, want);
            if (distance < bestDistance - 0.0001f || (Mathf.Abs(distance - bestDistance) <= 0.0001f && best != null && total < best.Parts.Sum(part => part.Amount)))
            {
                bestDistance = distance;
                Color gamma = mixed.gamma;
                gamma.a = 1f;
                best = new Mix { Result = gamma };
                for (int i = 0; i < picks.Count; i++)
                {
                    best.Parts.Add((candidates[picks[i]], amounts[i]));
                }
            }

            return;
        }

        Dye dye = candidates[picks[index]];
        int most = stock != null ? Mathf.Min(MaxOfEach, stock[dye]) : MaxOfEach;
        for (int amount = 1; amount <= most; amount++)
        {
            amounts[index] = amount;
            TryAmounts(picks, index + 1, amounts, candidates, stock, want, ref best, ref bestDistance);
        }
    }

    // Weighted like the eye: green counts most, blue least. 0 is the same colour, about 1 is black against white.
    private static float Distance(Color a, Color b)
    {
        Color ga = a.gamma;
        Color gb = b.gamma;
        float dr = ga.r - gb.r;
        float dg = ga.g - gb.g;
        float db = ga.b - gb.b;
        return Mathf.Sqrt((2f * dr * dr + 4f * dg * dg + 3f * db * db) / 9f);
    }

    private static List<Dye> Measure()
    {
        List<Dye> measured = new();
        if (ObjectDB.instance == null || VisualHelper.IsHeadless)
        {
            return measured;
        }

        foreach (string prefabName in dyePrefabs)
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName);
            ItemDrop.ItemData.SharedData shared = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared : null;
            Sprite icon = shared != null && shared.m_icons.Length > 0 ? shared.m_icons[0] : null;
            if (icon == null)
            {
                Jotunn.Logger.LogWarning($"Paint: dye {prefabName} not found, skipped");
                continue;
            }

            measured.Add(new Dye { Prefab = prefabName, Name = shared.m_name, Linear = IconColor(icon).linear });
        }

        return measured;
    }

    // The average of the icon's solid pixels, leaning to the coloured ones, so a red berry with a green leaf reads red.
    private static Color IconColor(Sprite icon)
    {
        Color32[] pixels = VisualHelper.ReadPixels(icon.texture, IconSize, IconSize, icon.textureRect);
        Color sum = Color.clear;
        float weights = 0f;
        foreach (Color32 pixel in pixels)
        {
            if (pixel.a < 128)
            {
                continue;
            }

            Color color = pixel;
            Color.RGBToHSV(color, out _, out float saturation, out float brightness);
            float weight = 0.25f + saturation * brightness;
            sum += color.linear * weight;
            weights += weight;
        }

        if (weights <= 0f)
        {
            return Color.gray;
        }

        Color average = (sum / weights).gamma;
        average.a = 1f;
        return average;
    }
}
