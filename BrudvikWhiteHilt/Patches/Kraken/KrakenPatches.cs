using BrudvikWhiteHilt.Kraken;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Kraken;

/// <summary>
/// Hooks the Kraken service onto the game object, holds ships the Kraken has caught, scales the damage it does to the crew
/// and the ship, and lets octopuses spawn wherever the ocean fish Fish8 does.
/// </summary>
[HarmonyPatch]
public static class KrakenPatches
{
    private const string OctopusTemplate = "Fish8";
    private const string OctopusSpawnName = "WhiteHilt octopus";

    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void GameStartPostfix(Game __instance)
    {
        __instance.gameObject.AddComponent<KrakenService>();
    }

    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    [HarmonyPostfix]
    private static void ShipFixedUpdatePostfix(Ship __instance, float fixedDeltaTime)
    {
        if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner() || !KrakenBody.Holds(__instance))
        {
            return;
        }

        Rigidbody body = __instance.m_body;
        Vector3 velocity = body.linearVelocity;
        float keep = Mathf.Exp(-2.5f * fixedDeltaTime);
        body.linearVelocity = new Vector3(velocity.x * keep, velocity.y, velocity.z * keep);
        body.angularVelocity *= Mathf.Exp(-1.5f * fixedDeltaTime);

        // Now and then the arms under the hull give the ship a shove.
        if (Random.value < fixedDeltaTime * 0.3f)
        {
            body.AddTorque(__instance.transform.forward * Random.Range(-0.35f, 0.35f), ForceMode.VelocityChange);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    [HarmonyPrefix]
    private static bool CharacterDamagePrefix(Character __instance, HitData hit)
    {
        return !(__instance is Player) || Scale(hit, KrakenSettings.CrewDamagePercent.Value, KrakenSettings.CrewDamagePerExtraPlayer.Value);
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
    [HarmonyPrefix]
    private static bool WearNTearDamagePrefix(HitData hit)
    {
        return Scale(hit, KrakenSettings.ShipDamagePercent.Value, KrakenSettings.ShipDamagePerExtraPlayer.Value);
    }

    [HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.Awake))]
    [HarmonyPostfix]
    private static void SpawnSystemAwakePostfix(SpawnSystem __instance)
    {
        GameObject octopus = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(KrakenRegistry.OctopusName) : null;
        if (octopus == null)
        {
            return;
        }

        bool found = false;
        foreach (SpawnSystemList list in __instance.m_spawnLists)
        {
            SpawnSystem.SpawnData data = list.m_spawners.FirstOrDefault(each => each.m_name == OctopusSpawnName);
            if (data == null)
            {
                SpawnSystem.SpawnData template = list.m_spawners.FirstOrDefault(each => each.m_prefab != null && each.m_prefab.name == OctopusTemplate);
                if (template == null)
                {
                    continue;
                }

                data = template.Clone();
                data.m_name = OctopusSpawnName;
                data.m_prefab = octopus;
                list.m_spawners.Add(data);
                Jotunn.Logger.LogInfo($"Octopus spawns added next to {OctopusTemplate} ({data.m_biome}, altitude {data.m_minAltitude}..{data.m_maxAltitude})");
            }

            found = true;
            Configure(data);
        }

        // Without a vanilla fish entry to copy, the octopus gets its own: deep ocean, rising from the seabed like the fish do.
        if (!found && __instance.m_spawnLists.Count > 0)
        {
            SpawnSystem.SpawnData data = new()
            {
                m_name = OctopusSpawnName,
                m_prefab = octopus,
                m_biome = Heightmap.Biome.Ocean,
                m_spawnInterval = 90f,
                m_spawnDistance = 20f,
                m_maxAltitude = -8f,
                m_groundOffset = 1f
            };
            Configure(data);
            __instance.m_spawnLists[0].m_spawners.Add(data);
            Jotunn.Logger.LogInfo($"Octopus spawns added on their own (no {OctopusTemplate} spawn found)");
        }
    }

    private static void Configure(SpawnSystem.SpawnData data)
    {
        data.m_enabled = KrakenSettings.OctopusEnabled.Value;
        data.m_maxSpawned = KrakenSettings.OctopusMaxSpawned.Value;
        data.m_spawnChance = KrakenSettings.OctopusSpawnChance.Value;
    }

    // Returns false when the hit is scaled to nothing, so it is skipped.
    private static float CrewDamageMultiplier(int playersAboard, float bonusPercent)
    {
        return 1f + Mathf.Max(0, playersAboard - 1) * bonusPercent / 100f;
    }

    private static bool Scale(HitData hit, float percent, float bonusPercent)
    {
        Character attacker = hit.GetAttacker();
        if (attacker == null || (attacker.GetComponent<KrakenBody>() == null && attacker.GetComponent<KrakenTentacle>() == null))
        {
            return true;
        }

        if (percent <= 0f)
        {
            return false;
        }

        ZNetView nview = attacker.m_nview;
        int crew = nview != null && nview.IsValid() ? nview.GetZDO().GetInt(KrakenBody.CrewKey, 1) : 1;
        hit.ApplyModifier(percent / 100f * CrewDamageMultiplier(crew, bonusPercent));
        return true;
    }
}
