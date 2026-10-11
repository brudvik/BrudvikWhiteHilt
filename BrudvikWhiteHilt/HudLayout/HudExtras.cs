using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Production;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.HudLayout;

/// <summary>
/// What White Hilt itself puts in the console through <see cref="WhiteHiltHud"/>: worn gear and a heavy load among the
/// status effects, and the creature you fight and the stations near you that need you in the strip over the bars.
/// </summary>
public static class HudExtras
{
    private const int WearSlots = 6;
    private const string WearId = "whitehilt.wear.";
    private const string WeightId = "whitehilt.weight";
    private const string TargetId = "whitehilt.target";
    private const string ProductionId = "whitehilt.production";
    private const int TargetPriority = 130;
    private const int ProductionPriority = 90;
    private const int WearPriority = 20;
    private const int WeightPriority = 10;
    private const float TargetLinger = 6f;
    private const float TargetRange = 30f;
    private const float ProductionSeconds = 1f;
    private const int ProductionShown = 2;
    private const string Separator = "   ·   ";

    private static readonly Color wornColor = new(1f, 0.65f, 0.3f);
    private static readonly Color brokenColor = new(1f, 0.35f, 0.3f);
    private static readonly Color heavyColor = new(0.95f, 0.8f, 0.35f);
    private static readonly Color weakColor = new(0.6f, 0.9f, 0.55f);
    private static readonly Color resistColor = new(0.95f, 0.75f, 0.4f);
    private static readonly Color immuneColor = new(0.7f, 0.7f, 0.7f);
    private static readonly HudIndicator[] wear = new HudIndicator[WearSlots];
    private static readonly HudIndicator weight = new();
    private static readonly List<ItemDrop.ItemData> equipped = new();
    private static readonly List<Component> stations = new();
    private static readonly List<ProductionStatus> needing = new();
    private static readonly StringBuilder line = new();
    private static readonly (string Token, System.Func<HitData.DamageModifiers, HitData.DamageModifier> Read)[] damageTypes =
    {
        ("$inventory_blunt", modifiers => modifiers.m_blunt),
        ("$inventory_slash", modifiers => modifiers.m_slash),
        ("$inventory_pierce", modifiers => modifiers.m_pierce),
        ("$inventory_fire", modifiers => modifiers.m_fire),
        ("$inventory_frost", modifiers => modifiers.m_frost),
        ("$inventory_lightning", modifiers => modifiers.m_lightning),
        ("$inventory_poison", modifiers => modifiers.m_poison),
        ("$inventory_spirit", modifiers => modifiers.m_spirit)
    };

    private static int wearFrame = -1;
    private static int wearCount;
    private static Sprite weightIcon;
    private static Character lastTarget;
    private static float lastTargetTime;
    private static float nextProduction;
    private static string productionLine;

    /// <summary>
    /// Registers the texts and puts the indicators and context lines in the White Hilt HUD. Call from the plugin's
    /// Awake.
    /// </summary>
    public static void Register()
    {
        Translations.AddEnglish("whitehilt_hud_weight", "Load");
        Translations.AddEnglish("whitehilt_hud_weak", "weak to");
        Translations.AddEnglish("whitehilt_hud_resists", "resists");
        Translations.AddEnglish("whitehilt_hud_immune", "immune to");
        Translations.AddEnglish("whitehilt_hud_star", "1 star");
        Translations.AddEnglish("whitehilt_hud_stars", "{0} stars");

        for (int i = 0; i < WearSlots; i++)
        {
            int slot = i;
            wear[i] = new HudIndicator();
            WhiteHiltHud.RegisterIndicator(WearId + i, WearPriority, () => Wear(slot));
        }

        WhiteHiltHud.RegisterIndicator(WeightId, WeightPriority, Weight);
        WhiteHiltHud.RegisterContext(TargetId, TargetPriority, TargetLine);
        WhiteHiltHud.RegisterContext(ProductionId, ProductionPriority, ProductionLine);
    }

