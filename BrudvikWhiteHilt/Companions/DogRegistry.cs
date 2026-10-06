using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Creates the dog (a smaller clone of the vanilla wolf), the puppies the Bog Witch sells and the dog's remains,
/// and tells a dog's owner when the dog is gone.
/// </summary>
public static class DogRegistry
{
    /// <summary>
    /// Prefab name of the dog creature.
    /// </summary>
    public const string DogPrefabName = "WhiteHiltDog";

    /// <summary>
    /// Prefab name of the remains a dead dog leaves.
    /// </summary>
    public const string RemainsPrefabName = "WhiteHiltDogRemains";

    /// <summary>
    /// Prefab name of the gravestone cut at the stonecutter, with the dog's name and age chiselled in.
    /// </summary>
    public const string GravestonePrefabName = "WhiteHiltDogGravestone";

    /// <summary>
    /// Player key the trader sets when a puppy is bought. Only one dog per player: cleared when the dog is gone.
    /// </summary>
    public const string BuyKey = "whitehilt_dog";

    /// <summary>
    /// Player key set with the first dog. It stays, and makes the dog's pieces known.
    /// </summary>
    public const string KnownKey = "whitehilt_dog_known";

    /// <summary>
    /// Player key set while the player has a living dog.
    /// </summary>
    public const string AliveKey = "whitehilt_dog_alive";

    /// <summary>
    /// Prefab name of the whistle that calls the dog or sends it home.
    /// </summary>
    public const string WhistlePrefabName = "WhiteHiltDogWhistle";

    /// <summary>
    /// Prefab name of the dog food cooked in the Stone Pot.
    /// </summary>
    public const string DogFoodPrefabName = "WhiteHiltDogFood";

    /// <summary>
    /// Prefab name of the stick to throw for the dog.
    /// </summary>
    public const string StickPrefabName = "WhiteHiltDogStick";

    /// <summary>
    /// Prefab name of the treat that teaches tricks and cheers the dog up.
    /// </summary>
    public const string TreatPrefabName = "WhiteHiltDogTreat";

    /// <summary>
    /// Prefab name of the bandage for a hurt or poisoned dog.
    /// </summary>
    public const string BandagePrefabName = "WhiteHiltDogBandage";

    /// <summary>
    /// Prefab name of the coat that keeps the dog warm.
    /// </summary>
    public const string CoatPrefabName = "WhiteHiltDogCoat";

    /// <summary>
    /// Messages reach the owner anywhere with this range.
    /// </summary>
    public const float Anywhere = float.MaxValue;

    /// <summary>
    /// Steps from a dark muzzle to a fully grey one.
    /// </summary>
    public const int GreySteps = 4;

    // 64 x 64 texels of the wolf texture, found offline from the rig's head-weighted vertices.
    private const int MaskSize = 64;


    private const string LeatherCollarPrefabName = "WhiteHiltDogCollarLeather";
    private const string IronCollarPrefabName = "WhiteHiltDogCollarIron";
    private const string TraderPrefabName = "BogWitch";
    private const string GoneRpc = "WhiteHilt_DogGone";
    private const string NoticeRpc = "WhiteHilt_DogNotice";
    private const string CuddleEffectName = "WhiteHiltDogCuddle";
    private const string CuddleDayKey = "whitehilt_dog_cuddle_day";
    private const string MemoriesEffectName = "WhiteHiltDogMemories";
    private const string MemoriesDayKey = "whitehilt_dog_memories_day";
    private const float MemoriesMinutes = 30f;
    private const string NameData = "whitehilt_dog_name";
    private const string DaysData = "whitehilt_dog_days";

    // Index 0 is no collar.
    private static readonly string[] collarPrefabs = { null, LeatherCollarPrefabName, IronCollarPrefabName };
    private static readonly float[] collarArmor = { 0f, 4f, 8f };

    // Sounds in the bundle, by name; a missing one is simply not played.
    private static readonly string[] soundNames = { "dogbark", "dogwhine", "doghappy", "dogsnore" };

    private static readonly HashSet<string> dogItems = new()
    {
        WhistlePrefabName, LeatherCollarPrefabName, IronCollarPrefabName, DogFoodPrefabName, GravestonePrefabName, StickPrefabName,
        TreatPrefabName, BandagePrefabName, CoatPrefabName
    };

    private static readonly CoatColor[] colors =
    {
        new("WhiteHiltPuppyBrown", "Brown Puppy", RecolorBrown),
        new("WhiteHiltPuppyPiebald", "Black and White Puppy", RecolorPiebald),
        new("WhiteHiltPuppyGolden", "Golden Puppy", RecolorGolden)
    };

    private static readonly Dictionary<int, Material> coatMaterials = new();

    // The snout: row (0 = bottom of the texture) to 64 bits, the leftmost texel in the highest bit. Other rows are empty.
    private static readonly Dictionary<int, ulong> muzzleRows = new()
    {
        { 26, 0x0000800000000000UL },
        { 27, 0x0003E00000000000UL },
        { 28, 0x0002F00000000000UL },
        { 29, 0x0001F00000000000UL },
        { 30, 0x0000C00000000000UL },
        { 35, 0x0000000040400000UL },
        { 36, 0x00000000DAC00000UL },
        { 37, 0x000000001B800000UL },
        { 38, 0x000032D833800000UL },
        { 39, 0x000036D800000000UL },
        { 42, 0x0000300000000000UL },
        { 43, 0x0000180000000000UL },
        { 45, 0x03000000E0000000UL },
        { 46, 0xC60E020000000000UL },
        { 47, 0x4000020000000000UL },
        { 53, 0x0000000100000000UL },
        { 54, 0x0000000300000000UL },
        { 55, 0x0000000200000000UL },
        { 56, 0x0000000600000000UL },
        { 57, 0x0000000400000000UL },
        { 58, 0x0000000800000000UL },
    };

    // The back and flanks, where the coat lies; same layout as the muzzle.
    private static readonly Dictionary<int, ulong> coatRows = new()
    {
        { 14, 0x0000000208000000UL },
        { 15, 0x0000001F1E000000UL },
        { 16, 0x0000007FDC000000UL },
        { 17, 0x000000FFFC000000UL },
        { 18, 0x000003FFFC000000UL },
        { 19, 0x000007FFFC000000UL },
        { 20, 0x000001FFFC000000UL },
        { 21, 0x000001FC00000000UL },
        { 22, 0x000000F000000000UL },
        { 23, 0x0000007000000000UL },
        { 24, 0x0000002000000000UL },
    };

