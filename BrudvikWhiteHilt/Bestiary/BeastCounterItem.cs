using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items;
using BrudvikWhiteHilt.Items.Foraging;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Bestiary;

/// <summary>Common registration for the material arrows and weapon treatments.</summary>
public abstract class BeastCounterItem : IWhiteHiltCustomItem, IWhiteHiltConfigurable
{
    private readonly ItemManager manager;
    private readonly BeastCounter counter;
    private ItemDrop.ItemData.SharedData shared;
    private BeastCounterCoating effect;

    /// <inheritdoc/>
    public bool Enabled => true;
    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.Start;
    /// <inheritdoc/>
    public string Id => counter.PrefabName;
    /// <inheritdoc/>
    public string DisplayName => counter.Name;
    /// <inheritdoc/>
    public string NameToken => Translations.Token(counter.NameKey);
    /// <inheritdoc/>
    public string GatedPrefabName => Id;

    /// <summary>Creates a content entry for one of the shared counter definitions.</summary>
    /// <param name="manager">Item manager.</param>
    /// <param name="key">Counter key.</param>
    protected BeastCounterItem(ItemManager manager, string key)
    {
        this.manager = manager;
        counter = BeastCounter.Get(key);
    }

    /// <inheritdoc/>
    public void Add()
    {
        try
        {
            CustomItem item = new(Id, counter.CopyFrom, new ItemConfig
            {
                Name = NameToken,
                Description = Translations.Token(counter.NameKey + "_description"),
                CraftingStation = CraftingStations.Workbench,
                Amount = counter.Yield,
                Requirements = counter.Requirements()
            });
            shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_maxQuality = 1;
            if (counter.IsArrow)
            {
                StatusEffect marker = ScriptableObject.CreateInstance<StatusEffect>();
                marker.name = "WhiteHiltCounter_" + counter.Key;
                shared.m_attackStatusEffect = marker;
                shared.m_attackStatusEffectChance = 1f;
            }
            else
            {
                effect = ScriptableObject.CreateInstance<BeastCounterCoating>();
                effect.name = "SE_" + Id;
                effect.CounterKey = counter.Key;
                effect.m_name = NameToken;
                effect.m_icon = shared.m_icons[0];
                shared.m_consumeStatusEffect = effect;
                shared.m_attackStatusEffect = null;
                shared.m_food = 0f;
                shared.m_foodStamina = 0f;
            }
            ApplyConfig();
            ApplyLook(item);
            if (effect != null)
            {
                effect.m_icon = shared.m_icons[0];
                manager.AddStatusEffect(new CustomStatusEffect(effect, false));
            }
            manager.AddItem(item);
            Jotunn.Logger.LogInfo($"{DisplayName} added!");
        }
        catch (Exception exception)
        {
            Jotunn.Logger.LogError($"{DisplayName} failed to load!");
            Jotunn.Logger.LogError(exception);
        }
    }

    /// <inheritdoc/>
    public void ApplyConfig()
    {
        if (shared == null)
        {
            return;
        }
        if (counter.IsArrow)
        {
            HitData.DamageTypes damage = new() { m_pierce = counter.Pierce };
            switch (counter.DamageType)
            {
                case HitData.DamageType.Fire: damage.m_fire = counter.Element; break;
                case HitData.DamageType.Frost: damage.m_frost = counter.Element; break;
                case HitData.DamageType.Spirit: damage.m_spirit = counter.Element; break;
                case HitData.DamageType.Poison: damage.m_poison = counter.Element; break;
                case HitData.DamageType.Lightning: damage.m_lightning = counter.Element; break;
            }
            shared.m_damages = damage;
        }
        RefreshYields();
    }

    /// <summary>Updates actual recipes after ObjectDB registration and server config changes.</summary>
    public static void RefreshYields()
    {
        if (ObjectDB.instance == null)
        {
            return;
        }
        foreach (Recipe recipe in ObjectDB.instance.m_recipes)
        {
            BeastCounter definition = BeastCounter.All.FirstOrDefault(candidate => recipe.m_item != null && candidate.PrefabName == recipe.m_item.name);
            if (definition != null)
            {
                recipe.m_amount = definition.Yield;
            }
        }
    }

