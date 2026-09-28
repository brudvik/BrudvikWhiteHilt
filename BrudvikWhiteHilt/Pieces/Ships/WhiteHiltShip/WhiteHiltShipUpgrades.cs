using BrudvikWhiteHilt.Items.ShipUpgrades;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// Holds the upgrades used on a White Hilt Ship and switches the matching parts on: a lantern that lights at night,
/// barrels with a bigger cargo hold, a tent that gives shelter and a mast wisp that clears the mist.
/// The upgrades are a bit mask in the ship's ZDO, so every player sees the same ship.
/// </summary>
public class WhiteHiltShipUpgrades : MonoBehaviour
{
    /// <summary>
    /// Name of the mast wisp object added to the ship prefab.
    /// </summary>
    public const string MastWispName = "WhiteHiltMastWisp";

    /// <summary>
    /// Cargo hold size without barrels (vanilla longship).
    /// </summary>
    public static readonly Vector2i SmallHold = new(6, 3);

    /// <summary>
    /// Cargo hold size with barrels. The prefab's container has this size, so saved cargo always fits when loading.
    /// </summary>
    public static readonly Vector2i LargeHold = new(8, 4);

    /// <summary>
    /// Centre of the area under the tent, in ship-root space. Set when the prefab is built.
    /// </summary>
    public Vector3 m_tentCenter;

    /// <summary>
    /// Size of the area under the tent, in ship-root space. Set when the prefab is built.
    /// </summary>
    public Vector3 m_tentSize;

    private const string ZdoKey = "whitehilt_ship_upgrades";
    private const string AddRpc = "WhiteHiltShipAddUpgrade";
    private const string TakeRpc = "WhiteHiltShipTakeUpgrade";

    private static readonly List<WhiteHiltShipUpgrades> instances = new();

    private ZNetView nview;
    private Container container;
    private GameObject lantern;
    private GameObject lanternLight;
    private GameObject[] barrels = new GameObject[0];
    private GameObject[] tent = new GameObject[0];
    private GameObject mastWisp;
    private int shownMask = -1;
    private float holdCheckTimer;

    /// <summary>
    /// Bit mask of the upgrades on the ship.
    /// </summary>
    public int Mask => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(ZdoKey) : 0;

