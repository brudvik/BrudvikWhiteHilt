using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Chests;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Quartermaster;

/// <summary>
/// The window of a Quartermaster's Table, with three pages. The store shows everything in the chests around the table
/// as one grid: a click takes a stack, Shift + click adds a stack to the chosen pack list, a right click watches the
/// item; below it, the stacks in your bag that a chest here takes, to put back. The pack page takes what the piece in
/// the hammer, a blueprint or a pack list needs. The watch page sets when a watched item counts as low.
/// Everything taken goes into the bag, or into a cart or ship hold within range when one is chosen. Before anything
/// is moved, the chests it touches are handed over with their newest contents (<see cref="ContainerHandoff"/>).
/// </summary>
public class QuartermasterPanel : MonoBehaviour
{
    private const float Width = 860f;
    private const float Height = 740f;
    private const float Padding = 24f;
    private const float RowHeight = 30f;
    private const float LineHeight = 28f;
    private const float Gap = 6f;
    private const float Slot = 60f;
    private const float FilterWidth = 128f;
    private const float FilterHeight = 26f;
    private const float StoreHeight = 268f;
    private const float BagHeight = 70f;
    private const float BuildListWidth = 300f;
    private const float MaxDistance = 6f;
    private const float HandoffSeconds = 3f;
    private const float ConfirmSeconds = 3f;
    private const int FontSize = 14;
    private const int MaxCopies = 50;
    private const int Step = 10;
    private const string Infinity = "\u221E";

    private static readonly Color slotColor = new(0f, 0f, 0f, 0.45f);
    private static readonly Color slotHoverColor = new(1f, 0.8f, 0.4f, 0.3f);
    private static readonly Color unlimitedColor = new(1f, 0.8f, 0.4f);
    private static readonly Color selectedColor = new(1f, 0.8f, 0.4f);
    private static readonly Color lackingColor = new(1f, 0.45f, 0.35f);
    private static readonly Color subtleColor = new(0.75f, 0.75f, 0.75f);

    private static QuartermasterPanel instance;

    private enum Page
    {
        Store,
        Pack,
        Watch
    }

    private enum Build
    {
        Hammer,
        Blueprint,
        List
    }

    private readonly List<GameObject> storeSlots = new();
    private readonly List<GameObject> bagSlots = new();
    private readonly List<GameObject> filterButtons = new();
    private readonly List<GameObject> buildRows = new();
    private readonly List<GameObject> costRows = new();
    private readonly List<GameObject> watchRows = new();

    private QuartermasterStand stand;
    private List<Container> containers = new();
    private List<QuartermasterStore.Entry> entries = new();
    private List<KeyValuePair<string, int>> watches = new();
    private List<StockWatch.Low> low = new();
    private Container target;
    private string group;
    private Page page;
    private Job job;

    private Build build = Build.Hammer;
    private List<Blueprint> blueprints = new();
    private List<PackList> packLists = new();
    private string blueprintFile;
    private int listIndex = -1;
    private int copies = 1;
    private float deleteArmedUntil;

    private Text subtitle;
    private Text status;
    private Text targetLabel;
    private readonly Text[] tabs = new Text[3];
    private GameObject[] pages;
    private InputField search;
    private RectTransform filters;
    private RectTransform storeContent;
    private RectTransform bagContent;
    private RectTransform buildContent;
    private RectTransform costContent;
    private RectTransform watchContent;
    private InputField listName;
    private Text copiesLabel;
    private Text deleteLabel;
    private Text watchHint;

    // Something to do once the chests it touches are handed over.
    private sealed class Job
    {
        public List<Container> Needed;
        public Container Target;
        public Action<List<Container>> Run;
        public float Started;
    }

