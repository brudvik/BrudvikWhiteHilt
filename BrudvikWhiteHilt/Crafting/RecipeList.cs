using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Makes the recipe list at every station easier to find one's way in: a search field over names and materials, a
/// Craftable button that hides what cannot be made now, tabs by kind of item, favourites at the top (right-click a
/// recipe) and the list sorted by name or by what the items give (<see cref="RecipeStats"/>): a food's health, stamina
/// or eitr, the best food for the player's profile, or the strength of gear, shown beside each recipe too. It works on
/// the list vanilla has just built, so the game's own stations and other mods' recipes are included.
/// </summary>
public static class RecipeList
{
    private const string BarName = "WhiteHiltRecipeTools";
    private const string FavoritesKey = "WhiteHiltRecipeFavorites";
    private const char FavoriteSeparator = ';';
    private const float RowHeight = 28f;
    private const float Gap = 4f;
    private const float BarHeight = RowHeight * 2f + Gap * 3f;
    private const float CraftableWidth = 96f;
    private const float SortWidth = 92f;
    private const string StatsName = "WhiteHiltStats";
    private const float StatsGap = 6f;
    private const float AllTabWidth = 52f;
    private const string Star = "<color=#FFC94D>★</color> ";

    // Each tab's icon, taken from a vanilla item everyone knows.
    private static readonly (RecipeCategory Category, string Icon)[] tabs =
    {
        (RecipeCategory.All, null),
        (RecipeCategory.Weapons, "SwordBronze"),
        (RecipeCategory.Armor, "HelmetBronze"),
        (RecipeCategory.Tools, "Hammer"),
        (RecipeCategory.Ammo, "ArrowWood"),
        (RecipeCategory.Food, "CookedMeat"),
        (RecipeCategory.Materials, "Bronze")
    };

    private static readonly Color OffColor = new(0.75f, 0.75f, 0.75f);
    private static readonly Color DimIcon = new(1f, 1f, 1f, 0.6f);
    private static readonly Dictionary<RecipeCategory, (Button Button, Image Icon)> tabButtons = new();
    private static readonly HashSet<RecipeCategory> present = new();
    private static readonly Dictionary<Recipe, string> names = new();
    private static readonly Dictionary<(Recipe Recipe, int Quality), ItemStats> stats = new();
    private static readonly Dictionary<string, ItemDrop> meads = new();
    private static ZNetScene meadsFor;

    private static InventoryGui builtFor;
    private static RectTransform bar;
    private static RectTransform scrollRect;
    private static RectTransform scrollbarRect;
    private static float scrollTop;
    private static float scrollbarTop;
    private static float baseListSize;
    private static bool barShown;
    private static GameObject tooltipPrefab;
    private static InputField search;
    private static Text craftableLabel;
    private static Text sortLabel;
    private static RecipeCategory selectedTab = RecipeCategory.All;
    private static bool inputBlocked;
    private static bool clearing;
    private static Player favoritesOf;
    private static HashSet<string> favorites = new();

    /// <summary>True while the search field above the recipe list has the keyboard.</summary>
    public static bool Typing => barShown && search != null && search.isFocused;

