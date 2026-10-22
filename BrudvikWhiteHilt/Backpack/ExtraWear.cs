using System.Collections.Generic;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Items the mod lets the local player wear beyond the game's own slots: the extra accessories and the extra trinkets.
/// The game does not equip them, so their effects, equipment modifiers, eitr regeneration and weight are summed and
/// given here, as if they were worn.
/// </summary>
internal sealed class ExtraWear
{
    private readonly HashSet<StatusEffect> applied = new();
    private float[] modifierSums;

    /// <summary>The items worn, one per extra slot; null for an empty slot.</summary>
    public List<ItemDrop.ItemData> Worn { get; } = new();

    /// <summary>Eitr regeneration added by the items.</summary>
    public float EitrRegen { get; private set; }

    /// <summary>Weight of the items, counted as worn equipment.</summary>
    public float Weight { get; private set; }

    /// <summary>
    /// Forgets everything worn, for a new player.
    /// </summary>
    public void Reset()
    {
        Worn.Clear();
        applied.Clear();
    }

    /// <summary>
    /// Sums the eitr regeneration, weight and the equipment modifiers the game reads by reflection of what is in
    /// <see cref="Worn"/>.
    /// </summary>
    public void Collect()
    {
        EitrRegen = 0f;
        Weight = 0f;
        System.Reflection.FieldInfo[] fields = Player.s_equipmentModifierSourceFields;
        modifierSums = fields != null ? new float[fields.Length] : null;
        foreach (ItemDrop.ItemData item in Worn)
        {
            if (item == null)
            {
                continue;
            }

            EitrRegen += item.m_shared.m_eitrRegenModifier;
            Weight += item.m_shared.m_weight;
            for (int i = 0; modifierSums != null && i < fields.Length; i++)
            {
                if (fields[i].GetValue(item.m_shared) is float value)
                {
                    modifierSums[i] += value;
                }
            }
        }
    }

    /// <summary>
    /// Adds the items' equipment modifiers to the player's.
    /// </summary>
    /// <param name="values">The player's summed equipment modifiers.</param>
    public void AddModifiers(float[] values)
    {
        if (modifierSums == null || values == null)
        {
            return;
        }

        for (int i = 0; i < values.Length && i < modifierSums.Length; i++)
        {
            values[i] += modifierSums[i];
        }
    }

    /// <summary>
    /// Keeps the status effects of the worn items on the player, removing those of items taken off (unless worn gear
    /// gives the same).
    /// </summary>
    /// <param name="player">The local player.</param>
    public void ApplyStatusEffects(Player player)
    {
        HashSet<StatusEffect> desired = new();
        foreach (ItemDrop.ItemData item in Worn)
        {
            if (item != null && item.m_shared.m_equipStatusEffect != null)
            {
                desired.Add(item.m_shared.m_equipStatusEffect);
            }
        }

        foreach (StatusEffect effect in applied)
        {
            if (!desired.Contains(effect) && !player.m_equipmentStatusEffects.Contains(effect))
            {
                player.m_seman.RemoveStatusEffect(effect.NameHash());
            }
        }

        foreach (StatusEffect effect in desired)
        {
            if (!player.m_seman.HaveStatusEffect(effect.NameHash()))
            {
                player.m_seman.AddStatusEffect(effect);
            }
        }

        applied.Clear();
        applied.UnionWith(desired);
    }
}
