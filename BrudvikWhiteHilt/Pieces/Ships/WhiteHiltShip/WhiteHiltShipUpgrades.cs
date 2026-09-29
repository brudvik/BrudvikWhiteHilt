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

    private const string ZdoKey = "whitehilt_ship_upgrades";
    private const string AddRpc = "WhiteHiltShipAddUpgrade";
    private const string TakeRpc = "WhiteHiltShipTakeUpgrade";
    private const string AnchorZdoKey = "whitehilt_ship_anchored";
    private const string AnchorRpc = "WhiteHiltShipToggleAnchor";
    private const float MinFishingSpeed = 2f;

    // At Fishing 100 the net catches twice as often, and half the catches are two fish.
    private const float SkillSpeedUp = 0.5f;
    private const float SkillDoubleChance = 0.5f;
    private const float SkillRaise = 0.5f;
    private const float SeaweedChance = 0.1f;
    private const float PearlChance = 0.03f;
    private const float AnchorDrop = 2f;

    // The ship must lie empty and nearly still this long before the anchor drops on its own.
    private const float AutoAnchorSeconds = 2f;
    private const float AutoAnchorMaxSpeed = 2f;

    private const RigidbodyConstraints AnchoredConstraints =
        RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY;

    private static readonly List<WhiteHiltShipUpgrades> instances = new();

    // Fish1 perch, Fish2 pike, Fish3 tuna, Fish5 trollfish, Fish6 giant herring, Fish7 grouper, Fish8 coral cod,
    // Fish9 anglerfish, Fish10 northern salmon, Fish11 magmafish, Fish12 pufferfish.
    private static readonly Dictionary<Heightmap.Biome, (string Prefab, float Weight)[]> fishByBiome = new()
    {
        [Heightmap.Biome.Meadows] = new[] { ("Fish1", 0.7f), ("Fish2", 0.3f) },
        [Heightmap.Biome.BlackForest] = new[] { ("Fish2", 0.6f), ("Fish1", 0.25f), ("Fish5", 0.15f) },
        [Heightmap.Biome.Swamp] = new[] { ("Fish6", 0.7f), ("Fish1", 0.3f) },
        [Heightmap.Biome.Mountain] = new[] { ("Fish1", 1f) },
        [Heightmap.Biome.Plains] = new[] { ("Fish7", 0.7f), ("Fish1", 0.3f) },
        [Heightmap.Biome.Ocean] = new[] { ("Fish3", 0.5f), ("Fish8", 0.35f), ("Fish12", 0.15f) },
        [Heightmap.Biome.Mistlands] = new[] { ("Fish9", 0.6f), ("Fish12", 0.4f) },
        [Heightmap.Biome.DeepNorth] = new[] { ("Fish10", 1f) },
        [Heightmap.Biome.AshLands] = new[] { ("Fish11", 1f) }
    };

    private ZNetView nview;
    private Container container;
    private GameObject lantern;
    private GameObject lanternLight;
    private GameObject[] barrels = new GameObject[0];
    private GameObject[] tent = new GameObject[0];
    private GameObject mastWisp;
    private GameObject brazier;
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

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        container = GetComponentInChildren<Container>(true);
        ship = GetComponent<Ship>();
        body = GetComponent<Rigidbody>();
        freeConstraints = body != null ? body.constraints : RigidbodyConstraints.None;
        anchor = GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == AnchorName);
        anchorRaised = anchor != null ? anchor.localPosition : Vector3.zero;

        Transform customize = transform.Find("ship/visual/Customize");
        Transform storage = customize?.Find("storage");
        lantern = customize?.Find("TraderLamp")?.gameObject;
        lanternLight = lantern != null ? lantern.GetComponentInChildren<Light>(true)?.gameObject : null;
        barrels = storage != null ? storage.Cast<Transform>().Where(child => !child.name.StartsWith("Shield")).Select(child => child.gameObject).ToArray() : barrels;
        tent = customize != null ? customize.Cast<Transform>().Where(IsTentPart).Select(child => child.gameObject).ToArray() : tent;
        mastWisp = GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == MastWispName)?.gameObject;
        brazier = transform.Find(BrazierName)?.gameObject;
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
            SetActive(anchor?.gameObject, Has(ShipDriftAnchor.Bit));
            SetActive(brazier, Has(ShipBrazier.Bit));
            SetActive(chest?.gameObject, Has(ShipChestUpgrade.Bit));
            holdCheckTimer = 0f;
        }

        if (portal != null)
        {
            portal.SetInstalled(Has(ShipPortalUpgrade.Bit));
        }

        UpdateAutoAnchor(Time.deltaTime);
        UpdateAnchor();
        UpdateFishingNet(Time.deltaTime);

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
        if (!nview.IsOwner() || ship == null || !Has(ShipFishingNet.Bit) || IsAnchored || Mathf.Abs(ship.GetSpeed()) < MinFishingSpeed)
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
        GameObject fish = ZNetScene.instance.GetPrefab(PickFish(biome));
        if (inventory == null || fish == null)
        {
            return;
        }

        float skill = FishingSkill();
        int count = Random.value < SkillDoubleChance * skill ? 2 : 1;
        Catch(inventory, fish, count);
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
        if (player != null && ship.IsPlayerInBoat(player))
        {
            player.RaiseSkill(Skills.SkillType.Fishing, SkillRaise);
        }
    }

    private void Catch(Inventory inventory, GameObject prefab, int count)
    {
        if (prefab == null || !inventory.CanAddItem(prefab, count))
        {
            return;
        }

        inventory.AddItem(prefab, count);
        if (Player.m_localPlayer != null && ship.IsPlayerInBoat(Player.m_localPlayer))
        {
            string itemName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            string amount = count > 1 ? $" x{count}" : string.Empty;
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize($"$msg_whitehilt_ship_net_catch: {itemName}{amount}"));
        }
    }

    private static string PickFish(Heightmap.Biome biome)
    {
        if (!fishByBiome.TryGetValue(biome, out (string Prefab, float Weight)[] fish))
        {
            fish = fishByBiome[Heightmap.Biome.Ocean];
        }

        float roll = Random.value * fish.Sum(entry => entry.Weight);
        foreach ((string prefab, float weight) in fish)
        {
            roll -= weight;
            if (roll <= 0f)
            {
                return prefab;
            }
        }

        return fish[fish.Length - 1].Prefab;
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
