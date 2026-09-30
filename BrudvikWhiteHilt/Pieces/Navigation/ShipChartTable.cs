using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Navigation;
using BrudvikWhiteHilt.Navigation;
using BrudvikWhiteHilt.Pieces.Defenses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// The Navigator's Table on a ship's deck. The item is used on the helm to set the table up and taken back with
/// Shift + Use there. Whether a ship has one is a flag in the ship's ZDO, so every player sees the same. The number of
/// map scrolls on and under the table follows the Exploration skill of the player looking at it.
/// </summary>
public class ShipChartTable : MonoBehaviour
{
    /// <summary>
    /// Ships that get a table, with the layout mount whose placement they use.
    /// </summary>
    public static readonly (string Ship, string Mount)[] Ships =
    {
        ("Karve", "Karve"),
        ("VikingShip", "VikingShip"),
        ("VikingShip_Ashlands", "VikingShip_Ashlands"),
        ("WhiteHiltShip", "VikingShip")
    };

    private const string VisualName = "WhiteHiltChartTable";
    private const string TableLayout = "navigatorbord";
    private const string ZdoKey = "whitehilt_ship_charttable";
    private const string SetRpc = "WhiteHiltChartTableSet";

    private static readonly List<ShipChartTable> instances = new();

    private ZNetView nview;
    private Ship ship;
    private GameObject visual;
    private bool? shownInstalled;

    /// <summary>
    /// True if the table is set up on this ship.
    /// </summary>
    public bool Installed => nview != null && nview.IsValid() && nview.GetZDO().GetBool(ZdoKey);

    /// <summary>
    /// Adds the table to a ship prefab: the component everywhere, the hidden model on clients. Does nothing if the
    /// prefab already has it.
    /// </summary>
    /// <param name="shipPrefab">The ship prefab.</param>
    /// <param name="mount">Ship in the layout whose table placement is used.</param>
    public static void Prepare(GameObject shipPrefab, string mount)
    {
        if (shipPrefab == null || shipPrefab.GetComponent<ShipChartTable>() != null)
        {
            return;
        }

        shipPrefab.AddComponent<ShipChartTable>();
        shipPrefab.AddComponent<ShipRoute>();
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        DefenseLayout layout = DefenseLayout.Load();
        DefensePartData placement = layout.Get($"mount_{mount}").parts.First(part => part.piece == TableLayout);
        MeshRenderer shipRenderer = shipPrefab.GetComponentInChildren<MeshRenderer>(true);
        int layer = shipRenderer != null ? shipRenderer.gameObject.layer : shipPrefab.layer;

        GameObject table = new(VisualName) { layer = layer };
        table.transform.SetParent(shipPrefab.transform, false);
        table.transform.localPosition = DefenseModelBuilder.ToVector(placement.position, Vector3.zero);
        table.transform.localRotation = Quaternion.Euler(DefenseModelBuilder.ToVector(placement.rotation, Vector3.zero));

        Dictionary<string, Transform> groups = new();
        for (int i = 1; i <= SkillScrolls.Count; i++)
        {
            GameObject scroll = new(SkillScrolls.SlotName(i)) { layer = layer };
            scroll.transform.SetParent(table.transform, false);
            groups[scroll.name] = scroll.transform;
        }

        DefenseModelBuilder.Build(table.transform, groups, DefenseModelBuilder.Flatten(layout, layout.Get(TableLayout)));
        table.AddComponent<SkillScrolls>();
        AddCollider(table.transform);
        table.SetActive(false);
    }

    // One box around the table and its scrolls, on the ship's vehicle layer, so players walk around it and can point at it.
    private static void AddCollider(Transform table)
    {
        Bounds bounds = default;
        bool first = true;
        foreach (MeshFilter filter in table.GetComponentsInChildren<MeshFilter>(true).Where(filter => filter.sharedMesh != null))
        {
            Bounds mesh = filter.sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = mesh.center + Vector3.Scale(mesh.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                Vector3 point = table.InverseTransformPoint(filter.transform.TransformPoint(local));
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        if (first)
        {
            return;
        }

        int vehicle = LayerMask.NameToLayer("vehicle");
        GameObject collider = new("collider") { layer = vehicle >= 0 ? vehicle : table.gameObject.layer };
        collider.transform.SetParent(table, false);
        BoxCollider box = collider.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
        collider.AddComponent<ShipChartTableHover>();
    }

    /// <summary>
    /// True if the player is aboard a ship with a table set up.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True if the table helps the player.</returns>
    public static bool IsAboardWithTable(Player player)
    {
        foreach (ShipChartTable table in instances)
        {
            if (table.ship != null && table.ship.IsPlayerInBoat(player) && table.Installed)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Sets up the table if the item is a Navigator's Table.
    /// </summary>
    /// <param name="user">The player using the item on the helm.</param>
    /// <param name="item">The item.</param>
    /// <returns>True if the item was a table.</returns>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (!NavigatorTable.IsTable(item) || nview == null || !nview.IsValid())
        {
            return false;
        }

        if (Installed)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_charttable_already");
            return true;
        }

        user.GetInventory().RemoveOneItem(item);
        nview.InvokeRPC(SetRpc, true);
        user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_charttable_added");
        return true;
    }

    /// <summary>
    /// Takes the table off the ship; it drops next to where it stood.
    /// </summary>
    /// <returns>True if there was a table.</returns>
    public bool Take()
    {
        if (!Installed)
        {
            return false;
        }

        nview.InvokeRPC(SetRpc, false);
        return true;
    }

    /// <summary>
    /// Extra lines for the helm's hover text.
    /// </summary>
    /// <returns>The unlocalized text, or an empty string without a table.</returns>
    public string GetHoverText()
    {
        if (!Installed || Player.m_localPlayer == null)
        {
            return string.Empty;
        }

        float baseRadius = Minimap.instance != null ? Minimap.instance.m_exploreRadius : 100f;
        int radius = Mathf.RoundToInt(ExplorationSkill.GetExploreRadius(Player.m_localPlayer, baseRadius));
        return $"\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_charttable_take\n$whitehilt_charttable_sight: {radius} m";
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        ship = GetComponent<Ship>();
        visual = transform.Find(VisualName)?.gameObject;
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<bool>(SetRpc, RPC_Set);
        WearNTear wearNTear = GetComponent<WearNTear>();
        if (wearNTear != null)
        {
            wearNTear.m_onDestroyed += DropIfInstalled;
        }

        instances.Add(this);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    private void Update()
    {
        if (visual == null || nview == null || !nview.IsValid())
        {
            return;
        }

        bool installed = Installed;
        if (installed != shownInstalled)
        {
            shownInstalled = installed;
            visual.SetActive(installed);
        }
    }

    private void RPC_Set(long sender, bool installed)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        bool wasInstalled = Installed;
        if (installed == wasInstalled)
        {
            // Two players set up a table at once: give the second one back. Taking a table twice does nothing.
            if (installed)
            {
                Drop();
            }

            return;
        }

        nview.GetZDO().Set(ZdoKey, installed);
        if (!installed)
        {
            Drop();
        }
    }

    private void DropIfInstalled()
    {
        if (Installed)
        {
            nview.GetZDO().Set(ZdoKey, false);
            Drop();
        }
    }

    private void Drop()
    {
        GameObject prefab = ZNetScene.instance.GetPrefab(NavigatorTable.PrefabName);
        if (prefab != null)
        {
            Vector3 position = visual != null ? visual.transform.position : transform.position;
            Instantiate(prefab, position + Vector3.up * 1.5f, Quaternion.identity);
        }
    }
}
