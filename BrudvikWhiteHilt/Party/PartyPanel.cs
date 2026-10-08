using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Navigation.Portraits;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Party;

/// <summary>
/// The other players in a compact list under the hotbar, nearest first: portrait, name, distance and the worst status
/// effects on one line, then health and stamina (and eitr, for those who have it) as bars with numbers. A row flashes
/// when its player takes a hard hit, the name turns red under a quarter of the health, and a dead player's row fades.
/// Hidden in the inventory, in build mode, on the large map and in menus.
/// </summary>
public static class PartyPanel
{
    private const float RefreshSeconds = 0.2f;
    private const float Left = 12f;

    // Below the hotbar slots, leaving room for the arrows, bolts or casts shown under them (HotbarSupply).
    private const float BelowHotbar = 30f;
    private const float FallbackTop = 150f;

    private const float Portrait = 30f;
    private const float Gap = 4f;
    private const float BarWidth = 96f;
    private const float NumberWidth = 52f;
    private const float Width = Portrait + Gap + BarWidth + Gap + NumberWidth;
    private const float NameHeight = 14f;
    private const float HealthHeight = 8f;
    private const float StaminaHeight = 4f;
    private const float EitrHeight = 3f;
    private const float LineGap = 2f;
    private const float RowGap = 5f;
    private const float EffectSize = 12f;
    private const float FlashSeconds = 0.6f;

    private static readonly Color textColor = new(1f, 0.95f, 0.85f);
    private static readonly Color dimColor = new(0.8f, 0.78f, 0.72f);
    private static readonly Color lowColor = new(1f, 0.4f, 0.35f);
    private static readonly Color backColor = new(0f, 0f, 0f, 0.55f);
    private static readonly Color flashColor = new(1f, 0.85f, 0.8f, 0.9f);
    private static readonly Color healthColor = new(0.8f, 0.16f, 0.1f);
    private static readonly Color staminaColor = new(1f, 0.78f, 0.2f);
    private static readonly Color eitrColor = new(0.6f, 0.45f, 1f);

    private static readonly List<Row> rows = new();
    private static readonly Vector3[] corners = new Vector3[4];

    private static RectTransform root;
    private static HotkeyBar hotbar;
    private static Sprite discSprite;
    private static float nextRefresh;

    /// <summary>
    /// Shows, hides and updates the list. Called after the game's HUD update.
    /// </summary>
    /// <param name="hud">The HUD.</param>
    public static void Update(Hud hud)
    {
        if (!BackpackInput.Typing() && BackpackInput.Pressed(PartySettings.KeyToggle))
        {
            PartySettings.Enabled.Value = !PartySettings.Enabled.Value;
        }

        Player player = Player.m_localPlayer;
        if (player == null || ZNet.instance == null || !ShouldShow(player))
        {
            SetVisible(false);
            return;
        }

        Ensure(hud);
        SetVisible(true);
        float now = Time.unscaledTime;
        foreach (Row row in rows)
        {
            row.Animate(now);
        }

        if (now < nextRefresh)
        {
            return;
        }

        nextRefresh = now + RefreshSeconds;
        Refresh(player);
    }

    private static bool ShouldShow(Player player)
    {
        return PartySettings.AllowParty.Value && PartySettings.Enabled.Value && !player.InPlaceMode()
            && !InventoryGui.IsVisible() && !Menu.IsVisible() && !TextInput.IsVisible() && !StoreGui.IsVisible()
            && (Minimap.instance == null || Minimap.instance.m_mode != Minimap.MapMode.Large);
    }

