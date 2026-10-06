using BepInEx.Configuration;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Weapons;
using BrudvikWhiteHilt.Items.Weapons.WhiteHiltStaffNecromancy;
using BrudvikWhiteHilt.Progression;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.Necromancy;

/// <summary>
/// Waking a fallen friend at their grave with the Necromancer's Staff. The game has no state between living and dead: a
/// fallen player wakes at their bed. So the staff, held at their gravestone, offers to bring them back to it: if they
/// agree, the necromancer stands still a few seconds while a green flame rises from the grave, pays for it in their own
/// health, stamina and eitr, and the friend is brought to the grave with their gear back and the skills the death took.
/// </summary>
public static class Raising
{
    private const string Section = "Necromancy";

    /// <summary>Saved on a new gravestone: when its owner died, in ticks of the world clock.</summary>
    private const string DiedKey = "whitehilt_died_at";

    /// <summary>Saved on a new gravestone: its owner's skills as they were before the death took its share.</summary>
    private const string SkillsKey = "whitehilt_died_skills";

    // Saved on the necromancer: when the staff may wake someone again, in ticks of the world clock.
    private const string ReadyKey = "whitehilt_raise_ready";

    private const string OfferRpc = "WhiteHilt_RaiseOffer";
    private const string AnswerRpc = "WhiteHilt_RaiseAnswer";
    private const string RaiseRpc = "WhiteHilt_Raise";
    private const string EffectRpc = "WhiteHilt_RaiseEffect";
    private const string PriceEffectName = "WhiteHiltDeathsPrice";

    // How near the grave the necromancer must stand, and how much later than the friend's answer the offer lapses.
    private const float Reach = 6f;
    private const float AnswerGrace = 5f;

    private static SE_Stats priceEffect;
    private static string pendingSkills;
    private static Offer pending;
    private static readonly HashSet<ZDOID> risen = new();

    /// <summary>Share of the necromancer's maximum health a waking takes.</summary>
    public static ConfigEntry<float> HealthCostPercent { get; private set; }

    /// <summary>Eitr a waking takes.</summary>
    public static ConfigEntry<float> EitrCost { get; private set; }

    /// <summary>Whether a waking empties the necromancer's stamina.</summary>
    public static ConfigEntry<bool> DrainStamina { get; private set; }

    /// <summary>Minutes after a death within which its grave can be woken.</summary>
    public static ConfigEntry<float> WindowMinutes { get; private set; }

    /// <summary>Minutes before the staff can wake someone again.</summary>
    public static ConfigEntry<float> CooldownMinutes { get; private set; }

    /// <summary>Minutes Death's Price lasts.</summary>
    public static ConfigEntry<float> PriceMinutes { get; private set; }

    /// <summary>Seconds the necromancer must stand still.</summary>
    public static ConfigEntry<float> ChannelSeconds { get; private set; }

    /// <summary>Whether the woken get back the skills the death took.</summary>
    public static ConfigEntry<bool> RestoreSkills { get; private set; }

    /// <summary>Whether the fallen are asked first.</summary>
    public static ConfigEntry<bool> AskFirst { get; private set; }

    /// <summary>Seconds the fallen have to answer.</summary>
    public static ConfigEntry<float> AnswerSeconds { get; private set; }

