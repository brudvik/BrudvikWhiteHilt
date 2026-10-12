using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

public sealed partial class DogCompanion
{
    /// <summary>
    /// Makes the dog bark on every client.
    /// </summary>
    public void Bark()
    {
        PlaySound("dogbark");
    }

    /// <summary>
    /// Makes the dog glad on every client: a happy sound and a wagging tail.
    /// </summary>
    public void Happy()
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(ZNetView.Everybody, HappyRpc);
        }
    }

    /// <summary>
    /// Plays one of the dog's sounds on every client.
    /// </summary>
    /// <param name="sound">Sound name, e.g. dogwhine.</param>
    public void PlaySound(string sound)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(ZNetView.Everybody, SoundRpc, sound);
        }
    }

    /// <summary>
    /// Adds bond experience, on the dog's owner.
    /// </summary>
    /// <param name="xp">Experience to add.</param>
    public void AddXp(float xp)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(XpRpc, xp);
        }
    }

    /// <summary>
    /// Puts a collar on the dog, on the dog's owner.
    /// </summary>
    /// <param name="collar">1 for leather, 2 for iron.</param>
    /// <returns>The collar the dog wore before, 0 for none.</returns>
    public int SetCollar(int collar)
    {
        int old = Zdo?.GetInt(collarKey) ?? 0;
        if (Zdo != null)
        {
            nview.InvokeRPC(CollarRpc, collar);
        }

        return old;
    }

    /// <summary>
    /// Moves the dog's home to a dog house, on the dog's owner.
    /// </summary>
    /// <param name="position">Position of the dog house.</param>
    public void SetHome(Vector3 position)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(HomeRpc, position);
        }
    }

    /// <summary>
    /// Calls the dog to follow the player, from the whistle. A far dog is brought over at once.
    /// </summary>
    /// <param name="player">The local player.</param>
    public void Call(Player player)
    {
        nview.ClaimOwnership();
        ai.SetFollowTarget(player.gameObject);
        Zdo.Set(followKey, player.GetPlayerID());
        rest.SetPose(RestPose.Pose.None);
        if (Vector3.Distance(transform.position, player.transform.position) > FarAway)
        {
            TeleportTo(player.transform.position - player.transform.forward * 2f);
        }

        Bark();
    }

    /// <summary>
    /// Sends the dog home, from the whistle. A far dog is brought home at once.
    /// </summary>
    public void SendHome()
    {
        nview.ClaimOwnership();
        ai.SetFollowTarget(null);
        Zdo.Set(followKey, 0L);
        Vector3 home = Home;
        ai.SetPatrolPoint(home);
        if (Vector3.Distance(transform.position, home) > FarAway)
        {
            TeleportTo(home + new Vector3(1.5f, 0f, 1.5f));
        }
    }

    /// <summary>
    /// Stops following and stays where it is, from the stay trick. Call on the owner.
    /// </summary>
    public void Stay()
    {
        ai.SetFollowTarget(null);
        Zdo.Set(followKey, 0L);
        ai.SetPatrolPoint(transform.position);
    }

    /// <summary>
    /// Moves the dog, also into an area that is not loaded; it appears there when the area loads.
    /// </summary>
    /// <param name="position">Where to.</param>
    public void TeleportTo(Vector3 position)
    {
        nview.ClaimOwnership();
        transform.position = position;
        if (character.m_body != null)
        {
            character.m_body.position = position;
            character.m_body.linearVelocity = Vector3.zero;
        }

        Zdo.SetPosition(position);
    }

    /// <summary>
    /// Remembers which player the dog follows, so it keeps following when it is loaded again. Call on the owner.
    /// </summary>
    public void RememberFollow()
    {
        Player master = ai.GetFollowTarget() != null ? ai.GetFollowTarget().GetComponent<Player>() : null;
        Zdo?.Set(followKey, master != null ? master.GetPlayerID() : 0L);
    }

    /// <summary>
    /// Walks the dog to its bed or house and lays it down there. Called from the AI tick on the owner.
    /// </summary>
    /// <param name="dt">Frame time.</param>
    public void UpdateGoal(float dt)
    {
        if (goal == null || rest.IsResting || !nview.IsOwner() || ai.m_targetCreature != null || ai.GetFollowTarget() != null || activities.IsBusy)
        {
            return;
        }

        goalWalkTime += dt;
        bool arrived = ai.MoveTo(dt, goal.Approach, 0.6f, run: false);
        if (!arrived && goalWalkTime < GiveUpWalkingSeconds)
        {
            return;
        }

        // Snap in: the dog cannot path into a house or basket, and after a long try it simply finds its way.
        Vector3 position = goal.Position;
        Quaternion rotation = goal.Rotation;
        transform.SetPositionAndRotation(position, rotation);
        Rigidbody body = character.m_body;
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector3.zero;
        }

        character.SetLookDir(rotation * Vector3.forward);
        ai.StopMoving();
        rest.SetPose(goalPose);
        Zdo.Set(goalRestKey, true);
    }

    private void RPC_Sound(long sender, string sound)
    {
        // Without its own bark in the bundle, the wolf's alert sound stands in.
        if (DogRegistry.PlaySound(sound, transform.position) <= 0f && sound == "dogbark")
        {
            ai.m_alertedEffects.Create(transform.position, transform.rotation, transform);
        }
    }

    private void RPC_Happy(long sender)
    {
        expression?.MakeHappy(HappySeconds);
        DogRegistry.PlaySound("doghappy", transform.position);
        care?.OnHappy();
    }

    private void RPC_Xp(long sender, float xp)
    {
        if (nview.IsOwner() && !IsPuppy)
        {
            Zdo.Set(xpKey, Zdo.GetFloat(xpKey) + xp);
            UpdateMaxHealth();
        }
    }

    private void RPC_Collar(long sender, int collar)
    {
        if (nview.IsOwner())
        {
            Zdo.Set(collarKey, collar);
        }
    }

    private void RPC_Home(long sender, Vector3 position)
    {
        if (nview.IsOwner())
        {
            Zdo.Set(homeKey, position);
        }
    }
}
