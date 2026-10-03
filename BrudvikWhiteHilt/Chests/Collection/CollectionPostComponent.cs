using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Collection;

/// <summary>A workbench-connected collector that sorts loose items into White Hilt chests.</summary>
internal sealed class CollectionPostComponent : MonoBehaviour, Hoverable, Interactable
{
    private const string PausedKey = "whitehilt_collection_paused";
    private static readonly HashSet<CollectionPostComponent> posts = new();
    internal static ChestModule Module;
    private ZNetView view;
    private StationExtension extension;
    private Light glow;
    private float nextRound;
    private int cursor;

    private bool Active => CollectionSettings.Enabled.Value && view != null && view.IsValid()
        && !view.GetZDO().GetBool(PausedKey) && Bench != null;
    private CraftingStation Bench
    {
        get
        {
            if (extension == null) return null;
            extension.m_maxStationDistance = CollectionSettings.StationDistance.Value;
            return extension.FindClosestStationInRange(transform.position);
        }
    }

    /// <inheritdoc/>
    public string GetHoverName() => "$piece_whitehilt_collectionpost";

    /// <inheritdoc/>
    public float GetHoverOffset() => 0f;

    /// <inheritdoc/>
    public string GetHoverText()
    {
        string status = !CollectionSettings.Enabled.Value || view == null || !view.IsValid() || view.GetZDO().GetBool(PausedKey)
            ? "$whitehilt_collection_paused" : Bench == null ? "$whitehilt_collection_nobench"
            : Player.m_localPlayer != null && Receivers(Player.m_localPlayer).Count == 0 ? "$whitehilt_collection_nochests" : "$whitehilt_collection_active";
        return Localization.instance.Localize(GetHoverName() + "\n" + status + "\n$whitehilt_collection_ranges\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_collection_toggle");
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user is not Player || view == null || !view.IsValid() || !PrivateArea.CheckAccess(transform.position)) return false;
        view.InvokeRPC("WhiteHilt_CollectionToggle");
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        extension = GetComponent<StationExtension>();
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
            foreach (var receiver in receivers.Select(chest => new { Chest = chest, Priority = Module.CollectionPriority(chest, drop.m_itemData) })
                .Where(entry => entry.Priority >= 0).OrderBy(entry => entry.Priority)
                .ThenBy(entry => (entry.Chest.transform.position - drop.transform.position).sqrMagnitude))
            {
                var link = receiver.Chest.GetComponent<CollectionChestLink>();
                if (link == null) link = receiver.Chest.gameObject.AddComponent<CollectionChestLink>();
                if (!link.Ready(player.GetPlayerID())) break;
                if (!drop.CanPickup() || !Active) break;
                int accepted = Module.DepositCollected(receiver.Chest, drop.m_itemData);
                if (accepted <= 0) continue;
                drop.m_itemData.m_stack -= accepted;
                if (drop.m_itemData.m_stack <= 0) { drop.m_nview.Destroy(); break; }
                drop.Save();
            }
        }
        cursor = drops.Count == 0 ? 0 : (cursor + examined) % drops.Count;
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