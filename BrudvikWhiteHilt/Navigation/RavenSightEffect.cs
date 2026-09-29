using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation;

/// <summary>
/// Raven Sight, the Pathfinder's Amulet's full-adrenaline burst: Odin's ravens fly out and the map is uncovered for
/// 500 m around the player.
/// </summary>
public class RavenSightEffect : StatusEffect
{
    /// <summary>
    /// Radius in metres uncovered by the burst.
    /// </summary>
    public const float RevealRadius = 500f;

    private const string EffectKey = "se_whitehiltravensight";
    private const float Duration = 10f;

    // Raven effects from Hugin and Munin; whichever exist in this game version are played.
    private static readonly string[] ravenEffects = { "fx_raven_despawn", "vfx_raven_feathers", "sfx_raven_kaw", "sfx_raven_poof" };

    /// <summary>
    /// Registers the English text. Call from a constructor, before Valheim loads its languages.
    /// </summary>
    public static void RegisterEnglish()
    {
        Translations.AddEnglish(EffectKey, "Raven Sight");
        Translations.AddEnglish($"{EffectKey}_tooltip", "Odin's ravens show you the land for 500 m around.");
        Translations.AddEnglish("msg_whitehilt_ravensight", "The ravens show you the land around you");
    }

    /// <summary>
    /// Creates the status effect.
    /// </summary>
    /// <returns>The effect.</returns>
    public static RavenSightEffect Create()
    {
        RavenSightEffect effect = CreateInstance<RavenSightEffect>();
        effect.name = "SE_WhiteHiltRavenSight";
        effect.m_name = Translations.Token(EffectKey);
        effect.m_tooltip = Translations.Token($"{EffectKey}_tooltip");
        effect.m_ttl = Duration;
        effect.m_startEffects = CreateRavenEffects();
        return effect;
    }

    /// <summary>
    /// Uncovers the map around the local player when the burst starts.
    /// </summary>
    /// <param name="character">The character the effect is on.</param>
    public override void Setup(Character character)
    {
        base.Setup(character);
        if (character != null && character == Player.m_localPlayer && Minimap.instance != null)
        {
            Minimap.instance.Explore(character.transform.position, RevealRadius);
            character.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_ravensight");
        }
    }

    private static EffectList CreateRavenEffects()
    {
        List<GameObject> found = ravenEffects.Select(name => PrefabManager.Instance.GetPrefab(name)).Where(prefab => prefab != null).ToList();
        Jotunn.Logger.LogInfo($"Raven Sight effects: {(found.Count > 0 ? string.Join(", ", found.Select(prefab => prefab.name)) : "none found")}");
        return new EffectList
        {
            m_effectPrefabs = found.Select(prefab => new EffectList.EffectData { m_prefab = prefab, m_enabled = true }).ToArray()
        };
    }
}
