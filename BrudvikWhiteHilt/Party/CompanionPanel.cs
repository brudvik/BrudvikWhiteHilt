using BrudvikWhiteHilt.Companions;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Party;

/// <summary>
/// The player's companions under the party list: their own dogs and the tamed animals following them, nearest first,
/// each with its name, distance and health. Shown and hidden with the party list.
/// </summary>
public static class CompanionPanel
{
    private const float RefreshSeconds = 0.5f;
    private const float Width = 182f;
    private const float NameHeight = 15f;
    private const float BarHeight = 5f;
    private const float LineGap = 2f;
    private const float RowGap = 4f;
    private const float ListGap = 8f;

    private static readonly Color textColor = new(1f, 0.95f, 0.85f);
    private static readonly Color dimColor = new(0.8f, 0.78f, 0.72f);
    private static readonly Color lowColor = new(1f, 0.4f, 0.35f);
    private static readonly Color backColor = new(0f, 0f, 0f, 0.55f);
    private static readonly Color healthColor = new(0.8f, 0.16f, 0.1f);
    private static readonly Color healthBack = new(0.23f, 0.14f, 0.13f, 0.85f);

    private static readonly List<Row> rows = new();
    private static readonly List<Character> companions = new();

    private static RectTransform root;
    private static float nextRefresh;

    /// <summary>
    /// Shows, hides and updates the list. Called after the party list's update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void Update(Hud hud)
    {
        Player player = Player.m_localPlayer;
        RectTransform party = PartyPanel.Root;
        if (player == null || party == null || !PartySettings.ShowCompanions.Value)
        {
            SetVisible(false);
            return;
        }

        Ensure(hud);
        if (Time.unscaledTime < nextRefresh)
        {
            Place(party);
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshSeconds;
        Collect(player);
        SetVisible(companions.Count > 0);
        if (companions.Count == 0)
        {
            return;
        }

        while (rows.Count < companions.Count)
        {
            rows.Add(new Row(root));
        }

        Vector3 here = player.transform.position;
        float y = 0f;
        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < companions.Count;
            rows[i].SetActive(used);
            if (used)
            {
                y -= rows[i].Show(companions[i], Vector3.Distance(here, companions[i].transform.position), y) + RowGap;
            }
        }

        root.sizeDelta = new Vector2(Width, -y);
        Place(party);
    }

    // The player's own loaded dogs, then every tamed animal following them, nearest first.
    private static void Collect(Player player)
    {
        companions.Clear();
        long id = player.GetPlayerID();
        foreach (DogCompanion dog in DogCompanion.All)
        {
            Character character = dog != null && dog.OwnerId == id ? dog.GetComponent<Character>() : null;
            if (character != null && !character.IsDead())
            {
                companions.Add(character);
            }
        }

        foreach (Character character in Character.GetAllCharacters())
        {
            if (character == null || character.IsPlayer() || character.IsDead() || !character.IsTamed() || companions.Contains(character))
            {
                continue;
            }

            if (character.GetBaseAI() is MonsterAI ai && ai.GetFollowTarget() == player.gameObject)
            {
                companions.Add(character);
            }
        }

        Vector3 here = player.transform.position;
        companions.Sort((a, b) => Vector3.Distance(here, a.transform.position).CompareTo(Vector3.Distance(here, b.transform.position)));
        int max = Mathf.Clamp(PartySettings.MaxCompanions.Value, 1, 16);
        if (companions.Count > max)
        {
            companions.RemoveRange(max, companions.Count - max);
        }
    }

    // Under the party list, as wide and as large as it.
    private static void Place(RectTransform party)
    {
        float scale = party.localScale.y;
        float below = party.sizeDelta.y * scale;
        root.localScale = party.localScale;
        root.anchoredPosition = party.anchoredPosition + new Vector2(0f, -(below + (below > 0f ? ListGap * scale : 0f)));
    }

    private static void SetVisible(bool visible)
    {
        if (root != null && root.gameObject.activeSelf != visible)
        {
            root.gameObject.SetActive(visible);
            nextRefresh = 0f;
        }
    }

    // Under the HUD root, so the list goes away with the HUD.
    private static void Ensure(Hud hud)
    {
        if (root != null && root.parent == hud.m_rootObject.transform)
        {
            return;
        }

        rows.Clear();
        GameObject list = new("WhiteHiltCompanions", typeof(RectTransform));
        root = (RectTransform)list.transform;
        root.SetParent(hud.m_rootObject.transform, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(Width, 10f);
        nextRefresh = 0f;
    }

    private static Text Label(Transform parent, string name, TextAnchor alignment, Color color)
    {
        GameObject text = GUIManager.Instance.CreateText(string.Empty, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, 13, color, true, Color.black, Width, NameHeight, false);
        text.name = name;
        Text label = text.GetComponent<Text>();
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.raycastTarget = false;
        label.rectTransform.pivot = new Vector2(0f, 1f);
        return label;
    }

    private static Image Box(Transform parent, string name, Color color)
    {
        GameObject box = new(name, typeof(RectTransform), typeof(Image));
        box.transform.SetParent(parent, false);
        Image image = box.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        return image;
    }

    // One companion: its name and distance, and its health as a bar under them.
    private sealed class Row
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Text name;
        private readonly Text distance;
        private readonly Image barBack;
        private readonly Image bar;

        internal Row(Transform parent)
        {
            GameObject go = new("Companion", typeof(RectTransform));
            root = (RectTransform)go.transform;
            root.SetParent(parent, false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
            back = Box(root, "Back", backColor);
            name = Label(root, "Name", TextAnchor.UpperLeft, textColor);
            distance = Label(root, "Distance", TextAnchor.UpperRight, dimColor);
            barBack = Box(root, "HealthBack", healthBack);
            bar = Box(root, "Health", healthColor);
        }

        internal void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        // Returns the row's height.
        internal float Show(Character character, float metres, float y)
        {
            const float pad = 4f;
            float inner = Width - 2f * pad;
            float height = pad + NameHeight + LineGap + BarHeight + pad;
            root.anchoredPosition = new Vector2(0f, y);
            root.sizeDelta = new Vector2(Width, height);
            back.rectTransform.anchoredPosition = Vector2.zero;
            back.rectTransform.sizeDelta = new Vector2(Width, height);

            float share = Mathf.Clamp01(character.GetHealthPercentage());
            name.text = character.GetHoverName();
            name.color = share < 0.25f ? lowColor : textColor;
            name.rectTransform.anchoredPosition = new Vector2(pad, -pad);
            name.rectTransform.sizeDelta = new Vector2(inner, NameHeight);
            distance.text = $"{Mathf.RoundToInt(metres)} m";
            distance.rectTransform.anchoredPosition = new Vector2(pad, -pad);
            distance.rectTransform.sizeDelta = new Vector2(inner, NameHeight);

            float barY = -(pad + NameHeight + LineGap);
            barBack.rectTransform.anchoredPosition = new Vector2(pad, barY);
            barBack.rectTransform.sizeDelta = new Vector2(inner, BarHeight);
            bar.rectTransform.anchoredPosition = new Vector2(pad, barY);
            bar.rectTransform.sizeDelta = new Vector2(inner * share, BarHeight);
            return height;
        }
    }
}
