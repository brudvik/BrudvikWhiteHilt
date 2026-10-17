using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Sits on the Mare. She floats: her model hovers above the ground and drifts slowly up and down. And she shows only up
/// close: on each client her see-through materials fade out beyond <see cref="MareSettings.VisibleRange"/> from the
/// local player, farther for one who carries a Wisp light. Also holds her Nightmare, which her grip and her ride on
/// sleepers give.
/// </summary>
public class MareSpirit : MonoBehaviour
{
    /// <summary>Name of the Nightmare status effect.</summary>
    public const string NightmareName = "SE_WhiteHiltNightmare";

    /// <summary>Hash of the Nightmare, as the game looks status effects up.</summary>
    public static readonly int NightmareHash = NightmareName.GetStableHashCode();

    private const float Hover = 0.3f;
    private const float Drift = 0.08f;
    private const float FadeWidth = 4f;
    private static readonly int colourId = Shader.PropertyToID("_Color");

    private Transform visual;
    private Renderer[] renderers;
    private Color[][] colours;
    private MaterialPropertyBlock block;
    private float phase;
    private float shown = -1f;

    /// <summary>
    /// Makes the Nightmare: while it lasts, no stamina comes back.
    /// </summary>
    /// <returns>The status effect, registered with the game.</returns>
    public static StatusEffect CreateNightmare()
    {
        SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
        effect.name = NightmareName;
        effect.m_name = Translations.Token("se_whitehilt_nightmare");
        effect.m_tooltip = Translations.Token("se_whitehilt_nightmare_tooltip");
        effect.m_startMessageType = MessageHud.MessageType.Center;
        effect.m_startMessage = Translations.Token("se_whitehilt_nightmare_start");
        effect.m_ttl = MareSettings.NightmareSeconds.Value;
        effect.m_staminaRegenMultiplier = 0f;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, fixReference: false));
        return effect;
    }

    /// <summary>
    /// Whether a character is kept from the Nightmare, by a Mare Charm.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>True when it cannot be given.</returns>
    public static bool Warded(Character character)
    {
        return character != null && character.GetSEMan() != null && character.GetSEMan().HaveStatusEffect(Items.Accessories.MareCharm.EffectHash);
    }

    private void Awake()
    {
        visual = transform.Find("Visual");
        phase = Random.value * 10f;
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        colours = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            colours[i] = new Color[materials.Length];
            for (int m = 0; m < materials.Length; m++)
            {
                colours[i][m] = materials[m] != null && materials[m].HasProperty(colourId) ? materials[m].color : Color.white;
            }
        }

        block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (visual != null)
        {
            Vector3 position = visual.localPosition;
            position.y = Hover + Mathf.Sin(Time.time * 1.3f + phase) * Drift;
            visual.localPosition = position;
        }

        if (renderers != null)
        {
            Fade();
        }
    }

    // Fades her by the local player's distance; a Wisp light shows her from farther away.
    private void Fade()
    {
        Player player = Player.m_localPlayer;
        float range = player != null && CarriesWisp(player) ? MareSettings.WispRange.Value : MareSettings.VisibleRange.Value;
        float distance = player != null ? Vector3.Distance(player.transform.position, transform.position) : 0f;
        float alpha = Mathf.Clamp01((range - distance) / FadeWidth + 1f);
        if (Mathf.Abs(alpha - shown) < 0.02f)
        {
            return;
        }

        shown = alpha;
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = alpha > 0.01f;
            for (int m = 0; m < colours[i].Length; m++)
            {
                Color colour = colours[i][m];
                colour.a *= alpha;
                block.Clear();
                block.SetColor(colourId, colour);
                renderers[i].SetPropertyBlock(block, m);
            }
        }
    }

    private static bool CarriesWisp(Player player)
    {
        ItemDrop.ItemData utility = player.m_utilityItem;
        return utility?.m_dropPrefab != null && utility.m_dropPrefab.name == "Demister";
    }
}
