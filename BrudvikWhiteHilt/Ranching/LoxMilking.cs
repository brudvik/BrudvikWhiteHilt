using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Foraging;
using BrudvikWhiteHilt.Progression;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Ranching;

/// <summary>
/// Milking tame lox: Use while crouching on a tame lox gives Lox Milk once a day, one more from a content (groomed) lox. The
/// time of the last milking lives in the lox's ZDO, set by its owner through an RPC, so every machine agrees.
/// </summary>
public static class LoxMilking
{
    /// <summary>Prefab name of the milk item.</summary>
    public const string MilkName = "WhiteHiltLoxMilk";

    private const string LoxName = "Lox";
    private const string MilkedKey = "whitehilt_lox_milked";
    private const string MilkedRpc = "WhiteHilt_LoxMilked";
    private const string Section = "Husbandry";
    private const double DefaultDaySeconds = 1800d;

    private static ConfigEntry<bool> enabled;
    private static ConfigEntry<int> milkPerLox;
    private static ConfigEntry<float> milkDays;

    private static double DaySeconds => EnvMan.instance != null && EnvMan.instance.m_dayLengthSec > 0 ? EnvMan.instance.m_dayLengthSec : DefaultDaySeconds;

    /// <summary>
    /// Binds the settings, registers the English text and adds the milk item once the vanilla prefabs exist. Call from the
    /// plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        enabled = WhiteHiltConfig.BindAdminOnly(Section, "LoxMilking", true, "Let players milk tame lox by crouching and using them.");
        milkPerLox = WhiteHiltConfig.BindAdminOnly(Section, "MilkPerLox", 2, "Lox Milk from one milking; a content (groomed) lox gives one more.",
            new AcceptableValueRange<int>(1, 10));
        milkDays = WhiteHiltConfig.BindAdminOnly(Section, "MilkDays", 1f, "In-game days before a lox can be milked again.",
            new AcceptableValueRange<float>(0.1f, 10f));

        Translations.AddEnglishNameAndDescription(Translations.ItemKey(MilkName), "Lox Milk",
            "Thick, rich milk from a tame lox, still warm in the pail. Churned, soured or set, it becomes butter, skyr and cheese.");
        Translations.AddEnglish("whitehilt_lox_milk", "Milk");
        Translations.AddEnglish("whitehilt_lox_milked", "Milked today");
        Translations.AddEnglish("msg_whitehilt_lox_milked", "You fill a pail with lox milk");
        Translations.AddEnglish("msg_whitehilt_lox_not_ready", "This lox was milked today");
        PrefabManager.OnVanillaPrefabsAvailable += AddItem;
    }

    /// <summary>
    /// Whether a creature is a tame lox that can be milked by this mod.
    /// </summary>
    /// <param name="tameable">The animal.</param>
    /// <returns>True for a tame lox.</returns>
    public static bool IsMilkable(Tameable tameable)
    {
        return enabled.Value && tameable != null && tameable.IsTamed() && global::Utils.GetPrefabName(tameable.gameObject) == LoxName;
    }

    /// <summary>
    /// Registers the RPC that records a milking. Call from Tameable.Awake.
    /// </summary>
    /// <param name="tameable">The animal.</param>
    public static void RegisterRpc(Tameable tameable)
    {
        ZNetView nview = tameable.m_nview;
        if (nview == null || nview.GetZDO() == null || global::Utils.GetPrefabName(tameable.gameObject) != LoxName)
        {
            return;
        }

        nview.Register<long>(MilkedRpc, (_, ticks) =>
        {
            if (nview.IsValid() && nview.IsOwner())
            {
                nview.GetZDO().Set(MilkedKey, ticks);
            }
        });
    }

    /// <summary>
    /// Milks a tame lox if it is ready, filling the player's inventory.
    /// </summary>
    /// <param name="tameable">The lox.</param>
    /// <param name="user">The player.</param>
    /// <returns>True when the use was about milking, whether or not milk was given.</returns>
    public static bool TryMilk(Tameable tameable, Humanoid user)
    {
        if (!IsMilkable(tameable) || tameable.m_nview == null || !tameable.m_nview.IsValid() || user is not Player player)
        {
            return false;
        }

        if (!IsReady(tameable))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_lox_not_ready");
            return true;
        }

        GameObject milk = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(MilkName) : null;
        if (milk == null)
        {
            return false;
        }

        int amount = milkPerLox.Value + (AnimalCare.IsContent(tameable) ? 1 : 0);
        if (!player.GetInventory().AddItem(milk, amount))
        {
            player.Message(MessageHud.MessageType.Center, "$inventory_full");
            return true;
        }

        tameable.m_nview.InvokeRPC(MilkedRpc, ZNet.instance.GetTime().Ticks);
        tameable.m_sootheEffect.Create(tameable.transform.position, Quaternion.identity);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_lox_milked");
        player.RaiseSkill(HusbandrySkill.Type, AnimalCare.GroomExperience);
        return true;
    }

    /// <summary>
    /// Hover text line for a tame lox.
    /// </summary>
    /// <param name="tameable">The lox.</param>
    /// <returns>The line, or an empty string.</returns>
    public static string HoverText(Tameable tameable)
    {
        if (!IsMilkable(tameable) || tameable.m_nview == null || !tameable.m_nview.IsValid())
        {
            return string.Empty;
        }

        return IsReady(tameable)
            ? "\n[<color=yellow><b>$KEY_Crouch + $KEY_Use</b></color>] $whitehilt_lox_milk"
            : "\n$whitehilt_lox_milked";
    }

    private static bool IsReady(Tameable tameable)
    {
        long last = tameable.m_nview.GetZDO().GetLong(MilkedKey);
        if (last == 0L || ZNet.instance == null)
        {
            return true;
        }

        double seconds = (ZNet.instance.GetTime() - new DateTime(last)).TotalSeconds;
        return seconds >= milkDays.Value * DaySeconds;
    }

    private static void AddItem()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddItem;
        try
        {
            CustomItem milk = new(MilkName, "Tar");
            ItemDrop.ItemData.SharedData shared = milk.ItemDrop.m_itemData.m_shared;
            shared.m_name = Translations.Token(Translations.ItemKey(MilkName));
            shared.m_description = Translations.Token($"{Translations.ItemKey(MilkName)}_description");
            shared.m_weight = 1f;
            shared.m_maxStackSize = 20;
            if (!VisualHelper.IsHeadless)
            {
                Texture2D texture = ForagingAssets.LoadTexture("milkpail_albedo");
                GameObject model = VisualHelper.ReplaceMesh(milk.ItemPrefab, ForagingAssets.LoadMesh("milkpail"), texture);
                model.GetComponent<MeshRenderer>().sharedMaterial = ForageableBase.PlantMaterial(texture);
                Sprite icon = VisualHelper.RenderIcon(milk.ItemPrefab);
                if (icon != null)
                {
                    shared.m_icons = new[] { icon };
                }
            }

            ItemManager.Instance.AddItem(milk);
            Jotunn.Logger.LogInfo("Lox Milk added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Lox Milk failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }
}
