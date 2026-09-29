using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.EternalFire;

/// <summary>
/// Eternal fire: fires, torches, ovens and optionally smelters burn without fuel and stay lit in rain. In linear
/// progression it takes a Surt's Brazier standing somewhere in the world; the server looks for one and sets a global key.
/// </summary>
public static class EternalFireRules
{
    /// <summary>
    /// When eternal fire is on.
    /// </summary>
    public enum FireMode
    {
        /// <summary>Never.</summary>
        Off,

        /// <summary>In linear progression while a Surt's Brazier stands in the world; in full progression always.</summary>
        Progression,

        /// <summary>Always, with or without a brazier.</summary>
        Always
    }

    /// <summary>
    /// Global key the server sets while a Surt's Brazier stands in the world.
    /// </summary>
    public const string GlobalKey = "whitehilt_eternalfire";

    private const string Section = "EternalFire";

    private static readonly Dictionary<string, bool> vanillaInfinite = new();

    private static ConfigEntry<FireMode> mode;
    private static ConfigEntry<bool> fires;
    private static ConfigEntry<bool> ovens;
    private static ConfigEntry<bool> smelters;
    private static ConfigEntry<string> excluded;
    private static HashSet<string> excludedSet = new();

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("whitehilt_eternalfire_burning", "Eternal fire");

        mode = WhiteHiltConfig.BindAdminOnly(Section, "Mode", FireMode.Progression,
            "Off: fires need fuel as usual.\nProgression: in linear progression mode fires are eternal while a Surt's Brazier stands anywhere " +
            "in the world; in full mode they always are.\nAlways: fires are always eternal.");
        fires = WhiteHiltConfig.BindAdminOnly(Section, "Fires", true, "Campfires, hearths, bonfires, torches, sconces, braziers, jack-o-turnips and hot tubs.");
        ovens = WhiteHiltConfig.BindAdminOnly(Section, "Ovens", true, "Cooking stations with their own fire, like the stone oven.");
        smelters = WhiteHiltConfig.BindAdminOnly(Section, "Smelters", false,
            "Smelters, blast furnaces and other stations that burn fuel. Off by default: coal is part of smelting, and is taken from nearby chests.");
        excluded = WhiteHiltConfig.BindAdminOnly(Section, "ExcludedPrefabs", string.Empty, "Comma-separated prefab names that are never eternal.");

        excluded.SettingChanged += (_, _) => excludedSet = ParseList(excluded.Value);
        excludedSet = ParseList(excluded.Value);
    }

    /// <summary>
    /// True while eternal fire is on in this world.
    /// </summary>
    public static bool IsActive
    {
        get
        {
            if (mode == null)
            {
                return false;
            }

            return mode.Value switch
            {
                FireMode.Always => true,
                FireMode.Progression => WhiteHiltConfig.Mode.Value != ProgressionMode.Linear
                    || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKey)),
                _ => false
            };
        }
    }

    /// <summary>
    /// True if a fire burns forever now.
    /// </summary>
    /// <param name="fireplace">The fire.</param>
    /// <returns>True if eternal.</returns>
    public static bool Applies(Fireplace fireplace)
    {
        return fires.Value && Applies(fireplace.m_nview);
    }

    /// <summary>
    /// True if a cooking station's own fire burns forever now.
    /// </summary>
    /// <param name="station">The cooking station.</param>
    /// <returns>True if eternal.</returns>
    public static bool Applies(CookingStation station)
    {
        return station.m_useFuel && ovens.Value && Applies(station.m_nview);
    }

    /// <summary>
    /// True if a smelter's fuel lasts forever now.
    /// </summary>
    /// <param name="smelter">The smelter.</param>
    /// <returns>True if eternal.</returns>
    public static bool Applies(Smelter smelter)
    {
        return smelter.m_fuelItem != null && smelters.Value && Applies(smelter.m_nview);
    }

    /// <summary>
    /// True if the fire's prefab already burns forever in vanilla, like the fires in dungeons.
    /// </summary>
    /// <param name="fireplace">The fire.</param>
    /// <returns>True for a vanilla eternal fire.</returns>
    public static bool IsVanillaInfinite(Fireplace fireplace)
    {
        string prefab = Utils.GetPrefabName(fireplace.m_nview != null ? fireplace.m_nview.gameObject : fireplace.gameObject);
        if (!vanillaInfinite.TryGetValue(prefab, out bool infinite))
        {
            GameObject original = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            Fireplace originalFire = original != null ? original.GetComponentInChildren<Fireplace>(true) : null;
            infinite = originalFire != null && originalFire.m_infiniteFuel;
            vanillaInfinite[prefab] = infinite;
        }

        return infinite;
    }

    /// <summary>
    /// Sets or clears the global key. Server only.
    /// </summary>
    /// <param name="braziers">True if a Surt's Brazier stands in the world.</param>
    public static void SetBrazierPresent(bool braziers)
    {
        if (ZoneSystem.instance == null || ZoneSystem.instance.GetGlobalKey(GlobalKey) == braziers)
        {
            return;
        }

        if (braziers)
        {
            ZoneSystem.instance.SetGlobalKey(GlobalKey);
        }
        else
        {
            ZoneSystem.instance.RemoveGlobalKey(GlobalKey);
        }
    }

    private static bool Applies(ZNetView nview)
    {
        return IsActive && nview != null && nview.IsValid() && !excludedSet.Contains(Utils.GetPrefabName(nview.gameObject));
    }

    private static HashSet<string> ParseList(string value)
    {
        return new HashSet<string>(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).Where(part => part.Length > 0));
    }
}