    /// <summary>True while the window is open.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// Opens the window for a table.
    /// </summary>
    /// <param name="stand">The table.</param>
    public static void Open(QuartermasterStand stand)
    {
        if (GUIManager.CustomGUIFront == null || Player.m_localPlayer == null)
        {
            return;
        }

        if (instance == null)
        {
            instance = Create();
        }

        instance.stand = stand;
        instance.target = null;
        instance.job = null;
        instance.group = null;
        instance.search.text = string.Empty;
        instance.gameObject.SetActive(true);
        GUIManager.BlockInput(true);
        instance.Refresh();
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
    public static void Close()
    {
        if (IsOpen)
        {
            instance.job = null;
            instance.gameObject.SetActive(false);
            GUIManager.BlockInput(false);
        }
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (Input.GetKeyDown(KeyCode.Escape) || stand == null || player == null || player.IsDead()
            || Vector3.Distance(player.transform.position, stand.transform.position) > MaxDistance)
        {
            Close();
            return;
        }

        if (job != null)
        {
            TryRunJob();
        }

        if (deleteArmedUntil > 0f && Time.unscaledTime > deleteArmedUntil)
        {
            deleteArmedUntil = 0f;
            deleteLabel.text = Localization.instance.Localize("$whitehilt_qm_delete");
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            GUIManager.BlockInput(false);
        }
    }

    // ---- Shared ----------------------------------------------------------------------------------------------------

    // Reads the chests again and redraws the page that is shown.
    private void Refresh()
    {
        containers = QuartermasterStore.Around(stand.transform.position);
        if (target != null && !containers.Contains(target))
        {
            target = null;
        }

        watches = stand.GetWatches();
        low = StockWatch.FindLow(Sources(), watches);
        subtitle.text = string.Format(Localization.instance.Localize("$whitehilt_qm_chests"), containers.Count,
            Mathf.RoundToInt(QuartermasterSettings.Range.Value));
        targetLabel.text = string.Format(Localization.instance.Localize("$whitehilt_qm_target"), TargetName(target));
        for (int index = 0; index < pages.Length; index++)
        {
            pages[index].SetActive((int)page == index);
            tabs[index].color = (int)page == index ? selectedColor : Color.white;
        }

        if (job == null)
        {
            status.text = string.Empty;
        }

        switch (page)
        {
            case Page.Store:
                entries = QuartermasterStore.List(Sources());
                if (group != null && entries.All(entry => entry.Group != group))
                {
                    group = null;
                }

                RefreshFilters();
                RefreshStore();
                RefreshBag();
                break;
            case Page.Pack:
                RefreshBuilds();
                break;
            case Page.Watch:
                RefreshWatches();
                break;
        }
    }

    // The containers to take from: everything around the table but the cart or ship being loaded.
    private List<Container> Sources()
    {
        return containers.Where(container => container != target).ToList();
    }

    private void Begin(IEnumerable<Container> needed, Action<List<Container>> run)
    {
        if (job != null)
        {
            return;
        }

        job = new Job { Needed = needed.Where(container => container != null).Distinct().ToList(), Target = target, Run = run, Started = Time.time };
        status.text = Localization.instance.Localize("$whitehilt_qm_waiting");
        TryRunJob();
    }

    // Runs the job once every chest it needs is handed over, or after a while with those that are; a chest someone
    // keeps open is left out.
    private void TryRunJob()
    {
        Player player = Player.m_localPlayer;
        long playerId = player != null ? player.GetPlayerID() : 0L;
        List<Container> alive = job.Needed.Where(container => container != null).ToList();
        List<Container> ready = alive.Where(container => ContainerHandoff.Ready(container, playerId)).ToList();
        if (ready.Count < alive.Count && Time.time - job.Started < HandoffSeconds)
        {
            return;
        }

        Job current = job;
        job = null;
        status.text = string.Empty;
        if (player != null)
        {
            if (current.Target != null && !ready.Contains(current.Target))
            {
                player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$whitehilt_qm_target_busy"));
            }
            else
            {
                current.Run(ready);
            }
        }

        Refresh();
    }

    private void NextTarget()
    {
        List<Container> targets = containers.Where(QuartermasterStore.IsCargo).ToList();
        int index = target == null ? -1 : targets.IndexOf(target);
        target = index + 1 < targets.Count ? targets[index + 1] : null;
        Refresh();
    }

    private string TargetName(Container container)
    {
        if (container == null)
        {
            return Localization.instance.Localize("$whitehilt_qm_target_bag");
        }

        int distance = Mathf.RoundToInt(Vector3.Distance(container.transform.position, stand.transform.position));
        return string.Format(Localization.instance.Localize("$whitehilt_qm_target_cargo"), Localization.instance.Localize(container.m_name), distance);
    }

    private static void Message(string text)
    {
        Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, text);
    }

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

    private void RefreshStore()
    {
        storeSlots.ForEach(Destroy);
        storeSlots.Clear();
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
            storeSlots.Add(CreateSlot(storeContent, entry.Sample, entry.Unlimited ? Infinity : entry.Count.ToString(), color, description,
                shift => OnStoreClick(chosen, shift), () => ToggleWatch(prefab)));
        }

