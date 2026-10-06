using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BrudvikWhiteHilt.Building.Media;

/// <summary>
/// Media state of the build camera: photo view, hidden HUD and build helpers, a hidden character, local time of day,
/// local weather and zoom. Everything here only changes what this player sees, and is undone when the camera stops.
/// </summary>
public static class MediaMode
{
    private const float MinFov = 10f;
    private const float MaxFov = 100f;
    private const float FovPerWheel = 20f;

    private static readonly List<Renderer> hiddenRenderers = new();

    private static bool hudHiddenByUs;
    private static bool savedHudHidden;
    private static bool smallMapHiddenByUs;

    /// <summary>True while photo view is on: only the world shows and the mouse wheel zooms.</summary>
    public static bool PhotoView { get; private set; }

    /// <summary>True for the few frames a photo or a thumbnail is taken.</summary>
    public static bool Capturing { get; private set; }

    /// <summary>Zoom set with the mouse wheel, or null for the game's own field of view.</summary>
    public static float? Fov { get; private set; }

    /// <summary>Local time of day in hours, or null for the real time.</summary>
    public static float? Hour { get; private set; }

    /// <summary>Local weather, or null for the real weather.</summary>
    public static string Weather { get; private set; }

    /// <summary>True while the player's own character is hidden.</summary>
    public static bool CharacterHidden { get; private set; }

    /// <summary>True while HUD, toolbar and build helpers are to be hidden.</summary>
    public static bool HideUi => PhotoView || Capturing || FilmPlayer.HidesUi;

    /// <summary>
    /// Keeps the HUD hidden while wanted, and zooms with the mouse wheel. Called every frame for the local player.
    /// </summary>
    public static void Tick()
    {
        ApplyHud();
        if (!BuildCamera.Active || FilmPlayer.Playing || !(PhotoView || MediaPanel.IsOpen) || GameCamera.instance == null)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        float wheel = ZInput.GetMouseScrollWheel();
        if (wheel != 0f)
        {
            Fov = Mathf.Clamp((Fov ?? GameCamera.instance.m_fov) - wheel * FovPerWheel, MinFov, MaxFov);
        }
    }

    /// <summary>
    /// The field of view the camera should use now.
    /// </summary>
    /// <param name="gameFov">The game's own field of view.</param>
    /// <returns>The field of view.</returns>
    public static float CurrentFov(float gameFov)
    {
        return Fov ?? gameFov;
    }

    /// <summary>
    /// Sets the zoom, e.g. when going back to a film point.
    /// </summary>
    /// <param name="fov">Field of view in degrees.</param>
    public static void SetFov(float fov)
    {
        Fov = Mathf.Clamp(fov, MinFov, MaxFov);
    }

    /// <summary>
    /// Photo view on or off.
    /// </summary>
    public static void TogglePhotoView()
    {
        PhotoView = !PhotoView;
        ApplyHud();
    }

    /// <summary>
    /// Takes a photo without HUD or build helpers, in the game's screenshot folder.
    /// </summary>
    /// <param name="host">Runs the capture over a few frames.</param>
    public static void TakePhoto(MonoBehaviour host)
    {
        if (!Capturing)
        {
            host.StartCoroutine(PhotoRoutine());
        }
    }

    /// <summary>
    /// Takes a small picture of the current view without HUD or panels.
    /// </summary>
    /// <param name="host">Runs the capture over a few frames.</param>
    /// <param name="done">Gets the picture.</param>
    public static void TakeThumbnail(MonoBehaviour host, Action<Texture2D> done)
    {
        if (!Capturing)
        {
            host.StartCoroutine(ThumbnailRoutine(done));
        }
    }

    /// <summary>
    /// Shows or hides the player's own character.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void ToggleCharacter(Player player)
    {
        if (CharacterHidden)
        {
            ShowCharacter();
            return;
        }

        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
        {
            if (renderer.enabled)
            {
                renderer.enabled = false;
                hiddenRenderers.Add(renderer);
            }
        }

        CharacterHidden = true;
    }

    /// <summary>
    /// Moves the local time of day by whole hours, starting from the real time.
    /// </summary>
    /// <param name="hours">Hours to move.</param>
    public static void ShiftHour(int hours)
    {
        if (EnvMan.instance == null)
        {
            return;
        }

        float start = Hour ?? Mathf.Round(EnvMan.instance.GetDayFraction() * 24f);
        SetHour(Mathf.Repeat(start + hours, 24f));
    }

    /// <summary>
    /// Goes back to the real time of day.
    /// </summary>
    public static void RealTime()
    {
        Hour = null;
        if (EnvMan.instance != null)
        {
            EnvMan.instance.m_debugTimeOfDay = false;
        }
    }

