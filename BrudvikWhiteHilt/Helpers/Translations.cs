using Jotunn.Managers;
using System;
using System.IO;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Registers translations. English text lives next to each item in code; other languages come from embedded JSON files.
/// </summary>
public static class Translations
{
    private static readonly string[] embeddedLanguages = { "Norwegian" };

    /// <summary>
    /// Turns a translation key into the token Valheim looks up, e.g. <c>item_x</c> into <c>$item_x</c>.
    /// </summary>
    /// <param name="key">Translation key without the leading <c>$</c>.</param>
    /// <returns>The token.</returns>
    public static string Token(string key)
    {
        return "$" + key;
    }

    /// <summary>
    /// Registers the English text for a key. Must run in the plugin's Awake, before Valheim loads its languages.
    /// </summary>
    /// <param name="key">Translation key without the leading <c>$</c>.</param>
    /// <param name="text">English text.</param>
    public static void AddEnglish(string key, string text)
    {
        LocalizationManager.Instance.GetLocalization().AddTranslation("English", key, text);
    }

    /// <summary>
    /// Loads the embedded <c>Translations/&lt;Language&gt;.json</c> files.
    /// </summary>
    public static void LoadEmbedded()
    {
        foreach (string language in embeddedLanguages)
        {
            string resourceName = $"BrudvikWhiteHilt.Translations.{language}.json";
            try
            {
                using Stream stream = typeof(Translations).Assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
                using StreamReader reader = new(stream);
                LocalizationManager.Instance.GetLocalization().AddJsonFile(language, reader.ReadToEnd());
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Failed to load the {language} translations!");
                Jotunn.Logger.LogError(ex);
            }
        }
    }
}
