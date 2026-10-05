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

    private ZNetView view;
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

    private void Register(ZNetView nview)
    {
        view = nview;
        view.Register<long, long>(RequestRpc, Request);
        view.Register<long, ZPackage>(ResponseRpc, Response);
    }

    private IEnumerable<Container> Containers()
    {
        return view.GetComponentsInChildren<Container>(true).Where(container => container.m_nview == view);
    }

    private bool InUse()
    {
        return view.GetZDO().GetInt(ZDOVars.s_inUse) != 0 || Containers().Any(container => container.IsInUse());
    }

    private bool Ready(long playerId)
    {
        if (view == null || !view.IsValid() || InUse())
        {
            return false;
        }

        if (pending)
        {
            if (revision >= 0 && view.IsOwner() && view.GetZDO().DataRevision >= revision)
            {
                foreach (Container container in Containers())
                {
                    container.Load();
                }

                pending = false;
                revision = -1;
            }
            else
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

    private void Request(long sender, long playerId, long request)
    {
        if (!view.IsOwner())
        {
            return;
        }

        List<Container> containers = Containers().ToList();
        ZPackage data = new();
        bool granted = !InUse() && containers.All(container => container.CheckAccess(playerId));
        data.Write(granted);
        if (granted)
        {
            foreach (Container container in containers)
            {
                container.Load();
                container.Save();
            }

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
