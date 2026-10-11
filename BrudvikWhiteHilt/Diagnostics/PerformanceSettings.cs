using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Diagnostics;

/// <summary>
/// Each machine's own settings for drawing and measuring, sections "Graphics" and "Diagnostics".
/// </summary>
public static class PerformanceSettings
{
    /// <summary>
    /// Share of the screen's height below which the mod's own models are hidden; 0 never hides them. Read when the
    /// models are made, so a change needs a restart.
    /// </summary>
    public static ConfigEntry<float> ModelCullSize { get; private set; }

    /// <summary>Minutes between performance reports written to the log; 0 writes none.</summary>
    public static ConfigEntry<int> PerfLogMinutes { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake, before any item or piece is made.
    /// </summary>
    public static void Initialize()
    {
        ModelCullSize = WhiteHiltConfig.BindLocal("Graphics", "ModelCullSize", 0.015f,
            "The mod's own models (plants, props, pieces) are hidden when they are smaller than this share of the screen's "
            + "height, as Valheim's own are. 0 never hides them. Restart required.", new AcceptableValueRange<float>(0f, 0.1f));
        PerfLogMinutes = WhiteHiltConfig.BindLocal("Diagnostics", "PerfLogMinutes", 0,
            "Measure the mod's time per frame and the network traffic all the time, and write a report to the log every so "
            + "many minutes, like whitehilt_perf. Meant for a dedicated server, which has no console. 0 is off; measuring "
            + "costs a little time itself.", new AcceptableValueRange<int>(0, 240));
    }
}
