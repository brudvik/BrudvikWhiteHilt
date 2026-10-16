using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// The darkness heard and glimpsed by the local player: the flames guttering before it, snuffed out and flaring back,
/// and in the black glowing eyes that blink and are gone, scraping in the walls, breathing and whispers; from half fear
/// the heart pounds and the view trembles. None of it is real or seen by anyone else. On the <see cref="Game"/> object.
/// </summary>
public class DarknessSounds : MonoBehaviour
{
    /// <summary>The flames guttering as the darkness comes.</summary>
    public static readonly string[] Gutter = { "sfx_dark_gutter", "sfx_dark_gutter_2" };

    /// <summary>Every flame snuffed out at once.</summary>
    public static readonly string[] Out = { "sfx_dark_out" };

    /// <summary>The flames flaring back.</summary>
    public static readonly string[] Return = { "sfx_dark_return" };

    private static readonly string[] scrapes = { "sfx_dark_scrape", "sfx_dark_scrape_2", "sfx_dark_scrape_3" };
    private static readonly string[] breaths = { "sfx_dark_breath", "sfx_dark_breath_2" };
    private static readonly string[] whispers = { "sfx_storm_whisper", "sfx_storm_whisper_2", "sfx_storm_whisper_3" };
    private const string Heartbeat = "sfx_dark_heartbeat";
    private const float EyesNearest = 7f;
    private const float EyesFarthest = 15f;
    private const float ShakeSeconds = 0.5f;

    private static readonly string[] caveLines =
    {
        "$whitehilt_stormvoice_cave1", "$whitehilt_stormvoice_cave2", "$whitehilt_stormvoice_cave3",
        "$whitehilt_stormvoice_cave4", "$whitehilt_stormvoice_cave5", "$whitehilt_stormvoice_cave6"
    };

    private static Material eyeMaterial;
    private static int solidMask;

    private AudioSource heart;
    private float nextHaunt;
    private float nextShake;
    private GameObject eyes;
    private float eyesGone;
    private float eyesBlink;

    /// <summary>
    /// Registers the English lines the voice in the dark says, the storm's voice come below ground. Call from the
    /// plugin's Awake.
    /// </summary>
    public static void AddTexts()
    {
        Translations.AddEnglish("whitehilt_stormvoice_cave1", "Turn back...");
        Translations.AddEnglish("whitehilt_stormvoice_cave2", "It is right behind you.");
        Translations.AddEnglish("whitehilt_stormvoice_cave3", "We have waited so long for you...");
        Translations.AddEnglish("whitehilt_stormvoice_cave4", "Let the light go out...");
        Translations.AddEnglish("whitehilt_stormvoice_cave5", "You should never have come down here.");
        Translations.AddEnglish("whitehilt_stormvoice_cave6", "Can you hear it breathing?");
    }

    /// <summary>
    /// Plays one of a darkness sound's takes at a place, at the darkness's volume.
    /// </summary>
    /// <param name="takes">The sound's takes.</param>
    /// <param name="position">Where.</param>
    /// <param name="near">Distance it is heard at full volume.</param>
    /// <param name="far">Distance it fades out at.</param>
    public static void Play(string[] takes, Vector3 position, float near = 3f, float far = 30f)
    {
        float volume = (SoundSettings.EffectsVolume?.Value ?? 1f) * (DarknessSettings.Volume?.Value ?? 1f);
        AudioClip clip = VisualHelper.IsHeadless || volume <= 0f || takes.Length == 0 ? null : AudioAssets.TryLoad(takes[Random.Range(0, takes.Length)]);
        if (clip == null)
        {
            return;
        }

        GameObject holder = new($"whitehilt_sound_{clip.name}");
        holder.transform.position = position;
        AudioSource source = holder.AddComponent<AudioSource>();
        WhiteHiltSound.Place(source, near, far);
        source.PlayOneShot(clip, volume);
        Destroy(holder, clip.length + 0.5f);
    }

    private void Start()
    {
        if (VisualHelper.IsHeadless)
        {
            enabled = false;
            return;
        }

        solidMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
        heart = gameObject.AddComponent<AudioSource>();
        heart.playOnAwake = false;
        heart.spatialBlend = 0f;
        heart.loop = true;
        heart.volume = 0f;
        heart.outputAudioMixerGroup = WhiteHiltSound.EffectsMixer;
    }

    private void OnDestroy()
    {
        if (eyes != null)
        {
            Destroy(eyes);
        }
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        UpdateHeart(player);
        UpdateEyes(player);
        if (player == null || player.IsDead() || DarknessSettings.EyesAndSounds == null || !DarknessSettings.EyesAndSounds.Value
            || DarknessClient.Phase != DarkPhase.Black)
        {
            nextHaunt = Mathf.Max(nextHaunt, Time.time + 3f);
            return;
        }

        if (Time.time < nextHaunt)
        {
            return;
        }

        nextHaunt = Time.time + Random.Range(6f, 14f);
        float roll = Random.value;
        Vector2 around = Random.insideUnitCircle.normalized;
        Vector3 aside = new(around.x, 0f, around.y);
        if (roll < 0.4f && eyes == null)
        {
            ShowEyes(player, aside);
        }
        else if (roll < 0.7f)
        {
            Play(scrapes, player.transform.position + aside * Random.Range(4f, 9f) + Vector3.up, 2f, 18f);
        }
        else if (roll < 0.8f)
        {
            Play(breaths, player.GetHeadPoint() - player.transform.forward * 1.2f, 1f, 6f);
        }
        else if (roll < 0.88f)
        {
            Play(whispers, player.GetHeadPoint() + aside * 1.5f, 1f, 8f);
        }
        else
        {
            Speak(player, aside);
        }
    }

