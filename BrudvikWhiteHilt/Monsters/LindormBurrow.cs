using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Sits on the Lindorm. It breaks out of the ground where it spawns, and burrows back down when it has lost its prey for a
/// while, or at dawn. While it is below ground it neither moves nor takes damage. The Lava Worm
/// (<see cref="LavaWormBurrow"/>) rises out of the lava the same way, with its own settings.
/// </summary>
public class LindormBurrow : MonoBehaviour
{
    /// <summary>Path of the model under the creature.</summary>
    public const string VisualPath = "Visual/lindorm_visual";

    private const string StartKey = "whitehilt_lindorm_start";
    private const string RetreatKey = "whitehilt_lindorm_retreat";
    private const string HomeKey = "whitehilt_lindorm_home";
    private const float RiseSeconds = 2.2f;
    private const float SinkSeconds = 2.5f;
    private const float AnnounceRange = 40f;
    private static readonly string[] burstEffects = { "vfx_RockDestroyed_large", "sfx_rock_destroyed" };

    private static readonly List<LindormBurrow> instances = new();

    private ZNetView nview;
    private Character character;
    private MonsterAI ai;
    private Transform visual;
    private float depth;
    private float nextTick;
    private float lastPrey;

    /// <summary>Every Lindorm in the loaded world.</summary>
    public static IReadOnlyList<LindormBurrow> Instances => instances;

    /// <summary>How deep below the ground it waits, in metres.</summary>
    protected virtual float Depth => 3.5f * MonsterSettings.LindormScale.Value;

    /// <summary>Whether it leaves at dawn.</summary>
    protected virtual bool LeavesAtDawn => MonsterSettings.LindormNightOnly.Value;

    /// <summary>Seconds without prey before it leaves.</summary>
    protected virtual float GiveUpSeconds => MonsterSettings.LindormGiveUpSeconds.Value;

    /// <summary>How far from where it rose it follows its prey before it leaves, or zero for no limit.</summary>
    protected virtual float LeashRange => 0f;

    /// <summary>What a player close by is told as it rises.</summary>
    protected virtual string RiseMessage => "$msg_whitehilt_lindorm_rises";

    /// <summary>Whether it is still rising or already burrowing away.</summary>
    public bool IsHidden => nview != null && nview.IsValid() && (Seconds(StartKey) < RiseSeconds * 0.7f || nview.GetZDO().GetLong(RetreatKey) != 0L);

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        visual = transform.Find(VisualPath);
        depth = Depth;
        lastPrey = Time.time;
        instances.Add(this);
        if (nview.IsValid() && nview.IsOwner() && nview.GetZDO().GetLong(StartKey) == 0L)
        {
            nview.GetZDO().Set(StartKey, ZNet.instance.GetTime().Ticks);
            nview.GetZDO().Set(HomeKey, transform.position);
            Burst();
        }

        UpdateVisual();
    }

    private void Start()
    {
        Player player = Player.m_localPlayer;
        if (player == null || Seconds(StartKey) > RiseSeconds || Vector3.Distance(player.transform.position, transform.position) > AnnounceRange)
        {
            return;
        }

        player.Message(MessageHud.MessageType.Center, RiseMessage);
        GameCamera.instance?.AddShake(transform.position, 40f, 1.5f, false);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    // On the owner: sinks back into the ground when it has had no prey for a while, sooner at dawn if it only comes at
    // night.
    private void Update()
    {
        if (!nview.IsValid())
        {
            return;
        }

        UpdateVisual();
        if (!nview.IsOwner() || Time.time < nextTick || character.IsDead())
        {
            return;
        }

        nextTick = Time.time + 1f;
        if (nview.GetZDO().GetLong(RetreatKey) != 0L)
        {
            if (Seconds(RetreatKey) > SinkSeconds)
            {
                nview.Destroy();
            }

            return;
        }

        if (ai != null && ai.GetTargetCreature() != null)
        {
            lastPrey = Time.time;
        }

        bool dawn = LeavesAtDawn && !EnvMan.IsNight();
        if (Time.time - lastPrey > (dawn ? 5f : GiveUpSeconds) || TooFarFromHome())
        {
            Retreat();
        }
    }

    private bool TooFarFromHome()
    {
        if (LeashRange <= 0f)
        {
            return false;
        }

        Vector3 home = nview.GetZDO().GetVec3(HomeKey, transform.position);
        home.y = transform.position.y;
        return Vector3.Distance(home, transform.position) > LeashRange;
    }

    /// <summary>
    /// Burrows back into the ground, without loot.
    /// </summary>
    public void Retreat()
    {
        if (!nview.IsValid() || !nview.IsOwner() || nview.GetZDO().GetLong(RetreatKey) != 0L)
        {
            return;
        }

        nview.GetZDO().Set(RetreatKey, ZNet.instance.GetTime().Ticks);
        Burst();
    }

    // Only the owner creates them: the effect prefabs are networked objects.
    private void Burst()
    {
        ArrivalSound.Mark(nview.GetZDO());
        foreach (string name in burstEffects)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab != null)
            {
                Instantiate(prefab, transform.position, Quaternion.identity);
            }
        }
    }

    // Rising and burrowing only move the model, on every client from the same ZDO times.
    private void UpdateVisual()
    {
        if (visual == null)
        {
            return;
        }

        float rise = Mathf.SmoothStep(0f, 1f, Seconds(StartKey) / RiseSeconds);
        long retreat = nview.IsValid() ? nview.GetZDO().GetLong(RetreatKey) : 0L;
        float sink = retreat != 0L ? Mathf.SmoothStep(0f, 1f, Seconds(RetreatKey) / SinkSeconds) : 0f;
        Vector3 position = visual.localPosition;
        position.y = -depth * (1f - rise) - depth * sink;
        visual.localPosition = position;
    }

    private float Seconds(string key)
    {
        long ticks = nview.IsValid() ? nview.GetZDO().GetLong(key) : 0L;
        return ticks == 0L ? float.MaxValue : (float)System.TimeSpan.FromTicks(ZNet.instance.GetTime().Ticks - ticks).TotalSeconds;
    }
}
