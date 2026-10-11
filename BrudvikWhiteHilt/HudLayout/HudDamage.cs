using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// A soft red arc at the edge of the screen on the side a hit came from, turned with the camera so it keeps pointing
/// at the attacker while it fades: in front at the top, behind at the bottom. Works with either HUD.
/// </summary>
public static class HudDamage
{
    private const int Arcs = 4;
    private const float Seconds = 1.4f;
    private const float Width = 520f;
    private const float Height = 90f;
    private const float EdgeShare = 0.42f;
    private const int TextureWidth = 128;
    private const int TextureHeight = 32;

    private static readonly List<Arc> arcs = new();
    private static RectTransform root;
    private static Sprite sprite;
    private static int next;

    /// <summary>
    /// Shows where a hit on the local player came from. Called when the player takes damage.
    /// </summary>
    /// <param name="player">The player hit.</param>
    /// <param name="hit">The hit.</param>
    public static void Hit(Player player, HitData hit)
    {
        if (player != Player.m_localPlayer || !HudSettings.DamageDirection.Value || hit == null || Hud.instance == null
            || hit.GetTotalDamage() <= 0f)
        {
            return;
        }

        Character attacker = hit.GetAttacker();
        Vector3 from = attacker != null ? attacker.transform.position : hit.m_point - hit.m_dir;
        Vector3 away = from - player.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
        {
            return;
        }

        Ensure(Hud.instance);
        Arc arc = arcs[next];
        next = (next + 1) % Arcs;
        arc.Source = from;
        arc.Started = Time.time;
        arc.Strength = Mathf.Clamp01(0.5f + hit.GetTotalDamage() / Mathf.Max(1f, player.GetMaxHealth()) * 2f);
    }

    /// <summary>
    /// Turns the arcs with the camera and fades them. Called after the HUD's late update.
    /// </summary>
    public static void Update()
    {
        Player player = Player.m_localPlayer;
        Camera camera = Utils.GetMainCamera();
        if (root == null || player == null || camera == null)
        {
            return;
        }

        float radius = Mathf.Min(root.rect.width, root.rect.height) * EdgeShare;
        Vector3 forward = camera.transform.forward;
        forward.y = 0f;
        foreach (Arc arc in arcs)
        {
            float age = Time.time - arc.Started;
            bool shown = age < Seconds;
            if (arc.Pivot.gameObject.activeSelf != shown)
            {
                arc.Pivot.gameObject.SetActive(shown);
            }

            if (!shown)
            {
                continue;
            }

            Vector3 away = arc.Source - player.transform.position;
            away.y = 0f;
            float angle = Vector3.SignedAngle(forward, away, Vector3.up);
            arc.Pivot.localRotation = Quaternion.Euler(0f, 0f, -angle);
            arc.Image.rectTransform.anchoredPosition = new Vector2(0f, radius);
            Color color = new(0.85f, 0.08f, 0.05f, arc.Strength * (1f - age / Seconds));
            arc.Image.color = color;
        }
    }

    // Under the HUD root, behind everything else on it, so it goes away with the HUD.
    private static void Ensure(Hud hud)
    {
        if (root != null && root.parent == hud.m_rootObject.transform)
        {
            return;
        }

        arcs.Clear();
        root = (RectTransform)new GameObject("WhiteHiltDamageDirection", typeof(RectTransform)).transform;
        root.SetParent(hud.m_rootObject.transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;
        root.SetAsFirstSibling();
        for (int i = 0; i < Arcs; i++)
        {
            RectTransform pivot = (RectTransform)new GameObject("arc", typeof(RectTransform)).transform;
            pivot.SetParent(root, false);
            pivot.anchorMin = pivot.anchorMax = new Vector2(0.5f, 0.5f);
            pivot.sizeDelta = Vector2.zero;
            Image image = new GameObject("glow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.rectTransform.SetParent(pivot, false);
            image.rectTransform.sizeDelta = new Vector2(Width, Height);
            image.sprite = Glow();
            image.raycastTarget = false;
            pivot.gameObject.SetActive(false);
            arcs.Add(new Arc { Pivot = pivot, Image = image, Started = float.MinValue });
        }
    }

    // A soft band that curves like the edge of a circle, brightest at its middle and outer edge.
    private static Sprite Glow()
    {
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = new(TextureWidth, TextureHeight, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < TextureHeight; y++)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                float across = (x + 0.5f) / TextureWidth * 2f - 1f;
                float up = (y + 0.5f) / TextureHeight;
                float bend = across * across * 0.5f;
                float band = Mathf.Clamp01(1f - Mathf.Abs(up - 0.6f + bend) * 2.2f);
                float sides = Mathf.Clamp01(1f - Mathf.Abs(across));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, band * band * sides));
            }
        }

        texture.Apply();
        sprite = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, TextureWidth, TextureHeight), new Vector2(0.5f, 0.5f));
        return sprite;
    }

    private sealed class Arc
    {
        internal RectTransform Pivot;
        internal Image Image;
        internal Vector3 Source;
        internal float Started;
        internal float Strength;
    }
}
