using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items;
using BrudvikWhiteHilt.Items.Summoning;
using BrudvikWhiteHilt.Progression;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses.GateControl;

/// <summary>
/// The Gate Horn: blown outside the walls, it tells the gate to open or close for whoever may open it. A new horn
/// calls the nearest gate; taught at a Gate Rope or a Windlass House, it calls that gate only. Opening also raises the
/// gatehouse's portcullis, and a drawbridge nearby follows the gate.
/// </summary>
public class GateHorn : IWhiteHiltCustomItem
{
    /// <summary>Prefab name of the horn.</summary>
    public const string ItemName = "WhiteHiltGateHorn";

    private const string GateKey = "whitehilt_gatehorn_gate";

    private static readonly int GateIdKey = "whitehilt_gate_id".GetStableHashCode();


    private readonly ItemManager manager;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    public string Id => ItemName;

    /// <inheritdoc/>
    public string DisplayName => "Gate Horn";

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(Id));

    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>
    /// Registers the horn's texts.
    /// </summary>
    /// <param name="manager">The item manager.</param>
    public GateHorn(ItemManager manager)
    {
        this.manager = manager;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(Id), DisplayName,
            "A brass-bound horn. Blow it outside your walls and the gate opens for you, or closes behind you. Use it at a Gate Rope or a Windlass House to teach it that gate's call.");
        Translations.AddEnglish("whitehilt_gatehorn_open", "The gate opens");
        Translations.AddEnglish("whitehilt_gatehorn_close", "The gate closes");
        Translations.AddEnglish("whitehilt_gatehorn_none", "No gate answers the horn");
        Translations.AddEnglish("whitehilt_gatehorn_bound", "Calls one gate");
        Translations.AddEnglish("whitehilt_gatehorn_unbound", "Calls the nearest gate");
    }

    /// <summary>
    /// Clones the celebration horn as a brass horn made at the workbench.
    /// </summary>
    public void Add()
    {
        try
        {
            CustomItem item = new(Id, "TankardAnniversary", new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token(Translations.ItemKey(Id) + "_description"),
                CraftingStation = CraftingStations.Workbench,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "TrophyBoar", Amount = 1 },
                    new() { Item = "LeatherScraps", Amount = 2 },
                    new() { Item = "Copper", Amount = 1 }
                }
            });
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_ammoType = string.Empty;
            shared.m_useDurability = false;
            shared.m_attack = new Attack();
            shared.m_secondaryAttack = new Attack();
            shared.m_startEffect = new EffectList();
            shared.m_triggerEffect = new EffectList();
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    VisualHelper.Recolor(item.ItemPrefab, Brass);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = Enumerable.Repeat(icon, shared.m_icons.Length).ToArray();
                    }

                    HornCaller.LoadSound();
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"Gate Horn visual: {ex.Message}");
                }
            }

            manager.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Gate Horn failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    /// <summary>
    /// Checks whether an item is a Gate Horn.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>True for a Gate Horn.</returns>
    public static bool Is(ItemDrop.ItemData item)
    {
        return item?.m_dropPrefab != null && item.m_dropPrefab.name == ItemName;
    }

    /// <summary>
    /// Teaches a horn the call of the nearest gate within range of a gate control, if the item is a Gate Horn.
    /// </summary>
    /// <param name="user">The player using the horn on the control.</param>
    /// <param name="item">The item used.</param>
    /// <param name="position">The control's position.</param>
    /// <param name="range">The control's reach.</param>
    /// <returns>True if the item was a Gate Horn and so was handled.</returns>
    public static bool TeachAt(Humanoid user, ItemDrop.ItemData item, Vector3 position, float range)
    {
        if (!Is(item))
        {
            return false;
        }

        GateMechanism gate = GateMechanisms.Nearest(position, range, GateMechanismKind.Gate);
        if (gate == null)
        {
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_gate_no_gate"));
            return true;
        }

        if (GateMechanisms.MayUse(gate))
        {
            item.m_customData[GateKey] = GateId(gate, true);
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_gate_taught"));
        }

        return true;
    }

    /// <summary>
    /// Answers a finished call of the local player's Gate Horn: the gate it calls opens, raising its portcullis, or
    /// closes if it was open. Only a gate the player may open answers.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Sounded(Player player)
    {
        ItemDrop.ItemData horn = player.GetRightItem();
        if (!Is(horn))
        {
            return;
        }

        horn.m_customData.TryGetValue(GateKey, out string bound);
        GateMechanism gate = GateMechanisms.Around(player.transform.position, GateMechanisms.HornRange.Value)
            .Where(mechanism => mechanism.Kind == GateMechanismKind.Gate)
            .FirstOrDefault(mechanism => string.IsNullOrEmpty(bound) || GateId(mechanism, false) == bound);
        if (gate == null || !GateMechanisms.MayUse(gate))
        {
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_gatehorn_none"));
            return;
        }

        bool open = !gate.IsOpen;
        if (open)
        {
            foreach (GateMechanism portcullis in GateMechanism.Of(gate.Piece).Where(mechanism => mechanism.Kind == GateMechanismKind.Portcullis))
            {
                portcullis.Set(true);
            }
        }

        gate.Set(open);
        player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(open ? "$whitehilt_gatehorn_open" : "$whitehilt_gatehorn_close"));
    }

    // The celebration horn's coloured trim turned to brass; the horn itself keeps its colour.
    private static Color32 Brass(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
        if (saturation > 0.5f && value > 0.2f)
        {
            return new Color32((byte)(110f + value * 120f), (byte)(80f + value * 80f), (byte)(30f + value * 40f), pixel.a);
        }

        return pixel;
    }

    // The name a horn knows a gate by: kept on the gate itself, so it lasts across sessions, made when first taught.
    private static string GateId(GateMechanism gate, bool create)
    {
        ZDO zdo = gate.View.GetZDO();
        string id = zdo.GetString(GateIdKey);
        if (string.IsNullOrEmpty(id) && create)
        {
            gate.View.ClaimOwnership();
            id = Guid.NewGuid().ToString("N");
            zdo.Set(GateIdKey, id);
        }

        return id;
    }

    /// <summary>
    /// Says on the horn's tooltip whether it calls one gate or the nearest.
    /// </summary>
    [HarmonyPatch]
    public static class TooltipPatch
    {
        /// <summary>
        /// Adds the line.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="__result">The tooltip.</param>
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
            new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
        [HarmonyPostfix]
        public static void AddGateLine(ItemDrop.ItemData item, ref string __result)
        {
            if (Is(item))
            {
                __result += "\n" + (item.m_customData.ContainsKey(GateKey) ? "$whitehilt_gatehorn_bound" : "$whitehilt_gatehorn_unbound");
            }
        }
    }
}
