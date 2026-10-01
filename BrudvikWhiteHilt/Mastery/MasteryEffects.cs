using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// The status effects behind the Blocking and Pickaxes milestones.
/// </summary>
public static class MasteryEffects
{
    /// <summary>Name of the hidden effect every player carries.</summary>
    public const string GuardName = "SE_WhiteHiltMastery";

    /// <summary>Name of the Riposte effect.</summary>
    public const string RiposteName = "SE_WhiteHiltRiposte";

    /// <summary>Name of the Shield Wall effect.</summary>
    public const string ShieldWallName = "SE_WhiteHiltShieldWall";

    /// <summary>Name of the Last Stand effect, which is also its cooldown.</summary>
    public const string LastStandName = "SE_WhiteHiltLastStand";

    /// <summary>Seconds of the Last Stand during which nothing hurts.</summary>
    public static float LastStandGrace => MasterySettings.LastStandGraceSeconds.Value;

    private static bool registered;

    /// <summary>Hash of the hidden effect.</summary>
    public static int GuardHash => GuardName.GetStableHashCode();

    /// <summary>Hash of the Riposte effect.</summary>
    public static int RiposteHash => RiposteName.GetStableHashCode();

    /// <summary>Hash of the Shield Wall effect.</summary>
    public static int ShieldWallHash => ShieldWallName.GetStableHashCode();

