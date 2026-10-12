using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.HudLayout;

public static partial class HudConsole
{
    // Fades a part of the console with Calm: shown while it is awake and for a while after, else faded to the floor.
    private sealed class Fader
    {
        private readonly CanvasGroup group;
        private float awake = float.MinValue;

        internal Fader(CanvasGroup group)
        {
            this.group = group;
            if (group != null)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        internal float Alpha { get; private set; } = 1f;

        internal void Wake(bool changed)
        {
            if (changed)
            {
                awake = Time.unscaledTime;
            }
        }

        internal void Apply(bool calm, float now, float linger, float floor)
        {
            float target = !calm || now - awake < linger ? 1f : floor;
            Alpha = Mathf.MoveTowards(Alpha, target, Time.unscaledDeltaTime * FadeSpeed);
            if (group != null)
            {
                group.alpha = Alpha;
            }
        }
    }

    // A status effect or an indicator as the tray shows it.
    private readonly struct TrayItem
    {
        internal readonly Sprite Icon;
        internal readonly string Text;
        internal readonly Color Tint;
        internal readonly bool Flash;

        // Share of the time left, for the countdown ring; below 0 for none.
        internal readonly float Remaining;

        // The name, shown in the list; null when only the icons show.
        internal readonly string Name;

        internal TrayItem(Sprite icon, string text, Color tint, bool flash, float remaining, string name)
        {
            Icon = icon;
            Text = text ?? string.Empty;
            Tint = tint;
            Flash = flash;
            Remaining = remaining;
            Name = name ?? string.Empty;
        }
    }

    // One vital: the number over a bar.
    private sealed class Bar
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Image fill;
        private readonly Text number;
        private string shown;

        internal Bar(Transform parent, string name, Color color, Color backColor)
        {
            root = Element(parent, name);
            Fader = new Fader(root.gameObject.AddComponent<CanvasGroup>());
            back = Box(root, "Back", backColor);
            fill = Box(root, "Fill", color);
            number = Label(root, "Number", TextAnchor.LowerLeft);
        }

        internal Fader Fader { get; }

        internal void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        internal void Show(float value, float max, Vector2 position, float width, float scale, bool low)
        {
            SetActive(true);
            float height = BarHeight * scale;
            Place(root, position, new Vector2(width, height + NumberHeight * scale));
            Place(back.rectTransform, Vector2.zero, new Vector2(width, height));
            float share = max > 0f ? Mathf.Clamp01(value / max) : 0f;
            Place(fill.rectTransform, Vector2.zero, new Vector2(width * share, height));
            Place(number.rectTransform, new Vector2(0f, height + 1f), new Vector2(width, NumberHeight * scale));
            number.fontSize = Mathf.RoundToInt(15f * scale);
            string text = $"<color=#{Hex(low ? lowColor : textColor)}>{Mathf.CeilToInt(value)}</color>"
                + $"<size={Mathf.RoundToInt(12f * scale)}><color=#{Hex(dimColor)}> / {Mathf.CeilToInt(max)}</color></size>";
            if (text != shown)
            {
                number.text = text;
                shown = text;
            }
        }
    }

    // One food: the icon on a dark disc, a ring round it that empties and turns from green to red as the food runs
    // out, and the time left under it.
    private sealed class FoodSlot
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Image track;
        private readonly Image ring;
        private readonly Image icon;
        private readonly Image crossA;
        private readonly Image crossB;
        private readonly Text time;

