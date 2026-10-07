using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.Styles;

/// <summary>
/// Gives a player the Rune Sword's own cuts while the sword is in their right hand: the player's animator gets an
/// override controller in which the dual knives' three clips are the Rune Sword's (made by
/// AssetSource/Unity/BuildAttackClips.cs), so the dual knives' triggers play them. Put back when the sword leaves the
/// hand.
/// </summary>
/// <remarks>
/// Runs on every client for every player, keyed on the right-hand item the game already syncs, so the others see the
/// cuts too. Swapping a controller resets the animator, so the parameters and the state of each layer are carried
/// over. Nothing on a dedicated server, which draws nothing.
/// </remarks>
public class RuneSwordMotion : MonoBehaviour
{
    private const string RuneSwordPrefab = "WhiteHiltRuneSword";

    // The dual knives' combo clips, in chain order, and the Rune Sword's clips that take their place.
    private static readonly string[] ReplacedClips = { "Knife Attack Combo (1)", "Knife Attack Combo (2)", "Knife Attack Combo (3)" };
    private static readonly string[] RuneClipNames = { "runesword_cut0", "runesword_cut1", "runesword_whirl" };

    private static readonly int RuneSwordHash = RuneSwordPrefab.GetStableHashCode();
    private static AnimationClip[] runeClips;
    private static bool clipsLoaded;

    private VisEquipment equipment;
    private Animator animator;
    private RuntimeAnimatorController original;
    private AnimatorOverrideController runeController;
    private bool active;
    private bool failed;

    /// <summary>
    /// Adds the component to a player, unless it has one or the game draws nothing.
    /// </summary>
    /// <param name="player">The player.</param>
    public static void Attach(Player player)
    {
        if (VisualHelper.IsHeadless || player.GetComponent<RuneSwordMotion>() != null)
        {
            return;
        }

        player.gameObject.AddComponent<RuneSwordMotion>();
    }

    /// <summary>
    /// Whether a character's animator plays the Rune Sword's cuts under the dual knives' triggers right now.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>True while the Rune Sword's clips are in place.</returns>
    public static bool IsActive(Humanoid character)
    {
        return character != null && character.TryGetComponent(out RuneSwordMotion motion) && motion.active;
    }

    private void Awake()
    {
        equipment = GetComponent<VisEquipment>();
        animator = GetComponent<Character>()?.m_animator;
    }

    private void Update()
    {
        if (failed || equipment == null || animator == null)
        {
            return;
        }

        bool wanted = equipment.m_currentRightItemHash == RuneSwordHash && Clips() != null;
        if (wanted != active)
        {
            Switch(wanted);
        }
    }

    private void OnDestroy()
    {
        if (runeController != null)
        {
            Destroy(runeController);
        }
    }

    // Puts the override controller in or takes it out. Any failure leaves the vanilla controller and turns the
    // component off for this player, so the sword then simply swings in its other styles.
    private void Switch(bool on)
    {
        try
        {
            if (on)
            {
                original = animator.runtimeAnimatorController;
                runeController ??= BuildController(original);
                if (runeController == null)
                {
                    failed = true;
                    return;
                }
            }

            Swap(on ? runeController : original);
            active = on;
        }
        catch (Exception ex)
        {
            failed = true;
            active = false;
            Jotunn.Logger.LogWarning($"Rune Sword cuts turned off for {name}: {ex.Message}");
        }
    }

    private static AnimatorOverrideController BuildController(RuntimeAnimatorController baseController)
    {
        AnimatorOverrideController controller = new(baseController) { name = baseController.name + "_runesword" };
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new(controller.overridesCount);
        controller.GetOverrides(overrides);
        int replaced = 0;
        for (int i = 0; i < overrides.Count; i++)
        {
            int index = Array.IndexOf(ReplacedClips, overrides[i].Key.name);
            if (index >= 0)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, runeClips[index]);
                replaced++;
            }
        }

        if (replaced != ReplacedClips.Length)
        {
            Jotunn.Logger.LogWarning($"Rune Sword cuts: found {replaced} of the {ReplacedClips.Length} dual knives clips; keeping the vanilla animations.");
            Destroy(controller);
            return null;
        }

        controller.ApplyOverrides(overrides);
        return controller;
    }

    // A new controller restarts the animator, which would drop the stance, the movement and an equip animation half
    // way; the values and states from before are put back.
    private void Swap(RuntimeAnimatorController controller)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        object[] values = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            values[i] = parameters[i].type switch
            {
                AnimatorControllerParameterType.Float => animator.GetFloat(parameters[i].nameHash),
                AnimatorControllerParameterType.Int => animator.GetInteger(parameters[i].nameHash),
                AnimatorControllerParameterType.Bool => animator.GetBool(parameters[i].nameHash),
                _ => null
            };
        }

        int layers = animator.layerCount;
        int[] states = new int[layers];
        float[] times = new float[layers];
        for (int layer = 0; layer < layers; layer++)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
            states[layer] = state.fullPathHash;
            times[layer] = state.normalizedTime;
        }

        float speed = animator.speed;
        animator.runtimeAnimatorController = controller;
        animator.speed = speed;
        for (int i = 0; i < parameters.Length; i++)
        {
            switch (values[i])
            {
                case float f:
                    animator.SetFloat(parameters[i].nameHash, f);
                    break;
                case int n:
                    animator.SetInteger(parameters[i].nameHash, n);
                    break;
                case bool b:
                    animator.SetBool(parameters[i].nameHash, b);
                    break;
            }
        }

        for (int layer = 0; layer < Math.Min(layers, animator.layerCount); layer++)
        {
            animator.Play(states[layer], layer, times[layer]);
        }
    }

    // Loaded once for all players; null when the bundle lacks them, which leaves the Rune Sword its other styles.
    private static AnimationClip[] Clips()
    {
        if (!clipsLoaded)
        {
            clipsLoaded = true;
            try
            {
                runeClips = Array.ConvertAll(RuneClipNames, ForagingAssets.LoadAnimation);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Rune Sword cuts not available: {ex.Message}");
                runeClips = null;
            }
        }

        return runeClips;
    }
}
