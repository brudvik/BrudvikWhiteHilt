using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Bestiary;

/// <summary>A paged field guide with live recipes and indistinct ink sketches of the black beasts.</summary>
public sealed class BeastBookPanel : MonoBehaviour
{
    private const float Width = 720f;
    private const float Height = 690f;
    private const int SketchSize = 96;
    private const int StudySize = 32;
    private static BeastBookPanel instance;
    private static int closedFrame = -1;

    private BeastBookStand stand;
    private Text heading;
    private Text body;
    private Text pageLabel;
    private Image illustration;
    private Button previous;
    private Button next;
    private ScrollRect scroll;
    private BeastCounter[] pages = Array.Empty<BeastCounter>();
    private int page;
    private float nextRefresh;
    private readonly Dictionary<Sprite, Sprite> sketches = new();

    /// <summary>Whether the guide currently owns input.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;
    /// <summary>Whether the menu should be suppressed for this frame.</summary>
    public static bool BlocksMenu => IsOpen || closedFrame == Time.frameCount;

    /// <summary>Opens the book and retains the last page read in this scene.</summary>
    /// <param name="stand">Book being read.</param>
    public static void Open(BeastBookStand stand)
    {
        if (GUIManager.CustomGUIFront == null)
        {
            return;
        }
        if (instance == null)
        {
            instance = Build();
        }
        instance.stand = stand;
        instance.gameObject.SetActive(true);
        instance.Fit();
        instance.Refresh();
        instance.scroll.verticalNormalizedPosition = 1f;
        GUIManager.BlockInput(true);
    }

    /// <summary>Closes the guide and returns control to the player.</summary>
    public static void Close()
    {
        if (IsOpen)
        {
            instance.gameObject.SetActive(false);
            closedFrame = Time.frameCount;
            GUIManager.BlockInput(false);
        }
    }

