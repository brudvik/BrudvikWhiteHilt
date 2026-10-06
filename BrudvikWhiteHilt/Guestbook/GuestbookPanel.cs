using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Guestbook;

/// <summary>
/// The window that shows a guestbook's newest entries with their game time.
/// </summary>
public class GuestbookPanel : MonoBehaviour
{
    private const float Width = 620f;
    private const float Height = 640f;
    private const int ShownEntries = 26;

    private static GuestbookPanel instance;

    private Text entries;
    private GuestbookStand stand;

    /// <summary>True while the window is open.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// Opens the window for a guestbook.
    /// </summary>
    /// <param name="stand">The guestbook.</param>
    public static void Open(GuestbookStand stand)
    {
        if (GUIManager.CustomGUIFront == null)
        {
            return;
        }

        if (instance == null)
        {
            instance = Build();
        }

        instance.stand = stand;
        instance.gameObject.SetActive(true);
        GUIManager.BlockInput(true);
        Refresh(stand);
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
    public static void Close()
    {
        if (IsOpen)
        {
            instance.gameObject.SetActive(false);
            GUIManager.BlockInput(false);
        }
    }

    /// <summary>
    /// Shows the entries again if the window shows this guestbook.
    /// </summary>
    /// <param name="stand">The guestbook that changed.</param>
    public static void Refresh(GuestbookStand stand)
    {
        if (!IsOpen || instance.stand != stand || stand == null)
        {
            return;
        }

        List<GuestbookStand.Entry> list = stand.Entries();
        instance.entries.text = list.Count == 0
            ? Localization.instance.Localize("$whitehilt_guest_empty")
            : string.Join("\n", list.AsEnumerable().Reverse().Take(ShownEntries).Select(entry => $"<color=#E8B04B>{entry.Time()}</color>   {entry.Text()}"));
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (Input.GetKeyDown(KeyCode.Escape) || stand == null || player == null
            || Vector3.Distance(player.transform.position, stand.transform.position) > 8f)
        {
            Close();
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

    // Builds the guestbook panel once: a title, the entries and a close button.
    private static GuestbookPanel Build()
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Width, Height, true);
        GuestbookPanel book = panel.AddComponent<GuestbookPanel>();
        float inner = Width - 60f;
        Text title = GUIManager.Instance.CreateText(Localization.instance.Localize("$whitehilt_guest_title"), panel.transform, new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -34f), GUIManager.Instance.AveriaSerifBold, 26, GUIManager.Instance.ValheimOrange, true, Color.black,
            inner, 40f, false).GetComponent<Text>();
        title.alignment = TextAnchor.MiddleCenter;
        book.entries = GUIManager.Instance.CreateText(string.Empty, panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -70f - (Height - 150f) / 2f), GUIManager.Instance.AveriaSerif, 15, Color.white, true, Color.black, inner, Height - 150f, false)
            .GetComponent<Text>();
        book.entries.alignment = TextAnchor.UpperLeft;
        book.entries.supportRichText = true;
        book.entries.verticalOverflow = VerticalWrapMode.Truncate;
        GameObject close = GUIManager.Instance.CreateButton(Localization.instance.Localize("$whitehilt_guest_close"), panel.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), 160f, 36f);
        close.GetComponent<Button>().onClick.AddListener(Close);
        return book;
    }
}
