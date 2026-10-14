using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The dome over a trading post: a copy of the force field over Haldor's camp, the shimmering sphere with the
/// no-monsters area inside it, so no monster comes in while players trade. The copy is taken once from the vanilla
/// location and scaled to the post.
/// </summary>
public static class TradingPostDome
{
    private const string HaldorLocation = "Vendor_BlackForest";
    private const string ForceFieldName = "ForceField";

    private static GameObject template;
    private static float templateRadius;
    private static bool failed;

    /// <summary>
    /// Puts a dome on a post.
    /// </summary>
    /// <param name="post">The post's marker; the dome becomes its child.</param>
    /// <param name="centre">Centre in post coordinates.</param>
    /// <param name="radius">Radius in metres.</param>
    /// <returns>The dome, or null when Haldor's could not be copied.</returns>
    public static GameObject Attach(Transform post, Vector3 centre, float radius)
    {
        if (!EnsureTemplate())
        {
            return null;
        }

        GameObject dome = Object.Instantiate(template, post, false);
        dome.name = "whitehilt_tradingpost_dome";
        dome.transform.localPosition = centre;
        dome.transform.localRotation = Quaternion.identity;
        dome.transform.localScale = template.transform.localScale * (radius / templateRadius);
        dome.SetActive(true);
        if (VisualHelper.IsHeadless)
        {
            // A dedicated server draws nothing, but monsters it simulates must still keep out.
            foreach (Renderer renderer in dome.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
        }

        return dome;
    }

    // Copies the force field out of Haldor's location the first time a post needs it, into an inactive holder that
    // lives as long as the game, and lets the location's assets go again. If the location or its field cannot be
    // found, posts have no dome and the log says so once.
    private static bool EnsureTemplate()
    {
        if (template != null)
        {
            return true;
        }

        if (failed || ZoneSystem.instance == null)
        {
            return false;
        }

        ZoneSystem.ZoneLocation location = ZoneManager.Instance.GetZoneLocation(HaldorLocation);
        if (location == null || !location.m_prefab.IsValid)
        {
            return Fail($"the location {HaldorLocation} was not found");
        }

        location.m_prefab.Load();
        try
        {
            GameObject asset = location.m_prefab.Asset;
            Transform field = asset != null ? asset.transform.Find(ForceFieldName) : null;
            MeshFilter mesh = field != null ? field.GetComponent<MeshFilter>() : null;
            if (mesh == null || mesh.sharedMesh == null)
            {
                return Fail($"{HaldorLocation} has no {ForceFieldName}");
            }

            GameObject holder = new("whitehilt_tradingpost_dome_template");
            holder.SetActive(false);
            Object.DontDestroyOnLoad(holder);
            template = Object.Instantiate(field.gameObject, holder.transform, false);
            template.name = ForceFieldName;
            templateRadius = mesh.sharedMesh.bounds.extents.x * field.localScale.x;
            Jotunn.Logger.LogInfo($"Trading posts: copied Haldor's dome ({templateRadius:0.#} m).");
            return templateRadius > 0f;
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
        finally
        {
            location.m_prefab.Release();
        }
    }

    private static bool Fail(string reason)
    {
        failed = true;
        Jotunn.Logger.LogWarning($"Trading posts: no dome, {reason}.");
        return false;
    }
}
