using BrudvikWhiteHilt.Building.Media;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Production;

/// <summary>
/// Small labels over the working stations near the player: time left in green, ready or full in blue, why it stopped
/// in red. Fires only get a label when they run low or go out.
/// </summary>
public static class ProductionLabels
{
    private const float RefreshInterval = 0.5f;
    private const int MaxLabels = 40;
    private const float Gap = 0.3f;
    private const float MaxLift = 4f;

    private static readonly List<Component> nearby = new();
    private static readonly List<Component> targets = new();
    private static readonly List<Text> labels = new();
    private static readonly Dictionary<Component, float> lifts = new();
    private static readonly ProductionStatus status = new();

    private static GameObject root;
    private static float nextRefresh;

    /// <summary>
    /// Updates the labels. Called after the HUD update.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Update(Player player)
    {
        bool show = ProductionSettings.ShowLabels.Value && !MediaMode.HideUi && !InventoryGui.IsVisible() && !Minimap.IsOpen()
            && !Menu.IsVisible() && GameCamera.instance != null && !player.IsDead();
        if (!show)
        {
            if (root != null)
            {
                root.SetActive(false);
            }

            return;
        }

        if (!Ensure())
        {
            return;
        }

        root.SetActive(true);
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + RefreshInterval;
            Refresh(player);
        }

        Camera camera = GameCamera.instance.m_camera;
        for (int i = 0; i < labels.Count; i++)
        {
            Component target = i < targets.Count ? targets[i] : null;
            Text label = labels[i];
            if (target == null)
            {
                label.gameObject.SetActive(false);
                continue;
            }

            Vector3 screen = camera.WorldToScreenPoint(target.transform.position + Vector3.up * Lift(target));
            bool visible = screen.z > 0f;
            label.gameObject.SetActive(visible);
            if (visible)
            {
                label.transform.position = screen;
            }
        }
    }

    private static void Refresh(Player player)
    {
        Vector3 position = player.transform.position;
        ProductionRegistry.Near(position, ProductionSettings.LabelRange.Value, nearby);
        nearby.Sort((a, b) => (a.transform.position - position).sqrMagnitude.CompareTo((b.transform.position - position).sqrMagnitude));
        targets.Clear();
        List<string> texts = new();
        foreach (Component source in nearby)
        {
            if (targets.Count >= MaxLabels)
            {
                break;
            }

            if (!ProductionReader.Read(source, status, false) || !Wanted(status) || string.IsNullOrEmpty(status.Summary))
            {
                continue;
            }

            targets.Add(source);
            texts.Add(status.Summary);
        }

        while (labels.Count < targets.Count)
        {
            labels.Add(CreateLabel());
        }

        for (int i = 0; i < targets.Count; i++)
        {
            labels[i].text = texts[i];
        }

        if (lifts.Count > 200)
        {
            lifts.Clear();
        }
    }

    /// <summary>
    /// True if a station's state is worth a label or an overview row.
    /// </summary>
    /// <param name="status">The station.</param>
    /// <returns>True if shown.</returns>
    public static bool Wanted(ProductionStatus status)
    {
        if (status.State == ProductionState.Idle)
        {
            return false;
        }

        return status.Kind != ProductionKind.Fire || status.Attention || status.State == ProductionState.Stopped;
    }

    // Just over the top of the station's renderers, measured once.
    private static float Lift(Component target)
    {
        if (lifts.TryGetValue(target, out float lift))
        {
            return lift;
        }

        float top = 0f;
        float baseY = target.transform.position.y;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            {
                top = Mathf.Max(top, renderer.bounds.max.y - baseY);
            }
        }

        lift = Mathf.Clamp(top + Gap, Gap, MaxLift);
        lifts[target] = lift;
        return lift;
    }

    private static bool Ensure()
    {
        if (root != null)
        {
            return true;
        }

        if (GUIManager.Instance == null || GUIManager.Instance.AveriaSerifBold == null)
        {
            return false;
        }

        labels.Clear();
        root = new GameObject("WhiteHiltProductionLabels", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        return true;
    }

    private static Text CreateLabel()
    {
        Text text = GUIManager.Instance.CreateText(string.Empty, root.transform, Vector2.zero, Vector2.zero, Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, 14, Color.white, true, Color.black, 220f, 24f, false).GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = true;
        text.raycastTarget = false;
        return text;
    }
}
