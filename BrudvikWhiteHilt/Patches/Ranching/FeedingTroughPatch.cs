using BrudvikWhiteHilt.Pieces.Ranching.FeedingTrough;
using BrudvikWhiteHilt.Ranching;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Ranching;

/// <summary>
/// Lets hungry animals eat from a <see cref="FeedingTroughComponent"/> when there is no food on the ground.
/// </summary>
[HarmonyPatch]
public static class FeedingTroughPatch
{
    // Animals stop at the trough's edge, which the path finder cannot quite reach, so allow a little more than vanilla.
    private const float MinReach = 1.5f;
    private const float GiveUpSeconds = 30f;

    private static readonly ConditionalWeakTable<MonsterAI, FeedingState> states = new();

    /// <summary>
    /// Walks a hungry animal to the nearest trough with food it eats, and eats one item there.
    /// </summary>
    /// <param name="__instance">The animal's AI.</param>
    /// <param name="humanoid">The animal.</param>
    /// <param name="dt">Frame time.</param>
    /// <param name="__result">True while the animal is busy eating.</param>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateConsumeItem))]
    [HarmonyPostfix]
    private static void FeedFromTrough(MonsterAI __instance, Humanoid humanoid, float dt, ref bool __result)
    {
        if (__instance.m_consumeItems == null || __instance.m_consumeItems.Count == 0 || __instance.m_tamable == null)
        {
            return;
        }

        FeedingState state = states.GetOrCreateValue(__instance);

        // Food on the ground comes first, as in vanilla.
        if (__result)
        {
            state.Trough = null;
            return;
        }

        if (state.Trough == null && !TryFindTrough(__instance, state, dt))
        {
            return;
        }

        ItemDrop.ItemData food = state.Trough.FindFood(__instance.CanConsume, item => FavoriteFoods.IsFavorite(__instance.gameObject, item));
        state.WalkTime += dt;
        if (food == null || state.WalkTime > GiveUpSeconds)
        {
            state.Trough = null;
            return;
        }

        __result = true;
        Vector3 point = state.Trough.GetFeedingPoint(__instance.transform.position);
        if (!__instance.MoveTo(dt, point, Mathf.Max(__instance.m_consumeRange, MinReach), run: false))
        {
            return;
        }

        __instance.LookAt(point);
        if (!__instance.IsLookingAt(point, 20f))
        {
            return;
        }

        state.Trough.Eat(food);
        state.Trough = null;

        // Taming and feeding timers listen here; they do not use the item, but it must be a real ItemDrop.
        ItemDrop eaten = food.m_dropPrefab != null ? food.m_dropPrefab.GetComponent<ItemDrop>() : null;
        if (eaten != null)
        {
            __instance.m_onConsumedItem?.Invoke(eaten);
        }

        humanoid.m_consumeItemEffects.Create(__instance.transform.position, Quaternion.identity);
        __instance.m_animator.SetTrigger("consume");
    }

    // Now and then, while the animal is hungry, looks for a trough with food it eats within reach.
    private static bool TryFindTrough(MonsterAI ai, FeedingState state, float dt)
    {
        state.SearchTimer += dt;
        if (state.SearchTimer < ai.m_consumeSearchInterval)
        {
            return false;
        }

        state.SearchTimer = 0f;
        if (!ai.m_tamable.IsHungry())
        {
            return false;
        }

        FeedingTroughComponent trough = FeedingTroughComponent.FindNearest(ai.transform.position, ai.CanConsume);
        if (trough == null || !ai.HavePath(trough.GetFeedingPoint(ai.transform.position)))
        {
            return false;
        }

        state.Trough = trough;
        state.WalkTime = 0f;
        return true;
    }

    private sealed class FeedingState
    {
        public FeedingTroughComponent Trough;
        public float SearchTimer;
        public float WalkTime;
    }
}
