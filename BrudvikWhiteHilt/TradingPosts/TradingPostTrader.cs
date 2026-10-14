using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// A trading post's trader: the player body, dressed and coloured by the post's biome (<see cref="TraderLook"/>, kept in
/// the network data so every machine draws the same), with the game's <see cref="Trader"/> for the store window. Haldor's
/// trader behaviour drives an animator only Haldor has, so this component takes it over: it turns to the nearest player,
/// greets, talks now and then and says goodbye, with the player body's own emotes. It runs on every machine, as
/// Haldor's does; nothing of it is saved or sent.
/// </summary>
public class TradingPostTrader : MonoBehaviour
{
    /// <summary>Prefab name of the trader.</summary>
    public const string PrefabName = "WhiteHilt_Trader";

    private const float StandRange = 15f;
    private const float GreetRange = 5f;
    private const float ByeRange = 8f;
    private const float TalkInterval = 30f;
    private const float TurnSpeed = 120f;
    private const float MaxTurn = 75f;
    private const float DialogHeight = 2.1f;
    private const float DialogShowTime = 5f;

    // A soft warm light over the trader, so the trader is seen at night from the jetty.
    private const float GlowHeight = 2.6f;
    private const float GlowRange = 6f;
    private const float GlowIntensity = 1.1f;

    // Haldor's animator triggers and the emotes that stand in for them on the player body.
    private static readonly Dictionary<string, string> emotes = new()
    {
        { "Greet", "wave" }, { "Talk", "point" }, { "Buy", "thumbsup" }, { "Sell", "bow" }
    };

    private static readonly string[] talkEmotes = { "point", "shrug", "comehere", "thumbsup" };

    private ZNetView view;
    private Trader trader;
    private int biome;
    private string postId;
    private Animator animator;
    private HashSet<string> parameters;
    private float restYaw;
    private bool greeted;
    private float nextTalk;

    /// <summary>
    /// Gets the index of the trader's biome in <see cref="TradingPostGoods.Biomes"/>.
    /// </summary>
    public int Biome => biome;

    /// <summary>
    /// Gets the id of the trader's post.
    /// </summary>
    public string PostId => postId;

    /// <summary>
    /// Registers the dialogue and the names. Call from the plugin's Awake, before the game loads its languages.
    /// </summary>
    public static void RegisterTranslations()
    {
        Dialogue("greet", "Welcome ashore, traveller!", "Tie up your ship and see what I have.", "A ship! Come, come.");
        Dialogue("bye", "Fair winds!", "Come back when your hold is full.");
        Dialogue("talk", "Fish from every shore, that is what I buy.", "Bring me the goods of the next land over, and I pay well.",
            "No beast comes in under the dome. Trade in peace.", "The sea was kind to my boat this morning.");
        Dialogue("start", "Let us see what you have.", "Take your time, friend.");
        Dialogue("buy", "A fine choice.", "That will serve you well.");
        Dialogue("sell", "A fair price for fair goods.", "Good, I can sell that on.");
        Translations.AddEnglish("whitehilt_trader_closed", "I have nothing for you yet. Come back when the land beyond is open to you.");
        foreach (TraderLook look in TraderLooks.All)
        {
            Translations.AddEnglish(look.NameKey, $"{look.name} the Trader");
        }
    }

    /// <summary>
    /// Makes the trader prefab from the player's: the body, its equipment display and animator stay; everything that
    /// makes it a player (movement, physics, skills, sounds, network movement) goes. Call when the vanilla prefabs are
    /// available.
    /// </summary>
    public static void CreatePrefab()
    {
        GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, "Player");
        if (prefab == null)
        {
            Jotunn.Logger.LogError("Trading posts: the player prefab was not found, no traders.");
            return;
        }

        // A component another one requires cannot go before it, so the removal runs until nothing more goes.
        Transform visual = prefab.transform.Find("Visual");
        GameObject[] holders = new[] { prefab, visual != null ? visual.gameObject : null }.Where(holder => holder != null).ToArray();
        for (int round = 0; round < 4; round++)
        {
            foreach (MonoBehaviour behaviour in holders.SelectMany(holder => holder.GetComponents<MonoBehaviour>()))
            {
                if (behaviour is not ZNetView and not VisEquipment)
                {
                    Object.DestroyImmediate(behaviour);
                }
            }
        }

        Object.DestroyImmediate(prefab.GetComponent<Rigidbody>());
        Animator body = visual != null ? visual.GetComponent<Animator>() : null;
        if (body != null)
        {
            // The character moved itself by the animations' root motion; without it an emote would walk the body away.
            body.applyRootMotion = false;
        }
        prefab.layer = 0;
        ZNetView view = prefab.GetComponent<ZNetView>();
        view.m_persistent = true;
        view.m_syncInitialScale = false;

