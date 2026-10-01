using BrudvikWhiteHilt.Ranching;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ranching.FeedingTrough;

/// <summary>
/// Lets hungry animals eat from the trough's inventory. The animal's AI walks here and eats at once;
/// the trough's owner takes the item out afterwards, since only the owner may change the inventory.
/// </summary>
public class FeedingTroughComponent : MonoBehaviour
{
    /// <summary>
    /// How far away, in metres, an animal notices a trough.
    /// </summary>
    public static float Range => RanchingSettings.TroughRange.Value;

    private const string EatRpc = "WhiteHilt_TroughEat";
    private const string AddRpc = "WhiteHilt_TroughAdd";

    private static readonly List<FeedingTroughComponent> troughs = new();

    private ZNetView nview;
    private Container container;
    private Collider[] colliders;

    /// <summary>
    /// Finds the closest trough within <see cref="Range"/> that holds food the animal eats.
    /// </summary>
    /// <param name="position">Position of the animal.</param>
    /// <param name="canEat">Whether the animal eats an item.</param>
    /// <returns>The trough, or null.</returns>
    public static FeedingTroughComponent FindNearest(Vector3 position, Func<ItemDrop.ItemData, bool> canEat)
    {
        troughs.RemoveAll(trough => trough == null);
        return troughs
            .Where(trough => Vector3.Distance(trough.transform.position, position) <= Range && trough.FindFood(canEat) != null)
            .OrderBy(trough => Vector3.Distance(trough.transform.position, position))
            .FirstOrDefault();
    }

    /// <summary>
    /// Finds the closest trough within <see cref="Range"/> with room for one <paramref name="prefab"/>.
    /// </summary>
    /// <param name="position">Position of the animal.</param>
    /// <param name="prefab">The item to put in.</param>
    /// <returns>The trough, or null.</returns>
    public static FeedingTroughComponent FindNearestWithRoom(Vector3 position, GameObject prefab)
    {
        troughs.RemoveAll(trough => trough == null);
        return troughs
            .Where(trough => Vector3.Distance(trough.transform.position, position) <= Range
                && trough.container != null && trough.container.GetInventory().CanAddItem(prefab, 1))
            .OrderBy(trough => Vector3.Distance(trough.transform.position, position))
            .FirstOrDefault();
    }

    /// <summary>
    /// Finds an item in the trough the animal eats, its favourites first.
    /// </summary>
    /// <param name="canEat">Whether the animal eats an item.</param>
    /// <param name="isFavorite">Whether the animal prefers an item, or null for no preference.</param>
    /// <returns>The item, or null.</returns>
    public ItemDrop.ItemData FindFood(Func<ItemDrop.ItemData, bool> canEat, Func<ItemDrop.ItemData, bool> isFavorite = null)
    {
        Inventory inventory = container != null ? container.GetInventory() : null;
        List<ItemDrop.ItemData> food = inventory?.GetAllItems().Where(item => item.m_stack > 0 && canEat(item)).ToList();
        if (food == null || food.Count == 0)
        {
            return null;
        }

        return (isFavorite != null ? food.FirstOrDefault(isFavorite) : null) ?? food[0];
    }

    /// <summary>
    /// Returns the point on the trough's edge closest to <paramref name="from"/>, where an animal stands to eat.
    /// </summary>
    /// <param name="from">Position of the animal.</param>
    /// <returns>The feeding point.</returns>
    public Vector3 GetFeedingPoint(Vector3 from)
    {
        Vector3 best = transform.position;
        float bestDistance = float.MaxValue;
        foreach (Collider collider in colliders.Where(collider => collider != null && collider.enabled))
        {
            Vector3 point = collider.ClosestPoint(from);
            float distance = Vector3.Distance(point, from);
            if (distance < bestDistance)
            {
                best = point;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>
    /// Takes one of <paramref name="food"/> out of the trough, on the machine that owns it.
    /// </summary>
    /// <param name="food">The item the animal ate.</param>
    public void Eat(ItemDrop.ItemData food)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(EatRpc, food.m_shared.m_name);
        }
    }

    /// <summary>
    /// Puts one item in the trough, on the machine that owns it.
    /// </summary>
    /// <param name="prefabName">Prefab name of the item.</param>
    public void AddProduct(string prefabName)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(AddRpc, prefabName);
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        container = GetComponent<Container>();
        colliders = GetComponentsInChildren<Collider>().Where(collider => !collider.isTrigger).ToArray();

        // The placement ghost has no ZDO and must not feed anything.
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<string>(EatRpc, RPC_Eat);
        nview.Register<string>(AddRpc, RPC_Add);
        troughs.Add(this);
    }

    private void OnDestroy()
    {
        troughs.Remove(this);
    }

    private void RPC_Eat(long sender, string sharedName)
    {
        if (!nview.IsOwner() || container == null)
        {
            return;
        }

        Inventory inventory = container.GetInventory();
        ItemDrop.ItemData item = inventory.GetAllItems().FirstOrDefault(candidate => candidate.m_shared.m_name == sharedName);
        if (item != null)
        {
            inventory.RemoveItem(item, 1);
        }
    }

    private void RPC_Add(long sender, string prefabName)
    {
        GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName) : null;
        if (nview.IsOwner() && container != null && prefab != null)
        {
            container.GetInventory().AddItem(prefab, 1);
        }
    }
}
