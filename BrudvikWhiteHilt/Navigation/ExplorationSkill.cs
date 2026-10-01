using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Navigation;
using BrudvikWhiteHilt.Pieces.Navigation;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Navigation;

/// <summary>
/// The Exploration skill. It rises with every patch of the map a player uncovers, and makes the Navigator's Table and the
/// Pathfinder's Amulet reveal a wider circle around the player.
/// </summary>
public static class ExplorationSkill
{
    /// <summary>
    /// Key in the player's ZDO with the skill level, so other players see the right number of gems on the amulet.
    /// </summary>
    public const string LevelZdoKey = "whitehilt_exploration_level";

    private static float SkillPerSquareMetre => NavigationSettings.SkillPerSquareMetre.Value;

    private static float TableBonus => NavigationSettings.TableBonus.Value;

    private static float AmuletBonus => NavigationSettings.AmuletBonus.Value;

    private static float BonusAtLevelZero => NavigationSettings.BonusAtLevelZero.Value;

    private const string Identifier = "com.jotunn.BrudvikWhiteHilt.exploration";
    private const string NameKey = "whitehilt_skill_exploration";

    private static SkillConfig config;

    /// <summary>
    /// The skill type, valid after <see cref="Register"/>.
    /// </summary>
    public static Skills.SkillType Type { get; private set; }

    /// <summary>
    /// Registers the skill and its English text. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        int revealLevel = NavigationSettings.SharedMapRevealLevel?.Value ?? 50;
        Translations.AddEnglishNameAndDescription(NameKey, "Exploration", $"Uncovering the map. Widens what the Navigator's Table and the Pathfinder's Amulet reveal, and from level {revealLevel} shows map shared by others like your own.");
        config = new SkillConfig
        {
            Identifier = Identifier,
            Name = Translations.Token(NameKey),
            Description = Translations.Token($"{NameKey}_description"),
            IncreaseStep = 1f
        };
        Type = SkillManager.Instance.AddSkill(config);
    }

    /// <summary>
    /// Gives the skill the map table's icon. Jotunn builds the skill definition when a player is created, so this only
    /// has to happen before the first player spawns.
    /// </summary>
    public static void SetIconFromMapTable()
    {
        Sprite icon = PrefabManager.Instance.GetPrefab("piece_cartographytable")?.GetComponent<Piece>()?.m_icon;
        if (config != null && config.Icon == null && icon != null)
        {
            config.Icon = icon;
        }
    }

    /// <summary>
    /// Skill level of a player: its own skills for the local player, the level it shares in its ZDO for others.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The level, 0 to 100.</returns>
    public static int GetLevel(Player player)
    {
        if (player == null)
        {
            return 0;
        }

        if (player == Player.m_localPlayer)
        {
            return Mathf.FloorToInt(player.GetSkills().GetSkillLevel(Type));
        }

        ZDO zdo = player.m_nview != null && player.m_nview.IsValid() ? player.m_nview.GetZDO() : null;
        return zdo?.GetInt(LevelZdoKey) ?? 0;
    }

    /// <summary>
    /// How far the map is uncovered around the player, from the vanilla radius, the gear and the skill.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="baseRadius">Vanilla radius in metres.</param>
    /// <returns>The radius in metres.</returns>
    public static float GetExploreRadius(Player player, float baseRadius)
    {
        if (!NavigationSettings.ExploreRadiusBonus.Value)
        {
            return baseRadius;
        }

        float bonus = 0f;
        if (ShipChartTable.IsAboardWithTable(player))
        {
            bonus = TableBonus;
        }

        if (PathfinderAmulet.IsWorn(player))
        {
            bonus = Mathf.Max(bonus, AmuletBonus);
        }

        if (bonus <= 0f)
        {
            return baseRadius;
        }

        return baseRadius * (1f + bonus * Mathf.Lerp(BonusAtLevelZero, 1f, player.GetSkillFactor(Type)));
    }

    /// <summary>
    /// Raises the skill for newly uncovered map, fills the amulet's adrenaline and shares the level with other players.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="squareMetres">Area of the newly uncovered map.</param>
    public static void OnExplored(Player player, float squareMetres)
    {
        if (squareMetres > 0f)
        {
            player.RaiseSkill(Type, squareMetres * SkillPerSquareMetre);
            PathfinderAmulet.OnExplored(player, squareMetres);
        }

        ZDO zdo = player.m_nview != null && player.m_nview.IsValid() && player.m_nview.IsOwner() ? player.m_nview.GetZDO() : null;
        int level = GetLevel(player);
        if (zdo != null && zdo.GetInt(LevelZdoKey) != level)
        {
            zdo.Set(LevelZdoKey, level);
        }
    }
}
