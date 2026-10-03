using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty.Beasts;

/// <summary>
/// Spawns a beast near the local player when the server allows it: out of sight in the dark, on land or in deep water.
/// </summary>
public static class BeastSpawner
{
    private const int Attempts = 40;
    private const float SeaDepth = 8f;
    private const float ShoreMargin = 0.5f;
    private const float BaseMargin = 10f;

    /// <summary>
    /// Spawns a beast near a player.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="beast">The beast.</param>
    /// <param name="summoned">Whether a horn called this beast.</param>
    /// <returns>True if it was spawned.</returns>
    public static bool Spawn(Player player, BeastDefinition beast, bool summoned = false)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(beast.PrefabName) : null;
        if (prefab == null)
        {
            Jotunn.Logger.LogWarning($"Beast prefab {beast.PrefabName} not found");
            return false;
        }

        if (!TryFindPoint(player.transform.position, beast.Sea, out Vector3 point))
        {
            Jotunn.Logger.LogInfo($"No place for {beast.EnglishName} near {player.GetPlayerName()}");
            return false;
        }

        Vector3 toPlayer = player.transform.position - point;
        toPlayer.y = 0f;
        Quaternion rotation = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;
        GameObject instance = Object.Instantiate(prefab, point, rotation);
        Character character = instance.GetComponent<Character>();
        ZDO zdo = character.m_nview.GetZDO();
        zdo.Set(CreatureStars.BeastKey, true);
        zdo.Set(Items.Summoning.SummoningHornService.SummonedKey, summoned);
        zdo.Set(CreatureStars.HealthKey, DifficultySettings.BeastHealth.Value);
        zdo.Set(CreatureStars.DamageKey, DifficultySettings.BeastDamage.Value);
        character.SetLevel(CreatureStars.MaxLevel);
        BaseAI ai = character.GetBaseAI();
        if (ai != null)
        {
            ai.SetHuntPlayer(true);
        }

        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_beast_near");
        if (GameCamera.instance != null)
        {
            GameCamera.instance.AddShake(player.transform.position, 100f, 1.5f, false);
        }
        Jotunn.Logger.LogInfo($"{beast.EnglishName} came for {player.GetPlayerName()} at {point}");
        return true;
    }

    private static bool TryFindPoint(Vector3 origin, bool sea, out Vector3 point)
    {
        ZoneSystem zones = ZoneSystem.instance;
        float water = zones.m_waterLevel;
        for (int i = 0; i < Attempts; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(DifficultySettings.SpawnDistanceMin.Value, Mathf.Max(DifficultySettings.SpawnDistanceMin.Value, DifficultySettings.SpawnDistanceMax.Value));
            point = origin + new Vector3(direction.x, 0f, direction.y) * distance;

            if (sea)
            {
                if (zones.GetGroundHeight(point) > water - SeaDepth)
                {
                    continue;
                }

                point.y = water;
                return true;
            }

            if (!zones.GetSolidHeight(point, out float height) || height < water + ShoreMargin)
            {
                continue;
            }

            point.y = height + 0.2f;
            if (EffectArea.IsPointInsideArea(point, EffectArea.Type.PlayerBase, BaseMargin) != null)
            {
                continue;
            }

            return true;
        }

        point = Vector3.zero;
        return false;
    }
}
