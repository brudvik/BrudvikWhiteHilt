using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;

/// <summary>
/// A White Hilt portal the travel map can be opened from: a built portal or the one on a White Hilt Ship's deck.
/// Naming goes through <see cref="TextReceiver"/>.
/// </summary>
public interface IWhiteHiltPortal : TextReceiver
{
    /// <summary>
    /// The portal's name, or empty.
    /// </summary>
    string PortalName { get; }

    /// <summary>
    /// True if only its builder may travel to it.
    /// </summary>
    bool IsPrivate { get; }

    /// <summary>
    /// The portal's stable id, or empty until the owner has given it one.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Where the portal is in the world.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// True if the player may rename the portal and change its privacy.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True if allowed.</returns>
    bool CanEdit(Player player);

    /// <summary>
    /// Switches the portal between public and private.
    /// </summary>
    void TogglePrivate();

    /// <summary>
    /// The name shown when hovering the portal; may contain localization tokens.
    /// </summary>
    /// <returns>The name.</returns>
    string GetHoverName();
}
