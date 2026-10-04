using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Dowsing;

/// <summary>Local proximity signals for loaded, unpicked root-bearing plants.</summary>
public class RootDowsingEffect : StatusEffect
{
    private static readonly HashSet<Pickable> plants = new();
    private float scanTimer;
    private float pingTimer;
    private Pickable target;
    private GameObject glow;
    private Light glowLight;

    /// <summary>Green Wishbone-style proximity pulse.</summary>
    public EffectList Ping { get; set; } = new();

    /// <summary>Registers loaded plants, including those enabled by a later config change.</summary>
    /// <param name="plant">The loaded pickable.</param>
    public static void Register(Pickable plant)
    {
        if (plant.m_itemPrefab != null)
        {
            plants.RemoveWhere(known => known == null);
            plants.Add(plant);
        }
    }

    /// <summary>Finds the nearest eligible loaded plant.</summary>
    /// <param name="point">Player position.</param>
    /// <param name="range">Maximum distance.</param>
    /// <returns>The nearest unpicked tracked plant, or null.</returns>
    public static Pickable FindClosest(Vector3 point, float range)
    {
        plants.RemoveWhere(known => known == null);
        Pickable closest = null;
        float best = range;
        foreach (Pickable plant in plants)
        {
            if (!IsAvailable(plant))
            {
                continue;
            }

            float distance = Vector3.Distance(point, plant.transform.position);
            if (distance <= best)
            {
                best = distance;
                closest = plant;
            }
        }

        return closest;
    }

    /// <summary>Updates proximity pulses and the nearest plant's local green light.</summary>
    /// <param name="dt">Elapsed seconds.</param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        if (m_character == null || m_character != Player.m_localPlayer || VisualHelper.IsHeadless)
        {
            ClearGlow();
            return;
        }

        Transform body = m_character.transform;
        float range = RootDowsingSettings.PingRange.Value;
        scanTimer -= dt;
        if (scanTimer <= 0f || (target != null && !IsAvailable(target)))
        {
            scanTimer = RootDowsingSettings.ScanSeconds.Value;
            Pickable found = FindClosest(body.position, range);
            if (found != target)
            {
                ClearGlow();
                target = found;
                pingTimer = 0f;
            }
        }

        if (!IsAvailable(target))
        {
            ClearGlow();
            return;
        }

        float distance = Vector3.Distance(body.position, target.transform.position);
        if (distance > range)
        {
            ClearGlow();
            return;
        }

        UpdateGlow(distance);
        float close = RootDowsingSettings.CloseInterval.Value;
        float distant = Mathf.Max(close, RootDowsingSettings.DistantInterval.Value);
        pingTimer += dt;
        if (pingTimer >= Mathf.Lerp(close, distant, Mathf.Clamp01(distance / range)))
        {
            pingTimer = 0f;
            Ping.Create(body.position, body.rotation, body, 1f, -1, m_character.GetZDOID());
        }
    }

    /// <summary>Removes local plant lighting when the accessory is taken off.</summary>
    public override void Stop()
    {
        ClearGlow();
        target = null;
        base.Stop();
    }

    private static bool IsAvailable(Pickable plant)
    {
        return plant != null && plant.m_nview != null && plant.m_nview.IsValid()
            && plant.m_itemPrefab != null && RootDowsingSettings.IsTrackedItem(plant.m_itemPrefab.name) && plant.CanBePicked();
    }

    private void UpdateGlow(float distance)
    {
        if (RootDowsingSettings.GlowDistance.Value <= 0f || distance > RootDowsingSettings.GlowDistance.Value)
        {
            ClearGlow();
            return;
        }

        if (glow == null)
        {
            glow = new GameObject("WhiteHiltRootGlow");
            glow.transform.SetParent(target.transform, false);
            glowLight = glow.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = Color.green;
            glowLight.shadows = LightShadows.None;
        }

        glowLight.range = RootDowsingSettings.GlowRange.Value;
        glowLight.intensity = RootDowsingSettings.GlowIntensity.Value;
    }

    private void ClearGlow()
    {
        if (glow != null)
        {
            glow.SetActive(false);
            Object.Destroy(glow);
        }

        glow = null;
        glowLight = null;
    }
}