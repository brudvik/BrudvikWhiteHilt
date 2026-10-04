using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Navigation;
using BrudvikWhiteHilt.Items.ShipUpgrades;
using BrudvikWhiteHilt.Pieces.Navigation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;

/// <summary>
/// Holds the upgrades used on a White Hilt Ship and switches the matching parts on: a lantern that lights at night,
/// barrels with a bigger cargo hold, a tent that gives shelter, a mast wisp that clears the mist, a fishing net that
/// fills the hold while sailing, an anchor that holds the ship still and a brazier that keeps the crew warm.
/// The upgrades are a bit mask in the ship's ZDO, so every player sees the same ship.
/// </summary>
public class WhiteHiltShipUpgrades : MonoBehaviour
{
    /// <summary>
    /// Name of the mast wisp object added to the ship prefab.
    /// </summary>
    public const string MastWispName = "WhiteHiltMastWisp";

    /// <summary>
    /// Name of the anchor object added to the ship prefab.
    /// </summary>
    public const string AnchorName = "WhiteHiltShipAnchor";

    /// <summary>
    /// Name of the deck brazier object added to the ship prefab.
    /// </summary>
    public const string BrazierName = "WhiteHiltShipBrazier";

    /// <summary>
    /// Name of the sea chest object added to the ship prefab.
    /// </summary>
    public const string ChestName = "WhiteHiltShipChest";

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

    private static readonly int ZdoKey = "whitehilt_ship_upgrades".GetStableHashCode();
    private const string AddRpc = "WhiteHiltShipAddUpgrade";
    private const string TakeRpc = "WhiteHiltShipTakeUpgrade";
    private static readonly int AnchorZdoKey = "whitehilt_ship_anchored".GetStableHashCode();
    private const string AnchorRpc = "WhiteHiltShipToggleAnchor";
    private static readonly int LanternZdoKey = "whitehilt_ship_lantern".GetStableHashCode();
    private const string LanternRpc = "WhiteHiltShipToggleLantern";
    private static float MinFishingSpeed => ShipSettings.FishingNetMinSpeed.Value;

    // At Fishing 100 the net catches twice as often, and half the catches are two fish (defaults).
    private static float SkillSpeedUp => ShipSettings.FishingNetSkillSpeedUp.Value;
    private static float SkillDoubleChance => ShipSettings.FishingNetDoubleChance.Value;
    private static float SkillRaise => ShipSettings.FishingNetSkillRaise.Value;
    private static float SeaweedChance => ShipSettings.FishingNetSeaweedChance.Value;
    private static float PearlChance => ShipSettings.FishingNetPearlChance.Value;
    private const float AnchorDrop = 2f;

    // The ship must lie empty and nearly still this long before the anchor drops on its own.
    private static float AutoAnchorSeconds => ShipSettings.AutoAnchorSeconds.Value;
    private static float AutoAnchorMaxSpeed => ShipSettings.AutoAnchorMaxSpeed.Value;

    private const RigidbodyConstraints AnchoredConstraints =
        RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY;

    private static readonly List<WhiteHiltShipUpgrades> instances = new();

    private ZNetView nview;
    private Container container;
    private GameObject lantern;
    private GameObject[] lanternLights = System.Array.Empty<GameObject>();
    private GameObject[] barrels = new GameObject[0];
    private GameObject[] tent = new GameObject[0];
    private GameObject mastWisp;
    private GameObject brazier;
    private GameObject tentColliders;
    private Container chest;
    private ShipPortal portal;
    private Ship ship;
    private Rigidbody body;
    private RigidbodyConstraints freeConstraints;
    private Transform anchor;
    private Vector3 anchorRaised;
    private int shownMask = -1;
    private bool? shownAnchored;
    private float holdCheckTimer;
    private float netTimer;
    private float emptyTimer;

    /// <summary>
    /// Bit mask of the upgrades on the ship.
    /// </summary>
    public int Mask => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(ZdoKey) : 0;

    /// <summary>
    /// True while the ship has the anchor upgrade and the anchor is lowered.
    /// </summary>
    public bool IsAnchored => Has(ShipDriftAnchor.Bit) && nview.GetZDO().GetBool(AnchorZdoKey);

    /// <summary>Whether the lantern is switched on, with automatic night lighting until first used.</summary>
    public bool IsLanternOn => nview != null && nview.IsValid()
        && Has(ShipLantern.Bit) && nview.GetZDO().GetInt(LanternZdoKey, -1) switch
        {
            0 => false,
            1 => true,
            _ => EnvMan.IsNight()
        };

