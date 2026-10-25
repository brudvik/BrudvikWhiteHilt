using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// A shadow of the deep dark: a ghost gone black, with pale eyes. It comes for a player standing without a light in the
/// black, hunts only those without a light, is hard to hurt in the dark and easy to hurt in the light, and is gone when
/// the light comes back. It is made on the machine of the player it comes for, which runs it.
/// </summary>
public class DarkShadow : MonoBehaviour
{
    /// <summary>The shadow's prefab.</summary>
    public const string PrefabName = "WhiteHilt_Shadow";

    /// <summary>The place of a shadow that came out in the night of the Deep North, which it lasts as long as.</summary>
    public const string NightPlace = "night";

    private const string BaseCreature = "Ghost";
    private const string PlaceKey = "WhiteHilt_ShadowPlace";
    private const float TickSeconds = 0.5f;
    private const float HuntReach = 30f;
    private const float LightReach = 6f;
    private const float PlaceReach = 40f;
    private const float NearestSpawn = 8f;
    private const float FarthestSpawn = 12f;
    private const float MinimumSpawn = 4f;

    private static readonly List<DarkShadow> instances = new();
    private static int solidMask;

    private ZNetView view;
    private Character character;
    private MonsterAI ai;
    private float nextTick;
    private bool? inLight;

