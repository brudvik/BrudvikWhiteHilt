using UnityEngine;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Lets Unity load assets at full speed while the screen is black for a portal or a respawn. The game keeps
/// <c>Application.backgroundLoadingPriority</c> at Low in the world, so loading takes at most a couple of milliseconds
/// of each frame and does not cause hitches; it raises it only for a scene load. Behind the loading screen of a portal
/// or a respawn there is nothing to keep smooth, so High lets what the destination needs arrive sooner. The request goes
/// through the game's own <c>BackgroundLoadingBudgetController</c>, so it stacks with the game's requests instead of
/// overwriting them. The idea comes from HitchFix (MIT).
/// </summary>
public static class LoadingPriority
{
    private static bool raised;

    /// <summary>
    /// Raises or lowers the priority after the HUD has updated its black screen.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    /// <param name="player">The local player, or null while waiting to spawn.</param>
    public static void Update(Hud hud, Player player)
    {
        bool want = VanillaFixSettings.LoadingPriority.Active && hud.m_loadingScreen != null
            && hud.m_loadingScreen.gameObject.activeSelf && hud.m_loadingScreen.alpha >= 0.99f
            && (player == null ? Game.instance != null && Game.instance.WaitingForRespawn() : player.IsTeleporting());
        Set(want);
    }

    /// <summary>
    /// Gives back the raised priority, for when the HUD goes away with the world.
    /// </summary>
    public static void Release()
    {
        Set(false);
    }

    private static void Set(bool high)
    {
        if (high == raised)
        {
            return;
        }

        raised = high;
        if (high)
        {
            BackgroundLoadingBudgetController.RequestLoadingBudget(ThreadPriority.High);
        }
        else
        {
            BackgroundLoadingBudgetController.ReleaseLoadingBudgetRequest(ThreadPriority.High);
        }
    }
}
