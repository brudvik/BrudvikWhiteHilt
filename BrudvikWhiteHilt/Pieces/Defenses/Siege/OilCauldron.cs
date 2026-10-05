using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Defenses.Siege;

/// <summary>
/// The Oil Cauldron on the walk over a gate. Fill it with pitch (Resin or Tar), which must heat a while, or with stones;
/// tip it and it pours down in front of the wall, through the machicolations: pitch burns, stones strike. What it
/// holds and when it was filled are kept on the cauldron; the player who tips it deals the damage.
/// </summary>
public class OilCauldron : MonoBehaviour, Interactable, Hoverable
{
    private const string LoadRpc = "WhiteHiltCauldronLoad";
    private const string PourRpc = "WhiteHiltCauldronPour";
    private const float TipAngle = 105f;
    private const float TipSeconds = 0.6f;
    private const float HoldSeconds = 0.6f;
    private const float BackSeconds = 1.0f;

    // Below and in front of the cauldron: 2.4 m wide, from the walk down 5 m, 1 to 3 m out.
    private static readonly Vector3 HitCentre = new(0f, -2.5f, 2.0f);
    private static readonly Vector3 HitHalfSize = new(1.2f, 2.5f, 1.0f);

    private static readonly int contentsKey = "whitehilt_cauldron".GetStableHashCode();
    private static readonly int filledKey = "whitehilt_cauldron_filled".GetStableHashCode();

    // What can fill it: item prefab, amount, and what it becomes.
    private static readonly (string Item, int Amount, Contents Contents)[] fillings =
    {
        ("Resin", 5, Contents.Pitch),
        ("Tar", 2, Contents.Pitch),
        ("Stone", 10, Contents.Stones)
    };

    private static EffectList pitchEffects;
    private static EffectList stoneEffects;

    private ZNetView nview;
    private Piece piece;
    private Transform pot;
    private Quaternion potRest;
    private float pouredAt = float.NegativeInfinity;
    private Light glow;

    /// <summary>What the cauldron holds.</summary>
    public enum Contents
    {
        /// <summary>Nothing.</summary>
        Empty,

        /// <summary>Pitch, hot once it has heated.</summary>
        Pitch,

        /// <summary>Stones.</summary>
        Stones
    }

    private Contents Held => nview != null && nview.IsValid() ? (Contents)nview.GetZDO().GetInt(contentsKey) : Contents.Empty;

    // Seconds the pitch still needs; 0 when hot or for stones.
    private float HeatLeft
    {
        get
        {
            if (Held != Contents.Pitch || ZNet.instance == null)
            {
                return 0f;
            }

            double since = (ZNet.instance.GetTime().Ticks - nview.GetZDO().GetLong(filledKey)) / (double)TimeSpan.TicksPerSecond;
            return Mathf.Max(0f, SiegeSettings.HeatSeconds.Value - (float)since);
        }
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer || nview == null || !nview.IsValid() || !PrivateArea.CheckAccess(transform.position))
        {
            return false;
        }

        Contents held = Held;
        if (held == Contents.Empty)
        {
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_cauldron_nothing"));
            return true;
        }

        if (HeatLeft > 0f)
        {
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_cauldron_not_hot"));
            return true;
        }

        Strike((Player)user, held);
        nview.InvokeRPC(ZNetView.Everybody, PourRpc, (int)held);
        nview.InvokeRPC(LoadRpc, (int)Contents.Empty);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (user != Player.m_localPlayer || item?.m_dropPrefab == null || nview == null || !nview.IsValid())
        {
            return false;
        }

        (string Item, int Amount, Contents Contents) filling = fillings.FirstOrDefault(candidate => candidate.Item == item.m_dropPrefab.name);
        if (filling.Item == null)
        {
            return false;
        }

        if (!PrivateArea.CheckAccess(transform.position))
        {
            return true;
        }

        if (Held != Contents.Empty)
        {
            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_cauldron_full"));
            return true;
        }

