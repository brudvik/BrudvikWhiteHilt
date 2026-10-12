using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Makes the Nidhogg Spawn roar once, the first time it is alerted: everyone sees the <c>roar</c> clip, and the owner's
/// AI stands still for as long as it lasts (see the prefix in <c>MonsterPatches</c>). The roar sound is the alert sound
/// the game already plays at that moment.
/// </summary>
public class SpawnRoar : MonoBehaviour
{
    private const string RoarState = "roar";
    private const string MoveState = "Movement";
    private const float FallbackSeconds = 2.3f;

    // Hashed once; the ZDO holds the time of the roar, so a client that comes later does not play it again.
    private static readonly int roarKey = "whitehilt_roar".GetStableHashCode();
    private static readonly HashSet<SpawnRoar> roaring = new();

    private ZNetView view;
    private MonsterAI ai;
    private Animator animator;
    private float roarSeconds = FallbackSeconds;
    private long seen;
    private float animationEnds;
    private float aiEnds;

    /// <summary>How many Spawns are standing still in their roar on this machine; the AI patch looks at this first.</summary>
    public static int RoaringCount => roaring.Count;

    /// <summary>
    /// Whether the creature is roaring and its AI must not act.
    /// </summary>
    /// <param name="creature">The creature's AI.</param>
    /// <returns>True while it roars.</returns>
    public static bool IsRoaring(MonsterAI creature)
    {
        return creature.TryGetComponent(out SpawnRoar roar) && roaring.Contains(roar);
    }

    /// <summary>
    /// Whether the roar clip is playing, so the gait does not speed it up.
    /// </summary>
    /// <returns>True while the roar clip plays.</returns>
    public bool IsAnimating()
    {
        return animationEnds > 0f;
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        ai = GetComponent<MonsterAI>();
        animator = GetComponentInChildren<Animator>();
        AnimationClip clip = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.animationClips.FirstOrDefault(each => each.name == RoarState)
            : null;
        if (clip != null)
        {
            roarSeconds = clip.length;
        }
    }

    private void Start()
    {
        ZDO zdo = view != null ? view.GetZDO() : null;
        if (zdo == null)
        {
            enabled = false;
            return;
        }

        seen = zdo.GetLong(roarKey);
    }

    private void OnDestroy()
    {
        roaring.Remove(this);
    }

    private void Update()
    {
        ZDO zdo = view.GetZDO();
        if (zdo == null)
        {
            return;
        }

        // Every client plays the roar when the owner has written a new time.
        long stamp = zdo.GetLong(roarKey);
        if (stamp != seen)
        {
            seen = stamp;
            animationEnds = Time.time + roarSeconds;
            animator.CrossFadeInFixedTime(RoarState, 0.15f);
        }
        else if (animationEnds > 0f && Time.time >= animationEnds)
        {
            animationEnds = 0f;
            animator.CrossFadeInFixedTime(MoveState, 0.25f);
        }

        if (view.IsOwner())
        {
            UpdateOwner(zdo);
        }
    }

    // Only the owner decides when the roar starts and holds its AI still.
    private void UpdateOwner(ZDO zdo)
    {
        if (roaring.Contains(this))
        {
            if (Time.time >= aiEnds)
            {
                roaring.Remove(this);
            }

            return;
        }

        if (zdo.GetLong(roarKey) == 0L && ai.IsAlerted() && !ai.IsSleeping())
        {
            zdo.Set(roarKey, ZNet.instance.GetTime().Ticks);
            aiEnds = Time.time + roarSeconds;
            roaring.Add(this);
        }
    }
}
