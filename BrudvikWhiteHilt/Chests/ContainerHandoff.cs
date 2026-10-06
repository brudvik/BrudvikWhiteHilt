using BrudvikWhiteHilt.Chests.Collection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests;

/// <summary>
/// Hands a closed container over to the player who wants to change it, with its authoritative inventory, before
/// anything is moved. The owner saves the container, sends its newest data and passes ownership on; the asking side
/// loads that data before it writes. Two players changing the same chest then never overwrite each other.
/// One handoff serves a whole network object, so a ship with a hold and a sea chest has one, for both.
/// </summary>
internal sealed class ContainerHandoff : MonoBehaviour
{
    private const string RequestRpc = "WhiteHilt_CollectionRequest";
    private const string ResponseRpc = "WhiteHilt_CollectionResponse";
    private const float DefaultRetrySeconds = 5f;

    // When the container was last handed over, in world time ticks, so others can leave it be for a while.
    private static readonly int HandedAtKey = "whitehilt_handoff_at".GetStableHashCode();

    private ZNetView view;
    private List<Container> containers;
    private long sequence;
    private bool pending;
    private long revision = -1;
    private float requestedAt;

    /// <summary>
    /// Adds the handoff to a container's network object, once. Call on every machine when the container wakes up.
    /// </summary>
    /// <param name="container">The container.</param>
    internal static void Attach(Container container)
    {
        ZNetView nview = container.m_nview;
        if (nview != null && nview.IsValid() && nview.GetComponent<ContainerHandoff>() == null)
        {
            nview.gameObject.AddComponent<ContainerHandoff>().Register(nview);
        }
    }

    /// <summary>
    /// Checks whether the local player owns a container with its newest contents and nobody has it open; otherwise
    /// asks its owner for it. Call again until it returns true.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="playerId">The local player.</param>
    /// <returns>True if the container may be changed now.</returns>
    internal static bool Ready(Container container, long playerId)
    {
        if (container == null || container.m_nview == null || !container.m_nview.IsValid())
        {
            return false;
        }

        ContainerHandoff handoff = container.m_nview.GetComponent<ContainerHandoff>();
        return handoff != null ? handoff.Ready(playerId) : container.m_nview.IsOwner() && !container.IsInUse();
    }

    /// <summary>
    /// Checks, without asking anyone, whether the local player holds a container: owns it with its newest contents,
    /// and nobody has it open. Only a held container may be changed straight away.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <returns>True if the container is held.</returns>
    internal static bool Held(Container container)
    {
        if (container == null || container.m_nview == null || !container.m_nview.IsValid())
        {
            return false;
        }

        ContainerHandoff handoff = container.m_nview.GetComponent<ContainerHandoff>();
        return handoff != null ? handoff.Held() : container.m_nview.IsOwner() && !container.IsInUse();
    }

    /// <summary>
    /// Checks whether someone has a container, or another container on the same network object, open, so it cannot be
    /// handed over now.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <returns>True if it is in use.</returns>
    internal static bool InUse(Container container)
    {
        if (container == null || container.m_nview == null || !container.m_nview.IsValid())
        {
            return false;
        }

        ContainerHandoff handoff = container.m_nview.GetComponent<ContainerHandoff>();
        return handoff != null ? handoff.InUse() : container.IsInUse() || container.m_nview.GetZDO().GetInt(ZDOVars.s_inUse) != 0;
    }

    /// <summary>
    /// Checks whether a container was handed over to another player less than <paramref name="seconds"/> ago, so a
    /// player who only gets ready to use it leaves it with them for now.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="seconds">How long a handed-over container is left with its new owner.</param>
    /// <returns>True if another player has had it for a shorter time.</returns>
    internal static bool KeptByOther(Container container, float seconds)
    {
        if (seconds <= 0f || container == null || container.m_nview == null || !container.m_nview.IsValid()
            || container.m_nview.IsOwner() || ZNet.instance == null)
        {
            return false;
        }

        long handedAt = container.m_nview.GetZDO().GetLong(HandedAtKey);
        return handedAt > 0 && (ZNet.instance.GetTime() - new System.DateTime(handedAt)).TotalSeconds < seconds;
    }

