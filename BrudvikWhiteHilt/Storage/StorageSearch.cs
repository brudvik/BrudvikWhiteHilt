using BepInEx.Configuration;
using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Storage;

/// <summary>
/// Searching the chests around you: type part of an item's name, and every chest you may open that holds it glows and
/// gets a pin on the map, for a while after the window closes too. The list shows how many of each match there are.
/// </summary>
public class StorageSearch : MonoBehaviour
{
    private const string Section = "Storage";
    private const float Width = 380f;
    private const float Height = 470f;
    private const float RefreshSeconds = 1f;
    private const float HighlightSeconds = 0.2f;
    private const int ShownRows = 16;

    private static readonly List<Container> marked = new();
    private static readonly List<Minimap.PinData> pins = new();

    private static StorageSearch instance;
    private static ConfigEntry<float> range;
    private static ConfigEntry<float> markSeconds;
    private static ConfigEntry<KeyboardShortcut> key;
    private static float markUntil;
    private static float nextHighlight;

    private InputField search;
    private Text results;
    private float nextRefresh;

    /// <summary>True while the window is open.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// Binds the config entries and adds the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        range = WhiteHiltConfig.BindAdminOnly(Section, "SearchRange", 100f, "How far from you chests are searched, in metres. Only chests in the loaded world are found.",
            new AcceptableValueRange<float>(10f, 300f));
        markSeconds = WhiteHiltConfig.BindLocal(Section, "MarkSeconds", 60f, "Seconds the found chests keep glowing and their map pins stay after the search closes.",
            new AcceptableValueRange<float>(0f, 600f));
        key = WhiteHiltConfig.BindLocal("Storage.Keys", "Search", new KeyboardShortcut(KeyCode.F9), "Opens or closes the chest search.");
        Translations.AddEnglish("whitehilt_storage_title", "Search the chests");
        Translations.AddEnglish("whitehilt_storage_placeholder", "Item name...");
        Translations.AddEnglish("whitehilt_storage_hint", "Type part of a name. The chests that hold it glow and show on the map.");
        Translations.AddEnglish("whitehilt_storage_row", "{0}   x{1}   in {2} chests");
        Translations.AddEnglish("whitehilt_storage_none", "Nothing like that in the chests within {0} m.");
        Translations.AddEnglish("whitehilt_storage_close", "Close");
    }

    /// <summary>
    /// Handles the key and keeps the found chests glowing. Call every frame.
    /// </summary>
    public static void Tick()
    {
        if (key != null && Player.m_localPlayer != null && BackpackInput.Pressed(key) && (IsOpen || !BackpackInput.Typing()))
        {
            Toggle();
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Toggle();
        }

        if (!IsOpen && marked.Count > 0 && Time.time > markUntil)
        {
            ClearMarks();
        }

        if (marked.Count > 0 && Time.time >= nextHighlight)
        {
            nextHighlight = Time.time + HighlightSeconds;
            foreach (Container container in marked)
            {
                WearNTear piece = container != null ? container.GetComponentInParent<WearNTear>() : null;
                if (piece != null)
                {
                    piece.Highlight();
                }
            }
        }
    }

    /// <summary>
    /// Opens the window, or closes it when it is open.
    /// </summary>
    public static void Toggle()
    {
        if (IsOpen)
        {
            instance.gameObject.SetActive(false);
            GUIManager.BlockInput(false);
            markUntil = Time.time + markSeconds.Value;
            return;
        }

        if (GUIManager.CustomGUIFront == null || Player.m_localPlayer == null)
        {
            return;
        }

        if (instance == null)
        {
            instance = Build();
        }

        instance.gameObject.SetActive(true);
        GUIManager.BlockInput(true);
        instance.search.ActivateInputField();
        instance.nextRefresh = 0f;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            Search();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            GUIManager.BlockInput(false);
        }
    }

    // Searches the chests in range for items whose name contains the text, lists them by amount and pins the chests on
    // the map.
    private void Search()
    {
        Player player = Player.m_localPlayer;
        string query = (search.text ?? string.Empty).Trim().ToLowerInvariant();
        ClearMarks();
        if (player == null || query.Length < 2)
        {
            results.text = Localization.instance.Localize("$whitehilt_storage_hint");
            return;
        }

        Dictionary<string, (int Count, HashSet<Container> Chests)> found = new();
        float reach = range.Value;
        foreach (Container container in NearbyContainers.Registered())
        {
            if (!CanSearch(player, container, reach))
            {
                continue;
            }

            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
            {
                string name = Localization.instance.Localize(item.m_shared.m_name);
                if (!name.ToLowerInvariant().Contains(query))
                {
                    continue;
                }

                if (!found.TryGetValue(name, out var entry))
                {
                    entry = (0, new HashSet<Container>());
                }

                entry.Chests.Add(container);
                found[name] = (entry.Count + item.m_stack, entry.Chests);
                if (!marked.Contains(container))
                {
                    marked.Add(container);
                }
            }
        }

        if (found.Count == 0)
        {
            results.text = string.Format(Localization.instance.Localize("$whitehilt_storage_none"), Mathf.RoundToInt(reach));
            return;
        }

        string row = Localization.instance.Localize("$whitehilt_storage_row");
        results.text = string.Join("\n", found.OrderByDescending(entry => entry.Value.Count).Take(ShownRows)
            .Select(entry => string.Format(row, entry.Key, entry.Value.Count, entry.Value.Chests.Count)));
        AddPins();
    }

    private static bool CanSearch(Player player, Container container, float reach)
    {
        if (container == null || container.m_nview == null || !container.m_nview.IsValid())
        {
            return false;
        }

        Vector3 position = container.transform.position;
        return Vector3.Distance(position, player.transform.position) <= reach && container.CheckAccess(player.GetPlayerID())
            && PrivateArea.CheckAccess(position, 0f, false);
    }

    private static void AddPins()
    {
        if (Minimap.instance == null)
        {
            return;
        }

        foreach (Container container in marked)
        {
            pins.Add(Minimap.instance.AddPin(container.transform.position, Minimap.PinType.Icon3, string.Empty, false, false));
        }
    }

    private static void ClearMarks()
    {
        marked.Clear();
        if (Minimap.instance != null)
        {
            foreach (Minimap.PinData pin in pins)
            {
                Minimap.instance.RemovePin(pin);
            }
        }

        pins.Clear();
    }

    // Builds the search panel once: a title, the search field, the results and a close button.
    private static StorageSearch Build()
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-Width / 2f - 40f, 0f), Width, Height, true);
        StorageSearch storage = panel.AddComponent<StorageSearch>();
        float inner = Width - 40f;
        Text title = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_storage_title"), panel.transform, new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -32f), GUIManager.Instance.AveriaSerifBold, 22, GUIManager.Instance.ValheimOrange, true, Color.black,
            inner, 34f, false).GetComponent<Text>();
        title.alignment = TextAnchor.MiddleCenter;
        storage.search = GUIManager.Instance.CreateInputField(panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f),
            InputField.ContentType.Standard, Localization.instance.Localize("$whitehilt_storage_placeholder"), 18, inner, 36f).GetComponent<InputField>();
        storage.search.onValueChanged.AddListener(_ => storage.nextRefresh = 0f);
        storage.results = GUIManager.Instance.CreateText(string.Empty, panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -110f - (Height - 190f) / 2f), GUIManager.Instance.AveriaSerif, 15, Color.white, true, Color.black, inner, Height - 190f, false)
            .GetComponent<Text>();
        storage.results.alignment = TextAnchor.UpperLeft;
        storage.results.verticalOverflow = VerticalWrapMode.Truncate;
        GameObject close = GUIManager.Instance.CreateButton(Localization.instance.Localize("$whitehilt_storage_close"), panel.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), 160f, 34f);
        close.GetComponent<Button>().onClick.AddListener(Toggle);
        return storage;
    }
}
