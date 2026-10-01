using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// On a falling log from a tree felled with Domino Felling: the trees it lands on come down too.
/// </summary>
public class DominoLog : MonoBehaviour
{
    private const float ActiveSeconds = 8f;
    private const float MinSpeed = 1.5f;
    private const float ChopDamage = 100000f;

    private readonly HashSet<TreeBase> hit = new();
    private ZNetView nview;
    private Rigidbody body;
    private ZDOID attacker;
    private float until;

    /// <summary>
    /// Sets who felled the tree.
    /// </summary>
    /// <param name="attackerId">The player's ZDOID.</param>
    public void Setup(ZDOID attackerId)
    {
        attacker = attackerId;
        until = Time.time + ActiveSeconds;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        body = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time > until || nview == null || !nview.IsValid() || !nview.IsOwner() || body == null || body.linearVelocity.magnitude < MinSpeed)
        {
            return;
        }

        TreeBase tree = collision.collider.GetComponentInParent<TreeBase>();
        if (tree == null || !hit.Add(tree))
        {
            return;
        }

        Vector3 direction = body.linearVelocity;
        direction.y = 0f;
        HitData data = new()
        {
            m_point = collision.GetContact(0).point,
            m_dir = direction.normalized,
            m_toolTier = short.MaxValue,
            m_skill = Skills.SkillType.WoodCutting,
            m_skillRaiseAmount = 0f,
            m_itemLevel = Gathering.DominoMarker,
            m_attacker = attacker
        };
        data.m_damage.m_chop = ChopDamage;
        tree.Damage(data);
    }
}
