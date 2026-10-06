using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// A second chest on the White Hilt Ship, next to the helm. It is a vanilla <see cref="Container"/> on the ship's own
/// network object, like the cargo hold, so it keeps its items under its own ZDO key and opens through its own RPCs;
/// the vanilla ones are taken by the hold.
/// </summary>
public class ShipChest : MonoBehaviour
{
    /// <summary>
    /// ZDO key of the chest's items.
    /// </summary>
    public static readonly int ItemsKey = "whitehilt_ship_chest_items".GetStableHashCode();

    private const string OpenRpc = "WhiteHiltShipChestOpen";
    private const string OpenedRpc = "WhiteHiltShipChestOpened";

    /// <summary>
    /// Replaces the vanilla setup of the chest's container: the same inventory and change checks, but its own RPCs.
    /// </summary>
    /// <param name="container">The chest's container.</param>
    public static void Setup(Container container)
    {
        container.m_nview = container.m_rootObjectOverride != null
            ? container.m_rootObjectOverride.GetComponent<ZNetView>()
            : container.GetComponentInParent<ZNetView>();
        if (container.m_nview == null || container.m_nview.GetZDO() == null)
        {
            return;
        }

        container.m_inventory = new Inventory(container.m_name, container.m_bkg, container.m_width, container.m_height);
        container.m_inventory.m_onChanged += container.OnContainerChanged;
        container.m_nview.Register<long>(OpenRpc, (sender, playerID) => RequestOpen(container, sender));
        container.m_nview.Register<bool>(OpenedRpc, (_, granted) => Opened(container, granted));
        WearNTear wearNTear = container.m_nview.GetComponent<WearNTear>();
        if (wearNTear != null)
        {
            wearNTear.m_onDestroyed += container.OnDestroyed;
        }

        container.InvokeRepeating(nameof(Container.CheckForChanges), 0f, 1f);
    }

    /// <summary>
    /// Asks the ship's owner to open the chest.
    /// </summary>
    /// <param name="container">The chest's container.</param>
    public static void Open(Container container)
    {
        container.m_nview.InvokeRPC(OpenRpc, Game.instance.GetPlayerProfile().GetPlayerID());
    }

    /// <summary>
    /// Writes the items under the chest's own key.
    /// </summary>
    /// <param name="container">The chest's container.</param>
    public static void Save(Container container)
    {
        ZPackage package = new();
        container.m_inventory.Save(package);
        container.m_nview.GetZDO().Set(ItemsKey, package.GetArray());
        container.m_lastRevision = container.m_nview.GetZDO().DataRevision;
    }

    /// <summary>
    /// Reads the items from the chest's own key when the ship's data changed.
    /// </summary>
    /// <param name="container">The chest's container.</param>
    /// <returns>True if the data changed.</returns>
    public static bool Load(Container container)
    {
        ZDO zdo = container.m_nview.GetZDO();
        if (zdo.DataRevision == container.m_lastRevision || container.m_inUse)
        {
            return false;
        }

        container.m_lastRevision = zdo.DataRevision;
        byte[] data = zdo.GetByteArray(ItemsKey);
        if (data == null)
        {
            return true;
        }

        container.m_loading = true;
        container.m_inventory.Load(new ZPackage(data));
        container.m_loading = false;
        return true;
    }

    // On the ship's owner: hands the ship over to the player opening the sea chest, with its newest data, unless
    // someone else has it open. The chest is part of the ship's network object, so opening it means owning the ship.
    private static void RequestOpen(Container container, long sender)
    {
        ZNetView nview = container.m_nview;
        if (!nview.IsOwner())
        {
            return;
        }

        if (container.IsInUse() && sender != ZNet.GetUID())
        {
            nview.InvokeRPC(sender, OpenedRpc, false);
            return;
        }

        nview.GetComponent<ShipAssist>()?.ReserveContainerOwnership();
        ZDOMan.instance.ForceSendZDO(sender, nview.GetZDO().m_uid);
        nview.GetZDO().SetOwner(sender);
        nview.InvokeRPC(sender, OpenedRpc, true);
    }

    private static void Opened(Container container, bool granted)
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }

        if (granted)
        {
            InventoryGui.instance.Show(container);
        }
        else
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
        }
    }
}
