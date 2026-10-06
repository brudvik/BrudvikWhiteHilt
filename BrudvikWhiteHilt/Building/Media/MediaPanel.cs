using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Building.Media;

/// <summary>
/// The media panel on the right while the build camera is on: photo, photo view, hidden character, local time and weather,
/// and the film editor with its list of camera points. The cursor is free while it is open; hold the right mouse button
/// to look around.
/// </summary>
public class MediaPanel : MonoBehaviour
{
    private const float PanelWidth = 440f;
    private const float Padding = 14f;
    private const float RowHeight = 30f;
    private const float RowGap = 4f;
    private const float ListHeight = 200f;
    private const float PointRowHeight = 60f;
    private const float ConfirmSeconds = 3f;
    private const int FontSize = 14;
    private const float RefreshInterval = 0.2f;

    private static readonly Color selectedColor = new(1f, 0.8f, 0.4f);

    private static MediaPanel instance;

    private readonly List<GameObject> rows = new();

    private GameObject panel;
    private Text photoViewLabel;
    private Text characterLabel;
    private Text timeLabel;
    private Text weatherLabel;
    private Text filmLabel;
    private Text deleteFilmLabel;
    private Text fadeLabel;
    private Text titleCardLabel;
    private Text transitionLabel;
    private InputField titleField;
    private InputField authorField;
    private InputField dateField;
    private RectTransform listContent;
    private List<Film> films;
    private string filmsWorld;
    private int filmIndex;
    private int selected = -1;
    private float deleteArmedUntil;
    private bool inputBlocked;
    private float nextRow;
    private float nextRefresh;

    /// <summary>True while the panel is open.</summary>
    public static bool IsOpen => instance != null && instance.panel.activeSelf;

    /// <summary>True while one of the panel's text fields has focus.</summary>
    public static bool Typing => instance != null && instance.inputBlocked;

    /// <summary>Hosts coroutines for photos and thumbnails.</summary>
    public static MonoBehaviour Host => instance;

    private Film CurrentFilm => films != null && filmIndex >= 0 && filmIndex < films.Count ? films[filmIndex] : null;

    /// <summary>
    /// Creates the panel once the custom GUI exists. Safe to call every frame.
    /// </summary>
    public static void Ensure()
    {
        if (instance != null || GUIManager.CustomGUIFront == null)
        {
            return;
        }

        GameObject root = new("WhiteHiltMediaPanel", typeof(RectTransform));
        root.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        instance = root.AddComponent<MediaPanel>();
        instance.Build();
    }

    /// <summary>
    /// Opens or closes the panel.
    /// </summary>
    public static void Toggle()
    {
        if (instance == null)
        {
            return;
        }

        if (IsOpen)
        {
            Close();
        }
        else
        {
            instance.Open();
        }
    }

    /// <summary>
    /// Closes the panel.
    /// </summary>
    public static void Close()
    {
        if (instance == null)
        {
            return;
        }

        instance.panel.SetActive(false);
        instance.SetInputBlocked(false);
    }

