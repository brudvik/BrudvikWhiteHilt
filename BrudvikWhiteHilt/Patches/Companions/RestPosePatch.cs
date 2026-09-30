using BrudvikWhiteHilt.Companions;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Companions;

/// <summary>
/// Keeps a resting creature in place, and gets it up when it is told to follow or finds an enemy.
/// </summary>
[HarmonyPatch]
public static class RestPosePatch
{
    /// <summary>
    /// Cancels the movement the AI chose this tick while the creature rests. The rest of the AI (regeneration, targets) keeps running.
    /// </summary>
    /// <param name="__instance">The creature's AI.</param>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    [HarmonyPostfix]
    private static void KeepResting(MonsterAI __instance)
    {
        if (!__instance.TryGetComponent(out RestPose rest) || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner() || !rest.IsResting)
        {
            return;
        }

        // A sitting dog still follows; it stands up by itself when its master walks on. A trick is finished first.
        bool acting = __instance.TryGetComponent(out DogCare care) && care.IsActing;
        bool following = __instance.GetFollowTarget() != null && rest.Current != RestPose.Pose.Sit && !acting;
        if (following || __instance.m_targetCreature != null)
        {
            rest.SetPose(RestPose.Pose.None);
            return;
        }

        __instance.StopMoving();
    }
}