    // The voice from down a passage: where the way is open, as far as it goes, up to 20 m.
    private static void Speak(Player player, Vector3 aside)
    {
        Vector3 head = player.GetHeadPoint();
        float distance = Physics.Raycast(head, aside, out RaycastHit hit, 20f, solidMask) ? Mathf.Max(3f, hit.distance - 0.5f) : 20f;
        AudioClip clip = WhiteHiltSound.VoiceClip("stormvoice", caveLines[Random.Range(0, caveLines.Length)]);
        float volume = (SoundSettings.VoiceVolume?.Value ?? 1f) * DarknessSettings.Volume.Value;
        if (clip == null || volume <= 0f)
        {
            return;
        }

        GameObject holder = new($"whitehilt_sound_{clip.name}");
        holder.transform.position = head + aside * distance;
        AudioSource source = holder.AddComponent<AudioSource>();
        WhiteHiltSound.Place(source, 4f, 30f);
        source.PlayOneShot(clip, volume);
        Destroy(holder, clip.length + 0.5f);
    }

    // From half fear the heart pounds, louder the worse it is, and the view trembles.
    private void UpdateHeart(Player player)
    {
        float fear = player != null && !player.IsDead() ? DarknessClient.Fear : 0f;
        float share = Mathf.Clamp01((fear - DarknessClient.Terrified) / (DarknessClient.MaxFear - DarknessClient.Terrified));
        float target = fear >= DarknessClient.Terrified ? Mathf.Lerp(0.35f, 1f, share) * (SoundSettings.EffectsVolume?.Value ?? 1f) * DarknessSettings.Volume.Value : 0f;
        heart.volume = Mathf.MoveTowards(heart.volume, target, Time.deltaTime);
        if (heart.volume > 0f && !heart.isPlaying)
        {
            heart.clip ??= AudioAssets.TryLoad(Heartbeat);
            if (heart.clip != null)
            {
                heart.Play();
            }
        }
        else if (heart.volume <= 0f && heart.isPlaying)
        {
            heart.Stop();
        }

        heart.pitch = Mathf.Lerp(1f, 1.25f, share);
        if (fear >= DarknessClient.Terrified && GameCamera.instance != null && Time.time >= nextShake)
        {
            nextShake = Time.time + ShakeSeconds;
            GameCamera.instance.AddShake(player.transform.position, 10f, Mathf.Lerp(0.05f, 0.25f, share), false);
        }
    }

    // A pair of pale eyes in the black, off where the dark is deepest, facing the player; they blink once and are gone.
    private void ShowEyes(Player player, Vector3 aside)
    {
        Vector3 head = player.GetHeadPoint();
        float distance = Random.Range(EyesNearest, EyesFarthest);
        if (Physics.Raycast(head, aside, out RaycastHit hit, distance, solidMask))
        {
            distance = hit.distance - 0.6f;
        }

        if (distance < 4f)
        {
            return;
        }

        eyeMaterial ??= VisualHelper.UnlitMaterial(new Color(1f, 0.85f, 0.45f));
        if (eyeMaterial == null)
        {
            return;
        }

        Vector3 at = head + aside * distance + Vector3.up * Random.Range(-0.6f, 0.2f);
        eyes = new GameObject("whitehilt_dark_eyes");
        eyes.AddComponent<DarknessKeep>();
        eyes.transform.position = at;
        eyes.transform.rotation = Quaternion.LookRotation(head - at);
        foreach (float side in new[] { -0.07f, 0.07f })
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(eyes.transform, false);
            eye.transform.localPosition = new Vector3(side, 0f, 0f);
            eye.transform.localScale = new Vector3(0.05f, 0.025f, 0.03f);
            Renderer renderer = eye.GetComponent<Renderer>();
            renderer.sharedMaterial = eyeMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        eyesBlink = Time.time + Random.Range(1f, 2f);
        eyesGone = eyesBlink + Random.Range(1.5f, 3f);
    }

    private void UpdateEyes(Player player)
    {
        if (eyes == null)
        {
            return;
        }

        bool gone = player == null || DarknessClient.Phase != DarkPhase.Black || Time.time >= eyesGone;
        if (gone)
        {
            Destroy(eyes);
            eyes = null;
            return;
        }

        // A blink: shut for a fifth of a second.
        bool shut = Time.time >= eyesBlink && Time.time < eyesBlink + 0.2f;
        eyes.SetActive(!shut);
    }
}
