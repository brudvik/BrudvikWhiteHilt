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
    // Keeps elements other mods put on the HUD clear of the console: one that overlaps it is lifted into a band over
    // it, stacked with the others there, and kept there.
    private static void AvoidForeign(Hud hud, Rect console)
    {
        foreign.RemoveWhere(rect => rect == null);
        float zone = console.yMax + ForeignGap;
        Vector2 screen = root.rect.size;
        foreach (Transform child in hud.m_rootObject.transform)
        {
            if (child is not RectTransform rect || native.Contains(child) || child == root || !child.gameObject.activeInHierarchy
                || child.name.StartsWith("WhiteHilt") || child.name.StartsWith("Brudvik"))
            {
                continue;
            }

            single.Clear();
            single.Add(rect);
            if (!VanillaPlacement.Bounds(root, single, out Rect box) || box.width < 1f || box.height < 1f
                || box.width > screen.x / 2f || box.height > screen.y / 2f)
            {
                continue;
            }

            if (!foreign.Contains(rect) && !box.Overlaps(console))
            {
                continue;
            }

            foreign.Add(rect);
            VanillaPlacement.Move(rect, root, new Vector2(0f, zone - box.yMin));
            zone += box.height + ForeignGap;
        }
    }

    private static HotkeyBar Hotbar(Hud hud)
    {
        if (hotbar == null)
        {
            hotbar = hud.GetComponentInChildren<HotkeyBar>(true);
        }

        return hotbar;
    }

    // Centres the hotbar at the bottom, always in the same place. Returns the box of its slots; with no slots yet, the
    // box eight slots would take.
    private static Rect PlaceHotbar(Hud hud)
    {
        float width = root.rect.width;
        float bottom = HudSettings.BottomMargin.Value;
        HotkeyBar bar = Hotbar(hud);
        hotbarSlots.Clear();
        if (bar != null && bar.m_elements != null)
        {
            foreach (HotkeyBar.ElementData element in bar.m_elements)
            {
                if (element?.m_go != null && element.m_go.transform is RectTransform rect)
                {
                    hotbarSlots.Add(rect);
                }
            }
        }

        bool measured = VanillaPlacement.Bounds(root, hotbarSlots, out Rect slots);
        if (!measured)
        {
            float space = bar != null ? bar.m_elementSpace : 70f;
            slots = new Rect(0f, 0f, space * FallbackSlots, FallbackSlotHeight);
        }

        Vector2 delta = new(width / 2f - slots.center.x, bottom - slots.yMin);
        if (measured && bar != null)
        {
            VanillaPlacement.Move((RectTransform)bar.transform, root, delta);
        }

        slots.position += delta;
        return slots;
    }

    // The food the player has eaten, left of the hotbar: icon, a frame coloured by how much is left and the time under
    // it, flashing when it can be eaten again. Returns the left edge of the row.
    // Every place for food stays, so the console keeps its shape: an empty one is a red ring with a red cross, and
    // with nothing eaten at all the red rings pulse.
    private static float LayoutFood(Player player, Rect bar, float scale)
    {
        List<Player.Food> foods = player.GetFoods();
        int places = Mathf.Max(FoodPlaces, foods.Count);
        while (foodSlots.Count < places)
        {
            foodSlots.Add(new FoodSlot(foodLayer));
        }

        float size = FoodSize * scale;
        float gap = FoodGap * scale;
        float right = bar.xMin - WingGap * scale;
        float left = right - places * size - (places - 1) * gap;
        float y = bar.yMin + TimeHeight * scale;
        bool hungry = foods.Count != lastFoods || foods.Count < places;
        lastFoods = foods.Count;
        for (int i = 0; i < foodSlots.Count; i++)
        {
            bool used = i < places;
            foodSlots[i].SetActive(used);
            if (!used)
            {
                continue;
            }

            Vector2 position = new(left + i * (size + gap), y);
            if (i < foods.Count)
            {
                hungry |= foodSlots[i].Show(foods[i], position, size, scale);
            }
            else
            {
                foodSlots[i].ShowEmpty(position, size, scale, foods.Count == 0);
            }
        }

        foodFader.Wake(hungry);
        return left;
    }

    // The game's guardian power, moved left of the food with its bottom on the hotbar's. Returns the console's left
    // edge with it.
    private static float PlaceGuardianPower(Hud hud, float left, Rect bar)
    {
        RectTransform power = hud.m_gpRoot;
        single.Clear();
        single.Add(power);
        if (power == null || !VanillaPlacement.Bounds(root, single, out Rect box))
        {
            return left;
        }

        float gap = WingGap * Mathf.Clamp(HudSettings.Scale.Value, 0.6f, 1.6f);
        float shift = left - gap - box.xMax;
        VanillaPlacement.Move(power, root, new Vector2(shift, bar.yMin - box.yMin));
        return box.xMin + shift;
    }

    // Health, stamina, eitr (for those who have it) and adrenaline (while there is some) side by side over the hotbar,
    // as wide as it, health the widest, each with its number above it. Returns the top of the row.
    private static float LayoutVitals(Player player, Rect bar, float scale)
    {
        float maxEitr = player.GetMaxEitr();
        float maxAdrenaline = player.GetMaxAdrenaline();
        if (maxAdrenaline > 0f)
        {
            lastMaxAdrenaline = maxAdrenaline;
        }

        float current = player.GetAdrenaline();
        bool hasEitr = maxEitr > 0f;
        bool hasAdrenaline = current > 0f && lastMaxAdrenaline > 0f;
        float weights = 5f + 3f + (hasEitr ? 2f : 0f) + (hasAdrenaline ? 2f : 0f);
        int count = 2 + (hasEitr ? 1 : 0) + (hasAdrenaline ? 1 : 0);
        float gap = BarGap * scale;
        float unit = (bar.width - gap * (count - 1)) / weights;
        float y = bar.yMax + SupplyRoom;
        float x = bar.xMin;

        float maxHealth = player.GetMaxHealth();
        float hp = player.GetHealth();
        health.Show(hp, maxHealth, new Vector2(x, y), unit * 5f, scale, hp < maxHealth * 0.25f);
        x += unit * 5f + gap;
        stamina.Show(player.GetStamina(), player.GetMaxStamina(), new Vector2(x, y), unit * 3f, scale, false);
        x += unit * 3f + gap;
        eitr.SetActive(hasEitr);
        if (hasEitr)
        {
            eitr.Show(player.GetEitr(), maxEitr, new Vector2(x, y), unit * 2f, scale, false);
            x += unit * 2f + gap;
        }

        adrenaline.SetActive(hasAdrenaline);
        if (hasAdrenaline)
        {
            adrenaline.Show(current, lastMaxAdrenaline, new Vector2(x, y), unit * 2f, scale, false);
        }

        return y + (BarHeight + NumberHeight) * scale + 1f;
    }

    // The status effects right of the hotbar, rows growing upwards, each with its time under it; the game's, White
    // Hilt's and other mods' alike, then the indicators other mods registered. When they do not all fit, the last place
    // goes through the ones left out, with how many they are. In the inventory and while the show-all key is held,
    // they are a list instead, each with its name and time. Returns the tray's upper right corner.
    private static Vector2 LayoutStatus(Player player, Rect bar, float scale)
    {
        bool listed = InventoryGui.IsVisible() || showAllHeld;
        effects.Clear();
        player.GetSEMan().GetHUDStatusEffects(effects);
        WhiteHiltHud.ReadIndicators(indicators);
        tray.Clear();
        bool flashing = false;
        foreach (StatusEffect effect in effects)
        {
            if (effect != null && !effect.m_hidden)
            {
                float remaining = effect.m_ttl > 0f ? Mathf.Clamp01(1f - effect.m_time / effect.m_ttl) : -1f;
                string name = listed ? Localization.instance.Localize(effect.m_name) : null;
                tray.Add(new TrayItem(effect.m_icon, effect.GetIconText(), Color.white, effect.m_flashIcon, remaining, name));
                flashing |= effect.m_flashIcon;
            }
        }

        foreach (HudIndicator indicator in indicators)
        {
            tray.Add(new TrayItem(indicator.Icon, indicator.Text, indicator.Tint, indicator.Flash, -1f, indicator.Name));
            flashing |= indicator.Flash;
        }

        statusFader.Wake(flashing || tray.Count != lastTray);
        lastTray = tray.Count;
        if (listed)
        {
            return ListStatus(bar, scale);
        }

        int max = Mathf.Max(2, HudSettings.MaxStatusEffects.Value);
        int columns = Mathf.Max(1, HudSettings.StatusColumns.Value);
        bool overflow = tray.Count > max;
        int shown = overflow ? max : tray.Count;
        while (statusSlots.Count < shown)
        {
            statusSlots.Add(new StatusSlot(statusLayer));
        }

        float size = StatusSize * scale;
        float step = size + StatusGap * scale;
        float rowStep = size + TimeHeight * scale + StatusGap * scale;
        float left = bar.xMax + WingGap * scale;
        float bottom = bar.yMin + TimeHeight * scale;
        for (int i = 0; i < statusSlots.Count; i++)
        {
            bool used = i < shown;
            statusSlots[i].SetActive(used);
            if (!used)
            {
                continue;
            }

            Vector2 position = new(left + i % columns * step, bottom + i / columns * rowStep);
            if (overflow && i == shown - 1)
            {
                int hidden = tray.Count - shown + 1;
                int turn = Mathf.FloorToInt(Time.unscaledTime / CycleSeconds) % hidden;
                statusSlots[i].ShowMore(tray[shown - 1 + turn], hidden, position, size, scale);
            }
            else
            {
                statusSlots[i].Show(tray[i], position, size, scale);
            }
        }

        int rows = shown == 0 ? 0 : (shown - 1) / columns + 1;
        return new Vector2(left + Mathf.Min(shown, columns) * step, bottom + rows * rowStep);
    }

    // Every status effect and indicator in a column right of the hotbar, each with its name and time beside it.
    // Returns the list's upper right corner.
    private static Vector2 ListStatus(Rect bar, float scale)
    {
        int count = Mathf.Min(tray.Count, MaxListed);
        while (statusSlots.Count < count)
        {
            statusSlots.Add(new StatusSlot(statusLayer));
        }

        float size = StatusSize * scale;
        float step = size + StatusGap * scale;
        float left = bar.xMax + WingGap * scale;
        float names = NameWidth * scale;
        for (int i = 0; i < statusSlots.Count; i++)
        {
            bool used = i < count;
            statusSlots[i].SetActive(used);
            if (used)
            {
                statusSlots[i].ShowListed(tray[i], new Vector2(left, bar.yMin + i * step), size, scale, names);
            }
        }

        return new Vector2(left + size + StatusGap * scale + names, bar.yMin + count * step);
    }

    // The half dark rounded panel behind the console, from the guardian power to the status effects and from under the
    // hotbar to over the bars, so they read against snow and sky; none with an opacity of 0. Returns the panel's top,
    // or the top of what it would go round without it.
    private static float PlacePanel(Rect around, float scale)
    {
        bool shown = HudSettings.PanelOpacity.Value > 0f;
        if (panel.gameObject.activeSelf != shown)
        {
            panel.gameObject.SetActive(shown);
        }

        if (!shown)
        {
            return around.yMax;
        }

        float pad = PanelPadding * scale;
        Place(panel.rectTransform, new Vector2(around.xMin - pad, around.yMin - pad),
            new Vector2(around.width + 2f * pad, around.height + 2f * pad));
        panel.pixelsPerUnitMultiplier = HudSprites.RoundedBoxCorner / Mathf.Max(1f, HudSettings.PanelRadius.Value * scale);
        return around.yMax + pad;
    }

    // The context lines in a strip over the panel, clear of it, centred on the hotbar; hidden when no line has anything
    // to say. Returns the top of the strip, or the given top without it.
    private static float LayoutStrip(Rect bar, float top, float scale)
    {
        WhiteHiltHud.ReadContexts(lines);
        if (lines.Count > StripLines)
        {
            lines.RemoveRange(StripLines, lines.Count - StripLines);
        }

        return strip.Show(lines, new Vector2(bar.center.x, top + StripGap * scale), scale) ?? top;
    }

    // The key hints beside the console: the game's (for fighting, building, the inventory, fishing) at its right and the
    // build tools' at its left, each made smaller to fit, or over the console when there is too little room. As set,
    // they show for a few seconds after taking out a tool or weapon, always, or only on demand; always in the
    // inventory and while the show-all key is held.
    private static void PlaceHints(Player player, Rect console)
    {
        HudHints mode = HudSettings.KeyHints.Value;
        bool inventory = InventoryGui.IsVisible();
        string buildText = BuildHint != null && BuildHint.gameObject.activeInHierarchy ? BuildHint.text : string.Empty;
        int key = (player.InPlaceMode() ? 1 : 0) + (inventory ? 2 : 0) + 4 * (player.GetCurrentWeapon()?.GetHashCode() ?? 0)
            + 31 * buildText.GetHashCode();
        hintFader.Wake(key != lastHintKey || inventory || showAllHeld);
        lastHintKey = key;
        float linger = mode == HudHints.Hidden ? HiddenLinger : Mathf.Max(1f, HudSettings.KeyHintSeconds.Value);
        hintFader.Apply(mode != HudHints.Always, Time.unscaledTime, mode == HudHints.Always ? float.MaxValue : linger, 0f);

        KeyHints hints = KeyHints.instance;
        hintGroups.Clear();
        if (hints != null)
        {
            hintGroups.Add(hints.m_buildHints);
            hintGroups.Add(hints.m_combatHints);
            hintGroups.Add(hints.m_inventoryHints);
            hintGroups.Add(hints.m_inventoryWithContainerHints);
            hintGroups.Add(hints.m_fishingHints);
            hintGroups.Add(hints.m_barberHints);
        }

        foreach (GameObject group in hintGroups)
        {
            if (group == null || !group.activeInHierarchy || group.transform is not RectTransform rect)
            {
                continue;
            }

            // Measured by what is drawn in it, as a hint group may be a row as wide as the screen.
            drawn.Clear();
            group.GetComponentsInChildren(false, graphics);
            foreach (Graphic graphic in graphics)
            {
                if (graphic.enabled && graphic.color.a > 0f)
                {
                    drawn.Add(graphic.rectTransform);
                }
            }

            if (VanillaPlacement.Bounds(root, drawn, out Rect box) && box.width > 1f && box.height > 1f)
            {
                Fit(rect, box, console, true);
            }

            HintAlpha(group).alpha = hintFader.Alpha;
        }

        if (buildText.Length > 0)
        {
            RectTransform rect = BuildHint.rectTransform;
            float ratio = rect.lossyScale.x / Mathf.Max(0.0001f, root.lossyScale.x);
            Vector2 corner = (Vector2)root.InverseTransformPoint(rect.position) - root.rect.min;
            Fit(rect, new Rect(corner, new Vector2(BuildHint.preferredWidth, BuildHint.preferredHeight) * ratio), console, false);
            HintAlpha(BuildHint.gameObject).alpha = hintFader.Alpha;
        }
    }

    // Fits a box of hints beside the console, at its right or left, its bottom on the console's, made smaller to fit
    // the room there; with too little room, over the console instead. Scales first and moves on the next frame, as the
    // box is measured at the scale it has.
    private static void Fit(RectTransform mover, Rect box, Rect console, bool right)
    {
        float share = VanillaPlacement.ScaleOf(mover);
        float width = box.width / share;
        float screen = root.rect.width;
        float from = right ? console.xMax + HintGap : HintMargin;
        float to = right ? screen - HintMargin : console.xMin - HintGap;
        float target = Mathf.Min(1f, (to - from) / width);
        Vector2 corner;
        if (target >= MinHintScale)
        {
            corner = new Vector2(right ? from : to - width * target, console.yMin);
        }
        else
        {
            target = Mathf.Min(1f, (screen / 2f - HintMargin) / width);
            corner = new Vector2(right ? console.xMax - width * target : console.xMin, console.yMax + HintGap);
        }

        if (Mathf.Abs(target - share) > 0.01f)
        {
            VanillaPlacement.Scale(mover, target);
            return;
        }

        VanillaPlacement.Move(mover, root, corner - box.min);
    }
}
