using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BrudvikWhiteHilt.Helpers;

/// <summary>
/// One walk through every object in the world, shared by the server's services that look for things in it (the portal
/// map, discoveries, map areas). Each object is read once per walk and handed only to the services that asked for its
/// prefab, instead of every service walking the whole world on its own. The walk goes on for as long as any service has
/// a scan running, a slice of time per frame, and stops between sectors, never inside one. A service's scan starts
/// wherever the walk is and ends when the walk has been once round every sector, so it never waits for another
/// service's scan. Driven by <see cref="WorldScanService"/>.
/// </summary>
public sealed class WorldScan
{
    /// <summary>
    /// A service that scans the world.
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// Takes an object of a prefab the service asked for. Values must be read at once: ZDOs are pooled, so a reference
        /// kept to the next frame may be another object.
        /// </summary>
        /// <param name="zdo">The object.</param>
        /// <param name="prefab">Its prefab hash.</param>
        void Collect(ZDO zdo, int prefab);

        /// <summary>
        /// The scan has been through every sector.
        /// </summary>
        void Finish();

        /// <summary>
        /// The scan was stopped because <see cref="Collect"/> or <see cref="Finish"/> threw.
        /// </summary>
        /// <param name="error">What was thrown.</param>
        void Fail(Exception error);
    }

    // Sectors without objects cost next to nothing; the clock is read only every so many of them.
    private const int EmptySectorsPerCheck = 1024;

    private readonly List<Lap> laps = new();
    private readonly Dictionary<int, int> wanted = new();
    private readonly Stopwatch watch = new();
    private int active;
    private int cursor;
    private bool tableChanged;

    /// <summary>
    /// Whether a service's scan is running.
    /// </summary>
    /// <param name="client">The service.</param>
    /// <returns>True from <see cref="Begin"/> until its scan finishes or fails.</returns>
    public bool IsRunning(IClient client)
    {
        int slot = laps.FindIndex(lap => lap.Client == client);
        return slot >= 0 && (active & (1 << slot)) != 0;
    }

    /// <summary>
    /// Starts a service's scan; nothing happens if it is running already. The prefabs are read when the next step begins,
    /// so a service that sorts the game's prefabs again passes a new collection.
    /// </summary>
    /// <param name="client">The service.</param>
    /// <param name="prefabs">The prefab hashes the service wants to see.</param>
    public void Begin(IClient client, ICollection<int> prefabs)
    {
        int slot = laps.FindIndex(lap => lap.Client == client);
        if (slot < 0)
        {
            if (laps.Count >= 32)
            {
                throw new InvalidOperationException("World scan: too many services");
            }

            slot = laps.Count;
            laps.Add(new Lap { Client = client });
        }

        Lap lap = laps[slot];
        if ((active & (1 << slot)) != 0)
        {
            return;
        }

        if (!ReferenceEquals(lap.Prefabs, prefabs))
        {
            lap.Prefabs = prefabs;
            tableChanged = true;
        }

        lap.Remaining = -1;
        active |= 1 << slot;
    }

    /// <summary>
    /// Walks on through the sectors until the time is used up or no scan is running, and ends the scans that have been
    /// through every sector.
    /// </summary>
    /// <param name="sectors">The world's objects by sector (<c>ZDOMan.m_objectsBySector</c>).</param>
    /// <param name="budgetTicks">How long this step may take, in <see cref="Stopwatch"/> ticks; at least one sector is read.</param>
    public void Step(List<ZDO>[] sectors, long budgetTicks)
    {
        if (active == 0 || sectors == null || sectors.Length == 0)
        {
            return;
        }

        if (tableChanged)
        {
            BuildTable();
        }

        // A scan begun since the last step goes once round from where the walk is now.
        for (int slot = 0; slot < laps.Count; slot++)
        {
            if ((active & (1 << slot)) != 0 && laps[slot].Remaining < 0)
            {
                laps[slot].Remaining = sectors.Length;
            }
        }

        watch.Restart();
        int empty = 0;
        while (active != 0)
        {
            if (cursor >= sectors.Length)
            {
                cursor = 0;
            }

            List<ZDO> sector = sectors[cursor++];
            bool any = sector != null && sector.Count > 0;
            if (any)
            {
                Visit(sector);
            }

            CountDown();
            if ((any || ++empty % EmptySectorsPerCheck == 0) && watch.ElapsedTicks >= budgetTicks)
            {
                return;
            }
        }
    }

    // Hands each object of a sector to the running scans that asked for its prefab. An index loop: no enumerator to
    // allocate, and nothing a service does while collecting changes the list.
    private void Visit(List<ZDO> sector)
    {
        for (int i = 0; i < sector.Count; i++)
        {
            ZDO zdo = sector[i];
            if (zdo == null || !zdo.IsValid())
            {
                continue;
            }

            int prefab = zdo.GetPrefab();
            if (!wanted.TryGetValue(prefab, out int slots) || (slots &= active) == 0)
            {
                continue;
            }

            for (int slot = 0; slot < laps.Count; slot++)
            {
                if ((slots & (1 << slot)) == 0)
                {
                    continue;
                }

                try
                {
                    laps[slot].Client.Collect(zdo, prefab);
                }
                catch (Exception ex)
                {
                    Stop(slot, ex);
                }
            }
        }
    }

    private void CountDown()
    {
        for (int slot = 0; slot < laps.Count; slot++)
        {
            if ((active & (1 << slot)) == 0 || --laps[slot].Remaining > 0)
            {
                continue;
            }

            active &= ~(1 << slot);
            try
            {
                laps[slot].Client.Finish();
            }
            catch (Exception ex)
            {
                Stop(slot, ex);
            }
        }
    }

    private void Stop(int slot, Exception error)
    {
        active &= ~(1 << slot);
        try
        {
            laps[slot].Client.Fail(error);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"World scan: {ex.Message}");
        }
    }

    // Which services want which prefab, as one bit per service, so an object is looked up once for all of them.
    private void BuildTable()
    {
        tableChanged = false;
        wanted.Clear();
        for (int slot = 0; slot < laps.Count; slot++)
        {
            if (laps[slot].Prefabs == null)
            {
                continue;
            }

            foreach (int prefab in laps[slot].Prefabs)
            {
                wanted.TryGetValue(prefab, out int slots);
                wanted[prefab] = slots | (1 << slot);
            }
        }
    }

    private sealed class Lap
    {
        public IClient Client;
        public ICollection<int> Prefabs;
        public int Remaining;
    }
}
