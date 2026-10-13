using BrudvikWhiteHilt.Saga;
using BrudvikWhiteHilt.Treasure;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Raids;

/// <summary>
/// On the hoard chest of a draugr harbour. The chest's owner fills it once from the harbour's loot list. It stays locked
/// while the harbour's shore guards live near it (a patch on <c>Container.Interact</c> asks <see cref="GuardsAlive"/>);
/// the first time it is opened with no guards left the owner writes the deed in the sagas, once. A chest that is sacked
/// and emptied removes itself, which also frees its place among the harbours the world may hold.
/// </summary>
public class RaidHoard : MonoBehaviour
{
    /// <summary>Object RPC the opener sends to the chest's owner.</summary>
    public const string DoneRpc = "WhiteHiltRaidDone";

    private const float CheckInterval = 1f;

    // Guards this far beyond the ring they hold and the distance they may walk from it still count as the hoard's.
    private const float GuardMargin = 10f;

    /// <summary>ZDO key that marks a sacked harbour; the server's count of active harbours reads it.</summary>
    public static readonly int DoneKey = "whitehilt_raid_done".GetStableHashCode();

    private static readonly int filledKey = "whitehilt_raid_filled".GetStableHashCode();

    private ZNetView nview;
    private Container container;
    private float nextCheck;

    /// <summary>The id of the harbour this chest belongs to, from its ZDO.</summary>
    public string SiteId => nview != null && nview.IsValid() ? nview.GetZDO().GetString(RaiderKeys.Site) : string.Empty;

    /// <summary>Whether the harbour has been sacked: the chest was opened once with no guards alive.</summary>
    public bool IsDone => nview != null && nview.IsValid() && nview.GetZDO().GetBool(DoneKey);

    /// <summary>
    /// Whether a shore guard of this harbour lives near the chest, among the characters loaded on this machine. Guards
    /// that are not loaded do not count; they are far from anyone who could stand at the chest.
    /// </summary>
    /// <returns>True while the chest must stay shut.</returns>
    public bool GuardsAlive()
    {
        string site = SiteId;
        if (site.Length == 0)
        {
            return false;
        }

        float reach = SeaBattleSettings.Raids.GuardRadius.Value + SeaBattleSettings.Raids.GuardHoldRadius.Value + GuardMargin;
        foreach (RaiderCrew member in RaiderCrew.All)
        {
            if (member.ShipId.IsNone() && member.SiteId == site && member.Alive
                && Utils.DistanceXZ(member.transform.position, transform.position) <= reach)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tells the chest's owner that a player has opened it with no guards alive. Safe to call every time it is opened.
    /// </summary>
    public void Opened()
    {
        if (nview != null && nview.IsValid() && !IsDone)
        {
            nview.InvokeRPC(DoneRpc);
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        container = GetComponent<Container>();
        if (nview == null || nview.GetZDO() == null || container == null)
        {
            enabled = false;
            return;
        }

        nview.Register(DoneRpc, RPC_Done);
        nextCheck = Time.time + Random.Range(0f, CheckInterval);
    }

    // Once a second, on the owner: fills the chest the first time, and removes it when it is sacked and empty. The
    // chest may have no owner when it appears (the server made it), so this waits for one instead of acting in Start.
    private void Update()
    {
        if (Time.time < nextCheck || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        ZDO zdo = nview.GetZDO();
        if (!zdo.GetBool(filledKey))
        {
            zdo.Set(filledKey, true);
            TreasureLoot.Roll(container.GetInventory(), SeaBattleSettings.Raids.Loot.Value, SeaBattleSettings.Raids.LootRolls.Value);
            container.Save();
            return;
        }

        if (zdo.GetBool(DoneKey) && container.GetInventory().NrOfItems() == 0 && !container.IsInUse())
        {
            ZNetScene.instance.Destroy(gameObject);
        }
    }

    // On the owner: the harbour is sacked. It is announced once, and only if no guard lives here either, so a chest
    // opened by a modified client that skipped the lock does not count.
    private void RPC_Done(long sender)
    {
        if (!nview.IsValid() || !nview.IsOwner() || IsDone || GuardsAlive())
        {
            return;
        }

        nview.GetZDO().Set(DoneKey, true);
        SagaLog.Announce(SagaKind.Raid, "$whitehilt_raid_pin", transform.position);
    }
}