    /// <summary>
    /// Filters, sorts and marks the recipes vanilla has just listed, and lays them out again. Call after vanilla
    /// builds the recipe list.
    /// </summary>
    /// <param name="gui">The inventory screen.</param>
    public static void AfterRecipeList(InventoryGui gui)
    {
        EnsureBuilt(gui);
        ApplyLayout(gui);
        List<InventoryGui.RecipeDataPair> list = gui.m_availableRecipes;
        if (barShown)
        {
            Filter(list);
        }

        Order(list);
        float space = gui.m_recipeListSpace;
        for (int i = 0; i < list.Count; i++)
        {
            GameObject element = list[i].InterfaceElement;
            ((RectTransform)element.transform).anchoredPosition = new Vector2(0f, i * -space);
            Decorate(element, list[i]);
        }

        gui.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(gui.m_recipeListBaseSize, list.Count * space));
    }

    /// <summary>
    /// Shows or hides the tools when the setting changes, and keeps the game's keys quiet while typing in the search
    /// field. Call every frame the crafting panel is updated.
    /// </summary>
    /// <param name="gui">The inventory screen.</param>
    public static void Tick(InventoryGui gui)
    {
        if (builtFor == gui && barShown != (bar != null && CraftingPanelSettings.RecipeTools.Value))
        {
            Refresh();
        }

        SetInputBlocked(Typing);
    }

    /// <summary>
    /// Starts the next visit with an empty search on the All tab. Call when the inventory closes.
    /// </summary>
    public static void OnHide()
    {
        ClearSearch();
        selectedTab = RecipeCategory.All;
        SetInputBlocked(false);
    }

    /// <summary>
    /// Whether a recipe is found by a search: every word must be in its name or in the name of one of its materials.
    /// </summary>
    /// <param name="words">The search, in lower case and split into words.</param>
    /// <param name="name">The recipe's name, as shown.</param>
    /// <param name="materials">The names of its materials, as shown.</param>
    /// <returns>True when the recipe is found, or the search is empty.</returns>
    public static bool Matches(IList<string> words, string name, IEnumerable<string> materials)
    {
        string[] haystack = new[] { name ?? string.Empty }.Concat(materials ?? Enumerable.Empty<string>())
            .Select(text => (text ?? string.Empty).ToLowerInvariant()).ToArray();
        return words.All(word => haystack.Any(text => text.Contains(word)));
    }

    /// <summary>
    /// Splits a search into lower-case words.
    /// </summary>
    /// <param name="query">What was typed.</param>
    /// <returns>The words, none for an empty search.</returns>
    public static string[] Words(string query)
    {
        return (query ?? string.Empty).ToLowerInvariant().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Reads the favourite recipes as saved on the character.
    /// </summary>
    /// <param name="text">The saved text, the recipes' names separated by semicolons.</param>
    /// <returns>The names.</returns>
    public static HashSet<string> ParseFavorites(string text)
    {
        return new HashSet<string>((text ?? string.Empty).Split(new[] { FavoriteSeparator }, StringSplitOptions.RemoveEmptyEntries)
            .Select(name => name.Trim()).Where(name => name.Length > 0), StringComparer.Ordinal);
    }

    /// <summary>
    /// Writes the favourite recipes to be saved on the character, in a fixed order.
    /// </summary>
    /// <param name="recipes">The recipes' names.</param>
    /// <returns>The names separated by semicolons.</returns>
    public static string FormatFavorites(IEnumerable<string> recipes)
    {
        return string.Join(FavoriteSeparator.ToString(), recipes.OrderBy(name => name, StringComparer.Ordinal));
    }

    /// <summary>
    /// Makes a recipe a favourite, or no longer one, on the local player's character.
    /// </summary>
    /// <param name="recipe">The recipe.</param>
    public static void ToggleFavorite(Recipe recipe)
    {
        Player player = Player.m_localPlayer;
        if (player == null || recipe == null)
        {
            return;
        }

        HashSet<string> set = Favorites(player);
        if (!set.Remove(recipe.name))
        {
            set.Add(recipe.name);
        }

        if (set.Count == 0)
        {
            player.m_customData.Remove(FavoritesKey);
        }
        else
        {
            player.m_customData[FavoritesKey] = FormatFavorites(set);
        }

        Refresh();
    }

    private static HashSet<string> Favorites(Player player)
    {
        if (favoritesOf != player)
        {
            favoritesOf = player;
            favorites = ParseFavorites(player != null && player.m_customData.TryGetValue(FavoritesKey, out string text) ? text : null);
        }

        return favorites;
    }

    private static bool IsFavorite(Recipe recipe)
    {
        return recipe != null && Favorites(Player.m_localPlayer).Contains(recipe.name);
    }

    // Builds the list again with vanilla's own method, which ends in AfterRecipeList.
    private static void Refresh()
    {
        InventoryGui gui = InventoryGui.instance;
        if (gui != null && InventoryGui.IsVisible() && Player.m_localPlayer != null)
        {
            gui.UpdateCraftingPanel();
        }
    }

    // Takes out what is not on the chosen tab, cannot be crafted while Craftable is on, or is not found by the search.
    // The tabs with nothing in them at this station are hidden, and a hidden tab falls back to All.
    private static void Filter(List<InventoryGui.RecipeDataPair> list)
    {
        present.Clear();
        foreach (InventoryGui.RecipeDataPair pair in list)
        {
            present.Add(RecipeCategories.Of(pair.Recipe));
        }

        if (selectedTab != RecipeCategory.All && !present.Contains(selectedTab))
        {
            selectedTab = RecipeCategory.All;
        }

        UpdateButtons();
        string[] words = Words(search.text);
        bool onlyCraftable = CraftingPanelSettings.OnlyCraftable.Value;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            InventoryGui.RecipeDataPair pair = list[i];
            bool keep = (selectedTab == RecipeCategory.All || RecipeCategories.Of(pair.Recipe) == selectedTab)
                && (!onlyCraftable || pair.CanCraft)
                && (words.Length == 0 || Matches(words, Name(pair.Recipe), Materials(pair.Recipe)));
            if (!keep)
            {
                Object.Destroy(pair.InterfaceElement);
                list.RemoveAt(i);
            }
        }
    }

    // Favourites first, then by what the items give when the list is sorted by that, then by name with the highest
    // quality first; or in the game's order when sorting by name is off.
    private static void Order(List<InventoryGui.RecipeDataPair> list)
    {
        names.Clear();
        stats.Clear();
        RecipeSort sort = CraftingPanelSettings.SortBy.Value;
        if (sort == RecipeSort.Name && !CraftingPanelSettings.SortByName.Value)
        {
            List<InventoryGui.RecipeDataPair> first = list.Where(pair => IsFavorite(pair.Recipe)).ToList();
            if (first.Count > 0 && first.Count < list.Count)
            {
                List<InventoryGui.RecipeDataPair> rest = list.Where(pair => !IsFavorite(pair.Recipe)).ToList();
                list.Clear();
                list.AddRange(first);
                list.AddRange(rest);
            }

            return;
        }

        bool craftableFirst = CraftingPanelSettings.CraftableFirst.Value;
        (float Health, float Stamina, float Eitr) weights = CraftingPanelSettings.Weights;
        list.Sort((a, b) =>
        {
            int order = IsFavorite(b.Recipe).CompareTo(IsFavorite(a.Recipe));
            if (order == 0 && craftableFirst)
            {
                order = b.CanCraft.CompareTo(a.CanCraft);
            }

            if (order == 0 && sort != RecipeSort.Name)
            {
                order = RecipeStats.Compare(RecipeStats.Key(Stats(a), sort, weights), RecipeStats.Key(Stats(b), sort, weights));
            }

            if (order == 0)
            {
                order = string.Compare(Name(a.Recipe), Name(b.Recipe), StringComparison.CurrentCultureIgnoreCase);
            }

            if (order == 0 && a.ItemData != null && b.ItemData != null)
            {
                order = b.ItemData.m_quality.CompareTo(a.ItemData.m_quality);
            }

            return order;
        });
    }

    private static string Name(Recipe recipe)
    {
        if (!names.TryGetValue(recipe, out string name))
        {
            name = recipe.m_item != null ? Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name) : recipe.name;
            names[recipe] = name;
        }

        return name;
    }

    private static IEnumerable<string> Materials(Recipe recipe)
    {
        return recipe.m_resources.Where(req => req != null && req.m_resItem != null)
            .Select(req => Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name));
    }

    // What a recipe's item gives, at the quality the recipe makes: an upgrade makes the next one.
    private static ItemStats Stats(InventoryGui.RecipeDataPair pair)
    {
        ItemDrop item = pair.Recipe != null ? pair.Recipe.m_item : null;
        if (item == null)
        {
            return default;
        }

        int quality = pair.ItemData != null ? Mathf.Min(pair.ItemData.m_quality + 1, item.m_itemData.m_shared.m_maxQuality) : 1;
        if (!stats.TryGetValue((pair.Recipe, quality), out ItemStats found))
        {
            found = Read(item, quality, true);
            stats[(pair.Recipe, quality)] = found;
        }

        return found;
    }

    // A food's health, stamina, eitr and time; a weapon's or ammunition's damage (not chopping or mining), armour's
    // armour, a shield's block; a potion's effect and time. A mead base shows the mead it ferments into.
    private static ItemStats Read(ItemDrop item, int quality, bool followMead)
    {
        ItemDrop.ItemData.SharedData shared = item.m_itemData.m_shared;
        if (shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f)
        {
            return new ItemStats(shared.m_food, shared.m_foodStamina, shared.m_foodEitr, shared.m_foodBurnTime / 60f);
        }

        int levels = Mathf.Max(0, quality - 1);
        switch (shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.Shield:
                return new ItemStats(0f, 0f, 0f, 0f, shared.m_blockPower + shared.m_blockPowerPerLevel * levels, StrengthKind.Block);
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Hands:
            case ItemDrop.ItemData.ItemType.Shoulder:
                float armor = shared.m_armor + shared.m_armorPerLevel * levels;
                return armor > 0f ? new ItemStats(0f, 0f, 0f, 0f, armor, StrengthKind.Armor) : default;
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                HitData.DamageTypes damage = shared.m_damages.Clone();
                damage.Add(shared.m_damagesPerLevel, levels);
                float total = damage.GetTotalDamage() - damage.m_chop - damage.m_pickaxe;
                return total > 0f ? new ItemStats(0f, 0f, 0f, 0f, total, StrengthKind.Damage) : default;
        }

        StatusEffect effect = shared.m_consumeStatusEffect;
        if (effect != null && !string.IsNullOrEmpty(effect.m_name))
        {
            return new ItemStats(0f, 0f, 0f, effect.m_ttl / 60f, effect: Localization.instance.Localize(effect.m_name));
        }

        ItemDrop mead = followMead ? Mead(item.gameObject.name) : null;
        return mead != null ? Read(mead, 1, false) : default;
    }

    // The mead a mead base ferments into, from the fermenters the game knows.
    private static ItemDrop Mead(string meadBase)
    {
        ZNetScene scene = ZNetScene.instance;
        if (scene != meadsFor)
        {
            meadsFor = scene;
            meads.Clear();
            foreach (GameObject prefab in scene != null ? scene.m_prefabs : new List<GameObject>())
            {
                Fermenter fermenter = prefab != null ? prefab.GetComponent<Fermenter>() : null;
                foreach (Fermenter.ItemConversion conversion in fermenter != null ? fermenter.m_conversion : new List<Fermenter.ItemConversion>())
                {
                    if (conversion?.m_from != null && conversion.m_to != null)
                    {
                        meads[conversion.m_from.gameObject.name] = conversion.m_to;
                    }
                }
            }
        }

        return meads.TryGetValue(meadBase, out ItemDrop mead) ? mead : null;
    }

    // A star before a favourite's name, what the item gives at the right of the line, and a right-click on any recipe
    // to make it a favourite or not.
    private static void Decorate(GameObject element, InventoryGui.RecipeDataPair pair)
    {
        Recipe recipe = pair.Recipe;
        if (CraftingPanelSettings.ShowStats.Value)
        {
            (string, string, string) labels = (Localization.instance.Localize("$whitehilt_recipes_stat_damage"),
                Localization.instance.Localize("$whitehilt_recipes_stat_armor"), Localization.instance.Localize("$whitehilt_recipes_stat_block"));
            ShowStats(element, RecipeStats.Format(Stats(pair), CraftingPanelSettings.SortBy.Value, CraftingPanelSettings.Weights, labels));
        }

        if (IsFavorite(recipe))
        {
            TMP_Text name = element.transform.Find("name")?.GetComponent<TMP_Text>();
            if (name != null && !name.text.StartsWith(Star, StringComparison.Ordinal))
            {
                name.text = Star + name.text;
            }
        }

        FavoriteClick click = element.GetComponent<FavoriteClick>() ?? element.AddComponent<FavoriteClick>();
        click.Recipe = recipe;
    }

    // A copy of the line's name text at its right end, holding the stats; the name gives up that much room. The game
    // makes the lines anew every time it lists the recipes, so this runs once per line.
    private static void ShowStats(GameObject element, string text)
    {
        TMP_Text name = element.transform.Find("name")?.GetComponent<TMP_Text>();
        if (name == null || string.IsNullOrEmpty(text) || element.transform.Find(StatsName) != null)
        {
            return;
        }

        TMP_Text line = Object.Instantiate(name, name.transform.parent);
        line.name = StatsName;
        line.text = text;
        line.fontSize = name.fontSize * 0.85f;
        line.color = Color.white;
        line.alignment = TextAlignmentOptions.MidlineRight;
        line.enableWordWrapping = false;
        line.raycastTarget = false;
        float width = line.GetPreferredValues(text).x;
        RectTransform room = name.rectTransform;
        room.offsetMax = new Vector2(room.offsetMax.x - width - StatsGap, room.offsetMax.y);
        name.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void CycleSort()
    {
        int count = Enum.GetValues(typeof(RecipeSort)).Length;
        CraftingPanelSettings.SortBy.Value = (RecipeSort)(((int)CraftingPanelSettings.SortBy.Value + 1) % count);
        ScrollToTop();
        Refresh();
    }

    private static string SortName()
    {
        return Localization.instance.Localize("$whitehilt_recipes_sort_" + CraftingPanelSettings.SortBy.Value.ToString().ToLowerInvariant());
    }

    private static void SelectTab(RecipeCategory category)
    {
        selectedTab = category;
        ScrollToTop();
        Refresh();
    }

    private static void ToggleCraftable()
    {
        CraftingPanelSettings.OnlyCraftable.Value = !CraftingPanelSettings.OnlyCraftable.Value;
        ScrollToTop();
        Refresh();
    }

    private static void OnSearchChanged()
    {
        if (!clearing)
        {
            ScrollToTop();
            Refresh();
        }
    }

    private static void ScrollToTop()
    {
        if (builtFor != null && builtFor.m_recipeListScroll != null)
        {
            builtFor.m_recipeListScroll.value = 1f;
        }
    }

    private static void ClearSearch()
    {
        if (search == null)
        {
            return;
        }

        clearing = true;
        search.text = string.Empty;
        search.DeactivateInputField();
        clearing = false;
    }

    private static void UpdateButtons()
    {
        foreach (KeyValuePair<RecipeCategory, (Button Button, Image Icon)> tab in tabButtons)
        {
            bool show = tab.Key == RecipeCategory.All || present.Contains(tab.Key);
            if (tab.Value.Button.gameObject.activeSelf != show)
            {
                tab.Value.Button.gameObject.SetActive(show);
            }

            // Like vanilla's Craft and Upgrade tabs, the chosen tab is the one that cannot be pressed.
            bool chosen = tab.Key == selectedTab;
            tab.Value.Button.interactable = !chosen;
            if (tab.Value.Icon != null)
            {
                tab.Value.Icon.color = chosen ? Color.white : DimIcon;
            }
        }

        craftableLabel.color = CraftingPanelSettings.OnlyCraftable.Value ? GUIManager.Instance.ValheimOrange : OffColor;
        sortLabel.text = SortName();
        sortLabel.color = CraftingPanelSettings.SortBy.Value != RecipeSort.Name ? GUIManager.Instance.ValheimOrange : OffColor;
    }

    private static void SetInputBlocked(bool block)
    {
        if (block != inputBlocked)
        {
            GUIManager.BlockInput(block);
            inputBlocked = block;
        }
    }

    // Makes room for the tools by moving the top of the recipe list down, or gives the room back.
    private static void ApplyLayout(InventoryGui gui)
    {
        bool show = bar != null && CraftingPanelSettings.RecipeTools.Value;
        if (show == barShown)
        {
            return;
        }

        barShown = show;
        if (bar == null)
        {
            return;
        }

        bar.gameObject.SetActive(show);
        float shift = show ? BarHeight : 0f;
        scrollRect.offsetMax = new Vector2(scrollRect.offsetMax.x, scrollTop - shift);
        if (scrollbarRect != null)
        {
            scrollbarRect.offsetMax = new Vector2(scrollbarRect.offsetMax.x, scrollbarTop - shift);
        }

        gui.m_recipeListBaseSize = baseListSize - shift;
        if (!show)
        {
            ClearSearch();
            selectedTab = RecipeCategory.All;
            SetInputBlocked(false);
        }
    }

    // Adds the tools above the recipe list, again when the inventory is rebuilt for another world. A failure is logged
    // and the list works as in vanilla, still sorted.
    private static void EnsureBuilt(InventoryGui gui)
    {
        if (builtFor == gui)
        {
            return;
        }

        builtFor = gui;
        bar = null;
        barShown = false;
        tabButtons.Clear();
        try
        {
            Build(gui);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"Could not add the search and tabs to the recipe list: {ex}");
            if (bar != null)
            {
                Object.Destroy(bar.gameObject);
                bar = null;
            }
        }
    }

    // Two rows in the room taken from the top of the list's scroll view: the search field and the Craftable button,
    // then the tabs. Laid out by layout groups, so they fit however wide the list is.
    private static void Build(InventoryGui gui)
    {
        ScrollRect scroll = gui.m_recipeListRoot.GetComponentInParent<ScrollRect>(true);
        if (scroll == null)
        {
            Jotunn.Logger.LogWarning("The recipe list has no scroll view; no search or tabs are added.");
            return;
        }

        scrollRect = (RectTransform)scroll.transform;
        scrollTop = scrollRect.offsetMax.y;
        Scrollbar scrollbar = gui.m_recipeListScroll;
        scrollbarRect = scrollbar != null && !scrollbar.transform.IsChildOf(scrollRect) ? (RectTransform)scrollbar.transform : null;
        scrollbarTop = scrollbarRect != null ? scrollbarRect.offsetMax.y : 0f;
        baseListSize = gui.m_recipeListBaseSize;
        tooltipPrefab = gui.m_playerGrid != null && gui.m_playerGrid.m_elementPrefab != null
            ? gui.m_playerGrid.m_elementPrefab.GetComponent<UITooltip>()?.m_tooltipPrefab
            : null;

        GameObject barObject = new(BarName, typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform rect = (RectTransform)barObject.transform;
        rect.SetParent(scrollRect.parent, false);
        rect.anchorMin = new Vector2(scrollRect.anchorMin.x, scrollRect.anchorMax.y);
        rect.anchorMax = scrollRect.anchorMax;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(scrollRect.offsetMin.x, scrollTop - BarHeight);
        rect.offsetMax = new Vector2(scrollRect.offsetMax.x, scrollTop);
        VerticalLayoutGroup rows = barObject.GetComponent<VerticalLayoutGroup>();
        rows.padding = new RectOffset(0, 0, (int)Gap, (int)Gap);
        rows.spacing = Gap;
        rows.childControlWidth = rows.childControlHeight = true;
        rows.childForceExpandWidth = rows.childForceExpandHeight = true;
        bar = rect;

        RectTransform top = Row("Search");
        search = GUIManager.Instance.CreateInputField(top, Vector2.zero, Vector2.zero, Vector2.zero, InputField.ContentType.Standard,
            Localization.instance.Localize("$whitehilt_recipes_search"), 15, 100f, RowHeight).GetComponent<InputField>();
        Size(search.gameObject, 1f, 0f);
        search.onValueChanged.AddListener(_ => OnSearchChanged());
        Tooltip(search.gameObject, "$whitehilt_recipes_search_tip");

        GameObject sort = GUIManager.Instance.CreateButton(SortName(), top, Vector2.zero, Vector2.zero, Vector2.zero, SortWidth, RowHeight);
        sort.name = "Sort";
        Size(sort, 0f, SortWidth);
        sort.GetComponent<Button>().onClick.AddListener(CycleSort);
        sortLabel = sort.GetComponentInChildren<Text>();
        sortLabel.resizeTextForBestFit = true;
        sortLabel.resizeTextMinSize = 9;
        sortLabel.resizeTextMaxSize = 14;
        Tooltip(sort, "$whitehilt_recipes_sort_tip");

        GameObject craftable = GUIManager.Instance.CreateButton(Localization.instance.Localize("$whitehilt_recipes_craftable"), top,
            Vector2.zero, Vector2.zero, Vector2.zero, CraftableWidth, RowHeight);
        craftable.name = "Craftable";
        Size(craftable, 0f, CraftableWidth);
        craftable.GetComponent<Button>().onClick.AddListener(ToggleCraftable);
        craftableLabel = craftable.GetComponentInChildren<Text>();
        craftableLabel.fontSize = 14;
        craftableLabel.resizeTextForBestFit = false;
        Tooltip(craftable, "$whitehilt_recipes_craftable_tip");

        RectTransform tabRow = Row("Tabs");
        foreach ((RecipeCategory category, string icon) in tabs)
        {
            AddTab(tabRow, category, IconOf(icon));
        }
    }

    private static RectTransform Row(string name)
    {
        GameObject row = new(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(bar, false);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = Gap;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        return (RectTransform)row.transform;
    }

    // A tab with an item's icon, or its name when the icon is missing. All is a little wider and always shows its name.
    private static void AddTab(RectTransform row, RecipeCategory category, Sprite icon)
    {
        string key = "$whitehilt_recipes_tab_" + category.ToString().ToLowerInvariant();
        GameObject tab = GUIManager.Instance.CreateButton(icon == null ? Localization.instance.Localize(key) : string.Empty, row,
            Vector2.zero, Vector2.zero, Vector2.zero, 40f, RowHeight);
        tab.name = "Tab" + category;
        bool all = category == RecipeCategory.All;
        Size(tab, all ? 0f : 1f, all ? AllTabWidth : 0f);
        Text label = tab.GetComponentInChildren<Text>();
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = 14;

        Image image = null;
        if (icon != null)
        {
            GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(tab.transform, false);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(3f, 3f);
            iconRect.offsetMax = new Vector2(-3f, -3f);
            image = iconObject.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        Button button = tab.GetComponent<Button>();
        button.onClick.AddListener(() => SelectTab(category));
        Tooltip(tab, key);
        tabButtons[category] = (button, image);
    }

    private static Sprite IconOf(string prefab)
    {
        if (prefab == null || ObjectDB.instance == null)
        {
            return null;
        }

        GameObject item = ObjectDB.instance.GetItemPrefab(prefab);
        ItemDrop drop = item != null ? item.GetComponent<ItemDrop>() : null;
        return drop != null ? drop.m_itemData.GetIcon() : null;
    }

    private static void Size(GameObject child, float flexibleWidth, float width)
    {
        LayoutElement element = child.GetComponent<LayoutElement>() ?? child.AddComponent<LayoutElement>();
        element.flexibleWidth = flexibleWidth;
        element.preferredWidth = width;
        element.minWidth = width > 0f ? width : 24f;
    }

    // The game's own tooltip, so the tools explain themselves on hover.
    private static void Tooltip(GameObject target, string key)
    {
        if (tooltipPrefab == null)
        {
            return;
        }

        UITooltip tooltip = target.AddComponent<UITooltip>();
        tooltip.m_tooltipPrefab = tooltipPrefab;
        tooltip.m_topic = string.Empty;
        tooltip.m_text = Localization.instance.Localize(key);
    }

    /// <summary>
    /// Makes a recipe a favourite, or no longer one, on a right-click. The left click still selects it.
    /// </summary>
    public class FavoriteClick : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>The recipe of this line in the list.</summary>
        public Recipe Recipe { get; set; }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ToggleFavorite(Recipe);
            }
        }
    }
}
