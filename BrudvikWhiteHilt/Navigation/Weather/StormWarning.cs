using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Navigation;
using BrudvikWhiteHilt.Pieces.Ships;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Weather;

/// <summary>
/// Warns everyone aboard a ship with a Navigator's Table, each on their own client, when the next weather period where
/// they are is a storm and it begins within the warning time. Once per storm.
/// </summary>
public static class StormWarning
{
    private const float CheckInterval = 5f;

    private static float nextCheck;
    private static long warnedPeriod = -1L;

    /// <summary>
    /// Checks the coming weather now and then. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        if (!ForecastSettings.StormWarning.Value || EnvMan.instance == null || ZNet.instance == null || !ShipChartTable.IsAboardWithTable(player))
        {
            return;
        }

        List<WeatherForecast.Period> periods = WeatherForecast.Foretell(1);
        if (periods.Count < 2 || WeatherForecast.IsStorm(periods[0].Env) || !WeatherForecast.IsStorm(periods[1].Env)
            || periods[1].StartsIn > ForecastSettings.StormWarningMinutes.Value * 60.0)
        {
            return;
        }

        long next = (long)ZNet.instance.GetTimeSeconds() / EnvMan.instance.m_environmentDuration + 1;
        if (next == warnedPeriod)
        {
            return;
        }

        warnedPeriod = next;
        int minutes = Mathf.Max(1, Mathf.CeilToInt((float)periods[1].StartsIn / 60f));
        player.Message(MessageHud.MessageType.Center, string.Format(Translations.Word("whitehilt_storm_coming"), minutes));
        if (ForecastSettings.StormBell.Value)
        {
            ShipBell.Ring(player.transform.position);
        }
    }
}
