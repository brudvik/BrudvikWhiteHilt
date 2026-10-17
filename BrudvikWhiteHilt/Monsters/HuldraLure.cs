using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Sits on the Huldra. While she lures, her AI stands still and faces the nearest player (see the prefix in
/// <c>MonsterPatches</c>); now and then she waves or blows a kiss, which every client sees. A player who stands still
/// within reach of her song is drawn slowly towards her, on their own machine. When a player comes too close, or she is
/// hurt, she turns her back and, a moment later, her owner puts her true form in her place.
/// </summary>
public class HuldraLure : MonoBehaviour
{
    private const string GestureKey = "whitehilt_huldra_gesture";
    private const string GestureKindKey = "whitehilt_huldra_gesture_kind";
    private const string TurnedKey = "whitehilt_huldra_turned";
    private const string MoveState = "Movement";
    private static readonly string[] gestures = { "wave", "kiss" };

    private const float NoticeRange = 40f;
    private const float GestureRange = 30f;
    private const float SongRange = 22f;
    private const float RevealRange = 3.5f;
    private const float PullSpeed = 0.8f;
    private const float StillSpeed = 0.3f;
    private const float TurnSeconds = 1.4f;
    private const float AnnounceRange = 25f;

    private static readonly HashSet<HuldraLure> luring = new();

    private ZNetView view;
    private Character character;
    private Animator animator;
    private long seenGesture;
    private long seenTurn;
    private float gestureEnds;
    private float nextGesture;
    private float nextCheck;
    private bool heardSong;

    /// <summary>How many Huldras hold their AI on this machine; the AI patch looks at this first.</summary>
    public static int LuringCount => luring.Count;

    /// <summary>
    /// Whether a creature is a Huldra luring or turning, whose AI must only stand and face its prey.
    /// </summary>
    /// <param name="creature">The creature's AI.</param>
    /// <param name="prey">The player she faces, or null.</param>
    /// <returns>True while she lures.</returns>
    public static bool IsLuring(MonsterAI creature, out Player prey)
    {
        prey = null;
        if (!creature.TryGetComponent(out HuldraLure lure) || !luring.Contains(lure))
        {
            return false;
        }

        // Turned, she keeps her back to them until her true form takes her place.
        prey = lure.Turned ? null : Player.GetClosestPlayer(lure.transform.position, NoticeRange);
        return true;
    }

    private bool Turned => view.IsValid() && view.GetZDO().GetLong(TurnedKey) != 0L;

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        animator = GetComponentInChildren<Animator>();
        nextGesture = Time.time + Random.Range(3f, 6f);
    }

    private void OnDestroy()
    {
        luring.Remove(this);
    }

    private void Update()
    {
        if (!view.IsValid())
        {
            return;
        }

        ZDO zdo = view.GetZDO();
        PlayGesture(zdo);
        ShowTurn(zdo);
        if (view.IsOwner())
        {
            UpdateOwner(zdo);
        }
        else
        {
            luring.Remove(this);
        }
    }

    private void FixedUpdate()
    {
        if (view.IsValid() && !Turned && !character.IsDead())
        {
            DrawLocalPlayer();
        }
    }

    // Every client plays a gesture when the owner writes a new one, and goes back to standing when it is over.
    private void PlayGesture(ZDO zdo)
    {
        long stamp = zdo.GetLong(GestureKey);
        if (stamp != seenGesture)
        {
            seenGesture = stamp;
            string state = gestures[Mathf.Clamp(zdo.GetInt(GestureKindKey), 0, gestures.Length - 1)];
            AnimationClip clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(each => each.name == state);
            gestureEnds = Time.time + (clip != null ? clip.length : 3f);
            animator.CrossFadeInFixedTime(state, 0.25f);
        }
        else if (gestureEnds > 0f && Time.time >= gestureEnds)
        {
            gestureEnds = 0f;
            animator.CrossFadeInFixedTime(MoveState, 0.3f);
        }
    }

    // Every client hears her change and is told, once.
    private void ShowTurn(ZDO zdo)
    {
        long turned = zdo.GetLong(TurnedKey);
        if (turned == 0L || turned == seenTurn)
        {
            return;
        }

        seenTurn = turned;
        if (gestureEnds > 0f)
        {
            gestureEnds = 0f;
            animator.CrossFadeInFixedTime(MoveState, 0.1f);
        }

        Player player = Player.m_localPlayer;
        if (player != null && Vector3.Distance(player.transform.position, transform.position) < AnnounceRange)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_huldra_turns");
        }

        if (!VisualHelper.IsHeadless)
        {
            GameObject holder = new("whitehilt_huldra_transform");
            holder.transform.position = transform.position;
            AudioSource source = holder.AddComponent<AudioSource>();
            WhiteHiltSound.Place(source, 6f, 40f);
            WhiteHiltSound.PlayFrom(source, "huldra_transform");
            Destroy(holder, 6f);
        }
    }

    private void UpdateOwner(ZDO zdo)
    {
        long turned = zdo.GetLong(TurnedKey);
        if (turned != 0L)
        {
            luring.Add(this);
            if ((ZNet.instance.GetTime() - new System.DateTime(turned)).TotalSeconds >= TurnSeconds)
            {
                BecomeTrueForm();
            }

            return;
        }

        if (character.IsDead())
        {
            luring.Remove(this);
            return;
        }

        luring.Add(this);
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + 0.25f;
        Player prey = Player.GetClosestPlayer(transform.position, NoticeRange);
        if (character.GetHealth() < character.GetMaxHealth() - 0.5f
            || (prey != null && Vector3.Distance(prey.transform.position, transform.position) < RevealRange))
        {
            Turn(zdo, prey);
            return;
        }

        if (prey != null && Time.time >= nextGesture && Vector3.Distance(prey.transform.position, transform.position) < GestureRange)
        {
            nextGesture = Time.time + Random.Range(7f, 12f);
            zdo.Set(GestureKindKey, Random.Range(0, gestures.Length));
            zdo.Set(GestureKey, ZNet.instance.GetTime().Ticks);
        }
    }

    // She turns her back on the one who came: the hollow, rotten back of a tree.
    private void Turn(ZDO zdo, Player prey)
    {
        if (prey != null)
        {
            Vector3 away = transform.position - prey.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(away);
            }
        }

        zdo.Set(TurnedKey, ZNet.instance.GetTime().Ticks);
    }

    // Puts her true form where she stood, facing the nearest player and hunting, with her stars, and removes her.
    private void BecomeTrueForm()
    {
        GameObject prefab = ZNetScene.instance.GetPrefab(WildMonsters.HuldraTrueForm.PrefabName);
        if (prefab != null)
        {
            Player prey = Player.GetClosestPlayer(transform.position, NoticeRange);
            Vector3 toPrey = prey != null ? prey.transform.position - transform.position : transform.forward;
            toPrey.y = 0f;
            GameObject root = Instantiate(prefab, transform.position,
                toPrey.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPrey) : transform.rotation);
            Character trueForm = root.GetComponent<Character>();
            trueForm.SetLevel(character.GetLevel());
            root.GetComponent<BaseAI>()?.SetHuntPlayer(true);
            if (prey != null)
            {
                root.GetComponent<MonsterAI>()?.SetTarget(prey);
            }
        }

        view.Destroy();
    }

    // A player standing still within her song drifts towards her, until they are close enough for her to turn.
    private void DrawLocalPlayer()
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead() || player.IsAttached() || player.InInterior())
        {
            return;
        }

        Vector3 toHer = transform.position - player.transform.position;
        float distance = toHer.magnitude;
        if (distance > SongRange || distance < RevealRange * 0.8f)
        {
            return;
        }

        if (!heardSong)
        {
            heardSong = true;
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_huldra_song");
        }

        Vector3 velocity = player.GetVelocity();
        velocity.y = 0f;
        if (velocity.magnitude > StillSpeed || player.InAttack() || player.IsBlocking())
        {
            return;
        }

        toHer.y = 0f;
        Rigidbody body = player.m_body;
        if (body != null)
        {
            body.MovePosition(body.position + toHer.normalized * (PullSpeed * Time.fixedDeltaTime));
        }
    }
}
