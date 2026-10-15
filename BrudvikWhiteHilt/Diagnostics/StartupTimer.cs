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

    /// <summary>
    /// Starts timing a phase made of steps that follow each other in one method.
    /// </summary>
    /// <param name="phase">Name of the phase in the log.</param>
    /// <returns>The laps: mark the end of each step with <see cref="Laps.Lap"/>, then log them with
    /// <see cref="Laps.Report"/>.</returns>
    public static Laps Begin(string phase) => new(phase);

    /// <summary>
    /// The steps of one phase, timed one after the other and logged on one line.
    /// </summary>
    public sealed class Laps
    {
        private readonly string phase;
        private readonly Stopwatch total = Stopwatch.StartNew();
        private readonly Stopwatch step = Stopwatch.StartNew();
        private readonly List<string> steps = new();

        internal Laps(string phase)
        {
            this.phase = phase;
        }

        /// <summary>
        /// Gets the time since the phase began, in milliseconds.
        /// </summary>
        public double TotalMs => total.Elapsed.TotalMilliseconds;

        /// <summary>
        /// Ends a step: the time since the previous one is its time.
        /// </summary>
        /// <param name="name">Name of the step in the log.</param>
        public void Lap(string name)
        {
            steps.Add($"{name} {step.Elapsed.TotalMilliseconds:0} ms");
            step.Restart();
        }

        /// <summary>
        /// Logs the phase's total and every step's time.
        /// </summary>
        public void Report()
        {
            Jotunn.Logger.LogInfo($"Start-up: {phase} took {TotalMs:0} ms: {string.Join(", ", steps)}");
        }
    }
}
