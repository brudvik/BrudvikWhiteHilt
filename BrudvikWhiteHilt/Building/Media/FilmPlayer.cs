using UnityEngine;

namespace BrudvikWhiteHilt.Building.Media;

/// <summary>
/// Plays a film in real time from the build camera: countdown, title card, fades and the camera gliding through the
/// points. It is meant to be recorded with the player's own screen recorder.
/// </summary>
public static class FilmPlayer
{
    private const float CountdownSeconds = 3f;
    private const float TitleSeconds = 4f;
    private const float TitleFadeSeconds = 0.8f;
    private const float FadeSeconds = 1.5f;
    private const float EndSeconds = 1f;
    private const float PreviewSpeed = 2f;
    private const float MinLegSeconds = 0.5f;

    private static Film film;
    private static bool preview;
    private static float time;
    private static bool frozeTime;

    /// <summary>True while a film plays.</summary>
    public static bool Playing => film != null;

    /// <summary>True while a film plays for recording, with the HUD hidden.</summary>
    public static bool HidesUi => film != null && !preview;

    /// <summary>
    /// Starts a film.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="toPlay">The film.</param>
    /// <param name="asPreview">True for a quick run with the panel shown and no title or fades.</param>
    public static void Play(Player player, Film toPlay, bool asPreview)
    {
        if (!BuildCamera.Active)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_media_camera_only");
            return;
        }

        if (toPlay == null || toPlay.Points.Count < 2)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_media_need_points");
            return;
        }

        Stop();
        film = toPlay;
        preview = asPreview;
        time = 0f;

        // Keep the light steady through the film when no time of day is chosen.
        if (!preview && !MediaMode.Hour.HasValue && EnvMan.instance != null)
        {
            EnvMan.instance.m_debugTime = EnvMan.instance.GetDayFraction();
            EnvMan.instance.m_debugTimeOfDay = true;
            frozeTime = true;
        }
    }

    /// <summary>
    /// Stops the film.
    /// </summary>
    public static void Stop()
    {
        if (film == null)
        {
            return;
        }

        film = null;
        MediaOverlay.ClearFilm();
        if (frozeTime && !MediaMode.Hour.HasValue && EnvMan.instance != null)
        {
            EnvMan.instance.m_debugTimeOfDay = false;
        }

        frozeTime = false;
    }

    /// <summary>
    /// Moves the film on and gives the camera pose for this frame.
    /// </summary>
    /// <param name="dt">Unscaled frame time.</param>
    /// <param name="position">Camera position.</param>
    /// <param name="rotation">Camera rotation.</param>
    /// <param name="fov">Field of view.</param>
    /// <returns>False once the film has ended.</returns>
    public static bool Update(float dt, out Vector3 position, out Quaternion rotation, out float fov)
    {
        position = default;
        rotation = default;
        fov = 0f;
        if (film == null)
        {
            return false;
        }

        time += dt * (preview ? PreviewSpeed : 1f);
        float countdown = preview ? 0f : CountdownSeconds;
        bool hasTitle = !preview && film.TitleCard && (!string.IsNullOrEmpty(film.Title) || !string.IsNullOrEmpty(film.Author));
        float titleLength = hasTitle ? TitleSeconds : 0f;
        float fade = !preview && film.Fade ? FadeSeconds : 0f;
        float path = PathLength(film);
        float end = preview ? 0f : EndSeconds;

        if (time >= countdown + titleLength + path + end)
        {
            Stop();
            return false;
        }

        float pathTime = Mathf.Clamp(time - countdown - titleLength, 0f, path);
        Evaluate(film, pathTime, out position, out rotation, out fov);
        if (preview)
        {
            return true;
        }

        if (time < countdown)
        {
            MediaOverlay.SetBlack(1f);
            MediaOverlay.SetCenter(Mathf.CeilToInt(countdown - time).ToString());
            return true;
        }

        MediaOverlay.SetCenter(string.Empty);
        if (time < countdown + titleLength)
        {
            float titleTime = time - countdown;
            float alpha = Mathf.Min(1f, titleTime / TitleFadeSeconds, (titleLength - titleTime) / TitleFadeSeconds);
            MediaOverlay.SetBlack(1f);
            MediaOverlay.SetTitle(film.Title, Subtitle(film), alpha);
            return true;
        }

        MediaOverlay.SetTitle(string.Empty, string.Empty, 0f);
        float onPath = time - countdown - titleLength;
        float blackness = 0f;
        if (fade > 0f)
        {
            blackness = onPath < fade ? 1f - onPath / fade : 0f;
            blackness = Mathf.Max(blackness, onPath > path - fade ? (onPath - (path - fade)) / fade : 0f);
        }

        MediaOverlay.SetBlack(blackness);
        return true;
    }

    private static string Subtitle(Film playing)
    {
        if (string.IsNullOrEmpty(playing.Author))
        {
            return playing.Date;
        }

        return string.IsNullOrEmpty(playing.Date) ? playing.Author : playing.Author + "  ·  " + playing.Date;
    }

    private static float PathLength(Film playing)
    {
        float length = 0f;
        for (int i = 0; i < playing.Points.Count; i++)
        {
            length += Mathf.Max(0f, playing.Points[i].Hold);
            if (i < playing.Points.Count - 1)
            {
                length += Mathf.Max(MinLegSeconds, playing.Points[i].Seconds);
            }
        }

        return length;
    }

    private static void Evaluate(Film playing, float at, out Vector3 position, out Quaternion rotation, out float fov)
    {
        var points = playing.Points;
        for (int i = 0; i < points.Count; i++)
        {
            FilmPoint point = points[i];
            float hold = Mathf.Max(0f, point.Hold);
            if (at <= hold || i == points.Count - 1)
            {
                position = point.Position;
                rotation = point.Rotation;
                fov = point.Fov;
                return;
            }

            at -= hold;
            float leg = Mathf.Max(MinLegSeconds, point.Seconds);
            if (at <= leg)
            {
                float u = at / leg;
                if (point.Smooth)
                {
                    u = u * u * (3f - 2f * u);
                }

                FilmPoint next = points[i + 1];
                position = CatmullRom(points[Mathf.Max(i - 1, 0)].Position, point.Position, next.Position,
                    points[Mathf.Min(i + 2, points.Count - 1)].Position, u);
                rotation = Quaternion.Slerp(point.Rotation, next.Rotation, u);
                fov = Mathf.Lerp(point.Fov, next.Fov, u);
                return;
            }

            at -= leg;
        }

        FilmPoint last = points[points.Count - 1];
        position = last.Position;
        rotation = last.Rotation;
        fov = last.Fov;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }
}