    // The worn pieces of gear the player has on, under the warning share, worst first; one per slot.
    private static HudIndicator Wear(int slot)
    {
        Player player = Player.m_localPlayer;
        if (player == null || !HudSettings.ShowWear.Value)
        {
            return null;
        }

        if (wearFrame != Time.frameCount)
        {
            wearFrame = Time.frameCount;
            wearCount = 0;
            equipped.Clear();
            equipped.AddRange(player.GetInventory().GetEquippedItems());
            equipped.RemoveAll(item => !IsWorn(item));
            equipped.Sort((a, b) => Share(a).CompareTo(Share(b)));
            foreach (ItemDrop.ItemData item in equipped)
            {
                if (wearCount >= WearSlots)
                {
                    break;
                }

                float share = Share(item);
                HudIndicator indicator = wear[wearCount++];
                indicator.Icon = item.GetIcon();
                indicator.Name = Localization.instance.Localize(item.m_shared.m_name);
                indicator.Text = Mathf.FloorToInt(share * 100f) + "%";
                indicator.Tint = share <= 0f ? brokenColor : wornColor;
                indicator.Flash = share <= 0f;
            }
        }

        return slot < wearCount ? wear[slot] : null;
    }

    private static bool IsWorn(ItemDrop.ItemData item)
    {
        return item != null && item.m_shared.m_useDurability && item.GetMaxDurability() > 0f
            && Share(item) < Mathf.Clamp01(HudSettings.WearWarning.Value);
    }

    private static float Share(ItemDrop.ItemData item)
    {
        float max = item.GetMaxDurability();
        return max > 0f ? Mathf.Clamp01(item.m_durability / max) : 1f;
    }

    // The load against what the player can carry, from the warning share on; red when it is too much.
    private static HudIndicator Weight()
    {
        Player player = Player.m_localPlayer;
        if (player == null || !HudSettings.ShowWeight.Value)
        {
            return null;
        }

        float carried = player.GetInventory().GetTotalWeight();
        float max = player.GetMaxCarryWeight();
        if (max <= 0f || carried < max * Mathf.Clamp01(HudSettings.WeightWarning.Value))
        {
            return null;
        }

        weight.Icon = WeightIcon();
        if (weight.Icon == null)
        {
            return null;
        }

        weight.Name = Localization.instance.Localize("$whitehilt_hud_weight");
        weight.Text = $"{Mathf.CeilToInt(carried)}/{Mathf.RoundToInt(max)}";
        weight.Tint = carried > max ? brokenColor : heavyColor;
        weight.Flash = false;
        return weight;
    }

    // The game's own icon for carrying too much.
    private static Sprite WeightIcon()
    {
        if (weightIcon == null && ObjectDB.instance != null)
        {
            weightIcon = ObjectDB.instance.GetStatusEffect(SEMan.s_statusEffectEncumbered)?.m_icon;
        }

        return weightIcon;
    }

