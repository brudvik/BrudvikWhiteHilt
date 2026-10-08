using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Fishing;

/// <summary>
/// On a Net Winch: takes the catch from the Shore Nets around it and puts it in its barrel. Runs on the machine
/// that owns the winch; the time since the last check is kept in the ZDO, so the nets go on catching for a while
/// when nobody is near.
/// </summary>
public class NetWinchComponent : MonoBehaviour
{
    private const float TickSeconds = 5f;
    private const int MaxCatchesPerTick = 200;
    private const string MendRpc = "WhiteHiltNetWinchMend";
    private const string ClaimRpc = "WhiteHiltNetWinchClaim";
    private static readonly int LastKey = "whitehilt_netwinch_last".GetStableHashCode();
    private static readonly int ProgressKey = "whitehilt_netwinch_progress".GetStableHashCode();
    private static readonly int WearKey = "whitehilt_netwinch_wear".GetStableHashCode();
    private static readonly int UnclaimedKey = "whitehilt_netwinch_unclaimed".GetStableHashCode();

    private static readonly HashSet<NetWinchComponent> winches = new();

    private Container container;
    private ZNetView nview;

    /// <summary>
    /// Where the rope to the nets leaves the winch drum, in world space.
    /// </summary>
    public Vector3 RopePoint => transform.TransformPoint(NetWinch.RopePoint);

    private int Wear => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(WearKey) : 0;

    private bool Torn => FishingNetSettings.WearCatches.Value > 0 && Wear >= FishingNetSettings.WearCatches.Value;

    /// <summary>
    /// The nearest winch within <see cref="FishingNetSettings.Range"/> of a position, measured flat.
    /// </summary>
    /// <param name="position">The net.</param>
    /// <returns>The winch, or null.</returns>
    public static NetWinchComponent Nearest(Vector3 position)
    {
        float range = FishingNetSettings.Range.Value;
        NetWinchComponent nearest = null;
        float best = range * range;
        foreach (NetWinchComponent winch in winches)
        {
            if (winch == null)
            {
                continue;
            }

            Vector3 offset = winch.transform.position - position;
            float distance = offset.x * offset.x + offset.z * offset.z;
            if (distance <= best)
            {
                best = distance;
                nearest = winch;
            }
        }

        return nearest;
    }

    /// <summary>
    /// The nets this winch takes the catch from: the nearest ones that have it as their winch, at most
    /// <see cref="FishingNetSettings.MaxNets"/>.
    /// </summary>
    /// <returns>The nets.</returns>
    public List<FishingNetComponent> Nets()
    {
        return FishingNetComponent.All
            .Where(net => net != null && net.Winch() == this)
            .OrderBy(net => (net.transform.position - transform.position).sqrMagnitude)
            .Take(FishingNetSettings.MaxNets.Value)
            .ToList();
    }

    /// <summary>
    /// Whether another net can be tied to this winch.
    /// </summary>
    /// <returns>True while it has room for one more.</returns>
    public bool HasRoom()
    {
        return FishingNetComponent.All.Count(net => net != null && net.Winch() == this) < FishingNetSettings.MaxNets.Value;
    }

    /// <summary>
    /// What the winch and its nets are doing, for the hover text.
    /// </summary>
    /// <returns>Localized lines.</returns>
    public string StatusText()
    {
        List<FishingNetComponent> nets = Nets();
        Localization localization = Localization.instance;
        string text;
        if (nets.Count == 0)
        {
            text = string.Format(localization.Localize("$whitehilt_netwinch_nonets"), Translations.Number(FishingNetSettings.Range.Value));
        }
        else
        {
            float perHour = CatchRate(nets, HasBait()) * 60f;
            text = string.Format(localization.Localize("$whitehilt_netwinch_nets"), nets.Count, FishingNetSettings.MaxNets.Value, perHour.ToString("0.#"));
        }

        int bait = BaitCount();
        if (bait > 0)
        {
            text += "\n" + string.Format(localization.Localize("$whitehilt_netwinch_bait"), bait);
        }

        Inventory inventory = container != null ? container.GetInventory() : null;
        if (inventory != null && !inventory.HaveEmptySlot())
        {
            text += "\n" + localization.Localize("$whitehilt_netwinch_full");
        }

        int wearLimit = FishingNetSettings.WearCatches.Value;
        if (Torn)
        {
            text += "\n<color=orange>" + localization.Localize("$whitehilt_netwinch_torn") + "</color>";
        }
        else if (wearLimit > 0 && Wear > 0)
        {
            text += "\n" + string.Format(localization.Localize("$whitehilt_netwinch_wear"), Wear, wearLimit);
        }

        string result = $"\n<color=#a0a0a0>{text}</color>";
        if (Wear > 0)
        {
            List<(string SharedName, int Amount)> costs = FishingNetSettings.MendCosts();
            string mend = costs.Count > 0 ? string.Format(localization.Localize("$whitehilt_netwinch_mend_cost"), CostText(costs)) : localization.Localize("$whitehilt_netwinch_mend");
            result += "\n" + localization.Localize("[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] ") + mend;
        }

        return result;
    }

