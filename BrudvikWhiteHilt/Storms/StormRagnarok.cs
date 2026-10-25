using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// What makes Ragnarok more than a storm: the wolves Skoll and Hati swallow the sun and the moon, so the light goes out
/// in a red dark as the storm breaks and comes back only when it has passed; and the earth shakes now and then, as
/// Yggdrasil trembles, wearing down what is already weak in the buildings. Every client darkens its own sky and feels
/// the quakes at the same moments as the others; each harms only the pieces it owns. On the <see cref="Game"/> object.
/// </summary>
public class StormRagnarok : MonoBehaviour
{
    private const float SwallowSeconds = 40f;
    private const float ReturnSeconds = 30f;
    private const float QuakeLength = 5f;
    private const float QuakeReach = 40f;
    private const float QuakeShake = 2.2f;
    private const float WeakShare = 0.5f;

    private static readonly Color blood = new(0.5f, 0.07f, 0.03f);
    private static readonly Color ember = new(0.16f, 0.03f, 0.02f);

    private static float eclipse;

    private double lastTime = -1.0;
    private float quakeStarted = float.NegativeInfinity;
    private int lastNumber;
    private bool shownSwallowed;

    /// <summary>How much of the light the wolves have swallowed where the local player is, 0 to 1.</summary>
    public static float Eclipse => eclipse;

    /// <summary>
    /// Darkens and reddens the light the game has just set, as far as the sun is swallowed. Called after
    /// <see cref="EnvMan.SetEnv"/>.
    /// </summary>
    public static void ApplyLight()
    {
        // Below ground the game's own light holds.
        if (eclipse <= 0.001f || (Player.m_localPlayer != null && Player.m_localPlayer.InInterior()))
        {
            return;
        }

        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, RenderSettings.ambientLight * 0.3f + ember, eclipse);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, ember, eclipse * 0.85f);
        Light sun = EnvMan.instance != null ? EnvMan.instance.m_dirLight : null;
        if (sun != null)
        {
            sun.color = Color.Lerp(sun.color, blood, eclipse * 0.8f);
            sun.intensity *= Mathf.Lerp(1f, 0.08f, eclipse);
        }
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool here = player != null && StormSettings.Enabled.Value && StormState.Ragnarok && StormState.Visible
            && StormSettings.HasStorms(player.GetCurrentBiome());
        UpdateEclipse(player, here);
        UpdateQuakes(player, here);
    }

    // The wolves swallow the light slowly as Ragnarok breaks, and let it go more quickly when it passes.
    private void UpdateEclipse(Player player, bool here)
    {
        float target = here && StormSettings.RagnarokSky.Value ? RagnarokRules.Eclipse(true, StormState.Phase, StormState.Wave) : 0f;
        eclipse = Mathf.MoveTowards(eclipse, target, Time.deltaTime / (target > eclipse ? SwallowSeconds : ReturnSeconds));

        if (StormState.Number != lastNumber)
        {
            lastNumber = StormState.Number;
            shownSwallowed = false;
        }

        if (!shownSwallowed && here && StormState.Raging && StormSettings.RagnarokSky.Value)
        {
            shownSwallowed = true;
            bool night = EnvMan.IsNight();
            player.Message(MessageHud.MessageType.TopLeft, night ? "$msg_whitehilt_ragnarok_moon" : "$msg_whitehilt_ragnarok_sun");
            StormAmbience.PlayEffect("sfx_storm_ragnarok_wolves");
        }
    }

    // A quake at the moments every machine works out alike, while Ragnarok rages; not in its eye.
    private void UpdateQuakes(Player player, bool here)
    {
        double now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : 0.0;
        bool quaking = here && StormSettings.RagnarokQuakes.Value && StormState.Raging && !StormState.InEye && !player.IsDead();
        if (quaking && lastTime >= 0.0 && RagnarokRules.QuakeBetween(lastTime, now, StormState.Number, StormSettings.RagnarokQuakeSeconds.Value))
        {
            Quake(player);
        }

        lastTime = quaking ? now : -1.0;
        float strength = RagnarokRules.QuakeStrength(Time.time - quakeStarted, QuakeLength);
        if (strength > 0f && player != null && GameCamera.instance != null)
        {
            GameCamera.instance.AddShake(player.transform.position, QuakeReach, QuakeShake * strength, true);
        }
    }

    private void Quake(Player player)
    {
        quakeStarted = Time.time;
        StormAmbience.PlayEffect("sfx_storm_quake");
        player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_ragnarok_quake");
        float damage = StormSettings.RagnarokQuakeDamage.Value;
        if (damage <= 0f)
        {
            return;
        }

        // What is already weak takes a blow: the pieces this machine owns, near the player.
        Vector3 position = player.transform.position;
        foreach (WearNTear piece in WearNTear.GetAllInstances().ToArray())
        {
            ZNetView view = piece != null ? piece.m_nview : null;
            if (view == null || !view.IsValid() || !view.IsOwner() || Vector3.Distance(piece.transform.position, position) > QuakeReach
                || piece.GetHealthPercentage() > WeakShare)
            {
                continue;
            }

            piece.ApplyDamage(piece.m_health * damage);
        }
    }
}