    private static void Refresh(Player player)
    {
        long self = ZDOMan.GetSessionID();
        List<ZNet.PlayerInfo> players = ZNet.instance.GetPlayerList();
        Vector3 here = player.transform.position;
        List<(PartyNetwork.Entry entry, float distance)> shown = PartyNetwork.Entries
            .Where(entry => entry.Uid != self || PartySettings.ShowSelf.Value)
            .Select(entry => (entry, entry.Uid == self ? 0f : Distance(entry.Uid, here, players)))
            .OrderBy(item => item.Item2 < 0f ? float.MaxValue : item.Item2)
            .ThenBy(item => item.entry.Status.Name)
            .Take(Mathf.Clamp(PartySettings.MaxPlayers.Value, 1, 16))
            .ToList();

        while (rows.Count < shown.Count)
        {
            rows.Add(new Row(root));
        }

        float y = 0f;
        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < shown.Count;
            rows[i].SetActive(used);
            if (used)
            {
                y -= rows[i].Show(shown[i].entry, shown[i].distance, y) + RowGap;
            }
        }

        root.sizeDelta = new Vector2(Width, -y);
        root.localScale = Vector3.one * Mathf.Clamp(PartySettings.Scale.Value, 0.6f, 2f);
        Place();
    }

    // How far away a player is: their character when it is near enough to be loaded, else the position they share on
    // the map; -1 when neither is known, as a player hiding their position should stay hidden here too.
    private static float Distance(long uid, Vector3 here, List<ZNet.PlayerInfo> players)
    {
        foreach (Player other in Player.GetAllPlayers())
        {
            if (other != null && other.m_nview != null && other.m_nview.IsValid() && other.m_nview.GetZDO().GetOwner() == uid)
            {
                return Vector3.Distance(here, other.transform.position);
            }
        }

        foreach (ZNet.PlayerInfo info in players)
        {
            if (info.m_characterID.UserID == uid && info.m_publicPosition)
            {
                return Vector3.Distance(here, info.m_position);
            }
        }

        return -1f;
    }

    // Under the lowest hotbar slot, with room for the supply line under it, so the list does not jump when a bow is
    // taken out.
    private static void Place()
    {
        RectTransform parent = (RectTransform)root.parent;
        float top = -FallbackTop;
        if (hotbar != null && hotbar.isActiveAndEnabled && hotbar.m_elements != null && hotbar.m_elements.Count > 0)
        {
            float lowest = float.MaxValue;
            foreach (HotkeyBar.ElementData element in hotbar.m_elements)
            {
                if (element?.m_go == null || element.m_go.transform is not RectTransform rect)
                {
                    continue;
                }

                rect.GetWorldCorners(corners);
                Vector3 bottom = corners[0] - rect.TransformVector(new Vector3(0f, BelowHotbar, 0f));
                lowest = Mathf.Min(lowest, parent.InverseTransformPoint(bottom).y);
            }

            if (lowest < float.MaxValue)
            {
                top = lowest - parent.rect.yMax;
            }
        }

        root.anchoredPosition = new Vector2(Left, top);
    }

    private static void SetVisible(bool visible)
    {
        if (root != null && root.gameObject.activeSelf != visible)
        {
            root.gameObject.SetActive(visible);
            nextRefresh = 0f;
        }
    }

    // Under the HUD root, so the list goes away with the HUD (Ctrl + F3, cutscenes, media mode).
    private static void Ensure(Hud hud)
    {
        if (root != null && root.parent == hud.m_rootObject.transform)
        {
            return;
        }

        rows.Clear();
        GameObject list = new("WhiteHiltParty", typeof(RectTransform));
        root = (RectTransform)list.transform;
        root.SetParent(hud.m_rootObject.transform, false);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(Width, 10f);
        hotbar = hud.GetComponentInChildren<HotkeyBar>(true);
        nextRefresh = 0f;
    }

    // A round, soft-edged disc behind players without a portrait, with their initial on it.
    private static Sprite Disc()
    {
        if (discSprite != null)
        {
            return discSprite;
        }

        const int size = 64;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
            }
        }

        texture.Apply();
        discSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return discSprite;
    }

    private static Text Label(Transform parent, string name, int size, TextAnchor alignment)
    {
        GameObject text = GUIManager.Instance.CreateText(string.Empty, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, size, textColor, true, Color.black, 10f, 10f, false);
        text.name = name;
        Text label = text.GetComponent<Text>();
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
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
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        return image;
    }

    private static void Put(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    // A bar with its number to the right.
    private sealed class Bar
    {
        private readonly Image back;
        private readonly RectTransform fill;
        private readonly Text number;
        private readonly float height;

        public Bar(Transform parent, string name, Color color, float height, int fontSize)
        {
            this.height = height;
            back = Box(parent, name, backColor);
            Image fillImage = Box(back.transform, "Fill", color);
            fill = fillImage.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = Vector2.zero;
            number = Label(parent, name + "Number", fontSize, TextAnchor.MiddleLeft);
        }

        public Image Back => back;

        public Text Number => number;

        public void SetActive(bool active)
        {
            back.gameObject.SetActive(active);
            number.gameObject.SetActive(active);
        }

        // Places the bar at y, with the number centred on it; returns the height it takes.
        public float Show(float y, float value, float max, string text)
        {
            Put(back.rectTransform, Portrait + Gap, y, BarWidth, height);
            fill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(value / max) : 0f, 1f);
            float textHeight = Mathf.Max(height, number.fontSize + 2f);
            Put(number.rectTransform, Portrait + Gap + BarWidth + Gap, y + (textHeight - height) / 2f, NumberWidth, textHeight);
            if (number.text != text)
            {
                number.text = text;
            }

            return height;
        }
    }

    private sealed class Row
    {
        private readonly RectTransform rect;
        private readonly CanvasGroup group;
        private readonly Image disc;
        private readonly Text initial;
        private readonly RawImage portrait;
        private readonly Text name;
        private readonly Text distance;
        private readonly List<Image> effects = new();
        private readonly Bar health;
        private readonly Bar stamina;
        private readonly Bar eitr;

        private float flashUntil;
        private bool low;

        public Row(Transform parent)
        {
            GameObject row = new("Player", typeof(RectTransform), typeof(CanvasGroup));
            rect = (RectTransform)row.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            group = row.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            disc = Box(rect, "Disc", new Color(0.25f, 0.22f, 0.2f, 0.9f));
            disc.sprite = Disc();
            initial = Label(disc.transform, "Initial", 16, TextAnchor.MiddleCenter);
            Put(initial.rectTransform, 0f, 0f, Portrait, Portrait);

            GameObject picture = new("Portrait", typeof(RectTransform), typeof(RawImage));
            picture.transform.SetParent(rect, false);
            portrait = picture.GetComponent<RawImage>();
            portrait.raycastTarget = false;
            portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = new Vector2(0f, 1f);
            portrait.rectTransform.pivot = new Vector2(0f, 1f);

            name = Label(rect, "Name", 13, TextAnchor.MiddleLeft);
            distance = Label(rect, "Distance", 11, TextAnchor.MiddleRight);
            distance.color = dimColor;
            health = new Bar(rect, "Health", healthColor, HealthHeight, 11);
            stamina = new Bar(rect, "Stamina", staminaColor, StaminaHeight, 9);
            eitr = new Bar(rect, "Eitr", eitrColor, EitrHeight, 9);
            stamina.Number.color = dimColor;
            eitr.Number.color = dimColor;
        }

        public void SetActive(bool active)
        {
            if (rect.gameObject.activeSelf != active)
            {
                rect.gameObject.SetActive(active);
            }
        }

        // Fills the row from a player's status at y; returns the height it takes.
        public float Show(PartyNetwork.Entry entry, float metres, float y)
        {
            PartyStatus status = entry.Status;
            rect.anchoredPosition = new Vector2(0f, y);

            ShowPortrait(entry.Uid, status.Name);
            Put(name.rectTransform, Portrait + Gap, 0f, Width - Portrait - Gap, NameHeight);
            if (name.text != status.Name)
            {
                name.text = status.Name;
            }

            string far = PartySettings.ShowDistance.Value && metres >= 0f ? $"{Mathf.RoundToInt(metres)} m" : string.Empty;
            distance.text = far;
            Put(distance.rectTransform, 0f, 0f, Width, NameHeight);
            ShowEffects(PartySettings.ShowEffects.Value ? status.Effects : null, Portrait + Gap + Mathf.Min(name.preferredWidth, BarWidth) + Gap);

            float line = NameHeight + LineGap;
            string healthText = status.Dead ? Localization.instance.Localize("$whitehilt_party_dead") : PartyStatus.Amount(status.Health, status.MaxHealth);
            line += health.Show(-line, status.Dead ? 0f : status.Health, status.MaxHealth, healthText) + LineGap;

            bool showStamina = PartySettings.ShowStamina.Value && !status.Dead;
            stamina.SetActive(showStamina);
            if (showStamina)
            {
                line += stamina.Show(-line, status.Stamina, status.MaxStamina, PartyStatus.Amount(status.Stamina, status.MaxStamina)) + LineGap + 2f;
            }

            bool showEitr = PartySettings.ShowEitr.Value && !status.Dead && status.MaxEitr > 0f;
            eitr.SetActive(showEitr);
            if (showEitr)
            {
                line += eitr.Show(-line, status.Eitr, status.MaxEitr, PartyStatus.Amount(status.Eitr, status.MaxEitr)) + LineGap + 2f;
            }

            // A hit worth seeing: a tenth of the maximum, and at least 10, lost since the status before.
            PartyStatus before = entry.Previous;
            if (before != null && !status.Dead && before.Health - status.Health >= Mathf.Max(10f, status.MaxHealth * 0.1f)
                && Time.unscaledTime - entry.Received < 0.5f)
            {
                flashUntil = Time.unscaledTime + FlashSeconds;
            }

            low = !status.Dead && status.MaxHealth > 0f && status.Health / status.MaxHealth < 0.25f;
            name.color = low ? lowColor : textColor;
            group.alpha = status.Dead ? 0.45f : 1f;

            float height = Mathf.Max(Portrait, line - LineGap);
            rect.sizeDelta = new Vector2(Width, height);
            return height;
        }

        // The hard-hit flash fades out; a player low on health has a slowly pulsing bar.
        public void Animate(float now)
        {
            if (!rect.gameObject.activeSelf)
            {
                return;
            }

            float flash = Mathf.Clamp01((flashUntil - now) / FlashSeconds);
            float pulse = low ? (Mathf.Sin(now * 6f) + 1f) * 0.2f : 0f;
            health.Back.color = Color.Lerp(backColor, flashColor, Mathf.Max(flash, pulse));
        }

        private void ShowPortrait(long uid, string playerName)
        {
            Texture2D texture = PortraitSettings.Enabled.Value ? PortraitNetwork.Get(uid) : null;
            portrait.gameObject.SetActive(texture != null);
            disc.gameObject.SetActive(texture == null);
            if (texture != null)
            {
                portrait.texture = texture;
                Put(portrait.rectTransform, 0f, 0f, Portrait, Portrait);
                return;
            }

            Put(disc.rectTransform, 0f, 0f, Portrait, Portrait);
            string letter = string.IsNullOrEmpty(playerName) ? "?" : playerName.Substring(0, 1).ToUpperInvariant();
            if (initial.text != letter)
            {
                initial.text = letter;
            }
        }

        private void ShowEffects(int[] hashes, float x)
        {
            int count = 0;
            if (hashes != null && ObjectDB.instance != null)
            {
                foreach (int hash in hashes)
                {
                    StatusEffect effect = ObjectDB.instance.GetStatusEffect(hash);
                    if (effect == null || effect.m_icon == null)
                    {
                        continue;
                    }

                    if (effects.Count <= count)
                    {
                        Image icon = Box(rect, "Effect", Color.white);
                        icon.preserveAspect = true;
                        effects.Add(icon);
                    }

                    Image image = effects[count++];
                    image.sprite = effect.m_icon;
                    image.gameObject.SetActive(true);
                    Put(image.rectTransform, x, -(NameHeight - EffectSize) / 2f, EffectSize, EffectSize);
                    x += EffectSize + 1f;
                }
            }

            for (int i = count; i < effects.Count; i++)
            {
                effects[i].gameObject.SetActive(false);
            }
        }
    }
}
