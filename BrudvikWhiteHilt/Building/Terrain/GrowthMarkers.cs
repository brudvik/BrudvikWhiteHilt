using BrudvikWhiteHilt.Planting;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// Small labels over the plants near the player while the White Hilt cultivator is held: time left to grow in green,
/// or why a plant is not growing in red. Picked berry bushes, mushrooms and flowers show when they grow back in blue.
/// </summary>
public static class GrowthMarkers
{
    private const float Range = 15f;
    private const float RefreshInterval = 0.5f;
    private const int MaxMarkers = 60;
    private const float Lift = 0.8f;
    private const double ReadySoonSeconds = 30.0;

    private static readonly Color growing = new(0.5f, 1f, 0.5f);
    private static readonly Color stuck = new(1f, 0.45f, 0.35f);
    private static readonly Color regrowing = new(0.6f, 0.85f, 1f);
    private static readonly List<Component> targets = new();
    private static readonly List<Text> labels = new();

    private static GameObject root;
    private static float nextRefresh;
    private static int plantMask;

    /// <summary>
    /// Updates the labels. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        bool show = TerrainSettings.ShowGrowth.Value && TerrainSettings.HoldingCultivator(player) && !Media.MediaMode.HideUi
            && !InventoryGui.IsVisible() && !Minimap.IsOpen() && GameCamera.instance != null;
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

            Vector3 screen = camera.WorldToScreenPoint(target.transform.position + Vector3.up * Lift);
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
        targets.Clear();
        if (plantMask == 0)
        {
            plantMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "item");
        }

        bool pickables = PlantingSettings.ShowRegrowTimers.Value;
        HashSet<Component> seen = new();
        foreach (Collider collider in Physics.OverlapSphere(player.transform.position, Range, plantMask))
        {
            Plant plant = collider.GetComponentInParent<Plant>();
            if (plant != null)
            {
                if (plant.m_nview != null && plant.m_nview.IsValid() && seen.Add(plant))
                {
                    targets.Add(plant);
                }

                continue;
            }

            Pickable pickable = pickables ? collider.GetComponentInParent<Pickable>() : null;
            if (pickable != null && RegrowSeconds(pickable) >= 0.0 && seen.Add(pickable))
            {
                targets.Add(pickable);
            }
        }

        targets.Sort((a, b) => Vector3.Distance(a.transform.position, player.transform.position)
            .CompareTo(Vector3.Distance(b.transform.position, player.transform.position)));
        if (targets.Count > MaxMarkers)
        {
            targets.RemoveRange(MaxMarkers, targets.Count - MaxMarkers);
        }

        while (labels.Count < targets.Count)
        {
            labels.Add(CreateLabel());
        }

        for (int i = 0; i < targets.Count; i++)
        {
            Text label = labels[i];
            if (targets[i] is Pickable pickable)
            {
                label.text = TimeLeft(RegrowSeconds(pickable));
                label.color = regrowing;
                continue;
            }

            Plant plant = (Plant)targets[i];
            if (plant.GetStatus() == Plant.Status.Healthy)
            {
                label.text = TimeLeft(plant.GetGrowTime() - plant.TimeSincePlanted());
                label.color = growing;
            }
            else
            {
                string hover = plant.GetHoverText();
                int open = hover.LastIndexOf('(');
                label.text = open >= 0 ? hover.Substring(open).Trim('(', ')', ' ') : hover;
                label.color = stuck;
            }
        }
    }

    // Seconds until a picked pickable grows back, or -1 if it is not waiting to grow back.
    private static double RegrowSeconds(Pickable pickable)
    {
        if (pickable.m_nview == null || !pickable.m_nview.IsValid() || !pickable.m_picked || pickable.m_respawnTimeMinutes <= 0f
            || ZNet.instance == null)
        {
            return -1.0;
        }

        long pickedTime = pickable.m_nview.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L);
        if (pickedTime <= 1L)
        {
            return -1.0;
        }

        double elapsed = (ZNet.instance.GetTime() - new DateTime(pickedTime)).TotalSeconds;
        return Math.Max(0.0, pickable.m_respawnTimeMinutes * 60.0 - elapsed);
    }

    private static string TimeLeft(double seconds)
    {
        return seconds > ReadySoonSeconds ? Duration(seconds) : Localization.instance.Localize("$whitehilt_farm_ready");
    }

    private static string Duration(double seconds)
    {
        int minutes = Mathf.CeilToInt((float)(seconds / 60.0));
        return minutes >= 60 ? $"{minutes / 60} h {minutes % 60} min" : $"{minutes} min";
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
        root = new GameObject("WhiteHiltGrowthMarkers", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        return true;
    }

    private static Text CreateLabel()
    {
        Text text = GUIManager.Instance.CreateText(string.Empty, root.transform, Vector2.zero, Vector2.zero, Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, 14, Color.white, true, Color.black, 160f, 24f, false).GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }
}
