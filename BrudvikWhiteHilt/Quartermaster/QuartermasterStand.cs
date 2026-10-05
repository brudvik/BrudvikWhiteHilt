using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Quartermaster;

/// <summary>
/// The Quartermaster's Table in the world: shows how many chests are within its range, opens the store, and shows
/// the range as a ring on the ground while it is looked at. It keeps the list of items to watch, shared by everyone
/// who uses it, and warns with its hover text and a red glow when one of them runs low.
/// </summary>
public class QuartermasterStand : MonoBehaviour, Interactable, Hoverable
{
    /// <summary>Name of the child that draws the range ring.</summary>
    public const string AreaMarkerName = "QuartermasterArea";

    private const float CountSeconds = 1f;
    private const float HoverSeconds = 0.25f;
    private const float WatchSeconds = 5f;
    private const float WatchDistance = 40f;
    private const int ShownLowLines = 4;

    private static readonly int watchKey = "whitehilt_qm_watch".GetStableHashCode();
    private static readonly Color warningColor = new(1f, 0.25f, 0.15f);

    private ZNetView nview;
    private Piece piece;
    private GameObject areaMarker;
    private CircleProjector circle;
    private float countedAt = float.NegativeInfinity;
    private int chestCount;
    private float hoveredAt = float.NegativeInfinity;
    private float watchedAt = float.NegativeInfinity;
    private List<StockWatch.Low> low = new();
    private Light warning;

    /// <summary>
    /// Shows the range ring on a table being placed. Call every frame for the placement ghost.
    /// </summary>
    /// <param name="ghost">The placement ghost.</param>
    public static void ShowOnGhost(GameObject ghost)
    {
        QuartermasterStand stand = ghost.GetComponent<QuartermasterStand>();
        if (stand != null)
        {
            stand.ShowArea(QuartermasterSettings.ShowRangeRing.Value);
        }
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer || !QuartermasterSettings.Enabled.Value)
        {
            return false;
        }

        QuartermasterPanel.Open(this);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        hoveredAt = Time.time;
        if (!QuartermasterSettings.Enabled.Value)
        {
            return Localization.instance.Localize(GetHoverName());
        }

        if (Time.time - countedAt > CountSeconds)
        {
            countedAt = Time.time;
            chestCount = QuartermasterStore.Around(transform.position).Count;
        }

        string chests = string.Format(Localization.instance.Localize("$whitehilt_qm_chests"), chestCount, Mathf.RoundToInt(QuartermasterSettings.Range.Value));
        string warnings = string.Concat(low.Take(ShownLowLines).Select(line => "\n<color=#FF7055>" + string.Format(
            Localization.instance.Localize("$whitehilt_qm_low"), ItemName(line.Prefab), line.Have, line.Threshold) + "</color>"));
        if (low.Count > ShownLowLines)
        {
            warnings += "\n<color=#FF7055>" + string.Format(Localization.instance.Localize("$whitehilt_qm_low_more"), low.Count - ShownLowLines) + "</color>";
        }

        return Localization.instance.Localize($"{GetHoverName()}\n{chests}{warnings}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_qm_open");
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
    /// Stores the watched items on the table, for everyone.
    /// </summary>
    /// <param name="watches">Threshold per item prefab name.</param>
    public void SetWatches(IEnumerable<KeyValuePair<string, int>> watches)
    {
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        // Like a sign's text: the table's own data, so whoever changes it takes it over.
        nview.ClaimOwnership();
        nview.GetZDO().Set(watchKey, StockWatch.Format(watches));
        watchedAt = float.NegativeInfinity;
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

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? piece.m_name : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        Transform marker = transform.Find(AreaMarkerName);
        if (marker != null)
        {
            areaMarker = marker.gameObject;
            circle = areaMarker.GetComponent<CircleProjector>();
            areaMarker.SetActive(false);
        }

        if (nview != null && nview.IsValid() && !VisualHelper.IsHeadless)
        {
            GameObject lightObject = new("QuartermasterWarning");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.9f, -0.2f);
            warning = lightObject.AddComponent<Light>();
            warning.type = LightType.Point;
            warning.color = warningColor;
            warning.range = 3f;
            warning.intensity = 0f;
            warning.shadows = LightShadows.None;
        }
    }

    private void Update()
    {
        // The placement ghost has no network view; its ring is shown by the placement patch.
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        ShowArea(QuartermasterSettings.ShowRangeRing.Value && Time.time - hoveredAt < HoverSeconds);
        UpdateWarning();
    }

    // Looks for low stock now and then while a player is near, and lets the warning light breathe while some is low.
    private void UpdateWarning()
    {
        Player player = Player.m_localPlayer;
        if (Time.time - watchedAt > WatchSeconds)
        {
            watchedAt = Time.time;
            bool near = player != null && Vector3.Distance(player.transform.position, transform.position) <= WatchDistance;
            List<KeyValuePair<string, int>> watches = near && QuartermasterSettings.Enabled.Value ? GetWatches() : null;
            low = watches != null && watches.Count > 0 ? StockWatch.FindLow(QuartermasterStore.Around(transform.position), watches) : new List<StockWatch.Low>();
        }

        if (warning != null)
        {
            warning.intensity = low.Count > 0 ? 0.6f + 0.4f * Mathf.Sin(Time.time * 2.5f) : 0f;
        }
    }

    private void ShowArea(bool show)
    {
        if (areaMarker == null)
        {
            return;
        }

        if (show && circle != null)
        {
            circle.m_radius = QuartermasterSettings.Range.Value;
        }

        if (areaMarker.activeSelf != show)
        {
            areaMarker.SetActive(show);
        }
    }
}
