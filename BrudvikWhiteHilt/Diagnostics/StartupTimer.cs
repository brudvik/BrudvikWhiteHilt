using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BrudvikWhiteHilt.Diagnostics;

/// <summary>
/// Times the mod's start-up: how long each item, piece and forageable takes to be made, and each phase in total. The
/// result goes to the log, with the slowest entries, so slow start-ups can be traced to the content that causes them.
/// </summary>
public static class StartupTimer
{
    private const int SlowestShown = 10;

    /// <summary>
    /// Runs <paramref name="action"/> for every entry, timing each, and logs the phase's total and its slowest entries.
    /// </summary>
    /// <typeparam name="T">Kind of entry.</typeparam>
    /// <param name="phase">Name of the phase in the log.</param>
    /// <param name="entries">The entries, in the order they are made.</param>
    /// <param name="action">What is done with each entry.</param>
    public static void Each<T>(string phase, IEnumerable<T> entries, Action<T> action)
    {
        List<(string Name, double Ms)> timed = new();
        Stopwatch total = Stopwatch.StartNew();
        foreach (T entry in entries)
        {
            Stopwatch one = Stopwatch.StartNew();
            action(entry);
            timed.Add((entry.GetType().Name, one.Elapsed.TotalMilliseconds));
        }

        string slowest = string.Join(", ", timed.OrderByDescending(item => item.Ms).Take(SlowestShown)
            .Select(item => $"{item.Name} {item.Ms:0} ms"));
        Jotunn.Logger.LogInfo($"Start-up: {phase} took {total.Elapsed.TotalMilliseconds:0} ms for {timed.Count}; slowest: {slowest}");
    }

    /// <summary>
    /// Runs a step of the start-up and logs how long it took.
    /// </summary>
    /// <param name="phase">Name of the step in the log.</param>
    /// <param name="action">The step.</param>
    public static void Step(string phase, Action action)
    {
        Stopwatch watch = Stopwatch.StartNew();
        action();
        Jotunn.Logger.LogInfo($"Start-up: {phase} took {watch.Elapsed.TotalMilliseconds:0} ms");
    }
}
