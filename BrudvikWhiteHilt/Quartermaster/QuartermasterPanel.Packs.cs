using BrudvikWhiteHilt.Building.Groups;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Quartermaster;

public partial class QuartermasterPanel
{
    // ---- Pack page -------------------------------------------------------------------------------------------------

    // Lists what the pack page can pack for: the build table of the hammer in hand, the saved pack lists and the saved
    // blueprints, and keeps the selection valid when the hammer is put away or a list is deleted.
    private void RefreshBuilds()
    {
        buildRows.ForEach(Destroy);
        buildRows.Clear();
        blueprints = BlueprintStore.LoadAll();
        packLists = PackListStore.LoadAll();
        Piece hammer = HammerPiece();
        if (build == Build.Hammer && hammer == null)
        {
            build = packLists.Count > 0 ? Build.List : Build.Blueprint;
            listIndex = packLists.Count > 0 ? Mathf.Clamp(listIndex, 0, packLists.Count - 1) : -1;
        }

        if (hammer != null)
        {
            AddBuildRow(string.Format(Localization.instance.Localize("$whitehilt_qm_hammer"), Localization.instance.Localize(hammer.m_name)),
                build == Build.Hammer, () => build = Build.Hammer);
        }

        for (int index = 0; index < packLists.Count; index++)
        {
            int chosen = index;
            AddBuildRow(string.Format(Localization.instance.Localize("$whitehilt_qm_list"), packLists[index].Name), build == Build.List && listIndex == index, () =>
            {
                build = Build.List;
                listIndex = chosen;
            });
        }

        foreach (Blueprint blueprint in blueprints)
        {
            string file = blueprint.File;
            string label = blueprint.Name + "   " + string.Format(Localization.instance.Localize("$whitehilt_group_pieces"), blueprint.Pieces.Count);
            AddBuildRow(label, build == Build.Blueprint && blueprintFile == file, () =>
            {
                build = Build.Blueprint;
                blueprintFile = file;
            });
        }

        if (buildRows.Count == 0)
        {
            GameObject empty = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_qm_no_blueprints"), buildContent, Vector2.zero,
                Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerif, FontSize, Color.white, true, Color.black, BuildListWidth - 20f, 80f, false);
            empty.AddComponent<LayoutElement>().preferredHeight = 80f;
            buildRows.Add(empty);
        }