    /// <summary>
    /// Binds the config entries, adds the texts and the Death's Price effect. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        HealthCostPercent = WhiteHiltConfig.BindAdminOnly(Section, "HealthCostPercent", 50f,
            "Share of your maximum health, in percent, that waking a fallen friend with the Necromancer's Staff takes. It never kills you: with too little health the staff refuses.",
            new AcceptableValueRange<float>(0f, 95f));
        EitrCost = WhiteHiltConfig.BindAdminOnly(Section, "EitrCost", 60f, "Eitr a waking takes.", new AcceptableValueRange<float>(0f, 500f));
        DrainStamina = WhiteHiltConfig.BindAdminOnly(Section, "DrainStamina", true, "A waking empties your stamina.");
        WindowMinutes = WhiteHiltConfig.BindAdminOnly(Section, "WindowMinutes", 10f, "Minutes after a death within which its grave can be woken.",
            new AcceptableValueRange<float>(1f, 120f));
        CooldownMinutes = WhiteHiltConfig.BindAdminOnly(Section, "CooldownMinutes", 5f, "Minutes before the staff can wake someone again.",
            new AcceptableValueRange<float>(0f, 60f));
        PriceMinutes = WhiteHiltConfig.BindAdminOnly(Section, "PriceMinutes", 3f,
            "Minutes of Death's Price after a waking: no health regeneration and half stamina regeneration.", new AcceptableValueRange<float>(0f, 30f));
        ChannelSeconds = WhiteHiltConfig.BindAdminOnly(Section, "ChannelSeconds", 4f,
            "Seconds you must stand still at the grave; moving or being hit breaks the waking, and nothing is paid.", new AcceptableValueRange<float>(1f, 20f));
        RestoreSkills = WhiteHiltConfig.BindAdminOnly(Section, "RestoreSkills", true, "The woken get back the skills the death took.");
        AskFirst = WhiteHiltConfig.BindAdminOnly(Section, "AskFirst", true, "The fallen are asked before they are woken and brought to their grave.");
        AnswerSeconds = WhiteHiltConfig.BindAdminOnly(Section, "AnswerSeconds", 30f, "Seconds the fallen have to answer.",
            new AcceptableValueRange<float>(5f, 120f));

        Translations.AddEnglish("whitehilt_raise_hover", "Wake {0} (costs {1}% of your health)");
        Translations.AddEnglish("whitehilt_raise_unmarked", "This grave is too old to wake");
        Translations.AddEnglish("whitehilt_raise_too_late", "Too long dead to wake");
        Translations.AddEnglish("whitehilt_raise_cooldown", "The staff must rest {0} more");
        Translations.AddEnglish("whitehilt_raise_no_life", "You have no life to give");
        Translations.AddEnglish("whitehilt_raise_no_eitr", "You lack the eitr");
        Translations.AddEnglish("whitehilt_raise_busy", "You are already waking someone");
        Translations.AddEnglish("whitehilt_raise_calling", "Calling to {0}...");
        Translations.AddEnglish("whitehilt_raise_no_answer", "{0} does not answer");
        Translations.AddEnglish("whitehilt_raise_declined", "{0} will not be woken");
        Translations.AddEnglish("whitehilt_raise_channel", "Waking {0}: stand still");
        Translations.AddEnglish("whitehilt_raise_broken", "The waking is broken");
        Translations.AddEnglish("whitehilt_raise_done", "{0} rises from the grave");
        Translations.AddEnglish("whitehilt_raise_offer_header", "Wake at your grave");
        Translations.AddEnglish("whitehilt_raise_offer", "{0} would wake you at your grave, with your gear and the skills you lost. Go?");
        Translations.AddEnglish("whitehilt_raised", "{0} woke you at your grave");
        Translations.AddEnglish("whitehilt_deaths_price", "Death's Price");
        Translations.AddEnglish("whitehilt_deaths_price_tooltip", "You gave of your own life to wake the dead: no health regeneration, and stamina comes back at half speed.");

        priceEffect = ScriptableObject.CreateInstance<SE_Stats>();
        priceEffect.name = PriceEffectName;
        priceEffect.m_name = Translations.Token("whitehilt_deaths_price");
        priceEffect.m_tooltip = Translations.Token("whitehilt_deaths_price_tooltip");
        priceEffect.m_healthRegenMultiplier = 0f;
        priceEffect.m_staminaRegenMultiplier = 0.5f;
        priceEffect.m_ttl = PriceMinutes.Value * 60f;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(priceEffect, fixReference: false));
    }

    /// <summary>
    /// Registers the routed RPCs. Call once per game session.
    /// </summary>
    public static void RegisterRpcs()
    {
        ZRoutedRpc.instance?.Register<long, ZDOID, string>(OfferRpc, RPC_Offer);
        ZRoutedRpc.instance?.Register<ZDOID, bool>(AnswerRpc, RPC_Answer);
        ZRoutedRpc.instance?.Register<long, ZDOID, Vector3, string>(RaiseRpc, RPC_Raise);
        ZRoutedRpc.instance?.Register<Vector3, ZDOID, float>(EffectRpc, RPC_Effect);
    }

    /// <summary>
    /// Remembers the local player's skills as they die, before the death lowers them.
    /// </summary>
    /// <param name="player">The dying player.</param>
    public static void BeforeDeath(Player player)
    {
        pendingSkills = player == Player.m_localPlayer ? WriteSkills(player.GetSkills()) : null;
    }

    /// <summary>
    /// Marks a new gravestone of the local player with the time of death and the skills before it.
    /// </summary>
    /// <param name="tomb">The new gravestone.</param>
    /// <param name="owner">Its owner's player ID.</param>
    public static void GraveMade(TombStone tomb, long owner)
    {
        ZDO zdo = tomb.m_nview != null && tomb.m_nview.IsValid() ? tomb.m_nview.GetZDO() : null;
        if (zdo == null || Player.m_localPlayer == null || owner != Player.m_localPlayer.GetPlayerID() || ZNet.instance == null)
        {
            return;
        }

        zdo.Set(DiedKey, ZNet.instance.GetTime().Ticks);
        if (pendingSkills != null)
        {
            zdo.Set(SkillsKey, pendingSkills);
            pendingSkills = null;
        }
    }

    /// <summary>
    /// What the staff can do at a gravestone, for its hover text, or null if it does nothing there.
    /// </summary>
    /// <param name="tomb">The gravestone.</param>
    /// <returns>The line to add.</returns>
    public static string HoverLine(TombStone tomb)
    {
        Player player = Player.m_localPlayer;
        if (!WhiteHiltStaffNecromancy.IsHeldBy(player) || tomb.IsOwner())
        {
            return null;
        }

        string problem = Problem(player, tomb);
        if (problem != null)
        {
            return $"\n<color=#9AFFA0>{problem}</color>";
        }

        string wake = string.Format(Localize("whitehilt_raise_hover"), tomb.GetOwnerName(), Mathf.RoundToInt(HealthCostPercent.Value));
        return Localization.instance.Localize($"\n[<color=yellow><b>$KEY_Use</b></color>] ") + wake;
    }

    /// <summary>
    /// Begins waking a gravestone's owner, when the local player holds the staff at a grave not their own.
    /// </summary>
    /// <param name="tomb">The gravestone.</param>
    /// <param name="player">The local player.</param>
    /// <returns>True if the staff took the use; false to open the grave as usual.</returns>
    public static bool TryBegin(TombStone tomb, Player player)
    {
        if (player != Player.m_localPlayer || !WhiteHiltStaffNecromancy.IsHeldBy(player) || tomb.IsOwner())
        {
            return false;
        }

        string problem = Problem(player, tomb);
        if (problem != null)
        {
            player.Message(MessageHud.MessageType.Center, problem);
            return true;
        }

        ZDOID grave = tomb.m_nview.GetZDO().m_uid;
        pending = new Offer(grave, tomb.GetOwner(), tomb.GetOwnerName(), tomb.transform.position, Time.time + AnswerSeconds.Value + AnswerGrace);
        player.Message(MessageHud.MessageType.Center, string.Format(Localize("whitehilt_raise_calling"), pending.Name));
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, OfferRpc, pending.Owner, grave, player.GetPlayerName());
        return true;
    }

    /// <summary>
    /// Lets an unanswered offer lapse. Call every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Update(Player player)
    {
        if (pending != null && !pending.Accepted && Time.time > pending.Until)
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Localize("whitehilt_raise_no_answer"), pending.Name));
            pending = null;
        }
    }

    // Why the local player cannot wake this grave now, or null if they can.
    private static string Problem(Player player, TombStone tomb)
    {
        ZDO zdo = tomb.m_nview != null && tomb.m_nview.IsValid() ? tomb.m_nview.GetZDO() : null;
        long died = zdo?.GetLong(DiedKey) ?? 0L;
        if (died == 0L || ZNet.instance == null)
        {
            return Localize("whitehilt_raise_unmarked");
        }

        DateTime now = ZNet.instance.GetTime();
        if ((now - new DateTime(died)).TotalMinutes > WindowMinutes.Value)
        {
            return Localize("whitehilt_raise_too_late");
        }

        if (pending != null || player.GetComponent<RaiseChannel>() != null)
        {
            return Localize("whitehilt_raise_busy");
        }

        if (player.m_customData.TryGetValue(ReadyKey, out string saved) && long.TryParse(saved, out long ready) && ready > now.Ticks)
        {
            TimeSpan left = new DateTime(ready) - now;
            return string.Format(Localize("whitehilt_raise_cooldown"), left.TotalSeconds >= 60 ? $"{Mathf.CeilToInt((float)left.TotalMinutes)} min" : $"{Mathf.CeilToInt((float)left.TotalSeconds)} s");
        }

        if (player.GetHealth() <= HealthCost(player) + 1f)
        {
            return Localize("whitehilt_raise_no_life");
        }

        return EitrCost.Value > 0f && !player.HaveEitr(EitrCost.Value) ? Localize("whitehilt_raise_no_eitr") : null;
    }

    private static float HealthCost(Player player)
    {
        return player.GetMaxHealth() * HealthCostPercent.Value / 100f;
    }

    // On every machine: the fallen one, if it is this player, is asked.
    private static void RPC_Offer(long sender, long owner, ZDOID grave, string necromancer)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.GetPlayerID() != owner || player.IsDead())
        {
            return;
        }

        if (!AskFirst.Value)
        {
            Answer(sender, grave, true);
            return;
        }

        UnifiedPopup.Push(new YesNoPopup(Localize("whitehilt_raise_offer_header"), string.Format(Localize("whitehilt_raise_offer"), necromancer),
            () => { UnifiedPopup.Pop(); Answer(sender, grave, true); },
            () => { UnifiedPopup.Pop(); Answer(sender, grave, false); }, localizeText: false));
    }

    private static void Answer(long necromancer, ZDOID grave, bool yes)
    {
        ZRoutedRpc.instance.InvokeRoutedRPC(necromancer, AnswerRpc, grave, yes);
    }

    // On the necromancer's machine: the fallen one's answer.
    private static void RPC_Answer(long sender, ZDOID grave, bool yes)
    {
        Player player = Player.m_localPlayer;
        if (player == null || pending == null || pending.Grave != grave || pending.Accepted)
        {
            return;
        }

        if (!yes)
        {
            player.Message(MessageHud.MessageType.Center, string.Format(Localize("whitehilt_raise_declined"), pending.Name));
            pending = null;
            return;
        }

        if (player.IsDead() || Vector3.Distance(player.transform.position, pending.Position) > Reach)
        {
            player.Message(MessageHud.MessageType.Center, Localize("whitehilt_raise_broken"));
            pending = null;
            return;
        }

        pending.Accepted = true;
        player.gameObject.AddComponent<RaiseChannel>();
        player.Message(MessageHud.MessageType.Center, string.Format(Localize("whitehilt_raise_channel"), pending.Name));
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, EffectRpc, pending.Position, player.GetZDOID(), ChannelSeconds.Value);
    }

    /// <summary>
    /// Ends a waking: paid and sent if it ran its time, otherwise broken and free.
    /// </summary>
    /// <param name="player">The necromancer.</param>
    /// <param name="done">Whether the necromancer stood still to the end.</param>
    internal static void Finish(Player player, bool done)
    {
        Offer offer = pending;
        pending = null;
        if (offer == null)
        {
            return;
        }

        if (!done)
        {
            player.Message(MessageHud.MessageType.Center, Localize("whitehilt_raise_broken"));
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, EffectRpc, offer.Position, player.GetZDOID(), 0f);
            return;
        }

        player.SetHealth(Mathf.Max(1f, player.GetHealth() - HealthCost(player)));
        if (DrainStamina.Value)
        {
            player.UseStamina(player.GetMaxStamina());
        }

        if (EitrCost.Value > 0f)
        {
            player.UseEitr(EitrCost.Value);
        }

        if (PriceMinutes.Value > 0f)
        {
            priceEffect.m_ttl = PriceMinutes.Value * 60f;
            priceEffect.m_icon ??= PrefabManager.Instance.GetPrefab(WhiteHiltStaffNecromancy.PrefabName)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            player.GetSEMan().AddStatusEffect(priceEffect.NameHash(), resetTime: true);
        }

        DateTime ready = ZNet.instance.GetTime().AddMinutes(CooldownMinutes.Value);
        player.m_customData[ReadyKey] = ready.Ticks.ToString(CultureInfo.InvariantCulture);
        player.Message(MessageHud.MessageType.Center, string.Format(Localize("whitehilt_raise_done"), offer.Name));
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RaiseRpc, offer.Owner, offer.Grave, offer.Position, player.GetPlayerName());
    }

    // On every machine: the fallen one, if it is this player, is brought to the grave.
    private static void RPC_Raise(long sender, long owner, ZDOID grave, Vector3 position, string necromancer)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.GetPlayerID() != owner || player.IsDead() || !risen.Add(grave))
        {
            return;
        }

        // Beside the stone, not inside it.
        Vector3 beside = position + Vector3.ProjectOnPlane(player.transform.position - position, Vector3.up).normalized * 1.2f + Vector3.up * 0.3f;
        if (Vector3.Distance(beside, position) < 0.5f)
        {
            beside = position + new Vector3(1.2f, 0.3f, 0f);
        }

        player.TeleportTo(beside, Quaternion.LookRotation(Vector3.ProjectOnPlane(position - beside, Vector3.up).normalized + Vector3.forward * 0.001f), distantTeleport: true);
        player.gameObject.AddComponent<RiseAtGrave>().Begin(grave, necromancer);
    }

    /// <summary>
    /// Once the woken player stands at the grave: gives back the skills the death took and takes the gear from it.
    /// </summary>
    /// <param name="player">The woken player.</param>
    /// <param name="tomb">The gravestone.</param>
    /// <param name="necromancer">Who woke them.</param>
    internal static void Arrived(Player player, TombStone tomb, string necromancer)
    {
        ZDO zdo = tomb.m_nview != null && tomb.m_nview.IsValid() ? tomb.m_nview.GetZDO() : null;
        if (RestoreSkills.Value && zdo != null)
        {
            ReadSkills(player.GetSkills(), zdo.GetString(SkillsKey));
        }

        player.Message(MessageHud.MessageType.Center, string.Format(Localize("whitehilt_raised"), necromancer));
        tomb.Interact(player, false, false);
    }

    // On every machine: the green flame over the grave and the band to the necromancer, or its end.
    private static void RPC_Effect(long sender, Vector3 grave, ZDOID necromancer, float seconds)
    {
        if (ZNet.instance != null && ZNet.instance.IsDedicated())
        {
            return;
        }

        RaiseEffect.Play(grave, necromancer, seconds);
    }

    private static string WriteSkills(Skills skills)
    {
        if (skills == null)
        {
            return null;
        }

        StringBuilder text = new();
        foreach (KeyValuePair<Skills.SkillType, Skills.Skill> pair in skills.m_skillData)
        {
            text.Append((int)pair.Key).Append(':')
                .Append(pair.Value.m_level.ToString("R", CultureInfo.InvariantCulture)).Append(':')
                .Append(pair.Value.m_accumulator.ToString("R", CultureInfo.InvariantCulture)).Append(';');
        }

        return text.ToString();
    }

    // Raises each skill back to its level before the death; one raised since keeps its new level.
    private static void ReadSkills(Skills skills, string text)
    {
        if (skills == null || string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (string entry in text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = entry.Split(':');
            if (fields.Length < 3 || !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)
                || !float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float level)
                || !float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float accumulator))
            {
                continue;
            }

            Skills.Skill skill = skills.GetSkill((Skills.SkillType)type);
            if (skill != null && skill.m_level < level)
            {
                skill.m_level = level;
                skill.m_accumulator = accumulator;
            }
        }
    }

    private static string Localize(string key)
    {
        return Localization.instance.Localize("$" + key);
    }

    // A waking asked for, from the necromancer's side.
    private sealed class Offer
    {
        public Offer(ZDOID grave, long owner, string name, Vector3 position, float until)
        {
            Grave = grave;
            Owner = owner;
            Name = name;
            Position = position;
            Until = until;
        }

        public ZDOID Grave { get; }

        public long Owner { get; }

        public string Name { get; }

        public Vector3 Position { get; }

        public float Until { get; }

        public bool Accepted { get; set; }
    }

    /// <summary>The position of the grave being woken, for the channel's reach check.</summary>
    internal static Vector3? PendingGrave => pending?.Position;
}

