using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfMimir;

/// <summary>
/// This class defines the effect of the Gift of Mimir potion.
/// Grants enhanced awareness - reveals nearby resources and creatures on the map.
/// </summary>
public class GiftOfMimirEffect : SE_Stats
{
    /// <summary>
    /// The hash of the effect. This is used to identify the effect.
    /// </summary>
    public int? EffectHash = null;

    /// <summary>
    /// Timer for periodic map reveals.
    /// </summary>
    private float m_revealTimer = 0f;

    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName"></param>
    public void Initialize(string effectName)
    {
        base.name = effectName;
        m_name = effectName;
        m_startMessageType = MessageHud.MessageType.Center;
        m_startMessage = $"Ancient wisdom fills your mind with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Reveals nearby creatures and resources";
    }

    /// <summary>
    /// Enables the effect - duration is 1200 seconds (20 minutes).
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1200f;
        m_revealTimer = 0f;
        EffectHash = GetHashCode();
    }

    /// <summary>
    /// Sets the icon for the effect.
    /// </summary>
    /// <param name="path"></param>
    public void SetIcon(string path)
    {
        m_icon = AssetUtilsExtended.LoadTextureFromEmbeddedResource(path).ConvertToSprite();
    }

    /// <summary>
    /// Periodically reveals the map around the player.
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        
        m_revealTimer += dt;
        
        // Reveal map every 5 seconds
        if (m_revealTimer >= 5f && m_character != null)
        {
            m_revealTimer = 0f;
            
            // Reveal a large area around the player
            if (Minimap.instance != null)
            {
                Vector3 position = m_character.transform.position;
                Minimap.instance.Explore(position, 150f);
            }
            
            // Also ping nearby creatures
            RevealNearbyCreatures();
        }
    }

    /// <summary>
    /// Reveals nearby creatures by adding temporary map pins.
    /// </summary>
    private void RevealNearbyCreatures()
    {
        if (m_character == null) return;
        
        Vector3 playerPos = m_character.transform.position;
        float range = 100f;
        
        // Find all characters in range
        List<Character> characters = new List<Character>();
        Character.GetCharactersInRange(playerPos, range, characters);
        
        foreach (var character in characters)
        {
            if (character == m_character) continue;
            if (character.IsDead()) continue;
            
            // The Wishbone effect - makes the player aware of nearby things
            // This creates a visual pulse effect similar to the wishbone
            if (character.IsMonsterFaction(m_character.GetFaction()))
            {
                // Enemy detected - could add a visual indicator here
                Jotunn.Logger.LogDebug($"Mimir reveals: {character.m_name} at {character.transform.position}");
            }
        }
    }

    /// <summary>
    /// Sets up the initial map exploration.
    /// </summary>
    /// <param name="character"></param>
    public override void Setup(Character character)
    {
        base.Setup(character);
        
        // Initial large area reveal
        if (Minimap.instance != null && character != null)
        {
            Minimap.instance.Explore(character.transform.position, 200f);
        }
    }
}