        Inventory inventory = user.GetInventory();
        if (inventory.CountItems(item.m_shared.m_name) < filling.Amount)
        {
            user.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$whitehilt_cauldron_need"),
                Localization.instance.Localize(item.m_shared.m_name), filling.Amount));
            return true;
        }

        inventory.RemoveItem(item.m_shared.m_name, filling.Amount);
        nview.InvokeRPC(LoadRpc, (int)filling.Contents);
        return true;
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        string state = Held switch
        {
            Contents.Pitch when HeatLeft > 0f => string.Format(Localization.instance.Localize("$whitehilt_cauldron_heating"), Mathf.CeilToInt(HeatLeft)),
            Contents.Pitch => Localization.instance.Localize("$whitehilt_cauldron_pitch"),
            Contents.Stones => Localization.instance.Localize("$whitehilt_cauldron_stones"),
            _ => Localization.instance.Localize("$whitehilt_cauldron_empty")
        };
        string text = $"{GetHoverName()} ({state})";
        if (Held != Contents.Empty)
        {
            text += "\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_cauldron_pour";
        }

        return Localization.instance.Localize(text + "\n<color=#AAAAAA>$whitehilt_cauldron_load</color>");
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? piece.m_name : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        pot = transform.Find("pot");
        potRest = pot != null ? pot.localRotation : Quaternion.identity;
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        nview.Register<int>(LoadRpc, RPC_Load);
        nview.Register<int>(PourRpc, RPC_Pour);
        if (!Helpers.VisualHelper.IsHeadless)
        {
            GameObject lightObject = new("CauldronEmbers");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            glow = lightObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.45f, 0.15f);
            glow.range = 2.5f;
            glow.intensity = 0f;
            glow.shadows = LightShadows.None;
        }
    }

    // On the owner: what the cauldron holds, and when it was filled.
    private void RPC_Load(long sender, int contents)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        nview.GetZDO().Set(contentsKey, contents);
        nview.GetZDO().Set(filledKey, ZNet.instance.GetTime().Ticks);
    }

    // On everyone: the pot tips, and what it held pours down with its effects.
    private void RPC_Pour(long sender, int contents)
    {
        pouredAt = Time.time;
        Vector3 impact = transform.TransformPoint(new Vector3(0f, HitCentre.y - HitHalfSize.y + 0.2f, HitCentre.z));
        EffectList effects = (Contents)contents == Contents.Pitch ? PitchEffects() : StoneEffects();
        effects?.Create(impact, Quaternion.identity);
    }

    // The player who tips it deals the damage to every creature in the area below; players and tamed animals are spared.
    private void Strike(Player player, Contents held)
    {
        Collider[] hits = Physics.OverlapBox(transform.TransformPoint(HitCentre), HitHalfSize, transform.rotation);
        HashSet<Character> struck = new();
        foreach (Collider collider in hits)
        {
            Character character = collider.GetComponentInParent<Character>();
            if (character == null || character.IsPlayer() || character.IsTamed() || character.IsDead() || !struck.Add(character))
            {
                continue;
            }

            HitData hit = new()
            {
                m_point = character.GetCenterPoint(),
                m_dir = Vector3.down,
                m_attacker = player.GetZDOID(),
                m_pushForce = held == Contents.Stones ? 20f : 0f
            };
            if (held == Contents.Pitch)
            {
                hit.m_damage.m_fire = SiegeSettings.PitchDamage.Value;
            }
            else
            {
                hit.m_damage.m_blunt = SiegeSettings.StoneDamage.Value;
            }

            character.Damage(hit);
        }
    }

    private void Update()
    {
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        if (glow != null)
        {
            bool heating = Held == Contents.Pitch;
            glow.intensity = heating ? 0.8f + 0.25f * Mathf.Sin(Time.time * 7f) : 0.2f;
        }

        if (pot == null)
        {
            return;
        }

        // Tips forward, holds, and comes back.
        float t = Time.time - pouredAt;
        float angle = t < 0f ? 0f
            : t < TipSeconds ? Mathf.SmoothStep(0f, TipAngle, t / TipSeconds)
            : t < TipSeconds + HoldSeconds ? TipAngle
            : t < TipSeconds + HoldSeconds + BackSeconds ? Mathf.SmoothStep(TipAngle, 0f, (t - TipSeconds - HoldSeconds) / BackSeconds)
            : 0f;
        pot.localRotation = potRest * Quaternion.Euler(angle, 0f, 0f);
    }

    // Sparks and flame of a fire flaring up, for pitch.
    private static EffectList PitchEffects()
    {
        return pitchEffects ??= ZNetScene.instance?.GetPrefab("fire_pit")?.GetComponent<Fireplace>()?.m_fuelAddedEffects;
    }

    // Dust and the crack of a rock breaking, for stones.
    private static EffectList StoneEffects()
    {
        return stoneEffects ??= ZNetScene.instance?.GetPrefab("rock4_coast")?.GetComponent<MineRock5>()?.m_destroyedEffect;
    }
}

/// <summary>
/// The Oil Cauldron piece.
/// </summary>
public class OilCauldronPiece : DefensePieceBase
{
    /// <summary>
    /// Constructor for the OilCauldronPiece class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public OilCauldronPiece(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "oljegryte";

    /// <inheritdoc/>
    protected override string FullName => "Oil Cauldron";

    /// <inheritdoc/>
    protected override string Description => "An iron cauldron on a tipping frame over a small hearth, with a chute out over the parapet. Fill it with Resin, Tar or Stone, and tip it to pour boiling pitch or stones on whoever stands below the wall.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 10, Recover = true },
        new() { Item = "Iron", Amount = 4, Recover = true },
        new() { Item = "Wood", Amount = 6, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => DefenseSettings.Scale(1500f);

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        StoneDefense.Harden(prefab);
        prefab.AddComponent<OilCauldron>();
    }
}
