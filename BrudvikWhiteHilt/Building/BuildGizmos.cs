using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// Lines drawn in the world for building: coloured axes on the piece being placed, and rings showing how far the
/// build camera may fly when it reaches the edge.
/// </summary>
public static class BuildGizmos
{
    private const int RingSegments = 64;
    private const int MaxRings = 6;
    private const float RingShowSeconds = 2f;

    private static readonly Color ringColor = new(1f, 0.7f, 0.25f, 0.6f);
    private static readonly Color pathColor = new(0.35f, 0.75f, 1f, 0.9f);
    private static readonly List<LineRenderer> rings = new();
    private static readonly List<LineRenderer> paths = new();

    private static GameObject root;
    private static Material material;
    private static LineRenderer[] axes;
    private static LineRenderer boxLine;
    private static LineRenderer circle;
    private static GameObject axesGhost;
    private static float axesLength;
    private static float ringsUntil;

    /// <summary>
    /// Keeps the axes on the ghost, or hides them.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void UpdateAxes(Player player)
    {
        GameObject ghost = player.m_placementGhost;
        bool show = BuildToolSettings.ShowAxes.Value && !Media.MediaMode.HideUi && ghost != null && ghost.activeSelf && player.InPlaceMode() && EnsureRoot();
        if (!show)
        {
            SetAxesActive(false);
            return;
        }

        if (axes == null)
        {
            axes = new[]
            {
                CreateLine("AxisX", new Color(1f, 0.25f, 0.2f), 0.03f),
                CreateLine("AxisY", new Color(0.3f, 1f, 0.3f), 0.03f),
                CreateLine("AxisZ", new Color(0.3f, 0.55f, 1f), 0.03f)
            };
        }

        if (ghost != axesGhost)
        {
            axesGhost = ghost;
            axesLength = GhostSize(ghost);
        }

        Vector3 origin = ghost.transform.position;
        Vector3[] directions = { ghost.transform.right, ghost.transform.up, ghost.transform.forward };
        for (int i = 0; i < axes.Length; i++)
        {
            axes[i].SetPosition(0, origin);
            axes[i].SetPosition(1, origin + directions[i] * axesLength);
            axes[i].gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Shows the rings of the allowed camera area for a moment.
    /// </summary>
    public static void ShowRange()
    {
        ringsUntil = Time.unscaledTime + RingShowSeconds;
    }

    /// <summary>
    /// Draws the rings of the allowed camera area around the camera's height while they are to be shown.
    /// </summary>
    /// <param name="spheres">Centre and radius of every area the camera may be in.</param>
    /// <param name="cameraPosition">Where the camera is.</param>
    public static void UpdateRange(IEnumerable<(Vector3 Centre, float Radius)> spheres, Vector3 cameraPosition)
    {
        int used = 0;
        if (Time.unscaledTime < ringsUntil && EnsureRoot())
        {
            foreach ((Vector3 centre, float radius) in spheres)
            {
                float height = cameraPosition.y - centre.y;
                if (used >= MaxRings || Mathf.Abs(height) >= radius)
                {
                    continue;
                }

                // The ring is the sphere's cut at the camera's height, where the camera meets the edge.
                float ringRadius = Mathf.Sqrt(radius * radius - height * height);
                if (used == rings.Count)
                {
                    LineRenderer line = CreateLine("RangeRing", ringColor, 0.06f);
                    line.positionCount = RingSegments;
                    line.loop = true;
                    rings.Add(line);
                }

                LineRenderer ring = rings[used++];
                for (int i = 0; i < RingSegments; i++)
                {
                    float angle = i * Mathf.PI * 2f / RingSegments;
                    ring.SetPosition(i, new Vector3(centre.x + Mathf.Cos(angle) * ringRadius, cameraPosition.y, centre.z + Mathf.Sin(angle) * ringRadius));
                }

                ring.gameObject.SetActive(true);
            }
        }

        for (int i = used; i < rings.Count; i++)
        {
            rings[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Draws a line through points, e.g. the outline of a ramp.
    /// </summary>
    /// <param name="path">The points.</param>
    public static void ShowPath(Vector3[] path)
    {
        if (!EnsureRoot())
        {
            return;
        }

        if (boxLine == null)
        {
            boxLine = CreateLine("SelectionBox", new Color(0.35f, 0.75f, 1f, 0.9f), 0.05f);
        }

        boxLine.positionCount = path.Length;
        boxLine.SetPositions(path);
        boxLine.gameObject.SetActive(true);
    }

    /// <summary>
    /// Draws several separate lines, e.g. both edges of a planned moat.
    /// </summary>
    /// <param name="lines">The lines.</param>
    public static void ShowPaths(IList<Vector3[]> lines)
    {
        if (!EnsureRoot())
        {
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            if (i == paths.Count)
            {
                paths.Add(CreateLine("Path", pathColor, 0.06f));
            }

            paths[i].positionCount = lines[i].Length;
            paths[i].SetPositions(lines[i]);
            paths[i].gameObject.SetActive(true);
        }

        for (int i = lines.Count; i < paths.Count; i++)
        {
            paths[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Hides the lines drawn by <see cref="ShowPaths"/>.
    /// </summary>
    public static void HidePaths()
    {
        foreach (LineRenderer line in paths)
        {
            if (line != null)
            {
                line.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Draws a flat circle, e.g. the size of the hoe's brush.
    /// </summary>
    /// <param name="centre">Centre of the circle.</param>
    /// <param name="radius">Radius in metres.</param>
    public static void ShowCircle(Vector3 centre, float radius)
    {
        if (!EnsureRoot())
        {
            return;
        }

        if (circle == null)
        {
            circle = CreateLine("BrushCircle", new Color(1f, 0.85f, 0.4f, 0.8f), 0.05f);
            circle.positionCount = RingSegments;
            circle.loop = true;
        }

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            Vector3 point = centre + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            point.y = Heightmap.GetHeight(point, out float ground) ? ground + 0.1f : centre.y;
            circle.SetPosition(i, point);
        }

        circle.gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the brush circle.
    /// </summary>
    public static void HideCircle()
    {
        if (circle != null)
        {
            circle.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Draws the outline of a box, e.g. a selection box being drawn.
    /// </summary>
    /// <param name="box">The box.</param>
    public static void ShowBox(Bounds box)
    {
        Vector3 a = box.min;
        Vector3 b = box.max;
        ShowPath(new Vector3[]
        {
            new(a.x, a.y, a.z), new(b.x, a.y, a.z), new(b.x, a.y, b.z), new(a.x, a.y, b.z), new(a.x, a.y, a.z),
            new(a.x, b.y, a.z), new(b.x, b.y, a.z), new(b.x, a.y, a.z), new(b.x, b.y, a.z), new(b.x, b.y, b.z),
            new(b.x, a.y, b.z), new(b.x, b.y, b.z), new(a.x, b.y, b.z), new(a.x, a.y, b.z), new(a.x, b.y, b.z), new(a.x, b.y, a.z)
        });
    }

    /// <summary>
    /// Hides the box outline.
    /// </summary>
    public static void HideBox()
    {
        if (boxLine != null)
        {
            boxLine.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Hides every line.
    /// </summary>
    public static void HideAll()
    {
        HideBox();
        HidePaths();
        SetAxesActive(false);
        ringsUntil = 0f;
        foreach (LineRenderer ring in rings)
        {
            ring.gameObject.SetActive(false);
        }
    }

    private static void SetAxesActive(bool active)
    {
        if (axes == null)
        {
            return;
        }

        foreach (LineRenderer axis in axes)
        {
            axis.gameObject.SetActive(active);
        }
    }

    private static bool EnsureRoot()
    {
        if (root != null)
        {
            return true;
        }

        // The lines live in the game scene; after a logout the old ones are gone.
        axes = null;
        boxLine = null;
        circle = null;
        rings.Clear();
        paths.Clear();
        if (material == null)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null)
            {
                return false;
            }

            material = new Material(shader);
        }

        root = new GameObject("WhiteHiltBuildGizmos");
        return true;
    }

    private static LineRenderer CreateLine(string name, Color color, float width)
    {
        GameObject lineObject = new(name);
        lineObject.transform.SetParent(root.transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = width;
        line.endWidth = width;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.positionCount = 2;
        return line;
    }

    private static float GhostSize(GameObject ghost)
    {
        Bounds bounds = default;
        bool any = false;
        foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            if (any)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            else
            {
                bounds = renderer.bounds;
                any = true;
            }
        }

        return any ? Mathf.Clamp(bounds.extents.magnitude * 0.8f, 0.5f, 3f) : 1f;
    }
}
