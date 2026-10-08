using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Items.Painting;
using BrudvikWhiteHilt.Items.Textiles;
using BrudvikWhiteHilt.Painting;
using BrudvikWhiteHilt.Pieces.Textiles;
using System.Collections.Generic;
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
    /// it on the cape model. Called every frame for every character's equipment.
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
        if (nview.IsOwner() && equipment.TryGetComponent(out Player player))
        {
            int wanted = CapeValue(player.m_shoulderItem);
            if (zdo.GetInt(capeZdoKey) != wanted)
            {
                zdo.Set(capeZdoKey, wanted);
            }
        }

        int value = zdo.GetInt(capeZdoKey);
        GameObject first = equipment.m_shoulderItemInstances != null && equipment.m_shoulderItemInstances.Count > 0 ? equipment.m_shoulderItemInstances[0] : null;
        CapeDyeState state = equipment.GetComponent<CapeDyeState>();
        if (state == null)
        {
            if (value == 0)
            {
                return;
            }

            state = equipment.gameObject.AddComponent<CapeDyeState>();
        }

        if (state.Value == value && state.Instance == first)
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
/// What a character's cape was last dyed with, so the dye is only applied when it or the cape changes.
/// </summary>
public class CapeDyeState : MonoBehaviour
{
    /// <summary>The packed colour shown.</summary>
    public int Value;

    /// <summary>The cape model it was shown on.</summary>
    public GameObject Instance;
}
