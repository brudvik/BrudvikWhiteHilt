using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// A dead monster: plays its death animation, lies still a while, then sinks into the ground and is gone. A flyer's corpse
/// first falls from where it died to the ground.
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

    /// <summary>Whether the model falls from where the monster died to the ground.</summary>
    public bool Falls;

    /// <summary>Height of the model at the start of the fall, relative to its place on the prefab.</summary>
    public float FallStartOffset;

    /// <summary>Seconds into the death animation the fall begins.</summary>
    public float FallDelay;

    /// <summary>Seconds the fall takes.</summary>
    public float FallSeconds = 1f;

    private ZNetView nview;
    private float started;
    private float fallFrom;
    private float fallTo;
    private bool falling;

    private void Start()
    {
        nview = GetComponent<ZNetView>();
        started = Time.time;
        Animator animator = Visual != null ? Visual.GetComponentInChildren<Animator>() : null;
        if (animator != null)
        {
            animator.Play("die", 0, 0f);
        }

        if (Falls && Visual != null)
        {
            StartFall();
        }
    }

    // Lets the corpse fall to the ground, sink into it after a while, and be removed on its owner when its time is up.
    private void Update()
    {
        float age = Time.time - started;
        if (falling)
        {
            float progress = Mathf.SmoothStep(0f, 1f, (age - FallDelay) / Mathf.Max(0.01f, FallSeconds));
            SetHeight(Mathf.Lerp(fallFrom, fallTo, progress));
            falling = age < FallDelay + FallSeconds;
        }

        if (Visual != null && age > SinkAfter)
        {
            Visual.localPosition += Vector3.down * (SinkSpeed * Time.deltaTime);
        }

        if (age > Lifetime && nview != null && nview.IsValid() && nview.IsOwner())
        {
            nview.Destroy();
        }
    }

    // Every client works the fall out from the same spot, so the corpse needs no sync.
    private void StartFall()
    {
        Vector3 position = transform.position;
        float ground = position.y;
        ZoneSystem zones = ZoneSystem.instance;
        if (zones != null)
        {
            ground = zones.GetSolidHeight(position, out float solid) ? solid : zones.GetGroundHeight(position);
            ground = Mathf.Max(ground, zones.m_waterLevel);
        }

        float scale = Mathf.Max(0.01f, transform.lossyScale.y);
        fallFrom = Visual.localPosition.y + FallStartOffset;
        fallTo = (ground - position.y) / scale;
        falling = true;
        SetHeight(fallFrom);
    }

    private void SetHeight(float height)
    {
        Vector3 local = Visual.localPosition;
        local.y = height;
        Visual.localPosition = local;
    }
}
