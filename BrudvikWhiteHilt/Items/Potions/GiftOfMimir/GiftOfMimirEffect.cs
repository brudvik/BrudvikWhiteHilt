using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfMimir;

/// <summary>
/// This class defines the effect of the Gift of Mimir potion.
/// Reveals the map around the player and marks nearby creatures on the minimap.
/// </summary>
public class GiftOfMimirEffect : SE_Stats
{
    private static float MapRevealInterval => PotionSettings.Mimir.RevealIntervalSeconds.Value;
    private static float CreatureRevealInterval => PotionSettings.Mimir.CreatureRefreshSeconds.Value;
    private static float CreatureRange => PotionSettings.Mimir.CreatureRange.Value;

    private readonly List<Minimap.PinData> creaturePins = new();
    private readonly List<Character> nearbyCharacters = new();
    private float m_revealTimer = 0f;
    private float m_creatureTimer = 0f;

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
        m_tooltip = "Reveals the map around you and marks nearby creatures";
    }

    /// <summary>
    /// Enables the effect - configurable duration, 20 minutes by default.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = PotionSettings.Mimir.DurationMinutes.Value * 60f;
        m_revealTimer = 0f;
        m_creatureTimer = 0f;
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
    /// Sets up the initial map exploration.
    /// </summary>
    /// <param name="character"></param>
    public override void Setup(Character character)
    {
        base.Setup(character);

        if (Minimap.instance != null && character != null)
        {
            Minimap.instance.Explore(character.transform.position, PotionSettings.Mimir.InitialRevealRadius.Value);
        }

        RevealNearbyCreatures();
    }

    /// <summary>
    /// Periodically reveals the map and refreshes the creature markers.
    /// </summary>
    /// <param name="dt"></param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);

        m_revealTimer += dt;
        if (m_revealTimer >= MapRevealInterval && m_character != null && Minimap.instance != null)
        {
            m_revealTimer = 0f;
            Minimap.instance.Explore(m_character.transform.position, PotionSettings.Mimir.RevealRadius.Value);
        }

        m_creatureTimer += dt;
        if (m_creatureTimer >= CreatureRevealInterval)
        {
            m_creatureTimer = 0f;
            RevealNearbyCreatures();
        }
    }

    /// <summary>
    /// Removes the creature markers when the effect ends.
    /// </summary>
    public override void Stop()
    {
        base.Stop();
        ClearCreaturePins();
    }

    private void RevealNearbyCreatures()
    {
        ClearCreaturePins();

        // The minimap is local, so only the drinking player gets markers.
        if (m_character == null || m_character != Player.m_localPlayer || Minimap.instance == null)
        {
            return;
        }

        nearbyCharacters.Clear();
        Character.GetCharactersInRange(m_character.transform.position, CreatureRange, nearbyCharacters);

        foreach (var character in nearbyCharacters)
        {
            if (character == m_character || character.IsPlayer() || character.IsDead() || character.IsTamed())
            {
                continue;
            }

            creaturePins.Add(Minimap.instance.AddPin(character.transform.position, Minimap.PinType.Icon3, character.m_name, save: false, isChecked: false));
        }
    }

    private void ClearCreaturePins()
    {
        if (Minimap.instance != null)
        {
            foreach (var pin in creaturePins)
            {
                Minimap.instance.RemovePin(pin);
            }
        }

        creaturePins.Clear();
    }
}
