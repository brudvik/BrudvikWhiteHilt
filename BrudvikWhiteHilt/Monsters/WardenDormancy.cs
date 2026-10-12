using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Keeps the Yggdrasil Warden's animation in step with the game's own sleep: while <c>MonsterAI.m_sleeping</c> holds it
/// still (until a player comes within the wake-up range or it is hurt), every client shows the <c>dormant</c> clip, and
/// when it wakes it moves on to its idle. The game syncs the sleeping state to everyone; only the clip is ours, as the
/// model's controller has no transition for it.
/// </summary>
public class WardenDormancy : MonoBehaviour
{
    private const string DormantState = "dormant";
    private const string MoveState = "Movement";

    private MonsterAI ai;
    private Animator animator;
    private bool dormant;

    private void Awake()
    {
        ai = GetComponent<MonsterAI>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        bool sleeping = ai.IsSleeping();
        if (sleeping == dormant)
        {
            return;
        }

        dormant = sleeping;
        animator.CrossFadeInFixedTime(sleeping ? DormantState : MoveState, sleeping ? 0.05f : 0.4f);
    }
}
