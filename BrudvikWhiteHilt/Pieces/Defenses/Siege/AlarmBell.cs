using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses.GateControl;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace BrudvikWhiteHilt.Pieces.Defenses.Siege;

/// <summary>
/// The Alarm Bell. Ringing it shuts every gate, drawbridge, portcullis and door around it, and tells every player
/// within earshot, with a mark on the map. A guestbook nearby that notes a raid rings it by itself.
/// </summary>
public class AlarmBell : MonoBehaviour, Interactable, Hoverable
{
    private const string RingRpc = "WhiteHiltBellRing";
    private const float CooldownSeconds = 8f;
    private const float SwingSeconds = 4f;
    private const float PinSeconds = 60f;

    private static readonly HashSet<AlarmBell> bells = new();
    private static AudioClip clip;
    private static AudioMixerGroup mixer;

    private ZNetView nview;
    private Piece piece;
    private Transform bell;
    private Quaternion bellRest;
    private AudioSource audioSource;
    private float rungAt = float.NegativeInfinity;

    /// <summary>
    /// Rings the bells within range of a raid, when the setting allows. Called by the guestbook noting the raid.
    /// </summary>
    /// <param name="position">Where the raid is noted.</param>
    /// <param name="range">The guestbook's radius.</param>
    public static void RaidStarted(Vector3 position, float range)
    {
        if (!SiegeSettings.BellRingsOnRaid.Value)
        {
            return;
        }

        foreach (AlarmBell candidate in bells.Where(candidate => candidate != null && (candidate.transform.position - position).sqrMagnitude <= range * range))
        {
            candidate.Ring("$whitehilt_bell_raid", false);
        }
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer || !PrivateArea.CheckAccess(transform.position))
        {
            return false;
        }

        Ring(((Player)user).GetPlayerName(), true);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        string text = GetHoverName() + "\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_bell_ring";
        if (SiegeSettings.BellRingsOnRaid.Value)
        {
            text += "\n<color=#AAAAAA>$whitehilt_bell_auto</color>";
        }

        return Localization.instance.Localize(text);
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

    // Rings for everyone, and shuts what is around the bell from this machine. A ward refusing the player flashes when
    // a player rings it; a raid ringing it only shuts what this machine may.
    private void Ring(string who, bool byHand)
    {
        if (nview == null || !nview.IsValid() || Time.time - rungAt < CooldownSeconds)
        {
            return;
        }

        rungAt = Time.time;
        nview.InvokeRPC(ZNetView.Everybody, RingRpc, who ?? string.Empty);
        float range = SiegeSettings.BellShutRange.Value;
        foreach (GateMechanism mechanism in GateMechanisms.Around(transform.position, range))
        {
            if (mechanism.IsOpen && PrivateArea.CheckAccess(mechanism.Position, 0f, byHand))
            {
                mechanism.Set(false);
            }
        }

        foreach (Door door in Doors(range))
        {
            ZNetView view = door.GetComponent<ZNetView>();
            if (view != null && view.IsValid() && view.GetZDO().GetInt(ZDOVars.s_state) != 0 && PrivateArea.CheckAccess(door.transform.position, 0f, byHand))
            {
                view.InvokeRPC("UseDoor", true);
            }
        }
    }

    // Ordinary doors around the bell; the gates and drawbridges are shut as gate mechanisms.
    private IEnumerable<Door> Doors(float range)
    {
        HashSet<Door> doors = new();
        foreach (Collider collider in Physics.OverlapSphere(transform.position, range, LayerMask.GetMask("piece", "piece_nonsolid", "Default")))
        {
            Door door = collider.GetComponentInParent<Door>();
            if (door != null && door.m_keyItem == null && !door.m_canNotBeClosed && door.GetComponent<GateLeafDriver>() == null
                && door.GetComponent<DrawbridgeDriver>() == null)
            {
                doors.Add(door);
            }
        }

        return doors;
    }

    // On everyone: the bell swings and sounds; players within earshot are told, with a mark on the map for a minute.
    private void RPC_Ring(long sender, string who)
    {
        rungAt = Time.time;
        if (!VisualHelper.IsHeadless)
        {
            EnsureAudio();
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }

        Player player = Player.m_localPlayer;
        if (player == null || Vector3.Distance(player.transform.position, transform.position) > SiegeSettings.BellAlertRange.Value)
        {
            return;
        }

        player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$whitehilt_bell_rings"), Localization.instance.Localize(who)));
        if (Minimap.instance != null)
        {
            Minimap.PinData pin = Minimap.instance.AddPin(transform.position, Minimap.PinType.Icon3, Localization.instance.Localize("$whitehilt_bell_pin"), false, false);
            Minimap.instance.StartCoroutine(RemovePin(pin));
        }
    }

