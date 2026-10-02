using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships;

/// <summary>
/// The ship's bell: two strikes from a public-domain recording, played through a copy of a vanilla sound effect so it
/// follows the game's volume. Rung on this client only, for warnings at sea.
/// </summary>
public static class ShipBell
{
    private const string ClipName = "shipbell";
    private const string EffectName = "sfx_whitehilt_shipbell";

    private static GameObject effect;

    /// <summary>
    /// Creates the sound effect. Call once the vanilla prefabs are available.
    /// </summary>
    public static void Create()
    {
        if (effect != null || VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            GameObject source = PrefabManager.Instance.GetPrefab("Wolf")?.GetComponent<MonsterAI>()?.m_alertedEffects.m_effectPrefabs
                .Select(entry => entry.m_prefab)
                .FirstOrDefault(prefab => prefab != null && prefab.GetComponentInChildren<ZSFX>(true) != null);
            if (source == null)
            {
                Jotunn.Logger.LogWarning("Ship's bell: no vanilla sound effect to copy, the bell stays silent");
                return;
            }

            AudioClip clip = ForagingAssets.LoadAudio(ClipName);
            effect = PrefabManager.Instance.CreateClonedPrefab(EffectName, source);
            ZSFX sfx = effect.GetComponentInChildren<ZSFX>(true);
            sfx.m_audioClips = new[] { clip };
            sfx.m_minPitch = 1f;
            sfx.m_maxPitch = 1f;
            sfx.m_minVol = 1f;
            sfx.m_maxVol = 1f;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Ship's bell: {ex.Message}");
        }
    }

    /// <summary>
    /// Rings the bell at a position, heard only on this client.
    /// </summary>
    /// <param name="position">Where the bell rings, e.g. the ship.</param>
    public static void Ring(Vector3 position)
    {
        if (effect != null)
        {
            UnityEngine.Object.Instantiate(effect, position, Quaternion.identity);
        }
    }
}
