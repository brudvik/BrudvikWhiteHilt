using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Ranching;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// The skill book page: each skill's tooltip in the skills dialog lists what it gives at the player's level and its
/// milestones, reached ones in green.
/// </summary>
public static class SkillBook
{
    private const string Heading = "#FFB75C";
    private const string Reached = "#8FD18F";
    private const string Locked = "#8A8A8A";

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_book_now", "At your level");
        Translations.AddEnglish("whitehilt_book_milestones", "Milestones");
        Translations.AddEnglish("whitehilt_book_star_chance", "Chance of a star: {0}%, up to {1}");
        Translations.AddEnglish("whitehilt_book_foraging", "One more from a wild pick: {0}%");
        Translations.AddEnglish("whitehilt_book_cooking", "Cooking stations within {1} m work {0}% faster");
        Translations.AddEnglish("whitehilt_book_woodcutting", "Extra wood from a log: {0}%. Bird's nest in a felled tree: {1}%");
        Translations.AddEnglish("whitehilt_book_pickaxes", "Extra ore: {0}%. Finds in broken rock: {1}%");
        Translations.AddEnglish("whitehilt_book_blocking", "+{0} health, {1}% less damage, blocking costs {2}% less stamina");
        Translations.AddEnglish("whitehilt_book_fishing", "Snag: {0}%. Legendary fish: {1}%");
        Translations.AddEnglish("whitehilt_book_husbandry", "Taming {0}% faster, fed {1}% longer, pregnancy {2}% shorter, herd +{3}");
        Translations.AddEnglish("whitehilt_book_twins", "Twins: {0}%. Stronger young: {1}%");
    }

    /// <summary>
    /// Adds the levels and milestones to every skill's tooltip.
    /// </summary>
    /// <param name="dialog">The skills dialog, just set up.</param>
    /// <param name="player">The player.</param>
    public static void Fill(SkillsDialog dialog, Player player)
    {
        List<Skills.Skill> skills = player.GetSkills().GetSkillList();
        for (int i = 0; i < skills.Count && i < dialog.m_elements.Count; i++)
        {
            UITooltip tooltip = dialog.m_elements[i].GetComponentInChildren<UITooltip>();
            Skills.SkillType type = skills[i].m_info.m_skill;
            string extra = Page(player, type);
            if (tooltip != null && extra.Length > 0)
            {
                tooltip.m_text = Localization.instance.Localize(skills[i].m_info.m_description) + extra;
            }
        }
    }

    // A skill's page in the book: what the skill gives now, and its milestones, reached ones in a different colour.
    private static string Page(Player player, Skills.SkillType type)
    {
        float level = player.GetSkillLevel(type);
        List<string> now = Now(player, type, level).Where(line => !string.IsNullOrEmpty(line)).ToList();
        List<Perk> perks = Perks.For(type).Where(perk => perk.Enabled).ToList();
        if (now.Count == 0 && perks.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder text = new();
        if (now.Count > 0)
        {
            text.Append($"\n\n<color={Heading}>{Localize("$whitehilt_book_now")}</color>");
            foreach (string line in now)
            {
                text.Append("\n- ").Append(line);
            }
        }

        if (perks.Count > 0)
        {
            text.Append($"\n\n<color={Heading}>{Localize("$whitehilt_book_milestones")}</color>");
            foreach (Perk perk in perks)
            {
                string color = level >= perk.Level ? Reached : Locked;
                text.Append($"\n<color={color}>{perk.Level} {Localize(Translations.Token(perk.Key))}</color>: {perk.LocalizedDescription()}");
            }
        }

        return text.ToString();
    }

    // What a skill gives at its current level, one line per effect.
    private static IEnumerable<string> Now(Player player, Skills.SkillType type, float level)
    {
        if (type == ForagingSkill.Type && MasterySettings.Foraging.Value)
        {
            yield return Format("$whitehilt_book_foraging", Percent(Perks.ForagingExtraYield(level)));
            yield return StarChance(level, Perks.KeenEye, Perks.SeasonSense, Perks.ForagersBounty);
        }
        else if (type == Skills.SkillType.Cooking && MasterySettings.Cooking.Value)
        {
            yield return Format("$whitehilt_book_cooking", Percent(Perks.KitchenSpeed(level) - 1f), MasterySettings.CookRange.Value.ToString("0.#"));
            yield return StarChance(level, Perks.FineCooking, Perks.WatchfulCook, Perks.MasterChef);
        }
        else if (type == Skills.SkillType.Farming && MasterySettings.Farming.Value)
        {
            yield return StarChance(level, Perks.StarredCrops, Perks.GreenThumb, Perks.MasterFarmer);
        }
        else if (type == Skills.SkillType.WoodCutting && MasterySettings.Woodcutting.Value)
        {
            yield return Format("$whitehilt_book_woodcutting", Percent(Perks.CleanSplit(level)), Percent(Perks.BirdNest(level)));
        }
        else if (type == Skills.SkillType.Pickaxes && MasterySettings.Mining.Value)
        {
            yield return Format("$whitehilt_book_pickaxes", Percent(Perks.ExtraOre(level)), Percent(Perks.MiningFind(level)));
        }
        else if (type == Skills.SkillType.Blocking && MasterySettings.Blocking.Value)
        {
            yield return Format("$whitehilt_book_blocking", Perks.ExtraHealth(level).ToString("0"), Percent(Perks.DamageReduction(level)), Percent(Perks.BlockStaminaSaving(level)));
        }
        else if (type == Skills.SkillType.Fishing && MasterySettings.Fishing.Value)
        {
            yield return Format("$whitehilt_book_fishing", Percent(Perks.SnagChance(level)), Percent(Perks.LegendaryChance(level)));
            List<string> log = Angling.LogLines(player).ToList();
            if (log.Count > 0)
            {
                yield return $"<color={Heading}>{Localize("$whitehilt_catch_log")}</color>\n  " + string.Join("\n  ", log);
            }
        }
        else if (type == HusbandrySkill.Type)
        {
            float factor = Perks.Factor(level);
            yield return Format("$whitehilt_book_husbandry", Percent(HusbandrySkill.TamingSpeed(factor) - 1f), Percent(HusbandrySkill.FedDuration(factor) - 1f),
                Percent(1f - HusbandrySkill.PregnancyDuration(factor)), HusbandrySkill.ExtraHerd(factor).ToString());
            if (MasterySettings.Husbandry.Value)
            {
                yield return Format("$whitehilt_book_twins", Percent(Perks.TwinChance(level)), Percent(Perks.StrongYoungChance(level)));
            }
        }
    }

    private static string StarChance(float level, Perk first, Perk second, Perk last)
    {
        int max = Stars.MaxAt(level, first, second, last);
        return max == 0 ? null : Format("$whitehilt_book_star_chance", Percent(MasterySettings.FirstStarChance.Value * Perks.Factor(level)), Stars.Text(max));
    }

    private static string Percent(float share)
    {
        return Perks.Percent(share);
    }

    private static string Format(string token, params string[] words)
    {
        return Perks.Text(token, words);
    }

    private static string Localize(string text)
    {
        return Localization.instance.Localize(text);
    }
}
