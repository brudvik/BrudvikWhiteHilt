using BepInEx;
using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Building.Terrain;
using BrudvikWhiteHilt.Crafting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Moats;

/// <summary>
/// Digs a planned moat. Only terrain near the player is loaded, so the moat is dug stretch by stretch whenever the player
/// is within reach of an undug stretch, like a road. Each dug stretch leaves a <see cref="MoatSection"/> in the world;
/// a staked ditch gets sharp stakes in its bottom, paid for as if built. One moat is dug at a time; it is saved with the
/// world and carries on after a restart. Digging and the bank cost nothing.
/// </summary>
public static class MoatBuilder
{
    private const int ChunkSamples = 16;
    private const float TickInterval = 0.5f;
    private const float EdgeProbe = 0.5f;
    private const string StakePrefab = "piece_sharpstakes";
    private const int Version = 1;

    private static Moat moat;
    private static string loadedWorld;
    private static float nextTick;
    private static bool stakeWarning;

    /// <summary>True if a moat is being dug.</summary>
    public static bool HasMoat => moat != null;

    /// <summary>True if the moat is paused.</summary>
    public static bool Paused => moat != null && moat.Paused;

    /// <summary>
    /// Status of the moat, e.g. "Moat 45% being dug as you walk along it", or empty.
    /// </summary>
    /// <returns>The text.</returns>
    public static string Status()
    {
        if (moat == null)
        {
            return string.Empty;
        }

        int built = 0;
        foreach (Chunk chunk in moat.Chunks)
        {
            built += chunk.Built ? 1 : 0;
        }

        string state = moat.Paused
            ? Localization.instance.Localize("$whitehilt_terrain_road_paused") + (string.IsNullOrEmpty(moat.Blocker) ? string.Empty : ": " + moat.Blocker)
            : Localization.instance.Localize("$whitehilt_terrain_road_building");
        return string.Format(Localization.instance.Localize("$whitehilt_moat_status"), moat.Chunks.Count == 0 ? 100 : built * 100 / moat.Chunks.Count, state);
    }

    /// <summary>
    /// Starts digging a moat; a moat already being dug is given up, what is dug of it stays.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="spec">The moat's sizes.</param>
    /// <param name="runs">Where it runs.</param>
    public static void Start(Player player, MoatSpec spec, List<MoatRun> runs)
    {
        moat = new Moat { Spec = spec, Runs = runs };
        moat.Prepare();
        stakeWarning = false;
        Save();
        player.Message(MessageHud.MessageType.Center,
            string.Format(Localization.instance.Localize("$msg_whitehilt_moat_started"), Mathf.RoundToInt(MoatGeometry.Length(runs))));
    }

    /// <summary>
    /// Pauses or resumes the moat.
    /// </summary>
    public static void TogglePause()
    {
        if (moat != null)
        {
            moat.Paused = !moat.Paused;
            moat.Blocker = null;
            Save();
        }
    }

    /// <summary>
    /// Gives up the moat; what is dug stays.
    /// </summary>
    public static void Cancel()
    {
        moat = null;
        Save();
    }

    /// <summary>
    /// Digs the moat near the player. Called every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        string world = ZNet.instance != null ? ZNet.instance.GetWorldName() : null;
        if (world != null && world != loadedWorld)
        {
            loadedWorld = world;
            Load();
        }

        if (moat == null || moat.Paused || Time.time < nextTick || TerrainEdit.Busy || !MoatSettings.Enabled.Value)
        {
            return;
        }

