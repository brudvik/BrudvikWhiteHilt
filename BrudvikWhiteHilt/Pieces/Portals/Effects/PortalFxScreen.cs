using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Pieces.Portals.Effects;

/// <summary>
/// What the travelling player sees of their own trip: the camera swinging out, the view widening, a white flash
/// with a ring of runes, and the loading screen held back so the effects can be seen. Local only.
/// </summary>
public static class PortalFxScreen
{
    private const float CollisionRadius = 0.2f;
    private const float FocusHeight = 1.4f;
    private const float CameraEaseIn = 0.5f;
    private const float FovRampSeconds = 0.6f;
    private const float VeilRise = 0.25f;
    private const float VeilFade = 0.7f;
    private const float RingLead = 0.6f;
    private const int SortingOrder = 30000;

    private static GameObject veilRoot;
    private static Image veilWhite;
    private static RawImage veilRing;
    private static long departStamp = -1L;
    private static float departStartTime;

    /// <summary>
    /// Swings the camera out as the local player leaves. Call after GameCamera.UpdateCamera.
    /// </summary>
    /// <param name="camera">The game camera.</param>
    public static void ApplyCamera(GameCamera camera)
    {
        if (camera == null || GameCamera.InFreeFly() || !PortalFxSettings.Enabled.Value
            || !PortalFxPlayer.TryGetLocal(out int phase, out float elapsed, out _)
            || phase != PortalFxPlayer.Departing || elapsed < 0f || elapsed > PortalFx.VanillaWait)
        {
            return;
        }

        float flashAt = PortalFxSettings.FlashAt.Value;
        float fov = PortalFxSettings.FieldOfView.Value * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(flashAt - FovRampSeconds, flashAt, elapsed));
        camera.m_camera.fieldOfView = camera.m_fov + fov;
        camera.m_skyCamera.fieldOfView = camera.m_camera.fieldOfView;

        Player player = Player.m_localPlayer;
        if (!PortalFxSettings.Camera.Value || player == null)
        {
            return;
        }

        float weight = Mathf.SmoothStep(0f, 1f, elapsed / CameraEaseIn);
        float progress = Mathf.SmoothStep(0f, 1f, elapsed / PortalFx.VanillaWait) * weight;
        Transform transform = camera.transform;
        Vector3 focus = player.transform.position + Vector3.up * (FocusHeight + PortalFxSettings.LiftHeight.Value * progress);
        Vector3 offset = transform.position - focus;
        float distance = offset.magnitude;
        if (distance < 0.01f)
        {
            return;
        }

