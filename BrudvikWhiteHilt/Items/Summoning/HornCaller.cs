using BrudvikWhiteHilt.Helpers;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace BrudvikWhiteHilt.Items.Summoning;

/// <summary>
/// Synchronizes a horn call and poses the arms and horn at the mouth. Two horns are blown this way: the Horn of the
/// Deep, which calls a beast, and the Gate Horn, a shorter and brighter call that opens or closes a gate.
/// </summary>
[DefaultExecutionOrder(200)]
public class HornCaller : MonoBehaviour
{
    private static AudioClip clip;
    private static AudioMixerGroup mixer;
    private Player player;
    private AudioSource audio;
    private long observedCall;
    private long requestedCall;
    private Transform horn;
    private Vector3 hornPosition;
    private Quaternion hornRotation;
    private bool posed;

    /// <summary>Loads the original horn tone and the game's sound mixer.</summary>
    public static void LoadSound()
    {
        clip = ForagingAssets.LoadAudio("summoninghorn");
        var effects = PrefabManager.Instance.GetPrefab("Wolf")?.GetComponent<MonsterAI>()?.m_alertedEffects.m_effectPrefabs;
        AudioSource template = effects?.Select(effect => effect.m_prefab?.GetComponentInChildren<AudioSource>(true)).FirstOrDefault(source => source != null);
        mixer = template?.outputAudioMixerGroup;
    }

    /// <summary>Starts a call if the local player is holding the horn.</summary>
    /// <returns>Whether the attack was handled as a horn call.</returns>
    public bool Begin()
    {
        if (!HoldingHorn())
            return false;
        if (player != Player.m_localPlayer || (!HoldingGateHorn() && !SummoningHornService.Enabled.Value) || player.IsDead()
            || !player.CanMove() || player.InAttack() || player.InDodge() || player.IsStaggering() || player.IsKnockedBack()
            || player.InMinorAction() || player.IsSwimming() || player.IsAttached() || player.IsAttachedToShip() || player.InInterior())
            return true;
        if (Elapsed(player.m_nview.GetZDO().GetLong(SummoningHornService.CallKey)) < Duration())
            return true;
        long previous = player.m_nview.GetZDO().GetLong(SummoningHornService.CallKey);
        if (previous != 0 && previous != requestedCall)
            return true;
        player.m_nview.GetZDO().Set(SummoningHornService.CallKey, ZNet.instance.GetTime().Ticks);
        ZDOMan.instance.ForceSendZDO(player.GetZDOID());
        return true;
    }

    private void Awake() => player = GetComponent<Player>();

    private bool HoldingHorn() => HoldingGateHorn() || HoldingSummoningHorn();

    private bool HoldingGateHorn() => player.m_nview != null && player.m_nview.IsValid()
        && player.m_nview.GetZDO().GetInt(ZDOVars.s_rightItem) == Pieces.Defenses.GateControl.GateHorn.ItemName.GetStableHashCode();

    // The gate horn is a short call; the summoning horn is blown until the beast answers.
    private float Duration() => HoldingGateHorn() ? Pieces.Defenses.GateControl.GateMechanisms.HornSeconds.Value : SummoningHornService.BlowSeconds.Value;

    private bool HoldingSummoningHorn() => player.m_nview != null && player.m_nview.IsValid()
        && player.m_nview.GetZDO().GetInt(ZDOVars.s_rightItem) == SummoningHornService.HornName.GetStableHashCode();

    private static float Elapsed(long start) => start == 0 || ZNet.instance == null ? float.PositiveInfinity
        : (float)((ZNet.instance.GetTime().Ticks - start) / (double)TimeSpan.TicksPerSecond);

    // Runs a horn call from the network data on the caller's player: plays its sound on every machine, and on the
    // caller's own machine cancels the call when the player moves, fights or puts the horn away, or asks the server for
    // the summons once it has been blown long enough.
    private void Update()
    {
        if (player.m_nview == null || !player.m_nview.IsValid())
            return;
        ZDO zdo = player.m_nview.GetZDO();
        long start = zdo.GetLong(SummoningHornService.CallKey);
        float elapsed = Elapsed(start);
        float duration = Duration();
        bool gateHorn = HoldingGateHorn();
        bool active = start != 0 && elapsed >= 0f && elapsed < duration && HoldingHorn() && !player.IsDead();
        if (player == Player.m_localPlayer && start != 0 && start != requestedCall)
        {
            if (!HoldingHorn() || player.IsDead() || player.IsSwimming() || player.m_moveDir != Vector3.zero
                || !player.CanMove() || player.InAttack() || player.InDodge() || player.IsStaggering() || player.IsKnockedBack()
                || player.InMinorAction() || player.IsAttached() || player.IsAttachedToShip() || (!gateHorn && !SummoningHornService.Enabled.Value))
            {
                zdo.Set(SummoningHornService.CallKey, 0L);
                ZDOMan.instance.ForceSendZDO(player.GetZDOID());
                active = false;
            }
            else if (elapsed >= duration)
            {
                requestedCall = start;
                if (gateHorn)
                    Pieces.Defenses.GateControl.GateHorn.Sounded(player);
                else
                    SummoningHornService.Request();
            }
        }
        if (active && observedCall != start)
        {
            observedCall = start;
            if (!VisualHelper.IsHeadless && clip != null)
            {
                if (audio == null)
                {
                    GameObject sound = new("WhiteHiltHornSound");
                    sound.transform.SetParent(player.transform, false);
                    audio = sound.AddComponent<AudioSource>();
                    audio.playOnAwake = false;
                    audio.clip = clip;
                    audio.outputAudioMixerGroup = mixer;
                    audio.spatialBlend = 1f;
                    audio.rolloffMode = AudioRolloffMode.Linear;
                    audio.minDistance = 2f;
                }
                audio.maxDistance = gateHorn ? Pieces.Defenses.GateControl.GateMechanisms.HornRange.Value * 1.5f : SummoningHornService.SoundRange.Value;
                audio.pitch = gateHorn ? 1.35f : 1f;
                audio.time = Mathf.Min(elapsed, clip.length - 0.01f);
                audio.Play();
            }
        }
        if (audio != null)
        {
            audio.volume = Mathf.Clamp01(elapsed / 0.4f) * Mathf.Clamp01((duration - elapsed) / 0.5f);
            if (!active && audio.isPlaying)
                audio.Stop();
        }
    }

