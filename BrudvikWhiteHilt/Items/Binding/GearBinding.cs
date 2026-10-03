using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrudvikWhiteHilt.Items.Binding;

/// <summary>
/// Black beast trophies bound to White Hilt weapons and shields at the Binding Stone, and runes etched into bound
/// weapons at the Rune Etching Table. Both are kept in the item's own data, so they stay with the item wherever it goes.
/// </summary>
public static class GearBinding
{
    /// <summary>Item data key of the bound beast.</summary>
    public const string BoundKey = "whitehilt_bound";

    /// <summary>Item data key of the etched infusion.</summary>
    public const string InfusionKey = "whitehilt_infusion";

    private static readonly Dictionary<string, bool> shieldBySharedName = new();
    private static string beastsParsedFrom;
    private static HashSet<string> beastNames = new();

    /// <summary>
    /// Registers the English texts. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_bound_weapon", "Bound with the {0}: +{1}% damage");
        Translations.AddEnglish("whitehilt_bound_shield", "Bound with the {0}: +{1}% block power");
        Translations.AddEnglish("whitehilt_infused", "Etched rune: {0}");
        Translations.AddEnglish("whitehilt_infused_web", "Etched rune: {0}, every hit webs the target");
        Translations.AddEnglish("whitehilt_infused_deep", "Etched rune: {0}, {1}% of the damage dealt comes back as health");
        Translations.AddEnglish("whitehilt_infused_wolfsbane", "Etched rune: {0}, the poison is {1} times as strong against beasts");
        Infusion.RegisterTranslations();
    }

    /// <summary>
    /// Lets a White Hilt weapon or shield be bound.
    /// </summary>
    /// <param name="sharedName">The item's name token.</param>
    /// <param name="shield">True for a shield.</param>
    public static void Register(string sharedName, bool shield)
    {
        shieldBySharedName[sharedName] = shield;
    }

    /// <summary>
    /// Whether an item is White Hilt gear that can be bound.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for a White Hilt weapon or shield.</returns>
    public static bool CanBind(ItemDrop.ItemData item)
    {
        return item?.m_shared != null && shieldBySharedName.ContainsKey(item.m_shared.m_name);
    }

    /// <summary>
    /// Whether an item is a White Hilt weapon that can be etched, i.e. not a shield.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for a White Hilt weapon.</returns>
    public static bool CanInfuse(ItemDrop.ItemData item)
    {
        return item?.m_shared != null && shieldBySharedName.TryGetValue(item.m_shared.m_name, out bool shield) && !shield;
    }

    /// <summary>
    /// The beast whose trophy an item is.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The beast, or null for anything but a black trophy.</returns>
    public static BeastDefinition BeastOfTrophy(ItemDrop.ItemData item)
    {
        string prefab = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
        return prefab == null ? null : BeastDefinition.All.FirstOrDefault(beast => beast.TrophyName == prefab);
    }

    /// <summary>
    /// The beast whose trophy is bound to an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The beast, or null if none is bound.</returns>
    public static BeastDefinition GetBound(ItemDrop.ItemData item)
    {
        if (item?.m_customData == null || !item.m_customData.TryGetValue(BoundKey, out string key))
        {
            return null;
        }

        return BeastDefinition.All.FirstOrDefault(beast => beast.Key == key);
    }

    /// <summary>
    /// The rune etched into an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The infusion, or null if none is etched.</returns>
    public static Infusion GetInfusion(ItemDrop.ItemData item)
    {
        if (item?.m_customData == null || !item.m_customData.TryGetValue(InfusionKey, out string key)
            || !Enum.TryParse(key, out InfusionKind kind))
        {
            return null;
        }

        return Infusion.Get(kind);
    }

    /// <summary>
    /// Binds a beast's trophy to an item, in place of any trophy bound before.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="beast">The beast.</param>
    public static void Bind(ItemDrop.ItemData item, BeastDefinition beast)
    {
        item.m_customData[BoundKey] = beast.Key;
    }

    /// <summary>
    /// Etches a rune into an item, in place of any rune etched before.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="infusion">The infusion.</param>
    public static void Infuse(ItemDrop.ItemData item, Infusion infusion)
    {
        item.m_customData[InfusionKey] = infusion.Kind.ToString();
    }

    /// <summary>
    /// Adds the bound trophy's bonus and the etched rune's damage to a bound weapon's damage.
    /// </summary>
    /// <param name="item">The weapon.</param>
    /// <param name="damages">The damage, changed in place.</param>
    public static void ModifyDamage(ItemDrop.ItemData item, ref HitData.DamageTypes damages)
    {
        BeastDefinition beast = GetBound(item);
        if (beast == null)
        {
            return;
        }

        float factor = 1f + BindingSettings.Bonus(beast);
        damages.m_damage *= factor;
        damages.m_blunt *= factor;
        damages.m_slash *= factor;
        damages.m_pierce *= factor;
        damages.m_fire *= factor;
        damages.m_frost *= factor;
        damages.m_lightning *= factor;
        damages.m_poison *= factor;
        damages.m_spirit *= factor;

        GetInfusion(item)?.AddDamage(ref damages, BindingSettings.InfusionStrength(beast) * BaseDamage(item.m_shared.m_damages));
    }

    /// <summary>
    /// Adds the bound trophy's bonus to a bound shield's block power.
    /// </summary>
    /// <param name="item">The shield.</param>
    /// <param name="blockPower">The block power.</param>
    /// <returns>The block power with the bonus.</returns>
    public static float ModifyBlockPower(ItemDrop.ItemData item, float blockPower)
    {
        BeastDefinition beast = GetBound(item);
        return beast == null ? blockPower : blockPower * (1f + BindingSettings.Bonus(beast));
    }

    /// <summary>
    /// Share of the damage dealt that a weapon etched with the Grip of the Deep gives back as health.
    /// </summary>
    /// <param name="item">The weapon.</param>
    /// <returns>The share, 0 for any other weapon.</returns>
    public static float LifeSteal(ItemDrop.ItemData item)
    {
        BeastDefinition beast = GetBound(item);
        return beast != null && GetInfusion(item)?.Kind == InfusionKind.Deep
            ? BindingSettings.InfusionStrength(beast) * BindingSettings.DeepLifeStealShare.Value
            : 0f;
    }

    /// <summary>
    /// How many times stronger a weapon's poison is against a target: Wolfsbane against beasts.
    /// </summary>
    /// <param name="item">The weapon.</param>
    /// <param name="target">The creature hit.</param>
    /// <returns>The multiplier, 1 for anything else.</returns>
    public static float PoisonMultiplier(ItemDrop.ItemData item, Character target)
    {
        if (target == null || GetBound(item) == null || GetInfusion(item)?.Kind != InfusionKind.Wolfsbane)
        {
            return 1f;
        }

        string beasts = BindingSettings.WolfsbaneBeasts.Value ?? string.Empty;
        if (!ReferenceEquals(beasts, beastsParsedFrom))
        {
            beastsParsedFrom = beasts;
            beastNames = new HashSet<string>(beasts.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }

        return beastNames.Contains(global::Utils.GetPrefabName(target.gameObject)) ? BindingSettings.WolfsbaneBeastMultiplier.Value : 1f;
    }

    /// <summary>
    /// Whether every hit of a weapon webs the target.
    /// </summary>
    /// <param name="item">The weapon.</param>
    /// <returns>True for a bound weapon etched with the Spider's Web.</returns>
    public static bool Webs(ItemDrop.ItemData item)
    {
        return GetBound(item) != null && GetInfusion(item)?.Kind == InfusionKind.Web;
    }

    /// <summary>
    /// Tooltip lines for a bound item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The localized lines, starting with a line break, or an empty string.</returns>
    public static string TooltipText(ItemDrop.ItemData item)
    {
        BeastDefinition beast = GetBound(item);
        if (beast == null)
        {
            return string.Empty;
        }

        Localization localization = Localization.instance;
        bool shield = shieldBySharedName.TryGetValue(item.m_shared.m_name, out bool isShield) && isShield;
        string trophy = localization.Localize(Translations.Token(beast.TrophyKey));
        string text = "\n<color=orange>" + string.Format(localization.Localize(shield ? "$whitehilt_bound_shield" : "$whitehilt_bound_weapon"),
            trophy, Percent(BindingSettings.Bonus(beast))) + "</color>";

        Infusion infusion = GetInfusion(item);
        if (infusion != null)
        {
            string name = localization.Localize(Translations.Token(infusion.NameKey));
            string line = infusion.Kind switch
            {
                InfusionKind.Web => string.Format(localization.Localize("$whitehilt_infused_web"), name),
                InfusionKind.Deep => string.Format(localization.Localize("$whitehilt_infused_deep"), name, Percent(LifeSteal(item))),
                InfusionKind.Wolfsbane => string.Format(localization.Localize("$whitehilt_infused_wolfsbane"), name,
                    Translations.Number(BindingSettings.WolfsbaneBeastMultiplier.Value)),
                _ => string.Format(localization.Localize("$whitehilt_infused"), name),
            };
            text += "\n<color=orange>" + line + "</color>";
        }

        return text;
    }

    private static float BaseDamage(HitData.DamageTypes damages)
    {
        return damages.m_damage + damages.m_blunt + damages.m_slash + damages.m_pierce + damages.m_fire
            + damages.m_frost + damages.m_lightning + damages.m_poison + damages.m_spirit;
    }

    private static string Percent(float share)
    {
        return Translations.Number(share * 100f);
    }
}
