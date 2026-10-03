using BrudvikWhiteHilt.Helpers;
using System.Linq;

namespace BrudvikWhiteHilt.Items.Binding;

/// <summary>
/// A rune etched into a trophy-bound White Hilt weapon at the Rune Etching Table.
/// </summary>
public enum InfusionKind
{
    /// <summary>Dyrnwyn's flame: fire damage, which sets the target alight.</summary>
    Flame,

    /// <summary>Frost damage, which slows the target.</summary>
    Frost,

    /// <summary>Poison damage.</summary>
    Venom,

    /// <summary>Every hit webs the target like a giant spider's bite.</summary>
    Web,

    /// <summary>The grip of the deep: part of the damage dealt comes back as health.</summary>
    Deep,

    /// <summary>Lightning damage.</summary>
    Storm,

    /// <summary>Seid smoke: spirit damage, the bane of the dead.</summary>
    Seid,

    /// <summary>Wolfsbane: poison damage, several times as strong against beasts.</summary>
    Wolfsbane,

    /// <summary>Dread: some hits send the target running in terror.</summary>
    Dread,

    /// <summary>Mire's Hold: every hit tars the target, which slows it.</summary>
    Mire,
}

/// <summary>
/// What each infusion is etched with: its rune, its default cost besides the rune, and its name.
/// </summary>
public sealed class Infusion
{
    /// <summary>
    /// Every infusion.
    /// </summary>
    public static readonly Infusion[] All =
    {
        new(InfusionKind.Flame, "WhiteHiltFlametalRune", "SurtlingCore:3", "Dyrnwyn's Flame"),
        new(InfusionKind.Frost, "WhiteHiltSilverRune", "WhiteHiltCrowberries:10", "Frost"),
        new(InfusionKind.Venom, "WhiteHiltBronzeRune", "WhiteHilt_PoisonGland:3", "Venom"),
        new(InfusionKind.Web, "WhiteHiltIronRune", "WhiteHilt_SpiderSilk:5", "Spider's Web"),
        new(InfusionKind.Deep, "WhiteHiltGoldRune", "WhiteHilt_KrakenInk:3, WhiteHilt_KrakenMeat:1", "Grip of the Deep"),
        new(InfusionKind.Storm, "WhiteHiltBlackMetalRune", "WhiteHilt_KrakenInk:3", "Storm"),
        new(InfusionKind.Seid, "WhiteHiltBoneRune", "WhiteHiltJuniper:10", "Seid Smoke"),
        new(InfusionKind.Wolfsbane, "WhiteHiltFangRune", "WhiteHiltWolfLichen:10", "Wolfsbane"),
        new(InfusionKind.Dread, "WhiteHiltObsidianRune", "WhiteHiltErgot:5", "Dread"),
        new(InfusionKind.Mire, "WhiteHiltMireRune", "WhiteHiltPeat:5, Tar:3", "Mire's Hold"),
    };

    private Infusion(InfusionKind kind, string rune, string defaultCost, string englishName)
    {
        Kind = kind;
        Rune = rune;
        DefaultCost = defaultCost;
        EnglishName = englishName;
    }

    /// <summary>Which infusion this is.</summary>
    public InfusionKind Kind { get; }

    /// <summary>Prefab name of the rune that etches it.</summary>
    public string Rune { get; }

    /// <summary>Default cost besides the rune, as Prefab:Amount pairs.</summary>
    public string DefaultCost { get; }

    /// <summary>English name.</summary>
    public string EnglishName { get; }

    /// <summary>Translation key of its name.</summary>
    public string NameKey => $"whitehilt_infusion_{Kind.ToString().ToLowerInvariant()}";

    /// <summary>
    /// The infusion a rune etches.
    /// </summary>
    /// <param name="runePrefab">Prefab name of the rune.</param>
    /// <returns>The infusion, or null for anything else.</returns>
    public static Infusion FromRune(string runePrefab)
    {
        return All.FirstOrDefault(infusion => infusion.Rune == runePrefab);
    }

    /// <summary>
    /// The infusion with a kind.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The infusion.</returns>
    public static Infusion Get(InfusionKind kind)
    {
        return All.First(infusion => infusion.Kind == kind);
    }

    /// <summary>
    /// Adds the damage of this infusion to a weapon's damage.
    /// </summary>
    /// <param name="damages">The damage, changed in place.</param>
    /// <param name="amount">Damage to add.</param>
    public void AddDamage(ref HitData.DamageTypes damages, float amount)
    {
        switch (Kind)
        {
            case InfusionKind.Flame:
                damages.m_fire += amount;
                break;
            case InfusionKind.Frost:
                damages.m_frost += amount;
                break;
            case InfusionKind.Venom:
            case InfusionKind.Wolfsbane:
                damages.m_poison += amount;
                break;
            case InfusionKind.Storm:
                damages.m_lightning += amount;
                break;
            case InfusionKind.Seid:
                damages.m_spirit += amount;
                break;
        }
    }

    /// <summary>
    /// Registers the English names.
    /// </summary>
    public static void RegisterTranslations()
    {
        foreach (Infusion infusion in All)
        {
            Translations.AddEnglish(infusion.NameKey, infusion.EnglishName);
        }
    }
}
