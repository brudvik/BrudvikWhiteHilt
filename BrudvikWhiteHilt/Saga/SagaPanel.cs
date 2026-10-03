using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Saga;

/// <summary>
/// The saga window: the player's rank, renown and its rewards, and the newest deeds with their days.
/// </summary>
public class SagaPanel : MonoBehaviour
{
    private const float Width = 620f;
    private const float Height = 680f;
    private const int ShownEntries = 26;

    private static SagaPanel instance;

    private Text title;
    private Text rank;
    private Text reward;
    private Text entries;

    /// <summary>True while the window is open.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// Opens or closes the window when its key is pressed. Call every frame.
    /// </summary>
    public static void CheckKey()
    {
        if (SagaSettings.Key == null || Player.m_localPlayer == null || Backpack.BackpackInput.Typing() && !IsOpen)
        {
            return;
        }

        if (Backpack.BackpackInput.Pressed(SagaSettings.Key) || (IsOpen && Input.GetKeyDown(KeyCode.Escape)))
        {
            Toggle();
        }
    }

    /// <summary>
    /// Opens the window, or closes it when it is open.
    /// </summary>
    public static void Toggle()
    {
        if (IsOpen)
        {
            instance.gameObject.SetActive(false);
            GUIManager.BlockInput(false);
            return;
        }

        if (GUIManager.CustomGUIFront == null || Player.m_localPlayer == null || !SagaSettings.Enabled.Value)
        {
            return;
        }

        if (instance == null)
        {
            instance = Build();
        }

        instance.gameObject.SetActive(true);
        GUIManager.BlockInput(true);
        Refresh();
    }

    /// <summary>
    /// Shows the current saga, if the window is open.
    /// </summary>
    public static void Refresh()
    {
        Player player = Player.m_localPlayer;
        if (!IsOpen || player == null)
        {
            return;
        }

        int renown = SagaLog.Renown(player);
        int current = SagaLog.Rank(player);
        int max = SagaSettings.MaxRank.Value;
        string next = current < max
            ? string.Format(Localization.instance.Localize("$whitehilt_saga_next"), (current + 1) * Mathf.Max(1, SagaSettings.RenownPerRank.Value))
            : string.Empty;
        instance.title.text = string.Format(Localization.instance.Localize("$whitehilt_saga_title"), player.GetPlayerName());
        instance.rank.text = string.Format(Localization.instance.Localize("$whitehilt_saga_rank"), current, max, renown, next);
        instance.reward.text = string.Format(Localization.instance.Localize("$whitehilt_saga_reward"),
            Mathf.RoundToInt(current * SagaSettings.CarryPerRank.Value), Mathf.RoundToInt(current * SagaSettings.StaminaPerRank.Value));

        List<SagaLog.Entry> deeds = SagaLog.Entries(player);
        if (deeds.Count == 0)
        {
            instance.entries.text = Localization.instance.Localize("$whitehilt_saga_empty");
            return;
        }

        string day = Localization.instance.Localize("$whitehilt_saga_day");
        instance.entries.text = string.Join("\n", deeds.AsEnumerable().Reverse().Take(ShownEntries)
            .Select(entry => $"<color=#E8B04B>{string.Format(day, entry.Day)}</color>   {entry.Text()}"));
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            GUIManager.BlockInput(false);
        }
    }

    private static SagaPanel Build()
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Width, Height, true);
        SagaPanel saga = panel.AddComponent<SagaPanel>();
        saga.title = AddText(panel.transform, -30f, 26, 40f, GUIManager.Instance.ValheimOrange, TextAnchor.MiddleCenter);
        saga.title.font = GUIManager.Instance.AveriaSerifBold;
        saga.rank = AddText(panel.transform, -70f, 16, 26f, Color.white, TextAnchor.MiddleCenter);
        saga.reward = AddText(panel.transform, -96f, 14, 24f, new Color(0.75f, 0.85f, 0.65f), TextAnchor.MiddleCenter);
        saga.entries = AddText(panel.transform, -126f, 15, Height - 200f, Color.white, TextAnchor.UpperLeft);

        GameObject close = GUIManager.Instance.CreateButton(Localization.instance.Localize("$whitehilt_saga_close"), panel.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), 160f, 36f);
        close.GetComponent<Button>().onClick.AddListener(Toggle);
        return saga;
    }

    private static Text AddText(Transform parent, float top, int size, float height, Color colour, TextAnchor anchor)
    {
        float width = Width - 60f;
        Text text = GUIManager.Instance.CreateText(string.Empty, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, top - height / 2f), GUIManager.Instance.AveriaSerif, size, colour, true, Color.black, width, height, false).GetComponent<Text>();
        text.alignment = anchor;
        text.supportRichText = true;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
}