        nextTick = Time.time + TickInterval;
        DigNear(player);
    }

    // Digs the next stretch of the moat near the player, one per call. Terrain can only be changed where it is loaded,
    // so the moat is dug as the player walks along it.
    private static void DigNear(Player player)
    {
        Vector2 here = new(player.transform.position.x, player.transform.position.z);
        float reach = TerrainSettings.RoadBuildReach.Value;
        foreach (Chunk chunk in moat.Chunks)
        {
            if (chunk.Built)
            {
                continue;
            }

            MoatRun run = moat.Runs[chunk.Run];
            Vector2 middle = run.Points[(chunk.First + chunk.Last) / 2];
            if (Vector2.Distance(here, middle) > reach || !Loaded(run, chunk))
            {
                continue;
            }

            Dig(player, run, chunk);
            return;
        }
    }

    // Whether the ground at both ends of a stretch, and to both sides as far as the moat reaches, is loaded: digging
    // part of a stretch would leave a step in it.
    private static bool Loaded(MoatRun run, Chunk chunk)
    {
        float reach = moat.Spec.Reach;
        foreach (int index in new[] { chunk.First, chunk.Last })
        {
            Vector2 point = run.Points[index];
            Vector2 side = run.Outward[index] * reach;
            foreach (Vector2 probe in new[] { point, point + side, point - side })
            {
                if (Heightmap.FindHeightmap(new Vector3(probe.x, 0f, probe.y)) == null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Digs one stretch: the bottom is set from the lowest bank of the stretch, so the ditch is level and a wet moat
    // holds water; the earth goes to the chosen bank. Each terrain vertex belongs to the stretch of its nearest sample,
    // so neighbouring stretches never dig it twice. The section's shape is saved on a network object (MoatSection),
    // which shows the water and slows what wades in it on every machine.
    private static void Dig(Player player, MoatRun run, Chunk chunk)
    {
        MoatSpec spec = moat.Spec;
        int count = chunk.Last - chunk.First + 1;
        float[] top = new float[count];
        float[] centre = new float[count];
        float lowest = float.MaxValue;
        float edge = spec.Width / 2f + EdgeProbe;
        for (int i = 0; i < count; i++)
        {
            Vector2 point = run.Points[chunk.First + i];
            Vector2 side = run.Outward[chunk.First + i] * edge;
            top[i] = Mathf.Min(Ground(point - side), Ground(point + side));
            centre[i] = Ground(point);
            lowest = Mathf.Min(lowest, top[i]);
        }

        bool wet = spec.Profile == MoatProfile.Wet;
        float bottom = lowest - spec.Depth;
        float? waterLevel = wet && spec.Water ? lowest - spec.Freeboard : null;

        // Every vertex belongs to the stretch of its nearest sample, so neighbouring stretches never dig it twice.
        List<int> candidates = new();
        for (int i = chunk.First - ChunkSamples; i <= chunk.Last + ChunkSamples; i++)
        {
            if (run.Closed || (i >= 0 && i < run.Count))
            {
                candidates.Add(run.Wrap(i));
            }
        }

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        for (int i = chunk.First; i <= chunk.Last; i++)
        {
            min = Vector2.Min(min, run.Points[i]);
            max = Vector2.Max(max, run.Points[i]);
        }

        float reach = spec.Reach;
        TerrainShapes.Plan plan = TerrainShapes.Build(min - Vector2.one * reach, max + Vector2.one * reach, (x, z, current) =>
        {
            Vector2 at = new(x, z);
            int nearest = -1;
            float best = float.MaxValue;
            foreach (int index in candidates)
            {
                float distance = (run.Points[index] - at).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = index;
                }
            }

            if (nearest < chunk.First || nearest > chunk.Last)
            {
                return default;
            }

            Vector2 offset = at - run.Points[nearest];
            float factor = run.Factor[nearest];
            if (!run.Closed && (nearest == 0 || nearest == run.Count - 1))
            {
                float along = Vector2.Dot(offset, run.Tangent(nearest)) * (nearest == 0 ? -1f : 1f);
                factor *= Mathf.Clamp01(1f - (along - 0.5f) / 2f);
            }

            if (factor <= 0.001f || !spec.Wanted(Vector2.Dot(offset, run.Outward[nearest]), current, bottom, out float height))
            {
                return default;
            }

            return new TerrainShapes.Want(true, Mathf.Lerp(current, height, factor), true);
        }, TerrainModifier.PaintType.Dirt);

        plan.Cost = 0;
        plan.Job.Undoable = false;
        if (plan.Problem != null)
        {
            Pause(player, plan.Problem);
            return;
        }

        List<(Vector3 Position, float Yaw)> stakes = spec.Profile == MoatProfile.Staked ? StakePlaces(run, chunk, centre, spec) : null;
        plan.Job.Done = () =>
        {
            MoatSection.Spawn(run, chunk.First, chunk.Last, top, spec, waterLevel, bottom);
            if (stakes != null)
            {
                PlaceStakes(player, stakes);
            }
        };

        if (plan.Job.Ops.Count == 0)
        {
            plan.Job.Done();
        }
        else if (!TerrainShapes.Run(player, plan))
        {
            Pause(player, Localization.instance.Localize("$whitehilt_moat"));
            return;
        }

        chunk.Built = true;
        Save();
        if (moat.Chunks.TrueForAll(done => done.Built))
        {
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$whitehilt_moat") + ": "
                + Localization.instance.Localize("$whitehilt_terrain_road_done"));
            Cancel();
        }
    }

    private static List<(Vector3 Position, float Yaw)> StakePlaces(MoatRun run, Chunk chunk, float[] centre, MoatSpec spec)
    {
        List<(Vector3, float)> places = new();
        int every = Mathf.Max(1, Mathf.RoundToInt(spec.StakeSpacing));
        for (int i = chunk.First; i <= chunk.Last; i++)
        {
            if (i % every != 0 || run.Factor[i] < 0.99f)
            {
                continue;
            }

            Vector2 point = run.Points[i];
            Vector2 tangent = run.Tangent(i);
            // Local x along the ditch; every other stake turned round, so the points lean both ways.
            float yaw = Mathf.Atan2(-tangent.y, tangent.x) * Mathf.Rad2Deg + (i / every % 2 == 0 ? 0f : 180f);
            places.Add((new Vector3(point.x, centre[i - chunk.First] - spec.Depth, point.y), yaw));
        }

        return places;
    }

    // Places vanilla sharp stakes along a staked ditch, paid like building them by hand. What lies in chests other
    // players hold is fetched first, and the stakes are placed when it has arrived. When the materials run out it warns
    // once and the ditch goes on without stakes.
    private static void PlaceStakes(Player player, List<(Vector3 Position, float Yaw)> stakes)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(StakePrefab) : null;
        Piece template = prefab != null ? prefab.GetComponent<Piece>() : null;
        if (template == null)
        {
            return;
        }

        List<(string Name, int Amount)> each = Needs(template);
        if (!player.m_noPlacementCost && ChestCost.Gather(player, NearbyContainers.Use.Building,
                each.Select(need => (need.Name, need.Amount * stakes.Count)), () => PlaceStakes(player, stakes)) == ChestCost.Outcome.Fetching)
        {
            return;
        }

        foreach ((Vector3 position, float yaw) in stakes)
        {
            if (!player.m_noPlacementCost && each.Any(need => ChestCost.AtHand(player, NearbyContainers.Use.Building, need.Name) < need.Amount))
            {
                if (!stakeWarning)
                {
                    stakeWarning = true;
                    player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_moat_stakes");
                }

                return;
            }

            if (!player.m_noPlacementCost)
            {
                ChestCost.Take(player, NearbyContainers.Use.Building, each);
            }

            GameObject stake = UnityEngine.Object.Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f));
            stake.GetComponent<Piece>()?.SetCreator(player.GetPlayerID(), Splatform.PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
        }
    }

    private static List<(string Name, int Amount)> Needs(Piece piece)
    {
        return piece.m_resources.Where(requirement => requirement.m_resItem != null && requirement.m_amount > 0)
            .Select(requirement => (requirement.m_resItem.m_itemData.m_shared.m_name, requirement.m_amount)).ToList();
    }

    private static float Ground(Vector2 point)
    {
        Vector3 world = new(point.x, 0f, point.y);
        return Heightmap.GetHeight(world, out float height) ? height : WorldGenerator.instance.GetHeight(point.x, point.y);
    }

    private static void Pause(Player player, string blocker)
    {
        moat.Paused = true;
        moat.Blocker = blocker;
        player.Message(MessageHud.MessageType.Center, string.Format(Localization.instance.Localize("$msg_whitehilt_moat_paused"), blocker));
        Save();
    }

    private static string File()
    {
        string world = loadedWorld;
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            world = world.Replace(invalid, '_');
        }

        return Path.Combine(Paths.ConfigPath, "BrudvikWhiteHilt", "moats", world + ".whmoat");
    }

    // Saves the moat being dug, per world, so it carries on after a restart.
    private static void Save()
    {
        if (string.IsNullOrEmpty(loadedWorld))
        {
            return;
        }

        try
        {
            string file = File();
            if (moat == null)
            {
                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                }

                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(file));
            ZPackage package = new();
            package.Write(Version);
            moat.Spec.Write(package);
            package.Write(moat.Paused);
            package.Write(moat.Runs.Count);
            foreach (MoatRun run in moat.Runs)
            {
                run.Write(package);
            }

            package.Write(moat.Chunks.Count);
            foreach (Chunk chunk in moat.Chunks)
            {
                package.Write(chunk.Built);
            }

            System.IO.File.WriteAllBytes(file, package.GetArray());
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Could not save the moat: {ex.Message}");
        }
    }

    // Reads the moat being dug for this world, if any.
    private static void Load()
    {
        moat = null;
        string file = File();
        if (!System.IO.File.Exists(file))
        {
            return;
        }

        try
        {
            ZPackage package = new(System.IO.File.ReadAllBytes(file));
            package.ReadInt();
            Moat loaded = new() { Spec = MoatSpec.Read(package), Runs = new List<MoatRun>() };
            loaded.Paused = package.ReadBool();
            int runs = package.ReadInt();
            for (int i = 0; i < runs; i++)
            {
                loaded.Runs.Add(MoatRun.Read(package));
            }

            loaded.Prepare();
            int chunks = package.ReadInt();
            for (int i = 0; i < chunks && i < loaded.Chunks.Count; i++)
            {
                loaded.Chunks[i].Built = package.ReadBool();
            }

            moat = loaded;
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Could not read the moat: {ex.Message}");
        }
    }

    private sealed class Moat
    {
        public MoatSpec Spec;
        public List<MoatRun> Runs;
        public bool Paused;
        public string Blocker;
        public readonly List<Chunk> Chunks = new();

        public void Prepare()
        {
            Chunks.Clear();
            for (int r = 0; r < Runs.Count; r++)
            {
                for (int first = 0; first < Runs[r].Count; first += ChunkSamples)
                {
                    Chunks.Add(new Chunk { Run = r, First = first, Last = Mathf.Min(first + ChunkSamples, Runs[r].Count) - 1 });
                }
            }
        }
    }

    private sealed class Chunk
    {
        public int Run;
        public int First;
        public int Last;
        public bool Built;
    }
}