    private void Register(ZNetView nview)
    {
        view = nview;
        view.Register<long, long>(RequestRpc, Request);
        view.Register<long, ZPackage>(ResponseRpc, Response);
    }

    // Looked up once: the build menu asks whether a chest is held for every piece it shows.
    private List<Container> Containers()
    {
        if (containers == null || containers.Any(container => container == null))
        {
            containers = view.GetComponentsInChildren<Container>(true).Where(container => container.m_nview == view).ToList();
        }

        return containers;
    }

    // The game hands a container to whoever opens it, so on the owner the open flag is stale when none of its
    // containers is open here: left behind by a player who logged out with it open. It is cleared, or the container
    // could never be handed over again.
    private bool InUse()
    {
        if (Containers().Any(container => container.IsInUse()))
        {
            return true;
        }

        if (view.GetZDO().GetInt(ZDOVars.s_inUse) == 0)
        {
            return false;
        }

        if (!view.IsOwner())
        {
            return true;
        }

        view.GetZDO().Set(ZDOVars.s_inUse, 0);
        return false;
    }

    private bool Held()
    {
        return view != null && view.IsValid() && !InUse() && Settle() && view.IsOwner();
    }

    // Loads the contents once the handed-over data has arrived. False while still waiting for it.
    private bool Settle()
    {
        if (!pending)
        {
            return true;
        }

        if (revision < 0 || !view.IsOwner() || view.GetZDO().DataRevision < revision)
        {
            return false;
        }

        foreach (Container container in Containers())
        {
            container.Load();
        }

        pending = false;
        revision = -1;
        return true;
    }

    // Whether this machine owns the container and may change it. If not, asks the owner for it and waits for the newest
    // data to arrive; asks again if no answer comes.
    private bool Ready(long playerId)
    {
        if (view == null || !view.IsValid() || InUse())
        {
            return false;
        }

        // Nobody to ask: a container without an owner is simply taken, like vanilla does with objects nobody owns.
        if (!view.GetZDO().HasOwner())
        {
            view.ClaimOwnership();
            pending = false;
            revision = -1;
        }

        if (pending)
        {
            if (!Settle())
            {
                float retry = CollectionSettings.OwnershipRetry?.Value ?? DefaultRetrySeconds;
                if (!view.IsOwner() && Time.time - requestedAt >= retry)
                {
                    sequence++;
                    requestedAt = Time.time;
                    revision = -1;
                    view.InvokeRPC(RequestRpc, playerId, sequence);
                }

                return false;
            }
        }

        if (view.IsOwner())
        {
            return true;
        }

        pending = true;
        sequence++;
        requestedAt = Time.time;
        view.InvokeRPC(RequestRpc, playerId, sequence);
        return false;
    }

    // On the owner: hands the container over if nobody is using it and the player may open it. The newest data is saved
    // and sent first, so the new owner does not work on an older copy.
    private void Request(long sender, long playerId, long request)
    {
        if (!view.IsOwner())
        {
            return;
        }

        List<Container> all = Containers();
        ZPackage data = new();
        bool granted = !InUse() && all.All(container => container.CheckAccess(playerId));
        data.Write(granted);
        if (granted)
        {
            foreach (Container container in all)
            {
                container.Load();
                container.Save();
            }

            view.GetZDO().Set(HandedAtKey, ZNet.instance.GetTime().Ticks);
            data.Write((long)view.GetZDO().DataRevision);
            ZDOMan.instance.ForceSendZDO(sender, view.GetZDO().m_uid);
            view.GetZDO().SetOwner(sender);
        }

        view.InvokeRPC(sender, ResponseRpc, request, data);
    }

    private void Response(long sender, long request, ZPackage data)
    {
        if (!pending || request != sequence)
        {
            return;
        }

        if (data.ReadBool())
        {
            revision = data.ReadLong();
        }
        else
        {
            pending = false;
        }
    }
}
