using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The Fimbulwinter after Ragnarok: for a while the snow falls and the cold bites in every biome but the Ashlands, the
/// light is dim, and no crop grows (the plants' growing time leaves it out). The server sets when it begins and ends
/// as Ragnarok passes; every client shows it where its player is and tells them when it falls and when it loosens.
/// On the <see cref="Game"/> object.
/// </summary>
public class Fimbulwinter : MonoBehaviour
{
    private const string EnvName = "WhiteHilt_Fimbulwinter";
    private const float Light = 0.65f;

    private static readonly string[] sources = { "Snow", "SnowStorm", "Twilight_Snow" };

    private static EnvMan registeredWith;

    private bool shown;

    /// <summary>
    /// The weather of the Fimbulwinter where the local player is, if it holds and no storm is near. Called after the
    /// game's own forced weathers and the storms'.
    /// </summary>
    /// <param name="env">The environment manager.</param>
    /// <returns>The weather's name, or null to leave the weather alone.</returns>
    public static string Override(EnvMan env)
    {
        Player player = Player.m_localPlayer;
        if (player == null || !Holds() || StormState.Visible || player.GetCurrentBiome() == Heightmap.Biome.AshLands)
        {
            return null;
        }

        Register(env);
        return env.GetEnv(EnvName) != null ? EnvName : null;
    }

    private static bool Holds() => StormSettings.Enabled.Value && StormSettings.RagnarokFimbul.Value && StormState.InFimbulwinter;

    // A copy of the game's snowfall: cold by day, freezing by night, and darker.
    private static void Register(EnvMan env)
    {
        if (registeredWith == env)
        {
            return;
        }

        registeredWith = env;
        EnvSetup source = sources.Select(env.GetEnv).FirstOrDefault(found => found != null);
        if (source == null)
        {
            Jotunn.Logger.LogWarning("Storms: no snow to copy for the Fimbulwinter");
            return;
        }

        EnvSetup winter = source.Clone();
        winter.m_name = EnvName;
        winter.m_default = false;
        winter.m_isCold = true;
        winter.m_isColdAtNight = true;
        winter.m_isFreezing = false;
        winter.m_isFreezingAtNight = true;
        winter.m_isWet = false;
        winter.m_lightIntensityDay = source.m_lightIntensityDay * Light;
        winter.m_lightIntensityNight = source.m_lightIntensityNight * Light;
        env.AppendEnvironment(winter);
        Jotunn.Logger.LogInfo($"Storms: the Fimbulwinter is a copy of {source.m_name}");
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool holds = player != null && Holds();
        if (holds == shown)
        {
            return;
        }

        shown = holds;
        if (player != null)
        {
            player.Message(MessageHud.MessageType.Center, holds ? "$msg_whitehilt_fimbul_start" : "$msg_whitehilt_fimbul_end");
        }
    }
}
