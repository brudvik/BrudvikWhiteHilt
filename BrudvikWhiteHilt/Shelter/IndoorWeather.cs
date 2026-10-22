using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Shelter;

/// <summary>
/// Keeps fog, snow and rain outside a house. The game only asks whether the player is sheltered when the weather changes,
/// and only for misty weather, so a snowstorm that starts outside keeps snowing in the hall, and its fog is a render
/// setting for the whole view. Under a roof the fog is thinned, less snow and rain is made round the player, and what
/// is under the roof is taken away. Rain and snow the game lets fall through roofs are also made to land on them.
/// </summary>
public static class IndoorWeather
{
    // The game counts 80 % cover as shelter; below 30 % under a roof the weather is as outside.
    private const float LeastCover = 0.3f;
    private const float ShelterCover = 0.8f;
    private const float CellSize = 1f;
    private const float RoofSearchHeight = 60f;
    private const int RaysPerFrame = 96;
    private const float CellSeconds = 2f;

    private static readonly Dictionary<GameObject, ParticleSystem[]> systemsOf = new();
    private static readonly Dictionary<ParticleSystem, Vector2> emissionOf = new();
    private static readonly List<ParticleSystem> current = new();
    private static readonly HashSet<ParticleSystem> currentSet = new();
    private static readonly List<ParticleSystem> released = new();
    private static readonly List<ParticleSystem> collisionAdded = new();
    private static readonly Dictionary<Vector2Int, Roof> roofs = new();
    private static readonly List<Vector2Int> staleCells = new();

    private static ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[1024];
    private static float indoor;
    private static float fogIndoor;
    private static int pieceMask;
    private static int scanIndex;
    private static EnvMan collisionEnv;
    private static bool collisionOn;

    private struct Roof
    {
        public float Height;
        public float Time;
    }

    /// <summary>
    /// Thins the fog the game has just set, by how covered the player is. Called after <see cref="EnvMan.SetEnv"/>.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    public static void ApplyFog(float dt)
    {
        fogIndoor = Mathf.MoveTowards(fogIndoor, Target(), dt / Fade());
        if (fogIndoor > 0f)
        {
            float density = RenderSettings.fogDensity;
            float least = Mathf.Max(0f, IndoorWeatherSettings.FogDensity.Value);
            RenderSettings.fogDensity = Mathf.Lerp(density, Mathf.Min(density, least), fogIndoor);
        }
    }

    /// <summary>
    /// Makes less snow and rain round the player under a roof and takes away what is under it. Called after the game's
    /// environment update, each frame.
    /// </summary>
    /// <param name="env">The environment manager.</param>
    public static void Tick(EnvMan env)
    {
        UpdateRoofCollision(env);

        Player player = Player.m_localPlayer;
        indoor = Mathf.MoveTowards(indoor, Target(), Time.deltaTime / Fade());
        CollectCurrent(env);

        float amount = Mathf.Lerp(1f, Mathf.Clamp01(IndoorWeatherSettings.ParticleAmount.Value), indoor);
        foreach (ParticleSystem system in current)
        {
            SetEmission(system, amount);
        }

        ReleaseOthers();

        bool clearing = IndoorWeatherSettings.Enabled.Value && player != null && !player.IsDead() && !player.InInterior()
            && player.m_underRoof;
        if (!clearing)
        {
            if (roofs.Count > 0)
            {
                roofs.Clear();
            }

            return;
        }

        Vector3 centre = player.transform.position;
        float reach = Mathf.Max(1f, IndoorWeatherSettings.ClearReach.Value);
        ScanRoofs(centre, reach);
        foreach (ParticleSystem system in current)
        {
            ClearUnderRoofs(system, centre, reach);
        }
    }

    // 0 outdoors, 1 in a closed house. Dungeons have their own weather and are left alone.
    private static float Target()
    {
        Player player = Player.m_localPlayer;
        if (!IndoorWeatherSettings.Enabled.Value || player == null || player.IsDead() || player.InInterior() || !player.m_underRoof)
        {
            return 0f;
        }

        return Mathf.InverseLerp(LeastCover, ShelterCover, player.m_coverPercentage);
    }

    private static float Fade()
    {
        return Mathf.Max(0.05f, IndoorWeatherSettings.FadeSeconds.Value);
    }

    // The particle systems of the weather now, the game's own and their children.
    private static void CollectCurrent(EnvMan env)
    {
        current.Clear();
        currentSet.Clear();
        GameObject[] objects = env.m_currentPSystems;
        if (objects == null)
        {
            return;
        }

        foreach (GameObject go in objects)
        {
            if (go == null)
            {
                continue;
            }

            if (!systemsOf.TryGetValue(go, out ParticleSystem[] systems))
            {
                systems = go.GetComponentsInChildren<ParticleSystem>(true);
                systemsOf[go] = systems;
            }

            foreach (ParticleSystem system in systems)
            {
                if (system != null && currentSet.Add(system))
                {
                    current.Add(system);
                }
            }
        }
    }

    // Scales how much a system makes, remembering what the game set.
    private static void SetEmission(ParticleSystem system, float amount)
    {
        ParticleSystem.EmissionModule emission = system.emission;
        if (!emissionOf.TryGetValue(system, out Vector2 original))
        {
            if (amount >= 1f)
            {
                return;
            }

            original = new Vector2(emission.rateOverTimeMultiplier, emission.rateOverDistanceMultiplier);
            emissionOf[system] = original;
        }

        emission.rateOverTimeMultiplier = original.x * amount;
        emission.rateOverDistanceMultiplier = original.y * amount;
        if (amount >= 1f)
        {
            emissionOf.Remove(system);
        }
    }

