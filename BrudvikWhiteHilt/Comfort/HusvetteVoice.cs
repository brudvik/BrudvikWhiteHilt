using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Comfort;

/// <summary>
/// The Husvette talks to the local player, aloud and in a speech bubble: he welcomes them home after a while away,
/// murmurs when they sit down to rest, grumbles when the room is cold with no fire burning at night, and is pleased
/// when the room grows cosier. Runs on every machine for its own player; nothing is sent.
/// </summary>
public class HusvetteVoice : MonoBehaviour
{
    /// <summary>Whose recorded voice the Husvette speaks with.</summary>
    public const string Speaker = "husvette";

    private const float CheckInterval = 2f;
    private const float NearRange = 14f;
    private const float AwaySeconds = 300f;
    private const float LinePause = 600f;
    private const float BubbleHeight = 0.9f;
    private const float BubbleTime = 5f;
    private const float StormCallRange = 50f;

    // The storm whose warning, and whose raging with the player inside, a Husvette has spoken of.
    private static int warnedStorm = -1;
    private static int shelteredStorm = -1;

    // Only one Husvette speaks at a time, wherever the player is.
    private static float lastSpoken = float.NegativeInfinity;

    private Husvette husvette;
    private AudioSource voice;
    private float nextCheck;
    private float lastNear = float.NegativeInfinity;
    private float nextRest;
    private float nextCold;
    private float nextCosy;
    private int lastComfort = -1;

    /// <summary>
    /// Registers the Husvette's lines. Call from the plugin's Awake, before the game loads its languages.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_husvette_home1", "Ah, there you are. Wipe your feet.");
        Translations.AddEnglish("whitehilt_husvette_home2", "Home again! I kept the place for you.");
        Translations.AddEnglish("whitehilt_husvette_home3", "Back at last. The porridge is cold, you know.");
        Translations.AddEnglish("whitehilt_husvette_rest1", "Sit, sit. Rest your bones a while.");
        Translations.AddEnglish("whitehilt_husvette_rest2", "Mmm. A warm fire and a full belly. That is a good life.");
        Translations.AddEnglish("whitehilt_husvette_rest3", "Close your eyes. I keep watch.");
        Translations.AddEnglish("whitehilt_husvette_cold1", "Brr! Who let the fire go out?");
        Translations.AddEnglish("whitehilt_husvette_cold2", "A cold hearth is a sad house. Light a fire!");
        Translations.AddEnglish("whitehilt_husvette_cold3", "My old bones are freezing. Some fire, please.");
        Translations.AddEnglish("whitehilt_husvette_cosy1", "Oh, that is nice. Very homely.");
        Translations.AddEnglish("whitehilt_husvette_cosy2", "Now this feels like a real hall.");
        Translations.AddEnglish("whitehilt_husvette_cosy3", "Cosier and cosier. I like it here.");
        Translations.AddEnglish("whitehilt_husvette_storm1", "A storm is coming! Inside with you, and light the fire!");
        Translations.AddEnglish("whitehilt_husvette_storm2", "Feel that wind? A great storm. Hurry home!");
        Translations.AddEnglish("whitehilt_husvette_storm3", "The sky turns black. Get under the roof, quickly!");
        Translations.AddEnglish("whitehilt_husvette_shelter1", "Listen to it howl out there. Good thing we have walls.");
        Translations.AddEnglish("whitehilt_husvette_shelter2", "Stay by the fire until it passes. Nobody goes out in that.");
        Translations.AddEnglish("whitehilt_husvette_shelter3", "Let it rage. In here we are snug and dry.");
    }

    private void Awake()
    {
        husvette = GetComponent<Husvette>();
    }

    private void Update()
    {
        if (Time.time < nextCheck || husvette == null || !husvette.IsBuilt)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead())
        {
            return;
        }

        float distance = Vector3.Distance(player.transform.position, transform.position);
        bool near = distance <= NearRange && player.InShelter();

        // A storm coming: the Husvette calls the player in from around the house, once per storm.
        if (Storms.StormState.Coming && distance <= StormCallRange && warnedStorm != Storms.StormState.Number && Say("storm", force: true))
        {
            warnedStorm = Storms.StormState.Number;
            return;
        }

        if (!near)
        {
            return;
        }

        if (Storms.StormState.Raging && shelteredStorm != Storms.StormState.Number && Say("shelter", force: true))
        {
            shelteredStorm = Storms.StormState.Number;
            lastNear = Time.time;
            return;
        }

        bool cameHome = Time.time - lastNear > AwaySeconds && lastNear > float.NegativeInfinity;
        bool first = lastNear == float.NegativeInfinity;
        lastNear = Time.time;
        int comfort = player.GetComfortLevel();
        if (cameHome)
        {
            Say("home");
        }
        else if (player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectResting) && Time.time >= nextRest)
        {
            nextRest = Time.time + LinePause;
            Say("rest");
        }
        else if (EnvMan.IsNight() && !player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectCampFire) && Time.time >= nextCold)
        {
            nextCold = Time.time + LinePause;
            Say("cold");
        }
        else if (!first && lastComfort >= 0 && comfort > lastComfort && Time.time >= nextCosy)
        {
            nextCosy = Time.time + LinePause;
            Say("cosy");
        }

        lastComfort = comfort;
    }

    private bool Say(string kind, bool force = false)
    {
        if (!force && Time.time - lastSpoken < 20f)
        {
            return false;
        }

        lastSpoken = Time.time;
        voice ??= WhiteHiltSound.AddVoice(gameObject);
        string token = $"$whitehilt_husvette_{kind}{Random.Range(1, 4)}";
        if (Chat.instance != null)
        {
            Chat.instance.SetNpcText(gameObject, Vector3.up * BubbleHeight, 20f, BubbleTime, string.Empty, token, false);
        }

        WhiteHiltSound.Speak(voice, Speaker, token);
        return true;
    }
}
