using BrudvikWhiteHilt.Ranching;
using HarmonyLib;
using System;
using System.Linq;

namespace BrudvikWhiteHilt.Patches.Ranching;

/// <summary>
/// Favourite foods and the Animal Husbandry skill: faster taming, longer feeding and faster, larger herds,
/// and experience for the players nearby.
/// </summary>
[HarmonyPatch]
public static class TamingPatches
{
    // Stored on the animal, so every machine agrees on how long its last meal lasts.
    private const string FedFavoriteKey = "whitehilt_fed_favorite";
    private const string FedDurationKey = "whitehilt_fed_duration";

    // A groomed animal halves both the pregnancy and vanilla's chance to skip a breeding check.
    private const float ContentBreedingFactor = 0.5f;

    /// <summary>
    /// Remembers whether the animal ate a favourite, how long the meal lasts, and gives experience.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="item">The food.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.OnConsumedItem))]
    [HarmonyPostfix]
    private static void RememberMeal(Tameable __instance, ItemDrop item)
    {
        ZDO zdo = GetOwnedZdo(__instance.m_nview);
        if (zdo == null)
        {
            return;
        }

        bool favorite = FavoriteFoods.IsFavorite(__instance.gameObject, item != null ? item.m_itemData : null);
        float factor = HusbandrySkill.GetFactorNear(__instance.transform.position);
        zdo.Set(FedFavoriteKey, favorite);
        zdo.Set(FedDurationKey, (favorite ? FavoriteFoods.FedDuration : 1f) * HusbandrySkill.FedDuration(factor));
        HusbandrySkill.GiveExperience(__instance.transform.position, HusbandrySkill.FeedingExperience);
    }

    /// <summary>
    /// Keeps the animal fed for longer after a favourite meal or with a skilled keeper nearby.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__result">Whether it is hungry.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.IsHungry))]
    [HarmonyPostfix]
    private static void KeepFedLonger(Tameable __instance, ref bool __result)
    {
        ZDO zdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
        if (!__result || zdo == null || ZNet.instance == null)
        {
            return;
        }

        float duration = zdo.GetFloat(FedDurationKey, 1f);
        long lastFeeding = zdo.GetLong(ZDOVars.s_tameLastFeeding, 0L);
        if (duration > 1f && lastFeeding != 0L)
        {
            __result = (ZNet.instance.GetTime() - new DateTime(lastFeeding)).TotalSeconds > __instance.m_fedDuration * duration;
        }
    }

    /// <summary>
    /// Speeds up taming after a favourite meal and with a skilled keeper nearby, and gives experience.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="time">Seconds of taming to take off.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.DecreaseRemainingTime))]
    [HarmonyPrefix]
    private static void SpeedUpTaming(Tameable __instance, ref float time)
    {
        ZDO zdo = GetOwnedZdo(__instance.m_nview);
        if (zdo == null)
        {
            return;
        }

        time *= HusbandrySkill.TamingSpeed(HusbandrySkill.GetFactorNear(__instance.transform.position));
        if (zdo.GetBool(FedFavoriteKey))
        {
            time *= FavoriteFoods.TamingSpeed;
        }

        HusbandrySkill.GiveExperience(__instance.transform.position, HusbandrySkill.TamingTickExperience);
    }

    /// <summary>
    /// Notes whether the animal was wild before taming.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__state">True when it was already tame.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Tame))]
    [HarmonyPrefix]
    private static void BeforeTame(Tameable __instance, out bool __state)
    {
        __state = __instance.IsTamed();
    }

    /// <summary>
    /// Gives experience when an animal becomes tame.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__state">True when it was already tame.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Tame))]
    [HarmonyPostfix]
    private static void AfterTame(Tameable __instance, bool __state)
    {
        if (!__state && __instance.IsTamed())
        {
            HusbandrySkill.GiveExperience(__instance.transform.position, HusbandrySkill.TamedExperience);
        }
    }

    /// <summary>
    /// Shortens pregnancy and allows larger herds while a skilled keeper is nearby, and breeds groomed animals faster.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__state">The vanilla values, restored afterwards.</param>
    [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
    [HarmonyPrefix]
    private static void ApplySkillToBreeding(Procreation __instance, out BreedingState __state)
    {
        __state = null;
        if (GetOwnedZdo(__instance.m_nview) == null)
        {
            return;
        }

        float factor = HusbandrySkill.GetFactorNear(__instance.transform.position);
        __state = new BreedingState(__instance.m_pregnancyDuration, __instance.m_pregnancyChance, __instance.m_maxCreatures, __instance.IsPregnant());
        __instance.m_pregnancyDuration *= HusbandrySkill.PregnancyDuration(factor);
        __instance.m_maxCreatures += HusbandrySkill.ExtraHerd(factor);
        if (__instance.m_tameable != null && AnimalCare.IsContent(__instance.m_tameable))
        {
            __instance.m_pregnancyDuration *= ContentBreedingFactor;
            __instance.m_pregnancyChance *= ContentBreedingFactor;
        }
    }

    /// <summary>
    /// Restores the vanilla values, also after an error, and gives experience for a birth.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__state">The vanilla values.</param>
    [HarmonyPatch(typeof(Procreation), nameof(Procreation.Procreate))]
    [HarmonyFinalizer]
    private static void RestoreBreeding(Procreation __instance, BreedingState __state)
    {
        if (__state == null)
        {
            return;
        }

        __instance.m_pregnancyDuration = __state.PregnancyDuration;
        __instance.m_pregnancyChance = __state.PregnancyChance;
        __instance.m_maxCreatures = __state.MaxCreatures;
        if (__state.WasPregnant && !__instance.IsPregnant())
        {
            HusbandrySkill.GiveExperience(__instance.transform.position, HusbandrySkill.BirthExperience);
        }
    }

    /// <summary>
    /// Shows the animal's favourite foods.
    /// </summary>
    /// <param name="__instance">The animal.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    [HarmonyPostfix]
    private static void ShowFavorites(Tameable __instance, ref string __result)
    {
        string[] tokens = FavoriteFoods.GetFavoriteTokens(__instance.gameObject).ToArray();
        if (tokens.Length > 0 && !string.IsNullOrEmpty(__result))
        {
            __result += Localization.instance.Localize($"\n$whitehilt_favorite_food: {string.Join(", ", tokens)}");
        }
    }

    /// <summary>
    /// Lets the player receive Animal Husbandry experience from other machines.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    [HarmonyPostfix]
    private static void RegisterExperienceRpc(Player __instance)
    {
        HusbandrySkill.RegisterRpc(__instance);
    }

    /// <summary>
    /// Shares the local player's skill level as soon as it spawns.
    /// </summary>
    /// <param name="__instance">The player.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    [HarmonyPostfix]
    private static void ShareLevelOnSpawn(Player __instance)
    {
        HusbandrySkill.ShareLevel(__instance);
    }

    private static ZDO GetOwnedZdo(ZNetView nview)
    {
        return nview != null && nview.IsValid() && nview.IsOwner() ? nview.GetZDO() : null;
    }

    /// <summary>
    /// Vanilla breeding values saved while the skill bonus applies.
    /// </summary>
    private sealed class BreedingState
    {
        public BreedingState(float pregnancyDuration, float pregnancyChance, int maxCreatures, bool wasPregnant)
        {
            PregnancyDuration = pregnancyDuration;
            PregnancyChance = pregnancyChance;
            MaxCreatures = maxCreatures;
            WasPregnant = wasPregnant;
        }

        public float PregnancyDuration { get; }

        public float PregnancyChance { get; }

        public int MaxCreatures { get; }

        public bool WasPregnant { get; }
    }
}
