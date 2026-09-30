using System.Collections;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation;

/// <summary>
/// From <see cref="RevealLevel"/> in Exploration, map that other players shared (map tables) is drawn like map the
/// player uncovered, without vanilla's see-through layer. Only the fog texture changes: the saved map data stays as it
/// is, so the layer comes back if the skill falls below the level or the shared map is hidden.
/// </summary>
public static class SharedMapReveal
{
    /// <summary>
    /// Exploration level from which shared map is drawn as the player's own.
    /// </summary>
    public const int RevealLevel = 50;

    private const float CheckInterval = 1f;

    private static float nextCheck;

    /// <summary>
    /// True while the shared map is drawn as the player's own.
    /// </summary>
    public static bool Revealed { get; private set; }

    /// <summary>
    /// Turns the reveal on or off when the skill level or the shared map toggle changes. Call every frame.
    /// </summary>
    /// <param name="minimap">The minimap.</param>
    public static void Update(Minimap minimap)
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        bool wanted = minimap.m_showSharedMapData && ExplorationSkill.GetLevel(Player.m_localPlayer) >= RevealLevel;
        if (wanted != Revealed)
        {
            Revealed = wanted;
            Redraw(minimap);
        }
    }

    /// <summary>
    /// Forgets the reveal after vanilla cleared the fog texture, so <see cref="Update"/> draws it again.
    /// </summary>
    public static void OnMapReset()
    {
        Revealed = false;
        nextCheck = 0f;
    }

    /// <summary>
    /// Draws every shared-only pixel as the player's own, or back as shared, to match <see cref="Revealed"/>.
    /// </summary>
    /// <param name="minimap">The minimap.</param>
    public static void Redraw(Minimap minimap)
    {
        BitArray explored = minimap.m_explored;
        BitArray others = minimap.m_exploredOthers;
        Texture2D fog = minimap.m_fogTexture;
        if (explored == null || others == null || fog == null)
        {
            return;
        }

        // Red is the player's own fog, green the shared fog (0 = uncovered); the R8G8 texture is edited as vanilla does.
        float ownFog = Revealed ? 0f : 1f;
        Color[] pixels = fog.GetPixels();
        bool changed = false;
        for (int i = 0; i < pixels.Length && i < others.Length; i++)
        {
            if (others[i] && !explored[i] && pixels[i].r != ownFog)
            {
                pixels[i].r = ownFog;
                changed = true;
            }
        }

        if (changed)
        {
            fog.SetPixels(pixels);
            fog.Apply();
        }
    }

    /// <summary>
    /// Draws one newly shared pixel as the player's own while <see cref="Revealed"/>.
    /// </summary>
    /// <param name="minimap">The minimap.</param>
    /// <param name="x">Pixel column.</param>
    /// <param name="y">Pixel row.</param>
    public static void OnSharedPixel(Minimap minimap, int x, int y)
    {
        if (!Revealed)
        {
            return;
        }

        Color pixel = minimap.m_fogTexture.GetPixel(x, y);
        pixel.r = 0f;
        minimap.m_fogTexture.SetPixel(x, y, pixel);
    }
}
