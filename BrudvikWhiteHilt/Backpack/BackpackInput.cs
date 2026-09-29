using BepInEx.Configuration;
using BrudvikWhiteHilt.Building.Media;
using UnityEngine;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Key handling shared by the backpack features.
/// </summary>
public static class BackpackInput
{
    /// <summary>
    /// True on the frame a bound key combination is pressed.
    /// </summary>
    /// <param name="key">The key config.</param>
    /// <returns>True when pressed.</returns>
    public static bool Pressed(ConfigEntry<KeyboardShortcut> key)
    {
        return key.Value.MainKey != KeyCode.None && key.Value.IsDown();
    }

    /// <summary>
    /// True while the player types in chat, the console, a text field or the menu is open.
    /// </summary>
    /// <returns>True when keys should be ignored.</returns>
    public static bool Typing()
    {
        return (Chat.instance != null && Chat.instance.HasFocus()) || Console.IsVisible() || TextInput.IsVisible()
            || Menu.IsVisible() || MediaPanel.Typing;
    }
}
