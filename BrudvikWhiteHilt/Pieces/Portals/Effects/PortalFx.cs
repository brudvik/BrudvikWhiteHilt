using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Runes;
using BrudvikWhiteHilt.Pieces.Portals.RuneRack;
using Jotunn.Managers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.Effects;

/// <summary>
/// Shared parts of the portal travel effects: textures, sounds, materials, the effect colour and the dog's effect,
/// which is sent to every client. Nothing here runs on a dedicated server.
/// </summary>
public static class PortalFx
{
    /// <summary>Seconds the game keeps a player at the portal before moving them (Player.UpdateTeleport).</summary>
    public const float VanillaWait = 2f;

    private const string DogRpc = "WhiteHiltPortalFxDog";
    private const string ResourcePrefix = "BrudvikWhiteHilt.Assets.PortalFx.";
    private const float DogEffectScale = 0.5f;
    private const float DogEffectRange = 100f;

    private static Texture2D ringTexture;
    private static Texture2D sparkTexture;
    private static Mesh quad;
    private static GameObject departSound;
    private static GameObject arriveSound;
    private static int groundMask;
    private static bool texturesFailed;

    /// <summary>
    /// The rune ring drawn on the ground, white on transparent, or null if it could not be loaded.
    /// </summary>
    public static Texture2D RingTexture
    {
        get
        {
            LoadTextures();
            return ringTexture;
        }
    }

    /// <summary>
    /// A flat square of 1 by 1 metre lying on the ground, centred on its origin.
    /// </summary>
    public static Mesh Quad => quad ??= CreateQuad();

    /// <summary>
    /// Creates the two sounds as copies of a vanilla sound effect, so they follow the game's volume. Call once the
    /// vanilla prefabs are available.
    /// </summary>
    public static void CreateSounds()
    {
        if (departSound != null || VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            GameObject source = PrefabManager.Instance.GetPrefab("Wolf")?.GetComponent<MonsterAI>()?.m_alertedEffects.m_effectPrefabs
                .Select(entry => entry.m_prefab)
                .FirstOrDefault(prefab => prefab != null && prefab.GetComponentInChildren<ZSFX>(true) != null);
            if (source == null)
            {
                Jotunn.Logger.LogWarning("Portal effects: no vanilla sound effect to copy, travel stays silent");
                return;
            }

            departSound = CreateSound("sfx_whitehilt_portal_depart", source, "Depart");
            arriveSound = CreateSound("sfx_whitehilt_portal_arrive", source, "Arrive");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"Portal effects: sounds not loaded: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers the dog's effect with the network. Call when a game starts.
    /// </summary>
    public static void RegisterRpc()
    {
        ZRoutedRpc.instance?.Register<Vector3, Vector3, int>(DogRpc, RPC_Dog);
    }

    /// <summary>
    /// Shows every client a dog vanishing at one place and appearing at another.
    /// </summary>
    /// <param name="from">Where the dog was.</param>
    /// <param name="to">Where the dog goes.</param>
    /// <param name="colour">Colour of the effect.</param>
    public static void SendDog(Vector3 from, Vector3 to, Color colour)
    {
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, DogRpc, from, to, Pack(colour));
    }

    /// <summary>
    /// Colour of the effects for a trip from a place: the strongest rune on rune posts nearby, or the set colour.
    /// </summary>
    /// <param name="position">Where the trip starts.</param>
    /// <returns>The colour.</returns>
    public static Color ColourAt(Vector3 position)
    {
        if (PortalFxSettings.RuneColours.Value)
        {
            int mask = RunePortalRules.GetRunes(position, out _);
            for (int index = WhiteHiltRuneBase.Count - 1; index >= 0; index--)
            {
                WhiteHiltRuneBase rune = (mask & (1 << index)) != 0 ? WhiteHiltRuneBase.Get(index) : null;
                if (rune != null)
                {
                    return rune.GlowColor;
                }
            }
        }

        return PortalFxSettings.DefaultColour;
    }

    /// <summary>
    /// Packs a colour into an int (RGB, 8 bits each) for the network.
    /// </summary>
    /// <param name="colour">The colour.</param>
    /// <returns>The packed colour.</returns>
    public static int Pack(Color colour)
    {
        Color32 c = colour;
        return (c.r << 16) | (c.g << 8) | c.b;
    }

    /// <summary>
    /// Unpacks a colour packed by <see cref="Pack"/>.
    /// </summary>
    /// <param name="packed">The packed colour.</param>
    /// <returns>The colour.</returns>
    public static Color Unpack(int packed)
    {
        return new Color32((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed, 255);
    }

    /// <summary>
    /// The ground under a point, or the point itself if there is no ground within a few metres.
    /// </summary>
    /// <param name="position">The point, e.g. a player's feet.</param>
    /// <returns>A point on the ground.</returns>
    public static Vector3 Ground(Vector3 position)
    {
        if (groundMask == 0)
        {
            groundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
        }

        return Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 4f, groundMask, QueryTriggerInteraction.Ignore)
            ? hit.point
            : position;
    }

    /// <summary>
    /// A new material that draws a texture unlit in a colour, which may be brighter than white to make it shine.
    /// </summary>
    /// <param name="texture">The texture; the spark if null.</param>
    /// <returns>The material, or null if the shader is missing. The caller destroys it.</returns>
    public static Material CreateMaterial(Texture2D texture)
    {
        LoadTextures();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            return null;
        }

        return new Material(shader) { mainTexture = texture != null ? texture : sparkTexture, renderQueue = 3100 };
    }