    // Gives back what the game set to systems of a weather that has passed.
    private static void ReleaseOthers()
    {
        released.Clear();
        foreach (KeyValuePair<ParticleSystem, Vector2> pair in emissionOf)
        {
            if (pair.Key == null || !currentSet.Contains(pair.Key))
            {
                released.Add(pair.Key);
            }
        }

        foreach (ParticleSystem system in released)
        {
            if (system != null)
            {
                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTimeMultiplier = emissionOf[system].x;
                emission.rateOverDistanceMultiplier = emissionOf[system].y;
            }

            emissionOf.Remove(system);
        }
    }

    // Finds the top of the building pieces over each metre round the player, a few rays a frame.
    private static void ScanRoofs(Vector3 centre, float reach)
    {
        if (pieceMask == 0)
        {
            pieceMask = LayerMask.GetMask("piece");
        }

        float now = Time.time;
        int radius = Mathf.CeilToInt(reach / CellSize);
        int side = radius * 2 + 1;
        int cells = side * side;
        Vector2Int middle = CellOf(centre);
        for (int i = 0; i < RaysPerFrame && i < cells; i++)
        {
            scanIndex = (scanIndex + 1) % cells;
            Vector2Int cell = new(middle.x + scanIndex % side - radius, middle.y + scanIndex / side - radius);
            if (roofs.TryGetValue(cell, out Roof known) && now - known.Time < CellSeconds)
            {
                continue;
            }

            Vector3 top = new((cell.x + 0.5f) * CellSize, centre.y + RoofSearchHeight, (cell.y + 0.5f) * CellSize);
            float height = Physics.Raycast(top, Vector3.down, out RaycastHit hit, RoofSearchHeight * 2f, pieceMask)
                ? hit.point.y
                : float.NegativeInfinity;
            roofs[cell] = new Roof { Height = height, Time = now };
        }

        // Cells left far behind are forgotten.
        if (roofs.Count > cells * 2)
        {
            staleCells.Clear();
            foreach (KeyValuePair<Vector2Int, Roof> pair in roofs)
            {
                if (Mathf.Abs(pair.Key.x - middle.x) > radius || Mathf.Abs(pair.Key.y - middle.y) > radius)
                {
                    staleCells.Add(pair.Key);
                }
            }

            foreach (Vector2Int cell in staleCells)
            {
                roofs.Remove(cell);
            }
        }
    }

    // Ends the snowflakes and raindrops that are under a roof near the player.
    private static void ClearUnderRoofs(ParticleSystem system, Vector3 centre, float reach)
    {
        int count = system.particleCount;
        if (count == 0)
        {
            return;
        }

        if (buffer.Length < count)
        {
            buffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(count)];
        }

        count = system.GetParticles(buffer, count);
        ParticleSystem.MainModule main = system.main;
        Transform space = main.simulationSpace switch
        {
            ParticleSystemSimulationSpace.Local => system.transform,
            ParticleSystemSimulationSpace.Custom => main.customSimulationSpace,
            _ => null,
        };

        float reachSqr = reach * reach;
        bool changed = false;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = space != null ? space.TransformPoint(buffer[i].position) : buffer[i].position;
            float dx = position.x - centre.x;
            float dz = position.z - centre.z;
            if (dx * dx + dz * dz > reachSqr)
            {
                continue;
            }

            if (roofs.TryGetValue(CellOf(position), out Roof roof) && position.y < roof.Height)
            {
                buffer[i].remainingLifetime = 0f;
                changed = true;
            }
        }

        if (changed)
        {
            system.SetParticles(buffer, count);
        }
    }

    private static Vector2Int CellOf(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.z / CellSize));
    }

    // The game has made several of its rain and snow systems ready to die on roofs and ground, but left their collision
    // off, so that rain falls through every roof. Those are turned on; systems made otherwise are left alone.
    private static void UpdateRoofCollision(EnvMan env)
    {
        bool wanted = IndoorWeatherSettings.Enabled.Value && IndoorWeatherSettings.RoofCollision.Value;
        if (env == collisionEnv && wanted == collisionOn)
        {
            return;
        }

        if (env != collisionEnv)
        {
            collisionAdded.Clear();
            collisionEnv = env;
            FindRoofCollision(env);
        }

        collisionOn = wanted;
        foreach (ParticleSystem system in collisionAdded)
        {
            if (system != null)
            {
                ParticleSystem.CollisionModule collision = system.collision;
                collision.enabled = wanted;
            }
        }
    }

    private static void FindRoofCollision(EnvMan env)
    {
        int piece = LayerMask.GetMask("piece");
        HashSet<ParticleSystem> seen = new();
        foreach (EnvSetup setup in env.m_environments)
        {
            if (setup?.m_psystems == null)
            {
                continue;
            }

            foreach (GameObject go in setup.m_psystems)
            {
                if (go == null)
                {
                    continue;
                }

                foreach (ParticleSystem system in go.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ParticleSystem.CollisionModule collision = system.collision;
                    if (seen.Add(system) && !collision.enabled && collision.type == ParticleSystemCollisionType.World
                        && collision.lifetimeLossMultiplier >= 0.99f && (collision.collidesWith.value & piece) != 0)
                    {
                        collisionAdded.Add(system);
                    }
                }
            }
        }
    }
}
