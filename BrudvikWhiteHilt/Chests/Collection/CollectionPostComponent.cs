using BrudvikWhiteHilt.Crafting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Collection;

/// <summary>A collector that sorts loose items and the contents of its own basket into White Hilt chests.</summary>
internal sealed class CollectionPostComponent : MonoBehaviour
{
    private const string PausedKey = "whitehilt_collection_paused";
    private static readonly HashSet<CollectionPostComponent> posts = new();
    internal static ChestModule Module;
    private ZNetView view;
    private Container basket;
    private Light glow;
    private float nextRound;
    private int cursor;

    private bool Active => CollectionSettings.Enabled.Value && view != null && view.IsValid() && !view.GetZDO().GetBool(PausedKey);

    /// <summary>Status lines and the pause key, appended to the basket's hover text.</summary>
    /// <returns>The localized lines.</returns>
    internal string GetHoverText()
    {
        string status = !Active ? "$whitehilt_collection_paused"
            : Player.m_localPlayer != null && Receivers(Player.m_localPlayer).Count == 0 ? "$whitehilt_collection_nochests" : "$whitehilt_collection_active";
        return Localization.instance.Localize("\n" + status + "\n$whitehilt_collection_ranges\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_collection_toggle");
    }

    /// <summary>Pauses or resumes the post through its owner.</summary>
    internal void RequestToggle()
    {
        if (view != null && view.IsValid() && PrivateArea.CheckAccess(transform.position)) view.InvokeRPC("WhiteHilt_CollectionToggle");
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        basket = GetComponent<Container>();
        glow = GetComponentInChildren<Light>();
        if (view == null || !view.IsValid()) return;
        posts.Add(this);
        view.Register("WhiteHilt_CollectionToggle", Toggle);
    }

    private void OnDestroy() => posts.Remove(this);

    private void Toggle(long sender)
    {
        if (view.IsOwner()) view.GetZDO().Set(PausedKey, !view.GetZDO().GetBool(PausedKey));
    }

    private void Update()
    {
        if (glow != null) glow.enabled = Active;
        var player = Player.m_localPlayer;
        if (player == null || Module == null || !Active || Time.time < nextRound) return;
        nextRound = Time.time + CollectionSettings.Interval.Value;
        if (!PrivateArea.CheckAccess(transform.position, 0f, false)) return;
        EmptyBasket(player);
        var nearest = Player.GetAllPlayers().OrderBy(candidate => (candidate.transform.position - transform.position).sqrMagnitude)
            .ThenBy(candidate => candidate.GetPlayerID()).FirstOrDefault();
        if (nearest != player) return;

        var receivers = Receivers(player);
        if (receivers.Count == 0) return;
        var drops = new List<ItemDrop>(ItemDrop.s_instances);
        int processed = 0;
        int examined = 0;
        while (examined < drops.Count && examined < CollectionSettings.ScanLimit.Value && processed < CollectionSettings.BatchSize.Value)
        {
            var drop = drops[(cursor + examined) % drops.Count];
            examined++;
            if (drop == null || drop.IsPiece()
                || drop.GetComponent<Fish>() != null || drop.InTar() || drop.m_nview == null || !drop.m_nview.IsValid()
                || (drop.transform.position - transform.position).sqrMagnitude > CollectionSettings.Radius.Value * CollectionSettings.Radius.Value
                || !PrivateArea.CheckAccess(drop.transform.position, 0f, false)) continue;
            if (!CollectionSettings.PlayerDrops.Value && (!drop.m_autoPickup || drop.m_nview.GetZDO().GetBool("whitehilt_player_drop"))) continue;
            var chosen = posts.Where(post => post != null && post.Active
                    && PrivateArea.CheckAccess(post.transform.position, 0f, false)
                    && (post.transform.position - drop.transform.position).sqrMagnitude <= CollectionSettings.Radius.Value * CollectionSettings.Radius.Value
                    && post.Receivers(player).Any(chest => Module.CollectionPriority(chest, drop.m_itemData) >= 0))
                .OrderBy(post => (post.transform.position - drop.transform.position).sqrMagnitude)
                .ThenBy(post => post.view.GetZDO().m_uid.ToString()).FirstOrDefault();
            if (chosen != this) continue;
            processed++;
            if (!drop.CanPickup())
            {
                if (receivers.Any(chest => Module.CollectionPriority(chest, drop.m_itemData) >= 0)) drop.RequestOwn();
                continue;
            }
            drop.Load();
            foreach (var chest in Ordered(receivers, drop.m_itemData, drop.transform.position))
            {
                if (!Link(chest).Ready(player.GetPlayerID())) break;
                if (!drop.CanPickup() || !Active) break;
                int accepted = Module.DepositCollected(chest, drop.m_itemData);
                if (accepted <= 0) continue;
                drop.m_itemData.m_stack -= accepted;
                if (drop.m_itemData.m_stack <= 0) { drop.m_nview.Destroy(); break; }
                drop.Save();
            }
        }
        cursor = drops.Count == 0 ? 0 : (cursor + examined) % drops.Count;
    }