    /// <summary>Formats a page from the creature definition and actual registered recipe.</summary>
    /// <param name="counter">Page's material counter.</param>
    /// <returns>Localized rich text, including current server recipe overrides.</returns>
    public static string PageText(BeastCounter counter)
    {
        string monster = Local(Translations.Token(counter.Beast.NameKey));
        string prefix = "$whitehilt_counter_" + counter.Key.ToLowerInvariant();
        string biomes = string.Join(", ", Enum.GetValues(typeof(Heightmap.Biome)).Cast<Heightmap.Biome>()
            .Where(biome => biome != Heightmap.Biome.None && (counter.Beast.Biome & biome) == biome)
            .Select(biome => Local("$biome_" + biome.ToString().ToLowerInvariant())));
        string guardian = counter.Beast.BossKey.Substring("defeated_".Length);
        if (guardian == "queen")
        {
            guardian = "seekerqueen";
        }
        string text = Section("range", biomes + "\n" + string.Format(Local("$whitehilt_bestiary_unlock"), Local("$enemy_" + guardian)))
            + Section("danger", Local(prefix + "_danger"))
            + Section("counter", Local(Translations.Token(counter.NameKey)) + "\n" + Local(Translations.Token(counter.NameKey + "_description"))
                + "\n" + string.Format(Local("$whitehilt_counter_bonus"), monster, Translations.Number(counter.Bonus * 100f)));
        if (!counter.IsArrow)
        {
            string channel = counter.DamageType == HitData.DamageType.Pickaxe ? "pickaxe"
                : counter.DamageType == HitData.DamageType.Blunt ? "blunt" : "slash";
            text += Local("$whitehilt_counter_" + channel) + "\n"
                + string.Format(Local("$whitehilt_counter_treatment"), monster, counter.Attacks) + "\n\n";
        }
        else
        {
            text += Local("$whitehilt_counter_arrow") + "\n\n";
        }
        Recipe recipe = ObjectDB.instance?.m_recipes.FirstOrDefault(candidate => candidate.m_item != null && candidate.m_item.name == counter.PrefabName);
        if (recipe == null)
        {
            text += Section("recipe", Local("$whitehilt_bestiary_unavailable"));
        }
        else
        {
            string station = recipe.m_craftingStation != null ? Local(recipe.m_craftingStation.m_name) : Local("$inventory_crafting");
            string ingredients = string.Join("\n", recipe.m_resources.Where(requirement => requirement.m_resItem != null && requirement.m_amount > 0)
                .Select(requirement => Local(requirement.m_resItem.m_itemData.m_shared.m_name) + " x" + requirement.m_amount));
            text += Section("recipe", string.Format(Local("$whitehilt_bestiary_station"), station, recipe.m_minStationLevel)
                + "\n" + string.Format(Local("$whitehilt_bestiary_yield"), recipe.m_amount) + "\n" + ingredients
                + (!recipe.m_enabled ? "\n" + Local("$whitehilt_bestiary_disabled") : string.Empty));
        }
        return text + Section("gather", Local(prefix + "_gathering"));
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB") || stand == null || player == null || player.IsDead()
            || Vector3.Distance(player.transform.position, stand.transform.position) > BeastBookSettings.ReadingDistance)
        {
            Close();
            return;
        }
        Fit();
        if (Time.unscaledTime >= nextRefresh)
        {
            Refresh();
        }
    }

    private void OnDisable()
    {
        GUIManager.BlockInput(false);
    }

    private void OnDestroy()
    {
        foreach (Sprite sketch in sketches.Values)
        {
            if (sketch != null)
            {
                Destroy(sketch.texture);
                Destroy(sketch);
            }
        }
        sketches.Clear();
        if (instance == this)
        {
            instance = null;
            GUIManager.BlockInput(false);
        }
    }

    private void ChangePage(int step)
    {
        if (pages.Length == 0)
        {
            return;
        }
        page = Mathf.Clamp(page + step, 0, pages.Length - 1);
        Refresh();
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1f;
    }

    private void Refresh()
    {
        BeastCounter selected = page >= 0 && page < pages.Length ? pages[page] : null;
        pages = BeastCounter.Discovered(Player.m_localPlayer);
        page = selected != null ? Array.IndexOf(pages, selected) : 0;
        if (page < 0)
        {
            page = 0;
        }
        nextRefresh = Time.unscaledTime + BeastBookSettings.RefreshSeconds;
        if (pages.Length == 0)
        {
            heading.text = Local("$whitehilt_bestiary_empty_title");
            body.text = Local("$whitehilt_bestiary_empty");
            pageLabel.text = "0 / 0";
            previous.interactable = false;
            next.interactable = false;
            illustration.sprite = null;
            illustration.enabled = false;
            return;
        }
        BeastCounter counter = pages[page];
        heading.text = Local(Translations.Token(counter.Beast.NameKey));
        body.text = PageText(counter);
        pageLabel.text = (page + 1) + " / " + pages.Length;
        previous.interactable = page > 0;
        next.interactable = page < pages.Length - 1;
        ItemDrop trophy = ObjectDB.instance?.GetItemPrefab(counter.Beast.TrophyName)?.GetComponent<ItemDrop>();
        illustration.sprite = Sketch(trophy?.m_itemData.m_shared.m_icons.FirstOrDefault());
        illustration.enabled = illustration.sprite != null;
    }

    private Sprite Sketch(Sprite source)
    {
        if (source == null || VisualHelper.IsHeadless)
        {
            return null;
        }
        if (sketches.TryGetValue(source, out Sprite cached))
        {
            return cached;
        }
        Sprite sketch = null;
        Texture2D texture = null;
        try
        {
            Color32[] study = VisualHelper.ReadPixels(source.texture, StudySize, StudySize, source.textureRect);
            texture = new Texture2D(SketchSize, SketchSize, TextureFormat.RGBA32, false)
            {
                name = "Bestiary ink sketch",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels32(PaintSketch(study));
            texture.Apply(false, true);
            sketch = Sprite.Create(texture, new Rect(0f, 0f, SketchSize, SketchSize), new Vector2(0.5f, 0.5f));
        }
        catch (Exception exception)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
            Jotunn.Logger.LogWarning("Bestiary sketch unavailable: " + exception.Message);
        }
        sketches[source] = sketch;
        return sketch;
    }

    private static Color32[] PaintSketch(Color32[] study)
    {
        float[] coverage = new float[StudySize * StudySize];
        float[] tone = new float[coverage.Length];
        for (int studyY = 0; studyY < StudySize; studyY++)
        {
            for (int studyX = 0; studyX < StudySize; studyX++)
            {
                float alpha = 0f;
                float light = 0f;
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        Color32 sample = study[Mathf.Clamp(studyY + offsetY, 0, StudySize - 1) * StudySize
                            + Mathf.Clamp(studyX + offsetX, 0, StudySize - 1)];
                        float weight = sample.a / 255f;
                        alpha += weight;
                        light += (sample.r * 0.299f + sample.g * 0.587f + sample.b * 0.114f) / 255f * weight;
                    }
                }
                int index = studyY * StudySize + studyX;
                coverage[index] = alpha / 9f;
                tone[index] = alpha > 0f ? light / alpha : 1f;
            }
        }
        Color32[] pixels = new Color32[SketchSize * SketchSize];
        for (int pixelY = 0; pixelY < SketchSize; pixelY++)
        {
            for (int pixelX = 0; pixelX < SketchSize; pixelX++)
            {
                float grain = ((pixelX * 73 + pixelY * 151 + pixelX * pixelY * 17) % 101) / 100f;
                float studyX = (pixelX - 7f) * (StudySize - 1) / (SketchSize - 15f);
                float studyY = (pixelY - 7f) * (StudySize - 1) / (SketchSize - 15f);
                float alpha = SampleStudy(coverage, studyX, studyY);
                float contour = Mathf.Abs(alpha - SampleStudy(coverage, studyX + 1f, studyY))
                    + Mathf.Abs(alpha - SampleStudy(coverage, studyX, studyY + 1f));
                float shade = alpha * (0.35f + 0.65f * (1f - SampleStudy(tone, studyX, studyY)));
                float hatch = (pixelX + pixelY + (int)(grain * 2f)) % 7 == 0 ? 0.32f : 0f;
                if (shade > 0.65f && (pixelX - pixelY + SketchSize) % 11 == 0)
                {
                    hatch += 0.16f;
                }
                float ink = Mathf.Clamp01(contour * 1.25f + shade * (0.12f + hatch));
                if (pixelX < 7 || pixelY < 7 || pixelX >= SketchSize - 7 || pixelY >= SketchSize - 7)
                {
                    ink = 0f;
                }
                float paper = 0.96f + grain * 0.04f;
                int edge = Mathf.Min(Mathf.Min(pixelX, pixelY), Mathf.Min(SketchSize - 1 - pixelX, SketchSize - 1 - pixelY));
                pixels[pixelY * SketchSize + pixelX] = new Color32(
                    (byte)Mathf.Lerp(226f * paper, 65f, ink),
                    (byte)Mathf.Lerp(216f * paper, 57f, ink),
                    (byte)Mathf.Lerp(190f * paper, 45f, ink),
                    (byte)(edge == 0 ? 0 : edge == 1 ? 120 + grain * 80f : 255));
            }
        }
        return pixels;
    }

    private static float SampleStudy(float[] values, float positionX, float positionY)
    {
        float clampedX = Mathf.Clamp(positionX, 0f, StudySize - 1f);
        float clampedY = Mathf.Clamp(positionY, 0f, StudySize - 1f);
        int left = (int)clampedX;
        int bottom = (int)clampedY;
        int right = Mathf.Min(left + 1, StudySize - 1);
        int top = Mathf.Min(bottom + 1, StudySize - 1);
        return Mathf.Lerp(
            Mathf.Lerp(values[bottom * StudySize + left], values[bottom * StudySize + right], clampedX - left),
            Mathf.Lerp(values[top * StudySize + left], values[top * StudySize + right], clampedX - left), clampedY - bottom);
    }

    private void Fit()
    {
        RectTransform parent = transform.parent as RectTransform;
        if (parent != null)
        {
            float scale = Mathf.Min(1f, Mathf.Min(parent.rect.width / (Width + 24f), parent.rect.height / (Height + 24f)));
            transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }
    }

    private static BeastBookPanel Build()
    {
        Vector2 top = new(0.5f, 1f);
        Vector2 bottom = new(0.5f, 0f);
        GameObject root = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Width, Height, true);
        BeastBookPanel book = root.AddComponent<BeastBookPanel>();
        Text title = TextAt(root.transform, Local("$whitehilt_bestiary_title"), top, new Vector2(0f, -30f), Width - 60f, 38f, 24);
        title.alignment = TextAnchor.MiddleCenter;
        book.heading = TextAt(root.transform, string.Empty, top, new Vector2(30f, -86f), Width - 180f, 46f, 23);
        book.heading.alignment = TextAnchor.MiddleCenter;
        GameObject image = new("Beast", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(root.transform, false);
        RectTransform imageRect = (RectTransform)image.transform;
        imageRect.anchorMin = imageRect.anchorMax = top;
        imageRect.anchoredPosition = new Vector2(-Width / 2f + 72f, -86f);
        imageRect.sizeDelta = new Vector2(80f, 80f);
        book.illustration = image.GetComponent<Image>();
        book.illustration.preserveAspect = true;
        book.illustration.raycastTarget = false;
        GameObject view = GUIManager.Instance.CreateScrollView(root.transform, false, true, 18f, 4f,
            GUIManager.Instance.ValheimScrollbarHandleColorBlock, new Color(0f, 0f, 0f, 0.25f), Width - 60f, Height - 210f);
        RectTransform viewRect = (RectTransform)view.transform;
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = new Vector2(30f, 85f);
        viewRect.offsetMax = new Vector2(-30f, -125f);
        PortalTravelPanel.StretchScrollView(viewRect);
        book.scroll = view.GetComponentInChildren<ScrollRect>();
        book.scroll.horizontal = false;
        book.scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        book.scroll.viewport.offsetMin = Vector2.zero;
        book.scroll.viewport.offsetMax = new Vector2(-26f, 0f);
        RectTransform scrollbar = (RectTransform)book.scroll.verticalScrollbar.transform;
        scrollbar.SetParent(viewRect, false);
        scrollbar.anchorMin = new Vector2(1f, 0f);
        scrollbar.anchorMax = Vector2.one;
        scrollbar.pivot = new Vector2(1f, 0.5f);
        scrollbar.sizeDelta = new Vector2(18f, -8f);
        scrollbar.anchoredPosition = Vector2.zero;
        RectTransform handle = book.scroll.verticalScrollbar.handleRect;
        handle.anchorMin = new Vector2(0f, handle.anchorMin.y);
        handle.anchorMax = new Vector2(1f, handle.anchorMax.y);
        handle.pivot = new Vector2(0.5f, 0.5f);
        handle.offsetMin = new Vector2(2f, handle.offsetMin.y);
        handle.offsetMax = new Vector2(-2f, handle.offsetMax.y);
        RectTransform content = book.scroll.content;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = Vector2.zero;
        content.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        book.body = TextAt(content, string.Empty, top, Vector2.zero, Width - 110f, 1f, 17);
        book.body.alignment = TextAnchor.UpperLeft;
        book.body.supportRichText = true;
        book.body.horizontalOverflow = HorizontalWrapMode.Wrap;
        book.body.verticalOverflow = VerticalWrapMode.Overflow;
        book.previous = ButtonAt(root.transform, "<", new Vector2(-150f, 40f), 56f, () => book.ChangePage(-1));
        (book.previous.GetComponent<UITooltip>() ?? book.previous.gameObject.AddComponent<UITooltip>()).m_text = "$whitehilt_bestiary_previous";
        book.next = ButtonAt(root.transform, ">", new Vector2(150f, 40f), 56f, () => book.ChangePage(1));
        (book.next.GetComponent<UITooltip>() ?? book.next.gameObject.AddComponent<UITooltip>()).m_text = "$whitehilt_bestiary_next";
        book.pageLabel = TextAt(root.transform, string.Empty, bottom, new Vector2(0f, 40f), 110f, 36f, 17);
        book.pageLabel.alignment = TextAnchor.MiddleCenter;
        ButtonAt(root.transform, Local("$whitehilt_bestiary_close"), new Vector2(Width / 2f - 88f, 40f), 106f, Close);
        return book;
    }

    private static Text TextAt(Transform parent, string text, Vector2 anchor, Vector2 position, float width, float height, int size)
        => GUIManager.Instance.CreateText(text, parent, anchor, anchor, position, GUIManager.Instance.AveriaSerif, size,
            Color.white, true, Color.black, width, height, false).GetComponent<Text>();

    private static Button ButtonAt(Transform parent, string label, Vector2 position, float width, UnityEngine.Events.UnityAction action)
    {
        Button button = GUIManager.Instance.CreateButton(label, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, width, 36f).GetComponent<Button>();
        button.onClick.AddListener(action);
        return button;
    }

    private static string Local(string text) => Localization.instance.Localize(text);
    private static string Section(string key, string text) => "<color=#E8B04B><b>" + Local("$whitehilt_bestiary_" + key) + "</b></color>\n" + text + "\n\n";
}