    /// <summary>Whether the targeted ship's lantern is suppressed by a living, attacking Kraken.</summary>
    public bool IsLanternBlocked => LanternThreat() == 2;

    /// <summary>Requests a persistent, owner-synchronized lantern toggle.</summary>
    /// <param name="user">The interacting sailor.</param>
    /// <returns>True if the request was sent.</returns>
    public bool ToggleLantern(Humanoid user)
    {
        if (nview == null || !nview.IsValid() || !Has(ShipLantern.Bit))
            return false;
        if (IsLanternBlocked)
        {
            user.Message(MessageHud.MessageType.Center, "$whitehilt_ship_lantern_blocked");
            return false;
        }
        nview.InvokeRPC(LanternRpc);
        return true;
    }

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
    /// Checks whether a point is within reach of a lit mast wisp, measured along the ground since the wisp sits high.
    /// </summary>
    /// <param name="point">World position, e.g. the local player.</param>
    /// <param name="reach">Reach in metres.</param>
    /// <returns>True if a White Hilt Ship with the mast wisp is that close.</returns>
    public static bool IsNearMastWisp(Vector3 point, float reach)
    {
        foreach (WhiteHiltShipUpgrades ship in instances)
        {
            if (ship != null && ship.mastWisp != null && ship.Has(ShipMastWisp.Bit)
                && Utils.DistanceXZ(ship.mastWisp.transform.position, point) <= reach)
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
    /// Checks whether a ship's ZDO has an upgrade, for ships that are not loaded.
    /// </summary>
    /// <param name="zdo">The ship's ZDO.</param>
    /// <param name="bit">The upgrade's bit.</param>
    /// <returns>True if the upgrade is on the ship.</returns>
    public static bool Has(ZDO zdo, int bit)
    {
        return (zdo.GetInt(ZdoKey) & (1 << bit)) != 0;
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
        if (Has(ShipDriftAnchor.Bit))
        {
            text += $"[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] {(IsAnchored ? "$whitehilt_ship_anchor_raise" : "$whitehilt_ship_anchor_lower")}\n";
        }

        text += mask == 0 ? "$whitehilt_ship_none" : $"$whitehilt_ship_upgrades: {UpgradeNames(mask)}";
        if (GetComponent<ShipChartTable>() is ShipChartTable table && table.Installed)
        {
            text += $"\n{Helpers.Translations.Token(Helpers.Translations.ItemKey(NavigatorTable.PrefabName))}";
        }
        if (IsAnchored)
        {
            text += "\n$whitehilt_ship_anchored";
        }

        return Localization.instance.Localize(text);
    }

    /// <summary>
    /// Uses an upgrade item on the ship. The Navigator's Table is set up here too, as on the helm.
    /// </summary>
    /// <param name="user">The player.</param>
    /// <param name="item">The item used.</param>
    /// <returns>True if the item was an upgrade or the table.</returns>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (NavigatorTable.IsTable(item))
        {
            ShipChartTable table = GetComponent<ShipChartTable>();
            return table != null && table.UseItem(user, item);
        }

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

        if (bit == ShipChestUpgrade.Bit && chest != null && chest.GetInventory() != null && chest.GetInventory().NrOfItems() > 0)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ship_chest_full");
            return true;
        }

        nview.InvokeRPC(TakeRpc, bit);
        return true;
    }

    /// <summary>
    /// Lowers or raises the anchor.
    /// </summary>
    /// <param name="user">The player.</param>
    /// <returns>True if the ship has an anchor.</returns>
    public bool ToggleAnchor(Humanoid user)
    {
        if (!Has(ShipDriftAnchor.Bit))
        {
            return false;
        }

        user.Message(MessageHud.MessageType.Center, IsAnchored ? "$msg_whitehilt_ship_anchor_raised" : "$msg_whitehilt_ship_anchor_lowered");
        nview.InvokeRPC(AnchorRpc);
        return true;
    }

