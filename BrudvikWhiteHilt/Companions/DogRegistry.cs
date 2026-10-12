using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Creates the dog (a smaller clone of the vanilla wolf), the puppies the Bog Witch sells and the dog's remains,
/// and tells a dog's owner when the dog is gone.
/// </summary>
public static partial class DogRegistry
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
}