    /// <summary>
    /// Plays the sound of leaving or arriving at a place.
    /// </summary>
    /// <param name="arrive">True for arriving, false for leaving.</param>
    /// <param name="position">Where the sound plays.</param>
    public static void PlaySound(bool arrive, Vector3 position)
    {
        GameObject sound = arrive ? arriveSound : departSound;
        if (sound != null && PortalFxSettings.Sounds.Value)
        {
            UnityEngine.Object.Instantiate(sound, position, Quaternion.identity);
        }
    }

    private static void RPC_Dog(long sender, Vector3 from, Vector3 to, int colour)
    {
        if (VisualHelper.IsHeadless || !PortalFxSettings.Enabled.Value || !PortalFxSettings.Dog.Value || Utils.GetMainCamera() == null)
        {
            return;
        }

        Vector3 eye = Utils.GetMainCamera().transform.position;
        foreach (Vector3 position in new[] { from, to })
        {
            if (Vector3.Distance(eye, position) <= DogEffectRange)
            {
                PortalFxWorld.Create(PortalFxKind.Arrive, Ground(position), Unpack(colour), DogEffectScale, 0f);
            }
        }
    }

    private static GameObject CreateSound(string name, GameObject source, string clipName)
    {
        AudioClip clip = LoadWav(clipName);
        if (clip == null)
        {
            return null;
        }

        GameObject effect = PrefabManager.Instance.CreateClonedPrefab(name, source);
        ZSFX sfx = effect.GetComponentInChildren<ZSFX>(true);
        sfx.m_audioClips = new[] { clip };
        sfx.m_minPitch = 1f;
        sfx.m_maxPitch = 1f;
        sfx.m_minVol = 1f;
        sfx.m_maxVol = 1f;
        return effect;
    }

    private static void LoadTextures()
    {
        if (sparkTexture != null || texturesFailed || VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            ringTexture = LoadTexture("RuneRing");
            sparkTexture = LoadTexture("Spark");
        }
        catch (Exception ex)
        {
            texturesFailed = true;
            Jotunn.Logger.LogWarning($"Portal effects: textures not loaded: {ex.Message}");
        }
    }

    private static Texture2D LoadTexture(string name)
    {
        Texture2D texture = AssetUtilsExtended.LoadTextureFromEmbeddedResource($"{ResourcePrefix}{name}.png");
        texture.name = $"whitehilt_portalfx_{name}";
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    // Reads a 16-bit PCM wave file from the embedded resources.
    private static AudioClip LoadWav(string name)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{ResourcePrefix}{name}.wav");
        if (stream == null)
        {
            Jotunn.Logger.LogWarning($"Portal effects: sound {name} is missing");
            return null;
        }

        using BinaryReader reader = new(stream);
        if (new string(reader.ReadChars(4)) != "RIFF")
        {
            return null;
        }

        reader.ReadInt32();
        reader.ReadChars(4);
        int channels = 0;
        int rate = 0;
        int bits = 0;
        while (stream.Position + 8 <= stream.Length)
        {
            string chunk = new(reader.ReadChars(4));
            int size = reader.ReadInt32();
            if (chunk == "fmt ")
            {
                reader.ReadInt16();
                channels = reader.ReadInt16();
                rate = reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt16();
                bits = reader.ReadInt16();
                stream.Position += size - 16;
            }
            else if (chunk == "data")
            {
                if (bits != 16 || channels < 1)
                {
                    Jotunn.Logger.LogWarning($"Portal effects: sound {name} is not 16-bit PCM");
                    return null;
                }

                float[] samples = new float[size / 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = reader.ReadInt16() / 32768f;
                }

                AudioClip clip = AudioClip.Create($"whitehilt_portalfx_{name}", samples.Length / channels, channels, rate, false);

                // Unity 6 adds a span overload of SetData that net48 cannot bind, so the array one is called by name.
                typeof(AudioClip).GetMethod(nameof(AudioClip.SetData), new[] { typeof(float[]), typeof(int) })?.Invoke(clip, new object[] { samples, 0 });
                return clip;
            }
            else
            {
                stream.Position += size + (size & 1);
            }
        }

        return null;
    }

    private static Mesh CreateQuad()
    {
        Mesh mesh = new() { name = "whitehilt_portalfx_quad" };
        mesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
