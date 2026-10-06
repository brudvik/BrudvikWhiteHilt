using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Production;

/// <summary>
/// Messages at the top left when a station near the player finishes, fills up, stops while working, or is about to
/// burn the food. A station is only watched once it is in range, so walking up to one sends nothing.
/// </summary>
public static class ProductionNotifier
{
    private const float RefreshInterval = 1f;

    private static readonly List<Component> nearby = new();
    private static readonly ProductionStatus status = new();
    private static Dictionary<Component, Memory> watched = new();
    private static Dictionary<Component, Memory> next = new();
    private static float nextRefresh;

    /// <summary>
    /// Checks the stations near the player. Called after the HUD update.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Update(Player player)
    {
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshInterval;
        if (!ProductionSettings.Notify.Value)
        {
            watched.Clear();
            return;
        }

        ProductionRegistry.Near(player.transform.position, ProductionSettings.NotifyRange.Value, nearby);
        next.Clear();
        foreach (Component source in nearby)
        {
            if (!ProductionReader.Read(source, status, false))
            {
                continue;
            }

            bool known = watched.TryGetValue(source, out Memory before);
            string message = known ? Change(before.State, status) : null;
            bool warned = status.Warning != null;
            if (warned && (!known || !before.Warned))
            {
                message ??= ProductionReader.Text(status.Warning, status.Name);
            }

            if (message != null)
            {
                player.Message(MessageHud.MessageType.TopLeft, message);
            }

            next[source] = new Memory(status.State, warned);
        }

        (watched, next) = (next, watched);
    }

    // The message for a piece that stopped working: ready, full, born, done or stopped with why. A windmill stopping
    // for want of wind is not news.
    private static string Change(ProductionState before, ProductionStatus now)
    {
        if (before != ProductionState.Working || now.State == ProductionState.Working)
        {
            return null;
        }

        switch (now.State)
        {
            case ProductionState.Ready:
                bool fills = now.Kind == ProductionKind.Beehive || now.Kind == ProductionKind.SapCollector;
                return ProductionReader.Text(fills ? "$whitehilt_prod_msg_full" : "$whitehilt_prod_msg_ready", now.Name);
            case ProductionState.Idle:
                return ProductionReader.Text(now.Kind == ProductionKind.Animal ? "$whitehilt_prod_msg_birth" : "$whitehilt_prod_msg_done", now.Name);
            case ProductionState.Stopped:
                // The wind comes and goes; a windmill stopping is not news.
                return now.Source is Smelter { m_windmill: not null } ? null : ProductionReader.Text("$whitehilt_prod_msg_stopped", now.Name, now.Reason);
            default:
                return null;
        }
    }

    private readonly struct Memory
    {
        public readonly ProductionState State;
        public readonly bool Warned;

        public Memory(ProductionState state, bool warned)
        {
            State = state;
            Warned = warned;
        }
    }
}
