using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrudvikWhiteHilt.Settings;

/// <summary>
/// Names and descriptions of config entries in the player's language. Translations use the keys
/// <c>whitehilt_cfg_&lt;section&gt;_&lt;key&gt;</c> (+ <c>_desc</c>), falling back to the section's family
/// (the part before the first dot), then to the English config text.
/// </summary>
public static class ConfigText
{
    /// <summary>
    /// Family of a section: the part before the first dot, e.g. <c>Skills</c> for <c>Skills.Woodcutting</c>.
    /// </summary>
    /// <param name="section">Config section.</param>
    /// <returns>The family.</returns>
    public static string Family(string section)
    {
        int dot = section.IndexOf('.');
        return dot < 0 ? section : section.Substring(0, dot);
    }

    /// <summary>
    /// Name of a family, shown in the window's list of groups.
    /// </summary>
    /// <param name="family">The family.</param>
    /// <returns>The name.</returns>
    public static string FamilyName(string family)
    {
        return Lookup($"whitehilt_cfgsec_{Sanitize(family)}") ?? Humanize(family);
    }

    /// <summary>
    /// Heading of a section within its family, or null for the family's own section.
    /// </summary>
    /// <param name="section">Config section.</param>
    /// <returns>The heading, or null.</returns>
    public static string SectionName(string section)
    {
        string family = Family(section);
        if (section == family)
        {
            return null;
        }

        string suffix = section.Substring(family.Length + 1);
        return LookupToken(WhiteHiltConfig.GetSectionLabel(section))
            ?? Lookup($"whitehilt_cfgsec_{Sanitize(section)}")
            ?? Lookup(Translations.ItemKey(suffix))
            ?? Humanize(suffix);
    }

    /// <summary>
    /// Name of an entry.
    /// </summary>
    /// <param name="definition">The entry's section and key.</param>
    /// <returns>The name.</returns>
    public static string Name(ConfigDefinition definition)
    {
        return LookupToken(WhiteHiltConfig.GetKeyLabel(definition.Section, definition.Key))
            ?? Lookup(Key(definition.Section, definition.Key))
            ?? Lookup(Key(Family(definition.Section), definition.Key))
            ?? Humanize(definition.Key);
    }

    /// <summary>
    /// Description of an entry.
    /// </summary>
    /// <param name="definition">The entry's section and key.</param>
    /// <param name="english">The English description from the config.</param>
    /// <returns>The description.</returns>
    public static string Description(ConfigDefinition definition, string english)
    {
        string family = Family(definition.Section);
        return Lookup(Key(definition.Section, definition.Key) + "_desc")
            ?? Lookup(Key(family, definition.Key) + "_desc")
            ?? Lookup(Key(family, "any") + "_desc")
            ?? english;
    }

    /// <summary>
    /// Name of an enum value in a dropdown.
    /// </summary>
    /// <param name="type">The enum type.</param>
    /// <param name="value">The value's name.</param>
    /// <returns>The name.</returns>
    public static string EnumName(Type type, string value)
    {
        return Lookup($"whitehilt_cfgenum_{Sanitize(type.Name)}_{Sanitize(value)}") ?? Humanize(value);
    }

    /// <summary>
    /// Whether a change only takes effect after a restart, as the English description says.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns>True if it needs a restart.</returns>
    public static bool NeedsRestart(ConfigEntryBase entry)
    {
        string text = entry.Description?.Description;
        return text != null && text.IndexOf("restart", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Registers the console command that lists entries without a translation in the current language.
    /// </summary>
    public static void RegisterCommand()
    {
        CommandManager.Instance.AddConsoleCommand(new MissingCommand());
    }

    private static string Key(string section, string key)
    {
        return $"whitehilt_cfg_{Sanitize(section)}_{Sanitize(key)}";
    }

    private static string Sanitize(string text)
    {
        StringBuilder builder = new(text.Length);
        foreach (char c in text)
        {
            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
        }

        return builder.ToString();
    }

    private static string Lookup(string key)
    {
        return Translations.TryWord(key, out string value) ? value : null;
    }

    private static string LookupToken(string token)
    {
        return token == null ? null : Lookup(token.TrimStart('$'));
    }

    /// <summary>
    /// Turns <c>FishingNetMinutes</c> into <c>Fishing net minutes</c>.
    /// </summary>
    /// <param name="key">A config key or section.</param>
    /// <returns>The readable text.</returns>
    public static string Humanize(string key)
    {
        StringBuilder builder = new(key.Length + 8);
        for (int i = 0; i < key.Length; i++)
        {
            char c = key[i];
            bool wordStart = i > 0 && char.IsUpper(c) && (char.IsLower(key[i - 1]) || (i + 1 < key.Length && char.IsLower(key[i + 1])));
            bool numberStart = i > 0 && char.IsDigit(c) && !char.IsDigit(key[i - 1]);
            if (c == '.' || c == '_')
            {
                builder.Append(' ');
                continue;
            }

            if ((wordStart || numberStart) && builder.Length > 0 && builder[builder.Length - 1] != ' ')
            {
                builder.Append(' ');
            }

            builder.Append(builder.Length == 0 ? char.ToUpperInvariant(c) : wordStart && i + 1 < key.Length && char.IsLower(key[i + 1]) ? char.ToLowerInvariant(c) : c);
        }

        return builder.ToString();
    }

    private sealed class MissingCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_config_missing";

        public override string Help => "Lists White Hilt settings without a name or description in the current language.";

        public override void Run(string[] args)
        {
            ConfigFile file = WhiteHiltConfig.File;
            List<string> missing = file.Keys
                .Where(definition => Lookup(Key(definition.Section, definition.Key)) == null
                    && Lookup(Key(Family(definition.Section), definition.Key)) == null
                    && WhiteHiltConfig.GetKeyLabel(definition.Section, definition.Key) == null)
                .Select(definition => $"{Key(definition.Section, definition.Key)}  [{definition.Section}] {definition.Key}")
                .ToList();
            missing.AddRange(file.Keys.Select(definition => Family(definition.Section)).Distinct()
                .Where(family => Lookup($"whitehilt_cfgsec_{Sanitize(family)}") == null)
                .Select(family => $"whitehilt_cfgsec_{Sanitize(family)}"));
            Jotunn.Logger.LogInfo("Settings without a translation:\n" + string.Join("\n", missing));
            global::Console.instance?.Print($"{missing.Count} settings without a translation, listed in the BepInEx log.");
        }
    }
}
