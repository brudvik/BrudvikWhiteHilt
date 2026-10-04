using UnityEngine;

namespace BrudvikWhiteHilt.OldLand;

/// <summary>
/// On every object of a kind <see cref="OldLandFiller"/> places. Old land used to be filled with the height of the biome
/// at each point instead of the terrain the game builds, so near biome borders (most of all at the edges of the Swamp)
/// objects lay buried or floated. When one loads, this puts it back on the ground, as the zone generator would have; the
/// owner saves the new position, so it only moves once.
/// </summary>
public class OldLandGrounding : MonoBehaviour
{
    // Smaller differences are the terrain mesh between its vertices.
    private const float Tolerance = 0.05f;
    private const float RetrySeconds = 1f;
    private const int MaxTries = 30;

    private static int terrainMask;

    /// <summary>Height above the terrain the vegetation entry places the object at.</summary>
    public float GroundOffset;

    private ZNetView view;
    private float nextTry;
    private int tries;

    private void Start()
    {
        view = GetComponent<ZNetView>();
        if (view == null || !view.IsValid())
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (Time.time < nextTry)
        {
            return;
        }

        nextTry = Time.time + RetrySeconds;
        if (view == null || !view.IsValid() || TryGround() || ++tries >= MaxTries)
        {
            enabled = false;
        }
    }

    // Waits until the zone's terrain, with any changes players made to it, is in place under the object.
    private bool TryGround()
    {
        Vector3 position = transform.position;
        Heightmap heightmap = Heightmap.FindHeightmap(position);
        if (heightmap == null || heightmap.IsDistantLod || heightmap.HaveQueuedRebuild())
        {
            return false;
        }

        if (terrainMask == 0)
        {
            terrainMask = LayerMask.GetMask("terrain");
        }

        if (!Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, terrainMask))
        {
            return false;
        }

        float target = hit.point.y + GroundOffset;
        if (Mathf.Abs(target - position.y) <= Tolerance)
        {
            return true;
        }

        // Every client moves its copy; the owner saves it, so the object loads in the right place from then on.
        transform.position = new Vector3(position.x, target, position.z);
        if (view.IsOwner())
        {
            view.GetZDO().SetPosition(transform.position);
        }

        return true;
    }
}
