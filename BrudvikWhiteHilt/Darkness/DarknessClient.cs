using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Darkness;

/// <summary>
/// Runs on the <see cref="Game"/> object of every machine with a player. Below ground it finds the place the player is
/// in, works out how dark it is there now, whether the player holds a light, and keeps their fear: it grows in the
/// black without a light and falls in the light. Fear slows the stamina, then the legs, and in the end hurts; from half
/// fear a shadow comes. A torch burns down faster in the black and gutters now and then.
/// </summary>
public class DarknessClient : MonoBehaviour
{
    /// <summary>The top of the fear meter.</summary>
    public const float MaxFear = 100f;

    /// <summary>From here stamina comes back at half the pace.</summary>
    public const float Afraid = 25f;

    /// <summary>From here the heart pounds and a shadow may come.</summary>
    public const float Terrified = 50f;

    /// <summary>From here the legs fail.</summary>
    public const float Panic = 75f;

    /// <summary>The player's lit flag in their network data, read by the shadows.</summary>
    public const string UnlitKey = "WhiteHilt_DarkUnlit";

    private const float TickSeconds = 0.25f;
    private const float HeartbeatSeconds = 20f;
    private const float FireReach = 6f;
    private const float FriendReach = 5f;
    private const float FirePlaceRefresh = 2f;
    private const float GutterSeconds = 1.5f;
    private const float GutterChance = 0.35f;

    private static readonly List<Fireplace> fires = new();
    private static float nextFireRefresh;

    private float nextTick;
    private float lastTick;
    private float nextHeartbeat;
    private float nextGutter;
    private string askedKey;
    private DarkPhase shownPhase;
    private int shownStage;
    private float owed;
    private bool spawnedThisDarkness;

    /// <summary>The key of the place below ground the local player is in, or null.</summary>
    public static string PlaceKey { get; private set; }

    /// <summary>The darkness where the local player is now.</summary>
    public static DarkPhase Phase { get; private set; }

    /// <summary>How dark it is where the local player is, 0 to 1.</summary>
    public static float Level { get; private set; }

    /// <summary>Whether the local player has a light.</summary>
    public static bool Lit { get; private set; }

    /// <summary>How quiet the place is, 0 to 1 (<see cref="DarknessSchedule.Silence"/>).</summary>
    public static float Silence { get; private set; }

    /// <summary>How far the darkness has crept into the place, 0 to 1 (<see cref="DarknessSchedule.Creep"/>).</summary>
    public static float Creep { get; private set; }

    /// <summary>The local player's fear, 0 to 100.</summary>
    public static float Fear { get; private set; }

    /// <summary>Until when the local player's torch is guttered out.</summary>
    public static float GutterUntil { get; private set; }

    /// <summary>
    /// The timing of the darkness from the settings.
    /// </summary>
    /// <returns>The timing.</returns>
    public static DarknessSchedule.Timing Timing()
    {
        return new DarknessSchedule.Timing(DarknessSettings.CalmMinutes.Value * 60f, DarknessSettings.LongestGapMinutes.Value * 60f,
            DarknessSettings.ShortestGapMinutes.Value * 60f, DarknessSettings.RampMinutes.Value * 60f,
            DarknessSettings.ShortestBlackSeconds.Value, DarknessSettings.LongestBlackSeconds.Value);
    }

    /// <summary>
    /// How dark a place is now, as far as this machine knows.
    /// </summary>
    /// <param name="key">The place.</param>
    /// <returns>0 to 1; 0 for a place the server has not spoken of.</returns>
    public static float LevelOf(string key)
    {
        if (string.IsNullOrEmpty(key) || ZNet.instance == null || DarknessSettings.Enabled == null || !DarknessSettings.Enabled.Value
            || !DarknessService.TryGetStart(key, out double start))
        {
            return 0f;
        }

        DarkPhase phase = DarknessSchedule.At(ZNet.instance.GetTimeSeconds() - start, DarknessSchedule.Seed(key), Timing(), out float into, out float length);
        return DarknessSchedule.Level(phase, into, length, Time.time);
    }

