using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// The darkness seen, on the local player's machine: the place's ambient light and fog fade to black, and its own
/// lights (wall torches, braziers, glowing things) dim and go out with their flames. The players' own lights stay: what
/// they carry, and the fires they built, though what they carry reaches less far the deeper the dark has crept in. A
/// guttering torch goes out for a moment, and a circlet flickers in the black. On the <see cref="Game"/> object.
/// </summary>
public class DarknessLights : MonoBehaviour
{
    private const float Reach = 50f;
    private const float RefreshSeconds = 1f;
    private const float FlamesOut = 0.85f;
    private const float AmbientLeft = 0.03f;
    private const float ShortestReach = 0.2f;

    private readonly Dictionary<Light, Dimmed> dimmed = new();
    private readonly List<Light> guttered = new();
    private readonly Dictionary<LightFlicker, float> circlet = new();
    private readonly Dictionary<Light, float> carried = new();
    private readonly Dictionary<GameObject, Light[]> lightsOf = new();
    private readonly HashSet<Light> ours = new();
    private readonly List<GameObject> goneItems = new();
    private float nextRefresh;
    private float nextCircletFlicker;

    /// <summary>
    /// Fades the ambient light, the fog's colour and the sun the game has just set. Called after <see cref="EnvMan.SetEnv"/>.
    /// </summary>
    public static void ApplyEnvironment()
    {
        float level = DarknessClient.Level;
        if (level <= 0.001f)
        {
            return;
        }

        float left = Mathf.Lerp(1f, AmbientLeft, level);
        RenderSettings.ambientLight *= left;
        RenderSettings.fogColor *= left;
        if (EnvMan.instance != null && EnvMan.instance.m_dirLight != null)
        {
            EnvMan.instance.m_dirLight.intensity *= left;
        }
    }

    // After the lights' own flicker has set them this frame.
    private void LateUpdate()
    {
        float level = DarknessClient.Level;
        ShrinkCarried(level);
        if (level <= 0.001f && dimmed.Count == 0)
        {
            RestoreHeld();
            return;
        }

        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + RefreshSeconds;
            Prune();
            Gather();
        }

        foreach (KeyValuePair<Light, Dimmed> pair in dimmed)
        {
            pair.Value.Apply(pair.Key, level);
        }

        if (level <= 0.001f)
        {
            foreach (KeyValuePair<Light, Dimmed> pair in dimmed)
            {
                pair.Value.Restore(pair.Key);
            }

            dimmed.Clear();
        }