        status.text = job != null ? status.text : Localization.instance.Localize(entries.Count == 0 ? "$whitehilt_qm_empty" : "$whitehilt_qm_take_hint");
    }

    private void RefreshBag()
    {
        bagSlots.ForEach(Destroy);
        bagSlots.Clear();
        List<QuartermasterStore.Entry> bag = QuartermasterStore.Returnable(Player.m_localPlayer, containers);
        foreach (QuartermasterStore.Entry entry in bag)
        {
            QuartermasterStore.Entry chosen = entry;
            bagSlots.Add(CreateSlot(bagContent, entry.Sample, entry.Count.ToString(), Color.white, Describe(entry), _ => OnPutBack(chosen), null));
        }

        if (bag.Count == 0)
        {
            GameObject empty = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_qm_bag_empty"), bagContent, Vector2.zero,
                Vector2.zero, Vector2.zero, GUIManager.Instance.AveriaSerif, FontSize, subtleColor, true, Color.black, 400f, Slot, false);
            empty.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            bagSlots.Add(empty);
        }
    }

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
            Message(string.Format(Localization.instance.Localize("$whitehilt_qm_unwatched"), QuartermasterStand.ItemName(prefab)));
        }
        else
        {
            ItemDrop item = ItemOf(prefab);
            watches.Add(new KeyValuePair<string, int>(prefab, Mathf.Max(1, item != null ? item.m_itemData.m_shared.m_maxStackSize : Step)));
            Message(string.Format(Localization.instance.Localize("$whitehilt_qm_watching"), QuartermasterStand.ItemName(prefab)));
        }

        stand.SetWatches(watches);
        Refresh();
    }

    // ---- Pack page -------------------------------------------------------------------------------------------------

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

    // ---- Watch page ------------------------------------------------------------------------------------------------

    private void RefreshWatches()
    {
        watchRows.ForEach(Destroy);
        watchRows.Clear();
        List<Container> sources = Sources();
        float width = Width - Padding * 2f - 20f;
        foreach (KeyValuePair<string, int> watch in watches)
        {
            QuartermasterStore.CountStock(sources, watch.Key, out int have, out bool unlimited);
            string amount = unlimited ? Localization.instance.Localize("$whitehilt_qm_unlimited") : have.ToString();
            string line = $"<b>{QuartermasterStand.ItemName(watch.Key)}</b>  "
                + string.Format(Localization.instance.Localize("$whitehilt_qm_watch_line"), amount, watch.Value);
            bool isLow = !unlimited && have < watch.Value;
            string prefab = watch.Key;
            watchRows.Add(CreateLine(watchContent, width, isLow ? $"<color=#{ColorUtility.ToHtmlStringRGB(lackingColor)}>{line}</color>" : line,
                () => ChangeWatch(prefab, -StepSize()), () => ChangeWatch(prefab, StepSize()), () => ChangeWatch(prefab, -int.MaxValue / 2)));
        }

        watchHint.text = Localization.instance.Localize(watches.Count == 0 ? "$whitehilt_qm_watch_empty" : "$whitehilt_qm_watch_hint");
    }

    private void ChangeWatch(string prefab, int change)
    {
        int index = watches.FindIndex(watch => watch.Key == prefab);
        if (index < 0)
        {
            return;
        }

        int threshold = watches[index].Value + change;
        if (threshold <= 0)
        {
            watches.RemoveAt(index);
        }
        else
        {
            watches[index] = new KeyValuePair<string, int>(prefab, threshold);
        }

        stand.SetWatches(watches);
        Refresh();
    }

    // ---- Building blocks -------------------------------------------------------------------------------------------

    private GameObject CreateSlot(Transform parent, ItemDrop.ItemData item, string amount, Color amountColor, string description,
        Action<bool> onClick, Action onRightClick)
    {
        GameObject slot = new("Slot", typeof(RectTransform));
        slot.transform.SetParent(parent, false);
        Image background = slot.AddComponent<Image>();
        background.color = slotColor;

        GameObject iconObject = new("Icon", typeof(RectTransform));
        iconObject.transform.SetParent(slot.transform, false);
        Image icon = iconObject.AddComponent<Image>();
        icon.sprite = item.GetIcon();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6f, 6f);
        iconRect.offsetMax = new Vector2(-6f, -6f);

        Text count = GUIManager.Instance.CreateText(amount, slot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-Slot / 2f + 1f, 10f),
            GUIManager.Instance.AveriaSerifBold, 14, amountColor, true, Color.black, Slot - 6f, 20f, false).GetComponent<Text>();
        count.alignment = TextAnchor.LowerRight;
        count.raycastTarget = false;
        if (amount == Infinity && count.font != null && !count.font.HasCharacter(Infinity[0]))
        {
            count.text = "MAX";
        }

        // Clicks come through an event trigger, not a button, to tell the left and the right mouse button apart.
        EventTrigger trigger = slot.AddComponent<EventTrigger>();
        EventTrigger.Entry click = new() { eventID = EventTriggerType.PointerClick };
        click.callback.AddListener(data =>
        {
            PointerEventData pointer = data as PointerEventData;
            if (pointer != null && pointer.button == PointerEventData.InputButton.Right)
            {
                onRightClick?.Invoke();
            }
            else if (pointer == null || pointer.button == PointerEventData.InputButton.Left)
            {
                onClick(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            }
        });
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            background.color = slotHoverColor;
            if (job == null)
            {
                status.text = description;
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

    private static QuartermasterPanel Create()
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Width, Height, true);
        panel.name = "WhiteHiltQuartermasterPanel";
        QuartermasterPanel window = panel.AddComponent<QuartermasterPanel>();
        window.BuildContents(panel.transform);
        return window;
    }

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
        Text bagTitle = AddText(page, Localization.instance.Localize("$whitehilt_qm_bag"), 0f, y, inner, 22f, FontSize);
        bagTitle.color = GUIManager.Instance.ValheimOrange;
        bagTitle.alignment = TextAnchor.MiddleLeft;
        y += 22f;
        bagContent = AddGrid(page, y, inner, BagHeight);
    }

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