    // The basket owner sorts it, only while nobody has it open; what fits nowhere stays for the player to take back.
    private void EmptyBasket(Player player)
    {
        if (basket == null || !view.IsOwner() || BasketOpen()) return;
        var inventory = basket.GetInventory();
        if (inventory == null || inventory.NrOfItems() == 0) return;
        var receivers = Receivers(player);
        if (receivers.Count == 0) return;
        int processed = 0;
        foreach (var item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
        {
            if (processed >= CollectionSettings.BatchSize.Value) return;
            processed++;
            foreach (var chest in Ordered(receivers, item, transform.position))
            {
                if (!Link(chest).Ready(player.GetPlayerID())) return;
                if (!view.IsOwner() || BasketOpen() || !Active) return;
                int accepted = Module.DepositCollected(chest, item);
                if (accepted <= 0) continue;
                bool all = accepted >= item.m_stack;
                inventory.RemoveItem(item, accepted);
                if (all) break;
            }
        }
    }

    private bool BasketOpen() => basket.IsInUse() || view.GetZDO().GetInt(ZDOVars.s_inUse) != 0;

    // Category chests first, then chests already holding the item, then the nearest.
    private static IEnumerable<Container> Ordered(List<Container> receivers, ItemDrop.ItemData item, Vector3 from) =>
        receivers.Select(chest => new { Chest = chest, Priority = Module.CollectionPriority(chest, item) })
            .Where(entry => entry.Priority >= 0).OrderBy(entry => entry.Priority)
            .ThenBy(entry => entry.Chest.GetInventory().ContainsItemByName(item.m_shared.m_name) ? 0 : 1)
            .ThenBy(entry => (entry.Chest.transform.position - from).sqrMagnitude)
            .Select(entry => entry.Chest).ToList();

    private static CollectionChestLink Link(Container chest)
    {
        var link = chest.GetComponent<CollectionChestLink>();
        return link != null ? link : chest.gameObject.AddComponent<CollectionChestLink>();
    }

    private List<Container> Receivers(Player player)
    {
        if (Module == null) return new List<Container>();
        return NearbyContainers.Registered().Where(chest => chest != null && Module.IsCollectionChest(chest)
            && chest.m_nview != null && chest.m_nview.IsValid() && chest.GetInventory() != null
            && !chest.IsInUse() && chest.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 0
            && (chest.transform.position - transform.position).sqrMagnitude <= CollectionSettings.ChestRadius.Value * CollectionSettings.ChestRadius.Value
            && chest.CheckAccess(player.GetPlayerID()) && (!chest.m_checkGuardStone || PrivateArea.CheckAccess(chest.transform.position, 0f, false))).ToList();
    }
}