    /// <summary>
    /// Weighs the anchor. Only on the ship's owner.
    /// </summary>
    public void WeighAnchor()
    {
        if (nview != null && nview.IsValid() && nview.IsOwner())
        {
            nview.GetZDO().Set(AnchorZdoKey, false);
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        ship = GetComponent<Ship>();
        body = GetComponent<Rigidbody>();
        freeConstraints = body != null ? body.constraints : RigidbodyConstraints.None;
        anchor = GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == AnchorName);
        anchorRaised = anchor != null ? anchor.localPosition : Vector3.zero;

        Transform customize = transform.Find("ship/visual/Customize");
        Transform storage = customize?.Find("storage");
        lantern = customize?.Find("TraderLamp")?.gameObject;
        lanternLights = lantern != null ? lantern.GetComponentsInChildren<Light>(true).Select(light => light.gameObject).ToArray() : lanternLights;
        barrels = storage != null ? storage.Cast<Transform>().Where(child => !child.name.StartsWith("Shield")).Select(child => child.gameObject).ToArray() : barrels;
        tent = customize != null ? customize.Cast<Transform>().Where(IsTentPart).Select(child => child.gameObject).ToArray() : tent;
        mastWisp = GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == MastWispName)?.gameObject;
        brazier = transform.Find(BrazierName)?.gameObject;
        tentColliders = transform.Find(ShipTentColliders.ObjectName)?.gameObject;
        chest = transform.Find(ChestName)?.GetComponent<Container>();
        portal = transform.Find(ShipPortal.ObjectName)?.GetComponent<ShipPortal>();
        container = GetComponentsInChildren<Container>(true).FirstOrDefault(found => found.GetComponent<ShipChest>() == null);

        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<int>(AddRpc, RPC_Add);
        nview.Register<int>(TakeRpc, RPC_Take);
        nview.Register(AnchorRpc, RPC_ToggleAnchor);
        nview.Register(LanternRpc, RPC_ToggleLantern);
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

            SetActive(tentColliders, Has(ShipTent.Bit));