/// <summary>
/// The necromancer standing still at the grave while the waking runs; moving away, being hit or dying breaks it.
/// </summary>
internal sealed class RaiseChannel : MonoBehaviour
{
    private const float MoveTolerance = 0.6f;

    private Player player;
    private Vector3 start;
    private float health;
    private float until;

    private void Start()
    {
        player = GetComponent<Player>();
        start = transform.position;
        health = player.GetHealth();
        until = Time.time + Raising.ChannelSeconds.Value;
    }

    // Runs the raising while the necromancer holds still; moving, being hurt or putting the staff away breaks it.
    private void Update()
    {
        Vector3? grave = Raising.PendingGrave;
        bool broken = player == null || player.IsDead() || grave == null
            || Vector3.Distance(transform.position, start) > MoveTolerance
            || player.GetHealth() < health - 0.5f
            || !WhiteHiltStaffNecromancy.IsHeldBy(player);
        if (broken || Time.time >= until)
        {
            if (player != null)
            {
                Raising.Finish(player, !broken);
            }

            Destroy(this);
            return;
        }

        health = Mathf.Max(health, player.GetHealth());
    }
}

/// <summary>
/// The woken player arriving at the grave: waits for the teleport and for the gravestone to load, then finishes.
/// </summary>
internal sealed class RiseAtGrave : MonoBehaviour
{
    private const float GiveUpSeconds = 30f;

