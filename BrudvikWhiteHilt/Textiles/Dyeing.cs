using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Items.Painting;
using BrudvikWhiteHilt.Items.Textiles;
using BrudvikWhiteHilt.Items.Weapons;
using BrudvikWhiteHilt.Painting;
using BrudvikWhiteHilt.Pieces.Textiles;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BrudvikWhiteHilt.Textiles;

/// <summary>
/// Dyeing with a Paint Pot from the hotbar: the banner or ship you look at gets its cloth or sail dyed, and standing at
/// a loom dyes the cape you wear. Anything else loads the brush as before.
/// </summary>
public static class Dyeing
{
    private const string CapeKey = "whitehilt_dye";
    private static readonly int capeZdoKey = "whitehilt_cape_dye".GetStableHashCode();
    private static readonly ConditionalWeakTable<VisEquipment, CapeDyeState> capeStates = new();

    /// <summary>
    /// Dyes what the player looks at or wears, if anything.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="pot">The paint pot used.</param>
    /// <returns>True if the pot was used for dyeing.</returns>
    public static bool TryUse(Player player, ItemDrop.ItemData pot)
    {
        if (!WhiteHiltPaintPot.TryGetColor(pot, out Color32 color))
        {
            return false;
        }

        GameObject hover = player.GetHoverObject();
        WearNTear piece = hover != null ? hover.GetComponentInParent<WearNTear>() : null;
        if (piece != null && piece.m_nview != null && piece.m_nview.IsValid() && piece.GetComponent<Piece>()?.IsPlacedByPlayer() == true
            && DyedCloth.HasCloth(piece.gameObject))
        {
            if (!PrivateArea.CheckAccess(piece.transform.position))
            {
                return true;
            }

            bool ship = piece.GetComponent<Ship>() != null;
            int uses = ship ? TextileSettings.SailUses.Value : TextileSettings.BannerUses.Value;
            int cloth = ship ? TextileSettings.SailCloth.Value : 0;
            if (Pay(player, pot, uses, cloth, () => TryUse(player, pot)))
            {
                DyedCloth.Set(piece.m_nview, PaintColor.Pack(color, PaintMode.Paint));
                Done(player, piece.GetComponent<Piece>().m_name, color);
            }

            return true;
        }

        ItemDrop.ItemData cape = player.m_shoulderItem;
        if (cape != null && NearLoom(player))
        {
            if (Pay(player, pot, TextileSettings.CapeUses.Value, 0, null))
            {
                cape.m_customData[CapeKey] = PaintColor.ToHex(color);
                GearSlots.LookChanged();
                Done(player, cape.m_shared.m_name, color);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// The packed dye of a cape item, or 0.
    /// </summary>
    /// <param name="item">The cape.</param>
    /// <returns>The packed colour.</returns>
    public static int CapeValue(ItemDrop.ItemData item)
    {
        return item != null && item.m_customData.TryGetValue(CapeKey, out string hex) && PaintColor.TryParseHex(hex, out Color32 color)
            ? PaintColor.Pack(color, PaintMode.Paint)
            : 0;
    }

    /// <summary>
    /// Keeps a character's cape in its dye: the wearer writes the dye of the worn cape to its ZDO, and every machine shows
    /// it on the cape model. Called every frame for every character's equipment, so it remembers per character what it
    /// last wrote, read and showed: the wearer works the dye out again only when the cape worn or a dye changes, and the
    /// ZDO is read again only when its data revision changes.
    /// </summary>
    /// <param name="equipment">The character's equipment visuals.</param>
    public static void RefreshCape(VisEquipment equipment)
    {
        ZNetView nview = equipment.m_nview;
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        CapeDyeState state = capeStates.GetOrCreateValue(equipment);
        if (!state.LookedForPlayer)
        {
            state.LookedForPlayer = true;
            equipment.TryGetComponent(out state.Player);
        }

        Player player = state.Player;
        if (player != null && nview.IsOwner()
            && (!ReferenceEquals(state.WrittenZdo, zdo) || state.WrittenVersion != GearSlots.LookVersion || !ReferenceEquals(state.Cape, player.m_shoulderItem)))
        {
            state.WrittenZdo = zdo;
            state.WrittenVersion = GearSlots.LookVersion;
            state.Cape = player.m_shoulderItem;
            int wanted = CapeValue(state.Cape);
            if (zdo.GetInt(capeZdoKey) != wanted)
            {
                zdo.Set(capeZdoKey, wanted);
            }
        }

        if (!ReferenceEquals(state.ReadZdo, zdo) || state.Revision != zdo.DataRevision)
        {
            state.ReadZdo = zdo;
            state.Revision = zdo.DataRevision;
            state.Wanted = zdo.GetInt(capeZdoKey);
            state.Active |= state.Wanted != 0;
        }

        // Nothing is shown on a character whose cape was never dyed, as most never are.
        if (!state.Active)
        {
            return;
        }

        int value = state.Wanted;
        GameObject first = equipment.m_shoulderItemInstances != null && equipment.m_shoulderItemInstances.Count > 0 ? equipment.m_shoulderItemInstances[0] : null;
        if (state.Value == value && ReferenceEquals(state.Instance, first))
        {
            return;
        }

        state.Value = value;
        state.Instance = first;
        if (first == null)
        {
            return;
        }

        foreach (GameObject instance in equipment.m_shoulderItemInstances)
        {
            DyedCloth.Apply(instance, value, true);
        }
    }

    private static bool NearLoom(Player player)
    {
        float range = TextileSettings.LoomRange.Value;
        foreach (CraftingStation station in CraftingStation.m_allStations)
        {
            if (Loom.IsLoom(station) && Vector3.Distance(station.transform.position, player.transform.position) <= range)
            {
                return true;
            }
        }

        return false;
    }

    // Takes the dye from the pot and the cloth from the player and nearby chests, checking both first so nothing is
    // taken when something is missing. Cloth in chests other players hold is fetched first, and the dyeing done again.
    private static bool Pay(Player player, ItemDrop.ItemData pot, int uses, int cloth, System.Action again)
    {
        if (player.m_noPlacementCost)
        {
            return true;
        }

        if (pot.m_durability < uses)
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_dye_pot"), uses));
            return false;
        }

        List<(string Name, int Amount)> needs = new();
        if (cloth > 0)
        {
            ItemDrop clothItem = ObjectDB.instance.GetItemPrefab(LinenCloth.PrefabName)?.GetComponent<ItemDrop>();
            ChestCost.Outcome outcome = ChestCost.Outcome.Lacking;
            if (clothItem != null)
            {
                needs.Add((clothItem.m_itemData.m_shared.m_name, cloth));
                outcome = ChestCost.Gather(player, NearbyContainers.Use.Building, needs, again);
            }

            if (outcome == ChestCost.Outcome.Lacking)
            {
                player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_dye_cloth"), cloth));
                return false;
            }

            if (outcome != ChestCost.Outcome.Ready)
            {
                ChestCost.ShowWaiting(player);
                return false;
            }
        }

        ChestCost.Take(player, NearbyContainers.Use.Building, needs);

        pot.m_durability -= uses;
        if (pot.m_durability <= 0f)
        {
            player.GetInventory().RemoveItem(pot);
        }

        return true;
    }

    private static void Done(Player player, string name, Color32 color)
    {
        player.Message(MessageHud.MessageType.TopLeft,
            string.Format(Localization.instance.Localize("$msg_whitehilt_dye_done"), Localization.instance.Localize(name)) + " " + PaintColor.Swatch(color));
    }
}

/// <summary>
/// What a character's cape dye was last written, read and shown as, so the dye is only worked out, read and applied
/// when it or the cape changes.
/// </summary>
public sealed class CapeDyeState
{
    /// <summary>Whether the character was looked at for being a player.</summary>
    public bool LookedForPlayer;

    /// <summary>The player, or null for another character.</summary>
    public Player Player;

    /// <summary>The ZDO the wearer last wrote the dye to.</summary>
    public ZDO WrittenZdo;

    /// <summary>The <see cref="GearSlots.LookVersion"/> the dye was last written at.</summary>
    public int WrittenVersion;

    /// <summary>The cape worn when the dye was last written.</summary>
    public ItemDrop.ItemData Cape;

    /// <summary>The ZDO last read.</summary>
    public ZDO ReadZdo;

    /// <summary>Its data revision when last read.</summary>
    public uint Revision;

    /// <summary>The packed colour read.</summary>
    public int Wanted;

    /// <summary>Whether the cape was ever dyed.</summary>
    public bool Active;

    /// <summary>The packed colour shown.</summary>
    public int Value;

    /// <summary>The cape model it was shown on.</summary>
    public GameObject Instance;
}
