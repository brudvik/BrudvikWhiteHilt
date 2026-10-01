using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Sits on the Lindorm. It breaks out of the ground where it spawns, and burrows back down when it has lost its prey for a
/// while, or at dawn. While it is below ground it neither moves nor takes damage.
/// </summary>
public class LindormBurrow : MonoBehaviour
{
    /// <summary>Path of the model under the creature.</summary>
    public const string VisualPath = "Visual/lindorm_visual";

    private const string StartKey = "whitehilt_lindorm_start";
    private const string RetreatKey = "whitehilt_lindorm_retreat";
    private const float RiseSeconds = 2.2f;
    private const float SinkSeconds = 2.5f;
    private const float LostPreySeconds = 25f;
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

    /// <summary>Whether it is still rising or already burrowing away.</summary>
    public bool IsHidden => nview != null && nview.IsValid() && (Seconds(StartKey) < RiseSeconds * 0.7f || nview.GetZDO().GetLong(RetreatKey) != 0L);

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        visual = transform.Find(VisualPath);
        depth = 3.5f * MonsterSettings.LindormScale.Value;
        lastPrey = Time.time;
        instances.Add(this);
        if (nview.IsValid() && nview.IsOwner() && nview.GetZDO().GetLong(StartKey) == 0L)
        {
            nview.GetZDO().Set(StartKey, ZNet.instance.GetTime().Ticks);
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

        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_lindorm_rises");
        GameCamera.instance?.AddShake(transform.position, 40f, 1.5f, false);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

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

        bool dawn = MonsterSettings.LindormNightOnly.Value && !EnvMan.IsNight();
        if (Time.time - lastPrey > (dawn ? 5f : LostPreySeconds))
        {
            Retreat();
        }
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
