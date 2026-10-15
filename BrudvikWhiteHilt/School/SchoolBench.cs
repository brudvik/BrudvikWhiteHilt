using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.School;

/// <summary>
/// The School Bench: players write what they know in it, and others read it and sit on its bench to study. What is
/// written lives in the piece's ZDO as one merged <see cref="SchoolLibrary"/>, written only by the ZDO's owner after a
/// writer's RPC, the way the map table keeps its map. Reading and studying change only the reader's own character, so
/// they run on the reader's machine.
/// </summary>
public class SchoolBench : MonoBehaviour, Hoverable, Interactable
{
    private const string WriteRpc = "WhiteHilt_SchoolWrite";

    // A written library is far smaller; the bench's ZDO must stay well under Steam's 512 KB per message.
    private const int MaxPacked = 384 * 1024;

    private static readonly int libraryKey = "whitehilt_school_library".GetStableHashCode();
    private static readonly List<SchoolBench> benches = new();
    private static Dictionary<string, Piece> stationPieces;

    private ZNetView nview;
    private Piece piece;
    private SchoolLibrary cached;
    private uint cachedRevision = uint.MaxValue;
    private bool studying;
    private bool caughtUpShown;
    private float nextStudy;

    /// <summary>
    /// Whether a School Bench stands within <see cref="SchoolSettings.BlueprintRange"/> of a position.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <returns>True if one does.</returns>
    public static bool IsNear(Vector3 position)
    {
        float range = SchoolSettings.BlueprintRange.Value;
        return benches.Any(bench => bench != null && Vector3.Distance(bench.transform.position, position) <= range);
    }

    /// <summary>
    /// What is written in the bench, read again only when the ZDO has changed.
    /// </summary>
    /// <returns>The library; empty if nothing is written.</returns>
    public SchoolLibrary Library()
    {
        ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
        if (zdo == null)
        {
            return new SchoolLibrary();
        }

        if (cached == null || zdo.DataRevision != cachedRevision)
        {
            byte[] packed = zdo.GetByteArray(libraryKey);
            cached = packed != null ? SchoolLibrary.FromBytes(packed) : new SchoolLibrary();
            cachedRevision = zdo.DataRevision;
        }

        return cached;
    }

    /// <summary>
    /// Writes what the local player knows in the bench. The owner of the bench merges it in.
    /// </summary>
    public void WriteLocalPlayer()
    {
        Player player = Player.m_localPlayer;
        if (player == null || nview == null || !nview.IsValid())
        {
            return;
        }

        global::BrudvikWhiteHilt.Helpers.WhiteHiltSound.PlayLocal(global::BrudvikWhiteHilt.Monsters.CreatureSoundNames.Pick("sfx_quill"));

        int day = EnvMan.instance != null ? EnvMan.instance.GetDay() : 0;
        ZPackage package = new();
        package.Write(SchoolLibrary.FromPlayer(player, day).ToBytes());
        nview.InvokeRPC(WriteRpc, package);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_school_written");
    }

    /// <summary>
    /// What the local player can learn here that they do not know yet.
    /// </summary>
    /// <param name="player">The reader.</param>
    /// <param name="materials">Materials.</param>
    /// <param name="stations">Crafting stations and their levels.</param>
    /// <param name="texts">Texts.</param>
    /// <param name="pins">Map pins the reader does not have, and may have.</param>
    public void FindNew(Player player, out List<string> materials, out List<KeyValuePair<string, int>> stations, out List<KeyValuePair<string, string>> texts,
        out List<SchoolLibrary.Pin> pins)
    {
        SchoolLibrary library = Library();
        materials = new List<string>();
        stations = new List<KeyValuePair<string, int>>();
        texts = new List<KeyValuePair<string, string>>();
        pins = new List<SchoolLibrary.Pin>();
        if (SchoolSettings.TeachRecipes.Value)
        {
            IEnumerable<string> unknown = library.Materials.Where(material => !player.m_knownMaterial.Contains(material));
            materials = (SchoolSettings.BiomeRule.Value ? LearnableItems.Filter(unknown, KnownBiomes.Of(player)) : unknown).ToList();
            HashSet<string> known = new(player.m_knownMaterial);
            known.UnionWith(materials);
            foreach (KeyValuePair<string, int> station in library.Stations)
            {
                bool higher = !player.m_knownStations.TryGetValue(station.Key, out int level) || station.Value > level;
                if (higher && (!SchoolSettings.BiomeRule.Value || CanBuildStation(station.Key, known)))
                {
                    stations.Add(station);
                }
            }
        }

        if (SchoolSettings.TeachTexts.Value)
        {
            texts = library.Texts.Where(text => !player.m_knownTexts.ContainsKey(text.Key)).ToList();
        }

        if (SchoolSettings.TeachPins.Value && Minimap.instance != null && WorldGenerator.instance != null)
        {
            Heightmap.Biome visited = KnownBiomes.Of(player);
            string reader = player.GetPlayerName();
            float distance = SchoolSettings.PinMergeDistance.Value;
            pins = library.Pins.Where(pin => pin.Author != reader
                    && (!SchoolSettings.BiomeRule.Value || (WorldGenerator.instance.GetBiome(pin.Position.x, pin.Position.z) & visited) != 0)
                    && !Minimap.instance.m_pins.Any(existing => existing.m_save && (int)existing.m_type == pin.Type
                        && Utils.DistanceXZ(existing.m_pos, pin.Position) < distance))
                .ToList();
        }
    }