        PackList list = SelectedList();
        listName.gameObject.SetActive(list != null);
        listName.SetTextWithoutNotify(list?.Name ?? string.Empty);
        copiesLabel.text = $"{Localization.instance.Localize("$whitehilt_qm_count")}: {copies}";
        RefreshCost();
    }

    private void AddBuildRow(string label, bool selected, Action choose)
    {
        GameObject row = GUIManager.Instance.CreateButton(label, buildContent, Vector2.zero, Vector2.zero, Vector2.zero, BuildListWidth - 20f, RowHeight);
        row.AddComponent<LayoutElement>().preferredHeight = RowHeight;
        Text text = row.GetComponentInChildren<Text>();
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = FontSize;
        text.color = selected ? selectedColor : Color.white;
        text.rectTransform.offsetMin = new Vector2(10f, 0f);
        row.GetComponent<Button>().onClick.AddListener(() =>
        {
            choose();
            RefreshBuilds();
        });
        buildRows.Add(row);
    }

    private PackList SelectedList()
    {
        return build == Build.List && listIndex >= 0 && listIndex < packLists.Count ? packLists[listIndex] : null;
    }

    // What the chosen build costs for one copy, per item prefab name, in a steady order.
    private List<KeyValuePair<string, int>> BaseCost()
    {
        if (build == Build.List)
        {
            return SelectedList()?.Items.ToList() ?? new List<KeyValuePair<string, int>>();
        }

        List<Piece> pieces = new();
        if (build == Build.Hammer)
        {
            Piece piece = HammerPiece();
            if (piece != null)
            {
                pieces.Add(piece);
            }
        }
        else
        {
            Blueprint blueprint = blueprints.FirstOrDefault(candidate => candidate.File == blueprintFile);
            if (blueprint != null)
            {
                pieces.AddRange(blueprint.Pieces.Select(snapshot => GroupPlacer.Resolve(snapshot.Prefab)).Where(piece => piece != null));
            }
        }

        return GroupPlacer.Cost(pieces)
            .OrderBy(pair => Localization.instance.Localize(pair.Key.m_itemData.m_shared.m_name))
            .Select(pair => new KeyValuePair<string, int>(pair.Key.name, pair.Value)).ToList();
    }

    private string BuildName()
    {
        return build switch
        {
            Build.Hammer => Localization.instance.Localize(HammerPiece()?.m_name ?? string.Empty),
            Build.List => SelectedList()?.Name ?? string.Empty,
            _ => blueprints.FirstOrDefault(candidate => candidate.File == blueprintFile)?.Name ?? string.Empty
        };
    }

    // Shows what the selected build needs times the number of copies, against what the player or the target holds and
    // what the chests can give. A row is red when there is not enough; on a pack list each row has buttons to change or
    // remove the item.
    private void RefreshCost()
    {
        costRows.ForEach(Destroy);
        costRows.Clear();
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        Inventory inventory = target != null ? target.GetInventory() : player.GetInventory();
        List<QuartermasterStore.Entry> store = QuartermasterStore.List(Sources());
        bool editable = build == Build.List;
        float width = Width - Padding * 2f - BuildListWidth - Gap * 3f - 20f;
        foreach (KeyValuePair<string, int> item in BaseCost())
        {
            ItemDrop drop = ItemOf(item.Key);
            if (drop == null)
            {
                continue;
            }

            string name = drop.m_itemData.m_shared.m_name;
            int need = item.Value * copies;
            int have = inventory.CountItems(name);
            List<QuartermasterStore.Entry> matching = store.Where(entry => entry.Name == name).ToList();
            bool unlimited = matching.Any(entry => entry.Unlimited);
            int available = matching.Where(entry => !entry.Unlimited).Sum(QuartermasterStore.Takeable);
            string inStore = unlimited ? Localization.instance.Localize("$whitehilt_qm_unlimited") : available.ToString();
            string line = $"<b>{Localization.instance.Localize(name)}</b>  "
                + string.Format(Localization.instance.Localize("$whitehilt_qm_need"), need, have, inStore);
            bool enough = unlimited || have + available >= need;
            string prefab = item.Key;
            costRows.Add(CreateLine(costContent, width, enough ? line : $"<color=#{ColorUtility.ToHtmlStringRGB(lackingColor)}>{line}</color>",
                editable ? () => ChangeListItem(prefab, -StepSize()) : null,
                editable ? () => ChangeListItem(prefab, StepSize()) : null,
                editable ? () => ChangeListItem(prefab, -int.MaxValue / 2) : null));
        }

        if (costRows.Count == 0 && editable)
        {
            costRows.Add(CreateLine(costContent, width, $"<color=#{ColorUtility.ToHtmlStringRGB(subtleColor)}>"
                + Localization.instance.Localize("$whitehilt_qm_list_empty") + "</color>", null, null, null));
        }
    }

    private void ChangeListItem(string prefab, int change)
    {
        PackList list = SelectedList();
        if (list == null)
        {
            return;
        }

        int current = list.Items.FirstOrDefault(item => item.Key == prefab).Value;
        list.Add(prefab, Mathf.Max(change, -current));
        PackListStore.SaveAll(packLists);
        RefreshCost();
    }

    // Packs what the selected build needs into the inventory or the chosen cart or ship hold, once every chest holding
    // any of it has been handed over. Says whether all of it was there.
    private void OnPack()
    {
        List<KeyValuePair<ItemDrop, int>> cost = BaseCost()
            .Select(item => new KeyValuePair<ItemDrop, int>(ItemOf(item.Key), item.Value * copies))
            .Where(item => item.Key != null).ToList();
        if (cost.Count == 0)
        {
            return;
        }

        string buildName = BuildName();
        Container into = target;
        HashSet<string> names = new(cost.Select(item => item.Key.m_itemData.m_shared.m_name));
        List<Container> needed = QuartermasterStore.Holding(Sources(), names);
        needed.Add(into);
        Begin(needed, ready =>
        {
            bool complete = true;
            foreach (KeyValuePair<ItemDrop, int> item in cost)
            {
                complete &= QuartermasterStore.Pack(Player.m_localPlayer, ready, item.Key.m_itemData.m_shared.m_name, item.Value, into);
            }

            Message(string.Format(Localization.instance.Localize(complete ? "$whitehilt_qm_packed" : "$whitehilt_qm_packed_short"), buildName));
        });
    }

    private void OnNewList()
    {
        packLists = PackListStore.LoadAll();
        packLists.Add(new PackList { Name = string.Format(Localization.instance.Localize("$whitehilt_qm_list_default"), packLists.Count + 1) });
        PackListStore.SaveAll(packLists);
        build = Build.List;
        listIndex = packLists.Count - 1;
        RefreshBuilds();
        listName.ActivateInputField();
    }

    // Saves the chosen piece or blueprint, times the copies, as a new pack list.
    private void OnSaveAsList()
    {
        if (build == Build.List)
        {
            return;
        }

        List<KeyValuePair<string, int>> cost = BaseCost();
        if (cost.Count == 0)
        {
            return;
        }

        PackList list = new() { Name = BuildName() };
        foreach (KeyValuePair<string, int> item in cost)
        {
            list.Add(item.Key, item.Value * copies);
        }

        packLists = PackListStore.LoadAll();
        packLists.Add(list);
        PackListStore.SaveAll(packLists);
        build = Build.List;
        listIndex = packLists.Count - 1;
        copies = 1;
        RefreshBuilds();
    }

    // Deletes the selected pack list on the second click within a few seconds, so a list is not lost to a slip of the
    // mouse.
    private void OnDeleteList()
    {
        PackList list = SelectedList();
        if (list == null)
        {
            return;
        }

        if (Time.unscaledTime > deleteArmedUntil)
        {
            deleteArmedUntil = Time.unscaledTime + ConfirmSeconds;
            deleteLabel.text = Localization.instance.Localize("$whitehilt_qm_confirm");
            return;
        }

        deleteArmedUntil = 0f;
        deleteLabel.text = Localization.instance.Localize("$whitehilt_qm_delete");
        packLists.RemoveAt(listIndex);
        PackListStore.SaveAll(packLists);
        listIndex = Mathf.Min(listIndex, packLists.Count - 1);
        build = listIndex >= 0 ? Build.List : Build.Hammer;
        RefreshBuilds();
    }

    private void OnRenameList(string name)
    {
        PackList list = SelectedList();
        if (list == null || string.IsNullOrWhiteSpace(name) || name.Trim() == list.Name)
        {
            return;
        }

        list.Name = name.Trim();
        PackListStore.SaveAll(packLists);
        RefreshBuilds();
    }

    private void ChangeCopies(int change)
    {
        copies = Mathf.Clamp(copies + change, 1, MaxCopies);
        copiesLabel.text = $"{Localization.instance.Localize("$whitehilt_qm_count")}: {copies}";
        RefreshCost();
    }

    // The piece selected in the hammer the player holds, or else in the first build tool in the inventory.
    private static Piece HammerPiece()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return null;
        }

        PieceTable table = player.m_buildPieces
            ?? player.GetInventory().GetAllItems().Select(item => item.m_shared.m_buildPieces).FirstOrDefault(pieces => pieces != null);
        Piece piece = table != null ? table.GetSelectedPiece() : null;
        return piece != null && GroupPlacer.Resolve(global::Utils.GetPrefabName(piece.gameObject)) != null ? piece : null;
    }
}
