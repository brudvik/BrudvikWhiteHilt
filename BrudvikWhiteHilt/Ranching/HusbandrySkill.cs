using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Ranching;

/// <summary>
/// The Animal Husbandry skill. It rises while players tame, feed and breed animals, and makes nearby animals
/// tame faster, stay fed longer and breed faster and in larger herds.
/// </summary>
public static class HusbandrySkill
{
    /// <summary>
    /// Experience for every taming tick (every 3 seconds). A full taming gives about 120 at the default.
    /// </summary>
    public static float TamingTickExperience => RanchingSettings.TamingTickExperience.Value;

    /// <summary>
    /// Experience when an animal becomes tame.
    /// </summary>
    public static float TamedExperience => RanchingSettings.TamedExperience.Value;

    /// <summary>
    /// Experience when an animal eats.
    /// </summary>
    public static float FeedingExperience => RanchingSettings.FeedingExperience.Value;

    /// <summary>
    /// Experience when an animal is born or an egg is laid.
    /// </summary>
    public static float BirthExperience => RanchingSettings.BirthExperience.Value;

    /// <summary>
    /// How far away, in metres, players get experience and lend their skill to an animal.
    /// </summary>
    public static float Range => RanchingSettings.HusbandryRange.Value;

    private const string LevelZdoKey = "whitehilt_husbandry_level";
    private const string ExperienceRpc = "WhiteHilt_HusbandryXp";
    private const string Identifier = "com.jotunn.BrudvikWhiteHilt.husbandry";
    private const string NameKey = "whitehilt_skill_husbandry";

    private static readonly List<Player> nearbyPlayers = new();
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
        Translations.AddEnglishNameAndDescription(NameKey, "Animal Husbandry",
            "Taming, feeding and breeding animals. Animals near you tame faster, stay fed longer and breed faster and in larger herds.");
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
    /// Gives the skill the boar trophy's icon. Must happen before the first player spawns.
    /// </summary>
    public static void SetIconFromBoarTrophy()
    {
        Sprite icon = PrefabManager.Instance.GetPrefab("TrophyBoar")?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        if (config != null && config.Icon == null && icon != null)
        {
            config.Icon = icon;
        }
    }

    /// <summary>
    /// Lets a player receive experience sent by the machine that owns an animal.
    /// </summary>
    /// <param name="player">A player that just woke up.</param>
    public static void RegisterRpc(Player player)
    {
        if (player.m_nview != null)
        {
            player.m_nview.Register<float>(ExperienceRpc, (_, amount) => OnExperience(player, amount));
        }
    }

    /// <summary>
    /// Shares the local player's level in its ZDO, so machines that own animals can read it.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void ShareLevel(Player player)
    {
        if (player == null || player != Player.m_localPlayer || player.m_nview == null || !player.m_nview.IsValid() || !player.m_nview.IsOwner())
        {
            return;
        }

        int level = Mathf.FloorToInt(player.GetSkills().GetSkillLevel(Type));
        ZDO zdo = player.m_nview.GetZDO();
        if (zdo.GetInt(LevelZdoKey) != level)
        {
            zdo.Set(LevelZdoKey, level);
        }
    }

    /// <summary>
    /// Gives experience to every player within <see cref="Range"/> of an animal.
    /// </summary>
    /// <param name="position">Position of the animal.</param>
    /// <param name="amount">Experience.</param>
    public static void GiveExperience(Vector3 position, float amount)
    {
        nearbyPlayers.Clear();
        Player.GetPlayersInRange(position, Range, nearbyPlayers);
        foreach (Player player in nearbyPlayers.Where(player => player.m_nview != null && player.m_nview.IsValid()))
        {
            player.m_nview.InvokeRPC(ExperienceRpc, amount);
        }
    }

    /// <summary>
    /// The highest skill of the players within <see cref="Range"/>, from 0 to 1.
    /// </summary>
    /// <param name="position">Position of the animal.</param>
    /// <returns>The skill factor.</returns>
    public static float GetFactorNear(Vector3 position)
    {
        if (!RanchingSettings.HusbandryEffects.Value)
        {
            return 0f;
        }

        nearbyPlayers.Clear();
        Player.GetPlayersInRange(position, Range, nearbyPlayers);
        return nearbyPlayers.Count == 0 ? 0f : nearbyPlayers.Max(GetFactor);
    }

    /// <summary>
    /// How much faster taming goes at a skill factor.
    /// </summary>
    /// <param name="factor">Skill factor, 0 to 1.</param>
    /// <returns>The speed multiplier.</returns>
    public static float TamingSpeed(float factor)
    {
        return 1f / (1f - RanchingSettings.TamingTimeReduction.Value * factor);
    }

    /// <summary>
    /// How much longer food lasts at a skill factor.
    /// </summary>
    /// <param name="factor">Skill factor, 0 to 1.</param>
    /// <returns>The duration multiplier.</returns>
    public static float FedDuration(float factor)
    {
        return 1f + RanchingSettings.FedDurationBonus.Value * factor;
    }

    /// <summary>
    /// How long a pregnancy lasts at a skill factor, as a share of vanilla.
    /// </summary>
    /// <param name="factor">Skill factor, 0 to 1.</param>
    /// <returns>The duration multiplier.</returns>
    public static float PregnancyDuration(float factor)
    {
        return 1f - RanchingSettings.PregnancyReduction.Value * factor;
    }

    /// <summary>
    /// How many more animals a herd may hold at a skill factor.
    /// </summary>
    /// <param name="factor">Skill factor, 0 to 1.</param>
    /// <returns>Extra animals.</returns>
    public static int ExtraHerd(float factor)
    {
        return Mathf.FloorToInt(RanchingSettings.ExtraHerdSize.Value * factor);
    }

    private static float GetFactor(Player player)
    {
        if (player == Player.m_localPlayer)
        {
            return player.GetSkillFactor(Type);
        }

        ZDO zdo = player.m_nview != null && player.m_nview.IsValid() ? player.m_nview.GetZDO() : null;
        return Mathf.Clamp01((zdo?.GetInt(LevelZdoKey) ?? 0) / 100f);
    }

    private static void OnExperience(Player player, float amount)
    {
        if (player != Player.m_localPlayer || amount <= 0f)
        {
            return;
        }

        player.RaiseSkill(Type, amount);
        ShareLevel(player);
    }
}
