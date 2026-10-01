using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Settings;

/// <summary>
/// The cog button in the bottom left corner while the inventory is open; it opens the settings window.
/// </summary>
public class ConfigButton : MonoBehaviour
{
    private const float Size = 52f;
    private const int IconPixels = 128;
    private const int Teeth = 8;

    private static ConfigButton instance;
    private static Sprite icon;

    private GameObject button;

    /// <summary>
    /// Creates the button once the game's GUI exists. Cheap to call every frame.
    /// </summary>
    public static void EnsureCreated()
    {
        if (instance != null || GUIManager.CustomGUIFront == null || InventoryGui.instance == null)
        {
            return;
        }

        GameObject root = new("WhiteHiltSettingsButton", typeof(RectTransform));
        root.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
        instance = root.AddComponent<ConfigButton>();
        instance.Build();
    }

    private void Build()
    {
        button = new GameObject("Cog", typeof(RectTransform));
        button.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(Size, Size);
        rect.anchoredPosition = new Vector2(24f, 24f);

        Image image = button.AddComponent<Image>();
        image.sprite = GetIcon();
        image.color = new Color(0.95f, 0.88f, 0.7f);
        Outline outline = button.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button click = button.AddComponent<Button>();
        click.targetGraphic = image;
        ColorBlock colors = click.colors;
        colors.highlightedColor = new Color(1f, 0.75f, 0.3f);
        colors.pressedColor = new Color(0.8f, 0.6f, 0.25f);
        click.colors = colors;
        click.onClick.AddListener(ConfigWindow.Toggle);
        button.SetActive(false);
    }

    private void Update()
    {
        bool show = InventoryGui.IsVisible() && !ConfigWindow.IsOpen;
        if (button.activeSelf != show)
        {
            button.SetActive(show);
        }
    }

    // A cog drawn in code, 4x supersampled, so no texture has to ship with the mod.
    private static Sprite GetIcon()
    {
        if (icon != null)
        {
            return icon;
        }

        Texture2D texture = new(IconPixels, IconPixels, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color32[] pixels = new Color32[IconPixels * IconPixels];
        for (int y = 0; y < IconPixels; y++)
        {
            for (int x = 0; x < IconPixels; x++)
            {
                int covered = 0;
                for (int sy = 0; sy < 4; sy++)
                {
                    for (int sx = 0; sx < 4; sx++)
                    {
                        float u = (x + (sx + 0.5f) / 4f) / IconPixels * 2f - 1f;
                        float v = (y + (sy + 0.5f) / 4f) / IconPixels * 2f - 1f;
                        covered += InCog(u, v) ? 1 : 0;
                    }
                }

                pixels[y * IconPixels + x] = new Color32(255, 255, 255, (byte)(covered * 255 / 16));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        icon = Sprite.Create(texture, new Rect(0f, 0f, IconPixels, IconPixels), new Vector2(0.5f, 0.5f));
        return icon;
    }

    private static bool InCog(float u, float v)
    {
        float radius = Mathf.Sqrt(u * u + v * v);
        if (radius < 0.3f || radius > 0.95f)
        {
            return false;
        }

        // Position within one tooth period, 0 at a tooth's middle; teeth take 40% of the rim.
        float angle = Mathf.Atan2(v, u) / (2f * Mathf.PI) * Teeth;
        float phase = Mathf.Abs(angle - Mathf.Round(angle));
        return radius <= 0.72f || phase < 0.2f;
    }
}
