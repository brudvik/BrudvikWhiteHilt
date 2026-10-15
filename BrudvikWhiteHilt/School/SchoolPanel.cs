using BrudvikWhiteHilt.Building.Groups;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.School;

/// <summary>
/// The window of a School Bench: the skills written in it with who wrote them and how far you can study them, what
/// there is to learn, who has written, and buttons to write, read and open the shared blueprints.
/// </summary>
public class SchoolPanel : MonoBehaviour
{
    private const float Width = 640f;
    private const float Height = 660f;
    private const float ButtonWidth = 180f;
    private const float ButtonHeight = 36f;
    private const float CloseDistance = 8f;
    private const float RefreshSeconds = 1f;
    private const int ShownWriters = 6;

    private static SchoolPanel instance;

    private Text title;
    private Text body;
    private SchoolBench bench;
    private float nextRefresh;

    /// <summary>True while the window is open.</summary>
    public static bool IsOpen => instance != null && instance.gameObject.activeSelf;

    /// <summary>
    /// Opens the window for a bench.
    /// </summary>
    /// <param name="bench">The bench.</param>
    public static void Open(SchoolBench bench)
    {
        if (GUIManager.CustomGUIFront == null)
        {
            return;
        }

        global::BrudvikWhiteHilt.Helpers.WhiteHiltSound.PlayLocal("sfx_book_open");
        if (instance == null)
        {
            instance = Build();
        }

        instance.bench = bench;
        instance.gameObject.SetActive(true);
        GUIManager.BlockInput(true);
        Refresh(bench);
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
    public static void Close()
    {
        if (IsOpen)
        {
            instance.gameObject.SetActive(false);
            GUIManager.BlockInput(false);
        }
    }

    /// <summary>
    /// Shows the bench again if the window shows it.
    /// </summary>
    /// <param name="bench">The bench that changed.</param>
    public static void Refresh(SchoolBench bench)
    {
        Player player = Player.m_localPlayer;
        if (!IsOpen || instance.bench != bench || bench == null || player == null)
        {
            return;
        }

        instance.title.text = bench.GetHoverName();
        instance.body.text = Describe(bench, player);
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (Input.GetKeyDown(KeyCode.Escape) || bench == null || player == null
            || Vector3.Distance(player.transform.position, bench.transform.position) > CloseDistance)
        {
            Close();
            return;
        }

        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + RefreshSeconds;
            Refresh(bench);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            GUIManager.BlockInput(false);
        }
    }

    // The window's text: the skills, best first, then what can be learned, then the newest writers.
    private static string Describe(SchoolBench bench, Player player)
    {
        SchoolLibrary library = bench.Library();
        StringBuilder text = new();
        text.AppendLine($"<color=#E8B04B><b>{Localization.instance.Localize("$whitehilt_school_skills")}</b></color>");
        foreach ((int id, float best, string teacher, float level, int target) in bench.StudyPlan(player).OrderByDescending(skill => skill.Best))
        {
            string name = Localization.instance.Localize("$skill_" + ((Skills.SkillType)id).ToString().ToLowerInvariant());
            bool open = SchoolSettings.TeachSkills.Value && SchoolRules.CanStudy(level, target);
            string format = Localization.instance.Localize(open ? "$whitehilt_school_skill_row" : "$whitehilt_school_skill_done");
            string line = string.Format(format, name, (int)best, teacher, (int)level, target);
            text.AppendLine(open ? line : $"<color=#A0A0A0>{line}</color>");
        }

        text.AppendLine();
        text.AppendLine(string.Format(Localization.instance.Localize("$whitehilt_school_knowledge"), library.Materials.Count, library.Stations.Count, library.Texts.Count, library.Pins.Count));
        bench.FindNew(player, out List<string> materials, out List<KeyValuePair<string, int>> stations, out List<KeyValuePair<string, string>> texts, out List<SchoolLibrary.Pin> pins);
        text.AppendLine(string.Format(Localization.instance.Localize("$whitehilt_school_new"), materials.Count, stations.Count, texts.Count, pins.Count));
        text.AppendLine();
        text.AppendLine($"<color=#E8B04B><b>{Localization.instance.Localize("$whitehilt_school_writers_title")}</b></color>");
        if (library.Writers.Count == 0)
        {
            text.AppendLine(Localization.instance.Localize("$whitehilt_school_nobody"));
        }

        foreach (SchoolLibrary.Writer writer in library.Writers.AsEnumerable().Reverse().Take(ShownWriters))
        {
            text.AppendLine(string.Format(Localization.instance.Localize("$whitehilt_school_writer_row"), writer.Name, writer.Day));
        }

        text.AppendLine();
        text.Append($"<i>{Localization.instance.Localize("$whitehilt_school_sit")}</i>");
        return text.ToString();
    }

    // Builds the window once: title, text and a row of buttons.
    private static SchoolPanel Build()
    {
        GameObject panel = GUIManager.Instance.CreateWoodpanel(GUIManager.CustomGUIFront.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Width, Height, true);
        SchoolPanel window = panel.AddComponent<SchoolPanel>();
        float inner = Width - 60f;
        window.title = GUIManager.Instance.CreateText(string.Empty, panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f),
            GUIManager.Instance.AveriaSerifBold, 26, GUIManager.Instance.ValheimOrange, true, Color.black, inner, 40f, false).GetComponent<Text>();
        window.title.alignment = TextAnchor.MiddleCenter;
        float bodyHeight = Height - 170f;
        window.body = GUIManager.Instance.CreateText(string.Empty, panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -64f - bodyHeight / 2f), GUIManager.Instance.AveriaSerif, 15, Color.white, true, Color.black, inner, bodyHeight, false)
            .GetComponent<Text>();
        window.body.alignment = TextAnchor.UpperLeft;
        window.body.supportRichText = true;
        window.body.verticalOverflow = VerticalWrapMode.Truncate;

        float gap = 10f;
        float left = -(ButtonWidth * 1.5f + gap);
        AddButton(panel, "$whitehilt_school_write", new Vector2(left + ButtonWidth / 2f, 80f), () =>
        {
            if (window.bench != null)
            {
                window.bench.WriteLocalPlayer();
            }
        });
        AddButton(panel, "$whitehilt_school_read", new Vector2(0f, 80f), () =>
        {
            if (window.bench != null)
            {
                window.bench.ReadLocalPlayer();
                Refresh(window.bench);
            }
        });
        AddButton(panel, "$whitehilt_school_blueprints", new Vector2(-left - ButtonWidth / 2f, 80f), () =>
        {
            SchoolBench chosen = window.bench;
            Close();
            BlueprintPanel.OpenShared(chosen);
        });
        AddButton(panel, "$whitehilt_school_close", new Vector2(0f, 36f), Close);
        return window;
    }

    private static void AddButton(GameObject panel, string token, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject button = GUIManager.Instance.CreateButton(Localization.instance.Localize(token), panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            position, ButtonWidth, ButtonHeight);
        button.GetComponent<Button>().onClick.AddListener(onClick);
    }
}
