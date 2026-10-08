using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// The four places a character's weapons and shields are drawn: both hands and, when sheathed, both places on the
/// back. The back models are separate instances, so a look kept on the hand models has to be kept on them too.
/// </summary>
public static class GearSlots
{
    /// <summary>The number of places.</summary>
    public const int Count = 4;

    /// <summary>
    /// The ZDO keys of a look, one per place: <c>&lt;prefix&gt;_right</c>, <c>_left</c>, <c>_backright</c> and <c>_backleft</c>.
    /// </summary>
    /// <param name="prefix">The look's name.</param>
    /// <returns>The keys in place order.</returns>
    public static int[] Keys(string prefix)
    {
        return new[]
        {
            $"{prefix}_right".GetStableHashCode(),
            $"{prefix}_left".GetStableHashCode(),
            $"{prefix}_backright".GetStableHashCode(),
            $"{prefix}_backleft".GetStableHashCode()
        };
    }

    /// <summary>
    /// Keeps a look on a character's gear: the owner writes the look of each place's item to their ZDO, and every
    /// machine applies it to the place's model whenever the value or the model changes. Called every frame.
    /// </summary>
    /// <typeparam name="TState">The look's own record of what each place was last given.</typeparam>
    /// <param name="equipment">The character's equipment visuals.</param>
    /// <param name="keys">The look's ZDO keys from <see cref="Keys"/>.</param>
    /// <param name="value">The packed look of an item, 0 for none.</param>
    /// <param name="apply">Gives a model a packed look, or takes it off for 0.</param>
    public static void Refresh<TState>(VisEquipment equipment, int[] keys, Func<ItemDrop.ItemData, int> value, Action<GameObject, int> apply)
        where TState : GearSlotState
    {
        ZNetView nview = equipment.m_nview;
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (nview.IsOwner() && equipment.TryGetComponent(out Player player))
        {
            for (int slot = 0; slot < Count; slot++)
            {
                int packed = value(Item(player, slot));
                if (zdo.GetInt(keys[slot]) != packed)
                {
                    zdo.Set(keys[slot], packed);
                }
            }
        }

        TState state = equipment.GetComponent<TState>();
        for (int slot = 0; slot < Count; slot++)
        {
            int packed = zdo.GetInt(keys[slot]);
            if (state == null)
            {
                if (packed == 0)
                {
                    continue;
                }

                state = equipment.gameObject.AddComponent<TState>();
            }

            GameObject instance = Instance(equipment, slot);
            if (state.Values[slot] != packed || state.Instances[slot] != instance)
            {
                state.Values[slot] = packed;
                state.Instances[slot] = instance;
                apply(instance, packed);
            }
        }
    }

    private static ItemDrop.ItemData Item(Player player, int slot)
    {
        return slot switch
        {
            0 => player.m_rightItem,
            1 => player.m_leftItem,
            2 => player.m_hiddenRightItem,
            _ => player.m_hiddenLeftItem,
        };
    }

    private static GameObject Instance(VisEquipment equipment, int slot)
    {
        return slot switch
        {
            0 => equipment.m_rightItemInstance,
            1 => equipment.m_leftItemInstance,
            2 => equipment.m_rightBackItemInstance,
            _ => equipment.m_leftBackItemInstance,
        };
    }
}

/// <summary>
/// What a look last gave each of a character's gear models (hands, then back), so it is only applied again when that
/// changes.
/// </summary>
public abstract class GearSlotState : MonoBehaviour
{
    /// <summary>The packed look of each place.</summary>
    public readonly int[] Values = new int[GearSlots.Count];

    /// <summary>The model of each place that was given it.</summary>
    public readonly GameObject[] Instances = new GameObject[GearSlots.Count];
}
