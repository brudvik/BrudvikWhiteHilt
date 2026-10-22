using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// The household panel, opened at the High Seat: who lives on the farm, what each one does, whether they have a bed of
/// their own and what they have been lent, with buttons to give them a bed, take the lent gear back and send them home.
/// Needs, mood and jobs join it in later phases (docs/plans/household.md). It shows the members loaded on this machine,
/// which within the farm's reach is all of them.
/// </summary>
public class HouseholdPanel : MonoBehaviour
{
    private const float Width = 780f;
    private const float Height = 700f;
    private const float RowHeight = 42f;
    private const float Top = 108f;
    private const float RefreshSeconds = 1f;
    private const float CloseDistance = 8f;

    private static readonly Color dim = new(0.75f, 0.72f, 0.65f);

    private static HouseholdPanel instance;

    private readonly List<GameObject> rows = new();
    private HighSeat seat;
    private Text title;
    private Text summary;
    private float nextRefresh;

    /// <summary>True while the panel is open.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// Registers the texts of the panel and of the High Seat. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_hs_count", "Household: $1 of $2");
        Translations.AddEnglish("whitehilt_hs_sit", "Sit and lead the household");
        Translations.AddEnglish("whitehilt_hs_open", "The household");
        Translations.AddEnglish("whitehilt_hs_too_near", "Another High Seat's farm reaches here.");
        Translations.AddEnglish("whitehilt_hs_title", "The household of $1");
        Translations.AddEnglish("whitehilt_hs_summary", "$1 of $2 live here. The farm reaches $3 m from the seat.");
        Translations.AddEnglish("whitehilt_hs_empty", "Nobody lives here yet.");
        Translations.AddEnglish("whitehilt_hs_bed_own", "Own bed");
        Translations.AddEnglish("whitehilt_hs_bed_none", "No bed");
        Translations.AddEnglish("whitehilt_hs_lent", "Lent: $1");
        Translations.AddEnglish("whitehilt_hs_bed", "Bed");
        Translations.AddEnglish("whitehilt_hs_take_back", "Take back");
        Translations.AddEnglish("whitehilt_hs_home", "Home");
        Translations.AddEnglish("whitehilt_hs_close", "Close");
        Translations.AddEnglish("whitehilt_hs_no_free_bed", "There is no free bed on the farm. A bed a player has slept in is theirs.");
        Translations.AddEnglish("whitehilt_hs_given_bed", "$1 sleeps in that bed now.");
        Translations.AddEnglish("whitehilt_hs_taken_back", "You took back $1 things from $2.");
    }

    /// <summary>
    /// Opens the panel for a High Seat.
    /// </summary>
    /// <param name="seat">The High Seat.</param>
    public static void Open(HighSeat seat)
    {
        if (GUIManager.CustomGUIFront == null || seat == null)
        {
            return;
        }

        if (instance == null)
        {
            instance = Build();
        }

        instance.seat = seat;
        instance.gameObject.SetActive(true);
        GUIManager.BlockInput(true);
        instance.Refresh();
    }

    /// <summary>
    /// Closes the panel.
    /// </summary>
    public static void Close()
    {
        if (IsOpen)
        {
            instance.gameObject.SetActive(false);
            GUIManager.BlockInput(false);
        }
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (Input.GetKeyDown(KeyCode.Escape) || seat == null || player == null
            || Vector3.Distance(player.transform.position, seat.transform.position) > CloseDistance)
        {
            Close();
            return;
        }

        if (Time.unscaledTime >= nextRefresh)
        {
            Refresh();
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

    // Lists the members again: one row each, with their state and the buttons.
    private void Refresh()
    {
        nextRefresh = Time.unscaledTime + RefreshSeconds;
        foreach (GameObject row in rows)
        {
            Destroy(row);
        }

        rows.Clear();
        List<HouseholdMember> members = seat.Members();
        title.text = Localization.instance.Localize("$whitehilt_hs_title", seat.DisplayName);
        summary.text = members.Count == 0
            ? Localization.instance.Localize("$whitehilt_hs_empty")
            : Localization.instance.Localize("$whitehilt_hs_summary", members.Count.ToString(),
                HouseholdSettings.MaxPerFarm.Value.ToString(), Mathf.RoundToInt(HouseholdSettings.FarmRadius.Value).ToString());
        int shown = Mathf.Min(members.Count, (int)((Height - Top - 80f) / RowHeight));
        for (int i = 0; i < shown; i++)
        {
            rows.Add(Row(members[i], -Top - i * RowHeight));
        }
    }

    // One member: name and role, what they are doing, their bed and what they were lent, and the three buttons.
    private GameObject Row(HouseholdMember member, float y)
    {
        GameObject row = new("Member", typeof(RectTransform));
        RectTransform rect = (RectTransform)row.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(Width - 60f, RowHeight);

        string role = member.Role == HouseholdRole.Huskarl ? "$whitehilt_hh_huskarl" : "$whitehilt_hh_farmhand";
        Label(row.transform, $"<b>{member.MemberName}</b>  <color=#a0a0a0>{Localization.instance.Localize(role)}</color>", -350f, 205f, Color.white);
        Label(row.transform, Localization.instance.Localize(State(member)), -140f, 125f, dim);
        string bed = !member.HasBed ? "$whitehilt_hs_bed_none" : "$whitehilt_hs_bed_own";
        int lent = member.GivenCount;
        string things = Localization.instance.Localize(bed) + (lent > 0 ? "  ·  " + Localization.instance.Localize("$whitehilt_hs_lent", lent.ToString()) : string.Empty);
        Label(row.transform, things, -10f, 150f, dim);

        Button(row.transform, "$whitehilt_hs_bed", 180f, () => GiveBed(member));
        Button(row.transform, "$whitehilt_hs_take_back", 260f, () => TakeBack(member)).interactable = lent > 0;
        Button(row.transform, "$whitehilt_hs_home", 340f, () =>
        {
            member.GoHome();
            Refresh();
        });
        return row;
    }

    private static string State(HouseholdMember member)
    {
        long me = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        return member.FollowsPlayerId == 0L ? "$whitehilt_hh_waiting"
            : member.FollowsPlayerId == me ? "$whitehilt_hh_following" : "$whitehilt_hh_following_other";
    }

    // The next free bed on the farm, after the one they have: pressing again moves them on to the next.
    private void GiveBed(HouseholdMember member)
    {
        List<Bed> beds = seat.FreeBeds(member);
        Player player = Player.m_localPlayer;
        if (beds.Count == 0)
        {
            player?.Message(MessageHud.MessageType.Center, "$whitehilt_hs_no_free_bed");
            return;
        }

        int current = beds.FindIndex(member.Sleeps);
        member.SetBed(beds[(current + 1) % beds.Count]);
        player?.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_hs_given_bed", member.MemberName));
        Refresh();
    }

    private void TakeBack(HouseholdMember member)
    {
        Player player = Player.m_localPlayer;
        int count = member.TakeBackGear(player);
        if (count > 0)
        {
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_hs_taken_back", count.ToString(), member.MemberName));
        }

        Refresh();
    }

    private static void Label(Transform parent, string text, float x, float width, Color color)
    {
        Text label = GUIManager.Instance.CreateText(text, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x + width / 2f, 0f), GUIManager.Instance.AveriaSerifBold, 16, color, true, Color.black, width, RowHeight, false)
            .GetComponent<Text>();
        label.alignment = TextAnchor.MiddleLeft;
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    private static Button Button(Transform parent, string text, float x, UnityEngine.Events.UnityAction onClick)
    {
        Button button = GUIManager.Instance.CreateButton(Localization.instance.Localize(text), parent, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(x, 0f), 74f, 32f).GetComponent<Button>();
        button.onClick.AddListener(onClick);
        return button;
    }

    // Builds the panel once: the title, the summary line and the close button. The rows are made by Refresh.
    private static HouseholdPanel Build()
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Width, Height, true);
        HouseholdPanel household = panel.AddComponent<HouseholdPanel>();
        float inner = Width - 60f;
        household.title = GUIManager.Instance.CreateText(string.Empty, panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), GUIManager.Instance.AveriaSerifBold, 26, GUIManager.Instance.ValheimOrange, true, Color.black, inner, 40f, false)
            .GetComponent<Text>();
        household.title.alignment = TextAnchor.MiddleCenter;
        household.summary = GUIManager.Instance.CreateText(string.Empty, panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -72f), GUIManager.Instance.AveriaSerif, 16, dim, true, Color.black, inner, 26f, false).GetComponent<Text>();
        household.summary.alignment = TextAnchor.MiddleCenter;
        GameObject close = GUIManager.Instance.CreateButton(Localization.instance.Localize("$whitehilt_hs_close"), panel.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), 160f, 36f);
        close.GetComponent<Button>().onClick.AddListener(Close);
        return household;
    }
}
