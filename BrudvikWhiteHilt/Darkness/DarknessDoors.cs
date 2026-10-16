using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// Doors that move by themselves in the dark: as the black falls an open door slams shut close by, or a shut one swings
/// open, and now and then again while it lasts. Only the place's own doors, never a locked one or one a player built.
/// It goes through the game's own door, so everyone hears it and sees it move; the player closest to a door moves it,
/// so it moves once. On the <see cref="Game"/> object.
/// </summary>
public class DarknessDoors : MonoBehaviour
{
    private const float Reach = 18f;
    private const float Nearest = 3f;
    private const float AgainChance = 0.4f;

    private DarkPhase shownPhase;
    private float nextDoor;

    private void Update()
    {
        Player player = Player.m_localPlayer;
        DarkPhase phase = DarknessClient.Phase;
        if (player == null || player.IsDead() || DarknessClient.PlaceKey == null)
        {
            shownPhase = phase;
            return;
        }

        if (phase == DarkPhase.Black && shownPhase != DarkPhase.Black)
        {
            Move(player);
            nextDoor = Time.time + Random.Range(12f, 22f);
        }
        else if (phase == DarkPhase.Black && Time.time >= nextDoor)
        {
            nextDoor = Time.time + Random.Range(12f, 22f);
            if (Random.value < AgainChance)
            {
                Move(player);
            }
        }

        shownPhase = phase;
    }

    // Shuts an open door if there is one, else opens a shut one.
    private static void Move(Player player)
    {
        List<Door> doors = FindObjectsByType<Door>(FindObjectsSortMode.None).Where(door => Movable(door, player)).ToList();
        if (doors.Count == 0)
        {
            return;
        }

        List<Door> open = doors.Where(door => door.m_nview.GetZDO().GetInt(ZDOVars.s_state) != 0).ToList();
        Door chosen = (open.Count > 0 ? open : doors)[Random.Range(0, open.Count > 0 ? open.Count : doors.Count)];
        Vector3 away = chosen.transform.position - player.transform.position;
        chosen.m_nview.InvokeRPC("UseDoor", Vector3.Dot(chosen.transform.forward, away) > 0f);
    }

    private static bool Movable(Door door, Player player)
    {
        if (door == null || door.m_nview == null || !door.m_nview.IsValid() || door.m_keyItem != null || door.m_canNotBeClosed)
        {
            return false;
        }

        float distance = Vector3.Distance(door.transform.position, player.transform.position);
        Piece piece = door.GetComponent<Piece>();
        return distance >= Nearest && distance <= Reach && (piece == null || !piece.IsPlacedByPlayer())
            && Player.GetClosestPlayer(door.transform.position, Reach * 2f) == player;
    }
}
