using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// A door or window a <see cref="StormBar"/> may hold. While it holds, the use key does not open it but pounds on it:
/// everyone near hears the blows, and those within the bar's reach are told who stands outside.
/// </summary>
public class StormBarredDoor : MonoBehaviour
{
    private const string PoundRpc = "WhiteHiltDoorPound";
    private const float PoundSeconds = 0.6f;
    private const float HeardWithin = 30f;

    private static float lastPound;

    private Door door;
    private ZNetView view;

    /// <summary>
    /// Pounds on the door, if a bar holds it.
    /// </summary>
    /// <param name="player">The one who pounds.</param>
    /// <returns>True when the bar holds it, so it was not opened.</returns>
    public bool TryPound(Player player)
    {
        if (StormBar.Holding(door) == null)
        {
            return false;
        }

        if (Time.time - lastPound >= PoundSeconds)
        {
            lastPound = Time.time;
            view.InvokeRPC(ZNetView.Everybody, PoundRpc, player.GetPlayerName());
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_door_barred");
        }

        return true;
    }

    /// <summary>
    /// The hover text of a door a bar holds.
    /// </summary>
    /// <returns>The localized text, or null when no bar holds it.</returns>
    public string HoverText()
    {
        if (StormBar.Holding(door) == null)
        {
            return null;
        }

        return Localization.instance.Localize($"{door.m_name}\n<color=orange>$whitehilt_door_barred</color>"
            + "\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_door_pound");
    }

    private void Awake()
    {
        door = GetComponent<Door>();
        view = GetComponent<ZNetView>();
        if (view != null && view.IsValid())
        {
            view.Register<string>(PoundRpc, RPC_Pound);
        }
    }

    // On every machine: the blows on the wood, and word to those inside.
    private void RPC_Pound(long sender, string name)
    {
        Player player = Player.m_localPlayer;
        if (player == null || Vector3.Distance(player.transform.position, transform.position) > HeardWithin)
        {
            return;
        }

        WearNTear wear = GetComponent<WearNTear>();
        if (wear != null)
        {
            wear.m_hitEffect.Create(transform.position + Vector3.up, Quaternion.identity);
        }

        if (sender != ZDOMan.GetSessionID() && StormBar.BarredAt(player.transform.position))
        {
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_whitehilt_door_pounding", name));
        }
    }
}
