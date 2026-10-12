using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Chests;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
public partial class QuartermasterPanel : MonoBehaviour
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

    // Item slots are kept when hidden and filled anew, rather than built again at every refresh.
    private readonly List<ItemSlot> storeSlots = new();

    private readonly List<ItemSlot> bagSlots = new();

    private GameObject bagEmpty;

    private readonly List<GameObject> filterButtons = new();

    private readonly List<GameObject> buildRows = new();

    private readonly List<GameObject> costRows = new();

    private readonly List<GameObject> watchRows = new();

    private QuartermasterSite stand;

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

    private GameObject unloadButton;

    private Text unloadLabel;

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
    /// <param name="stand">The table or crane.</param>
    public static void Open(QuartermasterSite stand)
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
        // The crane loads the ship beside it; the table starts with the bag.
        Container preferred = stand.PreferredTarget(instance.containers);
        if (preferred != null)
        {
            instance.target = preferred;
            instance.Refresh();
        }
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

    // Closes the window when the player walks away, dies or presses Escape, runs a job still waiting for its chests to
    // be handed over, and puts the delete button back after its confirmation time has run out.
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
                unloadButton.SetActive(target != null);
                if (target != null)
                {
                    unloadLabel.text = string.Format(Localization.instance.Localize("$whitehilt_qm_unload"), Localization.instance.Localize(target.m_name));
                }
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
                if (current.Target != null)
                {
                    stand.OnCargoMoved(current.Target);
                }
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

    private static QuartermasterPanel Create()
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Width, Height, true);
        panel.name = "WhiteHiltQuartermasterPanel";
        QuartermasterPanel window = panel.AddComponent<QuartermasterPanel>();
        window.BuildContents(panel.transform);
        return window;
    }
}

/// <summary>An item slot of the Quartermaster panel, with what it shows and does now, so it can be filled anew.</summary>
internal sealed class ItemSlot : MonoBehaviour
{
    public Image Background;
    public Image Icon;
    public Text Count;
    public string Description;
    public Action<bool> OnClick;
    public Action OnRightClick;
}
