using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Party;

/// <summary>
/// Config for the party list under the hotbar, section "Party". Only whether it is allowed at all is the server's.
/// </summary>
public static class PartySettings
{
    private const string Section = "Party";

    /// <summary>Whether the server lets players share their health and show the party list.</summary>
    public static ConfigEntry<bool> AllowParty { get; private set; }

    /// <summary>Whether the player shows the party list.</summary>
    public static ConfigEntry<bool> Enabled { get; private set; }

    /// <summary>Whether stamina is shown under the health.</summary>
    public static ConfigEntry<bool> ShowStamina { get; private set; }

    /// <summary>Whether eitr is shown for players who have it.</summary>
    public static ConfigEntry<bool> ShowEitr { get; private set; }

    /// <summary>Whether the distance to each player is shown.</summary>
    public static ConfigEntry<bool> ShowDistance { get; private set; }

    /// <summary>Whether a few status effects are shown after the name.</summary>
    public static ConfigEntry<bool> ShowEffects { get; private set; }

    /// <summary>Whether you are in the list yourself.</summary>
    public static ConfigEntry<bool> ShowSelf { get; private set; }

    /// <summary>Most players in the list.</summary>
    public static ConfigEntry<int> MaxPlayers { get; private set; }

    /// <summary>Size of the list.</summary>
    public static ConfigEntry<float> Scale { get; private set; }

    /// <summary>Shows or hides the list.</summary>
    public static ConfigEntry<KeyboardShortcut> KeyToggle { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        AllowParty = WhiteHiltConfig.BindAdminOnly(Section, "AllowParty", true,
            "Players share their health, stamina and eitr, and may show the others in a list under the hotbar.");
        Enabled = WhiteHiltConfig.BindLocal(Section, "Enabled", true,
            "Show the other players under the hotbar: portrait, name, health and stamina with numbers, nearest first. "
            + "Hidden in the inventory, in build mode, on the large map and in menus.");
        ShowStamina = WhiteHiltConfig.BindLocal(Section, "ShowStamina", true, "Show stamina under the health.");
        ShowEitr = WhiteHiltConfig.BindLocal(Section, "ShowEitr", true, "Show eitr for players who have it.");
        ShowDistance = WhiteHiltConfig.BindLocal(Section, "ShowDistance", true,
            "Show how far away each player is, when they are near or share their position on the map.");
        ShowEffects = WhiteHiltConfig.BindLocal(Section, "ShowEffects", true,
            "Show up to four status effects after the name, burning, poison and frost first.");
        ShowSelf = WhiteHiltConfig.BindLocal(Section, "ShowSelf", false, "Show yourself in the list too.");
        MaxPlayers = WhiteHiltConfig.BindLocal(Section, "MaxPlayers", 8, "Most players in the list.", new AcceptableValueRange<int>(1, 16));
        Scale = WhiteHiltConfig.BindLocal(Section, "Scale", 1f, "Size of the list.", new AcceptableValueRange<float>(0.6f, 2f));
        KeyToggle = WhiteHiltConfig.BindLocal(Section + ".Keys", "ToggleParty", KeyboardShortcut.Empty, "Show or hide the party list.");

        Translations.AddEnglish("whitehilt_party_dead", "dead");
    }
}