    // Poses the arms and the horn at the mouth while it is blown, blended in and out, after the animator has posed the
    // body. The horn is put back where it was when the call ends.
    private void LateUpdate()
    {
        if (VisualHelper.IsHeadless || player.m_nview == null || !player.m_nview.IsValid())
            return;
        float elapsed = Elapsed(player.m_nview.GetZDO().GetLong(SummoningHornService.CallKey));
        float duration = Duration();
        if (!HoldingHorn() || player.IsDead() || elapsed < 0f || elapsed >= duration)
        {
            RestoreHorn();
            return;
        }
        RestoreHorn();
        Transform held = player.m_visEquipment.m_rightItemInstance?.transform;
        if (held == null || player.m_head == null)
            return;
        if (horn != held)
        {
            RestoreHorn();
            horn = held;
            hornPosition = horn.localPosition;
            hornRotation = horn.localRotation;
        }
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(elapsed / 0.45f, (duration - elapsed) / 0.45f));
        Vector3 mouth = player.m_head.position + player.transform.forward * 0.12f - player.transform.up * 0.06f;
        Transform hand = player.m_visEquipment.m_rightHand;
        Vector3 tip = new(0.30f, 0.34f, -0.28f);
        Quaternion rotation = player.transform.rotation * Quaternion.FromToRotation(new Vector3(-0.15f, -0.4f, 0.39f), new Vector3(0f, -0.2f, 1f));
        Vector3 position = mouth - rotation * tip;
        PoseArm(hand, position + rotation * new Vector3(0.15f, -0.04f, -0.07f), player.transform.right, blend);
        PoseArm(player.m_visEquipment.m_leftHand, position + rotation * new Vector3(0.13f, -0.1f, 0.045f), -player.transform.right, blend);
        horn.rotation = Quaternion.Slerp(hand.rotation * hornRotation, rotation, blend);
        horn.position = Vector3.Lerp(hand.TransformPoint(hornPosition), position, blend);
        posed = true;
    }

    /// <summary>Solves a two-bone arm toward a target while preserving bone lengths.</summary>
    /// <param name="hand">The hand bone.</param>
    /// <param name="target">Desired hand position.</param>
    /// <param name="elbowDirection">Direction in which the elbow should bend.</param>
    /// <param name="blend">Pose blend.</param>
    public static void PoseArm(Transform hand, Vector3 target, Vector3 elbowDirection, float blend)
    {
        if (hand?.parent?.parent == null)
            return;
        Transform forearm = hand.parent;
        Transform upper = forearm.parent;
        float upperLength = Vector3.Distance(upper.position, forearm.position);
        float lowerLength = Vector3.Distance(forearm.position, hand.position);
        Vector3 delta = target - upper.position;
        float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upperLength - lowerLength) + 0.001f, upperLength + lowerLength - 0.001f);
        Vector3 direction = delta.normalized;
        Vector3 bend = Vector3.ProjectOnPlane(elbowDirection - Vector3.up, direction).normalized;
        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
        Vector3 elbow = upper.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
        upper.rotation = Quaternion.Slerp(upper.rotation, Quaternion.FromToRotation(forearm.position - upper.position, elbow - upper.position) * upper.rotation, blend);
        forearm.rotation = Quaternion.Slerp(forearm.rotation, Quaternion.FromToRotation(hand.position - forearm.position, target - forearm.position) * forearm.rotation, blend);
    }

    private void RestoreHorn()
    {
        if (posed && horn != null)
        {
            horn.localPosition = hornPosition;
            horn.localRotation = hornRotation;
        }
        posed = false;
    }
}

/// <summary>Installs the horn service and intercepts attacks with the equipped horn.</summary>
[HarmonyPatch]
public static class SummoningHornPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void StartService(Game __instance) => __instance.gameObject.AddComponent<SummoningHornService>();

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    [HarmonyPostfix]
    private static void AddCaller(Player __instance) => __instance.gameObject.AddComponent<HornCaller>();

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    [HarmonyPrefix]
    private static bool Blow(Humanoid __instance, ref bool __result)
    {
        if (__instance != Player.m_localPlayer || !__instance.TryGetComponent(out HornCaller caller) || !caller.Begin())
            return true;
        __result = true;
        return false;
    }
}