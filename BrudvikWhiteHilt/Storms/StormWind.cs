using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// What the wind of a raging storm does besides the cold: it puts out fires with no roof over them and the torch in
/// your hand, tears doors open so the storm comes in, and pushes you about in the open, now and then off your feet.
/// Each client looks after the fires it owns and the doors it is the closest player to, so nothing happens twice.
/// On the <see cref="Game"/> object.
/// </summary>
public class StormWind : MonoBehaviour
{
    private const float FireReach = 40f;
    private const float FireSeconds = 3f;
    private const float FireOutChance = 0.3f;
    private const float FireMessageReach = 15f;
    private const float TorchChance = 0.12f;
    private const float DoorReach = 15f;
    private const float DoorInside = 8f;
    private const float GustPush = 40f;
    private const float KnockDownChance = 0.2f;

    private const float DoorScanSeconds = 2f;

    private static readonly List<Door> doorsNear = new();

    private float nextFires;
    private float nextDoor;
    private float nextGust;
    private float nextDoorScan;
    private bool wasRaging;

    /// <summary>
    /// Whether a door close by stands open, letting a raging storm into the room.
    /// </summary>
    /// <param name="position">Where the player is.</param>
    /// <returns>True if an open door is near.</returns>
    public static bool OpenDoorNear(Vector3 position)
    {
        return StormSettings.DoorsBlowOpen != null && StormSettings.DoorsBlowOpen.Value && doorsNear.Exists(door => door != null && door.m_nview != null
            && door.m_nview.IsValid() && door.m_nview.GetZDO().GetInt(ZDOVars.s_state) != 0
            && Vector3.Distance(door.transform.position, position) <= DoorInside);
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool raging = player != null && StormClient.RagingHere && !player.IsDead();
        if (!raging)
        {
            wasRaging = false;
            doorsNear.Clear();
            return;
        }

        if (Time.time >= nextDoorScan)
        {
            nextDoorScan = Time.time + DoorScanSeconds;
            doorsNear.Clear();
            doorsNear.AddRange(FindObjectsByType<Door>(FindObjectsSortMode.None)
                .Where(door => door != null && Vector3.Distance(door.transform.position, player.transform.position) <= DoorReach * 2f));
        }

        if (!wasRaging)
        {
            wasRaging = true;
            nextFires = Time.time + Random.Range(5f, 10f);
            nextDoor = Time.time + Random.Range(15f, 30f);
            nextGust = Time.time + Random.Range(10f, 20f);
        }

        // A fiercer storm blows more often.
        float often = Mathf.Max(1f, StormClient.Fury);
        if (StormSettings.FiresGoOut.Value && StormClient.Profile.Kind != StormKind.Fire && Time.time >= nextFires)
        {
            nextFires = Time.time + FireSeconds;
            PutOutFires(player);
            BlowOutTorch(player);
        }

        if (StormSettings.DoorsBlowOpen.Value && Time.time >= nextDoor)
        {
            nextDoor = Time.time + Random.Range(20f, 45f) / often;
            BlowDoorOpen(player);
        }

        if (StormSettings.Gusts.Value && Time.time >= nextGust)
        {
            nextGust = Time.time + Random.Range(12f, 30f) / often;
            Gust(player);
        }
    }

    // Fires this client owns, with fuel and no roof over them, go out one by one; the game shows them out for everyone.
    private static void PutOutFires(Player player)
    {
        bool told = false;
        foreach (Fireplace fire in FindObjectsByType<Fireplace>(FindObjectsSortMode.None))
        {
            if (fire == null || fire.m_infiniteFuel || fire.m_nview == null || !fire.m_nview.IsValid() || !fire.m_nview.IsOwner())
            {
                continue;
            }

            float distance = Vector3.Distance(fire.transform.position, player.transform.position);
            ZDO zdo = fire.m_nview.GetZDO();
            if (distance > FireReach || zdo.GetFloat(ZDOVars.s_fuel) <= 0f)
            {
                continue;
            }

            Cover.GetCoverForPoint(fire.transform.position + Vector3.up * fire.m_coverCheckOffset, out _, out bool underRoof);
            if (underRoof || Random.value > FireOutChance)
            {
                continue;
            }

            zdo.Set(ZDOVars.s_fuel, 0f);
            if (!told && distance <= FireMessageReach)
            {
                told = true;
                player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_storm_fire_out");
            }
        }
    }

    // A torch held out in the open blows out: it goes back in the bag, to be lit again under a roof.
    private static void BlowOutTorch(Player player)
    {
        if (StormClient.Cover is not (StormCover.Open or StormCover.Lee or StormCover.Forest) || Random.value > TorchChance)
        {
            return;
        }

        ItemDrop.ItemData torch = new[] { player.GetRightItem(), player.GetLeftItem() }.FirstOrDefault(IsTorch);
        if (torch != null)
        {
            player.UnequipItem(torch);
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_storm_torch_out");
        }
    }

    private static bool IsTorch(ItemDrop.ItemData item)
    {
        return item?.m_shared != null && (item.m_shared.m_name == "$item_torch"
            || (item.m_dropPrefab != null && item.m_dropPrefab.name.IndexOf("torch", System.StringComparison.OrdinalIgnoreCase) >= 0));
    }

    // A gust tears a shut door open near the player, through the game's own door so everyone sees and hears it. A locked
    // door holds, and so does one a Storm Bar bars.
    private static void BlowDoorOpen(Player player)
    {
        List<Door> doors = doorsNear.Where(door => door != null && door.m_nview != null && door.m_nview.IsValid()
                && door.m_keyItem == null && !door.m_canNotBeClosed && door.m_nview.GetZDO().GetInt(ZDOVars.s_state) == 0
                && StormBar.Holding(door) == null
                && Vector3.Distance(door.transform.position, player.transform.position) <= DoorReach
                && Player.GetClosestPlayer(door.transform.position, DoorReach * 2f) == player)
            .ToList();
        if (doors.Count == 0)
        {
            return;
        }

        Door chosen = doors[Random.Range(0, doors.Count)];
        Vector3 wind = EnvMan.instance != null ? EnvMan.instance.GetWindDir() : chosen.transform.forward;
        chosen.m_nview.InvokeRPC("UseDoor", Vector3.Dot(chosen.transform.forward, wind) > 0f);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_storm_door");
    }

    // A hard gust pushes the player along with the wind, out in the open only; now and then it knocks them down. Not at
    // the helm or seated.
    private static void Gust(Player player)
    {
        if (StormClient.Cover != StormCover.Open || player.IsAttached() || EnvMan.instance == null)
        {
            return;
        }

        Vector3 wind = EnvMan.instance.GetWindDir();
        player.ApplyPushback(wind, GustPush * Random.Range(0.6f, 1.2f) * Mathf.Max(1f, StormClient.Fury));
        if (Random.value < KnockDownChance * (StormState.Ragnarok ? 1.5f : 1f) && !player.IsBlocking())
        {
            player.Stagger(wind);
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_storm_knocked_down");
        }
    }
}
