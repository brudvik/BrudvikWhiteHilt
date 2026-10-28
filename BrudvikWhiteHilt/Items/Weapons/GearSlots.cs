using System;
using System.Runtime.CompilerServices;
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
    /// Goes up whenever a look is etched into, dyed onto or taken off an item, so the holder writes the looks of what
    /// they carry again even though they still carry the same items.
    /// </summary>
    public static int LookVersion { get; private set; }

    /// <summary>
    /// Tells the holders of gear that the look of an item may have changed.
    /// </summary>
    public static void LookChanged()
    {
        LookVersion++;
    }

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
    /// The item a player has in a place.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The place.</param>
    /// <returns>The item, or null.</returns>
    public static ItemDrop.ItemData Item(Player player, int slot)
    {
        return slot switch
        {
            0 => player.m_rightItem,
            1 => player.m_leftItem,
            2 => player.m_hiddenRightItem,
            _ => player.m_hiddenLeftItem,
        };
    }

    /// <summary>
    /// The model drawn in a place.
    /// </summary>
    /// <param name="equipment">The character's equipment visuals.</param>
    /// <param name="slot">The place.</param>
    /// <returns>The model, or null.</returns>
    public static GameObject Instance(VisEquipment equipment, int slot)
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
/// One look kept on characters' gear (a glow, a flame): the owner writes the look of each place's item to their ZDO,
/// and every machine applies it to the place's model whenever the value or the model changes.
/// </summary>
/// <remarks>
/// <see cref="Refresh"/> runs every frame for every humanoid, draugr and goblins included, so it remembers per
/// character what it last read and wrote: the owner works out the looks again only when the items carried or
/// <see cref="GearSlots.LookVersion"/> change, and the ZDO is read again only when its data revision changes, which
/// it also does when a remote holder's new values arrive.
/// </remarks>
public sealed class GearLook
{
    private readonly int[] keys;
    private readonly Func<ItemDrop.ItemData, int> value;
    private readonly Action<GameObject, int> apply;
    private readonly ConditionalWeakTable<VisEquipment, Record> records = new();

    /// <summary>
    /// Makes a look.
    /// </summary>
    /// <param name="prefix">The look's name, which its ZDO keys start with (see <see cref="GearSlots.Keys"/>).</param>
    /// <param name="value">The packed look of an item, 0 for none.</param>
    /// <param name="apply">Gives a model a packed look, or takes it off for 0.</param>
    public GearLook(string prefix, Func<ItemDrop.ItemData, int> value, Action<GameObject, int> apply)
    {
        keys = GearSlots.Keys(prefix);
        this.value = value;
        this.apply = apply;
    }

    /// <summary>
    /// Keeps the look on a character's gear. Called every frame for every character's equipment.
    /// </summary>
    /// <param name="equipment">The character's equipment visuals.</param>
    public void Refresh(VisEquipment equipment)
    {
        ZNetView nview = equipment.m_nview;
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        Record record = records.GetOrCreateValue(equipment);
        if (!record.LookedForPlayer)
        {
            record.LookedForPlayer = true;
            equipment.TryGetComponent(out record.Player);
        }

        Player player = record.Player;
        if (player != null && nview.IsOwner() && OwnerChanged(record, player, zdo))
        {
            for (int slot = 0; slot < GearSlots.Count; slot++)
            {
                int packed = value(record.Items[slot]);
                if (zdo.GetInt(keys[slot]) != packed)
                {
                    zdo.Set(keys[slot], packed);
                }
            }
        }

        if (!ReferenceEquals(record.ReadZdo, zdo) || record.Revision != zdo.DataRevision)
        {
            record.ReadZdo = zdo;
            record.Revision = zdo.DataRevision;
            for (int slot = 0; slot < GearSlots.Count; slot++)
            {
                int packed = zdo.GetInt(keys[slot]);
                record.Wanted[slot] = packed;
                record.Active |= packed != 0;
            }
        }

        // Nothing is applied to a character whose gear never had this look, as most never do.
        if (!record.Active)
        {
            return;
        }

        for (int slot = 0; slot < GearSlots.Count; slot++)
        {
            int packed = record.Wanted[slot];
            GameObject instance = GearSlots.Instance(equipment, slot);
            if (record.Values[slot] != packed || !ReferenceEquals(record.Instances[slot], instance))
            {
                record.Values[slot] = packed;
                record.Instances[slot] = instance;
                apply(instance, packed);
            }
        }
    }

    // Whether the owner carries other items, an item's look changed, or the ZDO is new, since the looks were last
    // written; remembers what is carried now.
    private static bool OwnerChanged(Record record, Player player, ZDO zdo)
    {
        bool changed = !ReferenceEquals(record.WrittenZdo, zdo) || record.WrittenVersion != GearSlots.LookVersion;
        for (int slot = 0; slot < GearSlots.Count; slot++)
        {
            ItemDrop.ItemData item = GearSlots.Item(player, slot);
            if (!ReferenceEquals(record.Items[slot], item))
            {
                record.Items[slot] = item;
                changed = true;
            }
        }

        record.WrittenZdo = zdo;
        record.WrittenVersion = GearSlots.LookVersion;
        return changed;
    }

    // What this look last read, wrote and applied for one character.
    private sealed class Record
    {
        public bool LookedForPlayer;
        public Player Player;

        public ZDO WrittenZdo;
        public int WrittenVersion;
        public readonly ItemDrop.ItemData[] Items = new ItemDrop.ItemData[GearSlots.Count];

        public ZDO ReadZdo;
        public uint Revision;
        public bool Active;
        public readonly int[] Wanted = new int[GearSlots.Count];

        public readonly int[] Values = new int[GearSlots.Count];
        public readonly GameObject[] Instances = new GameObject[GearSlots.Count];
    }
}