    /// <summary>
    /// Registers the shadow's name and makes it once the game's creatures are there. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CreatureManager.OnVanillaCreaturesAvailable += Create;
    }

    /// <summary>
    /// Brings a shadow to a player in the black, behind them where the dark is, unless the place has enough already.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="place">The place below ground.</param>
    /// <returns>Whether one came, or the place has its fill; false to try again in a moment.</returns>
    public static bool Summon(Player player, string place)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(PrefabName) : null;
        if (prefab == null || player == null)
        {
            return false;
        }

        int most = DarknessSettings.MaxShadows.Value + (DarknessClient.DeepNorth ? DarknessSettings.DeepNorthShadows.Value : 0);
        if (instances.Count(shadow => shadow != null && Vector3.Distance(shadow.transform.position, player.transform.position) < PlaceReach) >= most)
        {
            return true;
        }

        if (!SpawnPoint(player, out Vector3 point))
        {
            return false;
        }

        Vector3 toPlayer = player.transform.position - point;
        toPlayer.y = 0f;
        GameObject made = Instantiate(prefab, point, toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity);
        Character body = made.GetComponent<Character>();
        if (body != null)
        {
            body.SetLevel(Level());
        }

        ZNetView madeView = made.GetComponent<ZNetView>();
        if (madeView != null && madeView.IsValid())
        {
            madeView.GetZDO().Set(PlaceKey, place ?? string.Empty);
        }

        return true;
    }

    // One star for every two bosses fallen, up to two, so the shadow keeps up with the world.
    private static int Level()
    {
        int bosses = ZoneSystem.instance != null ? BeastDefinition.BossKeys.Count(ZoneSystem.instance.GetGlobalKey) : 0;
        return 1 + Mathf.Min(2, bosses / 2);
    }

    // Behind the player if there is room, else to a side, else ahead: short of any wall, on the floor. In a narrow
    // crypt the way behind is often a wall within a step or two.
    private static bool SpawnPoint(Player player, out Vector3 point)
    {
        if (solidMask == 0)
        {
            solidMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
        }

        Vector3 head = player.GetHeadPoint();
        Vector3 forward = player.transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
        foreach (float turn in new[] { 180f, 140f, 220f, 90f, 270f, 40f, 320f, 0f })
        {
            Vector3 way = Quaternion.Euler(0f, turn + Random.Range(-15f, 15f), 0f) * forward;
            float distance = Random.Range(NearestSpawn, FarthestSpawn);
            if (Physics.Raycast(head, way, out RaycastHit wall, distance, solidMask))
            {
                distance = wall.distance - 1f;
            }

            Vector3 at = head + way * distance;
            if (distance >= MinimumSpawn && Physics.Raycast(at, Vector3.down, out RaycastHit floor, 6f, solidMask))
            {
                point = floor.point + Vector3.up * 0.2f;
                return true;
            }
        }

        point = Vector3.zero;
        return false;
    }

    /// <summary>
    /// The player a shadow hunts: the nearest one standing without a light in the black.
    /// </summary>
    /// <param name="ai">The shadow's mind.</param>
    /// <returns>The player, or null.</returns>
    public static Character Prey(BaseAI ai)
    {
        Player best = null;
        float bestDistance = HuntReach;
        foreach (Player player in Player.GetAllPlayers())
        {
            float distance = player != null ? Vector3.Distance(player.transform.position, ai.transform.position) : float.MaxValue;
            if (distance < bestDistance && !player.IsDead() && Unlit(player))
            {
                best = player;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static bool Unlit(Player player)
    {
        ZDO zdo = player.m_nview != null && player.m_nview.IsValid() ? player.m_nview.GetZDO() : null;
        return zdo != null && zdo.GetBool(DarknessClient.UnlitKey);
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        instances.Add(this);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    // On the owner: lets go of a lit player, is hurt more easily near a light, and goes when the light comes back.
    private void Update()
    {
        if (Time.time < nextTick || view == null || !view.IsValid() || !view.IsOwner() || character == null || character.IsDead())
        {
            return;
        }

        nextTick = Time.time + TickSeconds;
        string place = view.GetZDO().GetString(PlaceKey);
        if (place == NightPlace ? !DeepNorthNight.IsNight() : DarknessClient.LevelOf(place) < 0.5f)
        {
            Vanish();
            return;
        }

        if (ai != null && ai.m_targetCreature is Player target && !Unlit(target))
        {
            ai.m_targetCreature = null;
        }

        bool lit = NearLight();
        if (lit != inLight)
        {
            inLight = lit;
            HitData.DamageModifier modifier = lit ? HitData.DamageModifier.Weak : HitData.DamageModifier.VeryResistant;
            HitData.DamageModifiers modifiers = character.m_damageModifiers;
            modifiers.m_blunt = modifiers.m_slash = modifiers.m_pierce = modifiers.m_chop = modifier;
            modifiers.m_fire = modifiers.m_frost = modifiers.m_lightning = modifiers.m_poison = modifiers.m_spirit = modifier;
            character.m_damageModifiers = modifiers;
        }
    }

    private bool NearLight()
    {
        foreach (Player player in Player.GetAllPlayers())
        {
            if (player != null && Vector3.Distance(player.transform.position, transform.position) <= LightReach && DarknessClient.CarriesLight(player))
            {
                return true;
            }
        }

        return false;
    }

    // Gone in a puff, as a ghost dies.
    private void Vanish()
    {
        character.m_deathEffects?.Create(transform.position, transform.rotation);
        ZNetScene.instance.Destroy(gameObject);
    }

    // A clone of the ghost: black, with pale eyes, no loot, nothing it cares for but the players in the dark.
    private static void Create()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Create;
        try
        {
            CustomCreature creature = new(PrefabName, BaseCreature, new CreatureConfig { Name = Translations.Token("enemy_whitehilt_shadow") });
            GameObject prefab = creature.Prefab;
            Character body = prefab.GetComponent<Character>();
            body.m_name = "$enemy_whitehilt_shadow";
            body.m_boss = false;
            CharacterDrop drops = prefab.GetComponent<CharacterDrop>();
            drops?.m_drops.Clear();
            BaseAI mind = prefab.GetComponent<BaseAI>();
            if (mind != null)
            {
                mind.m_randomMoveRange = 4f;
            }

            if (prefab.GetComponent<MonsterAI>() is MonsterAI monster)
            {
                monster.m_attackPlayerObjects = false;
            }

            ZNetView netView = prefab.GetComponent<ZNetView>();
            netView.m_persistent = false;
            prefab.AddComponent<DarkShadow>();
            prefab.AddComponent<DarknessKeep>();
            Blacken(prefab, body);
            CreatureManager.Instance.AddCreature(creature);
        }
        catch (System.Exception ex)
        {
            Jotunn.Logger.LogError($"Shadow failed to load: {ex.Message}");
        }
    }

    private static void Blacken(GameObject prefab, Character body)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        VisualHelper.Recolor(prefab, pixel => new Color32((byte)(pixel.r * 0.08f), (byte)(pixel.g * 0.08f), (byte)(pixel.b * 0.1f), pixel.a));
        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials.Where(material => material != null && material.HasProperty("_Color")))
            {
                Color colour = material.color;
                material.color = new Color(0.04f, 0.04f, 0.06f, colour.a);
            }
        }

        foreach (ParticleSystem particles in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.startColor = new Color(0.03f, 0.03f, 0.05f, 0.7f);
        }

        foreach (Light glow in prefab.GetComponentsInChildren<Light>(true))
        {
            glow.color = new Color(0.6f, 0.15f, 0.1f);
            glow.intensity *= 0.3f;
        }

        Transform head = body.m_eye != null ? body.m_eye : prefab.transform;
        Material eyeMaterial = VisualHelper.UnlitMaterial(new Color(1f, 0.9f, 0.6f));
        if (eyeMaterial == null)
        {
            return;
        }

        foreach (float side in new[] { -0.08f, 0.08f })
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(eye.GetComponent<Collider>());
            eye.name = "whitehilt_shadow_eye";
            eye.transform.SetParent(head, false);
            eye.transform.localPosition = new Vector3(side, 0f, 0.1f);
            eye.transform.localScale = new Vector3(0.06f, 0.03f, 0.03f);
            Renderer renderer = eye.GetComponent<Renderer>();
            renderer.sharedMaterial = eyeMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
