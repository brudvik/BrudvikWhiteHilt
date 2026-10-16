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
        if (AudioMan.instance != null)
        {
            foreach (AudioSource source in new[] { AudioMan.instance.m_ambientLoopSource, AudioMan.instance.m_windLoopSource, AudioMan.instance.m_oceanAmbientSource })
            {
                if (source != null)
                {
                    // The game moves these towards its own volume each frame, so this holds them down without piling up.
                    source.volume = Mathf.Min(source.volume, share);
                }
            }
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

        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (source == null || !source.loop || quietened.ContainsKey(source) || source.gameObject == gameObject
                || Vector3.Distance(source.transform.position, player.transform.position) > Reach
                || source.GetComponentInParent<Character>() != null || source.GetComponentInParent<DarknessKeep>() != null)
            {
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
    }
}