    private static System.Collections.IEnumerator RemovePin(Minimap.PinData pin)
    {
        yield return new WaitForSeconds(PinSeconds);
        Minimap.instance?.RemovePin(pin);
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        bell = transform.Find("bell");
        bellRest = bell != null ? bell.localRotation : Quaternion.identity;
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        nview.Register<string>(RingRpc, RPC_Ring);
        bells.Add(this);
    }

    private void OnDestroy()
    {
        bells.Remove(this);
    }

    // Swings back and forth, dying away.
    private void Update()
    {
        if (bell == null)
        {
            return;
        }

        float t = Time.time - rungAt;
        float angle = t >= 0f && t < SwingSeconds ? 22f * Mathf.Sin(t * 7f) * (1f - t / SwingSeconds) : 0f;
        bell.localRotation = bellRest * Quaternion.Euler(angle, 0f, 0f);
    }

    private void EnsureAudio()
    {
        if (audioSource != null)
        {
            return;
        }

        clip ??= BellTone();
        if (mixer == null)
        {
            var effects = PrefabManager.Instance.GetPrefab("Wolf")?.GetComponent<MonsterAI>()?.m_alertedEffects.m_effectPrefabs;
            mixer = effects?.Select(effect => effect.m_prefab?.GetComponentInChildren<AudioSource>(true)).FirstOrDefault(source => source != null)?.outputAudioMixerGroup;
        }

        GameObject sound = new("WhiteHiltBellSound");
        sound.transform.SetParent(transform, false);
        sound.transform.localPosition = new Vector3(0f, 2f, 0f);
        audioSource = sound.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = clip;
        audioSource.outputAudioMixerGroup = mixer;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 5f;
        audioSource.maxDistance = SiegeSettings.BellAlertRange.Value;
    }

    // A bell's tone made in code: a few inharmonic partials of a struck bell, each dying away at its own pace,
    // rung three times.
    private static AudioClip BellTone()
    {
        const int rate = 22050;
        const float seconds = 4.5f;
        float[] ratios = { 0.5f, 1f, 1.183f, 1.506f, 2f, 2.514f, 2.662f, 3.011f };
        float[] amplitudes = { 0.35f, 1f, 0.6f, 0.5f, 0.45f, 0.25f, 0.2f, 0.15f };
        float[] decays = { 2.5f, 2.2f, 1.8f, 1.4f, 1.1f, 0.8f, 0.7f, 0.5f };
        float[] strikes = { 0f, 1.2f, 2.4f };
        const float fundamental = 330f;
        int count = (int)(rate * seconds);
        float[] samples = new float[count];
        float peak = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)rate;
            float value = 0f;
            foreach (float strike in strikes)
            {
                float since = t - strike;
                if (since < 0f)
                {
                    continue;
                }

                float attack = Mathf.Clamp01(since / 0.004f);
                for (int k = 0; k < ratios.Length; k++)
                {
                    value += amplitudes[k] * attack * Mathf.Exp(-since / decays[k]) * Mathf.Sin(2f * Mathf.PI * fundamental * ratios[k] * since);
                }
            }

            samples[i] = value;
            peak = Mathf.Max(peak, Mathf.Abs(value));
        }

        for (int i = 0; i < count; i++)
        {
            samples[i] = samples[i] / peak * 0.8f;
        }

        // The samples are handed over through the reader callback: SetData has an overload Unity 6 builds on a newer
        // netstandard than the mod compiles against.
        int position = 0;
        return AudioClip.Create("WhiteHiltBell", count, 1, rate, false,
            data =>
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = position + i < count ? samples[position + i] : 0f;
                }

                position += data.Length;
            },
            newPosition => position = newPosition);
    }
}

/// <summary>
/// The Alarm Bell piece.
/// </summary>
public class AlarmBellPiece : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public AlarmBellPiece(PieceManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string LayoutName => "alarmklokke";

    /// <inheritdoc/>
    protected override string FullName => "Alarm Bell";

    /// <inheritdoc/>
    protected override string Description => "A bell under a small roof on two posts. Ring it and every gate, drawbridge, portcullis and door around it shuts, and everyone near hears it and sees where on the map. It rings by itself when a guestbook nearby notes a raid.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Wood", Amount = 8, Recover = true },
        new() { Item = "Bronze", Amount = 4, Recover = true },
        new() { Item = "Stone", Amount = 6, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 800f;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        prefab.AddComponent<AlarmBell>();
    }
}
