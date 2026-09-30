using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// On a dog's grave, on each client: standing by it for a moment gives Good Memories once a game day, and at dusk the
/// dog's ghost sometimes sits in front of it for a little while.
/// </summary>
public sealed class DogMemorial : MonoBehaviour
{
    private const float CheckSeconds = 1f;
    private const float RememberRange = 4f;
    private const float RememberSeconds = 3f;
    private const float GhostRange = 25f;
    private const float GhostChance = 0.25f;
    private const float DuskStart = 0.7f;
    private const float DuskEnd = 0.77f;
    private const float GhostSeconds = 22f;
    private const float GhostFade = 3f;
    private const float GhostAlpha = 0.45f;
    private const float GhostLight = 0.8f;

    // In front of the inscription, which faces the grave's +z.
    private static readonly Vector3 ghostOffset = new(0f, 0f, 1.2f);
    private static readonly Color ghostColor = new(0.75f, 0.88f, 1f, 0f);

    private static Mesh ghostMesh;
    private static Texture ghostTexture;
    private static float ghostScale;
    private static bool bakeFailed;

    private Sign sign;
    private float nearSeconds;
    private float nextCheck;
    private int ghostDay = -1;
    private GameObject ghost;
    private Material ghostMaterial;
    private Light ghostLight;
    private float ghostStarted;
    private Vector3 ghostBase;

    private string DogName
    {
        get
        {
            string text = sign != null ? sign.GetText() : null;
            return string.IsNullOrEmpty(text) ? Localization.instance.Localize(Translations.Token("whitehilt_dog")) : text.Split('\n')[0];
        }
    }

    private void Awake()
    {
        // The placement ghost and a dedicated server have nothing to show.
        ZNetView nview = GetComponent<ZNetView>();
        enabled = nview != null && nview.GetZDO() != null && !VisualHelper.IsHeadless;
        sign = GetComponent<Sign>();
    }

    private void OnDestroy()
    {
        if (ghost != null)
        {
            Destroy(ghost);
        }
    }

    private void Update()
    {
        UpdateGhost();
        Player player = Player.m_localPlayer;
        if (Time.time < nextCheck || player == null || EnvMan.instance == null)
        {
            return;
        }

        nextCheck = Time.time + CheckSeconds;
        float distance = Vector3.Distance(player.transform.position, transform.position);
        nearSeconds = distance <= RememberRange ? nearSeconds + CheckSeconds : 0f;
        if (nearSeconds >= RememberSeconds)
        {
            DogRegistry.Remember(player, DogName);
        }

        TrySummonGhost(player, distance);
    }

    private void TrySummonGhost(Player player, float distance)
    {
        float time = EnvMan.instance.GetDayFraction();
        int day = EnvMan.instance.GetDay();
        if (ghost != null || distance > GhostRange || time < DuskStart || time > DuskEnd || ghostDay == day)
        {
            return;
        }

        ghostDay = day;
        if (Random.value < GhostChance && CreateGhost(player))
        {
            player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(Translations.Token("whitehilt_dog_ghost"), DogName));
        }
    }

    // A sitting dog baked from the game's wolf, pale and see-through, turned towards the player.
    private bool CreateGhost(Player player)
    {
        if (ghostMesh == null && !bakeFailed)
        {
            try
            {
                ghostMesh = DogRegistry.BakeWolf(RestPose.Pose.Sit, out ghostTexture, out float visualScale);
                ghostScale = visualScale * DogCompanion.AdultScale;
            }
            catch (System.Exception ex)
            {
                bakeFailed = true;
                Jotunn.Logger.LogWarning($"Dog's grave: no ghost: {ex.Message}");
            }
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (ghostMesh == null || shader == null)
        {
            return false;
        }

        ghostBase = transform.TransformPoint(ghostOffset);
        Vector3 toPlayer = Vector3.ProjectOnPlane(player.transform.position - ghostBase, Vector3.up);
        ghost = new GameObject("whitehilt_dog_ghost");
        ghost.transform.SetPositionAndRotation(ghostBase, toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : transform.rotation);
        ghost.transform.localScale = Vector3.one * ghostScale;
        ghost.AddComponent<MeshFilter>().sharedMesh = ghostMesh;
        ghostMaterial = new Material(shader) { mainTexture = ghostTexture, color = ghostColor };
        MeshRenderer renderer = ghost.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = ghostMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject glow = new("glow");
        glow.transform.SetParent(ghost.transform, false);
        glow.transform.localPosition = Vector3.up;
        ghostLight = glow.AddComponent<Light>();
        ghostLight.type = LightType.Point;
        ghostLight.color = new Color(0.6f, 0.8f, 1f);
        ghostLight.range = 4f;
        ghostLight.intensity = 0f;
        ghostStarted = Time.time;
        return true;
    }

    private void UpdateGhost()
    {
        if (ghost == null)
        {
            return;
        }

        float t = Time.time - ghostStarted;
        if (t >= GhostSeconds)
        {
            Destroy(ghost);
            Destroy(ghostMaterial);
            ghost = null;
            return;
        }

        float fade = Mathf.Clamp01(Mathf.Min(t, GhostSeconds - t) / GhostFade);
        Color color = ghostColor;
        color.a = GhostAlpha * fade;
        ghostMaterial.color = color;
        ghostLight.intensity = GhostLight * fade;
        ghost.transform.position = ghostBase + Vector3.up * (0.04f + Mathf.Sin(t * 1.5f) * 0.03f);
    }
}