    // The creature under the crosshair, or the one last looked at for a few seconds after, or else the nearest one
    // hunting the player: its name, stars and health, and what it is weak to, resists and is immune to.
    private static string TargetLine()
    {
        Player player = Player.m_localPlayer;
        if (player == null || !HudSettings.ShowTarget.Value)
        {
            return null;
        }

        Character target = player.GetHoverCreature();
        if (IsFoe(target))
        {
            lastTarget = target;
            lastTargetTime = Time.time;
        }
        else if (IsFoe(lastTarget) && Time.time - lastTargetTime < TargetLinger)
        {
            target = lastTarget;
        }
        else
        {
            target = Hunter(player);
        }

        if (!IsFoe(target))
        {
            return null;
        }

        line.Clear();
        line.Append("<b>").Append(target.GetHoverName()).Append("</b>");
        int stars = target.GetLevel() - 1;
        if (stars > 0)
        {
            line.Append(" <color=#F2C27A>").Append(stars == 1
                ? Localization.instance.Localize("$whitehilt_hud_star")
                : string.Format(Localization.instance.Localize("$whitehilt_hud_stars"), stars)).Append("</color>");
        }

        line.Append(Separator).Append(Mathf.CeilToInt(target.GetHealth())).Append(" / ").Append(Mathf.CeilToInt(target.GetMaxHealth()));
        HitData.DamageModifiers modifiers = target.GetDamageModifiers();
        AppendModifiers(modifiers, "$whitehilt_hud_weak", weakColor, modifier => modifier.ToString().Contains("Weak"));
        AppendModifiers(modifiers, "$whitehilt_hud_resists", resistColor, modifier => modifier.ToString().Contains("Resistant"));
        AppendModifiers(modifiers, "$whitehilt_hud_immune", immuneColor, modifier => modifier == HitData.DamageModifier.Immune);
        return line.ToString();
    }

    private static void AppendModifiers(HitData.DamageModifiers modifiers, string label, Color color, System.Predicate<HitData.DamageModifier> wanted)
    {
        bool first = true;
        foreach ((string token, System.Func<HitData.DamageModifiers, HitData.DamageModifier> read) in damageTypes)
        {
            if (!wanted(read(modifiers)))
            {
                continue;
            }

            if (first)
            {
                line.Append(Separator).Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>')
                    .Append(Localization.instance.Localize(label)).Append(' ');
                first = false;
            }
            else
            {
                line.Append(", ");
            }

            line.Append(Localization.instance.Localize(token).ToLowerInvariant());
        }

        if (!first)
        {
            line.Append("</color>");
        }
    }

    private static bool IsFoe(Character character)
    {
        return character != null && !character.IsDead() && !character.IsPlayer() && !character.IsTamed();
    }

    // The nearest living creature within range whose target is the player.
    private static Character Hunter(Player player)
    {
        Character nearest = null;
        float best = TargetRange * TargetRange;
        Vector3 here = player.transform.position;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!IsFoe(character) || character.GetBaseAI() is not BaseAI ai || ai.GetTargetCreature() != player)
            {
                continue;
            }

            float distance = (character.transform.position - here).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                nearest = character;
            }
        }

        return nearest;
    }

    // The stations near the player that have stopped, need them soon or are done, the most urgent first.
    private static string ProductionLine()
    {
        Player player = Player.m_localPlayer;
        if (player == null || !HudSettings.ShowProduction.Value)
        {
            return null;
        }

        if (Time.unscaledTime < nextProduction)
        {
            return productionLine;
        }

        nextProduction = Time.unscaledTime + ProductionSeconds;
        ProductionRegistry.Near(player.transform.position, Mathf.Max(1f, HudSettings.ProductionRange.Value), stations);
        needing.Clear();
        foreach (Component station in stations)
        {
            ProductionStatus status = new();
            if (ProductionReader.Read(station, status, false) && !string.IsNullOrEmpty(status.Summary)
                && (status.State == ProductionState.Stopped || status.State == ProductionState.Ready || status.Attention))
            {
                needing.Add(status);
            }
        }

        needing.Sort((a, b) => Urgency(a).CompareTo(Urgency(b)));
        line.Clear();
        for (int i = 0; i < needing.Count && i < ProductionShown; i++)
        {
            if (i > 0)
            {
                line.Append(Separator);
            }

            line.Append(Localization.instance.Localize(needing[i].Name)).Append(": ").Append(needing[i].Summary);
        }

        if (needing.Count > ProductionShown)
        {
            line.Append(Separator).Append('+').Append(needing.Count - ProductionShown);
        }

        productionLine = line.Length > 0 ? line.ToString() : null;
        return productionLine;
    }

    private static int Urgency(ProductionStatus status)
    {
        return status.State == ProductionState.Stopped ? 0 : status.Attention ? 1 : 2;
    }
}
