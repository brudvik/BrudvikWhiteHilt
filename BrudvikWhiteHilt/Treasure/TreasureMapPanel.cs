using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Navigation.Discoveries;
using BrudvikWhiteHilt.Navigation.Dowsing;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// The unrolled treasure map: the drawn land with its cross, the landmarks around it as symbols (named with enough
/// Exploration), a note at the bottom and a stamp once the treasure is plundered.
/// </summary>
public class TreasureMapPanel : MonoBehaviour
{
    private const float PanelWidth = 600f;
    private const float PanelHeight = 720f;
    private const float MapSize = 512f;
    private const float IconSize = 26f;
    private const int CacheSize = 6;

    private static readonly Color sepia = new(0.33f, 0.22f, 0.13f, 0.9f);
    private static readonly Color inkText = new(0.25f, 0.17f, 0.1f);
    private static readonly Dictionary<string, Texture2D> cache = new();
    private static readonly List<string> cacheOrder = new();
    private static TreasureMapPanel instance;

    private readonly List<GameObject> overlays = new();
    private RawImage picture;
    private RectTransform overlayRoot;
    private Text title;
    private Text note;
    private Text status;
    private Text stamp;
    private string shownId;

    /// <summary>True while the map is open.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// Unrolls a map, or rolls it up again if it is already open.
    /// </summary>
    /// <param name="map">The map.</param>
    public static void Toggle(ItemDrop.ItemData map)
    {
        Player player = Player.m_localPlayer;
        TreasureSite site = TreasureMapItem.GetSite(map);
        if (player == null || site == null || GUIManager.CustomGUIFront == null)
        {
            return;
        }

        if (IsOpen && instance.shownId == TreasureMapItem.GetId(map))
        {
            instance.Close();
            return;
        }

        if (instance == null)
        {
            instance = Build();
        }

        TreasureService.RequestStatus(map);
        instance.Show(player, map, site);
    }

    /// <summary>
    /// Shows the plundered stamp if the open map is of this treasure.
    /// </summary>
    /// <param name="id">Treasure id.</param>
    public static void Refresh(string id)
    {
        if (IsOpen && instance.shownId == id)
        {
            instance.stamp.gameObject.SetActive(true);
        }
    }

