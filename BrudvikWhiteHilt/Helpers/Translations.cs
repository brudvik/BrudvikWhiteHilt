using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// Registers translations. English text lives next to each item in code; other languages come from embedded JSON files.
/// </summary>
public static class Translations
{
    private static readonly string[] embeddedLanguages = { "Norwegian" };
    private static readonly Dictionary<string, Func<object[]>> dynamicTexts = new();

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
    /// Fills <c>{0}</c>, <c>{1}</c>... in the text of <paramref name="key"/> every time it is shown, in every language,
    /// so numbers in the text follow the config.
    /// </summary>
    /// <param name="key">Translation key without the leading <c>$</c>.</param>
    /// <param name="values">The values, or null to leave the text as it is. Must not call Localize.</param>
    public static void AddDynamic(string key, Func<object[]> values)
    {
        dynamicTexts[key] = values;
    }

    /// <summary>
    /// Fills the placeholders of a text registered with <see cref="AddDynamic"/>; other texts are returned unchanged.
    /// </summary>
    /// <param name="key">Translation key without the leading <c>$</c>.</param>
    /// <param name="text">The translated text.</param>
    /// <returns>The text with its values.</returns>
    public static string FillDynamic(string key, string text)
    {
        if (text == null || !dynamicTexts.TryGetValue(key, out Func<object[]> values))
        {
            return text;
        }

        try
        {
            object[] args = values();
            return args == null ? text : string.Format(CultureInfo.CurrentCulture, text, args);
        }
        catch (Exception ex) when (ex is FormatException || ex is NullReferenceException)
        {
            return text;
        }
    }

    /// <summary>
    /// Drops the game's cache of translated texts, so dynamic texts show changed config values.
    /// </summary>
    public static void RefreshDynamic()
    {
        Localization.instance?.m_cache.EvictAll();
    }

    /// <summary>
    /// Looks up a translation without <c>Localize</c>, which must not run while the game is translating another text.
    /// </summary>
    /// <param name="token">Key, with or without the leading <c>$</c>.</param>
    /// <returns>The translation, or the key when there is none.</returns>
    public static string Word(string token)
    {
        return TryWord(token, out string value) ? value : token?.TrimStart('$');
    }

    /// <summary>
    /// Looks up a translation in the current language without <c>Localize</c>.
    /// </summary>
    /// <param name="token">Key, with or without the leading <c>$</c>.</param>
    /// <param name="value">The translation.</param>
    /// <returns>True if the key has a translation.</returns>
    public static bool TryWord(string token, out string value)
    {
        value = null;
        string key = token != null && token.StartsWith("$") ? token.Substring(1) : token;
        return key != null && Localization.instance != null && Localization.instance.m_translations.TryGetValue(key, out value);
    }

    /// <summary>
    /// Formats a number for a text, with at most two decimals.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <returns>The text.</returns>
    public static string Number(float value)
    {
        return value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Formats a share as a whole percentage, e.g. 0.5 as 50.
    /// </summary>
    /// <param name="share">The share.</param>
    /// <returns>The text.</returns>
    public static string Percent(float share)
    {
        return Mathf.RoundToInt(share * 100f).ToString(CultureInfo.CurrentCulture);
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