    /// <summary>
    /// The place below ground a position is in: a dungeon, a cave or anything else the game counts as inside, or a
    /// troll cave.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The place's key, or null outside.</returns>
    public static string FindPlace(Player player)
    {
        bool cave = EnvMan.instance != null && EnvMan.instance.GetCurrentEnvironment() is EnvSetup env && env.m_name != null
            && env.m_name.IndexOf("Cave", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (player == null || (!player.InInterior() && !cave))
        {
            return null;
        }

        Location location = Location.GetLocation(player.transform.position);
        Vector3 at = location != null ? location.transform.position : player.transform.position;
        return location != null
            ? $"{Mathf.RoundToInt(at.x)}:{Mathf.RoundToInt(at.z)}"
            : $"{Mathf.RoundToInt(at.x / 64f)}:{Mathf.RoundToInt(at.z / 64f)}:area";
    }

    /// <summary>
    /// Whether a player holds a light: a torch or anything else alight in hand, a circlet or another light on the head.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True with a light.</returns>
    public static bool CarriesLight(Player player)
    {
        VisEquipment equipment = player != null ? player.m_visEquipment : null;
        return equipment != null && (HasLight(equipment.m_rightItemInstance) || HasLight(equipment.m_leftItemInstance) || HasLight(equipment.m_helmetItemInstance));
    }

    private static bool HasLight(GameObject instance)
    {
        if (instance == null)
        {
            return false;
        }

        foreach (Light light in instance.GetComponentsInChildren<Light>(true))
        {
            if (light != null && (light.enabled || Time.time < GutterUntil))
            {
                return true;
            }
        }

        return false;
    }

    // A light of the player's own, of a friend close by, or a fire the players built close by.
    private static bool IsLit(Player player)
    {
        if (CarriesLight(player))
        {
            return true;
        }

        foreach (Player other in Player.GetAllPlayers())
        {
            if (other != player && other != null && !other.IsDead()
                && Vector3.Distance(other.transform.position, player.transform.position) <= FriendReach && CarriesLight(other))
            {
                return true;
            }
        }

        if (Time.time >= nextFireRefresh)
        {
            nextFireRefresh = Time.time + FirePlaceRefresh;
            fires.Clear();
            foreach (Fireplace fire in FindObjectsByType<Fireplace>(FindObjectsSortMode.None))
            {
                Piece piece = fire != null ? fire.GetComponent<Piece>() : null;
                if (piece != null && piece.IsPlacedByPlayer())
                {
                    fires.Add(fire);
                }
            }
        }

        foreach (Fireplace fire in fires)
        {
            if (fire != null && fire.IsBurning() && Vector3.Distance(fire.transform.position, player.transform.position) <= FireReach)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The fear after some time: it grows in the black without a light, holds while the dark is coming or going,
    /// falls slowly when the place is lit again and twice as fast with a light of your own.
    /// </summary>
    /// <param name="fear">The fear now.</param>
    /// <param name="seconds">Seconds that passed.</param>
    /// <param name="black">Whether it is black (or near enough) where the player is.</param>
    /// <param name="calm">Whether the place is lit as ever.</param>
    /// <param name="lit">Whether the player has a light.</param>
    /// <param name="secondsToFull">Seconds in the black without a light to full fear.</param>
    /// <returns>The new fear, 0 to <see cref="MaxFear"/>.</returns>
    public static float StepFear(float fear, float seconds, bool black, bool calm, bool lit, float secondsToFull)
    {
        float rate = MaxFear / Mathf.Max(1f, secondsToFull);
        float change = lit ? -rate * 2f : black ? rate : calm ? -rate : 0f;
        return Mathf.Clamp(fear + change * seconds, 0f, MaxFear);
    }

    /// <summary>
    /// The step of the fear meter.
    /// </summary>
    /// <param name="fear">The fear.</param>
    /// <returns>0 below <see cref="Afraid"/>, up to 4 at full fear.</returns>
    public static int Stage(float fear)
    {
        return fear >= MaxFear ? 4 : fear >= Panic ? 3 : fear >= Terrified ? 2 : fear >= Afraid ? 1 : 0;
    }

    private void Start()
    {
        PlaceKey = null;
        Phase = DarkPhase.Calm;
        Level = 0f;
        Fear = 0f;
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (player == null || ZNet.instance == null || DarknessSettings.Enabled == null)
        {
            return;
        }

        UpdateLevel();
        if (Time.time >= nextTick)
        {
            float seconds = Mathf.Clamp(Time.time - lastTick, 0f, 2f);
            lastTick = Time.time;
            nextTick = Time.time + TickSeconds;
            Tick(player, seconds);
        }
    }

    // How dark it is, every frame, so the flicker is smooth.
    private static void UpdateLevel()
    {
        if (PlaceKey == null || !DarknessSettings.Enabled.Value || !DarknessService.TryGetStart(PlaceKey, out double start))
        {
            Phase = DarkPhase.Calm;
            Level = Mathf.MoveTowards(Level, 0f, Time.deltaTime);
            Silence = Mathf.MoveTowards(Silence, 0f, Time.deltaTime);
            return;
        }

        double elapsed = ZNet.instance.GetTimeSeconds() - start;
        DarknessSchedule.Timing timing = Timing();
        Phase = DarknessSchedule.At(elapsed, DarknessSchedule.Seed(PlaceKey), timing, out float into, out float length);
        Level = DarknessSchedule.Level(Phase, into, length, Time.time);
        Silence = DarknessSchedule.Silence(Phase, into, length);
        Creep = DarknessSchedule.Creep(elapsed, timing);
    }

    private void Tick(Player player, float seconds)
    {
        string key = player.IsDead() ? null : FindPlace(player);
        if (key != PlaceKey)
        {
            PlaceKey = key;
            askedKey = null;
            spawnedThisDarkness = false;
        }

        // Ask the server when this place was first gone into, and tell it now and then that someone is still here.
        if (key != null && (askedKey != key || Time.time >= nextHeartbeat))
        {
            askedKey = key;
            nextHeartbeat = Time.time + HeartbeatSeconds;
            DarknessService.Inside(key);
        }

        Lit = key != null && IsLit(player);
        bool black = Level >= 0.85f;
        Fear = key == null || player.IsDead() ? 0f : StepFear(Fear, seconds, black, Phase == DarkPhase.Calm, Lit, DarknessSettings.FearSeconds.Value);
        SetUnlit(player, key != null && Level >= 0.5f && !Lit);

        Announce(player);
        UpdateEffect(player);
        WarnOfStage(player);
        Hurt(player, seconds);
        WearTorch(player, seconds);
        Haunt(player);
    }

    // Tells the player when the lights flicker, when the dark falls and when it lifts.
    private void Announce(Player player)
    {
        if (Phase == shownPhase)
        {
            return;
        }

        DarkPhase was = shownPhase;
        shownPhase = Phase;
        switch (Phase)
        {
            case DarkPhase.Flicker:
                player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_dark_flicker");
                DarknessSounds.Play(DarknessSounds.Gutter, player.transform.position + Vector3.up * 2f);
                break;
            case DarkPhase.Black:
                player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_dark_falls");
                DarknessSounds.Play(DarknessSounds.Out, player.transform.position + Vector3.up * 2f);
                break;
            case DarkPhase.Returning:
                player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_dark_lifts");
                DarknessSounds.Play(DarknessSounds.Return, player.transform.position + Vector3.up * 2f);
                break;
            case DarkPhase.Calm when was != DarkPhase.Calm:
                spawnedThisDarkness = false;
                break;
        }
    }

    private static void SetUnlit(Player player, bool unlit)
    {
        ZDO zdo = player.m_nview != null && player.m_nview.IsValid() ? player.m_nview.GetZDO() : null;
        if (zdo != null && zdo.GetBool(UnlitKey) != unlit)
        {
            zdo.Set(UnlitKey, unlit);
        }
    }

    private static void UpdateEffect(Player player)
    {
        SEMan seman = player.GetSEMan();
        FearEffect template = FearEffect.Template;
        if (seman == null || template == null)
        {
            return;
        }

        bool has = seman.HaveStatusEffect(template.NameHash());
        if (Fear > 0.5f && !has)
        {
            seman.AddStatusEffect(template);
        }
        else if (Fear <= 0.5f && has)
        {
            seman.RemoveStatusEffect(template.NameHash(), quiet: true);
        }
    }

    private void WarnOfStage(Player player)
    {
        int stage = Stage(Fear);
        if (stage <= shownStage)
        {
            shownStage = Mathf.Min(shownStage, stage);
            return;
        }

        shownStage = stage;
        string token = stage switch
        {
            1 => "$msg_whitehilt_dark_afraid",
            3 => "$msg_whitehilt_dark_panic",
            4 when DarknessSettings.FearDamage.Value > 0f => "$msg_whitehilt_dark_dying",
            _ => null
        };
        if (token != null)
        {
            player.Message(MessageHud.MessageType.Center, token);
        }
    }

    // At full fear health drains away, gathered until it is at least 1 as the game ignores the smallest hits.
    private void Hurt(Player player, float seconds)
    {
        if (Fear < MaxFear || player.InGodMode() || player.IsDead() || DarknessSettings.FearDamage.Value <= 0f)
        {
            owed = 0f;
            return;
        }

        owed += player.GetMaxHealth() * DarknessSettings.FearDamage.Value * seconds;
        if (owed < 1f)
        {
            return;
        }

        HitData hit = new() { m_point = player.GetCenterPoint() };
        hit.m_damage.m_damage = owed;
        owed = 0f;
        player.ApplyDamage(hit, showDamageText: false, triggerEffects: false);
    }

    // In the black a torch burns down faster and now and then gutters out for a moment.
    private void WearTorch(Player player, float seconds)
    {
        ItemDrop.ItemData torch = player.GetRightItem() ?? player.GetLeftItem();
        if (Level < 0.5f || torch?.m_shared == null || !torch.m_shared.m_useDurability || torch.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch)
        {
            return;
        }

        torch.m_durability = Mathf.Max(0f, torch.m_durability
            - torch.m_shared.m_durabilityDrain * seconds * Game.m_durabilityRate * Mathf.Max(0f, DarknessSettings.TorchWear.Value - 1f));
        if (Phase == DarkPhase.Black && Time.time >= nextGutter)
        {
            nextGutter = Time.time + Random.Range(12f, 20f);
            if (Random.value < GutterChance)
            {
                GutterUntil = Time.time + GutterSeconds;
                player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_dark_gutter");
            }
        }
    }

    // From half fear, in the black without a light, once a darkness a shadow comes for the player.
    private void Haunt(Player player)
    {
        if (!DarknessSettings.Shadows.Value || spawnedThisDarkness || Phase != DarkPhase.Black || Lit || Fear < Terrified || player.IsDead())
        {
            return;
        }

        spawnedThisDarkness = DarkShadow.Summon(player, PlaceKey);
    }
}