            SetActive(mastWisp, Has(ShipMastWisp.Bit));
            SetActive(anchor?.gameObject, Has(ShipDriftAnchor.Bit));
            SetActive(brazier, Has(ShipBrazier.Bit));
            SetActive(chest?.gameObject, Has(ShipChestUpgrade.Bit));
            holdCheckTimer = 0f;
        }

        if (portal != null)
        {
            portal.SetInstalled(Has(ShipPortalUpgrade.Bit) && ShipSettings.AllowShipPortal.Value);
        }

        if (ship != null)
        {
            ship.m_sailForceFactor = ShipSettings.SailForce.Value
                * (GetComponent<global::BrudvikWhiteHilt.Pieces.Ships.Skidbladnir.SkidbladnirShip>() != null
                    ? global::BrudvikWhiteHilt.Pieces.Ships.Skidbladnir.SkidbladnirSettings.SpeedShare.Value : 1f);
        }

        UpdateAutoAnchor(Time.deltaTime);
        UpdateAnchor();
        UpdateFishingNet(Time.deltaTime);
        UpdateLantern();

        holdCheckTimer -= Time.deltaTime;
        if (holdCheckTimer <= 0f)
        {
            holdCheckTimer = 1f;
            UpdateHoldSize();
        }
    }

    private int LanternThreat()
    {
        return global::BrudvikWhiteHilt.Kraken.KrakenBody.LanternThreat(ship,
            ShipSettings.LanternKrakenRange.Value, ShipSettings.LanternWarningSeconds.Value);
    }

    private void UpdateLantern()
    {
        if (!Has(ShipLantern.Bit))
        {
            SetLanternLights(false);
            return;
        }
        int threat = LanternThreat();
        if (threat == 2 && nview.IsOwner() && nview.GetZDO().GetInt(LanternZdoKey, -1) != 0)
        {
            nview.GetZDO().Set(LanternZdoKey, 0);
        }
        bool flickerOn = threat != 1 || (long)(ZNet.instance.GetTime().TimeOfDay.TotalSeconds
            / ShipSettings.LanternFlickerSeconds.Value) % 2 == 0;
        SetLanternLights(threat != 2 && IsLanternOn && flickerOn);
    }

    private void SetLanternLights(bool on)
    {
        foreach (GameObject light in lanternLights)
            SetActive(light, on);
    }

    private void RPC_ToggleLantern(long sender)
    {
        if (!nview.IsOwner() || !Has(ShipLantern.Bit) || IsLanternBlocked)
            return;
        nview.GetZDO().Set(LanternZdoKey, IsLanternOn ? 0 : 1);
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
        if (bit == ShipDriftAnchor.Bit)
        {
            nview.GetZDO().Set(AnchorZdoKey, false);
        }

        Drop(bit);
    }

    private void RPC_ToggleAnchor(long sender)
    {
        if (!nview.IsOwner() || !Has(ShipDriftAnchor.Bit))
        {
            return;
        }

        bool anchored = !nview.GetZDO().GetBool(AnchorZdoKey);
        nview.GetZDO().Set(AnchorZdoKey, anchored);
        if (anchored && ship != null)
        {
            ship.m_speed = Ship.Speed.Stop;
        }
    }

    // Drops the anchor when the last person has left a still ship, and weighs it when someone takes the helm.
    private void UpdateAutoAnchor(float deltaTime)
    {
        if (!ShipSettings.AutoAnchor.Value || !nview.IsOwner() || ship == null || !Has(ShipDriftAnchor.Bit))
        {
            emptyTimer = 0f;
            return;
        }

        bool anchored = nview.GetZDO().GetBool(AnchorZdoKey);
        if (anchored)
        {
            emptyTimer = 0f;
            if (ship.HaveControllingPlayer())
            {
                nview.GetZDO().Set(AnchorZdoKey, false);
            }

            return;
        }

        bool still = body == null || body.linearVelocity.magnitude < AutoAnchorMaxSpeed;
        emptyTimer = ship.m_players.Count == 0 && still ? emptyTimer + deltaTime : 0f;
        if (emptyTimer >= AutoAnchorSeconds)
        {
            emptyTimer = 0f;
            nview.GetZDO().Set(AnchorZdoKey, true);
            ship.m_speed = Ship.Speed.Stop;
        }
    }

    // Freezing the drift and the turn, but not the bobbing, keeps the ship on the waves where it lies.
    private void UpdateAnchor()
    {
        bool anchored = IsAnchored;
        if (anchored == shownAnchored)
        {
            return;
        }

        shownAnchored = anchored;
        if (body != null)
        {
            body.constraints = anchored ? freeConstraints | AnchoredConstraints : freeConstraints;
        }

        if (anchor != null)
        {
            anchor.localPosition = anchored ? anchorRaised + Vector3.down * AnchorDrop : anchorRaised;
        }
    }

    private void UpdateFishingNet(float deltaTime)
    {
        if (!ShipSettings.FishingNet.Value || !nview.IsOwner() || ship == null || !Has(ShipFishingNet.Bit) || IsAnchored
            || Mathf.Abs(ship.GetSpeed()) < MinFishingSpeed)
        {
            return;
        }

        netTimer += deltaTime;
        float interval = ShipSettings.FishingNetMinutes.Value * 60f * (1f - SkillSpeedUp * FishingSkill());
        if (netTimer < interval)
        {
            return;
        }

        netTimer = 0f;
        CatchFish();
    }

    // The skill of the local player aboard, who owns the ship while sailing it; 0 when nobody here is aboard.
    private float FishingSkill()
    {
        Player player = Player.m_localPlayer;
        return player != null && ship.IsPlayerInBoat(player) ? player.GetSkillFactor(Skills.SkillType.Fishing) : 0f;
    }

    private void CatchFish()
    {
        Inventory inventory = container?.GetInventory();
        Heightmap.Biome biome = WorldGenerator.instance.GetBiome(transform.position);
        GameObject fish = ZNetScene.instance.GetPrefab(FishTable.Pick(biome));
        if (inventory == null || fish == null)
        {
            return;
        }

        float skill = FishingSkill();
        int count = Random.value < SkillDoubleChance * skill ? 2 : 1;
        bool caught = Catch(inventory, fish, count);
        if (ShipSettings.FishingNetBycatch.Value)
        {
            if (Random.value < SeaweedChance)
            {
                Catch(inventory, ZNetScene.instance.GetPrefab("FreshSeaweed"), 1);
            }

            if (biome == Heightmap.Biome.Ocean && Random.value < PearlChance)
            {
                Catch(inventory, ZNetScene.instance.GetPrefab("AmberPearl"), 1);
            }
        }

        Player player = Player.m_localPlayer;
        if (caught && player != null && ship.IsPlayerInBoat(player))
        {
            player.RaiseSkill(Skills.SkillType.Fishing, SkillRaise);
        }
    }

    private bool Catch(Inventory inventory, GameObject prefab, int count)
    {
        if (prefab == null || !inventory.CanAddItem(prefab, count))
        {
            return false;
        }

        inventory.AddItem(prefab, count);
        if (Player.m_localPlayer != null && ship.IsPlayerInBoat(Player.m_localPlayer))
        {
            string itemName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            string amount = count > 1 ? $" x{count}" : string.Empty;
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize($"$msg_whitehilt_ship_net_catch: {itemName}{amount}"));
        }

        return true;
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
        nview.GetZDO().Set(AnchorZdoKey, false);
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
