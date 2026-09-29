using UnityEngine;

namespace BrudvikWhiteHilt.Sound;

/// <summary>
/// Turns down one weather sound source by the indoor gain, on the audio thread. Working on the samples leaves the game's
/// own volume fading alone.
/// </summary>
public class IndoorAudioFilter : MonoBehaviour
{
    /// <summary>
    /// True for the wind, which uses the wind gain; everything else uses the rain gain.
    /// </summary>
    public bool Wind { get; set; }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float gain = Wind ? IndoorSound.WindGain : IndoorSound.RainGain;
        if (gain >= 0.999f)
        {
            return;
        }

        for (int i = 0; i < data.Length; i++)
        {
            data[i] *= gain;
        }
    }
}