        UpdateHeld(level);
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<Light, Dimmed> pair in dimmed)
        {
            pair.Value.Restore(pair.Key);
        }

        dimmed.Clear();
        RestoreHeld();
        ShrinkCarried(0f);
    }

    // What every player carries reaches less far in the dark, down to a fifth once the dark has crept in fully: a
    // torch lights a step or two around you.
    private void ShrinkCarried(float level)
    {
        float share = Mathf.Lerp(1f, Mathf.Lerp(1f, ShortestReach, DarknessClient.Creep), level);
        if (share >= 0.999f)
        {
            if (carried.Count > 0)
            {
                foreach (KeyValuePair<Light, float> pair in carried)
                {
                    if (pair.Key != null)
                    {
                        pair.Key.range = pair.Value;
                    }
                }

                carried.Clear();
            }

            lightsOf.Clear();
            return;
        }

        foreach (Player player in Player.GetAllPlayers())
        {
            VisEquipment equipment = player != null ? player.m_visEquipment : null;
            if (equipment == null)
            {
                continue;
            }

            Shrink(equipment.m_rightItemInstance, share);
            Shrink(equipment.m_leftItemInstance, share);
            Shrink(equipment.m_helmetItemInstance, share);
        }
    }

    private void Shrink(GameObject held, float share)
    {
        if (held == null)
        {
            return;
        }

        // An item shown in a hand is made anew each time it is taken out, so its lights are found once per instance.
        if (!lightsOf.TryGetValue(held, out Light[] lights))
        {
            lights = held.GetComponentsInChildren<Light>(true);
            lightsOf[held] = lights;
        }

        foreach (Light light in lights)
        {
            if (light == null)
            {
                continue;
            }

            if (!carried.TryGetValue(light, out float range))
            {
                range = light.range;
                carried[light] = range;
            }

            light.range = range * share;
        }
    }

    // Forgets items put away and lights gone with them, once a second.
    private void Prune()
    {
        goneItems.Clear();
        foreach (GameObject item in lightsOf.Keys)
        {
            if (item == null)
            {
                goneItems.Add(item);
            }
        }

        foreach (GameObject item in goneItems)
        {
            lightsOf.Remove(item);
        }

        ours.RemoveWhere(light => light == null);
    }

    // The place's own lights close to the player; not the players' lights, the fires they built or the sun.
    private void Gather()
    {
        Player player = Player.m_localPlayer;
        if (player == null || DarknessClient.PlaceKey == null)
        {
            return;
        }

        // A light found to be the players' own is remembered, so its parents are not searched again each second.
        Vector3 centre = player.transform.position;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light == null || dimmed.ContainsKey(light) || ours.Contains(light) || light.type == LightType.Directional
                || (light.transform.position - centre).sqrMagnitude > Reach * Reach)
            {
                continue;
            }

            if (IsOurs(light))
            {
                ours.Add(light);
                continue;
            }

            dimmed[light] = new Dimmed(light);
        }
    }

    private static bool IsOurs(Light light)
    {
        if (light.GetComponentInParent<Player>() != null || light.GetComponentInParent<DarknessKeep>() != null)
        {
            return true;
        }

        Piece piece = light.GetComponentInParent<Piece>();
        return piece != null && piece.IsPlacedByPlayer();
    }

    // The local player's torch out while it gutters; their circlet flickering in the black.
    private void UpdateHeld(float level)
    {
        Player player = Player.m_localPlayer;
        VisEquipment equipment = player != null ? player.m_visEquipment : null;
        if (equipment == null)
        {
            return;
        }

        bool guttering = Time.time < DarknessClient.GutterUntil;
        if (guttering && guttered.Count == 0)
        {
            foreach (GameObject held in new[] { equipment.m_rightItemInstance, equipment.m_leftItemInstance })
            {
                if (held == null)
                {
                    continue;
                }

                foreach (Light light in held.GetComponentsInChildren<Light>())
                {
                    if (light.enabled)
                    {
                        light.enabled = false;
                        guttered.Add(light);
                    }
                }
            }
        }
        else if (!guttering && guttered.Count > 0)
        {
            RestoreGuttered();
        }

        bool flicker = level >= 0.99f && equipment.m_helmetItemInstance != null;
        if (!flicker)
        {
            RestoreCirclet();
            return;
        }

        if (Time.time < nextCircletFlicker)
        {
            return;
        }

        nextCircletFlicker = Time.time + Random.Range(0.05f, 0.25f);
        foreach (LightFlicker light in equipment.m_helmetItemInstance.GetComponentsInChildren<LightFlicker>())
        {
            if (!circlet.ContainsKey(light))
            {
                circlet[light] = light.m_multiplier;
            }

            light.m_multiplier = circlet[light] * (Random.value < 0.15f ? Random.Range(0.1f, 0.4f) : Random.Range(0.75f, 1f));
        }
    }

    private void RestoreHeld()
    {
        RestoreGuttered();
        RestoreCirclet();
    }

    private void RestoreGuttered()
    {
        foreach (Light light in guttered)
        {
            if (light != null)
            {
                light.enabled = true;
            }
        }

        guttered.Clear();
    }

    private void RestoreCirclet()
    {
        foreach (KeyValuePair<LightFlicker, float> pair in circlet)
        {
            if (pair.Key != null)
            {
                pair.Key.m_multiplier = pair.Value;
            }
        }

        circlet.Clear();
    }

    // A light of the place as it was, and its flames, so it can be dimmed and put back.
    private sealed class Dimmed
    {
        private readonly LightFlicker flicker;
        private readonly float original;
        private readonly List<Renderer> flames = new();
        private bool flamesOut;

        public Dimmed(Light light)
        {
            flicker = light.GetComponent<LightFlicker>();
            original = flicker != null ? flicker.m_multiplier : light.intensity;
            Transform holder = light.transform.parent != null ? light.transform.parent : light.transform;
            foreach (ParticleSystemRenderer flame in holder.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                if (flame.enabled)
                {
                    flames.Add(flame);
                }
            }
        }

        public void Apply(Light light, float level)
        {
            if (light == null)
            {
                return;
            }

            if (flicker != null)
            {
                flicker.m_multiplier = original * (1f - level);
            }
            else
            {
                light.intensity = original * (1f - level);
            }

            bool outNow = level >= FlamesOut;
            if (outNow != flamesOut)
            {
                flamesOut = outNow;
                foreach (Renderer flame in flames)
                {
                    if (flame != null)
                    {
                        flame.enabled = !outNow;
                    }
                }
            }
        }

        public void Restore(Light light)
        {
            Apply(light, 0f);
        }
    }
}

/// <summary>
/// Marks something whose lights the darkness leaves alone, such as the eyes in the dark.
/// </summary>
public class DarknessKeep : MonoBehaviour
{
}