    /// <summary>
    /// Mends the nets with the player's materials and asks the owner to reset the wear.
    /// </summary>
    /// <param name="player">The player mending.</param>
    /// <returns>True, the interaction is handled.</returns>
    public bool Mend(Player player)
    {
        if (player == null || nview == null || !nview.IsValid())
        {
            return true;
        }

        if (Wear == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_netwinch_whole");
            return true;
        }

        List<(string SharedName, int Amount)> costs = FishingNetSettings.MendCosts();
        if (costs.Count > 0)
        {
            // The first of the costs the player can pay, from the inventory and nearby chests as crafting does; mended
            // again once what lay in other players' chests has arrived.
            string need = string.Format(Localization.instance.Localize("$msg_whitehilt_netwinch_need"), CostText(costs));
            int index = costs.FindIndex(cost => ChestCost.Have(player, NearbyContainers.Use.Crafting, cost.SharedName) >= cost.Amount);
            if (index < 0)
            {
                player.Message(MessageHud.MessageType.Center, need);
                return true;
            }

            if (!ChestCost.Pay(player, NearbyContainers.Use.Crafting, new[] { costs[index] }, () =>
                {
                    if (this != null)
                    {
                        Mend(player);
                    }
                }, need))
            {
                return true;
            }
        }

        nview.InvokeRPC(MendRpc);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_netwinch_mended");
        return true;
    }

    /// <summary>
    /// Gives the player Fishing experience for the fish caught since the barrel was last opened.
    /// </summary>
    /// <param name="player">The player who opened the barrel.</param>
    public void ClaimSkill(Player player)
    {
        if (player == null || nview == null || !nview.IsValid())
        {
            return;
        }

        int unclaimed = nview.GetZDO().GetInt(UnclaimedKey);
        if (unclaimed <= 0)
        {
            return;
        }

        if (FishingNetSettings.SkillRaise.Value > 0f)
        {
            player.RaiseSkill(Skills.SkillType.Fishing, FishingNetSettings.SkillRaise.Value * unclaimed);
        }

        nview.InvokeRPC(ClaimRpc);
    }

    private void Awake()
    {
        container = GetComponent<Container>();
        nview = GetComponent<ZNetView>();
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        winches.Add(this);
        nview.Register(MendRpc, _ => OnMend());
        nview.Register(ClaimRpc, _ => OnClaim());
        InvokeRepeating(nameof(Tick), TickSeconds, TickSeconds);
    }

    private void OnDestroy()
    {
        winches.Remove(this);
    }

