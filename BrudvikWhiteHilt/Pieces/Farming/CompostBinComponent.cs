using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Farming;

/// <summary>
/// Config for the Compost Bin, section "Farming.Compost". Server-synced.
/// </summary>
public static class CompostSettings
{
    private const string Section = "Farming.Compost";

    private static string itemsText;
    private static HashSet<string> items = new();
    private static bool bound;

    /// <summary>Prefab names that turn into compost, comma separated.</summary>
    public static ConfigEntry<string> Items { get; private set; }

    /// <summary>Minutes for the waste to turn into one compost.</summary>
    public static ConfigEntry<float> Minutes { get; private set; }

    /// <summary>Pieces of waste that make one compost.</summary>
    public static ConfigEntry<int> WastePerCompost { get; private set; }

    /// <summary>Compost a bin uses up each day.</summary>
    public static ConfigEntry<int> CompostPerDay { get; private set; }

    /// <summary>How far, in metres, a bin with compost speeds up crops.</summary>
    public static ConfigEntry<float> Range { get; private set; }

    /// <summary>Share of the vanilla growing time near a bin with compost.</summary>
    public static ConfigEntry<float> GrowTime { get; private set; }

    /// <summary>
    /// Binds the config entries. Runs in the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        if (bound)
        {
            return;
        }

        bound = true;
        Items = WhiteHiltConfig.BindAdminOnly(Section, "Items",
            "Entrails,BoneFragments,Raspberry,Blueberries,Cloudberry,Mushroom,MushroomYellow,Dandelion,Thistle,Carrot,Turnip,Onion,Guck,Fiddleheadfern",
            "Prefab names of the waste that turns into compost, comma separated.");
        Minutes = WhiteHiltConfig.BindAdminOnly(Section, "Minutes", 2f,
            "Minutes for the waste in a Compost Bin to turn into one compost.", new AcceptableValueRange<float>(0.1f, 60f));
        WastePerCompost = WhiteHiltConfig.BindAdminOnly(Section, "WastePerCompost", 5,
            "Pieces of waste that make one compost.", new AcceptableValueRange<int>(1, 50));
        CompostPerDay = WhiteHiltConfig.BindAdminOnly(Section, "CompostPerDay", 1,
            "Compost a Compost Bin uses up each day while it feeds the soil. 0: compost is never used up.", new AcceptableValueRange<int>(0, 10));
        Range = WhiteHiltConfig.BindAdminOnly(Section, "Range", 12f,
            "How far, in metres, a Compost Bin with compost in it speeds up crops.", new AcceptableValueRange<float>(2f, 40f));
        GrowTime = WhiteHiltConfig.BindAdminOnly(Section, "GrowTime", 0.7f,
            "Share of the growing time for crops near a Compost Bin with compost: 0.7 grows 30% faster.", new AcceptableValueRange<float>(0.1f, 1f));
    }

    /// <summary>
    /// True if the item turns into compost.
    /// </summary>
    /// <param name="prefabName">Prefab name of the item.</param>
    /// <returns>True for waste.</returns>
    public static bool IsWaste(string prefabName)
    {
        string text = Items.Value ?? string.Empty;
        if (text != itemsText)
        {
            itemsText = text;
            items = new HashSet<string>(text.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0), StringComparer.OrdinalIgnoreCase);
        }

        return items.Contains(prefabName);
    }
}

/// <summary>
/// On a Compost Bin: turns waste into compost on the machine that owns it, and tells plants whether they grow faster.
/// </summary>
public class CompostBinComponent : MonoBehaviour
{
    private const float TickSeconds = 5f;
    private const string NextKey = "whitehilt_compost_next";
    private const string DayKey = "whitehilt_compost_day";

    private static readonly HashSet<CompostBinComponent> bins = new();

    private Container container;
    private ZNetView nview;

    private static int WastePerCompost => CompostSettings.WastePerCompost.Value;

