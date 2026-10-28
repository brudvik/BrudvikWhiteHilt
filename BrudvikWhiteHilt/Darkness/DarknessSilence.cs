using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// The silence before the dark, on the local player's machine: while the lights flicker the place's sounds fall away
/// (the game's ambience, wind and music, and the place's own dripping and humming), it stays silent in the black, and
/// the sounds come back with the light. The darkness's own sounds and the creatures are not quietened. On the
/// <see cref="Game"/> object.
/// </summary>
public class DarknessSilence : MonoBehaviour
{
    private const float Reach = 40f;
    private const float RefreshSeconds = 1f;
    private const float Left = 0.05f;

    private readonly Dictionary<AudioSource, float> quietened = new();
    private readonly HashSet<AudioSource> spared = new();
    private float nextRefresh;

    // After the game has set its own volumes this frame.
    private void LateUpdate()
    {
        float silence = DarknessClient.Silence;
        if (silence <= 0.001f)
        {
            Restore();
            return;
        }

        float share = Mathf.Lerp(1f, Left, silence);
        AudioMan audio = AudioMan.instance;
        if (audio != null)
        {
            HoldDown(audio.m_ambientLoopSource, share);
            HoldDown(audio.m_windLoopSource, share);
            HoldDown(audio.m_oceanAmbientSource, share);
        }

        if (MusicMan.instance != null && MusicMan.instance.m_musicSource != null)
        {
            MusicMan.instance.m_musicSource.volume *= share;
        }

        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + RefreshSeconds;
            Gather();
        }

        foreach (KeyValuePair<AudioSource, float> pair in quietened)
        {
            if (pair.Key != null)
            {
                pair.Key.volume = pair.Value * share;
            }
        }
    }

    // The game moves these towards its own volume each frame, so this holds them down without piling up.
    private static void HoldDown(AudioSource source, float share)
    {
        if (source != null)
        {
            source.volume = Mathf.Min(source.volume, share);
        }
    }

    private void OnDestroy()
    {
        Restore();
    }

    // The place's own looping sounds near the player; not a creature's, a player's or the darkness's.
    private void Gather()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        // A creature's or the darkness's own sound is remembered as spared, so its parents are not searched again each
        // second; the list is cleared when the sounds come back.
        Vector3 centre = player.transform.position;
        spared.RemoveWhere(source => source == null);
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (source == null || !source.loop || quietened.ContainsKey(source) || spared.Contains(source)
                || source.gameObject == gameObject || (source.transform.position - centre).sqrMagnitude > Reach * Reach)
            {
                continue;
            }

            if (source.GetComponentInParent<Character>() != null || source.GetComponentInParent<DarknessKeep>() != null)
            {
                spared.Add(source);
                continue;
            }

            quietened[source] = source.volume;
        }
    }

    private void Restore()
    {
        foreach (KeyValuePair<AudioSource, float> pair in quietened)
        {
            if (pair.Key != null)
            {
                pair.Key.volume = pair.Value;
            }
        }

        quietened.Clear();
        spared.Clear();
    }
}
