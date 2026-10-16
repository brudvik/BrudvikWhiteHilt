using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.TradingPosts;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// What a player deep in the storm hears and sees, out in it with the meter past <see cref="StormExposure.Weary"/>:
/// footsteps right behind them, whispers at their ear, a voice calling from the fog, and once they freeze a dark figure
/// at the edge of the view that is gone when they look at it or come close. None of it is real and none of it is seen by
/// anyone else; it comes more often the worse the meter, and more often still in Ragnarok. On the <see cref="Game"/>
/// object of every machine with a player.
/// </summary>
public class StormVisions : MonoBehaviour
{
    private const float FirstSeconds = 8f;
    private const float LeastSeconds = 14f;
    private const float MostSeconds = 35f;
    private const float RagnarokShare = 0.7f;
    private const float FigureNearest = 24f;
    private const float FigureFarthest = 34f;
    private const float FigureLeastAngle = 22f;
    private const float FigureMostAngle = 38f;
    private const float LookedAt = 10f;
    private const float TooClose = 14f;
    private const float FigureSeconds = 9f;
    private const string Speaker = "stormvoice";
    private const string TemplateName = "WhiteHilt_StormFigure";

    private static readonly string[] steps = { "sfx_storm_steps", "sfx_storm_steps_2" };
    private static readonly string[] whispers = { "sfx_storm_whisper", "sfx_storm_whisper_2", "sfx_storm_whisper_3" };

    private static readonly string[] voiceLines =
    {
        "$whitehilt_stormvoice_call1", "$whitehilt_stormvoice_call2", "$whitehilt_stormvoice_call3",
        "$whitehilt_stormvoice_call4", "$whitehilt_stormvoice_call5", "$whitehilt_stormvoice_call6"
    };

    private static GameObject template;
    private static readonly System.Collections.Generic.Dictionary<Material, Material> shades = new();

    private bool haunting;
    private float nextVision;
    private GameObject figure;
    private float figureGone;

    /// <summary>
    /// Registers the English lines the voice in the storm says. Call from the plugin's Awake.
    /// </summary>
    public static void AddTexts()
    {
        Translations.AddEnglish("whitehilt_stormvoice_call1", "Over here...");
        Translations.AddEnglish("whitehilt_stormvoice_call2", "Come... it is warm here...");
        Translations.AddEnglish("whitehilt_stormvoice_call3", "Help me... I cannot see...");
        Translations.AddEnglish("whitehilt_stormvoice_call4", "You will not make it home.");
        Translations.AddEnglish("whitehilt_stormvoice_call5", "Why did you leave me out here?");
        Translations.AddEnglish("whitehilt_stormvoice_call6", "Stay... stay with us...");
    }

    /// <summary>
    /// The seconds until the next vision.
    /// </summary>
    /// <param name="exposure">The meter.</param>
    /// <param name="ragnarok">Whether Ragnarok rages.</param>
    /// <param name="roll">A random number from 0 to 1.</param>
    /// <returns>From 14 to 35 seconds when weary, down to half that at the top of the meter, less in Ragnarok.</returns>
    public static float Interval(float exposure, bool ragnarok, float roll)
    {
        float worse = Mathf.Clamp01((exposure - StormExposure.Weary) / (StormExposure.Max - StormExposure.Weary));
        return Mathf.Lerp(LeastSeconds, MostSeconds, Mathf.Clamp01(roll)) * Mathf.Lerp(1f, 0.5f, worse) * (ragnarok ? RagnarokShare : 1f);
    }

    private void Update()
    {
        UpdateFigure();
        Player player = Player.m_localPlayer;
        bool haunted = player != null && !player.IsDead() && !VisualHelper.IsHeadless && StormSettings.Visions.Value
            && StormClient.RagingHere && StormClient.Cover is StormCover.Open or StormCover.Lee or StormCover.Forest
            && StormClient.Exposure >= StormExposure.Weary && GameCamera.instance != null;
        if (!haunted)
        {
            haunting = false;
            return;
        }

        if (!haunting)
        {
            haunting = true;
            nextVision = Time.time + FirstSeconds + Random.Range(0f, FirstSeconds);
            return;
        }

        if (Time.time < nextVision)
        {
            return;
        }

        nextVision = Time.time + Interval(StormClient.Exposure, StormState.Ragnarok, Random.value);
        Haunt(player);
    }

