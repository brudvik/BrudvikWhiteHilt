using UnityEngine;

namespace BrudvikWhiteHilt.Crafting;

/// <summary>
/// Shows that something was taken from a chest: the lid opens, the chest glows amber and closes again.
/// Only the local player sees it.
/// </summary>
public class ContainerPulse : MonoBehaviour
{
    private const float Duration = 1.2f;
    private static readonly Color glow = new(1f, 0.72f, 0.25f);

    private Container container;
    private float elapsed;

    /// <summary>
    /// Starts or restarts the pulse on a chest.
    /// </summary>
    /// <param name="container">The chest something was taken from.</param>
    public static void Play(Container container)
    {
        ContainerPulse pulse = container.GetComponent<ContainerPulse>();
        if (pulse == null)
        {
            pulse = container.gameObject.AddComponent<ContainerPulse>();
        }

        pulse.container = container;
        pulse.Restart();
    }

    private void Restart()
    {
        if (elapsed <= 0f || elapsed >= Duration)
        {
            container.m_openEffects.Create(container.transform.position, container.transform.rotation);
        }

        elapsed = 0.0001f;
        enabled = true;
        SetLid(open: true);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= Duration)
        {
            MaterialMan.instance.ResetValue(gameObject, ShaderProps._EmissionColor);
            SetLid(open: container.m_nview.IsValid() && container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1);
            container.m_closeEffects.Create(container.transform.position, container.transform.rotation);
            enabled = false;
            return;
        }

        float strength = Mathf.Sin(elapsed / Duration * Mathf.PI) * 0.6f;
        MaterialMan.instance.SetValue(gameObject, ShaderProps._EmissionColor, glow * strength);
    }

    private void SetLid(bool open)
    {
        if (container.m_open != null)
        {
            container.m_open.SetActive(open);
        }

        if (container.m_closed != null)
        {
            container.m_closed.SetActive(!open);
        }
    }
}
