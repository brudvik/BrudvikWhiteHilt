using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// The Foraging skill. It rises with every wild berry, mushroom and herb picked, and gives wild picks stars, extra
/// yield, best picking times and sweep picking.
/// </summary>
public static class ForagingSkill
{
    private const string Identifier = "com.jotunn.BrudvikWhiteHilt.foraging";
    private const string NameKey = "whitehilt_skill_foraging";

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
        Translations.AddEnglishNameAndDescription(NameKey, "Foraging",
            "Picking wild berries, mushrooms and herbs. Gives more from each pick, stars on what you pick, and later sweep picking.");
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
    /// Gives the skill the mushroom's icon. Must happen before the first player spawns.
    /// </summary>
    public static void SetIconFromMushroom()
    {
        Sprite icon = PrefabManager.Instance.GetPrefab("Mushroom")?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        if (config != null && config.Icon == null && icon != null)
        {
            config.Icon = icon;
        }
    }
}
