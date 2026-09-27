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
    /// Translation key of an item, e.g. <c>item_whitehiltsword</c>. Its description uses the key plus <c>_description</c>.
    /// </summary>
    /// <param name="prefabName">Prefab name of the item.</param>
    /// <returns>The translation key.</returns>
    public static string ItemKey(string prefabName)
    {
        return $"item_{prefabName.ToLowerInvariant()}";
    }

    /// <summary>
    /// Registers the English name and description under <paramref name="key"/> and <paramref name="key"/><c>_description</c>.
    /// </summary>
    /// <param name="key">Translation key of the name.</param>
    /// <param name="name">English name.</param>
    /// <param name="description">English description.</param>
    public static void AddEnglishNameAndDescription(string key, string name, string description)
    {
        AddEnglish(key, name);
        AddEnglish($"{key}_description", description);
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
