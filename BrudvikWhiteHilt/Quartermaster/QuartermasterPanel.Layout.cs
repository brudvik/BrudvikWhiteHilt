using BrudvikWhiteHilt.Chests;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Quartermaster;

public partial class QuartermasterPanel
{
    // ---- Building blocks -------------------------------------------------------------------------------------------

    // Shows the slot at an index of a list, built if there are not yet so many, with the given item.
    private void ShowSlot(List<ItemSlot> slots, Transform parent, int index, ItemDrop.ItemData item, string amount, Color amountColor,
        string description, Action<bool> onClick, Action onRightClick)
    {
        if (index >= slots.Count)
        {
            slots.Add(CreateSlot(parent));
        }

        ItemSlot slot = slots[index];
        slot.gameObject.SetActive(true);
        slot.Background.color = slotColor;
        slot.Icon.sprite = item.GetIcon();
        slot.Count.text = amount == Infinity && slot.Count.font != null && !slot.Count.font.HasCharacter(Infinity[0]) ? "MAX" : amount;
        slot.Count.color = amountColor;
        slot.Description = description;
        slot.OnClick = onClick;
        slot.OnRightClick = onRightClick;
    }

    private static void HideSlots(List<ItemSlot> slots, int shown)
    {
        for (int i = shown; i < slots.Count; i++)
        {
            slots[i].gameObject.SetActive(false);
        }
    }

    // Builds one item slot of the store or the bag: background, icon and amount. Its click and hover handlers read the
    // slot's current item, so a slot can be filled anew (ShowSlot) instead of built again.
    private ItemSlot CreateSlot(Transform parent)
    {
        GameObject slotObject = new("Slot", typeof(RectTransform));
        slotObject.transform.SetParent(parent, false);
        ItemSlot slot = slotObject.AddComponent<ItemSlot>();
        Image background = slotObject.AddComponent<Image>();
        background.color = slotColor;
        slot.Background = background;

        GameObject iconObject = new("Icon", typeof(RectTransform));
        iconObject.transform.SetParent(slotObject.transform, false);
        Image icon = iconObject.AddComponent<Image>();
        icon.preserveAspect = true;
        slot.Icon = icon;
        icon.raycastTarget = false;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6f, 6f);
        iconRect.offsetMax = new Vector2(-6f, -6f);

