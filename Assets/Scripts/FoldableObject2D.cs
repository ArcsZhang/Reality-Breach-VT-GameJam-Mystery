using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class FoldableObject2D : MonoBehaviour
{
    [Header("Source Polygon (local space)")]  // Original before fold
    [SerializeField] private Vector2[] sourcePoints;  

    [Header("Optional Collider")]
    [SerializeField] private PolygonCollider2D polygonCollider2D;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh runtimeMesh;
    private Vector2[] originalSourcePoints;
    private bool isInitialized;

    private readonly List<Vector3> meshVertices = new List<Vector3>();
    private readonly List<int> meshTriangles = new List<int>();
    private readonly List<int> triangulationBuffer = new List<int>();

    private void Awake()
    {
        EnsureInitialized();
    }

    [ContextMenu("Capture Source Points From Collider")]
    public void CaptureSourcePointsFromCollider()
    {
        EnsureInitialized();

        if (polygonCollider2D == null || polygonCollider2D.pathCount == 0)
        {
            Debug.LogWarning("No PolygonCollider2D path found to capture.", this);
            return;
        }

        sourcePoints = polygonCollider2D.GetPath(0);
        InitializeSourcePoints();
        ResetFold();
    }

    public void ApplyFold(FoldData2D fold)
    {
        EnsureInitialized();

        List<Vector2> worldPolygon = new List<Vector2>(originalSourcePoints.Length);
        for (int i = 0; i < originalSourcePoints.Length; i++)
        {
            worldPolygon.Add(transform.TransformPoint(originalSourcePoints[i]));
        }

        FoldGeometry2D.ClipStrip(worldPolygon, fold, out List<Vector2> beforeWorld, out List<Vector2> afterWorld);
        Vector2 shift = -fold.Normal * fold.Gap;
        List<Vector2> afterShiftedWorld = FoldGeometry2D.Shift(afterWorld, shift);

        List<Vector2> beforeLocal = WorldToLocal(beforeWorld);
        List<Vector2> afterLocal = WorldToLocal(afterShiftedWorld);

        BuildFromPolygons(beforeLocal, afterLocal, updateCollider: true);
    }

    public void ResetFold()
    {
        EnsureInitialized();
        BuildFromPolygons(new List<Vector2>(originalSourcePoints), null, updateCollider: true);
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (polygonCollider2D == null)
        {
            polygonCollider2D = GetComponent<PolygonCollider2D>();
        }

        InitializeSourcePoints();
        BuildFromPolygons(new List<Vector2>(sourcePoints), null, updateCollider: true);
        isInitialized = true;
    }

    private void InitializeSourcePoints()
    {
        if (sourcePoints == null || sourcePoints.Length < 3)
        {
            if (polygonCollider2D != null && polygonCollider2D.pathCount > 0)
            {
                sourcePoints = polygonCollider2D.GetPath(0);
            }
        }

        if (sourcePoints == null || sourcePoints.Length < 3)
        {
            sourcePoints = new[]
            {
                new Vector2(-0.5f, -0.5f),
                new Vector2(0.5f, -0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-0.5f, 0.5f)
            };
        }

        originalSourcePoints = new Vector2[sourcePoints.Length];
        for (int i = 0; i < sourcePoints.Length; i++)
        {
            originalSourcePoints[i] = sourcePoints[i];
        }
    }

    private List<Vector2> WorldToLocal(List<Vector2> worldPoints)
    {
        List<Vector2> localPoints = new List<Vector2>(worldPoints.Count);
        for (int i = 0; i < worldPoints.Count; i++)
        {
            localPoints.Add(transform.InverseTransformPoint(worldPoints[i]));
        }

        return localPoints;
    }

    private void BuildFromPolygons(List<Vector2> first, List<Vector2> second, bool updateCollider)
    {
        meshVertices.Clear();
        meshTriangles.Clear();

        int pathCount = 0;

        AddPolygonToMesh(first);
        if (first != null && first.Count >= 3)
        {
            pathCount++;
        }

        AddPolygonToMesh(second);
        if (second != null && second.Count >= 3)
        {
            pathCount++;
        }

        if (runtimeMesh == null)
        {
            runtimeMesh = new Mesh
            {
                name = $"{name}_FoldMesh"
            };
            runtimeMesh.MarkDynamic();
        }
        else
        {
            runtimeMesh.Clear();
        }

        bool hasRenderableGeometry = meshVertices.Count > 0 && meshTriangles.Count > 0;
        if (hasRenderableGeometry)
        {
            runtimeMesh.SetVertices(meshVertices);
            runtimeMesh.SetTriangles(meshTriangles, 0);
            runtimeMesh.RecalculateBounds();
            runtimeMesh.RecalculateNormals();
        }

        meshFilter.sharedMesh = runtimeMesh;
        if (meshRenderer != null)
        {
            meshRenderer.enabled = hasRenderableGeometry;
        }

        if (updateCollider && polygonCollider2D != null)
        {
            if (pathCount == 0)
            {
                polygonCollider2D.pathCount = 0;
                polygonCollider2D.enabled = false;
                return;
            }

            polygonCollider2D.enabled = true;
            polygonCollider2D.pathCount = pathCount;

            int writePath = 0;
            if (first != null && first.Count >= 3)
            {
                polygonCollider2D.SetPath(writePath++, first.ToArray());
            }

            if (second != null && second.Count >= 3)
            {
                polygonCollider2D.SetPath(writePath++, second.ToArray());
            }
        }
    }

    private void AddPolygonToMesh(List<Vector2> polygon)
    {
        if (polygon == null || polygon.Count < 3)
        {
            return;
        }

        List<Vector2> poly = new List<Vector2>(polygon);
        FoldGeometry2D.EnsureCounterClockwise(poly);

        triangulationBuffer.Clear();
        if (!FoldGeometry2D.Triangulate(poly, triangulationBuffer))
        {
            return;
        }

        int vertexOffset = meshVertices.Count;
        for (int i = 0; i < poly.Count; i++)
        {
            meshVertices.Add(new Vector3(poly[i].x, poly[i].y, 0f));
        }

        for (int i = 0; i < triangulationBuffer.Count; i++)
        {
            meshTriangles.Add(vertexOffset + triangulationBuffer[i]);
        }
    }
}