    private void OnMend()
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(WearKey, 0);
        }
    }

    private void OnClaim()
    {
        if (nview.IsOwner())
        {
            nview.GetZDO().Set(UnclaimedKey, 0);
        }
    }

    private static float CatchRate(List<FishingNetComponent> nets, bool bait)
    {
        float rate = nets.Sum(net => net.Catching()) / FishingNetSettings.Minutes.Value;
        return bait ? rate / FishingNetSettings.BaitTime.Value : rate;
    }

    private static string CostText(List<(string SharedName, int Amount)> costs)
    {
        Localization localization = Localization.instance;
        return string.Join(localization.Localize("$whitehilt_netwinch_or"), costs.Select(cost => $"{cost.Amount} {localization.Localize(cost.SharedName)}"));
    }

    private ItemDrop.ItemData FindBait()
    {
        Inventory inventory = container != null ? container.GetInventory() : null;
        return inventory?.GetAllItems().FirstOrDefault(item => item.m_dropPrefab != null && FishingNetSettings.IsBait(item.m_dropPrefab.name));
    }

    private bool HasBait()
    {
        return FindBait() != null;
    }

    private int BaitCount()
    {
        Inventory inventory = container != null ? container.GetInventory() : null;
        return inventory == null ? 0 : inventory.GetAllItems()
            .Where(item => item.m_dropPrefab != null && FishingNetSettings.IsBait(item.m_dropPrefab.name))
            .Sum(item => item.m_stack);
    }

    // On the owner, while nobody has the winch open: adds up the time since the last tick, capped so a long absence
    // does not give a huge catch at once, and catches fish at the rate the nets in reach give. Progress is kept in the
    // network data, so it survives the area unloading.
    private void Tick()
    {
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || container == null || container.IsInUse() || ZNet.instance == null)
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        long now = ZNet.instance.GetTime().Ticks;
        long last = zdo.GetLong(LastKey);
        zdo.Set(LastKey, now);
        if (last == 0L || now <= last)
        {
            return;
        }

        // Away time beyond one tick only counts up to the catch-up limit.
        float minutes = (float)TimeSpan.FromTicks(now - last).TotalMinutes;
        minutes = Mathf.Min(minutes, Mathf.Max(FishingNetSettings.CatchUpHours.Value * 60f, TickSeconds * 2f / 60f));
        List<FishingNetComponent> nets = Nets();
        float rate = CatchRate(nets, HasBait());
        if (rate <= 0f || Torn)
        {
            return;
        }

        float progress = zdo.GetFloat(ProgressKey) + minutes * rate;
        Inventory inventory = container.GetInventory();
        for (int caught = 0; progress >= 1f && caught < MaxCatchesPerTick; caught++)
        {
            if (Torn || !Catch(inventory, PickNet(nets)))
            {
                progress = 1f;
                break;
            }

            progress -= 1f;
        }

        zdo.Set(ProgressKey, Mathf.Min(progress, 1f));
    }

    private static FishingNetComponent PickNet(List<FishingNetComponent> nets)
    {
        float roll = UnityEngine.Random.value * nets.Sum(net => net.Catching());
        foreach (FishingNetComponent net in nets)
        {
            roll -= net.Catching();
            if (roll <= 0f)
            {
                return net;
            }
        }

        return nets[nets.Count - 1];
    }

    // Catches one fish of the net's biome, sometimes a bigger one, and uses a bait if there is one. Each catch wears
    // the net, and bycatch may add seaweed or, at sea, an amber pearl.
    private bool Catch(Inventory inventory, FishingNetComponent net)
    {
        Heightmap.Biome biome = WorldGenerator.instance.GetBiome(net.transform.position);
        string fishName = FishTable.Pick(biome);
        GameObject fish = ObjectDB.instance.GetItemPrefab(fishName);
        if (fish == null)
        {
            return false;
        }

        int quality = UnityEngine.Random.value < FishingNetSettings.BiggerFishChance.Value ? 2 : 1;
        if (!CanTake(inventory, fish, quality) || inventory.AddItem(fishName, 1, quality, 0, 0L, string.Empty, false) == null)
        {
            return false;
        }

        ItemDrop.ItemData bait = FindBait();
        if (bait != null)
        {
            inventory.RemoveItem(bait, 1);
        }

        ZDO zdo = nview.GetZDO();
        zdo.Set(WearKey, zdo.GetInt(WearKey) + 1);
        zdo.Set(UnclaimedKey, zdo.GetInt(UnclaimedKey) + 1);
        if (FishingNetSettings.Bycatch.Value)
        {
            if (UnityEngine.Random.value < FishingNetSettings.SeaweedChance.Value)
            {
                AddBycatch(inventory, "FreshSeaweed");
            }

            if (biome == Heightmap.Biome.Ocean && UnityEngine.Random.value < FishingNetSettings.PearlChance.Value)
            {
                AddBycatch(inventory, "AmberPearl");
            }
        }

        return true;
    }

    private static bool CanTake(Inventory inventory, GameObject prefab, int quality)
    {
        string sharedName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        return inventory.HaveEmptySlot() || inventory.GetAllItems()
            .Any(item => item.m_shared.m_name == sharedName && item.m_quality == quality && item.m_stack < item.m_shared.m_maxStackSize);
    }

    private static void AddBycatch(Inventory inventory, string prefabName)
    {
        GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName);
        if (prefab != null && inventory.CanAddItem(prefab, 1))
        {
            inventory.AddItem(prefab, 1);
        }
    }
}