    /// <summary>
    /// Returns the upgraded ship with a tent over the given point, if any.
    /// </summary>
    /// <param name="point">World position, e.g. a player's centre.</param>
    /// <returns>True if the point is under the tent of a ship with the tent upgrade.</returns>
    public static bool IsUnderTent(Vector3 point)
    {
        foreach (WhiteHiltShipUpgrades ship in instances)
        {
            if (ship == null || !ship.Has(ShipTent.Bit))
            {
                continue;
            }

            Vector3 local = ship.transform.InverseTransformPoint(point);
            if (new Bounds(ship.m_tentCenter, ship.m_tentSize).Contains(local))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether the ship has an upgrade.
    /// </summary>
    /// <param name="bit">The upgrade's bit.</param>
    /// <returns>True if the upgrade is on the ship.</returns>
    public bool Has(int bit)
    {
        return (Mask & (1 << bit)) != 0;
    }

    /// <summary>
    /// Hover text for the mast.
    /// </summary>
    /// <param name="shipName">Name of the ship.</param>
    /// <returns>The localized text.</returns>
    public string GetHoverText(string shipName)
    {
        int mask = Mask;
        string text = $"{shipName}\n";
        if (mask != 0)
        {
            text += "[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_ship_take\n";
        }

        text += "[<color=yellow><b>1-8</b></color>] $whitehilt_ship_add\n";
        text += mask == 0 ? "$whitehilt_ship_none" : $"$whitehilt_ship_upgrades: {UpgradeNames(mask)}";
        return Localization.instance.Localize(text);
    }

    /// <summary>
    /// Uses an upgrade item on the ship.
    /// </summary>
    /// <param name="user">The player.</param>
    /// <param name="item">The item used.</param>
    /// <returns>True if the item was an upgrade.</returns>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        WhiteHiltShipUpgradeBase upgrade = WhiteHiltShipUpgradeBase.FromItem(item);
        if (upgrade == null)
        {
            return false;
        }

        if (Has(upgrade.Index))
        {
            user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ship_already");
            return true;
        }

        user.GetInventory().RemoveOneItem(item);
        nview.InvokeRPC(AddRpc, upgrade.Index);
        user.Message(MessageHud.MessageType.Center, Localization.instance.Localize($"$msg_whitehilt_ship_added: {upgrade.NameToken}"));
        return true;
    }

    /// <summary>
    /// Takes the last upgrade off the ship.
    /// </summary>
    /// <param name="user">The player.</param>
    /// <returns>True if an upgrade was taken off or the player was told why not.</returns>
    public bool TakeLast(Humanoid user)
    {
        int mask = Mask;
        if (mask == 0)
        {
            return false;
        }

        int bit = Enumerable.Range(0, WhiteHiltShipUpgradeBase.Count).Last(i => (mask & (1 << i)) != 0);
        if (bit == ShipBarrels.Bit && CargoOutside(SmallHold))
        {
            user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ship_barrels_full");
            return true;
        }

        nview.InvokeRPC(TakeRpc, bit);
        return true;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        container = GetComponentInChildren<Container>(true);

        Transform customize = transform.Find("ship/visual/Customize");
        Transform storage = customize?.Find("storage");
        lantern = customize?.Find("TraderLamp")?.gameObject;
        lanternLight = lantern != null ? lantern.GetComponentInChildren<Light>(true)?.gameObject : null;
        barrels = storage != null ? storage.Cast<Transform>().Where(child => !child.name.StartsWith("Shield")).Select(child => child.gameObject).ToArray() : barrels;
        tent = customize != null ? customize.Cast<Transform>().Where(IsTentPart).Select(child => child.gameObject).ToArray() : tent;
        mastWisp = GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == MastWispName)?.gameObject;

        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<int>(AddRpc, RPC_Add);
        nview.Register<int>(TakeRpc, RPC_Take);
        WearNTear wearNTear = GetComponent<WearNTear>();
        if (wearNTear != null)
        {
            wearNTear.m_onDestroyed += DropAll;
        }

        instances.Add(this);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    private void Update()
    {
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        int mask = Mask;
        if (mask != shownMask)
        {
            shownMask = mask;
            SetActive(lantern, Has(ShipLantern.Bit));
            foreach (GameObject part in barrels)
            {
                SetActive(part, Has(ShipBarrels.Bit));
            }

            foreach (GameObject part in tent)
            {
                SetActive(part, Has(ShipTent.Bit));
            }

            SetActive(mastWisp, Has(ShipMastWisp.Bit));
            holdCheckTimer = 0f;
        }

        // The hold and the lantern are checked once a second; the hold only after the container has loaded its cargo.
        holdCheckTimer -= Time.deltaTime;
        if (holdCheckTimer <= 0f)
        {
            holdCheckTimer = 1f;
            UpdateHoldSize();
            SetActive(lanternLight, EnvMan.IsNight());
        }
    }

    private void UpdateHoldSize()
    {
        Inventory inventory = container?.GetInventory();
        if (inventory == null)
        {
            return;
        }

        Vector2i size = Has(ShipBarrels.Bit) ? LargeHold : SmallHold;
        if (inventory.m_width == size.x && inventory.m_height == size.y)
        {
            return;
        }

        // Never shrink over cargo; it would become unreachable.
        if (size.x < inventory.m_width && CargoOutside(size))
        {
            return;
        }

        inventory.m_width = size.x;
        inventory.m_height = size.y;
        inventory.Changed();
    }

    private bool CargoOutside(Vector2i size)
    {
        Inventory inventory = container?.GetInventory();
        return inventory != null && inventory.GetAllItems().Any(item => item.m_gridPos.x >= size.x || item.m_gridPos.y >= size.y);
    }

    private void RPC_Add(long sender, int bit)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        int mask = Mask;
        if ((mask & (1 << bit)) != 0)
        {
            // Two players added the same upgrade at once: give the second one back.
            Drop(bit);
            return;
        }

        nview.GetZDO().Set(ZdoKey, mask | (1 << bit));
    }

    private void RPC_Take(long sender, int bit)
    {
        if (!nview.IsOwner() || (Mask & (1 << bit)) == 0)
        {
            return;
        }

        nview.GetZDO().Set(ZdoKey, Mask & ~(1 << bit));
        Drop(bit);
    }

    private void DropAll()
    {
        int mask = Mask;
        for (int bit = 0; bit < WhiteHiltShipUpgradeBase.Count; bit++)
        {
            if ((mask & (1 << bit)) != 0)
            {
                Drop(bit);
            }
        }

        nview.GetZDO().Set(ZdoKey, 0);
    }

    private void Drop(int bit)
    {
        GameObject prefab = ZNetScene.instance.GetPrefab(WhiteHiltShipUpgradeBase.Get(bit)?.PrefabName ?? string.Empty);
        if (prefab != null)
        {
            Instantiate(prefab, transform.position + Vector3.up * 2f, Quaternion.identity);
        }
    }

    private static bool IsTentPart(Transform child)
    {
        // The open tent sides are off on the vanilla longship too.
        return child.name.StartsWith("ShipTen") && child.name != "ShipTentLeft" && child.name != "ShipTentRight";
    }

    private static void SetActive(GameObject part, bool active)
    {
        if (part != null && part.activeSelf != active)
        {
            part.SetActive(active);
        }
    }

    private static string UpgradeNames(int mask)
    {
        return string.Join(", ", Enumerable.Range(0, WhiteHiltShipUpgradeBase.Count)
            .Where(bit => (mask & (1 << bit)) != 0)
            .Select(bit => WhiteHiltShipUpgradeBase.Get(bit)?.NameToken)
            .Where(name => name != null));
    }
}