    /// <summary>Hash of the Last Stand effect.</summary>
    public static int LastStandHash => LastStandName.GetStableHashCode();

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_se_riposte", "Riposte");
        Translations.AddEnglish("whitehilt_se_riposte_tooltip", "Your next hit does {0}% more damage.");
        Translations.AddEnglish("whitehilt_se_shieldwall", "Shield Wall");
        Translations.AddEnglish("whitehilt_se_shieldwall_tooltip", "{0}% less damage behind a raised tower shield.");
        Translations.AddEnglish("whitehilt_se_laststand", "Last Stand");
        Translations.AddEnglish("whitehilt_se_laststand_tooltip", "You held on. Ready again when this ends.");
        Translations.AddEnglish("msg_whitehilt_laststand", "Last Stand!");
        Translations.AddEnglish("msg_whitehilt_cleanstrike", "Clean strike!");
    }

    /// <summary>
    /// Adds the effects to the ObjectDB. Call once the vanilla prefabs are available.
    /// </summary>
    public static void Register()
    {
        if (registered)
        {
            return;
        }

        registered = true;
        Add(Create<GuardEffect>(GuardName, string.Empty, string.Empty, null, 0f));
        Add(Create<RiposteEffect>(RiposteName, "whitehilt_se_riposte", "whitehilt_se_riposte_tooltip", IconOf("ShieldBanded"), MasterySettings.RiposteSeconds.Value));
        Add(Create<ShieldWallEffect>(ShieldWallName, "whitehilt_se_shieldwall", "whitehilt_se_shieldwall_tooltip", IconOf("ShieldIronTower"), 1.5f));
        Add(Create<SE_Stats>(LastStandName, "whitehilt_se_laststand", "whitehilt_se_laststand_tooltip", IconOf("TrophyEikthyr"), MasterySettings.LastStandCooldownMinutes.Value * 60f));
    }

    /// <summary>
    /// True if the player has the Last Stand effect and is still within its grace time.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True while nothing should hurt.</returns>
    public static bool InLastStandGrace(Player player)
    {
        StatusEffect effect = player.GetSEMan().GetStatusEffect(LastStandHash);
        return effect != null && effect.m_time < LastStandGrace;
    }

    /// <summary>
    /// Adds an effect with the configured duration, which may have changed since the effect was registered.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="hash">Hash of the effect.</param>
    /// <param name="seconds">Duration.</param>
    public static void Start(Player player, int hash, float seconds)
    {
        StatusEffect effect = player.GetSEMan().AddStatusEffect(hash, resetTime: true);
        if (effect != null)
        {
            effect.m_ttl = seconds;
        }
    }

    private static T Create<T>(string name, string nameKey, string tooltipKey, Sprite icon, float ttl) where T : StatusEffect
    {
        T effect = ScriptableObject.CreateInstance<T>();
        effect.name = name;
        effect.m_name = nameKey.Length > 0 ? Translations.Token(nameKey) : string.Empty;
        effect.m_tooltip = tooltipKey.Length > 0 ? Translations.Token(tooltipKey) : string.Empty;
        effect.m_icon = icon;
        effect.m_ttl = ttl;
        return effect;
    }

    private static void Add(StatusEffect effect)
    {
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
    }

    private static Sprite IconOf(string itemPrefab)
    {
        ItemDrop drop = PrefabManager.Instance.GetPrefab(itemPrefab)?.GetComponent<ItemDrop>();
        return drop != null ? drop.m_itemData.GetIcon() : null;
    }

    /// <summary>
    /// Hidden effect on every player: the Blocking bonuses and the Clean Strike.
    /// </summary>
    public class GuardEffect : StatusEffect
    {
        /// <summary>
        /// Blocking costs less stamina with skill.
        /// </summary>
        /// <param name="baseStaminaUse">Vanilla stamina use.</param>
        /// <param name="staminaUse">Stamina use, changed.</param>
        public override void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
        {
            if (staminaUse > 0f)
            {
                staminaUse *= 1f - Perks.BlockStaminaSaving(Level(Skills.SkillType.Blocking));
            }
        }

        /// <summary>
        /// Takes less damage with Blocking skill.
        /// </summary>
        /// <param name="hit">The hit.</param>
        /// <param name="attacker">Who hit.</param>
        public override void OnDamaged(HitData hit, Character attacker)
        {
            float reduction = Perks.DamageReduction(Level(Skills.SkillType.Blocking));
            if (reduction > 0f)
            {
                hit.ApplyModifier(1f - reduction);
            }
        }

        /// <summary>
        /// Clean strikes and the ore echo.
        /// </summary>
        /// <param name="skill">Skill of the weapon.</param>
        /// <param name="hitData">The hit.</param>
        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            if (skill != Skills.SkillType.Pickaxes || m_character is not Player player)
            {
                return;
            }

            if (Perks.CleanStrike.Has(player) && Random.value < MasterySettings.CleanStrikeChance.Value && hitData.m_damage.m_pickaxe > 0f)
            {
                hitData.m_damage.m_pickaxe *= MasterySettings.CleanStrikeMultiplier.Value;
                DamageText.instance.ShowText(DamageText.TextType.Bonus, player.transform.position + Vector3.up * 2f,
                    Localization.instance.Localize("$msg_whitehilt_cleanstrike"), player: true);
            }

            OreEcho.TryPing(player);
        }

        private float Level(Skills.SkillType skill)
        {
            return m_character != null ? m_character.GetSkillLevel(skill) : 0f;
        }
    }

    /// <summary>
    /// The next melee hit after a perfect parry does more damage.
    /// </summary>
    public class RiposteEffect : SE_Stats
    {
        private bool used;

        /// <summary>
        /// The tooltip with the configured bonus.
        /// </summary>
        /// <returns>The tooltip.</returns>
        public override string GetTooltipString()
        {
            return Perks.Text(m_tooltip, Perks.Percent(MasterySettings.RiposteBonus.Value));
        }

        /// <summary>
        /// Strengthens the first melee hit and ends the effect.
        /// </summary>
        /// <param name="skill">Skill of the weapon.</param>
        /// <param name="hitData">The hit.</param>
        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            base.ModifyAttack(skill, ref hitData);
            if (used || !IsMelee(skill))
            {
                return;
            }

            used = true;
            hitData.m_damage.Modify(1f + MasterySettings.RiposteBonus.Value);
            m_time = m_ttl + 1f;
        }

        private static bool IsMelee(Skills.SkillType skill)
        {
            return skill is Skills.SkillType.Swords or Skills.SkillType.Knives or Skills.SkillType.Clubs or Skills.SkillType.Polearms
                or Skills.SkillType.Spears or Skills.SkillType.Axes or Skills.SkillType.Unarmed;
        }
    }

    /// <summary>
    /// Less damage while a tower shield is raised nearby.
    /// </summary>
    public class ShieldWallEffect : SE_Stats
    {
        /// <summary>
        /// The tooltip with the configured reduction.
        /// </summary>
        /// <returns>The tooltip.</returns>
        public override string GetTooltipString()
        {
            return Perks.Text(m_tooltip, Perks.Percent(MasterySettings.ShieldWallReduction.Value));
        }

        /// <summary>
        /// Takes less damage.
        /// </summary>
        /// <param name="hit">The hit.</param>
        /// <param name="attacker">Who hit.</param>
        public override void OnDamaged(HitData hit, Character attacker)
        {
            base.OnDamaged(hit, attacker);
            hit.ApplyModifier(1f - MasterySettings.ShieldWallReduction.Value);
        }
    }
}