    private ZDOID grave;
    private string necromancer;
    private float until;

    public void Begin(ZDOID tomb, string by)
    {
        grave = tomb;
        necromancer = by;
        until = Time.time + GiveUpSeconds;
    }

    // Waits for the raised player to arrive at their grave after the teleport, then finishes the raising there.
    private void Update()
    {
        Player player = GetComponent<Player>();
        if (player == null || Time.time > until)
        {
            Destroy(this);
            return;
        }

        if (player.IsTeleporting())
        {
            return;
        }

        TombStone tomb = ZNetScene.instance?.FindInstance(grave)?.GetComponent<TombStone>();
        if (tomb == null)
        {
            return;
        }

        Raising.Arrived(player, tomb, necromancer);
        Destroy(this);
    }
}

/// <summary>
/// The green flame rising from a grave while it is woken, and the band of green from the necromancer to it.
/// </summary>
internal sealed class RaiseEffect : MonoBehaviour
{
    private static readonly Dictionary<ZDOID, RaiseEffect> playing = new();

    private ZDOID necromancer;
    private Transform caster;
    private LineRenderer band;
    private Light glow;
    private float started;
    private float until;

    /// <summary>
    /// Starts the effect at a grave for a time, or ends the one there with no time.
    /// </summary>
    public static void Play(Vector3 grave, ZDOID by, float seconds)
    {
        if (playing.TryGetValue(by, out RaiseEffect running) && running != null)
        {
            Destroy(running.gameObject);
        }

        playing.Remove(by);
        if (seconds <= 0f)
        {
            return;
        }

        GameObject root = new("WhiteHiltRaiseEffect");
        root.transform.position = grave;
        RaiseEffect effect = root.AddComponent<RaiseEffect>();
        effect.necromancer = by;
        effect.started = Time.time;
        effect.until = Time.time + seconds;
        effect.Build();
        playing[by] = effect;
    }

