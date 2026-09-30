using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons.WhiteHiltBow;

/// <summary>
/// Bends the White Hilt Bow's limbs and pulls its string back to the drawing hand while the bow is drawn.
/// Lives on the bow model under the attach child, so it runs on the copy in the character's hand.
/// </summary>
public class WhiteHiltBowFlex : MonoBehaviour
{
    // Attach-space geometry of the whbow mesh, worked out from AssetSource/Models/whbow.weapon.json.
    private static readonly Vector3 grip = new(0.025f, 0f, 0.004f);
    private static readonly Vector3 stringMiddle = new(-0.2133f, -0.0119f, -0.0511f);
    private static readonly Vector3 longAxis = new Vector3(0.2171f, 0.1733f, -0.9606f).normalized;
    private static readonly Vector3 drawAxis = new Vector3(-0.9712f, -0.0605f, -0.2304f).normalized;
    private const float HalfLength = 0.936f;
    private const float StringDistance = 0.15f;
    private const float MaxPull = 0.55f;
    private const float TipBack = 0.14f;
    private const float TipIn = 0.05f;
    private const float ReleaseSpeed = 10f;
    private static readonly int drawPercent = ZSyncAnimation.GetHash("drawpercent");

    private Humanoid owner;
    private VisEquipment equipment;
    private Mesh mesh;
    private Vector3[] rest;
    private Vector3[] current;
    private bool[] isString;
    private float[] limbWeight;
    private float[] limbSide;
    private float pull;
    private float applied = -1f;

    private void Start()
    {
        owner = GetComponentInParent<Humanoid>();
        equipment = GetComponentInParent<VisEquipment>();
        MeshFilter filter = GetComponent<MeshFilter>();
        if (owner == null || equipment == null || filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable)
        {
            enabled = false;
            return;
        }

        mesh = Instantiate(filter.sharedMesh);
        mesh.MarkDynamic();
        filter.sharedMesh = mesh;
        rest = mesh.vertices;
        current = new Vector3[rest.Length];
        isString = new bool[rest.Length];
        limbWeight = new float[rest.Length];
        limbSide = new float[rest.Length];
        for (int i = 0; i < rest.Length; i++)
        {
            Vector3 offset = rest[i] - grip;
            float along = Vector3.Dot(offset, longAxis) / HalfLength;
            isString[i] = Mathf.Abs(along) < 0.1f && Vector3.Dot(offset, drawAxis) > StringDistance;
            float bend = Mathf.Clamp01((Mathf.Abs(along) - 0.15f) / 0.85f);
            limbWeight[i] = bend * bend;
            limbSide[i] = Mathf.Sign(along);
        }
    }

    private void LateUpdate()
    {
        float target = 0f;
        if (equipment.m_leftItemInstance != null && transform.IsChildOf(equipment.m_leftItemInstance.transform))
        {
            float draw = owner == Player.m_localPlayer
                ? owner.GetAttackDrawPercentage()
                : owner.m_animator != null ? owner.m_animator.GetFloat(drawPercent) : 0f;
            if (draw > 0.01f)
            {
                Vector3 hand = transform.InverseTransformPoint(equipment.m_rightHand.position);
                target = Mathf.Clamp(Vector3.Dot(hand - stringMiddle, drawAxis), 0f, MaxPull) * Mathf.Clamp01(draw * 4f);
            }
        }

        // The string follows the hand back, and snaps forward when the arrow is loosed.
        pull = target >= pull ? target : Mathf.MoveTowards(pull, target, ReleaseSpeed * Time.deltaTime);
        if (Mathf.Abs(pull - applied) < 0.001f)
        {
            return;
        }

        applied = pull;
        float flex = pull / MaxPull;
        Vector3 stringOffset = drawAxis * pull;
        for (int i = 0; i < rest.Length; i++)
        {
            current[i] = isString[i]
                ? rest[i] + stringOffset
                : rest[i] + (drawAxis * TipBack - longAxis * (limbSide[i] * TipIn)) * (HalfLength * flex * limbWeight[i]);
        }

        mesh.vertices = current;
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);
        }
    }
}
