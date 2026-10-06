using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.Foraging.Ergot;
using BrudvikWhiteHilt.Items.Foraging.Henbane;
using BrudvikWhiteHilt.Items.Foraging.RockLichen;
using BrudvikWhiteHilt.Mastery;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfVolva;

/// <summary>
/// The Gift of the Völva, the seeress's draught: what can be picked and treasure nearby show on the map while it lasts.
/// Its icon is the rendered, tinted mead.
/// </summary>
public class GiftOfVolva : PotionBase
{
    private const string Name = "GiftOfVolva";

    // Bound before the base constructor, which creates the effect once to register its text.
    static GiftOfVolva()
    {
        DurationMinutes = PotionSettings.BindDuration(Name, 10f);
        SightRadius = PotionSettings.BindSetting(Name, "SightRadius", 50f, 10f, 150f, "How far away things to pick and treasure show on the map, in metres.");
        MaxPins = PotionSettings.BindSetting(Name, "MaxPins", 40f, 5f, 200f, "Most things to pick shown at once, nearest first. Treasure always shows.");
        RefreshSeconds = PotionSettings.BindSetting(Name, "RefreshSeconds", 4f, 1f, 20f, "Seconds between updates of the map.");
    }

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GiftOfVolva(ItemManager instance) : base(instance) { }

    /// <summary>Duration in minutes.</summary>
    public static ConfigEntry<float> DurationMinutes { get; private set; }

    /// <summary>How far away things show, in metres.</summary>
    public static ConfigEntry<float> SightRadius { get; private set; }

    /// <summary>Most things to pick shown at once.</summary>
    public static ConfigEntry<float> MaxPins { get; private set; }

    /// <summary>Seconds between updates.</summary>
    public static ConfigEntry<float> RefreshSeconds { get; private set; }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Gift of the Völva";

    /// <inheritdoc/>
    protected override string Description => "The seeress's draught. Henbane smoke and blighted grain open eyes that see what hides in the grass and under the earth";

    /// <inheritdoc/>
    protected override Color Tint => new(0.45f, 0.3f, 0.6f);

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = Henbane.PrefabName, Amount = 4, Recover = false },
        new RequirementConfig { Item = Ergot.PrefabName, Amount = 3, Recover = false },
        new RequirementConfig { Item = RockLichen.PrefabName, Amount = 5, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Plains;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfVolvaEffect>();
        effect.Initialize(FullName);
        return effect;
    }
}

/// <summary>
/// The effect of the Gift of the Völva: things to pick and unopened treasure nearby show as pins on the map.
/// </summary>
public class GiftOfVolvaEffect : SE_Stats
{
    private static int mask;

    private readonly List<Pickable> found = new();
    private readonly HashSet<Component> seen = new();
    private float timer;

    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName">Name of the potion.</param>
    public void Initialize(string effectName)
    {
        name = effectName;
        m_name = effectName;
        m_startMessageType = MessageHud.MessageType.Center;
        m_startMessage = $"Your eyes open with the {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"The {effectName} has faded!";
        m_tooltip = "Things to pick and treasure nearby show on the map";
    }

    /// <summary>
    /// Sets the configured duration.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_think";
        m_ttl = GiftOfVolva.DurationMinutes.Value * 60f;
    }

    /// <inheritdoc/>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        timer -= dt;
        if (timer > 0f || m_character == null || m_character != Player.m_localPlayer)
        {
            return;
        }

        float refresh = GiftOfVolva.RefreshSeconds.Value;
        timer = refresh;
        if (mask == 0)
        {
            mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "item");
        }

        Vector3 position = m_character.transform.position;
        found.Clear();
        seen.Clear();
        foreach (Collider collider in Physics.OverlapSphere(position, GiftOfVolva.SightRadius.Value, mask))
        {
            Pickable pickable = collider.GetComponentInParent<Pickable>();
            if (pickable != null)
            {
                if (seen.Add(pickable) && pickable.CanBePicked())
                {
                    found.Add(pickable);
                }

                continue;
            }

            Container container = collider.GetComponentInParent<Container>();
            if (container != null && seen.Add(container) && IsTreasure(container))
            {
                TemporaryPins.Add(container.transform.position, Minimap.PinType.Icon4, string.Empty, refresh + 0.1f);
            }
        }

        found.Sort((a, b) => (a.transform.position - position).sqrMagnitude.CompareTo((b.transform.position - position).sqrMagnitude));
        int count = Mathf.Min(found.Count, Mathf.RoundToInt(GiftOfVolva.MaxPins.Value));
        for (int i = 0; i < count; i++)
        {
            TemporaryPins.Add(found[i].transform.position, Minimap.PinType.Icon3, string.Empty, refresh + 0.1f);
        }
    }

    // A chest the world placed, not one a player built, with something still in it.
    private static bool IsTreasure(Container container)
    {
        Piece piece = container.GetComponent<Piece>();
        return (piece == null || !piece.IsPlacedByPlayer()) && container.m_nview != null && container.m_nview.IsValid()
            && container.GetInventory() != null && container.GetInventory().NrOfItems() > 0;
    }
}