    /// <summary>
    /// Steps through the game's weathers.
    /// </summary>
    /// <param name="direction">1 next, -1 previous.</param>
    public static void NextWeather(int direction)
    {
        if (EnvMan.instance == null)
        {
            return;
        }

        List<string> names = EnvMan.instance.m_environments.Select(environment => environment.m_name).Where(name => !string.IsNullOrEmpty(name))
            .Distinct().ToList();
        if (names.Count == 0)
        {
            return;
        }

        int index = Weather == null ? (direction > 0 ? 0 : names.Count - 1) : (names.IndexOf(Weather) + direction + names.Count) % names.Count;
        Weather = names[index];
        EnvMan.instance.m_debugEnv = Weather;
    }

    /// <summary>
    /// Goes back to the real weather.
    /// </summary>
    public static void RealWeather()
    {
        Weather = null;
        if (EnvMan.instance != null)
        {
            EnvMan.instance.m_debugEnv = string.Empty;
        }
    }

    /// <summary>
    /// Formats the local time of day, e.g. "14:00", or the word for real time.
    /// </summary>
    /// <returns>The text.</returns>
    public static string TimeText()
    {
        return Hour.HasValue ? $"{Mathf.FloorToInt(Hour.Value):00}:00" : Localization.instance.Localize("$whitehilt_media_real");
    }

    /// <summary>
    /// Undoes everything: photo view, zoom, time, weather, hidden character and HUD. Called when the camera stops.
    /// </summary>
    public static void Reset()
    {
        PhotoView = false;
        Capturing = false;
        Fov = null;
        if (Hour.HasValue)
        {
            RealTime();
        }

        if (Weather != null)
        {
            RealWeather();
        }

        ShowCharacter();
        ApplyHud();
    }

    private static void SetHour(float hour)
    {
        Hour = hour;
        EnvMan.instance.m_debugTimeOfDay = true;
        EnvMan.instance.m_debugTime = hour / 24f;
    }

    private static void ShowCharacter()
    {
        foreach (Renderer renderer in hiddenRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        hiddenRenderers.Clear();
        CharacterHidden = false;
    }

    // Hides the HUD and the small map while the screen is cleared for photos and films, and gives back exactly what the
    // player had shown before, as the player may have hidden the HUD already.
    private static void ApplyHud()
    {
        bool hide = HideUi;
        if (hide && !hudHiddenByUs && Hud.instance != null)
        {
            savedHudHidden = Hud.instance.m_userHidden;
            Hud.instance.m_userHidden = true;
            hudHiddenByUs = true;
            if (Minimap.instance != null && Minimap.instance.m_smallRoot != null && Minimap.instance.m_smallRoot.activeSelf)
            {
                Minimap.instance.m_smallRoot.SetActive(false);
                smallMapHiddenByUs = true;
            }
        }
        else if (!hide && hudHiddenByUs)
        {
            if (Hud.instance != null)
            {
                Hud.instance.m_userHidden = savedHudHidden;
            }

            if (smallMapHiddenByUs && Minimap.instance != null && Minimap.instance.m_mode == Minimap.MapMode.Small)
            {
                Minimap.instance.m_smallRoot.SetActive(true);
            }

            hudHiddenByUs = false;
            smallMapHiddenByUs = false;
        }
    }

    // Takes a photo: hides the HUD, waits two frames for it to be gone from the picture, saves the screenshot (at up to
    // four times the screen size) and shows the HUD again.
    private static IEnumerator PhotoRoutine()
    {
        Capturing = true;
        ApplyHud();
        yield return null;
        yield return null;

        string folder = Path.Combine(Utils.GetSaveDataPath(FileHelpers.FileSource.Local), "screenshots");
        Directory.CreateDirectory(folder);
        string file = "whitehilt_" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".png";
        int size = Mathf.Clamp(MediaSettings.PhotoSize.Value, 1, 4);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, file), size);
        yield return null;
        yield return null;

        Capturing = false;
        ApplyHud();
        MediaOverlay.Toast(string.Format(Localization.instance.Localize("$msg_whitehilt_media_saved"), file));
    }

    // Takes a small picture of the current view for a film point: hides the HUD, waits for the end of the frame,
    // captures the screen and scales it down on the GPU.
    private static IEnumerator ThumbnailRoutine(Action<Texture2D> done)
    {
        Capturing = true;
        ApplyHud();
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        Capturing = false;
        ApplyHud();

        RenderTexture small = RenderTexture.GetTemporary(FilmPoint.ThumbWidth, FilmPoint.ThumbHeight, 0);
        Graphics.Blit(shot, small);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = small;
        Texture2D thumb = new(FilmPoint.ThumbWidth, FilmPoint.ThumbHeight, TextureFormat.RGB24, false);
        thumb.ReadPixels(new Rect(0f, 0f, FilmPoint.ThumbWidth, FilmPoint.ThumbHeight), 0, 0);
        thumb.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(small);
        UnityEngine.Object.Destroy(shot);
        done(thumb);
    }
}