    // Runs the overlay, closes the panel when the build camera is switched off, keeps the game from reading keys while
    // a text field has focus, hides the panel while the screen is cleared and refreshes its labels now and then.
    private void Update()
    {
        MediaOverlay.Tick();
        if (!IsOpen)
        {
            return;
        }

        if (!BuildCamera.Active || Player.m_localPlayer == null)
        {
            Close();
            return;
        }

        SetInputBlocked(titleField.isFocused || authorField.isFocused || dateField.isFocused);
        bool visible = !MediaMode.HideUi;
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            RefreshLabels();
        }
    }

    private void OnDestroy()
    {
        SetInputBlocked(false);
        if (instance == this)
        {
            instance = null;
        }
    }

    // Opens the panel on the films of this world, loaded the first time or after a world change, with an empty film if
    // there are none.
    private void Open()
    {
        string world = ZNet.instance != null ? ZNet.instance.GetWorldName() : string.Empty;
        if (films == null || filmsWorld != world)
        {
            films = FilmStore.LoadAll();
            filmsWorld = world;
            filmIndex = films.Count - 1;
        }

        if (films.Count == 0)
        {
            NewFilm();
        }

        selected = Mathf.Min(selected, CurrentFilm.Points.Count - 1);
        Groups.BlueprintPanel.Close();
        panel.SetActive(true);
        ShowFilm();
    }

    // Builds the panel once: photo buttons, time and weather, the films with their title card settings, the list of
    // points and the buttons to add, change and play them.
    private void Build()
    {
        panel = GUIManager.Instance.CreateWoodpanel(transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, PanelWidth, 800f, false);
        panel.name = "Panel";
        panel.AddComponent<CanvasGroup>();
        RectTransform rect = (RectTransform)panel.transform;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-20f, 0f);

        float full = PanelWidth - Padding * 2f;
        float half = (full - RowGap) / 2f;
        float third = (full - RowGap * 2f) / 3f;
        float quarter = (full - RowGap * 3f) / 4f;

        nextRow = Padding;
        Text title = AddText(Localization.instance.Localize("$whitehilt_media"), 22, GUIManager.Instance.ValheimOrange, 26f, full, 0f);
        title.font = GUIManager.Instance.AveriaSerifBold;
        NextRow(26f);

        AddButton(0f, half, OnPhoto).text = Key(MediaSettings.KeyPhoto.Value.MainKey) + Localization.instance.Localize("$whitehilt_media_photo");
        photoViewLabel = AddButton(half + RowGap, half, () => MediaMode.TogglePhotoView());
        NextRow();
        characterLabel = AddButton(0f, full, () => MediaMode.ToggleCharacter(Player.m_localPlayer));
        NextRow();

        float labelWidth = 160f;
        float small = (full - labelWidth - RowGap * 3f) / 3f;
        timeLabel = AddText(string.Empty, FontSize, Color.white, RowHeight, labelWidth, 0f);
        timeLabel.alignment = TextAnchor.MiddleLeft;
        AddButton(labelWidth + RowGap, small, () => MediaMode.ShiftHour(-1)).text = Localization.instance.Localize("$whitehilt_media_hour_back");
        AddButton(labelWidth + RowGap + (small + RowGap), small, () => MediaMode.ShiftHour(1)).text = Localization.instance.Localize("$whitehilt_media_hour_forward");
        AddButton(labelWidth + RowGap + (small + RowGap) * 2f, small, MediaMode.RealTime).text = Localization.instance.Localize("$whitehilt_media_real");
        NextRow();
        weatherLabel = AddText(string.Empty, FontSize, Color.white, RowHeight, labelWidth, 0f);
        weatherLabel.alignment = TextAnchor.MiddleLeft;
        AddButton(labelWidth + RowGap, small, () => MediaMode.NextWeather(-1)).text = "<";
        AddButton(labelWidth + RowGap + (small + RowGap), small, () => MediaMode.NextWeather(1)).text = ">";
        AddButton(labelWidth + RowGap + (small + RowGap) * 2f, small, MediaMode.RealWeather).text = Localization.instance.Localize("$whitehilt_media_real");
        NextRow();

        Text video = AddText(Localization.instance.Localize("$whitehilt_media_video"), 18, GUIManager.Instance.ValheimOrange, 24f, full, 0f);
        video.font = GUIManager.Instance.AveriaSerifBold;
        NextRow(24f);

        float filmLabelWidth = 132f;
        float filmButton = (full - filmLabelWidth - RowGap * 4f) / 4f;
        filmLabel = AddText(string.Empty, FontSize, Color.white, RowHeight, filmLabelWidth, 0f);
        filmLabel.alignment = TextAnchor.MiddleLeft;
        AddButton(filmLabelWidth + RowGap, filmButton, () => StepFilm(-1)).text = "<";
        AddButton(filmLabelWidth + RowGap + (filmButton + RowGap), filmButton, () => StepFilm(1)).text = ">";
        AddButton(filmLabelWidth + RowGap + (filmButton + RowGap) * 2f, filmButton, OnNewFilm).text = Localization.instance.Localize("$whitehilt_media_new");
        deleteFilmLabel = AddButton(filmLabelWidth + RowGap + (filmButton + RowGap) * 3f, filmButton, OnDeleteFilm);
        NextRow();

        titleField = AddField("$whitehilt_media_title", full, value => Edit(film => film.Title = value));
        authorField = AddField("$whitehilt_media_author", full, value => Edit(film => film.Author = value));
        dateField = AddField("$whitehilt_media_date", full, value => Edit(film => film.Date = value));

        fadeLabel = AddButton(0f, half, () => Edit(film => film.Fade = !film.Fade));
        titleCardLabel = AddButton(half + RowGap, half, () => Edit(film => film.TitleCard = !film.TitleCard));
        NextRow();

        GameObject scroll = GUIManager.Instance.CreateScrollView(panel.transform, false, true, 8f, 4f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
            new Color(0f, 0f, 0f, 0.35f), full, ListHeight);
        RectTransform scrollRect = (RectTransform)scroll.transform;
        scrollRect.anchorMin = new Vector2(0f, 1f);
        scrollRect.anchorMax = new Vector2(0f, 1f);
        scrollRect.pivot = new Vector2(0f, 1f);
        scrollRect.anchoredPosition = new Vector2(Padding, -nextRow);
        scrollRect.sizeDelta = new Vector2(full, ListHeight);
        listContent = scroll.GetComponentInChildren<ScrollRect>().content;
        VerticalLayoutGroup layout = listContent.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        NextRow(ListHeight);

        AddButton(0f, third, OnAddPoint).text = Localization.instance.Localize("$whitehilt_media_add");
        AddButton(third + RowGap, third, OnUpdatePoint).text = Localization.instance.Localize("$whitehilt_media_update");
        AddButton((third + RowGap) * 2f, third, OnGoTo).text = Localization.instance.Localize("$whitehilt_media_goto");
        NextRow();
        AddButton(0f, quarter, () => MovePoint(-1)).text = Localization.instance.Localize("$whitehilt_media_up");
        AddButton(quarter + RowGap, quarter, () => MovePoint(1)).text = Localization.instance.Localize("$whitehilt_media_down");
        AddButton((quarter + RowGap) * 2f, quarter, OnDeletePoint).text = Localization.instance.Localize("$whitehilt_media_delete");
        transitionLabel = AddButton((quarter + RowGap) * 3f, quarter, () => EditPoint(point => point.Smooth = !point.Smooth));
        NextRow();
        AddButton(0f, quarter, () => EditPoint(point => point.Seconds = Mathf.Max(0.5f, point.Seconds - 0.5f))).text =
            Localization.instance.Localize("$whitehilt_media_seconds_less");
        AddButton(quarter + RowGap, quarter, () => EditPoint(point => point.Seconds = Mathf.Min(60f, point.Seconds + 0.5f))).text =
            Localization.instance.Localize("$whitehilt_media_seconds_more");
        AddButton((quarter + RowGap) * 2f, quarter, () => EditPoint(point => point.Hold = Mathf.Max(0f, point.Hold - 0.5f))).text =
            Localization.instance.Localize("$whitehilt_media_hold_less");
        AddButton((quarter + RowGap) * 3f, quarter, () => EditPoint(point => point.Hold = Mathf.Min(30f, point.Hold + 0.5f))).text =
            Localization.instance.Localize("$whitehilt_media_hold_more");
        NextRow();
        AddButton(0f, half, () => FilmPlayer.Play(Player.m_localPlayer, CurrentFilm, asPreview: true)).text =
            Localization.instance.Localize("$whitehilt_media_preview");
        AddButton(half + RowGap, half, OnPlay).text = Localization.instance.Localize("$whitehilt_media_play");
        NextRow();
        Text footer = AddText(Localization.instance.Localize("$whitehilt_media_footer"), 12, new Color(0.85f, 0.85f, 0.85f), 36f, full, 0f);
        footer.alignment = TextAnchor.MiddleCenter;
        NextRow(36f);

        rect.sizeDelta = new Vector2(PanelWidth, nextRow - RowGap + Padding);
        panel.SetActive(false);
    }

    private void OnPhoto()
    {
        MediaMode.TakePhoto(this);
    }

    private void OnPlay()
    {
        FilmPlayer.Play(Player.m_localPlayer, CurrentFilm, asPreview: false);
    }

    private void NewFilm()
    {
        Player player = Player.m_localPlayer;
        Film film = new()
        {
            Title = string.Format(Localization.instance.Localize("$whitehilt_media_film_count"), films.Count + 1, films.Count + 1),
            Author = player != null ? player.GetPlayerName() : string.Empty,
            Date = DateTime.Now.ToString("yyyy-MM-dd")
        };

        films.Add(film);
        filmIndex = films.Count - 1;
        selected = -1;
    }

    private void OnNewFilm()
    {
        NewFilm();
        FilmStore.Save(CurrentFilm);
        ShowFilm();
    }

    // Deletes the film on the second click within a few seconds, so a film is not lost to a slip of the mouse.
    private void OnDeleteFilm()
    {
        if (Time.unscaledTime > deleteArmedUntil)
        {
            deleteArmedUntil = Time.unscaledTime + ConfirmSeconds;
            RefreshLabels();
            return;
        }

        deleteArmedUntil = 0f;
        Film film = CurrentFilm;
        FilmStore.Delete(film);
        foreach (FilmPoint point in film.Points)
        {
            Destroy(point.Thumb);
        }

        films.Remove(film);
        if (films.Count == 0)
        {
            NewFilm();
        }

        filmIndex = Mathf.Clamp(filmIndex, 0, films.Count - 1);
        selected = -1;
        ShowFilm();
    }

    private void StepFilm(int direction)
    {
        filmIndex = (filmIndex + direction + films.Count) % films.Count;
        selected = -1;
        ShowFilm();
    }

    private void OnAddPoint()
    {
        Film film = CurrentFilm;
        FilmPoint point = PointHere();
        int index = selected >= 0 ? selected + 1 : film.Points.Count;
        film.Points.Insert(index, point);
        selected = index;
        FilmStore.Save(film);
        RefreshList();
        MediaMode.TakeThumbnail(this, thumb => SetThumb(film, point, thumb));
    }

    // Moves the selected point to the current view, keeping its timing, and takes a new thumbnail for it.
    private void OnUpdatePoint()
    {
        Film film = CurrentFilm;
        if (selected < 0 || selected >= film.Points.Count)
        {
            return;
        }

        FilmPoint old = film.Points[selected];
        FilmPoint point = PointHere();
        point.Seconds = old.Seconds;
        point.Hold = old.Hold;
        point.Smooth = old.Smooth;
        point.Thumb = old.Thumb;
        film.Points[selected] = point;
        FilmStore.Save(film);
        MediaMode.TakeThumbnail(this, thumb =>
        {
            Destroy(old.Thumb);
            SetThumb(film, point, thumb);
        });
    }

    private void OnGoTo()
    {
        Film film = CurrentFilm;
        if (selected >= 0 && selected < film.Points.Count)
        {
            FilmPoint point = film.Points[selected];
            BuildCamera.SetPose(point.Position, point.Yaw, point.Pitch);
            MediaMode.SetFov(point.Fov);
        }
    }

    private void OnDeletePoint()
    {
        Film film = CurrentFilm;
        if (selected < 0 || selected >= film.Points.Count)
        {
            return;
        }

        Destroy(film.Points[selected].Thumb);
        film.Points.RemoveAt(selected);
        selected = Mathf.Min(selected, film.Points.Count - 1);
        FilmStore.Save(film);
        RefreshList();
    }

    private void MovePoint(int direction)
    {
        Film film = CurrentFilm;
        int target = selected + direction;
        if (selected < 0 || target < 0 || target >= film.Points.Count)
        {
            return;
        }

        (film.Points[selected], film.Points[target]) = (film.Points[target], film.Points[selected]);
        selected = target;
        FilmStore.Save(film);
        RefreshList();
    }

    private void EditPoint(Action<FilmPoint> change)
    {
        Film film = CurrentFilm;
        if (selected >= 0 && selected < film.Points.Count)
        {
            change(film.Points[selected]);
            FilmStore.Save(film);
            RefreshList();
        }
    }

    private void Edit(Action<Film> change)
    {
        Film film = CurrentFilm;
        if (film != null)
        {
            change(film);
            FilmStore.Save(film);
            RefreshLabels();
        }
    }

    private void SetThumb(Film film, FilmPoint point, Texture2D thumb)
    {
        point.Thumb = thumb;
        if (film.Points.Contains(point))
        {
            FilmStore.Save(film);
        }

        if (film == CurrentFilm)
        {
            RefreshList();
        }
    }

    private static FilmPoint PointHere()
    {
        return new FilmPoint
        {
            Position = BuildCamera.Position,
            Yaw = BuildCamera.Yaw,
            Pitch = BuildCamera.Pitch,
            Fov = MediaMode.CurrentFov(GameCamera.instance != null ? GameCamera.instance.m_fov : 65f)
        };
    }

    private void ShowFilm()
    {
        Film film = CurrentFilm;
        titleField.SetTextWithoutNotify(film.Title);
        authorField.SetTextWithoutNotify(film.Author);
        dateField.SetTextWithoutNotify(film.Date);
        RefreshList();
        RefreshLabels();
    }

    // Updates the buttons' labels from the current settings and the selected point.
    private void RefreshLabels()
    {
        string on = Localization.instance.Localize("$whitehilt_build_on");
        string off = Localization.instance.Localize("$whitehilt_build_off");
        Film film = CurrentFilm;

        photoViewLabel.text = Key(MediaSettings.KeyPhotoView.Value.MainKey) + Localization.instance.Localize("$whitehilt_media_photoview")
            + ": " + (MediaMode.PhotoView ? on : off);
        characterLabel.text = Localization.instance.Localize("$whitehilt_media_hide_character") + ": " + (MediaMode.CharacterHidden ? on : off);
        timeLabel.text = Localization.instance.Localize("$whitehilt_media_time") + ": " + MediaMode.TimeText();
        weatherLabel.text = Localization.instance.Localize("$whitehilt_media_weather") + ": "
            + (MediaMode.Weather ?? Localization.instance.Localize("$whitehilt_media_real"));
        filmLabel.text = string.Format(Localization.instance.Localize("$whitehilt_media_film_count"), filmIndex + 1, films.Count);
        deleteFilmLabel.text = Localization.instance.Localize(Time.unscaledTime < deleteArmedUntil ? "$whitehilt_media_confirm" : "$whitehilt_media_delete");
        fadeLabel.text = Localization.instance.Localize("$whitehilt_media_fade") + ": " + (film.Fade ? on : off);
        titleCardLabel.text = Localization.instance.Localize("$whitehilt_media_titlecard") + ": " + (film.TitleCard ? on : off);
        bool smooth = selected < 0 || selected >= film.Points.Count || film.Points[selected].Smooth;
        transitionLabel.text = Localization.instance.Localize(smooth ? "$whitehilt_media_smooth" : "$whitehilt_media_linear");
    }

    // Lists the film's points, with a note when there are none yet.
    private void RefreshList()
    {
        rows.ForEach(Destroy);
        rows.Clear();
        Film film = CurrentFilm;
        float width = PanelWidth - Padding * 2f - 20f;
        if (film.Points.Count == 0)
        {
            GameObject empty = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_media_no_points"), listContent, Vector2.zero,
                Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerif, FontSize, Color.white, true, Color.black, width, PointRowHeight, false);
            empty.AddComponent<LayoutElement>().preferredHeight = PointRowHeight;
            rows.Add(empty);
            RefreshLabels();
            return;
        }

        for (int i = 0; i < film.Points.Count; i++)
        {
            rows.Add(CreateRow(film, i, width));
        }

        RefreshLabels();
    }

    // One point of the film as a row: its thumbnail, its name and its timing; clicking it selects it.
    private GameObject CreateRow(Film film, int index, float width)
    {
        FilmPoint point = film.Points[index];
        string name = index == 0 ? Localization.instance.Localize("$whitehilt_media_point_start")
            : index == film.Points.Count - 1 ? Localization.instance.Localize("$whitehilt_media_point_end")
            : string.Format(Localization.instance.Localize("$whitehilt_media_point"), index + 1);
        string transition = Localization.instance.Localize(point.Smooth ? "$whitehilt_media_smooth" : "$whitehilt_media_linear");
        string info = index == film.Points.Count - 1
            ? string.Format(Localization.instance.Localize("$whitehilt_media_point_last"), Seconds(point.Hold))
            : string.Format(Localization.instance.Localize("$whitehilt_media_point_info"), Seconds(point.Seconds), Seconds(point.Hold), transition);

        GameObject row = GUIManager.Instance.CreateButton(name + "\n" + info, listContent, Vector2.zero, Vector2.zero, Vector2.zero, width, PointRowHeight);
        row.AddComponent<LayoutElement>().preferredHeight = PointRowHeight;
        Text label = row.GetComponentInChildren<Text>();
        label.alignment = TextAnchor.MiddleLeft;
        label.fontSize = FontSize;
        label.color = index == selected ? selectedColor : Color.white;
        label.rectTransform.offsetMin = new Vector2(104f, 0f);
        row.GetComponent<Button>().onClick.AddListener(() =>
        {
            selected = index;
            RefreshList();
        });

        GameObject thumbObject = new("Thumb", typeof(RectTransform));
        thumbObject.transform.SetParent(row.transform, false);
        RectTransform thumbRect = (RectTransform)thumbObject.transform;
        thumbRect.anchorMin = new Vector2(0f, 0.5f);
        thumbRect.anchorMax = new Vector2(0f, 0.5f);
        thumbRect.pivot = new Vector2(0f, 0.5f);
        thumbRect.anchoredPosition = new Vector2(6f, 0f);
        thumbRect.sizeDelta = new Vector2(92f, 52f);
        RawImage image = thumbObject.AddComponent<RawImage>();
        image.texture = point.Thumb;
        image.color = point.Thumb != null ? Color.white : new Color(0f, 0f, 0f, 0.5f);
        image.raycastTarget = false;
        return row;
    }

    private void SetInputBlocked(bool block)
    {
        if (block != inputBlocked)
        {
            GUIManager.BlockInput(block);
            inputBlocked = block;
        }
    }

    private Text AddText(string text, int fontSize, Color color, float height, float width, float x)
    {
        Text label = GUIManager.Instance.CreateText(text, panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + x + width / 2f, -(nextRow + height / 2f)), GUIManager.Instance.AveriaSerif, fontSize, color, true, Color.black,
            width, height, false).GetComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        return label;
    }

    // A button on the panel, which does nothing while there is no local player.
    private Text AddButton(float x, float width, Action onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(string.Empty, panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + x + width / 2f, -(nextRow + RowHeight / 2f)), width, RowHeight);
        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (Player.m_localPlayer != null)
            {
                onClick();
            }
        });

        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = FontSize;
        label.resizeTextForBestFit = false;
        label.supportRichText = true;
        return label;
    }

    private InputField AddField(string token, float full, Action<string> onChanged)
    {
        float labelWidth = 80f;
        Text label = AddText(Localization.instance.Localize(token), FontSize, Color.white, RowHeight, labelWidth, 0f);
        label.alignment = TextAnchor.MiddleLeft;
        float width = full - labelWidth - RowGap;
        InputField field = GUIManager.Instance.CreateInputField(panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(Padding + labelWidth + RowGap + width / 2f, -(nextRow + RowHeight / 2f)), InputField.ContentType.Standard,
            string.Empty, FontSize, width, RowHeight).GetComponent<InputField>();
        field.onEndEdit.AddListener(value => onChanged(value.Trim()));
        NextRow();
        return field;
    }

    private void NextRow(float height = RowHeight)
    {
        nextRow += height + RowGap;
    }

    private static string Seconds(float seconds)
    {
        return seconds.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Key(KeyCode key)
    {
        return key == KeyCode.None ? string.Empty : "<color=#E8B04B>[" + BuildToolSettings.KeyName(key) + "]</color> ";
    }
}