    /// <summary>
    /// Teaches the local player what they can learn here, with one message instead of one per recipe.
    /// </summary>
    public void ReadLocalPlayer()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        FindNew(player, out List<string> materials, out List<KeyValuePair<string, int>> stations, out List<KeyValuePair<string, string>> texts, out List<SchoolLibrary.Pin> pins);
        if (materials.Count + stations.Count + texts.Count + pins.Count == 0)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_school_nothing");
            return;
        }

        player.m_knownMaterial.UnionWith(materials);
        foreach (KeyValuePair<string, int> station in stations)
        {
            player.m_knownStations[station.Key] = station.Value;
        }

        foreach (KeyValuePair<string, string> text in texts)
        {
            player.m_knownTexts[text.Key] = text.Value;
        }

        foreach (SchoolLibrary.Pin pin in pins)
        {
            // Own pins: a pin with a foreign owner is removed by the next map table read.
            Minimap.instance.AddPin(pin.Position, (Minimap.PinType)pin.Type, pin.Name, save: true, isChecked: false, ownerID: 0L);
        }

        player.UpdateKnownRecipesList();
        MessageHud.instance?.m_unlockMsgQueue.Clear();
        player.Message(MessageHud.MessageType.Center,
            string.Format(Localization.instance.Localize("$msg_whitehilt_school_learned"), materials.Count, stations.Count, texts.Count, pins.Count));
    }

    /// <summary>
    /// The skills written here with the local player's level and how far studying can take it.
    /// </summary>
    /// <param name="player">The reader.</param>
    /// <returns>Skill id, best level, teacher, the reader's level and the study target, for skills the game knows.</returns>
    public List<(int Id, float Best, string Teacher, float Level, int Target)> StudyPlan(Player player)
    {
        Skills skills = player.GetSkills();
        HashSet<Skills.SkillType> defined = new(skills.m_skills.Select(definition => definition.m_skill));
        float share = SchoolSettings.SkillShare.Value;
        return Library().Skills
            .Where(skill => defined.Contains((Skills.SkillType)skill.Key))
            .Select(skill => (skill.Key, skill.Value.Level, skill.Value.Teacher, LevelOf(skills, skill.Key), SchoolRules.StudyTarget(skill.Value.Level, share)))
            .ToList();
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        int writers = Library().Writers.Count;
        string count = writers > 0
            ? string.Format(Localization.instance.Localize("$whitehilt_school_writers"), writers)
            : Localization.instance.Localize("$whitehilt_school_nobody");
        return Localization.instance.Localize($"{GetHoverName()}\n{count}\n[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_school_open");
    }

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? Localization.instance.Localize(piece.m_name) : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user != Player.m_localPlayer)
        {
            return false;
        }

        SchoolPanel.Open(this);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        if (nview == null || nview.GetZDO() == null)
        {
            enabled = false;
            return;
        }

        nview.Register<ZPackage>(WriteRpc, RPC_Write);
        benches.Add(this);
    }

    private void OnDestroy()
    {
        benches.Remove(this);
    }

    // Runs on every machine, but only does anything while the local player sits on this bench: the first moment they
    // sit they read what is new, then every StudySeconds they study the skill they are furthest behind in.
    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool seated = player != null && player.IsAttached() && player.GetAttachPoint() != null && player.GetAttachPoint().IsChildOf(transform);
        if (!seated)
        {
            studying = false;
            return;
        }

        if (!studying)
        {
            studying = true;
            caughtUpShown = false;
            nextStudy = Time.time + SchoolSettings.StudySeconds.Value;
            FindNew(player, out List<string> materials, out List<KeyValuePair<string, int>> stations, out List<KeyValuePair<string, string>> texts, out List<SchoolLibrary.Pin> pins);
            if (materials.Count + stations.Count + texts.Count + pins.Count > 0)
            {
                ReadLocalPlayer();
            }
        }

        if (Time.time >= nextStudy)
        {
            nextStudy = Time.time + SchoolSettings.StudySeconds.Value;
            Study(player);
        }
    }

    private void Study(Player player)
    {
        if (!SchoolSettings.TeachSkills.Value)
        {
            return;
        }

        int? id = SchoolRules.PickSkill(StudyPlan(player).Select(skill => (skill.Id, skill.Level, skill.Target)));
        if (id == null)
        {
            if (!caughtUpShown)
            {
                caughtUpShown = true;
                player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_school_caughtup");
            }

            return;
        }

        Skills skills = player.GetSkills();
        Skills.SkillType type = (Skills.SkillType)id.Value;
        Skills.SkillDef definition = skills.m_skills.First(candidate => candidate.m_skill == type);
        // The same experience for every skill, whatever its own step per use.
        float factor = SchoolSettings.StudyExperience.Value / Mathf.Max(0.01f, definition.m_increseStep);
        skills.RaiseSkill(type, factor);
    }

    // Owner only: merges what a writer knows into the bench. The writer list takes only the first name sent, so one
    // write cannot fill it.
    private void RPC_Write(long sender, ZPackage package)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        byte[] packed = package.ReadByteArray();
        if (packed == null || packed.Length > MaxPacked)
        {
            return;
        }

        SchoolLibrary written = SchoolLibrary.FromBytes(packed);
        if (written.Writers.Count > 1)
        {
            written.Writers.RemoveRange(1, written.Writers.Count - 1);
        }

        // Pins are set in the writer's name only, so one write cannot remove or add pins for someone else.
        string writerName = written.Writers.Count > 0 ? written.Writers[0].Name : string.Empty;
        List<SchoolLibrary.Pin> pins = written.Writers.Count > 0
            ? written.Pins.Select(pin => new SchoolLibrary.Pin(pin.Type, pin.Name, pin.Position, writerName)).ToList()
            : new List<SchoolLibrary.Pin>();
        written.Pins.Clear();
        written.Pins.AddRange(pins);

        SchoolLibrary library = SchoolLibrary.FromBytes(nview.GetZDO().GetByteArray(libraryKey));
        library.Merge(written, SchoolSettings.MaxWriters.Value, SchoolSettings.MaxPins.Value, SchoolSettings.PinMergeDistance.Value);
        byte[] merged = library.ToBytes();
        if (merged.Length > MaxPacked)
        {
            Jotunn.Logger.LogWarning($"School Bench: the library would grow to {merged.Length:N0} bytes, more than {MaxPacked:N0}; the write was not kept.");
            return;
        }

        nview.GetZDO().Set(libraryKey, merged);
        SchoolPanel.Refresh(this);
    }

    private static float LevelOf(Skills skills, int id)
    {
        return skills.m_skillData.TryGetValue((Skills.SkillType)id, out Skills.Skill skill) ? skill.m_level : 0f;
    }

    // A station is taught only to a reader who knows every material it is built from.
    private static bool CanBuildStation(string station, HashSet<string> known)
    {
        if (stationPieces == null && ZNetScene.instance != null)
        {
            stationPieces = new Dictionary<string, Piece>();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                CraftingStation craftingStation = prefab != null ? prefab.GetComponent<CraftingStation>() : null;
                Piece stationPiece = prefab != null ? prefab.GetComponent<Piece>() : null;
                if (craftingStation != null && stationPiece != null && !stationPieces.ContainsKey(craftingStation.m_name))
                {
                    stationPieces[craftingStation.m_name] = stationPiece;
                }
            }
        }

        return stationPieces != null && stationPieces.TryGetValue(station, out Piece buildable) && buildable.m_resources != null
            && buildable.m_resources.All(requirement => requirement?.m_resItem == null || known.Contains(requirement.m_resItem.m_itemData.m_shared.m_name));
    }
}