    // Builds the effect at the grave: a column of green flames, a light and a band from the necromancer to the grave.
    private void Build()
    {
        // A column of three flames, each larger than the staff's.
        for (int i = 0; i < 3; i++)
        {
            GameObject level = new("flame" + i);
            level.transform.SetParent(transform, false);
            level.transform.localPosition = Vector3.up * (0.3f + i * 0.7f);
            level.transform.localScale = Vector3.one * (3.2f - i * 0.6f);
            StaffFlame.CreateFlame(WhiteHiltStaffNecromancy.Green, level.transform);
        }

        glow = new GameObject("light").AddComponent<Light>();
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = Vector3.up * 1.2f;
        glow.type = LightType.Point;
        glow.color = WhiteHiltStaffNecromancy.Green;
        glow.range = 8f;
        glow.shadows = LightShadows.None;

        band = gameObject.AddComponent<LineRenderer>();
        band.positionCount = 2;
        band.startWidth = 0.05f;
        band.endWidth = 0.18f;
        band.startColor = WhiteHiltStaffNecromancy.Green;
        band.endColor = new Color(0.6f, 1f, 0.65f, 0.8f);
        ParticleSystemRenderer flameRenderer = GetComponentInChildren<ParticleSystemRenderer>();
        if (flameRenderer != null)
        {
            band.sharedMaterial = flameRenderer.sharedMaterial;
        }
    }

    // Swells the light as the raising goes on, keeps the band between the necromancer and the grave, and removes itself
    // when done.
    private void Update()
    {
        if (Time.time >= until)
        {
            Destroy(gameObject);
            return;
        }

        if (caster == null)
        {
            caster = ZNetScene.instance?.FindInstance(necromancer)?.transform;
        }

        // Swells over the waking.
        float progress = Mathf.Clamp01((Time.time - started) / Mathf.Max(0.1f, until - started));
        glow.intensity = 1.5f + 3f * progress + 0.5f * Mathf.Sin(Time.time * 9f);
        band.enabled = caster != null;
        if (caster != null)
        {
            band.SetPosition(0, caster.position + Vector3.up * 1.5f);
            band.SetPosition(1, transform.position + Vector3.up * 1.2f);
        }
    }

    private void OnDestroy()
    {
        if (playing.TryGetValue(necromancer, out RaiseEffect running) && running == this)
        {
            playing.Remove(necromancer);
        }
    }
}
