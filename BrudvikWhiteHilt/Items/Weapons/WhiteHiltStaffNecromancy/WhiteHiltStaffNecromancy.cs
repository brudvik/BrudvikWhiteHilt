using BrudvikWhiteHilt.Necromancy;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltStaffNecromancy;

/// <summary>
/// The White Hilt Necromancer's Staff: a skull on the dark scepter, burning green. It raises skeletons like the Dead
/// Raiser it is made from, and held at a fallen friend's grave it wakes them there (<see cref="Raising"/>).
/// </summary>
public class WhiteHiltStaffNecromancy : WhiteHiltWeaponBase
{
    /// <summary>The staff's prefab name.</summary>
    public const string PrefabName = "WhiteHiltStaffNecromancy";

    /// <summary>The colour of its flame, the raising and everything of the dead.</summary>
    public static readonly Color Green = new(0.3f, 1f, 0.35f);

    // The skull as the preview draft set it on the staff: the staff stood upright (its attach space turned -90° about x
    // and raised 0.9 m), the vanilla trophy skull raised up on the scepter's spike, its face forward.
    private static readonly Matrix4x4 previewStaff = Matrix4x4.TRS(new Vector3(0f, 0.9f, 0f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
    private static readonly Matrix4x4 previewSkull = Matrix4x4.TRS(new Vector3(0f, 2.0347f, -0.0486f), Quaternion.Euler(-60f, 180f, 0f), Vector3.one * 0.9f);
    private static readonly Vector3 previewSkullTop = new(0f, 2.19f, 0f);
    private static readonly Vector3[] previewEyes = { new(-0.056f, 2.124f, 0.095f), new(0.056f, 2.124f, 0.095f) };
    private const float EyeFlameScale = 0.18f;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltStaffNecromancy(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => PrefabName;

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Necromancer's Staff";

    /// <inheritdoc/>
    protected override string Description => "A skull on the dark scepter, burning green. It raises skeletons to fight for you, and at a fallen friend's grave it wakes them there with their gear, at a heavy price in your own life.";

    /// <summary>
    /// The Dead Raiser: its attack raises skeletons.
    /// </summary>
    protected override string CopyFrom => "StaffSkeleton";

    /// <summary>
    /// Dark scepter from Weapon Set by Asylum Nox, with a white grip wrap; the skull is added on top.
    /// </summary>
    protected override string ModelName => "whstaff";

    /// <inheritdoc/>
    protected override string Station => CraftingStations.GaldrTable;

    /// <inheritdoc/>
    protected override int StationLevel => 1;

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "YggdrasilWood", Amount = 10, Recover = false },
        new() { Item = "Eitr", Amount = 16, Recover = false },
        new() { Item = "TrophyDraugrElite", Amount = 1, Recover = false },
        new() { Item = "TrophySkeleton", Amount = 1, Recover = false }
    };

    /// <summary>
    /// Whether a character holds the staff.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>True with the staff in hand.</returns>
    public static bool IsHeldBy(Humanoid character)
    {
        return character != null && character.GetCurrentWeapon()?.m_dropPrefab?.name == PrefabName;
    }

    /// <summary>
    /// Sets the skull on the scepter's spike, lights the green flame on it and in its eyes.
    /// </summary>
    /// <param name="model">The staff model under the attach child.</param>
    protected override void OnModelApplied(GameObject model)
    {
        Transform attach = model.transform.parent;
        Matrix4x4 toAttach = previewStaff.inverse;
        AddSkull(attach, toAttach * previewSkull);

        GameObject equipped = StaffFlame.Apply(model, Green, toAttach.MultiplyPoint3x4(previewSkullTop));
        if (equipped == null)
        {
            return;
        }

        foreach (Vector3 eye in previewEyes)
        {
            GameObject socket = new("eye");
            socket.transform.SetParent(equipped.transform, false);
            socket.transform.localPosition = equipped.transform.InverseTransformPoint(attach.TransformPoint(toAttach.MultiplyPoint3x4(eye)));
            socket.transform.localScale = Vector3.one * EyeFlameScale;
            StaffFlame.CreateFlame(Green, socket.transform);
        }
    }

    // The vanilla trophy skull's meshes, placed by a matrix in attach space: one renderer per mesh, each sub-mesh with
    // its own material.
    private static void AddSkull(Transform attach, Matrix4x4 placement)
    {
        foreach (var mesh in VanillaMeshLibrary.Get("trophy_skull").GroupBy(source => (source.Mesh, source.Matrix)))
        {
            Matrix4x4 matrix = placement * mesh.Key.Matrix;
            GameObject part = new("skull") { layer = attach.gameObject.layer };
            part.transform.SetParent(attach, false);
            part.transform.localPosition = matrix.GetColumn(3);
            part.transform.localRotation = matrix.rotation;
            part.transform.localScale = matrix.lossyScale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh.Key.Mesh;
            Material[] materials = new Material[mesh.Key.Mesh.subMeshCount];
            foreach (VanillaMeshLibrary.MeshSource source in mesh)
            {
                materials[source.SubMesh] = source.Material;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] ??= mesh.First().Material;
            }

            part.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }
    }
}
