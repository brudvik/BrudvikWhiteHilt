using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ranching.FeedingTrough;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Ranching;

/// <summary>
/// Grooming, tethering and production of tame animals. State lives in the animal's ZDO, so every machine agrees;
/// only the machine that owns the animal changes it, through the RPCs registered here.
/// </summary>
public static class AnimalCare
{
    /// <summary>
    /// How far away, in metres, a tether post reaches.
    /// </summary>
    public static float TetherRange => RanchingSettings.TetherRange.Value;

    /// <summary>
    /// Experience for grooming an animal.
    /// </summary>
    public static float GroomExperience => RanchingSettings.GroomExperience.Value;

    /// <summary>
    /// Experience when an animal puts something in a trough.
    /// </summary>
    public static float ProduceExperience => RanchingSettings.ProduceExperience.Value;

    private const string GroomRpc = "WhiteHilt_Groom";
    private const string TetherRpc = "WhiteHilt_Tether";
    private const string GroomedKey = "whitehilt_groomed";
    private const string ProducedKey = "whitehilt_produced";
    private const string TetheredKey = "whitehilt_tethered";

    // Used before EnvMan exists; Valheim's day is 30 minutes.
    private const double DefaultDaySeconds = 1800d;

    // What a groomed, fed tame animal puts in a nearby trough, and how many days apart; parsed from the config, cached by its text.
    private static Dictionary<string, (string Item, float Days)> products = new();
    private static string productsText;

