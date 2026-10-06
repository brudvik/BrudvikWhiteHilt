using UnityEngine;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Animates the octopus: it jets along mantle first in short pulses while it swims, and floats with its arms down when still.
/// </summary>
public class OctopusSwim : MonoBehaviour
{
    private const float SwimSpeed = 0.5f;
    private const float PulseSeconds = 0.9f;
    private const float SwimPitch = 80f;

    private static readonly int forwardSpeed = Animator.StringToHash("forward_speed");

    /// <summary>The model, set on the prefab.</summary>
    public Transform Visual;

    private Animator animator;
    private Fish fish;
    private Vector3 lastPosition;
    private float speed;
    private float pitch;
    private float nextPulse;
    private bool swimming;

    private void Start()
    {
        Visual ??= transform.Find("octopus_visual");
        animator = Visual != null ? Visual.GetComponentInChildren<Animator>() : null;
        fish = GetComponent<Fish>();
        lastPosition = transform.position;
    }

    // Darts and tilts the octopus while it moves fast in water, from its measured speed, as a fish has no swim
    // animation of its own for this model.
    private void Update()
    {
        if (animator == null || Time.deltaTime <= 0f)
        {
            return;
        }

        Vector3 moved = transform.position - lastPosition;
        lastPosition = transform.position;
        speed = Mathf.Lerp(speed, moved.magnitude / Time.deltaTime, Time.deltaTime * 4f);
        bool inWater = fish == null || !fish.IsOutOfWater();
        bool swim = inWater && speed > SwimSpeed;

        if (swim && Time.time >= nextPulse)
        {
            animator.CrossFade("dart", 0.1f, 0, 0f);
            nextPulse = Time.time + PulseSeconds;
        }
        else if (!swim && swimming)
        {
            animator.CrossFade("Movement", 0.3f, 0);
        }

        swimming = swim;
        animator.SetFloat(forwardSpeed, inWater ? 0f : Mathf.Min(speed, 1.5f));
        pitch = Mathf.MoveTowards(pitch, swim ? SwimPitch : 0f, Time.deltaTime * 120f);
        Visual.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
