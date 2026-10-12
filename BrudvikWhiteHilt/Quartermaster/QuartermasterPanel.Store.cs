using BrudvikWhiteHilt.Chests;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Quartermaster;

public partial class QuartermasterPanel
{
    private static string Describe(QuartermasterStore.Entry entry)
    {
        string name = Localization.instance.Localize(entry.Name);
        return entry.Quality > 1 ? $"{name} ({entry.Quality})" : name;
    }

    private static int StepSize()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 1 : Step;
    }

    private static ItemDrop ItemOf(string prefab)
    {
        return ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>() : null;
    }

    // ---- Store page ------------------------------------------------------------------------------------------------

    private void RefreshFilters()
    {
        filterButtons.ForEach(Destroy);
        filterButtons.Clear();
        AddFilter(Localization.instance.Localize("$whitehilt_qm_all"), null);
        foreach (string name in entries.Select(entry => entry.Group).Distinct())
        {
            AddFilter(name, name);
        }
    }

    private void AddFilter(string label, string value)
    {
        GameObject button = GUIManager.Instance.CreateButton(label, filters, Vector2.zero, Vector2.zero, Vector2.zero, FilterWidth, FilterHeight);
        Text text = button.GetComponentInChildren<Text>();
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 9;
        text.resizeTextMaxSize = 12;
        text.color = group == value ? selectedColor : Color.white;
        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            group = value;
            RefreshFilters();
            RefreshStore();
        });
        filterButtons.Add(button);
    }

    // Fills the store page from the listed entries, filtered by the chosen group and the search text. Slots are reused
    // (ShowSlot) rather than rebuilt, as this runs on every keystroke in the search field. An entry is coloured red
    // when a stock watch says it runs low and gold when a chest keeps it without limit.
    private void RefreshStore()
    {
        int shown = 0;
        string filter = (search.text ?? string.Empty).Trim();
        HashSet<string> watched = new(watches.Select(watch => watch.Key));
        HashSet<string> lowItems = new(low.Select(line => line.Prefab));
        foreach (QuartermasterStore.Entry entry in entries)
        {
            if ((group != null && entry.Group != group)
                || (filter.Length > 0 && Localization.instance.Localize(entry.Name).IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) < 0))
            {
                continue;
            }

            string prefab = entry.Sample.m_dropPrefab.name;
            Color color = lowItems.Contains(prefab) ? lackingColor : entry.Unlimited ? unlimitedColor : Color.white;
            string description = Describe(entry) + "  -  " + entry.Group
                + (watched.Contains(prefab) ? "  -  " + Localization.instance.Localize("$whitehilt_qm_watched") : string.Empty);
            QuartermasterStore.Entry chosen = entry;
            ShowSlot(storeSlots, storeContent, shown++, entry.Sample, entry.Unlimited ? Infinity : entry.Count.ToString(), color, description,
                shift => OnStoreClick(chosen, shift), () => ToggleWatch(prefab));
        }

        HideSlots(storeSlots, shown);

        status.text = job != null ? status.text : Localization.instance.Localize(entries.Count == 0 ? "$whitehilt_qm_empty" : "$whitehilt_qm_take_hint");
    }

    // Shows what the player, or the chosen cart or ship hold, carries that belongs in a chest around, so it can be put
    // back with a click.
    private void RefreshBag()
    {
        List<QuartermasterStore.Entry> bag = QuartermasterStore.Returnable(Player.m_localPlayer, containers);
        for (int i = 0; i < bag.Count; i++)
        {
            QuartermasterStore.Entry chosen = bag[i];
            ShowSlot(bagSlots, bagContent, i, chosen.Sample, chosen.Count.ToString(), Color.white, Describe(chosen), _ => OnPutBack(chosen), null);
        }

        HideSlots(bagSlots, bag.Count);
        if (bagEmpty == null)
        {
            bagEmpty = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_qm_bag_empty"), bagContent, Vector2.zero,
                Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerif, FontSize, subtleColor, true, Color.black, 400f, Slot, false);
            bagEmpty.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        }

        bagEmpty.SetActive(bag.Count == 0);
        bagEmpty.transform.SetAsLastSibling();
    }

    // Takes a stack of an entry, or with Shift adds it to the selected pack list. The chests holding it and the target
    // are first handed over to this player (Begin), and the entry is read again once they are: another player may have
    // taken some while the request was on its way.
    private void OnStoreClick(QuartermasterStore.Entry entry, bool shift)
    {
        if (shift)
        {
            AddToList(entry);
            return;
        }

        if (QuartermasterStore.Takeable(entry) <= 0)
        {
            Message(Localization.instance.Localize("$whitehilt_qm_kept"));
            return;
        }

        string name = entry.Name;
        int quality = entry.Quality;
        Container into = target;
        List<Container> needed = QuartermasterStore.Holding(Sources(), new[] { name });
        needed.Add(into);
        Begin(needed, ready =>
        {
            List<Container> sources = ready.Where(container => container != into).ToList();
            QuartermasterStore.Entry fresh = QuartermasterStore.List(sources).FirstOrDefault(candidate => candidate.Name == name && candidate.Quality == quality);
            int taken = fresh != null && QuartermasterStore.Takeable(fresh) > 0
                ? QuartermasterStore.Take(Player.m_localPlayer, sources, fresh, fresh.Sample.m_shared.m_maxStackSize, into)
                : 0;
            Message(taken > 0
                ? string.Format(Localization.instance.Localize("$whitehilt_qm_taken"), Localization.instance.Localize(name), taken)
                : Localization.instance.Localize(fresh == null || QuartermasterStore.Takeable(fresh) <= 0 ? "$whitehilt_qm_kept" : "$whitehilt_qm_bag_full"));
        });
    }

    private void OnPutBack(QuartermasterStore.Entry entry)
    {
        string name = entry.Name;
        int quality = entry.Quality;
        Vector3 table = stand.transform.position;
        Begin(QuartermasterStore.Receiving(containers, entry.Sample), ready =>
        {
            int returned = QuartermasterStore.PutBack(Player.m_localPlayer, ready, table, name, quality);
            Message(returned > 0
                ? string.Format(Localization.instance.Localize("$whitehilt_qm_returned"), Localization.instance.Localize(name), returned)
                : Localization.instance.Localize("$whitehilt_qm_no_room"));
        });
    }

    // Puts everything in the chosen cart or ship hold back into the chests, the same way the Collection Post sorts.
    private void OnUnload()
    {
        Container cargo = target;
        if (cargo == null)
        {
            return;
        }

        Vector3 site = stand.transform.position;
        List<Container> chests = Sources();
        List<Container> needed = cargo.GetInventory().GetAllItems().SelectMany(item => QuartermasterStore.Receiving(chests, item)).Distinct().ToList();
        needed.Add(cargo);
        Begin(needed, ready =>
        {
            int moved = QuartermasterStore.Unload(cargo, ready.Where(container => container != cargo).ToList(), site);
            Message(moved > 0
                ? string.Format(Localization.instance.Localize("$whitehilt_qm_unloaded"), moved)
                : Localization.instance.Localize("$whitehilt_qm_no_room"));
        });
    }

    // Shift + click: a stack more of the item in the pack list chosen on the pack page.
    private void AddToList(QuartermasterStore.Entry entry)
    {
        if (build != Build.List || listIndex < 0 || listIndex >= packLists.Count)
        {
            Message(Localization.instance.Localize("$whitehilt_qm_no_list"));
            return;
        }

        PackList list = packLists[listIndex];
        int stack = entry.Sample.m_shared.m_maxStackSize;
        list.Add(entry.Sample.m_dropPrefab.name, stack);
        PackListStore.SaveAll(packLists);
        Message(string.Format(Localization.instance.Localize("$whitehilt_qm_added"), Localization.instance.Localize(entry.Name), stack, list.Name));
    }

    // Right click: watch the item, starting at a stack, or stop watching it.
    private void ToggleWatch(string prefab)
    {
        int index = watches.FindIndex(watch => watch.Key == prefab);
        if (index >= 0)
        {
            watches.RemoveAt(index);
            Message(string.Format(Localization.instance.Localize("$whitehilt_qm_unwatched"), QuartermasterSite.ItemName(prefab)));
        }
        else
        {
            ItemDrop item = ItemOf(prefab);
            watches.Add(new KeyValuePair<string, int>(prefab, Mathf.Max(1, item != null ? item.m_itemData.m_shared.m_maxStackSize : Step)));
            Message(string.Format(Localization.instance.Localize("$whitehilt_qm_watching"), QuartermasterSite.ItemName(prefab)));
        }

        stand.SetWatches(watches);
        Refresh();
    }
}
