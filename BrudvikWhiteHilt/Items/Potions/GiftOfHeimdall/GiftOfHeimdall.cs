using BepInEx.Configuration;
using BrudvikWhiteHilt.Items.Foraging.Angelica;
using BrudvikWhiteHilt.Items.Foraging.Crowberries;
using BrudvikWhiteHilt.Mastery;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfHeimdall;

/// <summary>
/// The Gift of Heimdall, the watchman who hears the grass grow: every foe nearby shows on the map while it lasts.
/// Its icon is the rendered, tinted mead.
/// </summary>
public class GiftOfHeimdall : PotionBase
{
    private const string Name = "GiftOfHeimdall";

    // Bound before the base constructor, which creates the effect once to register its text.
    static GiftOfHeimdall()
    {
        DurationMinutes = PotionSettings.BindDuration(Name, 10f);
        Radius = PotionSettings.BindSetting(Name, "Radius", 60f, 10f, 200f, "How far away foes show on the map, in metres.");
        RefreshSeconds = PotionSettings.BindSetting(Name, "RefreshSeconds", 2f, 0.5f, 10f, "Seconds between updates of the foes on the map.");
    }

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public GiftOfHeimdall(ItemManager instance) : base(instance) { }

    /// <summary>Duration in minutes.</summary>
    public static ConfigEntry<float> DurationMinutes { get; private set; }

    /// <summary>How far away foes show, in metres.</summary>
    public static ConfigEntry<float> Radius { get; private set; }

    /// <summary>Seconds between updates.</summary>
    public static ConfigEntry<float> RefreshSeconds { get; private set; }

    /// <inheritdoc/>
    protected override string BaseName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Gift of Heimdall";

    /// <inheritdoc/>
    protected override string Description => "Grants you the senses of Heimdall, the watchman who hears the grass grow";

    /// <inheritdoc/>
    protected override Color Tint => new(1f, 0.9f, 0.55f);

    /// <inheritdoc/>
    protected override RequirementConfig[] MeadBaseRequirements => new[]
    {
        new RequirementConfig { Item = Angelica.PrefabName, Amount = 5, Recover = false },
        new RequirementConfig { Item = Crowberries.PrefabName, Amount = 10, Recover = false },
        new RequirementConfig { Item = "Crystal", Amount = 2, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mountain;

    /// <inheritdoc/>
    protected override SE_Stats CreateEffect()
    {
        var effect = ScriptableObject.CreateInstance<GiftOfHeimdallEffect>();
        effect.Initialize(FullName);
        return effect;
    }
}

/// <summary>
/// The effect of the Gift of Heimdall: foes nearby show as pins on the map.
/// </summary>
public class GiftOfHeimdallEffect : SE_Stats
{
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
        m_startMessage = $"You hear the grass grow with {effectName}!";
        m_stopMessageType = MessageHud.MessageType.Center;
        m_stopMessage = $"{effectName} has faded!";
        m_tooltip = "Foes nearby show on the map";
    }

    /// <summary>
    /// Sets the configured duration.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = GiftOfHeimdall.DurationMinutes.Value * 60f;
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

        float refresh = GiftOfHeimdall.RefreshSeconds.Value;
        timer = refresh;
        float radius = GiftOfHeimdall.Radius.Value;
        Vector3 position = m_character.transform.position;
        foreach (Character other in Character.GetAllCharacters())
        {
            if (other != m_character && !other.IsDead() && BaseAI.IsEnemy(m_character, other)
                && Vector3.Distance(other.transform.position, position) <= radius)
            {
                TemporaryPins.Add(other.transform.position, Minimap.PinType.Icon3, string.Empty, refresh + 0.1f);
            }
        }
    }
}
