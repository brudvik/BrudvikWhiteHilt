using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// Shares the local player's levels in the skills other machines need, in the player's ZDO, so the machine that owns
/// a plant or a cooking station can use the skill of the player who picks or cooks.
/// </summary>
public static class SkillLevels
{
    private const string KeyPrefix = "whitehilt_skill_";
    private const float ShareInterval = 2f;

    private static readonly List<Player> nearby = new();
    private static float nextShare;

    /// <summary>
    /// Writes the local player's shared levels to its ZDO now and then. Call every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Share(Player player)
    {
        if (Time.time < nextShare || player.m_nview == null || !player.m_nview.IsValid() || !player.m_nview.IsOwner())
        {
            return;
        }

        nextShare = Time.time + ShareInterval;
        ZDO zdo = player.m_nview.GetZDO();
        foreach (Skills.SkillType type in SharedTypes())
        {
            int level = Mathf.FloorToInt(player.GetSkillLevel(type));
            string key = KeyPrefix + (int)type;
            if (zdo.GetInt(key) != level)
            {
                zdo.Set(key, level);
            }
        }
    }

    /// <summary>
    /// A player's level in a skill: its own skills for the local player, the shared level for others.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The skill.</param>
    /// <returns>The level, 0 to 100.</returns>
    public static float Get(Player player, Skills.SkillType type)
    {
        if (player == null)
        {
            return 0f;
        }

        if (player == Player.m_localPlayer)
        {
            return player.GetSkillLevel(type);
        }

        ZDO zdo = player.m_nview != null && player.m_nview.IsValid() ? player.m_nview.GetZDO() : null;
        return zdo?.GetInt(KeyPrefix + (int)type) ?? 0;
    }

    /// <summary>
    /// The player whose character is owned by a peer, e.g. the sender of an RPC.
    /// </summary>
    /// <param name="peer">The peer's session id.</param>
    /// <returns>The player, or null.</returns>
    public static Player FindPlayer(long peer)
    {
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != null && player.m_nview != null && player.m_nview.IsValid() && player.m_nview.GetZDO().GetOwner() == peer)
            {
                return player;
            }
        }

        return null;
    }

    /// <summary>
    /// The highest level in a skill among the players within a range.
    /// </summary>
    /// <param name="position">Centre.</param>
    /// <param name="range">Range in metres.</param>
    /// <param name="type">The skill.</param>
    /// <returns>The level, 0 to 100.</returns>
    public static float BestNear(Vector3 position, float range, Skills.SkillType type)
    {
        nearby.Clear();
        Player.GetPlayersInRange(position, range, nearby);
        float best = 0f;
        foreach (Player player in nearby)
        {
            best = Mathf.Max(best, Get(player, type));
        }

        return best;
    }

    private static IEnumerable<Skills.SkillType> SharedTypes()
    {
        yield return Skills.SkillType.Cooking;
        yield return Skills.SkillType.Farming;
        yield return ForagingSkill.Type;
    }
}
