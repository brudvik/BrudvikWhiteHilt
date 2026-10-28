using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Each machine's own switches for the fixes that make the world stream in without hitches: the map's fog and save,
/// the loading screen after a portal or respawn, and how many objects are made per frame. Section "Performance".
/// </summary>
public static class StreamingSettings
{
    private const string Section = "Performance";

    /// <summary>Uncovering the map sends only the changed patch of the fog texture to the graphics card.</summary>
    public static ConfigEntry<bool> MapFogUpload { get; private set; }

    /// <summary>Saving the character writes the map from the packed bits and reuses an unchanged map.</summary>
    public static ConfigEntry<bool> MapSave { get; private set; }

    /// <summary>A portal or respawn ends its loading screen when the server has sent the destination.</summary>
    public static ConfigEntry<bool> LoadingWait { get; private set; }

    /// <summary>Objects are made within a time budget per frame instead of a fixed count.</summary>
    public static ConfigEntry<bool> ObjectBudget { get; private set; }

    /// <summary>Milliseconds per frame the client spends making objects, before adapting.</summary>
    public static ConfigEntry<float> ObjectBudgetMs { get; private set; }

    /// <summary>Writes a line to the log every minute with how object creation went.</summary>
    public static ConfigEntry<bool> ObjectBudgetLog { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        MapFogUpload = WhiteHiltConfig.BindLocal(Section, "MapFogUpload", true,
            "When walking uncovers the map, send only the small patch that changed to the graphics card instead of the "
            + "whole 8 MB fog texture every two seconds. Off is the game's own way.");
        MapSave = WhiteHiltConfig.BindLocal(Section, "MapSave", true,
            "When the character is saved, write the explored map from its packed bits instead of eight million single "
            + "values, and reuse the last saved map when nothing was uncovered and no pin changed. The saved file is the "
            + "same, byte for byte; this is checked against the game's own code the first time a world loads.");
        LoadingWait = WhiteHiltConfig.BindLocal(Section, "LoadingWait", true,
            "After a portal, a respawn or joining, end the loading screen as soon as the server has sent the buildings and "
            + "terrain at the destination and they are built, instead of after a fixed 8 seconds. If the server does not "
            + "confirm in time, the game's own wait applies.");
        ObjectBudget = WhiteHiltConfig.BindLocal(Section, "ObjectBudget", true,
            "Make the objects that come into view within a time budget per frame, nearest first, instead of a fixed number "
            + "however heavy they are. Objects close to the player are always made at least as fast as the game does. "
            + "Behind a loading screen and on a dedicated server the game's own way is used.");
        ObjectBudgetMs = WhiteHiltConfig.BindLocal(Section, "ObjectBudgetMs", 5f,
            "Milliseconds per frame for making objects. It drops a fifth when the game runs below 30 frames per second "
            + "and rises a fifth while many objects wait.", new AcceptableValueRange<float>(2f, 16f));
        ObjectBudgetLog = WhiteHiltConfig.BindLocal(Section, "ObjectBudgetLog", false,
            "Write a line to the log every minute with how many objects were made, how many frames hit the budget and "
            + "the slowest frame.");
    }
}