        internal FoodSlot(Transform parent)
        {
            root = Element(parent, "Food");
            back = Box(root, "Back", panelColor);
            back.sprite = HudSprites.Disc;
            track = Box(root, "Track", Faint(foodFull));
            track.sprite = HudSprites.Ring;
            ring = Ring(root, "Ring", foodFull);
            icon = Box(root, "Icon", Color.white);
            icon.preserveAspect = true;
            crossA = Box(root, "Cross", foodLow);
            crossB = Box(root, "Cross", foodLow);
            foreach (Image bar in new[] { crossA, crossB })
            {
                bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            crossA.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            crossB.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            crossA.gameObject.SetActive(false);
            crossB.gameObject.SetActive(false);
            time = Label(root, "Time", TextAnchor.UpperCenter);
        }

        internal void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        // A place with no food in it: the dark disc in a red ring, a red cross over it and no time under it. The ring
        // pulses when nothing at all is eaten.
        internal void ShowEmpty(Vector2 position, float size, float scale, bool starving)
        {
            Layout(position, size, scale);
            SetEmpty(true);
            Color color = starving
                ? new Color(foodLow.r, foodLow.g, foodLow.b, 0.55f + 0.45f * Mathf.Sin(Time.time * PulseSpeed * 0.5f))
                : new Color(foodLow.r, foodLow.g, foodLow.b, 0.8f);
            ring.color = color;
            ring.fillAmount = 1f;
            track.color = Faint(foodLow);
            float length = size * CrossLength;
            float width = Mathf.Max(2f, CrossWidth * scale);
            crossA.rectTransform.sizeDelta = crossB.rectTransform.sizeDelta = new Vector2(length, width);
            crossA.rectTransform.anchoredPosition = crossB.rectTransform.anchoredPosition = Vector2.zero;
            crossA.color = crossB.color = color;
            if (time.text.Length > 0)
            {
                time.text = string.Empty;
            }
        }

        // Returns whether the food asks for attention: half gone or more, or ready to be eaten again.
        internal bool Show(Player.Food food, Vector2 position, float size, float scale)
        {
            Layout(position, size, scale);
            SetEmpty(false);
            float burn = food.m_item?.m_shared?.m_foodBurnTime ?? 0f;
            float share = burn > 0f ? Mathf.Clamp01(food.m_time / burn) : 1f;
            Color color = Countdown(foodFull, share);
            ring.color = color;
            ring.fillAmount = share;
            track.color = Faint(color);
            Sprite sprite = food.m_item?.GetIcon();
            if (icon.sprite != sprite)
            {
                icon.sprite = sprite;
            }

            bool again = food.CanEatAgain();
            icon.color = again ? new Color(1f, 1f, 1f, 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f) : Color.white;
            float seconds = food.m_time / Game.m_foodRate;
            time.fontSize = Mathf.RoundToInt(12f * scale);
            time.color = seconds >= 60f ? dimColor : new Color(1f, 1f, 1f, 0.4f + Mathf.Sin(Time.time * 10f) * 0.6f);
            string text = Duration(seconds);
            if (time.text != text)
            {
                time.text = text;
            }

            return again || share <= 0.5f;
        }

        private void Layout(Vector2 position, float size, float scale)
        {
            float inset = IconInset * size;
            Place(root, position, new Vector2(size, size));
            Place(back.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(track.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(ring.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(icon.rectTransform, new Vector2(inset, inset), new Vector2(size - 2f * inset, size - 2f * inset));
            Place(time.rectTransform, new Vector2(0f, -TimeHeight * scale), new Vector2(size, TimeHeight * scale));
        }

        private void SetEmpty(bool empty)
        {
            if (icon.gameObject.activeSelf == empty)
            {
                icon.gameObject.SetActive(!empty);
                crossA.gameObject.SetActive(empty);
                crossB.gameObject.SetActive(empty);
            }
        }
    }

    // One place in the tray: an icon on a dark disc, flashing when it asks for it, a ring round it that empties and
    // turns yellow and red as its time runs out, and its text under it; in the list, its name and time beside it.
    private sealed class StatusSlot
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Image ring;
        private readonly Image icon;
        private readonly Text more;
        private readonly Text time;
        private readonly Text name;

        internal StatusSlot(Transform parent)
        {
            root = Element(parent, "Status");
            back = Box(root, "Back", panelColor);
            back.sprite = HudSprites.Disc;
            ring = Ring(root, "Ring", ringColor);
            icon = Box(root, "Icon", Color.white);
            icon.preserveAspect = true;
            more = Label(root, "More", TextAnchor.UpperRight);
            time = Label(root, "Time", TextAnchor.UpperCenter);
            name = Label(root, "Name", TextAnchor.MiddleLeft);
        }

        internal void SetActive(bool active)
        {
            if (root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        internal void Show(TrayItem item, Vector2 position, float size, float scale)
        {
            Layout(position, size, scale);
            SetText(more, false, string.Empty);
            SetText(name, false, string.Empty);
            Fill(item, item.Text);
        }

        // The place for the effects left out: one of them in turn, with how many they are in its corner.
        internal void ShowMore(TrayItem item, int count, Vector2 position, float size, float scale)
        {
            Layout(position, size, scale);
            SetText(more, true, "+" + count);
            SetText(name, false, string.Empty);
            Fill(item, item.Text);
        }

        // A row of the list: the icon with its name and time beside it.
        internal void ShowListed(TrayItem item, Vector2 position, float size, float scale, float width)
        {
            Layout(position, size, scale);
            SetText(more, false, string.Empty);
            Place(name.rectTransform, new Vector2(size + StatusGap * scale, 0f), new Vector2(width, size));
            name.fontSize = Mathf.RoundToInt(14f * scale);
            string text = item.Text.Length > 0 ? $"{item.Name}  <color=#{Hex(dimColor)}>{item.Text}</color>" : item.Name;
            SetText(name, true, text);
            Fill(item, string.Empty);
        }

        private void Fill(TrayItem item, string under)
        {
            if (icon.sprite != item.Icon)
            {
                icon.sprite = item.Icon;
            }

            icon.color = item.Flash && Mathf.Sin(Time.time * 10f) > 0f ? lowColor : item.Tint;
            bool timed = item.Remaining >= 0f;
            if (ring.gameObject.activeSelf != timed)
            {
                ring.gameObject.SetActive(timed);
            }

            ring.fillAmount = timed ? item.Remaining : 0f;
            ring.color = timed ? Countdown(ringColor, item.Remaining) : ringColor;
            if (time.text != under)
            {
                time.text = under;
            }
        }

        private static void SetText(Text text, bool shown, string value)
        {
            if (text.gameObject.activeSelf != shown)
            {
                text.gameObject.SetActive(shown);
            }

            if (shown && text.text != value)
            {
                text.text = value;
            }
        }

        private void Layout(Vector2 position, float size, float scale)
        {
            float inset = IconInset * size;
            Place(root, position, new Vector2(size, size));
            Place(back.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(ring.rectTransform, Vector2.zero, new Vector2(size, size));
            Place(icon.rectTransform, new Vector2(inset, inset), new Vector2(size - 2f * inset, size - 2f * inset));
            Place(more.rectTransform, new Vector2(size * 0.3f, size * 0.55f), new Vector2(size, size * 0.6f));
            Place(time.rectTransform, new Vector2(0f, -TimeHeight * scale), new Vector2(size, TimeHeight * scale));
            more.fontSize = Mathf.RoundToInt(12f * scale);
            time.fontSize = Mathf.RoundToInt(11f * scale);
            time.color = dimColor;
        }
    }

    // The context lines: centred text on a dark panel sized to it.
    private sealed class Strip
    {
        private readonly RectTransform root;
        private readonly Image back;
        private readonly Text text;
        private string shown;

        internal Strip(Transform parent)
        {
            root = Element(parent, "Context");
            back = Box(root, "Back", panelColor);
            back.sprite = HudSprites.RoundedBox;
            back.type = Image.Type.Sliced;
            text = Label(root, "Lines", TextAnchor.MiddleCenter);
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }

        // Returns the strip's top, or null when it is hidden.
        internal float? Show(List<string> lines, Vector2 bottomCentre, float scale)
        {
            bool any = lines.Count > 0;
            if (root.gameObject.activeSelf != any)
            {
                root.gameObject.SetActive(any);
            }

            if (!any)
            {
                shown = null;
                return null;
            }

            string joined = string.Join("\n", lines);
            text.fontSize = Mathf.RoundToInt(14f * scale);
            if (joined != shown)
            {
                text.text = joined;
                shown = joined;
            }

            float height = lines.Count * StripLine * scale + 6f * scale;
            float width = text.preferredWidth + 2f * StripPadding * scale;
            Place(root, new Vector2(bottomCentre.x - width / 2f, bottomCentre.y), new Vector2(width, height));
            Place(back.rectTransform, Vector2.zero, new Vector2(width, height));

            // Rounded as the panel under it, at most half the strip's height.
            float radius = Mathf.Min(HudSettings.PanelRadius.Value * scale, height / 2f);
            back.pixelsPerUnitMultiplier = HudSprites.RoundedBoxCorner / Mathf.Max(1f, radius);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            return bottomCentre.y + height;
        }
    }
}
