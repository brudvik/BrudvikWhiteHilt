#nullable enable annotations

using BrudvikWhiteHilt.Chests.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Piece
{
    /// <summary>
    /// Makes a chest pulse with a bright light and sparkle for a while, so the player can see where an item belongs.
    /// Only the local player sees it.
    /// </summary>
    public class ChestHighlight : MonoBehaviour
    {
        /// <summary>Metres around the player within which chests light up.</summary>
        public const float Range = 100f;

        private const float Seconds = 20f;
        private const float FadeSeconds = 2f;
        private const float SparkleSeconds = 3f;

        private Container? container;
        private Light? glow;
        private float until;
        private float fadeSeconds = FadeSeconds;
        private float nextSparkle;

        /// <summary>
        /// Lights the chest up, or keeps it lit for longer if it already is.
        /// </summary>
        /// <param name="chest">The chest.</param>
        /// <param name="color">The light's colour.</param>
        /// <param name="seconds">How long it stays lit; a short time, renewed, keeps it lit while something lasts.</param>
        public static void Flash(Container chest, Color color, float seconds = Seconds)
        {
            if (!chest.TryGetComponent(out ChestHighlight highlight)) highlight = chest.gameObject.AddComponent<ChestHighlight>();
            highlight.Begin(chest, color, seconds);
        }

        // Lights the chest in a colour for some seconds, or keeps an already lit one lit longer.
        private void Begin(Container chest, Color color, float seconds)
        {
            bool lit = glow != null && Time.time < until;
            container = chest;
            if (glow == null)
            {
                var lightObject = new GameObject("WhiteHiltChestHighlight");
                lightObject.transform.SetParent(transform, false);
                lightObject.transform.localPosition = Vector3.up;
                glow = lightObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.shadows = LightShadows.None;
            }

            glow.color = color;
            float fade = Mathf.Min(FadeSeconds, seconds / 2f);
            fadeSeconds = lit ? Mathf.Max(fadeSeconds, fade) : fade;
            until = lit ? Mathf.Max(until, Time.time + seconds) : Time.time + seconds;
            // A light kept lit by renewing it sparkles as one that lasts, not at every renewal.
            if (!lit) nextSparkle = 0f;
        }

        // Pulses the light and sparkles now and then, fades it out at the end, and removes itself when done.
        private void Update()
        {
            if (glow == null || container == null || Time.time >= until)
            {
                if (glow != null) Destroy(glow.gameObject);
                Destroy(this);
                return;
            }

            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            var fade = Mathf.Clamp01((until - Time.time) / fadeSeconds);
            glow.intensity = (1.5f + 2f * pulse) * fade;
            glow.range = 4f + 2f * pulse;
            if (Time.time < nextSparkle) return;

            nextSparkle = Time.time + SparkleSeconds;
            ChestEffects.Play(container, ChestEffects.Refill);
        }

        private void OnDestroy()
        {
            if (glow != null) Destroy(glow.gameObject);
        }
    }
}