    private void ApplyLook(CustomItem item)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }
        try
        {
            int index = Array.IndexOf(BeastCounter.All, counter);
            Color tint = Color.HSVToRGB(index / (float)BeastCounter.All.Length, 0.35f, 0.9f);
            if (counter.IsArrow)
            {
                VisualHelper.Tint(item.ItemPrefab, tint);
            }
            else
            {
                string modelName = counter.Key == "BerserkerCoating" ? "oilflask" : "whetstone";
                Texture2D texture = ForagingAssets.LoadTexture(modelName + "_albedo");
                GameObject model = VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh(modelName), texture);
                model.GetComponent<MeshRenderer>().sharedMaterial = ForageableBase.PlantMaterial(texture);
                VisualHelper.Tint(model, tint);
            }
            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }
        catch (Exception exception)
        {
            Jotunn.Logger.LogWarning($"{DisplayName}: keeping the vanilla look: {exception.Message}");
        }
    }
}

/// <summary>Lingonberry-treated flint arrows.</summary>
public sealed class TrollArrow : BeastCounterItem
{
    /// <summary>Creates the Troll Arrow entry.</summary>
    /// <param name="manager">Item manager.</param>
    public TrollArrow(ItemManager manager) : base(manager, "TrollArrow") { }
}
/// <summary>Peat and resin fire arrows.</summary>
public sealed class EmberArrow : BeastCounterItem
{
    /// <summary>Creates the Ember Arrow entry.</summary>
    /// <param name="manager">Item manager.</param>
    public EmberArrow(ItemManager manager) : base(manager, "EmberArrow") { }
}
/// <summary>Crowberry frost arrows.</summary>
public sealed class RimeArrow : BeastCounterItem
{
    /// <summary>Creates the Rime Arrow entry.</summary>
    /// <param name="manager">Item manager.</param>
    public RimeArrow(ItemManager manager) : base(manager, "RimeArrow") { }
}
/// <summary>Juniper and silver spirit arrows.</summary>
public sealed class SeidArrow : BeastCounterItem
{
    /// <summary>Creates the Seid Arrow entry.</summary>
    /// <param name="manager">Item manager.</param>
    public SeidArrow(ItemManager manager) : base(manager, "SeidArrow") { }
}
/// <summary>Sweet gale and poison gland arrows.</summary>
public sealed class BogVenomArrow : BeastCounterItem
{
    /// <summary>Creates the Bog Venom Arrow entry.</summary>
    /// <param name="manager">Item manager.</param>
    public BogVenomArrow(ItemManager manager) : base(manager, "BogVenomArrow") { }
}
/// <summary>Rosehip and kraken ink lightning arrows.</summary>
public sealed class StormArrow : BeastCounterItem
{
    /// <summary>Creates the Storm Arrow entry.</summary>
    /// <param name="manager">Item manager.</param>
    public StormArrow(ItemManager manager) : base(manager, "StormArrow") { }
}
/// <summary>Rock lichen preparation for a pickaxe.</summary>
public sealed class StonebreakerCoating : BeastCounterItem
{
    /// <summary>Creates the Stonebreaker Coating entry.</summary>
    /// <param name="manager">Item manager.</param>
    public StonebreakerCoating(ItemManager manager) : base(manager, "StonebreakerCoating") { }
}
/// <summary>Henbane preparation for a blunt weapon.</summary>
public sealed class BerserkerCoating : BeastCounterItem
{
    /// <summary>Creates the Berserker Coating entry.</summary>
    /// <param name="manager">Item manager.</param>
    public BerserkerCoating(ItemManager manager) : base(manager, "BerserkerCoating") { }
}
/// <summary>Woad and slate treatment for a slashing weapon.</summary>
public sealed class CarapaceWhetstone : BeastCounterItem
{
    /// <summary>Creates the Carapace Whetstone entry.</summary>
    /// <param name="manager">Item manager.</param>
    public CarapaceWhetstone(ItemManager manager) : base(manager, "CarapaceWhetstone") { }
}