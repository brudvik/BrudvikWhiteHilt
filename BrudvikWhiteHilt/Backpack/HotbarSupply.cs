using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Backpack;

/// <summary>
/// Under a bow, crossbow or fishing rod on the hotbar: the arrows, bolts or bait it will use and how many are left,
/// red when running low. Under a staff: how many casts the current eitr allows.
/// </summary>
public static class HotbarSupply
{
    private const string OverlayName = "WhiteHiltSupply";
    private const float Height = 22f;
    private const float Gap = 2f;

    private static readonly Color countColor = new(1f, 0.95f, 0.85f);
    private static readonly Color lowColor = new(1f, 0.3f, 0.25f);
    private static readonly Color eitrColor = new(0.65f, 0.65f, 1f);

    /// <summary>
    /// Updates the lines under the hotbar slots. Called after the game has drawn the hotbar.
    /// </summary>
    /// <param name="bar">The hotbar.</param>
    /// <param name="player">The local player.</param>
    public static void Update(HotkeyBar bar, Player player)
    {
        if (player == null || player.IsDead())
        {
            return;
        }

        for (int i = 0; i < bar.m_elements.Count; i++)
        {
            GameObject element = bar.m_elements[i].m_go;
            if (element == null)
            {
                continue;
            }

            ItemDrop.ItemData item = ItemAt(bar, i);
            if (item != null && BackpackSettings.ShowHotbarAmmo.Value && UsesAmmo(item))
            {
                ShowAmmo(element, player, item);
            }
            else if (item != null && BackpackSettings.ShowHotbarCasts.Value && EitrCost(player, item) is float cost && cost > 0f)
            {
                int casts = Mathf.FloorToInt(player.GetEitr() / cost);
                Show(element, null, casts.ToString(), casts > 0 ? eitrColor : lowColor);
            }
            else
            {
                Hide(element);
            }
        }
    }

    private static ItemDrop.ItemData ItemAt(HotkeyBar bar, int x)
    {
        foreach (ItemDrop.ItemData item in bar.m_items)
        {
            if (item.m_gridPos.x == x)
            {
                return item;
            }
        }

        return null;
    }

    private static bool UsesAmmo(ItemDrop.ItemData item)
    {
        return !string.IsNullOrEmpty(item.m_shared.m_ammoType) && !HandSlots.IsAmmo(item);
    }

    private static float? EitrCost(Player player, ItemDrop.ItemData item)
    {
        Attack attack = item.m_shared.m_attack;
        return attack != null && attack.m_attackEitr > 0f ? attack.GetAttackEitr(player, item) : null;
    }

    // Same choice as the game's Attack.FindAmmo: the equipped ammo if it fits, else the first that does.
    private static ItemDrop.ItemData FindAmmo(Player player, ItemDrop.ItemData weapon)
    {
        string type = weapon.m_shared.m_ammoType;
        Inventory inventory = player.GetInventory();
        ItemDrop.ItemData ammo = player.GetAmmoItem();
        if (ammo != null && ammo.m_shared.m_ammoType == type && inventory.ContainsItem(ammo))
        {
            return ammo;
        }

        return inventory.GetAmmoItem(type);
    }

    private static void ShowAmmo(GameObject element, Player player, ItemDrop.ItemData weapon)
    {
        ItemDrop.ItemData ammo = FindAmmo(player, weapon);
        if (ammo == null)
        {
            Show(element, null, "0", lowColor);
            return;
        }

        int count = player.GetInventory().CountItems(ammo.m_shared.m_name);
        int low = BackpackSettings.LowAmmoWarning.Value;
        Show(element, ammo.GetIcon(), count.ToString(), low > 0 && count <= low ? lowColor : countColor);
    }

    private static void Show(GameObject element, Sprite sprite, string text, Color color)
    {
        Transform overlay = element.transform.Find(OverlayName);
        if (overlay == null)
        {
            overlay = Create(element.transform);
        }

        if (overlay == null)
        {
            return;
        }

        if (!overlay.gameObject.activeSelf)
        {
            overlay.gameObject.SetActive(true);
        }

        Image icon = overlay.Find("icon").GetComponent<Image>();
        Text label = overlay.Find("count").GetComponent<Text>();
        bool hasIcon = sprite != null;
        if (icon.gameObject.activeSelf != hasIcon)
        {
            icon.gameObject.SetActive(hasIcon);
            label.alignment = hasIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            label.rectTransform.pivot = new Vector2(hasIcon ? 0f : 0.5f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(hasIcon ? Gap : 0f, 0f);
        }

        if (hasIcon && icon.sprite != sprite)
        {
            icon.sprite = sprite;
        }

        if (label.text != text)
        {
            label.text = text;
        }

        label.color = color;
    }

    private static void Hide(GameObject element)
    {
        Transform overlay = element.transform.Find(OverlayName);
        if (overlay != null && overlay.gameObject.activeSelf)
        {
            overlay.gameObject.SetActive(false);
        }
    }

    private static Transform Create(Transform element)
    {
        if (GUIManager.Instance == null)
        {
            return null;
        }

        GameObject root = new(OverlayName, typeof(RectTransform));
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(element, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -Gap);
        rect.sizeDelta = new Vector2(70f, Height);

        GameObject iconObject = new("icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-Gap, 0f);
        iconRect.sizeDelta = new Vector2(Height, Height);
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        iconObject.SetActive(false);

        GameObject textObject = GUIManager.Instance.CreateText(string.Empty, rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, GUIManager.Instance.AveriaSerifBold, 15, countColor, true, Color.black, 40f, Height, false);
        textObject.name = "count";
        Text label = textObject.GetComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        return rect;
    }
}