    private static double DaySeconds => EnvMan.instance != null && EnvMan.instance.m_dayLengthSec > 0 ? EnvMan.instance.m_dayLengthSec : DefaultDaySeconds;

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_animal_content", "Groomed and content");
        Translations.AddEnglish("whitehilt_animal_wants_grooming", "Wants grooming");
        Translations.AddEnglish("whitehilt_animal_tethered", "Tethered");
    }

    /// <summary>
    /// Registers the grooming and tethering RPCs on an animal.
    /// </summary>
    /// <param name="tameable">The animal.</param>
    public static void RegisterRpcs(Tameable tameable)
    {
        ZNetView nview = tameable.m_nview;
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        nview.Register(GroomRpc, _ => OnGroom(tameable));
        nview.Register<Vector3, bool>(TetherRpc, (_, point, tether) => OnTether(tameable, point, tether));
    }

    /// <summary>
    /// Whether the animal was groomed less than a day ago.
    /// </summary>
    /// <param name="tameable">The animal.</param>
    /// <returns>True while it is content.</returns>
    public static bool IsContent(Tameable tameable)
    {
        return SecondsSince(GetZdo(tameable), GroomedKey) < DaySeconds;
    }

    /// <summary>
    /// Whether the animal is tethered to a post.
    /// </summary>
    /// <param name="tameable">The animal.</param>
    /// <returns>True when tethered.</returns>
    public static bool IsTethered(Tameable tameable)
    {
        ZDO zdo = GetZdo(tameable);
        return zdo != null && zdo.GetBool(TetheredKey) && zdo.GetBool(ZDOVars.s_patrol);
    }

    /// <summary>
    /// Asks the animal's owner to mark it groomed.
    /// </summary>
    /// <param name="tameable">The animal.</param>
    public static void Groom(Tameable tameable)
    {
        tameable.m_nview.InvokeRPC(GroomRpc);
    }

    /// <summary>
    /// Asks the animal's owner to tether it to <paramref name="point"/>, or to set it free.
    /// </summary>
    /// <param name="tameable">The animal.</param>
    /// <param name="point">The post.</param>
    /// <param name="tether">True to tether, false to set free.</param>
    public static void Tether(Tameable tameable, Vector3 point, bool tether)
    {
        tameable.m_nview.InvokeRPC(TetherRpc, point, tether);
    }

    /// <summary>
    /// Puts the animal's product in a nearby trough once it is due. Call regularly on the animal's owner.
    /// </summary>
    /// <param name="tameable">The animal.</param>
    public static void UpdateProduction(Tameable tameable)
    {
        ZNetView nview = tameable.m_nview;
        if (!RanchingSettings.AnimalProduction.Value || nview == null || !nview.IsValid() || !nview.IsOwner() || !tameable.IsTamed()
            || !GetProducts().TryGetValue(Utils.GetPrefabName(tameable.gameObject), out (string Item, float Days) product))
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (!IsContent(tameable) || tameable.IsHungry())
        {
            return;
        }

        // The first day starts when the animal is first both fed and groomed.
        if (zdo.GetLong(ProducedKey, 0L) == 0L)
        {
            zdo.Set(ProducedKey, ZNet.instance.GetTime().Ticks);
            return;
        }

        GameObject prefab = PrefabManager.Instance.GetPrefab(product.Item);
        if (SecondsSince(zdo, ProducedKey) < product.Days * DaySeconds || prefab == null)
        {
            return;
        }

        FeedingTroughComponent trough = FeedingTroughComponent.FindNearestWithRoom(tameable.transform.position, prefab);
        if (trough == null)
        {
            return;
        }

        trough.AddProduct(prefab.name);
        zdo.Set(ProducedKey, ZNet.instance.GetTime().Ticks);
        HusbandrySkill.GiveExperience(tameable.transform.position, ProduceExperience);
    }

    private static void OnGroom(Tameable tameable)
    {
        if (tameable.m_nview.IsOwner() && tameable.IsTamed() && !IsContent(tameable))
        {
            tameable.m_nview.GetZDO().Set(GroomedKey, ZNet.instance.GetTime().Ticks);
        }
    }

    private static void OnTether(Tameable tameable, Vector3 point, bool tether)
    {
        MonsterAI ai = tameable.m_monsterAI;
        if (!tameable.m_nview.IsOwner() || ai == null || !tameable.IsTamed())
        {
            return;
        }

        ZDO zdo = tameable.m_nview.GetZDO();
        if (tether)
        {
            // Same as the vanilla "stay" command, but around the post instead of where the animal stands.
            ai.SetFollowTarget(null);
            zdo.Set(ZDOVars.s_follow, string.Empty);
            ai.SetPatrolPoint(point);
            zdo.Set(TetheredKey, true);
        }
        else if (zdo.GetBool(TetheredKey))
        {
            ai.ResetPatrolPoint();
            zdo.Set(TetheredKey, false);
        }
    }

    private static ZDO GetZdo(Tameable tameable)
    {
        return tameable != null && tameable.m_nview != null ? tameable.m_nview.GetZDO() : null;
    }

    // "Boar:LeatherScraps:1, Lox:LoxPelt:3"; malformed parts are skipped with a warning.
    private static Dictionary<string, (string Item, float Days)> GetProducts()
    {
        string text = RanchingSettings.Products.Value ?? string.Empty;
        if (text == productsText)
        {
            return products;
        }

        Dictionary<string, (string Item, float Days)> parsed = new();
        foreach (string part in text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = part.Split(':');
            if (fields.Length != 3 || fields[0].Trim().Length == 0 || fields[1].Trim().Length == 0
                || !float.TryParse(fields[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float days) || days <= 0f)
            {
                Jotunn.Logger.LogWarning($"Animal products: skipping \"{part.Trim()}\", expected Creature:Item:Days");
                continue;
            }

            parsed[fields[0].Trim()] = (fields[1].Trim(), days);
        }

        products = parsed;
        productsText = text;
        return products;
    }

    private static double SecondsSince(ZDO zdo, string key)
    {
        long ticks = zdo?.GetLong(key, 0L) ?? 0L;
        return ticks == 0L || ZNet.instance == null ? double.MaxValue : (ZNet.instance.GetTime() - new DateTime(ticks)).TotalSeconds;
    }
}
