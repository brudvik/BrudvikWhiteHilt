using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Cooking;

/// <summary>
/// A drying rack (hjell): two crossed-pole trestles with a ridge pole, where hams, sausages and cod hang for days to
/// cure.
/// It is a vanilla cooking station without fire: <see cref="DryingRackAir"/> keeps it "lit" by the wind, nothing burns,
/// and the slots neither smoke nor sizzle.
/// </summary>
public class DryingRack : DefensePieceBase
{
    /// <summary>Prefab name of the drying rack.</summary>
    public const string RackPrefabName = "piece_whitehilt_hjell";

    private const int SlotCount = 8;
    private const float SlotSpacing = 0.4f;
    private const float SlotHeight = 1.75f;

    private static readonly Dictionary<string, Func<float>> cookTimes = new();

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DryingRack(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "hjell";

    /// <inheritdoc/>
    protected override string FullName => "Drying Rack";

    /// <inheritdoc/>
    protected override string Description => "A rack of poles where hams, sausages and cod hang in the wind for days until they are cured. Needs no fire.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 10, Recover = true },
        new() { Item = "RoundLog", Amount = 4, Recover = true },
        new() { Item = "LeatherScraps", Amount = 2, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 400f;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Crafting;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <summary>
    /// Registers how long an item hangs, so a changed setting reaches the rack.
    /// </summary>
    /// <param name="item">Prefab name of the hung item.</param>
    /// <param name="seconds">Its time in seconds, read from the settings.</param>
    public static void RegisterCookTime(string item, Func<float> seconds)
    {
        cookTimes[item] = seconds;
    }

    /// <summary>
    /// Applies the curing times from the settings to the rack prefab and to the racks in the loaded world.
    /// </summary>
    public static void ApplyCookTimes()
    {
        GameObject prefab = PrefabManager.Instance.GetPrefab(RackPrefabName);
        IEnumerable<CookingStation> stations = UnityEngine.Object.FindObjectsByType<DryingRackAir>(FindObjectsSortMode.None)
            .Select(rack => rack.GetComponent<CookingStation>());
        if (prefab != null)
        {
            stations = stations.Append(prefab.GetComponent<CookingStation>());
        }

        foreach (CookingStation station in stations)
        {
            foreach (CookingStation.ItemConversion conversion in station?.m_conversion ?? new List<CookingStation.ItemConversion>())
            {
                if (conversion.m_from != null && cookTimes.TryGetValue(conversion.m_from.name, out Func<float> seconds))
                {
                    conversion.m_cookTime = seconds();
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        CookingStation station = prefab.GetComponent<CookingStation>();
        Transform template = prefab.transform.Find("slot0");
        if (station == null || template == null)
        {
            Jotunn.Logger.LogWarning($"{FullName}: the vanilla cooking station was not found, nothing can hang on it.");
            return;
        }

        Transform[] slots = new Transform[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            Transform slot = prefab.transform.Find($"slot{i}") ?? UnityEngine.Object.Instantiate(template.gameObject, prefab.transform).transform;
            slot.name = $"slot{i}";
            slot.localPosition = new Vector3((i - (SlotCount - 1) / 2f) * SlotSpacing, SlotHeight, 0f);
            foreach (ParticleSystemRenderer renderer in slot.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                renderer.enabled = false;
            }

            AudioSource audio = slot.GetComponent<AudioSource>();
            if (audio != null)
            {
                audio.volume = 0f;
            }

            slots[i] = slot;
        }

        station.m_slots = slots;
        station.m_burntPS = new ParticleSystem[0];
        station.m_donePS = new ParticleSystem[0];
        station.m_spawnPoint = null;
        station.m_fireCheckPoints = new Transform[0];
        station.m_requireFire = false;
        station.m_useFuel = true;
        station.m_useFueldWhileEmpty = false;
        station.m_fuelItem = null;
        station.m_maxFuel = 1;
        station.m_secPerFuel = DryingRackAir.SecondsPerFuel;
        station.m_haveFireObject = null;
        station.m_haveFuelObject = null;
        station.m_cookingObject = null;
        station.m_canOvercookItems = false;
        station.m_name = Translations.Token(PrefabName);
        station.m_addItemTooltip = "$whitehilt_dryingrack_hang";
        station.m_fullyCookedTooltip = "$whitehilt_dryingrack_take";
        station.m_noCookableItemsMessage = "$msg_whitehilt_dryingrack_nothing";
        station.m_conversion = new List<CookingStation.ItemConversion>();
        prefab.AddComponent<DryingRackAir>();
    }
}

/// <summary>
/// Keeps a drying rack's "fuel" topped up on its owner, so the cooking station cures what hangs on it without any fire.
/// </summary>
public class DryingRackAir : MonoBehaviour
{
    /// <summary>Seconds one unit of fuel lasts; long, so a top-up now and then keeps it going.</summary>
    public const int SecondsPerFuel = 100000;

    private const float TopUpSeconds = 5f;

    private ZNetView nview;
    private float nextTopUp;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
    }

    private void Update()
    {
        if (Time.time < nextTopUp || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        nextTopUp = Time.time + TopUpSeconds;
        nview.GetZDO().Set(ZDOVars.s_fuel, 1f);
    }
}
