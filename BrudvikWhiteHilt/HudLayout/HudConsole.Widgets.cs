using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Crafting;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.HudLayout;

public static partial class HudConsole
{
    // Under the HUD root, filling it, so the console goes away with the HUD (Ctrl + F3, cutscenes, media mode) and
    // its positions are measured from the screen's lower left corner.
    private static void Ensure(Hud hud)
    {
        if (root != null && root.parent == hud.m_rootObject.transform)
        {
            return;
        }

        if (root != null)
        {
            Object.Destroy(root.gameObject);
        }

        foodSlots.Clear();
        statusSlots.Clear();
        hotbar = null;
        hotbarGroup = null;
        powerGroup = null;
        root = Layer(hud.m_rootObject.transform, "WhiteHiltConsole");
        root.SetAsFirstSibling();
        panel = Box(root, "Panel", Color.clear);
        panel.sprite = HudSprites.RoundedBox;
        panel.type = Image.Type.Sliced;
        foodLayer = Layer(root, "Food");
        statusLayer = Layer(root, "Status");
        foodFader = new Fader(foodLayer.gameObject.AddComponent<CanvasGroup>());
        statusFader = new Fader(statusLayer.gameObject.AddComponent<CanvasGroup>());
        handsFader = new Fader(null);
        hintFader = new Fader(null);
        health = new Bar(root, "Health", healthColor, healthBack);
        stamina = new Bar(root, "Stamina", staminaColor, staminaBack);
        eitr = new Bar(root, "Eitr", eitrColor, eitrBack);
        adrenaline = new Bar(root, "Adrenaline", adrenalineColor, adrenalineBack);
        strip = new Strip(root);
    }

    // A layer filling its parent, measured from the lower left corner.
    private static RectTransform Layer(Transform parent, string name)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static RectTransform Element(Transform parent, string name)
    {
        RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        return rect;
    }

    private static Image Box(Transform parent, string name, Color color)
    {
        Image image = Element(parent, name).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(Transform parent, string name, TextAnchor alignment)
    {
        GameObject go = GUIManager.Instance.CreateText(string.Empty, parent, Vector2.zero, Vector2.zero, Vector2.zero,
            GUIManager.Instance.AveriaSerifBold, 14, textColor, true, Color.black, 10f, 10f, false);
        go.name = name;
        Text text = go.GetComponent<Text>();
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = true;
        text.rectTransform.pivot = Vector2.zero;
        return text;
    }

    // A countdown ring: the share left, from the top round clockwise.
    private static Image Ring(Transform parent, string name, Color color)
    {
        Image ring = Box(parent, name, color);
        ring.sprite = HudSprites.Ring;
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;
        return ring;
    }

    // A countdown's colour: its own while more than half is left, yellow under half, red under a fifth, the red
    // pulsing.
    private static Color Countdown(Color full, float share)
    {
        if (share > RingHalf)
        {
            return full;
        }

        if (share > RingLow)
        {
            return foodHalf;
        }

        return new Color(foodLow.r, foodLow.g, foodLow.b, 0.55f + 0.45f * Mathf.Sin(Time.time * PulseSpeed));
    }

    private static Color Faint(Color color)
    {
        return new Color(color.r, color.g, color.b, 0.25f);
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static string Duration(float seconds)
    {
        return seconds >= 60f ? Mathf.CeilToInt(seconds / 60f) + "m" : Mathf.FloorToInt(seconds) + "s";
    }

    private static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }
}
