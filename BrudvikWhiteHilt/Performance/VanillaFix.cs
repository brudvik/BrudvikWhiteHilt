using BepInEx.Configuration;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// One fix of a vanilla fault: its on/off setting, and whether it runs now. It does not run while it is switched off, or
/// while Valheim Community Patch runs its own fix of the same fault (see <see cref="CommunityPatchCoexistence"/>).
/// </summary>
public sealed class VanillaFix
{
    private readonly string communityPatchId;
    private bool standDownLogged;
    private bool failed;

    /// <summary>
    /// Makes the fix.
    /// </summary>
    /// <param name="setting">Its on/off setting.</param>
    /// <param name="name">Its name in the log.</param>
    /// <param name="communityPatchId">Valheim Community Patch's id for the same fix, or null when it has none.</param>
    public VanillaFix(ConfigEntry<bool> setting, string name, string communityPatchId)
    {
        Setting = setting;
        Name = name;
        this.communityPatchId = communityPatchId;
        Active = setting.Value;
        setting.SettingChanged += (_, _) => Refresh();
    }

    /// <summary>The on/off setting.</summary>
    public ConfigEntry<bool> Setting { get; }

    /// <summary>The fix's name in the log.</summary>
    public string Name { get; }

    /// <summary>
    /// Whether the fix runs. A field, not a property, because some fixes read it thousands of times a frame; it is
    /// worked out again by <see cref="Refresh"/>.
    /// </summary>
    public bool Active;

    /// <summary>
    /// Works out <see cref="Active"/> again from the setting and from Valheim Community Patch. Logs once when the fix
    /// stands down for that mod.
    /// </summary>
    public void Refresh()
    {
        bool theirs = Setting.Value && CommunityPatchCoexistence.HasActiveFix(communityPatchId);
        if (theirs && !standDownLogged)
        {
            standDownLogged = true;
            Jotunn.Logger.LogInfo($"Performance: Valheim Community Patch runs its own {Name} fix, so White Hilt's stands down.");
        }

        Active = Setting.Value && !theirs && !failed;
    }

    /// <summary>
    /// Turns the fix off for the rest of the session after it failed, and logs why. The game's own way is used from then
    /// on, so one fault in a fix costs that fix and not the game.
    /// </summary>
    /// <param name="what">What failed, for the log.</param>
    /// <param name="ex">The exception.</param>
    public void Fail(string what, System.Exception ex)
    {
        failed = true;
        Active = false;
        Jotunn.Logger.LogError($"Performance: {what} failed ({ex.GetType().Name}: {ex.Message}); the {Name} fix is off "
                               + "until the game restarts.");
    }
}
