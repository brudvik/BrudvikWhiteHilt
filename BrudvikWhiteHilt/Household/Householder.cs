using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// A member of the household: the game's <see cref="Humanoid"/> with what only the player has in the game, sitting and
/// lying down on a chair or a bed (an attach point, as the player's <c>AttachStart</c> does). Everything else (moving,
/// fighting, taking hits, the equipment display) is the humanoid's own. The owner of the network object runs it; the
/// other machines see the animation through the animator's synced bool and the position through the network movement.
/// </summary>
public class Householder : Humanoid, Interactable
{
    private static readonly List<Householder> all = new();

    private bool attached;
    private Transform attachPoint;
    private string attachAnimation;
    private Vector3 detachOffset;
    private HouseholdMember member;

    /// <summary>Every householder loaded on this machine.</summary>
    public static IReadOnlyList<Householder> All => all;

    /// <summary>Who the householder is: name, role, farm and gear.</summary>
    public HouseholdMember Member => member;

    /// <summary>The network view, for the household's own components.</summary>
    public ZNetView View => m_nview;

    /// <summary>The synced animator, for the household's own components.</summary>
    public ZSyncAnimation Animation => m_zanim;

    /// <inheritdoc/>
    public override string GetHoverText()
    {
        return member != null ? member.HoverText() : base.GetHoverText();
    }

    /// <inheritdoc/>
    public override string GetHoverName()
    {
        return member != null ? member.MemberName : base.GetHoverName();
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        return member != null && member.Interact(user, hold, alt);
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return member != null && member.UseItem(user, item);
    }

    /// <inheritdoc/>
    public override bool IsAttached()
    {
        return attached || base.IsAttached();
    }

    /// <inheritdoc/>
    public override bool InBed()
    {
        return m_nview.IsValid() && m_nview.GetZDO().GetBool(ZDOVars.s_inBed);
    }

    /// <inheritdoc/>
    public override void AttachStart(Transform point, GameObject colliderRoot, bool hideWeapons, bool isBed, bool onShip,
        string animation, Vector3 offset, Transform cameraPos = null)
    {
        if (attached || point == null || !m_nview.IsOwner())
        {
            return;
        }

        attached = true;
        attachPoint = point;
        attachAnimation = animation;
        detachOffset = offset;
        m_zanim.SetBool(animation, true);
        m_nview.GetZDO().Set(ZDOVars.s_inBed, isBed);
        if (hideWeapons)
        {
            HideHandItems();
        }

        HoldAttach();
    }

    /// <inheritdoc/>
    public override void AttachStop()
    {
        if (!attached || !m_nview.IsOwner())
        {
            return;
        }

        // A chair or bed that was pulled down has no point left to step off from; they stand where they are.
        if (attachPoint != null)
        {
            transform.position = attachPoint.TransformPoint(detachOffset);
        }

        attached = false;
        m_body.useGravity = true;
        m_zanim.SetBool(attachAnimation, false);
        m_nview.GetZDO().Set(ZDOVars.s_inBed, false);
        attachPoint = null;
    }

    /// <inheritdoc/>
    public override void CustomFixedUpdate(float fixedDeltaTime)
    {
        if (attached && m_nview.IsValid() && m_nview.IsOwner())
        {
            HoldAttach();
        }

        base.CustomFixedUpdate(fixedDeltaTime);
    }

    /// <summary>
    /// Householders learn no skills. The game raises skills only for the tamed and looks for a Tameable to do it,
    /// which a householder has not, and would warn of that on every swing.
    /// </summary>
    /// <param name="skill">The skill.</param>
    /// <param name="value">How much.</param>
    public override void RaiseSkill(Skills.SkillType skill, float value = 1f)
    {
    }

    /// <inheritdoc/>
    public override void Awake()
    {
        // A householder is tame, as the base Awake reads it when the network data has no say: the creature AI then
        // follows and keeps to its post instead of shying from the base's fires and wandering, and friend and foe are
        // judged as for a tamed wolf.
        m_tamed = true;
        base.Awake();
        member = GetComponent<HouseholdMember>();
        all.Add(this);
    }

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        all.Remove(this);
        base.OnDestroy();
    }

    // Keeps the body on the chair or the bed every physics step, as the player's UpdateAttach does: on the point,
    // turned as it is, without gravity. A chair or bed pulled down lets them stand up.
    private void HoldAttach()
    {
        if (attachPoint == null || !attachPoint.gameObject.activeInHierarchy)
        {
            AttachStop();
            return;
        }

        transform.position = attachPoint.position;
        transform.rotation = attachPoint.rotation;
        m_body.useGravity = false;
        m_body.linearVelocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;
    }
}
