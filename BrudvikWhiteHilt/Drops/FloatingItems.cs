using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Drops;

/// <summary>
/// Makes dropped items float in water, e.g. a Serpent's trophy and meat, instead of sinking to the sea floor. Items that
/// float already keep their own settings; the others borrow the float settings of Wood. Items already lying on the sea
/// floor come up when their area loads.
/// </summary>
public static class FloatingItems
{
    private const string Section = "FloatingItems";
    private const string TemplateItem = "Wood";

    private static Floating template;
    private static string sinkingText;
    private static HashSet<string> sinking = new();

    /// <summary>Whether dropped items float.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Prefab names of items that still sink, comma separated.</summary>
    public static ConfigEntry<string> SinkingItems { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Enabled = WhiteHiltConfig.BindAdminOnly(Section, "Enabled", true,
            "Dropped items float in water instead of sinking, e.g. what a Serpent drops at sea. Applies to items as they appear in the world.");
        SinkingItems = WhiteHiltConfig.BindAdminOnly(Section, "SinkingItems", string.Empty,
            "Prefab names of items that still sink, comma separated, e.g. Stone,Flint.");
    }

    /// <summary>
    /// Gives a dropped item a float, unless it has one or is set to sink.
    /// </summary>
    /// <param name="item">The item that just woke.</param>
    public static void MakeFloat(ItemDrop item)
    {
        if (!Enabled.Value || item.GetComponent<Floating>() != null || item.GetComponent<Rigidbody>() == null
            || item.GetComponent<ZNetView>() == null || item.GetComponentInChildren<Collider>() == null || IsSinking(item))
        {
            return;
        }

        Floating floating = item.gameObject.AddComponent<Floating>();
        Floating source = GetTemplate();
        if (source != null)
        {
            floating.m_waterLevelOffset = source.m_waterLevelOffset;
            floating.m_forceDistance = source.m_forceDistance;
            floating.m_force = source.m_force;
            floating.m_balanceForceFraction = source.m_balanceForceFraction;
            floating.m_damping = source.m_damping;
            floating.m_impactEffects = source.m_impactEffects;
        }
    }

    private static bool IsSinking(ItemDrop item)
    {
        string text = SinkingItems.Value ?? string.Empty;
        if (text != sinkingText)
        {
            sinkingText = text;
            sinking = new HashSet<string>(text.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }

        return sinking.Count > 0 && sinking.Contains(Utils.GetPrefabName(item.gameObject));
    }

    private static Floating GetTemplate()
    {
        if (template == null && ObjectDB.instance != null)
        {
            template = ObjectDB.instance.GetItemPrefab(TemplateItem)?.GetComponent<Floating>();
        }

        return template;
    }
}