    private static TreasureMapPanel Build()
    {
        Vector2 middle = new(0.5f, 0.5f);
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, middle, middle, Vector2.zero, PanelWidth, PanelHeight, false);
        panel.name = "WhiteHiltTreasureMap";
        TreasureMapPanel component = panel.AddComponent<TreasureMapPanel>();
        component.BuildContent((RectTransform)panel.transform);
        panel.SetActive(false);
        return component;
    }

    // Builds the map panel once: the title, the map picture with its marks, a stamp for plundered maps and a note.
    private void BuildContent(RectTransform panel)
    {
        Vector2 top = new(0.5f, 1f);
        title = AddText(panel, top, new Vector2(0f, -34f), 26, PanelWidth - 40f, 40f, GUIManager.Instance.ValheimOrange);

        GameObject image = new("Map", typeof(RectTransform), typeof(RawImage));
        RectTransform rect = (RectTransform)image.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = top;
        rect.anchorMax = top;
        rect.anchoredPosition = new Vector2(0f, -66f - MapSize / 2f);
        rect.sizeDelta = new Vector2(MapSize, MapSize);
        picture = image.GetComponent<RawImage>();
        picture.raycastTarget = false;

        overlayRoot = new GameObject("Marks", typeof(RectTransform)).GetComponent<RectTransform>();
        overlayRoot.SetParent(rect, false);
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        status = AddText(rect, new Vector2(0.5f, 0.5f), Vector2.zero, 20, MapSize, 40f, Color.white);
        stamp = AddText(rect, new Vector2(0.5f, 0.5f), Vector2.zero, 64, MapSize, 90f, new Color(0.6f, 0.08f, 0.05f, 0.85f));
        stamp.text = Localization.instance.Localize("$whitehilt_treasure_plundered").ToUpperInvariant();
        stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);

        note = AddText(panel, top, new Vector2(0f, -66f - MapSize - 32f), 17, PanelWidth - 60f, 44f, new Color(0.9f, 0.86f, 0.75f));

        GameObject close = GUIManager.Instance.CreateButton(Localization.instance.Localize("$whitehilt_treasure_close"), panel,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), 160f, 36f);
        close.GetComponent<Button>().onClick.AddListener(Close);
    }

    // Shows a treasure map: its landmarks (named with enough skill), its compass, a note and the drawn map, which is
    // rendered in the background. Some of the map is faded, less with more skill.
    private void Show(Player player, ItemDrop.ItemData map, TreasureSite site)
    {
        string id = TreasureMapItem.GetId(map);
        shownId = id;
        gameObject.SetActive(true);
        GUIManager.BlockInput(true);
        title.text = Localization.instance.Localize("$whitehilt_treasure_title");
        stamp.gameObject.SetActive(TreasureMapItem.IsPlundered(map));
        ClearOverlays();

        TreasureHintLevel hint = TreasureSettings.HintLevel.Value;
        int level = ExplorationSkill.GetLevel(player);
        bool names = hint == TreasureHintLevel.Easy || (hint == TreasureHintLevel.Normal && level >= TreasureSettings.SkillNamesLevel.Value);
        bool path = hint == TreasureHintLevel.Easy || (hint == TreasureHintLevel.Normal && level >= TreasureSettings.SkillPathLevel.Value);
        float missing = TreasureSettings.MissingShare.Value;
        if (hint != TreasureHintLevel.Hard)
        {
            missing *= 1f - TreasureSettings.SkillFadeReduction.Value * Mathf.Clamp01(level / 100f);
        }

        TreasureLandmark? nearest = site.Landmarks.Count == 0 || hint == TreasureHintLevel.Hard
            ? null
            : site.Landmarks.OrderBy(mark => Vector2.Distance(mark.Position, site.Chest)).First();
        note.text = hint == TreasureHintLevel.Hard ? string.Empty : Note(site, nearest);
        AddCompassLabels(site);
        if (hint != TreasureHintLevel.Hard)
        {
            foreach (TreasureLandmark mark in site.Landmarks)
            {
                AddLandmark(site, mark, names);
            }
        }

        string key = $"{id}:{Mathf.RoundToInt(missing * 100f)}:{path}";
        if (cache.TryGetValue(key, out Texture2D cached) && cached != null)
        {
            picture.texture = cached;
            picture.color = Color.white;
            status.text = string.Empty;
            return;
        }

        picture.texture = null;
        picture.color = new Color(0f, 0f, 0f, 0f);
        status.text = Localization.instance.Localize("$whitehilt_treasure_unrolling");
        TreasureMapRenderer.Options options = new() { MissingShare = missing, PathFrom = path && nearest.HasValue ? nearest.Value.Position : null };
        StopAllCoroutines();
        StartCoroutine(TreasureMapRenderer.Render(site, options, texture =>
        {
            Remember(key, texture);
            if (shownId == id)
            {
                picture.texture = texture;
                picture.color = Color.white;
                status.text = string.Empty;
            }
        }));
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead() || Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        if (gameObject.activeSelf)
        {
            GUIManager.BlockInput(false);
        }
    }

    private void Close()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        StopAllCoroutines();
        gameObject.SetActive(false);
        GUIManager.BlockInput(false);
        shownId = null;
    }

    // "Swamp, by the water, south-east of Burial chamber": what the land looks like where the cross is.
    private static string Note(TreasureSite site, TreasureLandmark? nearest)
    {
        WorldGenerator world = WorldGenerator.instance;
        Vector3 chest = new(site.Chest.x, 0f, site.Chest.y);
        Heightmap.Biome biome = world.GetBiome(chest.x, chest.z);
        string biomeName = Localization.instance.Localize("$biome_" + biome.ToString().ToLowerInvariant());
        string feature = Translations.Word(FeatureKey(world, chest, biome));
        if (!nearest.HasValue)
        {
            return string.Format(Translations.Word("whitehilt_treasure_note"), biomeName, feature);
        }

        Vector2 toward = site.Chest - nearest.Value.Position;
        string direction = StoneDowsingService.Direction(new Vector3(toward.x, 0f, toward.y));
        return string.Format(Translations.Word("whitehilt_treasure_note_landmark"), biomeName, feature, direction,
            DiscoveryCatalog.GetLabel(nearest.Value.Kind));
    }

    // A word on where the treasure lies (by the sea, by water, in forest, in hills or in the open), from the land round
    // it.
    private static string FeatureKey(WorldGenerator world, Vector3 chest, Heightmap.Biome biome)
    {
        float water = ZoneSystem.instance.m_waterLevel;
        float reach = TreasureSettings.FeatureDistance.Value;
        bool wet = false;
        bool sea = false;
        float lowest = float.MaxValue;
        float highest = float.MinValue;
        for (int ring = 1; ring <= 4; ring++)
        {
            float radius = reach * ring / 4f;
            for (int step = 0; step < 12; step++)
            {
                float angle = step * Mathf.PI / 6f;
                float x = chest.x + Mathf.Cos(angle) * radius;
                float z = chest.z + Mathf.Sin(angle) * radius;
                float height = world.GetHeight(x, z);
                lowest = Mathf.Min(lowest, height);
                highest = Mathf.Max(highest, height);
                if (height < water)
                {
                    wet = true;
                    sea |= world.GetBiome(x, z) == Heightmap.Biome.Ocean;
                }
            }
        }

        if (wet)
        {
            return sea ? "whitehilt_treasure_by_sea" : "whitehilt_treasure_by_water";
        }

        bool woodland = biome == Heightmap.Biome.Meadows || biome == Heightmap.Biome.BlackForest || biome == Heightmap.Biome.Swamp
            || biome == Heightmap.Biome.Mistlands;
        if (woodland && WorldGenerator.GetForestFactor(chest) < 1.15f)
        {
            return "whitehilt_treasure_in_forest";
        }

        return highest - lowest > 15f ? "whitehilt_treasure_in_hills" : "whitehilt_treasure_in_open";
    }

    // Puts a landmark's icon on the map, with its name if the player may see names.
    private void AddLandmark(TreasureSite site, TreasureLandmark mark, bool named)
    {
        Vector2 position = TreasureMapRenderer.WorldToMap(site, mark.Position);
        if (position.x < 0.05f || position.y < 0.05f || position.x > 0.95f || position.y > 0.95f)
        {
            return;
        }

        Sprite sprite = DiscoveryCatalog.GetIcon(mark.Kind);
        Vector2 local = (position - new Vector2(0.5f, 0.5f)) * MapSize;
        if (sprite != null)
        {
            GameObject icon = new("Landmark", typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)icon.transform;
            rect.SetParent(overlayRoot, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.anchoredPosition = local;
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            Image image = icon.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = sepia;
            image.raycastTarget = false;
            overlays.Add(icon);
        }

        if (named)
        {
            Text label = AddText(overlayRoot, new Vector2(0.5f, 0.5f), local + new Vector2(0f, -IconSize * 0.9f), 13, 140f, 20f, inkText);
            label.text = DiscoveryCatalog.GetLabel(mark.Kind);
            overlays.Add(label.gameObject);
        }
    }

    // The N by the drawn north arrow and the length of the drawn scale bar, placed like TreasureMapRenderer draws them.
    private void AddCompassLabels(TreasureSite site)
    {
        float toUi = MapSize / TreasureMapRenderer.Resolution;
        Vector2 arrow = new Vector2(TreasureMapRenderer.Resolution - 24f, TreasureMapRenderer.Resolution - 26f) + TreasureMapRenderer.North(site) * 16f;
        Text north = AddText(overlayRoot, new Vector2(0f, 0f), arrow * toUi, 16, 30f, 24f, inkText);
        north.text = "N";
        overlays.Add(north.gameObject);

        int metres = TreasureMapRenderer.ScaleMetres(site);
        float length = metres / site.Size * TreasureMapRenderer.Resolution;
        Text scale = AddText(overlayRoot, new Vector2(0f, 0f), new Vector2(16f + length / 2f, 26f) * toUi, 13, 90f, 20f, inkText);
        scale.text = string.Format(Translations.Word("whitehilt_treasure_scale"), metres);
        overlays.Add(scale.gameObject);
    }

    private void ClearOverlays()
    {
        foreach (GameObject overlay in overlays)
        {
            Destroy(overlay);
        }

        overlays.Clear();
    }

    private static void Remember(string key, Texture2D texture)
    {
        cache[key] = texture;
        cacheOrder.Remove(key);
        cacheOrder.Add(key);
        while (cacheOrder.Count > CacheSize)
        {
            string oldest = cacheOrder[0];
            cacheOrder.RemoveAt(0);
            if (cache.TryGetValue(oldest, out Texture2D old) && old != null && (instance == null || instance.picture.texture != old))
            {
                Destroy(old);
            }

            cache.Remove(oldest);
        }
    }

    private static Text AddText(RectTransform parent, Vector2 anchor, Vector2 position, int size, float width, float height, Color colour)
    {
        GameObject label = GUIManager.Instance.CreateText(string.Empty, parent, anchor, anchor, position, GUIManager.Instance.AveriaSerifBold, size,
            colour, false, Color.black, width, height, false);
        Text text = label.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }
}
