using UnityEngine;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// The dead Kraken: plays its death animation, then sinks and is gone.
/// </summary>
public class KrakenCorpse : MonoBehaviour
{
    private const float SinkAfter = 2.5f;
    private const float SinkSpeed = 1.2f;
    private const float Lifetime = 16f;

    /// <summary>The model, set on the prefab.</summary>
    public Transform Visual;

    private ZNetView nview;
    private float started;

    private void Start()
    {
        nview = GetComponent<ZNetView>();
        started = Time.time;
        Visual ??= transform.Find("kraken_visual");
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
