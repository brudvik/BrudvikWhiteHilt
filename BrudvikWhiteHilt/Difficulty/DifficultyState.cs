using System;
using System.Globalization;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// The difficulty as the server last sent it: the pressure and what it is made of.
/// Every client keeps a copy, since creatures spawn on the client that owns their zone.
/// </summary>
public static class DifficultyState
{
    /// <summary>Pressure from 0 to 1.</summary>
    public static float Pressure { get; private set; }

    /// <summary>Biomes visited by the furthest player online, 1 to 7.</summary>
    public static int Biomes { get; private set; } = 1;

    /// <summary>Players online.</summary>
    public static int Players { get; private set; }

    /// <summary>World day.</summary>
    public static int Days { get; private set; }

    /// <summary>Players online wearing White Hilt gear.</summary>
    public static int GearPlayers { get; private set; }

    /// <summary>Pressure before smoothing.</summary>
    public static float RawPressure { get; private set; }

    /// <summary>
    /// Highest star count the visited biomes allow.
    /// </summary>
    public static int UnlockedMaxStars =>
        Biomes >= DifficultySettings.BiomesFor5Stars.Value ? 5 :
        Biomes >= DifficultySettings.BiomesFor4Stars.Value ? 4 :
        Biomes >= DifficultySettings.BiomesFor3Stars.Value ? 3 : 2;

    /// <summary>
    /// Stores a new state.
    /// </summary>
    /// <param name="pressure">Smoothed pressure.</param>
    /// <param name="raw">Pressure before smoothing.</param>
    /// <param name="biomes">Biomes visited.</param>
    /// <param name="players">Players online.</param>
    /// <param name="days">World day.</param>
    /// <param name="gearPlayers">Players wearing White Hilt gear.</param>
    public static void Set(float pressure, float raw, int biomes, int players, int days, int gearPlayers)
    {
        Pressure = Mathf.Clamp01(pressure);
        RawPressure = Mathf.Clamp01(raw);
        Biomes = Math.Max(1, biomes);
        Players = players;
        Days = days;
        GearPlayers = gearPlayers;
    }

    /// <summary>
    /// Writes the state for the clients.
    /// </summary>
    /// <returns>The package.</returns>
    public static ZPackage Write()
    {
        ZPackage package = new();
        package.Write(Pressure);
        package.Write(RawPressure);
        package.Write(Biomes);
        package.Write(Players);
        package.Write(Days);
        package.Write(GearPlayers);
        return package;
    }

    /// <summary>
    /// Reads a state the server sent.
    /// </summary>
    /// <param name="package">The package.</param>
    public static void Read(ZPackage package)
    {
        Set(package.ReadSingle(), package.ReadSingle(), package.ReadInt(), package.ReadInt(), package.ReadInt(), package.ReadInt());
    }

    /// <summary>
    /// Forgets the last world's state.
    /// </summary>
    public static void Reset()
    {
        Set(0f, 0f, 1, 0, 0, 0);
    }

    /// <summary>
    /// A readable summary for the console.
    /// </summary>
    /// <returns>The summary.</returns>
    public static string Describe()
    {
        CultureInfo culture = CultureInfo.InvariantCulture;
        string pressure = Pressure.ToString("0.00", culture);
        string raw = RawPressure.ToString("0.00", culture);
        return $"White Hilt difficulty: pressure {pressure} (target {raw})\n" +
               $"  players online {Players}, day {Days}, biomes visited {Biomes}, wearing White Hilt gear {GearPlayers}\n" +
               $"  highest stars from biomes {UnlockedMaxStars}, stars {(CreatureStars.Active ? "on" : "off")}";
    }
}
