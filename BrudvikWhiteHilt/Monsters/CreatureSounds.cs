using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// The attack sound of a creature cloned from a vanilla one, whose weapons it shares with the vanilla creature and so
/// cannot carry a sound of its own: played from the creature when it starts an attack (the patch on
/// <c>Humanoid.StartAttack</c> in <c>MonsterPatches</c>), one of its variants at random.
/// </summary>
public class CreatureAttackVoice : MonoBehaviour
{
    /// <summary>The sound's name, without its variant number (AssetSource/Audio/Creatures).</summary>
    public string sound;

    private AudioSource voice;

    /// <summary>
    /// Plays the attack sound.
    /// </summary>
    public void Attack()
    {
        voice ??= WhiteHiltSound.AddVoice(gameObject);
        if (voice != null)
        {
            voice.maxDistance = 40f;
            WhiteHiltSound.PlayFrom(voice, CreatureSoundNames.Pick(sound));
        }
    }
}

/// <summary>
/// The beat of a dragon's wings while it flies, a loop that fades in as it takes off and out as it lands.
/// </summary>
public class WingBeats : MonoBehaviour
{
    private const string Clip = "wings_dragon";
    private const float Fade = 1.5f;

    private Character character;
    private AudioSource source;

    private void Start()
    {
        character = GetComponent<Character>();
        AudioClip clip = AudioAssets.TryLoad(Clip);
        if (clip == null || VisualHelper.IsHeadless)
        {
            enabled = false;
            return;
        }

        source = gameObject.AddComponent<AudioSource>();
        WhiteHiltSound.Place(source, 10f, 90f);
        source.clip = clip;
        source.loop = true;
        source.volume = 0f;
    }

    private void Update()
    {
        if (source == null || character == null)
        {
            return;
        }

        float target = character.IsFlying() && !character.IsDead() ? SoundSettings.EffectsVolume.Value : 0f;
        source.volume = Mathf.MoveTowards(source.volume, target, Time.deltaTime / Fade);
        if (source.volume > 0f && !source.isPlaying)
        {
            source.time = Random.Range(0f, source.clip.length);
            source.Play();
        }
        else if (source.volume <= 0f && source.isPlaying)
        {
            source.Stop();
        }
    }
}

/// <summary>
/// A loop of sound around an object, heard within a few tens of metres: the rustle of a spider nest, a lurking
/// serpent's breath, an ancient tree's creak.
/// </summary>
public class LoopSound : MonoBehaviour
{
    /// <summary>The loop's name (AssetSource/Audio/Effects).</summary>
    public string clip;

    /// <summary>Within this distance it is heard at full volume.</summary>
    public float near = 4f;

    /// <summary>Beyond this distance it is not heard.</summary>
    public float far = 28f;

    private void Start()
    {
        AudioClip loop = AudioAssets.TryLoad(clip);
        float volume = SoundSettings.AmbienceVolume.Value;
        if (loop == null || volume <= 0f || VisualHelper.IsHeadless)
        {
            return;
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        WhiteHiltSound.Place(source, near, far);
        if (AudioMan.instance != null)
        {
            source.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
        }

        source.clip = loop;
        source.loop = true;
        source.volume = volume;
        source.time = Random.Range(0f, loop.length);
        source.Play();
    }
}

/// <summary>
/// The sound of a creature's coming, heard by everyone near: the owner writes the time it came into the creature's
/// network data (<see cref="Mark"/>), and every machine that has the creature plays the sound once for it.
/// </summary>
public class ArrivalSound : MonoBehaviour
{
    /// <summary>The time a creature came, in the world's ticks.</summary>
    public static readonly int ArrivedKey = "whitehilt_arrived".GetStableHashCode();

    // A coming older than this is not sounded, for a creature loaded long after it came.
    private const double FreshSeconds = 8.0;
    private const float CheckInterval = 0.5f;

    /// <summary>The sound's name (AssetSource/Audio/Effects).</summary>
    public string clip;

    /// <summary>Beyond this distance it is not heard.</summary>
    public float far = 120f;

    private ZNetView view;
    private long heard;
    private float nextCheck;

    /// <summary>
    /// Marks that a creature has just come, so its arrival sound plays wherever it is loaded.
    /// </summary>
    /// <param name="zdo">The creature's network data.</param>
    public static void Mark(ZDO zdo)
    {
        if (zdo != null && ZNet.instance != null)
        {
            zdo.Set(ArrivedKey, ZNet.instance.GetTime().Ticks);
        }
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
    }

    private void Update()
    {
        if (Time.time < nextCheck || view == null || !view.IsValid() || ZNet.instance == null)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        long arrived = view.GetZDO().GetLong(ArrivedKey);
        if (arrived == 0L || arrived == heard)
        {
            return;
        }

        heard = arrived;
        if ((ZNet.instance.GetTime() - new System.DateTime(arrived)).TotalSeconds > FreshSeconds || VisualHelper.IsHeadless)
        {
            return;
        }

        GameObject holder = new($"whitehilt_arrival_{clip}");
        holder.transform.position = transform.position;
        AudioSource source = holder.AddComponent<AudioSource>();
        WhiteHiltSound.Place(source, 15f, far);
        WhiteHiltSound.PlayFrom(source, clip);
        Destroy(holder, 10f);
    }
}

/// <summary>
/// The names of the creatures' sounds and their variants: <c>&lt;name&gt;</c>, <c>&lt;name&gt;_2</c>, <c>&lt;name&gt;_3</c>.
/// </summary>
public static class CreatureSoundNames
{
    /// <summary>The most variants a sound has.</summary>
    public const int MaxVariants = 3;

    /// <summary>
    /// The name of one variant of a sound.
    /// </summary>
    /// <param name="name">The sound's name.</param>
    /// <param name="variant">The variant, from 0.</param>
    /// <returns>The variant's name.</returns>
    public static string Variant(string name, int variant) => variant == 0 ? name : $"{name}_{variant + 1}";

    /// <summary>
    /// A variant of a sound the bundle has, at random.
    /// </summary>
    /// <param name="name">The sound's name.</param>
    /// <returns>The variant's name; the sound's own name when it has no variants.</returns>
    public static string Pick(string name)
    {
        int count = 1;
        while (count < MaxVariants && AudioAssets.TryLoad(Variant(name, count)) != null)
        {
            count++;
        }

        return Variant(name, Random.Range(0, count));
    }

    /// <summary>
    /// Every variant of a sound the bundle has.
    /// </summary>
    /// <param name="name">The sound's name.</param>
    /// <returns>The clips; none when the bundle has not even the first.</returns>
    public static AudioClip[] Clips(string name)
    {
        System.Collections.Generic.List<AudioClip> clips = new();
        for (int variant = 0; variant < MaxVariants; variant++)
        {
            AudioClip clip = AudioAssets.TryLoad(Variant(name, variant));
            if (clip != null)
            {
                clips.Add(clip);
            }
        }

        return clips.ToArray();
    }
}
