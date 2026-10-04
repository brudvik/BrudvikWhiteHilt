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
        private float nextSparkle;

        /// <summary>
        /// Lights the chest up, or keeps it lit for longer if it already is.
        /// </summary>
        /// <param name="chest">The chest.</param>
        /// <param name="color">The light's colour.</param>
        public static void Flash(Container chest, Color color)
        {
            if (!chest.TryGetComponent(out ChestHighlight highlight)) highlight = chest.gameObject.AddComponent<ChestHighlight>();
            highlight.Begin(chest, color);
        }

        private void Begin(Container chest, Color color)
        {
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
            until = Time.time + Seconds;
            nextSparkle = 0f;
        }

        private void Update()
        {
            if (glow == null || container == null || Time.time >= until)
            {
                if (glow != null) Destroy(glow.gameObject);
                Destroy(this);
                return;
            }

            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            var fade = Mathf.Clamp01((until - Time.time) / FadeSeconds);
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
