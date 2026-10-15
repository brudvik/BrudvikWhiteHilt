using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using BrudvikWhiteHilt.Navigation.Discoveries;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// Munin on his perch talks to the local player who comes up to him: he caws and tells of the new places found since
/// the player last listened, or now and then just croaks a word. Aloud and in a speech bubble; runs on every machine
/// for its own player, and the count of discoveries told is kept in the player's own data.
/// </summary>
public class MuninVoice : MonoBehaviour
{
    /// <summary>Whose recorded voice Munin speaks with.</summary>
    public const string Speaker = "munin";

    private const string ToldKey = "whitehilt_munin_told";
    private const float CheckInterval = 1.5f;
    private const float NearRange = 6f;
    private const float IdlePause = 240f;
    private const float IdleChance = 0.25f;
    private const float BubbleHeight = 2.8f;
    private const float BubbleTime = 5f;

    private AudioSource voice;
    private float nextCheck;
    private float nextIdle;
    private bool wasNear;

    /// <summary>
    /// Registers Munin's lines. Call from the plugin's Awake, before the game loads its languages.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_munin_news1", "Kraa! New places, new places! Look at the map!");
        Translations.AddEnglish("whitehilt_munin_news2", "I flew far and saw much. It is all on the table now.");
        Translations.AddEnglish("whitehilt_munin_news3", "Kraa! Caves and camps and treasures. Remember them, as I do!");
        Translations.AddEnglish("whitehilt_munin_idle1", "Kraa. I remember everything.");
        Translations.AddEnglish("whitehilt_munin_idle2", "Hugin thinks, Munin remembers. Kraa!");
        Translations.AddEnglish("whitehilt_munin_idle3", "Nothing new under the sky today.");
    }

    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        ZNetView view = GetComponent<ZNetView>();
        Player player = Player.m_localPlayer;
        if (view == null || !view.IsValid() || player == null)
        {
            return;
        }

        bool near = Vector3.Distance(player.transform.position, transform.position) <= NearRange;
        bool arrived = near && !wasNear;
        wasNear = near;
        if (!arrived)
        {
            return;
        }

        int count = DiscoveryOverlay.Count;
        int told = player.m_customData.TryGetValue(ToldKey, out string text) && int.TryParse(text, out int number) ? number : 0;
        if (count > told)
        {
            player.m_customData[ToldKey] = count.ToString();
            Say("news");
        }
        else if (Time.time >= nextIdle && Random.value < IdleChance)
        {
            nextIdle = Time.time + IdlePause;
            Say("idle");
        }
    }

    private void Say(string kind)
    {
        voice ??= WhiteHiltSound.AddVoice(gameObject);
        WhiteHiltSound.PlayFrom(voice, CreatureSoundNames.Pick("sfx_raven_caw"));
        string token = $"$whitehilt_munin_{kind}{Random.Range(1, 4)}";
        if (Chat.instance != null)
        {
            Chat.instance.SetNpcText(gameObject, Vector3.up * BubbleHeight, 20f, BubbleTime, string.Empty, token, false);
        }

        Invoke(nameof(SpeakLater), 1.1f);
        pending = token;
    }

    private string pending;

    // The words come after the caw.
    private void SpeakLater()
    {
        WhiteHiltSound.Speak(voice, Speaker, pending);
    }
}
