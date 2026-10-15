using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.TradingPosts;
using Jotunn.Managers;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// The merchant's skipper at the trading ship's helm, so the ship does not sail empty: the player body dressed in a
/// trader's clothes, holding the helm the way a player does. It is only a picture, made on every machine that draws the
/// ship and never sent or saved; the ship steers itself (<see cref="TradeShip"/>), and the helm stays shut to players
/// ("The merchant's skipper keeps the helm."). Each ship's skipper is put together from the traders' looks by the ship's
/// network id, so every player sees the same one and the skippers differ from ship to ship and from the traders ashore.
/// </summary>
public class TradeShipSkipper : MonoBehaviour
{
    private const string TemplateName = "WhiteHilt_TradeShipSkipper";

    /// <summary>Whose recorded voice the skipper speaks with (<see cref="WhiteHiltSound.Speak"/>).</summary>
    public const string Speaker = "skipper";

    private const float ShoutHeight = 2.2f;
    private const float ShoutTime = 5f;

    private static GameObject template;

    private GameObject skipper;
    private AudioSource voice;

    /// <summary>
    /// Registers the skipper's shouts. Call from the plugin's Awake, before the game loads its languages.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_skipper_depart1", "Cast off! Keep close, friend!");
        Translations.AddEnglish("whitehilt_skipper_depart2", "Hoist the sail, we are away!");
        Translations.AddEnglish("whitehilt_skipper_arrive1", "Land ho! We made it!");
        Translations.AddEnglish("whitehilt_skipper_arrive2", "Safe in port, thanks to you!");
        Translations.AddEnglish("whitehilt_skipper_wave1", "Nail Ships! Protect the cargo!");
        Translations.AddEnglish("whitehilt_skipper_wave2", "Raiders on the horizon! To arms!");
    }

    /// <summary>
    /// The skipper shouts at the helm when the trading ship sets out, comes under attack or reaches port.
    /// </summary>
    /// <param name="kind">What happened: depart, wave or arrive.</param>
    public void Shout(string kind)
    {
        if (skipper == null)
        {
            return;
        }

        string token = $"$whitehilt_skipper_{kind}{Random.Range(1, 3)}";
        if (Chat.instance != null)
        {
            Chat.instance.SetNpcText(skipper, Vector3.up * ShoutHeight, 30f, ShoutTime, string.Empty, token, false);
        }

        WhiteHiltSound.Speak(voice, Speaker, token);
    }

    /// <summary>
    /// Makes the skipper's body from the player prefab, once, when the vanilla prefabs are available. It is never
    /// registered with the network; ships copy it locally. Nothing is made on a dedicated server.
    /// </summary>
    public static void CreateTemplate()
    {
        if (VisualHelper.IsHeadless || template != null)
        {
            return;
        }

        GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(TemplateName, "Player");
        if (prefab == null)
        {
            Jotunn.Logger.LogWarning("Trading Ship: the player prefab was not found, the ship sails without a skipper.");
            return;
        }

        TradingPostTrader.StripToBody(prefab);

        // Part of the ship's body as a child of its rigidbody, a collider would change how the ship moves and floats.
        foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        template = prefab;
    }

    private void Start()
    {
        ZNetView view = GetComponent<ZNetView>();
        ShipControlls helm = GetComponentInChildren<ShipControlls>(true);
        if (template == null || view == null || !view.IsValid() || helm == null || helm.m_attachPoint == null)
        {
            // Nothing is drawn on a dedicated server; anywhere else a ship without its skipper says why.
            if (!VisualHelper.IsHeadless)
            {
                Jotunn.Logger.LogWarning($"Trading Ship: no skipper (template {template != null}, network {view != null && view.IsValid()}, helm {helm != null && helm.m_attachPoint != null})");
            }

            return;
        }

        // Without network registration, as the portrait's model is made: the copy's network view removes itself and
        // the equipment display shows what is set on it here.
        ZNetView.m_forceDisableInit = true;
        try
        {
            skipper = Instantiate(template, helm.m_attachPoint, false);
        }
        finally
        {
            ZNetView.m_forceDisableInit = false;
        }

        skipper.name = "skipper";
        skipper.transform.localPosition = Vector3.zero;
        skipper.transform.localRotation = Quaternion.identity;
        Dress(skipper.GetComponent<VisEquipment>(), view.GetZDO().m_uid);
        Pose(skipper.GetComponentInChildren<Animator>(), helm.m_attachAnimation);
        voice = WhiteHiltSound.AddVoice(skipper);
        if (voice != null)
        {
            voice.maxDistance = 45f;
        }
    }

    private void OnDestroy()
    {
        if (skipper != null)
        {
            Destroy(skipper);
        }
    }

    // A trader's body, hair and beard, another trader's clothes and a third one's hair colour, chosen by the ship's id.
    private static void Dress(VisEquipment visual, ZDOID id)
    {
        TraderLook[] looks = TraderLooks.All;
        if (visual == null || looks.Length == 0)
        {
            return;
        }

        uint seed = (uint)(id.UserID * 31 + id.ID);
        TraderLook body = looks[seed % looks.Length];
        TraderLook clothes = looks[(seed / 7 + 1) % looks.Length];
        if (clothes == body)
        {
            clothes = looks[(System.Array.IndexOf(looks, body) + 1) % looks.Length];
        }

        TraderLook[] bearded = looks.Where(look => !look.female && !string.IsNullOrEmpty(look.beard)).ToArray();
        string beard = body.female || bearded.Length == 0 ? null : bearded[seed / 13 % bearded.Length].beard;
        Color hair = TraderLook.ToColour(looks[seed / 29 % looks.Length].hairColour);

        visual.SetModel(body.female ? 1 : 0);
        visual.SetSkinColor(Vector3.one);
        visual.SetHairColor(new Vector3(hair.r, hair.g, hair.b));
        visual.SetHairItem(body.hair.GetStableHashCode());
        visual.SetBeardItem(string.IsNullOrEmpty(beard) ? 0 : beard.GetStableHashCode());
        visual.SetChestItem(clothes.TunicPrefab.GetStableHashCode());
        visual.SetLegItem(clothes.TrousersPrefab.GetStableHashCode());
        visual.SetShoulderItem(clothes.CapePrefab?.GetStableHashCode() ?? 0, 0, 1);
    }

    // The pose a player has at this helm, on firm ground so the body does not fall.
    private static void Pose(Animator animator, string attachAnimation)
    {
        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type != AnimatorControllerParameterType.Bool)
            {
                continue;
            }

            if (parameter.name == "onGround" || parameter.name == attachAnimation)
            {
                animator.SetBool(parameter.name, true);
            }
        }
    }
}
