using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// A dead monster: plays its death animation, lies still a while, then sinks into the ground and is gone.
/// </summary>
public class MonsterCorpse : MonoBehaviour
{
    /// <summary>The model, set on the prefab.</summary>
    public Transform Visual;

    /// <summary>Seconds before it starts to sink.</summary>
    public float SinkAfter = 6f;

    /// <summary>Metres per second it sinks.</summary>
    public float SinkSpeed = 0.4f;

    /// <summary>Seconds before it is removed.</summary>
    public float Lifetime = 15f;

    private ZNetView nview;
    private float started;

    private void Start()
    {
        nview = GetComponent<ZNetView>();
        started = Time.time;
        Animator animator = Visual != null ? Visual.GetComponentInChildren<Animator>() : null;
        if (animator != null)
        {
            animator.Play("die", 0, 0f);
        }
    }

    private void Update()
    {
        float age = Time.time - started;
        if (Visual != null && age > SinkAfter)
        {
            Visual.localPosition += Vector3.down * (SinkSpeed * Time.deltaTime);
        }

        if (age > Lifetime && nview != null && nview.IsValid() && nview.IsOwner())
        {
            nview.Destroy();
        }
    }
}