        Trader trader = prefab.AddComponent<Trader>();
        trader.m_name = "$" + TraderLooks.All[0].NameKey;
        trader.m_dialogHeight = DialogHeight;
        trader.m_hideDialogDelay = DialogShowTime;
        trader.m_randomTalkInterval = TalkInterval;
        trader.m_randomTalk = Lines("talk");
        trader.m_randomGreets = Lines("greet");
        trader.m_randomGoodbye = Lines("bye");
        trader.m_randomStartTrade = Lines("start");
        trader.m_randomBuy = Lines("buy");
        trader.m_randomSell = Lines("sell");
        trader.m_randomGiveItemNo = Lines("start");
        trader.m_randomUseItemAlreadyRecieved = Lines("start");
        if (visual != null)
        {
            visual.gameObject.AddComponent<LookAt>();
        }

        Light glow = new GameObject("glow").AddComponent<Light>();
        glow.transform.SetParent(prefab.transform, false);
        glow.transform.localPosition = new Vector3(0f, GlowHeight, 0.4f);
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.8f, 0.55f);
        glow.range = GlowRange;
        glow.intensity = GlowIntensity;
        glow.shadows = LightShadows.None;

        prefab.AddComponent<TradingPostTrader>();
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
    }

    /// <summary>
    /// Says a line over the trader's head and plays the emote that stands in for Haldor's animation.
    /// </summary>
    /// <param name="text">The line, a translation token or plain text.</param>
    /// <param name="trigger">Haldor's animator trigger, or an emote's own name.</param>
    public void Say(string text, string trigger)
    {
        // Opening the store of a biome the world has not reached yet: the trader says so instead.
        if (trigger == "Talk" && trader != null && trader.m_randomStartTrade.Contains(text) && Player.m_localPlayer != null
            && trader.GetAvailableItems().Count == 0)
        {
            text = "$whitehilt_trader_closed";
        }

        if (Chat.instance != null && !string.IsNullOrEmpty(text))
        {
            Chat.instance.SetNpcText(gameObject, Vector3.up * DialogHeight, 20f, DialogShowTime, string.Empty, text, false);
        }

        Emote(emotes.TryGetValue(trigger ?? string.Empty, out string emote) ? emote : trigger);
    }

    private static void Dialogue(string kind, params string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            Translations.AddEnglish($"whitehilt_trader_{kind}{i + 1}", lines[i]);
        }
    }

    private static List<string> Lines(string kind)
    {
        int count = kind switch { "greet" => 3, "talk" => 4, _ => 2 };
        return Enumerable.Range(1, count).Select(i => $"$whitehilt_trader_{kind}{i}").ToList();
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        trader = GetComponent<Trader>();
        animator = GetComponentInChildren<Animator>();
        parameters = animator != null ? new HashSet<string>(animator.parameters.Select(parameter => parameter.name)) : new HashSet<string>();
        restYaw = transform.eulerAngles.y;
        nextTalk = Time.time + TalkInterval;
        if (view != null && view.IsValid() && trader != null)
        {
            TraderLook look = TraderLooks.Get((Heightmap.Biome)view.GetZDO().GetInt(TradingPostKeys.Biome));
            trader.m_name = "$" + look.NameKey;
            biome = Mathf.Max(0, TradingPostGoods.IndexOf(look.biome));
            postId = view.GetZDO().GetString(TradingPostKeys.PostId);
            trader.m_items = TradingPostStore.BuildItems(biome);
            TradingPostService.RequestSlot(postId);
        }
    }

    // The player body's animator expects the character to report standing on firm ground; without it the body falls.
    private void Start()
    {
        SetBool("onGround", true);
        SetBool("inWater", false);
        SetBool("encumbered", false);
        SetBool("crouching", false);
    }

    private void Update()
    {
        Player player = Player.GetClosestPlayer(transform.position, StandRange);
        float targetYaw = restYaw;
        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - transform.position;
            float toward = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
            targetYaw = restYaw + Mathf.Clamp(Mathf.DeltaAngle(restYaw, toward), -MaxTurn, MaxTurn);
            float distance = toPlayer.magnitude;
            if (!greeted && distance < GreetRange)
            {
                greeted = true;
                Say(Pick(trader.m_randomGreets), "Greet");
            }
            else if (greeted && distance > ByeRange)
            {
                greeted = false;
                Say(Pick(trader.m_randomGoodbye), "Greet");
            }
            else if (greeted && Time.time >= nextTalk && !StoreGui.IsVisible())
            {
                nextTalk = Time.time + TalkInterval;
                Say(Pick(trader.m_randomTalk), talkEmotes[Random.Range(0, talkEmotes.Length)]);
            }
        }

        float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetYaw, TurnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static string Pick(List<string> lines)
    {
        return lines == null || lines.Count == 0 ? null : lines[Random.Range(0, lines.Count)];
    }

    private void Emote(string emote)
    {
        if (animator == null || string.IsNullOrEmpty(emote))
        {
            return;
        }

        string trigger = "emote_" + emote;
        if (parameters.Contains(trigger))
        {
            animator.SetTrigger(trigger);
        }
    }

    private void SetBool(string name, bool value)
    {
        if (animator != null && parameters.Contains(name))
        {
            animator.SetBool(name, value);
        }
    }
}