        Text count = GUIManager.Instance.CreateText(string.Empty, slotObject.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-Slot / 2f + 1f, 10f), GUIManager.Instance.AveriaSerifBold, 14, Color.white, true, Color.black, Slot - 6f, 20f, false).GetComponent<Text>();
        count.alignment = TextAnchor.LowerRight;
        count.raycastTarget = false;
        slot.Count = count;

        // Clicks come through an event trigger, not a button, to tell the left and the right mouse button apart.
        EventTrigger trigger = slotObject.AddComponent<EventTrigger>();
        EventTrigger.Entry click = new() { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(data =>
        {
            PointerEventData pointer = data as PointerEventData;
            if (pointer != null && pointer.button == PointerEventData.InputButton.Right)
            {
                slot.OnRightClick?.Invoke();
            }
            else if (pointer == null || pointer.button == PointerEventData.InputButton.Left)
            {
                slot.OnClick?.Invoke(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            }
        });
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            background.color = slotHoverColor;
            if (job == null)
            {
                status.text = slot.Description;
            }
        });
        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => background.color = slotColor);
        trigger.triggers.Add(click);
        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
        return slot;
    }

    // A text line with optional minus, plus and remove buttons on the right.
    private static GameObject CreateLine(Transform parent, float width, string text, Action minus, Action plus, Action remove)
    {
        GameObject row = new("Line", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        row.AddComponent<LayoutElement>().preferredHeight = LineHeight;
        float buttons = minus != null ? (LineHeight + 4f) * 3f : 0f;
        Text label = GUIManager.Instance.CreateText(text, row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2((width - buttons) / 2f + 6f, 0f), GUIManager.Instance.AveriaSerif, FontSize, Color.white, true, Color.black,
            width - buttons - 12f, LineHeight, false).GetComponent<Text>();
        label.alignment = TextAnchor.MiddleLeft;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        if (minus != null)
        {
            AddLineButton(row.transform, "-", width - buttons, minus);
            AddLineButton(row.transform, "+", width - buttons + LineHeight + 4f, plus);
            AddLineButton(row.transform, "x", width - buttons + (LineHeight + 4f) * 2f, remove);
        }

        return row;
    }

    private static void AddLineButton(Transform row, string text, float x, Action onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(text, row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(x + LineHeight / 2f, 0f), LineHeight, LineHeight - 2f);
        button.GetComponent<Button>().onClick.AddListener(() => onClick());
        button.GetComponentInChildren<Text>().fontSize = FontSize;
    }

    // Builds the window once: title, the three tabs, the target button that picks inventory, cart or ship, the three
    // pages, the status line and the close button.
    private void BuildContents(Transform panel)
    {
        float inner = Width - Padding * 2f;
        float y = Padding;
        Text title = AddText(panel, Localization.instance.Localize("$whitehilt_qm_title"), 0f, y, inner, 32f, 26);
        title.font = GUIManager.Instance.AveriaSerifBold;
        title.color = GUIManager.Instance.ValheimOrange;
        title.alignment = TextAnchor.MiddleCenter;
        y += 32f;
        subtitle = AddText(panel, string.Empty, 0f, y, inner, 22f, FontSize);
        subtitle.alignment = TextAnchor.MiddleCenter;
        y += 22f + Gap;

        float tabWidth = 170f;
        string[] tabNames = { "$whitehilt_qm_tab_store", "$whitehilt_qm_tab_pack", "$whitehilt_qm_tab_watch" };
        for (int index = 0; index < tabNames.Length; index++)
        {
            Page chosen = (Page)index;
            tabs[index] = AddButton(panel, Localization.instance.Localize(tabNames[index]), index * (tabWidth + Gap), y, tabWidth, () =>
            {
                page = chosen;
                Refresh();
            });
        }

        float targetWidth = inner - 3f * (tabWidth + Gap) - Gap;
        targetLabel = AddButton(panel, string.Empty, inner - targetWidth, y, targetWidth, NextTarget);
        targetLabel.resizeTextForBestFit = true;
        targetLabel.resizeTextMinSize = 10;
        targetLabel.resizeTextMaxSize = FontSize;
        y += RowHeight + Gap * 2f;

        pages = new[] { AddPage(panel, "Store"), AddPage(panel, "Pack"), AddPage(panel, "Watch") };
        float bottom = Height - Padding - RowHeight - Gap;
        BuildStorePage(pages[0].transform, y, inner);
        BuildPackPage(pages[1].transform, y, inner, bottom - 22f - Gap);
        BuildWatchPage(pages[2].transform, y, inner, bottom - 22f - Gap);

        status = AddText(panel, string.Empty, 0f, bottom - 22f, inner, 22f, FontSize);
        status.alignment = TextAnchor.MiddleLeft;
        status.color = subtleColor;
        AddButton(panel, Localization.instance.Localize("$whitehilt_qm_close"), (inner - 160f) / 2f, Height - Padding - RowHeight, 160f, Close);
    }

    // Builds the store page: the search field, the group filters, the grid of what the chests hold, and the bag with
    // its unload button.
    private void BuildStorePage(Transform page, float y, float inner)
    {
        search = GUIManager.Instance.CreateInputField(page, Vector2.zero, Vector2.zero, Vector2.zero, InputField.ContentType.Standard,
            Localization.instance.Localize("$whitehilt_qm_search"), FontSize, 260f, RowHeight).GetComponent<InputField>();
        Place((RectTransform)search.transform, 0f, y, 260f, RowHeight);
        search.onValueChanged.AddListener(_ => RefreshStore());
        y += RowHeight + Gap;

        GameObject filterArea = new("Filters", typeof(RectTransform));
        filterArea.transform.SetParent(page, false);
        filters = (RectTransform)filterArea.transform;
        float filterHeight = FilterHeight * 3f + Gap * 2f;
        Place(filters, 0f, y, inner, filterHeight);
        GridLayoutGroup filterGrid = filterArea.AddComponent<GridLayoutGroup>();
        filterGrid.cellSize = new Vector2(FilterWidth, FilterHeight);
        filterGrid.spacing = new Vector2(Gap, Gap);
        y += filterHeight + Gap;

        storeContent = AddGrid(page, y, inner, StoreHeight);
        y += StoreHeight + Gap;
        Text bagTitle = AddText(page, Localization.instance.Localize("$whitehilt_qm_bag"), 0f, y, inner - 230f, 22f, FontSize);
        bagTitle.color = GUIManager.Instance.ValheimOrange;
        bagTitle.alignment = TextAnchor.MiddleLeft;
        unloadLabel = AddButton(page, string.Empty, inner - 220f, y - 6f, 220f, OnUnload);
        unloadButton = unloadLabel.transform.parent.gameObject;
        y += 22f;
        bagContent = AddGrid(page, y, inner, BagHeight);
    }

    // Builds the pack page: the list of builds with buttons to make, save and delete pack lists, the cost list, and the
    // copies counter with the pack button.
    private void BuildPackPage(Transform page, float y, float inner, float bottom)
    {
        float listHeight = bottom - y - (RowHeight + Gap) * 2f;
        buildContent = AddList(page, 0f, y, BuildListWidth, listHeight);
        float half = (BuildListWidth - Gap) / 2f;
        AddButton(page, Localization.instance.Localize("$whitehilt_qm_new_list"), 0f, y + listHeight + Gap, half, OnNewList);
        AddButton(page, Localization.instance.Localize("$whitehilt_qm_save_list"), half + Gap, y + listHeight + Gap, half, OnSaveAsList);
        deleteLabel = AddButton(page, Localization.instance.Localize("$whitehilt_qm_delete"), 0f, y + listHeight + RowHeight + Gap * 2f, half, OnDeleteList);

        float x = BuildListWidth + Gap * 3f;
        float width = inner - x;
        listName = GUIManager.Instance.CreateInputField(page, Vector2.zero, Vector2.zero, Vector2.zero, InputField.ContentType.Standard,
            Localization.instance.Localize("$whitehilt_qm_list_name"), FontSize, width, RowHeight).GetComponent<InputField>();
        Place((RectTransform)listName.transform, x, y, width, RowHeight);
        listName.onEndEdit.AddListener(OnRenameList);
        y += RowHeight + Gap;

        float costHeight = bottom - y - RowHeight - Gap;
        costContent = AddList(page, x, y, width, costHeight);
        y += costHeight + Gap;

        AddButton(page, "-", x, y, RowHeight + 10f, () => ChangeCopies(-1));
        copiesLabel = AddText(page, string.Empty, x + RowHeight + 10f + Gap, y, 120f, RowHeight, FontSize);
        copiesLabel.alignment = TextAnchor.MiddleCenter;
        AddButton(page, "+", x + RowHeight + 10f + Gap * 2f + 120f, y, RowHeight + 10f, () => ChangeCopies(1));
        AddButton(page, Localization.instance.Localize("$whitehilt_qm_pack"), inner - 180f, y, 180f, OnPack);
    }

    private void BuildWatchPage(Transform page, float y, float inner, float bottom)
    {
        watchHint = AddText(page, string.Empty, 0f, y, inner, 22f, FontSize);
        watchHint.alignment = TextAnchor.MiddleLeft;
        watchHint.color = subtleColor;
        y += 22f + Gap;
        watchContent = AddList(page, 0f, y, inner, bottom - y);
    }

    private static GameObject AddPage(Transform panel, string name)
    {
        GameObject page = new(name, typeof(RectTransform));
        page.transform.SetParent(panel, false);
        RectTransform rect = (RectTransform)page.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return page;
    }

    // A scroll view whose content stacks rows.
    private static RectTransform AddList(Transform parent, float x, float y, float width, float height)
    {
        RectTransform content = AddScroll(parent, x, y, width, height).GetComponentInChildren<ScrollRect>().content;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        return content;
    }

    // A scroll view whose content lays out item slots in a grid.
    private static RectTransform AddGrid(Transform parent, float y, float width, float height)
    {
        RectTransform content = AddScroll(parent, 0f, y, width, height).GetComponentInChildren<ScrollRect>().content;
        DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(Slot, Slot);
        grid.spacing = new Vector2(4f, 4f);
        grid.padding = new RectOffset(4, 4, 4, 4);
        if (!content.TryGetComponent(out ContentSizeFitter fitter))
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return content;
    }

    private static GameObject AddScroll(Transform parent, float x, float y, float width, float height)
    {
        GameObject scroll = GUIManager.Instance.CreateScrollView(parent, false, true, 8f, 4f, GUIManager.Instance.ValheimScrollbarHandleColorBlock,
            new Color(0f, 0f, 0f, 0.35f), width, height);
        Place((RectTransform)scroll.transform, x, y, width, height);
        return scroll;
    }

    private static Text AddText(Transform parent, string text, float x, float y, float width, float height, int fontSize)
    {
        Text label = GUIManager.Instance.CreateText(text, parent, Vector2.zero, Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerif, fontSize,
            Color.white, true, Color.black, width, height, false).GetComponent<Text>();
        Place(label.rectTransform, x, y, width, height);
        return label;
    }

    private static Text AddButton(Transform parent, string text, float x, float y, float width, Action onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(text, parent, Vector2.zero, Vector2.zero, Vector2.zero, width, RowHeight);
        Place((RectTransform)button.transform, x, y, width, RowHeight);
        button.GetComponent<Button>().onClick.AddListener(() => onClick());
        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = FontSize;
        return label;
    }

    // Places a rect by its top-left corner: x from the panel's inner left edge, y from the panel's top.
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(Padding + x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