    // One vision: steps, a whisper or the voice, and once the player freezes now and then the figure.
    private void Haunt(Player player)
    {
        Transform view = GameCamera.instance.transform;
        Vector3 forward = Flat(view.forward);
        Vector3 right = Flat(view.right);
        bool freezing = StormClient.Exposure >= StormExposure.Freezing;
        float roll = Random.value;
        if (freezing && figure == null && roll < 0.3f && ShowFigure(player, forward))
        {
            return;
        }

        if (roll < 0.55f)
        {
            StormHazards.PlayAt(steps[Random.Range(0, steps.Length)], player.transform.position - forward * Random.Range(3f, 5f), 2f, 14f);
        }
        else if (roll < 0.8f)
        {
            Vector3 side = right * (Random.value < 0.5f ? -1.5f : 1.5f);
            StormHazards.PlayAt(whispers[Random.Range(0, whispers.Length)], player.GetHeadPoint() + side, 1f, 8f);
        }
        else
        {
            Vector2 around = Random.insideUnitCircle.normalized * Random.Range(16f, 26f);
            Vector3 from = player.transform.position + new Vector3(around.x, 1.5f, around.y);
            StormHazards.PlayAt(WhiteHiltSound.VoiceClip(Speaker, voiceLines[Random.Range(0, voiceLines.Length)]), from, 6f, 40f);
        }
    }

    // A dark figure standing on dry ground off to one side of where the player looks, facing them.
    private bool ShowFigure(Player player, Vector3 forward)
    {
        float angle = Random.Range(FigureLeastAngle, FigureMostAngle) * (Random.value < 0.5f ? -1f : 1f);
        Vector3 direction = Quaternion.Euler(0f, angle, 0f) * forward;
        Vector3 point = player.transform.position + direction * Random.Range(FigureNearest, FigureFarthest);
        if (ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(point, out float ground) || ground < ZoneSystem.instance.m_waterLevel + 0.3f)
        {
            return false;
        }

        GameObject made = Template();
        if (made == null)
        {
            return false;
        }

        point.y = ground;
        Vector3 toPlayer = Flat(player.transform.position - point);
        ZNetView.m_forceDisableInit = true;
        try
        {
            figure = Instantiate(made, point, toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity);
        }
        finally
        {
            ZNetView.m_forceDisableInit = false;
        }

        figure.name = "WhiteHilt_StormFigure";
        figure.SetActive(true);
        figureGone = Time.time + FigureSeconds;
        return true;
    }

    // The figure is gone when looked at, come close to, after a while, or when the storm lets go.
    private void UpdateFigure()
    {
        if (figure == null)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        bool gone = player == null || player.IsDead() || !StormClient.RagingHere || Time.time >= figureGone || GameCamera.instance == null;
        if (!gone)
        {
            Vector3 toFigure = figure.transform.position + Vector3.up * 1.2f - GameCamera.instance.transform.position;
            gone = Vector3.Angle(GameCamera.instance.transform.forward, toFigure) < LookedAt
                || Vector3.Distance(player.transform.position, figure.transform.position) < TooClose;
        }

        if (gone)
        {
            Destroy(figure);
            figure = null;
        }
    }

    private void OnDestroy()
    {
        if (figure != null)
        {
            Destroy(figure);
        }
    }

    private static Vector3 Flat(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    // A player's body with nothing of the player left in it, no collider or physics, all in one unlit dark.
    private static GameObject Template()
    {
        if (template != null)
        {
            return template;
        }

        GameObject prefab = PrefabManager.Instance.GetPrefab(TemplateName) ?? PrefabManager.Instance.CreateClonedPrefab(TemplateName, "Player");
        if (prefab == null)
        {
            return null;
        }

        TradingPostTrader.StripToBody(prefab);
        foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
        {
            DestroyImmediate(collider);
        }

        foreach (Rigidbody body in prefab.GetComponentsInChildren<Rigidbody>(true))
        {
            DestroyImmediate(body);
        }

        // The body's own materials gone dark, so the fog still swallows it with distance; the game has no plain
        // shader to draw it with.
        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = Darkened(renderer.sharedMaterials[i]);
            }

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        template = prefab;
        return template;
    }

    private static Material Darkened(Material source)
    {
        if (source == null)
        {
            return null;
        }

        if (!shades.TryGetValue(source, out Material dark))
        {
            dark = new Material(source);
            if (dark.HasProperty("_Color"))
            {
                dark.color = new Color(0.02f, 0.02f, 0.03f, dark.color.a);
            }

            if (dark.HasProperty("_MainTex"))
            {
                dark.mainTexture = Texture2D.blackTexture;
            }

            if (dark.HasProperty("_EmissionColor"))
            {
                dark.SetColor("_EmissionColor", Color.black);
            }

            shades[source] = dark;
        }

        return dark;
    }
}