    private static readonly Dictionary<string, GameObject> sounds = new();
    private static readonly Dictionary<string, float> soundLengths = new();
    private static SE_Stats cuddleEffect;
    private static SE_Stats memoriesEffect;
    private static Texture2D ironCollarTexture;

    /// <summary>
    /// Number of coat colours.
    /// </summary>
    public static int ColorCount => colors.Length;

    /// <summary>
    /// Registers the English text and the prefab hooks. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("whitehilt_dog", "Dog");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(RemainsPrefabName), "Dog's Remains",
            "What is left of a loyal friend. Take it to a stonecutter and cut a gravestone with its name.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(GravestonePrefabName), "Dog's Gravestone",
            "A stone with the dog's name and age chiselled in. Raise it over the grave with the hammer.");
        Translations.AddEnglish("whitehilt_dog_day", "1 day");
        Translations.AddEnglish("whitehilt_dog_days", "$1 days");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(WhistlePrefabName), "Dog Whistle",
            "A bone whistle. Blow it to call your dog to you, or, when it follows you, to send it home.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(LeatherCollarPrefabName), "Leather Dog Collar",
            "Use it on your dog. +4 armour for the dog.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(IronCollarPrefabName), "Iron Dog Collar",
            "Use it on your dog. +8 armour for the dog.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(DogFoodPrefabName), "Bone Broth",
            "Food for dogs, not for people. Keeps a dog fed for two days, heals half its health and makes a puppy grow twice as fast for a day. "
            + "Put it in the dog bowl.");
        Translations.AddEnglish("whitehilt_dog_cuddle", "Cuddled");
        Translations.AddEnglish("whitehilt_dog_cuddle_tooltip", "A good dog makes a good day: health and stamina regenerate 10% faster.");
        Translations.AddEnglish("whitehilt_dog_cuddled", "You cuddle $1.");
        Translations.AddEnglish("whitehilt_dog_level", "Bond: level $1");
        Translations.AddEnglish("whitehilt_dog_collar", "Collar: +$1 armour");
        Translations.AddEnglish("whitehilt_dog_collar_on", "$1 wears the collar.");
        Translations.AddEnglish("whitehilt_dog_guard", "$1 is barking at home!");
        Translations.AddEnglish("whitehilt_dog_found", "$1 has found something");
        Translations.AddEnglish("whitehilt_dog_dug", "$1 has dug something up!");
        Translations.AddEnglish("whitehilt_dog_age", "Age: $1");
        Translations.AddEnglish("whitehilt_dog_old", "Old: it has not long left");
        Translations.AddEnglish("whitehilt_dog_warn_old", "$1 has grown old. Make its last days good ones.");
        Translations.AddEnglish("whitehilt_dog_old_age", "$1 fell asleep and did not wake up, old and grey. Take the remains to a stonecutter and cut a gravestone.");
        Translations.AddEnglish("whitehilt_dog_watered", "Fresh water at home: grows faster");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(TreatPrefabName), "Dog Treat",
            "A chewy bit of dried meat. Make an emote near your dog with treats in your pack to teach it a trick, or use one on the dog to cheer it up.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BandagePrefabName), "Dog Bandage",
            "Use it on a hurt or poisoned dog: {0}% of its health back, and the poison is gone.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(CoatPrefabName), "Dog Coat",
            "A lined leather coat for the dog. Use it on the dog; it keeps it from freezing in the mountains and in frost.");
        Translations.AddEnglish("whitehilt_dog_mood_happy", "Mood: happy");
        Translations.AddEnglish("whitehilt_dog_mood_content", "Mood: content");
        Translations.AddEnglish("whitehilt_dog_mood_sad", "Mood: sad");
        Translations.AddEnglish("whitehilt_dog_mood_lonely", "Mood: lonely");
        Translations.AddEnglish("whitehilt_dog_poisoned", "Poisoned: a bandage or bone broth helps");
        Translations.AddEnglish("whitehilt_dog_cold", "Freezing: needs a dog coat or a fire");
        Translations.AddEnglish("whitehilt_dog_injured", "Injured: a bandage or rest at home helps");
        Translations.AddEnglish("whitehilt_dog_coat_on", "$1 wears the coat.");
        Translations.AddEnglish("whitehilt_dog_has_coat", "$1 already wears a coat.");
        Translations.AddEnglish("whitehilt_dog_bandaged", "You bandage $1.");
        Translations.AddEnglish("whitehilt_dog_treat", "$1 gulps the treat down.");
        Translations.AddEnglish("whitehilt_dog_trick_sit", "sit");
        Translations.AddEnglish("whitehilt_dog_trick_down", "lie down");
        Translations.AddEnglish("whitehilt_dog_trick_stay", "stay");
        Translations.AddEnglish("whitehilt_dog_trick_come", "come");
        Translations.AddEnglish("whitehilt_dog_trick_paw", "give paw");
        Translations.AddEnglish("whitehilt_dog_trick_roll", "roll over");
        Translations.AddEnglish("whitehilt_dog_trick_unknown", "$1 does not know how to $2 yet. Teach it with a Dog Treat in your pack.");
        Translations.AddEnglish("whitehilt_dog_trick_lesson", "$1 is learning to $2 ($3/$4).");
        Translations.AddEnglish("whitehilt_dog_trick_learned", "$1 has learned to $2!");
        Translations.AddEnglish("whitehilt_dog_litter", "$1 have had a puppy! It lies by the dog house.");
        Translations.AddEnglish("whitehilt_dog_legacy", "$1 left a puppy behind in the dog house.");
        Translations.AddEnglish("whitehilt_dog_release_has_dog", "You already have a dog. Keep the puppy until then, or give it to a friend.");
        Translations.AddEnglish("whitehilt_dog_memories", "Good Memories");
        Translations.AddEnglish("whitehilt_dog_memories_tooltip", "You remember a good friend. Stamina regenerates 15% faster.");
        Translations.AddEnglish("whitehilt_dog_remember", "You remember $1.");
        Translations.AddEnglish("whitehilt_dog_ghost", "For a moment, $1 sits by the grave.");
        Translations.AddEnglish("whitehilt_dog_home_pin", "Dog house");
        Translations.AddEnglish("whitehilt_dog_make_home", "Make this $1's home");
        Translations.AddEnglish("whitehilt_dog_lives_here", "$1 lives here");
        Translations.AddEnglish("whitehilt_dog_new_home", "$1 has a new home.");
        Translations.AddEnglish("whitehilt_dog_move_follow", "$1 must be grown and follow you to move in.");
        Translations.AddEnglish("whitehilt_dog_comes", "$1 comes running.");
        Translations.AddEnglish("whitehilt_dog_goes_home", "$1 goes home.");
        Translations.AddEnglish("whitehilt_dog_too_small", "$1 is too small to leave home.");
        Translations.AddEnglish("whitehilt_dog_not_near", "No dog of yours hears the whistle.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(StickPrefabName), "Stick",
            "Use it and throw it: your grown dog fetches it and brings it back. Good for the bond.");
        Translations.AddEnglish("whitehilt_dog_warn_hungry", "$1 is hungry and waiting for food at home.");
        Translations.AddEnglish("whitehilt_dog_warn_starving", "$1 is starving! It dies without food soon.");
        Translations.AddEnglish("whitehilt_dog_warn_homeless", "$1 has no dog house or bed under a roof and may run away.");
        Translations.AddEnglish("whitehilt_dog_no_fetch", "Your grown dog must be following you to fetch.");
        foreach (CoatColor color in colors)
        {
            Translations.AddEnglishNameAndDescription(Translations.ItemKey(color.PrefabName), color.EnglishName,
                "A puppy asleep in your pack. Use it where it shall live to let it out. It needs food, a dog house and a bed under a roof, "
                + "and grows up over ten days. Grown, it follows you and fights for you.");
        }

        Translations.AddEnglish("whitehilt_dog_growing", "Puppy, $1 % grown");
        Translations.AddEnglish("whitehilt_dog_adult", "Grown");
        Translations.AddEnglish("whitehilt_dog_paused", "Not growing: needs food, a dog house and a bed under a roof");
        Translations.AddEnglish("whitehilt_dog_hungry", "Has not eaten today");
        Translations.AddEnglish("whitehilt_dog_starving", "Starving! Feed it soon");
        Translations.AddEnglish("whitehilt_dog_needs_house", "Needs a dog house");
        Translations.AddEnglish("whitehilt_dog_needs_bed", "Needs a bed under a roof");
        Translations.AddEnglish("whitehilt_dog_unhappy", "Unhappy: it may run away");
        Translations.AddEnglish("whitehilt_dog_released", "The puppy sniffs around its new home.");
        Translations.AddEnglish("whitehilt_dog_release_blocked", "Let the puppy out under the open sky.");
        Translations.AddEnglish("whitehilt_dog_died", "$1 has died. Take the remains to a stonecutter and cut a gravestone.");
        Translations.AddEnglish("whitehilt_dog_starved", "$1 starved to death.");
        Translations.AddEnglish("whitehilt_dog_ranaway", "$1 has run away. It had no proper home.");

        CreatureManager.OnVanillaCreaturesAvailable += AddDog;
        PrefabManager.OnVanillaPrefabsAvailable += AddItems;
        PrefabManager.OnPrefabsRegistered += AddToTrader;
        PrefabManager.OnPrefabsRegistered += AddDogFoodToDiet;
        DogSettings.Price.SettingChanged += (_, _) => UpdateTraders();
        DogSettings.Enabled.SettingChanged += (_, _) => UpdateTraders();
    }

    /// <summary>
    /// True for items whose recipe stays unknown until the player's first dog.
    /// </summary>
    /// <param name="prefabName">Prefab name of the crafted item.</param>
    /// <returns>Whether the item belongs to the dog.</returns>
    public static bool IsDogItem(string prefabName)
    {
        return prefabName != null && dogItems.Contains(prefabName);
    }

    /// <summary>
    /// True when the item is the dog whistle.
    /// </summary>
    /// <param name="item">An inventory item.</param>
    /// <returns>Whether it is the whistle.</returns>
    public static bool IsWhistle(ItemDrop.ItemData item)
    {
        return item?.m_dropPrefab != null && item.m_dropPrefab.name == WhistlePrefabName;
    }

    /// <summary>
    /// Which collar an item is.
    /// </summary>
    /// <param name="item">An inventory item.</param>
    /// <returns>1 for leather, 2 for iron, 0 when it is no collar.</returns>
    public static int GetCollar(ItemDrop.ItemData item)
    {
        string prefabName = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
        return prefabName == null ? 0 : Math.Max(0, Array.IndexOf(collarPrefabs, prefabName));
    }

    /// <summary>
    /// Prefab name of a collar.
    /// </summary>
    /// <param name="collar">1 for leather, 2 for iron.</param>
    /// <returns>The prefab name, or null.</returns>
    public static string GetCollarPrefab(int collar)
    {
        return collar > 0 && collar < collarPrefabs.Length ? collarPrefabs[collar] : null;
    }

    /// <summary>
    /// Armour a collar gives the dog.
    /// </summary>
    /// <param name="collar">1 for leather, 2 for iron.</param>
    /// <returns>The armour.</returns>
    public static float GetCollarArmor(int collar)
    {
        return collar > 0 && collar < collarArmor.Length ? collarArmor[collar] : 0f;
    }

    /// <summary>
    /// The model of a collar: a ring in the XZ plane.
    /// </summary>
    /// <param name="collar">1 for leather, 2 for iron.</param>
    /// <param name="mesh">The ring.</param>
    /// <param name="texture">Its texture.</param>
    public static void GetCollarModel(int collar, out Mesh mesh, out Texture2D texture)
    {
        if (collar == 1)
        {
            mesh = ForagingAssets.LoadMesh("dogcollarleather");
            texture = ForagingAssets.LoadTexture("dogcollarleather_albedo");
            return;
        }

        mesh = ForagingAssets.LoadMesh("dogcollariron");

        // The spiked collar has no texture of its own, only a white base colour.
        if (ironCollarTexture == null)
        {
            ironCollarTexture = VisualHelper.RecolorTexture(ForagingAssets.LoadTexture("dogcollariron_albedo"), _ => new Color32(62, 62, 68, 255));
        }

        texture = ironCollarTexture;
    }

    /// <summary>
    /// Gives the local player the cuddle buff, once per game day.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="dogName">Name of the dog.</param>
    public static void Cuddle(Player player, string dogName)
    {
        if (EnvMan.instance == null)
        {
            return;
        }

        string today = EnvMan.instance.GetDay().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (cuddleEffect == null || (player.m_customData.TryGetValue(CuddleDayKey, out string day) && day == today))
        {
            return;
        }

        player.m_customData[CuddleDayKey] = today;
        cuddleEffect.m_ttl = DogSettings.CuddleMinutes.Value * 60f;
        player.GetSEMan().AddStatusEffect(cuddleEffect.NameHash(), resetTime: true);
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(Translations.Token("whitehilt_dog_cuddled"), dogName));
    }

    /// <summary>
    /// Drops a stack of an item on the ground.
    /// </summary>
    /// <param name="prefabName">Prefab name of the item.</param>
    /// <param name="amount">How many.</param>
    /// <param name="position">Where.</param>
    public static void DropItem(string prefabName, int amount, Vector3 position)
    {
        ItemDrop prefab = PrefabManager.Instance.GetPrefab(prefabName)?.GetComponent<ItemDrop>();
        if (prefab == null)
        {
            return;
        }

        ItemDrop.ItemData data = prefab.m_itemData.Clone();
        data.m_dropPrefab = prefab.gameObject;
        data.m_stack = amount;
        ItemDrop.DropItem(data, amount, position + Vector3.up * 0.5f, Quaternion.identity);
    }

    /// <summary>
    /// Prefab name of the puppy in a coat colour.
    /// </summary>
    /// <param name="color">Index of the coat colour.</param>
    /// <returns>The prefab name.</returns>
    public static string GetPuppyPrefab(int color)
    {
        return colors[Mathf.Clamp(color, 0, colors.Length - 1)].PrefabName;
    }

    /// <summary>
    /// Gives the local player Good Memories at a dog's grave, once per game day.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="dogName">Name on the grave.</param>
    public static void Remember(Player player, string dogName)
    {
        if (EnvMan.instance == null || memoriesEffect == null)
        {
            return;
        }

        string today = EnvMan.instance.GetDay().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (player.m_customData.TryGetValue(MemoriesDayKey, out string day) && day == today)
        {
            return;
        }

        player.m_customData[MemoriesDayKey] = today;
        player.GetSEMan().AddStatusEffect(memoriesEffect.NameHash(), resetTime: true);
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(Translations.Token("whitehilt_dog_remember"), dogName));
    }

    /// <summary>
    /// Tells the dog's owner something about it, if they are within <paramref name="range"/> of <paramref name="position"/>.
    /// </summary>
    /// <param name="ownerId">Player ID of the owner.</param>
    /// <param name="messageKey">Translation key; $1 is the dog's name.</param>
    /// <param name="dogName">Name of the dog.</param>
    /// <param name="position">Where it happens.</param>
    /// <param name="range">How far away the owner hears of it, or <see cref="Anywhere"/>.</param>
    public static void SendNotice(long ownerId, string messageKey, string dogName, Vector3 position, float range)
    {
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, NoticeRpc, ownerId, messageKey, dogName, position, range);
    }

    /// <summary>
    /// Plays one of the dog's sounds here, on this client only.
    /// </summary>
    /// <param name="name">Sound name, e.g. dogbark.</param>
    /// <param name="position">Where.</param>
    /// <returns>Length of the sound in seconds, or 0 when it is not in the bundle.</returns>
    public static float PlaySound(string name, Vector3 position)
    {
        if (!sounds.TryGetValue(name, out GameObject effect) || effect == null)
        {
            return 0f;
        }

        UnityEngine.Object.Instantiate(effect, position + Vector3.up * 0.5f, Quaternion.identity);
        return soundLengths[name];
    }

    /// <summary>
    /// Puts a stick in a dog's mouth: the vanilla club's mesh, across the jaw.
    /// </summary>
    /// <param name="jaw">The dog's jaw bone.</param>
    /// <returns>The stick, or null.</returns>
    public static GameObject CreateStickModel(Transform jaw)
    {
        MeshFilter source = PrefabManager.Instance.GetPrefab("Club")?.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(filter => filter.sharedMesh != null);
        MeshRenderer renderer = source != null ? source.GetComponent<MeshRenderer>() : null;
        if (renderer == null)
        {
            return null;
        }

        // The club lies along its z, like the jaw's z runs from cheek to cheek; the tongue sits at (-0.07, -0.02, 0).
        GameObject stick = new("whitehilt_stick");
        stick.transform.SetParent(jaw, false);
        stick.transform.localPosition = new Vector3(-0.07f, -0.01f, 0f);
        stick.transform.localScale = Vector3.one * 0.58f;
        stick.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
        stick.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        return stick;
    }

    private static void RPC_Notice(long sender, long ownerId, string messageKey, string dogName, Vector3 position, float range)
    {
        Player player = Player.m_localPlayer;
        if (player != null && player.GetPlayerID() == ownerId && Vector3.Distance(player.transform.position, position) <= range)
        {
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token(messageKey), dogName));
        }
    }

    /// <summary>
    /// Returns the coat colour of a puppy item.
    /// </summary>
    /// <param name="item">An inventory item.</param>
    /// <param name="color">Index of the coat colour.</param>
    /// <returns>True when the item is a puppy.</returns>
    public static bool IsPuppy(ItemDrop.ItemData item, out int color)
    {
        string prefabName = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
        color = Array.FindIndex(colors, candidate => candidate.PrefabName == prefabName);
        return color >= 0;
    }

    /// <summary>
    /// Returns the dog's body material in a coat colour, with a greying muzzle for an old dog and its coat when it wears one,
    /// made the first time it is needed.
    /// </summary>
    /// <param name="color">Index of the coat colour.</param>
    /// <param name="greyStep">How grey the muzzle is, 0 (none) to <see cref="GreySteps"/>.</param>
    /// <param name="coat">True when the dog wears its coat.</param>
    /// <param name="source">The vanilla wolf material.</param>
    /// <returns>The coloured material.</returns>
    public static Material GetCoatMaterial(int color, int greyStep, bool coat, Material source)
    {
        color = Mathf.Clamp(color, 0, colors.Length - 1);
        greyStep = Mathf.Clamp(greyStep, 0, GreySteps);
        int key = color * 100 + greyStep * 2 + (coat ? 1 : 0);
        if (!coatMaterials.TryGetValue(key, out Material material) || material == null)
        {
            material = new Material(source) { mainTexture = CreateCoatTexture(source.mainTexture, colors[color].Recolor, greyStep, coat) };
            coatMaterials[key] = material;
        }

        return material;
    }

    // The coat colour everywhere, the fur of the muzzle blended towards grey, and the coat painted over the back.
    // Both masks were worked out offline from the wolf's UVs.
    private static Texture2D CreateCoatTexture(Texture source, Func<Color32, Color32> recolor, int greyStep, bool coat)
    {
        if (greyStep == 0 && !coat)
        {
            return VisualHelper.RecolorTexture(source, recolor);
        }

        int width = source.width;
        int height = source.height;
        Color32[] pixels = VisualHelper.ReadPixels(source, width, height);
        float grey = 0.85f * greyStep / GreySteps;
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 original = pixels[i];
            int x = (i % width) * MaskSize / width;
            int y = (i / width) * MaskSize / height;
            Color.RGBToHSV(original, out _, out _, out float shade);
            if (coat && InMask(coatRows, x, y))
            {
                pixels[i] = WithAlpha(Color.HSVToRGB(0.075f, 0.6f, 0.16f + 0.3f * shade), original.a);
                continue;
            }

            Color32 recoloured = recolor(original);
            pixels[i] = greyStep > 0 && InMask(muzzleRows, x, y) && IsFur(original, out float value)
                ? Color32.Lerp(recoloured, WithAlpha(Color.HSVToRGB(0f, 0f, Mathf.Max(0.75f, value)), original.a), grey)
                : recoloured;
        }

        return VisualHelper.CreateTexture(source.name + "_whitehilt_dog", width, height, pixels, source);
    }

    private static bool InMask(Dictionary<int, ulong> rows, int x, int y)
    {
        return rows.TryGetValue(y, out ulong row) && ((row >> (MaskSize - 1 - x)) & 1UL) != 0;
    }

    /// <summary>
    /// True once the player has had a dog, which makes the dog's pieces known.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>Whether the player knows about dogs.</returns>
    public static bool IsDogKnown(Player player)
    {
        if (player.HaveUniqueKey(KnownKey))
        {
            return true;
        }

        if (!player.HaveUniqueKey(BuyKey))
        {
            return false;
        }

        // The buy key goes when the dog does; this one stays.
        player.AddUniqueKey(KnownKey);
        return true;
    }

    /// <summary>
    /// Writes a dog's name and age into an item (remains or gravestone).
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="dogName">The dog's name.</param>
    /// <param name="days">Days the dog lived.</param>
    public static void SetInscription(ItemDrop.ItemData item, string dogName, int days)
    {
        item.m_customData[NameData] = dogName;
        item.m_customData[DaysData] = days.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Copies the dog's name and age from one item to another.
    /// </summary>
    /// <param name="from">The item with the inscription.</param>
    /// <param name="to">The item to write it into.</param>
    public static void CopyInscription(ItemDrop.ItemData from, ItemDrop.ItemData to)
    {
        if (from.m_customData.TryGetValue(NameData, out string dogName))
        {
            to.m_customData[NameData] = dogName;
            to.m_customData[DaysData] = from.m_customData.TryGetValue(DaysData, out string days) ? days : "0";
        }
    }

    /// <summary>
    /// True when the item carries a dog's name.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Whether it is inscribed.</returns>
    public static bool HasInscription(ItemDrop.ItemData item)
    {
        return item != null && item.m_customData.ContainsKey(NameData);
    }

    /// <summary>
    /// The dog's name and age on two lines, in the local player's language, or null.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The inscription.</returns>
    public static string GetInscription(ItemDrop.ItemData item)
    {
        if (!HasInscription(item))
        {
            return null;
        }

        item.m_customData.TryGetValue(DaysData, out string text);
        int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int days);
        string age = days == 1
            ? Localization.instance.Localize(Translations.Token("whitehilt_dog_day"))
            : Localization.instance.Localize(Translations.Token("whitehilt_dog_days"), days.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return item.m_customData[NameData] + "\n" + age;
    }

    /// <summary>
    /// Lets a puppy out of the player's pack in front of them.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="inventory">The inventory holding the puppy.</param>
    /// <param name="item">The puppy item.</param>
    /// <param name="color">Index of the coat colour.</param>
    public static void Release(Player player, Inventory inventory, ItemDrop.ItemData item, int color)
    {
        if (player.InInterior() || player.IsSwimming())
        {
            player.Message(MessageHud.MessageType.Center, Translations.Token("whitehilt_dog_release_blocked"));
            return;
        }

        // One dog per player: a puppy from a litter or left behind by an old dog must wait, or go to a friend.
        if (player.HaveUniqueKey(AliveKey) || DogCompanion.FindOwnedBy(player.GetPlayerID()) != null)
        {
            player.Message(MessageHud.MessageType.Center, Translations.Token("whitehilt_dog_release_has_dog"));
            return;
        }

        GameObject prefab = PrefabManager.Instance.GetPrefab(DogPrefabName);
        if (prefab == null)
        {
            Jotunn.Logger.LogError($"Dog: prefab {DogPrefabName} not found, the puppy cannot be let out");
            return;
        }

        Vector3 forward = player.transform.forward;
        Vector3 position = player.transform.position + forward * 1.5f + Vector3.up * 0.3f;
        GameObject dog = UnityEngine.Object.Instantiate(prefab, position, Quaternion.LookRotation(-forward));
        dog.GetComponent<DogCompanion>().InitializeNew(player.GetPlayerID(), color);
        inventory.RemoveOneItem(item);
        player.AddUniqueKey(KnownKey);
        player.AddUniqueKey(BuyKey);
        player.AddUniqueKey(AliveKey);
        player.Message(MessageHud.MessageType.Center, Translations.Token("whitehilt_dog_released"));
        TextInput.instance.RequestText(dog.GetComponent<Tameable>(), "$hud_rename", 20);
    }

    /// <summary>
    /// Registers the routed RPC. Call once per game session.
    /// </summary>
    public static void RegisterRpcs()
    {
        ZRoutedRpc.instance?.Register<long, string, string>(GoneRpc, RPC_Gone);
        ZRoutedRpc.instance?.Register<long, string, string, Vector3, float>(NoticeRpc, RPC_Notice);
    }

    /// <summary>
    /// Tells the dog's owner, wherever they are, that the dog is gone, so they may buy a new one.
    /// </summary>
    /// <param name="ownerId">Player ID of the owner.</param>
    /// <param name="messageKey">Translation key of the message; $1 is the dog's name.</param>
    /// <param name="dogName">Name of the dog.</param>
    public static void SendGone(long ownerId, string messageKey, string dogName)
    {
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, GoneRpc, ownerId, messageKey, dogName);
    }

    private static void RPC_Gone(long sender, long ownerId, string messageKey, string dogName)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.GetPlayerID() != ownerId)
        {
            return;
        }

        player.RemoveUniqueKey(BuyKey);
        player.RemoveUniqueKey(AliveKey);
        string text = Localization.instance.Localize(Translations.Token(messageKey), dogName);
        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, text);
    }

    // Registers the dog as a clone of the wolf: a name of its own, a ragdoll that keeps its size, no drops (it leaves
    // its own remains) and no breeding.
    private static void AddDog()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= AddDog;
        try
        {
            string name = Translations.Token("whitehilt_dog");
            CustomCreature creature = new(DogPrefabName, "Wolf", new CreatureConfig { Name = name });
            GameObject prefab = creature.Prefab;
            Character character = prefab.GetComponent<Character>();
            character.m_name = name;

            // The dog is smaller than a wolf and grows, so its ragdoll must take its size.
            foreach (EffectList.EffectData effect in character.m_deathEffects.m_effectPrefabs)
            {
                effect.m_inheritParentScale = true;
            }

            // The remains are spawned by the dog itself, so they carry its name; no pelts or meat.
            CharacterDrop drops = prefab.GetComponent<CharacterDrop>();
            if (drops != null)
            {
                drops.m_drops.Clear();
            }

            Procreation procreation = prefab.GetComponent<Procreation>();
            if (procreation != null)
            {
                // Vanilla's m_pregnancyChance is the chance to skip a breeding check: 1 always skips.
                procreation.m_pregnancyChance = 1f;
            }

            prefab.AddComponent<RestPose>();
            prefab.AddComponent<DogCompanion>();
            prefab.AddComponent<DogActivities>();
            prefab.AddComponent<DogCare>();
            prefab.AddComponent<DogTricks>();
            prefab.AddComponent<DogExpression>();
            if (!VisualHelper.IsHeadless)
            {
                CreateSoundEffects(prefab);
            }

            CreatureManager.Instance.AddCreature(creature);
            Jotunn.Logger.LogInfo("Dog added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Dog failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Registers the dog's items: a puppy per coat colour, remains, the gravestone, the whistle, the collars and the
    // coat, and the cuddle and memories effects.
    private static void AddItems()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddItems;
        foreach (CoatColor color in colors)
        {
            AddItem(color.PrefabName, "TrophyWolf", 5f, new ItemConfig(), item => ShapePuppy(item, color.Recolor));
        }

        AddItem(RemainsPrefabName, "BoneFragments", 2f, new ItemConfig(), null);
        AddItem(GravestonePrefabName, "Stone", 20f, new ItemConfig
        {
            CraftingStation = CraftingStations.Stonecutter,
            Requirements = new RequirementConfig[]
            {
                new() { Item = RemainsPrefabName, Amount = 1 },
                new() { Item = "Stone", Amount = 10 }
            }
        }, ShapeGravestone);

        AddItem(WhistlePrefabName, "BoneFragments", 0.3f, new ItemConfig
        {
            CraftingStation = CraftingStations.Workbench,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "BoneFragments", Amount = 4 },
                new() { Item = "LeatherScraps", Amount = 2 }
            }
        }, item => VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("dogwhistle"), ForagingAssets.LoadTexture("dogwhistle_albedo"), size: 0.3f));
        AddItem(LeatherCollarPrefabName, "LeatherScraps", 1f, new ItemConfig
        {
            CraftingStation = CraftingStations.Workbench,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "LeatherScraps", Amount = 6 },
                new() { Item = "Bronze", Amount = 1 }
            }
        }, item => ShapeCollar(item, 1));
        AddItem(IronCollarPrefabName, "Chain", 2f, new ItemConfig
        {
            CraftingStation = CraftingStations.Forge,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "Iron", Amount = 3 },
                new() { Item = "Chain", Amount = 1 },
                new() { Item = "LeatherScraps", Amount = 2 }
            }
        }, item => ShapeCollar(item, 2));
        AddItem(DogFoodPrefabName, "TurnipStew", 1f, new ItemConfig
        {
            CraftingStation = Pieces.Cooking.StonePot.StonePot.PrefabName,
            Amount = 2,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "RawMeat", Amount = 2 },
                new() { Item = "BoneFragments", Amount = 3 },
                new() { Item = "Mushroom", Amount = 2 }
            }
        }, item => VisualHelper.Tint(item.ItemPrefab, new Color(0.85f, 0.7f, 0.5f)), maxStack: 10);
        AddItem(StickPrefabName, "Club", 0.5f, new ItemConfig
        {
            Requirements = new RequirementConfig[] { new() { Item = "Wood", Amount = 1 } }
        }, null);
        AddItem(TreatPrefabName, "CookedMeat", 0.1f, new ItemConfig
        {
            CraftingStation = Pieces.Cooking.StonePot.StonePot.PrefabName,
            Amount = 6,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "RawMeat", Amount = 1 },
                new() { Item = "BoneFragments", Amount = 1 }
            }
        }, item => VisualHelper.Tint(item.ItemPrefab, new Color(0.6f, 0.4f, 0.25f)), maxStack: 50);
        AddItem(BandagePrefabName, "LeatherScraps", 0.2f, new ItemConfig
        {
            CraftingStation = CraftingStations.Workbench,
            Amount = 2,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "LeatherScraps", Amount = 2 },
                new() { Item = "Resin", Amount = 1 },
                new() { Item = "Dandelion", Amount = 1 }
            }
        }, item => VisualHelper.Tint(item.ItemPrefab, new Color(0.95f, 0.9f, 0.8f)), maxStack: 10);
        AddItem(CoatPrefabName, "DeerHide", 1f, new ItemConfig
        {
            CraftingStation = CraftingStations.Workbench,
            MinStationLevel = 2,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "DeerHide", Amount = 2 },
                new() { Item = "LeatherScraps", Amount = 4 },
                new() { Item = "TrollHide", Amount = 1 }
            }
        }, item => VisualHelper.Tint(item.ItemPrefab, new Color(0.55f, 0.35f, 0.2f)));
        AddCuddleEffect();
        AddMemoriesEffect();
    }

    private static void AddMemoriesEffect()
    {
        memoriesEffect = ScriptableObject.CreateInstance<SE_Stats>();
        memoriesEffect.name = MemoriesEffectName;
        memoriesEffect.m_name = Translations.Token("whitehilt_dog_memories");
        memoriesEffect.m_tooltip = Translations.Token("whitehilt_dog_memories_tooltip");
        memoriesEffect.m_ttl = MemoriesMinutes * 60f;
        memoriesEffect.m_staminaRegenMultiplier = 1.15f;
        memoriesEffect.m_icon = PrefabManager.Instance.GetPrefab(GravestonePrefabName)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(memoriesEffect, fixReference: false));
    }

    private static void AddCuddleEffect()
    {
        cuddleEffect = ScriptableObject.CreateInstance<SE_Stats>();
        cuddleEffect.name = CuddleEffectName;
        cuddleEffect.m_name = Translations.Token("whitehilt_dog_cuddle");
        cuddleEffect.m_tooltip = Translations.Token("whitehilt_dog_cuddle_tooltip");
        cuddleEffect.m_ttl = DogSettings.CuddleMinutes.Value * 60f;
        cuddleEffect.m_healthRegenMultiplier = 1.1f;
        cuddleEffect.m_staminaRegenMultiplier = 1.1f;
        cuddleEffect.m_icon = PrefabManager.Instance.GetPrefab(colors[0].PrefabName)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(cuddleEffect, fixReference: false));
    }

    // Runs for every ZNetScene, so it must be idempotent.
    private static void AddDogFoodToDiet()
    {
        MonsterAI ai = PrefabManager.Instance.GetPrefab(DogPrefabName)?.GetComponent<MonsterAI>();
        ItemDrop food = PrefabManager.Instance.GetPrefab(DogFoodPrefabName)?.GetComponent<ItemDrop>();
        if (ai != null && food != null && !ai.m_consumeItems.Contains(food))
        {
            ai.m_consumeItems.Add(food);
        }
    }

    // Registers one dog item as a clone of a vanilla item with its own name, weight and look. Food values are cleared,
    // as dog food is not for people.
    private static void AddItem(string prefabName, string copyFrom, float weight, ItemConfig config, Action<CustomItem> applyVisual, int maxStack = 1)
    {
        try
        {
            config.Name = Translations.Token(Translations.ItemKey(prefabName));
            config.Description = Translations.Token(Translations.ItemKey(prefabName) + "_description");
            CustomItem item = new(prefabName, copyFrom, config);

            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_weight = weight;
            shared.m_maxStackSize = maxStack;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Misc;
            shared.m_value = 0;
            shared.m_teleportable = true;

            // Dog food is not for people.
            shared.m_food = 0f;
            shared.m_foodStamina = 0f;
            shared.m_foodEitr = 0f;
            shared.m_foodRegen = 0f;
            shared.m_consumeStatusEffect = null;
            if (applyVisual != null && !VisualHelper.IsHeadless)
            {
                try
                {
                    applyVisual(item);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = new[] { icon };
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{prefabName}: no custom look: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{prefabName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // A sleeping puppy baked from the game's own wolf at start-up, so nothing of the game is shipped; the recoloured trophy if that fails.
    private static void ShapePuppy(CustomItem item, Func<Color32, Color32> recolor)
    {
        try
        {
            Mesh baked = BakeWolf(RestPose.Pose.Sleep, out Texture texture, out _);
            baked.name = "whitehilt_puppy";
            VisualHelper.ReplaceMesh(item.ItemPrefab, baked, VisualHelper.RecolorTexture(texture, recolor), size: 0.5f);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{item.ItemPrefab.name}: no puppy model, the trophy is used: {ex.Message}");
            VisualHelper.Recolor(item.ItemPrefab, recolor);
        }
    }

    /// <summary>
    /// Bakes the game's own wolf in a resting pose into a static mesh, in the space of the wolf's Visual object.
    /// </summary>
    /// <param name="pose">The pose.</param>
    /// <param name="texture">The wolf's texture.</param>
    /// <param name="visualScale">Scale of the Visual object in the wolf prefab.</param>
    /// <returns>The mesh.</returns>
    public static Mesh BakeWolf(RestPose.Pose pose, out Texture texture, out float visualScale)
    {
        Transform visual = PrefabManager.Instance.GetPrefab("Wolf")?.transform.Find("Visual");
        SkinnedMeshRenderer source = visual != null ? visual.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
        if (source == null || source.sharedMaterial == null || source.sharedMaterial.mainTexture == null)
        {
            throw new InvalidOperationException("the wolf's skinned mesh was not found");
        }

        texture = source.sharedMaterial.mainTexture;
        visualScale = visual.localScale.x;
        GameObject holder = new("whitehilt_wolf_bake");
        holder.SetActive(false);
        try
        {
            GameObject copy = UnityEngine.Object.Instantiate(visual.gameObject, holder.transform, false);
            RestPose.ApplyTo(copy.transform, pose);
            SkinnedMeshRenderer skin = copy.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Mesh baked = new() { name = "whitehilt_wolf_" + pose };
            skin.BakeMesh(baked, true);
            if (baked.vertexCount == 0)
            {
                throw new InvalidOperationException("baking the wolf gave no vertices");
            }

            // BakeMesh leaves the vertices in the renderer's own orientation; turn them into the wolf's, so it lies upright.
            Matrix4x4 toRoot = copy.transform.worldToLocalMatrix * Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one);
            baked.vertices = baked.vertices.Select(vertex => toRoot.MultiplyPoint3x4(vertex)).ToArray();
            baked.normals = baked.normals.Select(normal => toRoot.MultiplyVector(normal).normalized).ToArray();
            baked.RecalculateBounds();
            return baked;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(holder);
        }
    }

    private static void ShapeCollar(CustomItem item, int collar)
    {
        GetCollarModel(collar, out Mesh mesh, out Texture2D texture);
        VisualHelper.ReplaceMesh(item.ItemPrefab, mesh, texture, size: 0.25f);
    }

    // Copies of the wolf's alert sound effect with the dog's sounds in them, so they play through the game's sound settings.
    private static void CreateSoundEffects(GameObject dog)
    {
        GameObject source = dog.GetComponent<MonsterAI>().m_alertedEffects.m_effectPrefabs
            .Select(effect => effect.m_prefab)
            .FirstOrDefault(prefab => prefab != null && prefab.GetComponentInChildren<ZSFX>(true) != null);
        if (source == null)
        {
            Jotunn.Logger.LogWarning("Dog: the wolf has no alert sound to copy, the dog makes no sounds of its own");
            return;
        }

        foreach (string name in soundNames)
        {
            AudioClip clip;
            try
            {
                clip = ForagingAssets.LoadAudio(name);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            GameObject effect = PrefabManager.Instance.CreateClonedPrefab("sfx_whitehilt_" + name, source);
            ZSFX sfx = effect.GetComponentInChildren<ZSFX>(true);
            sfx.m_audioClips = new[] { clip };
            sfx.m_minPitch = 0.92f;
            sfx.m_maxPitch = 1.08f;
            if (name == "dogsnore")
            {
                sfx.m_minVol = 0.5f;
                sfx.m_maxVol = 0.5f;
            }

            sounds[name] = effect;
            soundLengths[name] = clip.length;
        }
    }

    private static void ShapeGravestone(CustomItem item)
    {
        VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("valkyriestone"), ForagingAssets.LoadTexture("valkyriestone_albedo"));
    }

    // Runs for every ZNetScene, so it must be idempotent.
    private static void AddToTrader()
    {
        Trader trader = PrefabManager.Instance.GetPrefab(TraderPrefabName)?.GetComponent<Trader>();
        if (trader == null)
        {
            Jotunn.Logger.LogWarning($"Dog: trader {TraderPrefabName} not found, puppies cannot be bought");
            return;
        }

        UpdateTrader(trader);
    }

    // Bog Witches already in the world hold their own copy of the list, so they are updated too.
    private static void UpdateTraders()
    {
        AddToTrader();
        foreach (Trader trader in UnityEngine.Object.FindObjectsByType<Trader>(FindObjectsSortMode.None))
        {
            if (Utils.GetPrefabName(trader.gameObject) == TraderPrefabName)
            {
                UpdateTrader(trader);
            }
        }
    }

    // Adds the puppies to the trader's goods at the configured price, or takes them away when dogs are switched off.
    private static void UpdateTrader(Trader trader)
    {
        foreach (CoatColor color in colors)
        {
            ItemDrop puppy = PrefabManager.Instance.GetPrefab(color.PrefabName)?.GetComponent<ItemDrop>();
            if (puppy == null)
            {
                continue;
            }

            Trader.TradeItem entry = trader.m_items.Find(candidate => candidate.m_prefab == puppy);
            if (!DogSettings.Enabled.Value)
            {
                if (entry != null)
                {
                    trader.m_items.Remove(entry);
                }

                continue;
            }

            if (entry == null)
            {
                // StoreGui.BuySelectedItem plays these after every purchase without a null check.
                entry = new Trader.TradeItem { m_prefab = puppy, m_stack = 1, m_buyKey = BuyKey, m_buyPlayerEffects = new EffectList() };
                trader.m_items.Add(entry);
            }

            entry.m_price = DogSettings.Price.Value;
        }
    }

    // The wolf texture is grey fur with a lighter belly; eyes, nose and mouth are coloured or dark and stay as they are.
    private static bool IsFur(Color32 pixel, out float value)
    {
        Color.RGBToHSV(pixel, out _, out float saturation, out value);
        return saturation < 0.25f && value >= 0.12f;
    }

    private static Color32 WithAlpha(Color color, byte alpha)
    {
        Color32 result = color;
        result.a = alpha;
        return result;
    }

    private static Color32 RecolorBrown(Color32 pixel)
    {
        if (!IsFur(pixel, out float value))
        {
            return pixel;
        }

        return WithAlpha(value > 0.82f ? Color.HSVToRGB(0.09f, 0.18f, value) : Color.HSVToRGB(0.07f, 0.55f, value * 0.7f), pixel.a);
    }

    private static Color32 RecolorPiebald(Color32 pixel)
    {
        if (!IsFur(pixel, out float value))
        {
            return pixel;
        }

        return WithAlpha(value > 0.62f ? Color.HSVToRGB(0f, 0f, Mathf.Min(1f, value * 1.1f)) : Color.HSVToRGB(0f, 0f, value * 0.22f), pixel.a);
    }

    private static Color32 RecolorGolden(Color32 pixel)
    {
        if (!IsFur(pixel, out float value))
        {
            return pixel;
        }

        return WithAlpha(value > 0.85f ? Color.HSVToRGB(0.11f, 0.15f, value) : Color.HSVToRGB(0.1f, 0.55f, Mathf.Min(1f, value * 1.1f)), pixel.a);
    }

    private sealed class CoatColor
    {
        public CoatColor(string prefabName, string englishName, Func<Color32, Color32> recolor)
        {
            PrefabName = prefabName;
            EnglishName = englishName;
            Recolor = recolor;
        }

        public string PrefabName { get; }

        public string EnglishName { get; }

        public Func<Color32, Color32> Recolor { get; }
    }
}
