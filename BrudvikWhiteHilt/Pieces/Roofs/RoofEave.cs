using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Roofs;

/// <summary>
/// Hides a roof piece's eave overhangs (and the turf log on them) where another roof piece goes on downhill, so only
/// the lowest row of a roof shows them. Looks again every few seconds while the piece is seen.
/// </summary>
public class RoofEave : MonoBehaviour
{
    private const float ProbeRadius = 0.25f;

    private static readonly Collider[] found = new Collider[16];
    private static int pieceMask;

    /// <summary>The overhang objects, one per eave.</summary>
    public GameObject[] m_eaves = new GameObject[0];

    /// <summary>Where to look for a roof going on past each eave, in the piece's space.</summary>
    public Vector3[] m_probes = new Vector3[0];

    private Piece piece;
    private Renderer mainRenderer;
    private float nextCheck;

    private void Awake()
    {
        piece = GetComponent<Piece>();
        mainRenderer = GetComponentInChildren<MeshRenderer>();
        nextCheck = Time.time + Random.Range(0.2f, 0.6f);
    }

    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + Mathf.Max(0.5f, RoofSettings.EaveCheckSeconds.Value) * Random.Range(0.8f, 1.2f);
        if (mainRenderer != null && !mainRenderer.isVisible)
        {
            return;
        }

        if (pieceMask == 0)
        {
            pieceMask = LayerMask.GetMask("piece", "piece_nonsolid");
        }

        for (int i = 0; i < m_eaves.Length && i < m_probes.Length; i++)
        {
            if (m_eaves[i] != null)
            {
                bool covered = RoofContinues(transform.TransformPoint(m_probes[i]));
                if (m_eaves[i].activeSelf == covered)
                {
                    m_eaves[i].SetActive(!covered);
                }
            }
        }
    }

    private bool RoofContinues(Vector3 point)
    {
        int count = Physics.OverlapSphereNonAlloc(point, ProbeRadius, found, pieceMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Piece other = found[i].GetComponentInParent<Piece>();
            if (other != null && other != piece && global::Utils.GetPrefabName(other.gameObject).IndexOf("roof", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
