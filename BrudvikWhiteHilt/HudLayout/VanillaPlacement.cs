using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// Moves and hides the game's own HUD elements for the White Hilt console, remembering where each one was so the game's
/// layout comes back when the player switches to the vanilla HUD. Elements are hidden by scale, not deactivated, so the
/// game and other mods can go on updating them.
/// </summary>
internal static class VanillaPlacement
{
    private static readonly Dictionary<RectTransform, Original> originals = new();
    private static readonly List<RectTransform> gone = new();
    private static readonly Vector3[] corners = new Vector3[4];

    /// <summary>Whether any element is moved or hidden.</summary>
    internal static bool Any => originals.Count > 0;

    /// <summary>
    /// Hides an element. When it holds an element that is kept, its other children are hidden instead.
    /// </summary>
    /// <param name="rect">The element.</param>
    /// <param name="keep">Elements that must stay visible.</param>
    internal static void Hide(RectTransform rect, IList<RectTransform> keep)
    {
        if (rect == null)
        {
            return;
        }

        bool holdsKept = false;
        foreach (RectTransform kept in keep)
        {
            if (kept != null && kept != rect && kept.IsChildOf(rect))
            {
                holdsKept = true;
                break;
            }
        }

        if (!holdsKept)
        {
            Remember(rect);
            rect.localScale = Vector3.zero;
            return;
        }

        foreach (Transform child in rect)
        {
            if (child is RectTransform childRect && !keep.Contains(childRect))
            {
                Hide(childRect, keep);
            }
        }
    }

    /// <summary>
    /// Moves an element by a distance measured in another element's space.
    /// </summary>
    /// <param name="rect">The element to move.</param>
    /// <param name="space">The space the distance is measured in.</param>
    /// <param name="delta">The distance.</param>
    internal static void Move(RectTransform rect, RectTransform space, Vector2 delta)
    {
        if (rect == null || rect.parent == null || delta.sqrMagnitude < 0.01f)
        {
            return;
        }

        Remember(rect);
        Vector3 local = rect.parent.InverseTransformVector(space.TransformVector(new Vector3(delta.x, delta.y, 0f)));
        rect.anchoredPosition += new Vector2(local.x, local.y);
    }

    /// <summary>
    /// Scales an element to a share of the size it had before the console touched it.
    /// </summary>
    /// <param name="rect">The element.</param>
    /// <param name="share">Its size as a share of its own; 1 for as it was.</param>
    internal static void Scale(RectTransform rect, float share)
    {
        if (rect == null)
        {
            return;
        }

        Remember(rect);
        rect.localScale = originals[rect].Scale * share;
    }

    /// <summary>
    /// An element's size now as a share of the size it had before the console touched it.
    /// </summary>
    /// <param name="rect">The element.</param>
    /// <returns>The share; 1 for an element the console has not scaled.</returns>
    internal static float ScaleOf(RectTransform rect)
    {
        if (rect == null || !originals.TryGetValue(rect, out Original original) || Mathf.Approximately(original.Scale.x, 0f))
        {
            return 1f;
        }

        return rect.localScale.x / original.Scale.x;
    }

    /// <summary>
    /// The box around elements in a space, measured from the space's lower left corner.
    /// </summary>
    /// <param name="space">The space.</param>
    /// <param name="rects">The elements; missing and inactive ones are left out.</param>
    /// <param name="bounds">The box.</param>
    /// <returns>False when no element counted.</returns>
    internal static bool Bounds(RectTransform space, IEnumerable<RectTransform> rects, out Rect bounds)
    {
        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        bool any = false;
        Vector2 origin = space.rect.min;
        foreach (RectTransform rect in rects)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy)
            {
                continue;
            }

            rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 point = (Vector2)space.InverseTransformPoint(corner) - origin;
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            any = true;
        }

        bounds = any ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : default;
        return any;
    }

    /// <summary>
    /// Puts every moved and hidden element back where the game had it.
    /// </summary>
    internal static void RestoreAll()
    {
        foreach (var entry in originals)
        {
            if (entry.Key != null)
            {
                entry.Key.anchoredPosition = entry.Value.Position;
                entry.Key.localScale = entry.Value.Scale;
            }
        }

        originals.Clear();
    }

    /// <summary>
    /// Forgets elements that were destroyed with their HUD.
    /// </summary>
    internal static void Prune()
    {
        gone.Clear();
        foreach (var entry in originals)
        {
            if (entry.Key == null)
            {
                gone.Add(entry.Key);
            }
        }

        foreach (RectTransform rect in gone)
        {
            originals.Remove(rect);
        }
    }

    private static void Remember(RectTransform rect)
    {
        if (!originals.ContainsKey(rect))
        {
            originals.Add(rect, new Original(rect.anchoredPosition, rect.localScale));
        }
    }

    private readonly struct Original
    {
        internal readonly Vector2 Position;
        internal readonly Vector3 Scale;

        internal Original(Vector2 position, Vector3 scale)
        {
            Position = position;
            Scale = scale;
        }
    }
}
