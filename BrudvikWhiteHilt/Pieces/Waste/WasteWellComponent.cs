using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Waste;

/// <summary>
/// Makes what is thrown into the Waste Well vanish a few seconds after it is closed, and, when switched on for the well,
/// collects items that have lain on the ground nearby for a while. Runs on the machine that owns the well; opening a
/// container makes the opener its owner.
/// </summary>
public class WasteWellComponent : MonoBehaviour
{
    private const string ToggleRpc = "WhiteHiltWasteWellToggle";
    private const float CollectInterval = 2f;
    private const float MessageRange = 10f;

    private static readonly int CollectKey = "whitehilt_wastewell_collect".GetStableHashCode();

    private ZNetView nview;
    private Container container;
    private float closedAt = -1f;
    private float nextCollect;

    /// <summary>
    /// True while the well collects items from the ground.
    /// </summary>
    public bool Collecting => nview != null && nview.IsValid() && nview.GetZDO().GetBool(CollectKey);

    /// <summary>
    /// Switches collecting from the ground on or off.
    /// </summary>
    public void ToggleCollecting()
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(ToggleRpc);
        }
    }

    /// <summary>
    /// The lines the well adds to the container's hover text.
    /// </summary>
    /// <returns>Localized text starting with a line break.</returns>
    public string GetHoverText()
    {
        string state = Collecting
            ? string.Format(Localization.instance.Localize("$whitehilt_wastewell_collect_on"), Mathf.RoundToInt(WasteWellSettings.CollectRadius.Value))
            : Localization.instance.Localize("$whitehilt_wastewell_collect_off");
        string hint = string.Format(Localization.instance.Localize("$whitehilt_wastewell_hint"), Mathf.RoundToInt(WasteWellSettings.DisposeDelaySeconds.Value));
        return Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_wastewell_toggle")
            + $"\n{state}\n<size=80%>{hint}</size>";
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        container = GetComponent<Container>();

        // The placement ghost has no ZDO.
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register(ToggleRpc, RPC_Toggle);
    }

    private void Update()
    {
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || container == null)
        {
            return;
        }

        Dispose();
        if (Collecting && Time.time >= nextCollect)
        {
            nextCollect = Time.time + CollectInterval;
            Collect();
        }
    }

    private void Dispose()
    {
        if (container.IsInUse())
        {
            closedAt = -1f;
            return;
        }

        Inventory inventory = container.GetInventory();
        if (inventory == null || inventory.NrOfItems() == 0)
        {
            closedAt = -1f;
            return;
        }

        if (closedAt < 0f)
        {
            closedAt = Time.time;
        }

        if (Time.time - closedAt < WasteWellSettings.DisposeDelaySeconds.Value)
        {
            return;
        }

        int count = 0;
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            count += item.m_stack;
        }

        inventory.RemoveAll();
        closedAt = -1f;
        Tell(count);
    }

    // Items lying still for a while, not decorations placed as pieces and not eggs that are hatching.
    private void Collect()
    {
        float radius = WasteWellSettings.CollectRadius.Value;
        float minAge = WasteWellSettings.MinItemAgeSeconds.Value;
        Vector3 here = transform.position;
        int count = 0;
        foreach (ItemDrop item in new List<ItemDrop>(ItemDrop.s_instances))
        {
            ZNetView itemView = item != null ? item.m_nview : null;
            if (itemView == null || !itemView.IsValid() || Vector3.Distance(item.transform.position, here) > radius
                || itemView.GetZDO().GetBool(ZDOVars.s_piece) || item.GetComponent<EggGrow>() != null
                || item.GetTimeSinceSpawned() < minAge || WasteWellSettings.IsKept(Utils.GetPrefabName(item.gameObject)))
            {
                continue;
            }

            count += item.m_itemData.m_stack;
            if (!itemView.IsOwner())
            {
                itemView.ClaimOwnership();
            }

            ZNetScene.instance.Destroy(item.gameObject);
        }

        Tell(count);
    }

    private void Tell(int count)
    {
        Player player = Player.m_localPlayer;
        if (count > 0 && player != null && Vector3.Distance(player.transform.position, transform.position) <= MessageRange)
        {
            player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_wastewell_gone"), count));
        }
    }

    private void RPC_Toggle(long sender)
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(CollectKey, !nview.GetZDO().GetBool(CollectKey));
        }
    }
}
