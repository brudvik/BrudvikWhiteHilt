using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Quartermaster;

/// <summary>
/// A place that opens the store of the chests around it: the Quartermaster's Table, or the Harbour Crane. It keeps the
/// list of items to watch, shared by everyone who uses it, and may choose where taken items go.
/// </summary>
public abstract class QuartermasterSite : MonoBehaviour
{
    private static readonly int watchKey = "whitehilt_qm_watch".GetStableHashCode();

    /// <summary>The site's network view; invalid on a placement ghost.</summary>
    protected ZNetView nview;

    /// <summary>
    /// The cart or ship hold taken items should go to when the window opens, or null for the player's bag.
    /// </summary>
    /// <param name="containers">The containers around the site.</param>
    /// <returns>The container, or null.</returns>
    public virtual Container PreferredTarget(List<Container> containers)
    {
        return null;
    }

    /// <summary>
    /// Told after items were loaded into or unloaded from a cart or ship hold through the site's window.
    /// </summary>
    /// <param name="cargo">The cart or ship hold.</param>
    public virtual void OnCargoMoved(Container cargo)
    {
    }

    /// <summary>
    /// The watched items, each with the amount below which it is low.
    /// </summary>
    /// <returns>Threshold per item prefab name.</returns>
    public List<KeyValuePair<string, int>> GetWatches()
    {
        return nview != null && nview.IsValid() ? StockWatch.Parse(nview.GetZDO().GetString(watchKey)) : new List<KeyValuePair<string, int>>();
    }

    /// <summary>
    /// Stores the watched items on the site, for everyone.
    /// </summary>
    /// <param name="watches">Threshold per item prefab name.</param>
    public void SetWatches(IEnumerable<KeyValuePair<string, int>> watches)
    {
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        // Like a sign's text: the site's own data, so whoever changes it takes it over.
        nview.ClaimOwnership();
        nview.GetZDO().Set(watchKey, StockWatch.Format(watches));
        OnWatchesChanged();
    }

    /// <summary>
    /// Translated name of an item prefab.
    /// </summary>
    /// <param name="prefab">The item prefab name.</param>
    /// <returns>The name, or the prefab name for unknown items.</returns>
    public static string ItemName(string prefab)
    {
        ItemDrop item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>() : null;
        return item != null ? Localization.instance.Localize(item.m_itemData.m_shared.m_name) : prefab;
    }

    /// <summary>
    /// Called after the watched items changed.
    /// </summary>
    protected virtual void OnWatchesChanged()
    {
    }

    /// <summary>
    /// Finds the network view; subclasses call this first.
    /// </summary>
    protected virtual void Awake()
    {
        nview = GetComponent<ZNetView>();
    }
}
