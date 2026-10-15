using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles;

/// <summary>
/// The battle cries of a Nail Ship's crew and a draugr harbour's guards: when one leaps aboard a ship, and now and
/// then while it fights, it shouts, aloud and in a speech bubble. Runs on every machine on its own, nothing is sent; a
/// short pause between any two shouts keeps a whole crew from shouting at once.
/// </summary>
public class RaiderVoice : MonoBehaviour
{
    private const float CheckInterval = 2f;
    private const float ShoutChance = 0.12f;
    private const float OwnPause = 12f;
    private const float AnyPause = 3.5f;
    private const float BubbleHeight = 2.4f;
    private const float BubbleTime = 4f;

    private static float lastShout;

    private RaiderCrew crew;
    private AudioSource voice;
    private string speaker;
    private bool wasBoarded;
    private float nextCheck;
    private float nextOwn;

    /// <summary>
    /// Registers the crews' cries. Call from the plugin's Awake, before the game loads its languages.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_raider_board1", "Take the cargo! Take it all!");
        Translations.AddEnglish("whitehilt_raider_board2", "Over the rail, brothers!");
        Translations.AddEnglish("whitehilt_raider_board3", "This ship is ours now!");
        Translations.AddEnglish("whitehilt_raider_attack1", "For the Nail!");
        Translations.AddEnglish("whitehilt_raider_attack2", "Cut them down!");
        Translations.AddEnglish("whitehilt_raider_attack3", "No mercy for the living!");
        Translations.AddEnglish("whitehilt_raider_taunt1", "Your ship sinks tonight!");
        Translations.AddEnglish("whitehilt_raider_taunt2", "Run, little merchant, run!");
        Translations.AddEnglish("whitehilt_raider_taunt3", "The sea will have your bones!");
    }

    /// <summary>
    /// Gives a crew member its voice, once.
    /// </summary>
    /// <param name="crew">The crew member.</param>
    public static void Attach(RaiderCrew crew)
    {
        if (crew != null && !VisualHelper.IsHeadless && crew.GetComponent<RaiderVoice>() == null)
        {
            crew.gameObject.AddComponent<RaiderVoice>().crew = crew;
        }
    }

    private void Start()
    {
        voice = WhiteHiltSound.AddVoice(gameObject);
        if (voice != null)
        {
            voice.maxDistance = 45f;
        }

        // The jarl speaks with his own voice; the others with one of two, by their network id.
        ZNetView view = GetComponent<ZNetView>();
        uint id = view != null && view.IsValid() ? view.GetZDO().m_uid.ID : 0u;
        speaker = crew != null && crew.Jarl ? "raiderchief" : id % 2 == 0 ? "raider1" : "raider2";
        nextCheck = Time.time + Random.Range(1f, CheckInterval * 2f);
    }

    private void Update()
    {
        if (Time.time < nextCheck || crew == null || !crew.Alive)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        bool boarded = crew.Boarded;
        if (boarded && !wasBoarded)
        {
            wasBoarded = true;
            Shout("board");
            return;
        }

        wasBoarded = boarded;
        BaseAI ai = crew.Character.GetBaseAI();
        if (ai != null && ai.IsAlerted() && Time.time >= nextOwn && Random.value < ShoutChance)
        {
            Shout(Random.value < 0.5f ? "attack" : "taunt");
        }
    }

    private void Shout(string kind)
    {
        if (Time.time - lastShout < AnyPause)
        {
            return;
        }

        lastShout = Time.time;
        nextOwn = Time.time + OwnPause;
        string token = $"$whitehilt_raider_{kind}{Random.Range(1, 4)}";
        if (Chat.instance != null)
        {
            Chat.instance.SetNpcText(gameObject, Vector3.up * BubbleHeight, 30f, BubbleTime, string.Empty, token, false);
        }

        WhiteHiltSound.Speak(voice, speaker, token);
    }
}