        Vector3 swung = Quaternion.AngleAxis(PortalFxSettings.CameraTurn.Value * progress, Vector3.up) * (offset / distance);
        Vector3 position = focus + swung * (distance + PortalFxSettings.CameraDistance.Value * progress) + Vector3.up * (0.6f * progress);
        position = KeepClear(camera, focus, position);
        Vector3 look = focus - position;
        Quaternion rotation = look.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(look) : transform.rotation;
        transform.SetPositionAndRotation(position, Quaternion.Slerp(transform.rotation, rotation, weight));
    }

    /// <summary>
    /// Holds the loading screen back while the departure and arrival play, and draws the white flash.
    /// Call after Hud.UpdateBlackScreen.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void ApplyLoadingScreen(Hud hud)
    {
        if (hud == null || !PortalFxSettings.Enabled.Value || !PortalFxPlayer.TryGetLocal(out int phase, out float elapsed, out Color colour))
        {
            UpdateVeil(-1f, Color.white);
            departStamp = -1L;
            return;
        }

        float flashAt = PortalFxSettings.FlashAt.Value;
        float black = PortalFx.VanillaWait - 0.05f;
        if (phase == PortalFxPlayer.Departing)
        {
            long stamp = ZNet.instance.GetTime().Ticks - (long)(elapsed * System.TimeSpan.TicksPerSecond);
            if (System.Math.Abs(stamp - departStamp) > System.TimeSpan.TicksPerSecond / 2)
            {
                departStamp = stamp;
                departStartTime = Time.time - elapsed;
            }

            if (elapsed < black)
            {
                float alpha = PortalFxSettings.Flash.Value ? 0f : Mathf.InverseLerp(flashAt, black, elapsed);
                hud.m_loadingScreen.alpha = Mathf.Min(hud.m_loadingScreen.alpha, alpha);
            }
            else
            {
                hud.m_loadingScreen.alpha = 1f;
            }
        }
        else if (phase == PortalFxPlayer.Arriving && elapsed < 0.5f)
        {
            float alpha = Mathf.Clamp01(-elapsed / PortalFxPlayer.ArriveDelay);
            hud.m_loadingScreen.alpha = Mathf.Min(hud.m_loadingScreen.alpha, alpha);
        }

        UpdateVeil(departStamp >= 0L && PortalFxSettings.Flash.Value ? Time.time - departStartTime : -1f, colour);
    }

    // The white flash: a ring of runes closing in, then white over everything, fading to the loading screen.
    private static void UpdateVeil(float sinceDeparture, Color colour)
    {
        float flashAt = PortalFxSettings.FlashAt.Value;
        float end = PortalFx.VanillaWait + VeilFade;
        if (sinceDeparture < flashAt - RingLead || sinceDeparture > end)
        {
            if (veilRoot != null && veilRoot.activeSelf)
            {
                veilRoot.SetActive(false);
            }

            return;
        }

        if (!EnsureVeil())
        {
            return;
        }

        veilRoot.SetActive(true);
        float fadeOut = 1f - Mathf.Clamp01((sinceDeparture - PortalFx.VanillaWait) / VeilFade);
        float white = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(flashAt - 0.1f, flashAt + VeilRise, sinceDeparture)) * fadeOut;
        float ringShare = Mathf.InverseLerp(flashAt - RingLead, flashAt, sinceDeparture);
        float ringAlpha = Mathf.SmoothStep(0f, 1f, ringShare) * fadeOut;
        veilWhite.color = new Color(1f, 1f, 1f, white);

        float shortSide = Mathf.Min(Screen.width, Screen.height);
        float size = shortSide * Mathf.Lerp(2.4f, 0.9f, Mathf.SmoothStep(0f, 1f, ringShare));
        veilRing.rectTransform.sizeDelta = new Vector2(size, size);
        veilRing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -sinceDeparture * 90f);
        veilRing.color = new Color(colour.r, colour.g, colour.b, ringAlpha);
    }

    // The full-screen flash and rune ring the travelling player sees, made once on an overlay canvas that survives
    // scene changes.
    private static bool EnsureVeil()
    {
        if (veilRoot != null)
        {
            return true;
        }

        Texture2D ring = PortalFx.RingTexture;
        if (ring == null)
        {
            return false;
        }

        veilRoot = new GameObject("whitehilt_portalfx_veil");
        Object.DontDestroyOnLoad(veilRoot);
        Canvas canvas = veilRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        GameObject whiteObject = new("white", typeof(RectTransform));
        whiteObject.transform.SetParent(veilRoot.transform, false);
        veilWhite = whiteObject.AddComponent<Image>();
        veilWhite.raycastTarget = false;
        RectTransform whiteRect = veilWhite.rectTransform;
        whiteRect.anchorMin = Vector2.zero;
        whiteRect.anchorMax = Vector2.one;
        whiteRect.offsetMin = Vector2.zero;
        whiteRect.offsetMax = Vector2.zero;

        GameObject ringObject = new("ring", typeof(RectTransform));
        ringObject.transform.SetParent(veilRoot.transform, false);
        veilRing = ringObject.AddComponent<RawImage>();
        veilRing.texture = ring;
        veilRing.raycastTarget = false;
        RectTransform ringRect = veilRing.rectTransform;
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        veilRoot.SetActive(false);
        return true;
    }

    // Pulls the camera in front of walls and ground between it and the player, as the game's own camera does.
    private static Vector3 KeepClear(GameCamera camera, Vector3 focus, Vector3 position)
    {
        Vector3 offset = position - focus;
        float distance = offset.magnitude;
        if (distance > CollisionRadius
            && Physics.SphereCast(focus, CollisionRadius, offset / distance, out RaycastHit hit, distance, camera.m_blockCameraMask, QueryTriggerInteraction.Ignore))
        {
            position = focus + offset / distance * Mathf.Max(0f, hit.distance - CollisionRadius);
        }

        return position;
    }
}
