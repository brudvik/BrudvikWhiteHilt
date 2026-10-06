using BrudvikWhiteHilt.Items.Foraging.Roseroot;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// Roseroot planted on a turf roof, as on the sod roofs of old: use a Roseroot on the roof to plant it, and pick it when
/// it has grown. It grows back after every pick.
/// </summary>
public class TurfGarden : MonoBehaviour, Hoverable, Interactable
{
    private const string PlantedKey = "whitehilt_garden_planted";
    private const string PlantRpc = "WhiteHiltGardenPlant";
    private const string PickRpc = "WhiteHiltGardenPick";
    private const string PickedRpc = "WhiteHiltGardenPicked";
    private const float RefreshSeconds = 1f;

    /// <summary>The plant models, hidden while nothing is planted.</summary>
    public GameObject m_plants;

    private ZNetView nview;
    private Piece piece;
    private float nextRefresh;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        if (nview != null && nview.GetZDO() != null)
        {
            nview.Register(PlantRpc, RPC_Plant);
            nview.Register(PickRpc, RPC_Pick);
            nview.Register<int>(PickedRpc, RPC_Picked);
        }

        Refresh();
    }

    private void Update()
    {
        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + RefreshSeconds;
            Refresh();
        }
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (piece == null || !piece.IsPlacedByPlayer() || !RoofSettings.GardenEnabled.Value)
        {
            return string.Empty;
        }

        string text = piece.m_name + "\n";
        if (PlantedTicks() == 0)
        {
            text += "[<color=yellow><b>1-8</b></color>] $whitehilt_roof_garden_plant";
        }
        else if (Growth() < 1f)
        {
            text += $"$whitehilt_roof_garden_growing ({Mathf.FloorToInt(Growth() * 100f)}%)";
        }
        else
        {
            text += "[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_roof_garden_pick";
        }

        return Localization.instance.Localize(text);
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? piece.m_name : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || nview == null || !nview.IsValid() || !RoofSettings.GardenEnabled.Value || PlantedTicks() == 0 || Growth() < 1f)
        {
            return false;
        }

        nview.InvokeRPC(PickRpc);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (nview == null || !nview.IsValid() || !RoofSettings.GardenEnabled.Value || item?.m_dropPrefab == null
            || item.m_dropPrefab.name != Roseroot.PrefabName || PlantedTicks() != 0)
        {
            return false;
        }

        if (!user.GetInventory().RemoveOneItem(item))
        {
            return false;
        }

        nview.InvokeRPC(PlantRpc);
        user.Message(MessageHud.MessageType.Center, "$whitehilt_roof_garden_planted");
        return true;
    }

    private long PlantedTicks()
    {
        return nview != null && nview.GetZDO() != null ? nview.GetZDO().GetLong(PlantedKey) : 0L;
    }

    private float Growth()
    {
        long planted = PlantedTicks();
        if (planted == 0 || ZNet.instance == null)
        {
            return 0f;
        }

        double minutes = (ZNet.instance.GetTime() - new DateTime(planted)).TotalMinutes;
        return Mathf.Clamp01((float)(minutes / Math.Max(1f, RoofSettings.GardenGrowMinutes.Value)));
    }

    private void RPC_Plant(long sender)
    {
        if (nview.IsOwner() && PlantedTicks() == 0)
        {
            nview.GetZDO().Set(PlantedKey, ZNet.instance.GetTime().Ticks);
        }
    }

    private void RPC_Pick(long sender)
    {
        if (!nview.IsOwner() || PlantedTicks() == 0 || Growth() < 1f)
        {
            return;
        }

        nview.GetZDO().Set(PlantedKey, ZNet.instance.GetTime().Ticks);
        nview.InvokeRPC(sender, PickedRpc, RoofSettings.GardenPickAmount.Value);
    }

    // On the picking player's machine: puts the roseroot picked from the turf roof into the inventory, dropping what
    // does not fit.
    private void RPC_Picked(long sender, int amount)
    {
        Player player = Player.m_localPlayer;
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Roseroot.PrefabName) : null;
        if (player == null || prefab == null || amount <= 0)
        {
            return;
        }

        if (!player.GetInventory().AddItem(prefab, amount))
        {
            for (int i = 0; i < amount; i++)
            {
                ItemDrop.OnCreateNew(Instantiate(prefab, player.transform.position + Vector3.up, Quaternion.identity));
            }
        }

        player.Message(MessageHud.MessageType.TopLeft, $"$msg_added {prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name} x{amount}");
    }

    // Shows the plants on the turf while it is planted, growing from small to full size; each plant scales on its own
    // pivot so it grows where it stands.
    private void Refresh()
    {
        if (m_plants == null)
        {
            return;
        }

        bool planted = PlantedTicks() != 0;
        if (m_plants.activeSelf != planted)
        {
            m_plants.SetActive(planted);
        }

        if (planted)
        {
            // Each plant stands on its own pivot, so it grows where it stands.
            Vector3 scale = Vector3.one * Mathf.Lerp(0.35f, 1f, Growth());
            foreach (Transform pivot in m_plants.transform)
            {
                pivot.localScale = scale;
            }
        }
    }
}