    /// <summary>
    /// Share of the vanilla growing time at a position: lower near a bin with compost.
    /// </summary>
    /// <param name="position">The plant.</param>
    /// <returns>The multiplier.</returns>
    public static float GrowTimeFactor(Vector3 position)
    {
        if (bins.Count == 0)
        {
            return 1f;
        }

        float range = CompostSettings.Range.Value;
        foreach (CompostBinComponent bin in bins)
        {
            if (bin != null && Vector3.Distance(bin.transform.position, position) <= range && bin.HasCompost())
            {
                return CompostSettings.GrowTime.Value;
            }
        }

        return 1f;
    }

    /// <summary>
    /// What the bin is doing, for its hover text.
    /// </summary>
    /// <returns>A localized line.</returns>
    public string StatusText()
    {
        string state = HasCompost()
            ? string.Format(Localization.instance.Localize("$whitehilt_compost_fertile"), CompostSettings.Range.Value.ToString("0"))
            : string.Format(Localization.instance.Localize("$whitehilt_compost_waiting"), WastePerCompost);
        return $"\n<color=#a0a0a0>{state}</color>";
    }

    private void Awake()
    {
        container = GetComponent<Container>();
        nview = GetComponent<ZNetView>();
        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        bins.Add(this);
        InvokeRepeating(nameof(Tick), TickSeconds, TickSeconds);
    }

    private void OnDestroy()
    {
        bins.Remove(this);
    }

    private bool HasCompost()
    {
        Inventory inventory = container != null ? container.GetInventory() : null;
        return inventory != null && inventory.GetAllItems().Any(item => item.m_dropPrefab != null && item.m_dropPrefab.name == CompostBin.CompostName);
    }

    private void Tick()
    {
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || container == null || container.IsInUse() || ZNet.instance == null)
        {
            return;
        }

        Inventory inventory = container.GetInventory();
        ZDO zdo = nview.GetZDO();
        long now = ZNet.instance.GetTime().Ticks;
        long next = zdo.GetLong(NextKey);
        List<ItemDrop.ItemData> waste = inventory.GetAllItems().Where(item => item.m_dropPrefab != null && CompostSettings.IsWaste(item.m_dropPrefab.name)).ToList();
        if (waste.Sum(item => item.m_stack) < WastePerCompost)
        {
            zdo.Set(NextKey, 0L);
        }
        else if (next == 0L)
        {
            zdo.Set(NextKey, now + TimeSpan.FromMinutes(CompostSettings.Minutes.Value).Ticks);
        }
        else if (now >= next && MakeCompost(inventory, waste))
        {
            zdo.Set(NextKey, 0L);
        }

        UseCompostDaily(inventory, zdo);
    }

    private static bool MakeCompost(Inventory inventory, List<ItemDrop.ItemData> waste)
    {
        GameObject compost = ZNetScene.instance?.GetPrefab(CompostBin.CompostName);
        if (compost == null || !inventory.CanAddItem(compost, 1))
        {
            return false;
        }

        int left = WastePerCompost;
        foreach (ItemDrop.ItemData item in waste)
        {
            int take = Mathf.Min(item.m_stack, left);
            inventory.RemoveItem(item, take);
            left -= take;
            if (left == 0)
            {
                break;
            }
        }

        return inventory.AddItem(compost, 1);
    }

    private static void UseCompostDaily(Inventory inventory, ZDO zdo)
    {
        if (EnvMan.instance == null)
        {
            return;
        }

        int day = EnvMan.instance.GetDay();
        int last = zdo.GetInt(DayKey);
        if (last == 0)
        {
            zdo.Set(DayKey, day);
            return;
        }

        ItemDrop.ItemData compost = inventory.GetAllItems().FirstOrDefault(item => item.m_dropPrefab != null && item.m_dropPrefab.name == CompostBin.CompostName);
        if (day > last && compost != null)
        {
            for (int used = 0; used < CompostSettings.CompostPerDay.Value && compost != null; used++)
            {
                inventory.RemoveItem(compost, 1);
                compost = inventory.GetAllItems().FirstOrDefault(item => item.m_dropPrefab != null && item.m_dropPrefab.name == CompostBin.CompostName);
            }

            zdo.Set(DayKey, day);
        }
        else if (day > last)
        {
            zdo.Set(DayKey, day);
        }
    }
}
