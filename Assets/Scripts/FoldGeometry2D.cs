using System.Collections.Generic;
using UnityEngine;

public struct FoldData2D
{
    public Vector2 Normal;
    public float Lo;
    public float Hi;

    public float Gap => Hi - Lo;
}

public static class FoldGeometry2D
{
    public static bool TryBuildFold(Vector2 a, Vector2 b, out FoldData2D fold, float minLength = 0.05f)
    {
        Vector2 delta = b - a;
        float length = delta.magnitude;

        if (length < minLength)
        {
            fold = default;
            return false;
        }

        Vector2 normal = delta / length;
        float da = Vector2.Dot(a, normal);
        float db = Vector2.Dot(b, normal);

        fold = new FoldData2D
        {
            Normal = normal,
            Lo = Mathf.Min(da, db),
            Hi = Mathf.Max(da, db)
        };

        return true;
    }

    public static void ClipStrip(
        IReadOnlyList<Vector2> polygon,
        FoldData2D fold,
        out List<Vector2> before,
        out List<Vector2> after)
    {
        before = ClipToHalfPlane(polygon, fold.Normal, fold.Lo);
        after = ClipToHalfPlane(polygon, -fold.Normal, -fold.Hi);
    }

    public static List<Vector2> Shift(IReadOnlyList<Vector2> polygon, Vector2 delta)
    {
        List<Vector2> shifted = new List<Vector2>(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
        {
            shifted.Add(polygon[i] + delta);
        }

        return shifted;
    }

    public static List<Vector2> ClipToHalfPlane(IReadOnlyList<Vector2> polygon, Vector2 normal, float d)
    {
        List<Vector2> output = new List<Vector2>();
        int count = polygon.Count;

        if (count < 3)
        {
            return output;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % count];

            float da = Vector2.Dot(a, normal) - d;
            float db = Vector2.Dot(b, normal) - d;

            bool insideA = da <= 0f;
            bool insideB = db <= 0f;

            if (insideA)
            {
                output.Add(a);
            }

            if (insideA != insideB)
            {
                float t = da / (da - db);
                output.Add(Vector2.Lerp(a, b, t));
            }
        }

        return output;
    }

    public static float SignedArea(IReadOnlyList<Vector2> polygon)
    {
        float area = 0f;
        int count = polygon.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % count];
            area += a.x * b.y - b.x * a.y;
        }

        return area * 0.5f;
    }

    public static void EnsureCounterClockwise(List<Vector2> polygon)
    {
        if (polygon.Count >= 3 && SignedArea(polygon) < 0f)
        {
            polygon.Reverse();
        }
    }

    // Ear Clipping triangulation algorithm
    public static bool Triangulate(IReadOnlyList<Vector2> polygon, List<int> triangles)
    {
        triangles.Clear();

        if (polygon.Count < 3)
        {
            return false;
        }

        List<Vector2> vertices = new List<Vector2>(polygon);
        EnsureCounterClockwise(vertices);

        List<int> indices = new List<int>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }

        int guard = 0;
        while (indices.Count > 2 && guard < 2048)
        {
            guard++;
            bool earFound = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                Vector2 a = vertices[prev];
                Vector2 b = vertices[curr];
                Vector2 c = vertices[next];

                if (!IsConvex(a, b, c))
                {
                    continue;
                }

                bool containsPoint = false;
                for (int j = 0; j < indices.Count; j++)
                {
                    int p = indices[j];
                    if (p == prev || p == curr || p == next)
                    {
                        continue;
                    }

                    if (PointInTriangle(vertices[p], a, b, c))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (containsPoint)
                {
                    continue;
                }

                triangles.Add(prev);
                triangles.Add(curr);
                triangles.Add(next);
                indices.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                triangles.Clear();
                return false;
            }
        }

        return triangles.Count >= 3;
    }

    private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        return cross > 0f;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Cross(b - a, c - a);
        float area1 = Cross(b - p, c - p);
        float area2 = Cross(c - p, a - p);
        float area3 = Cross(a - p, b - p);

        if (area < 0f)
        {
            area1 = -area1;
            area2 = -area2;
            area3 = -area3;
        }

        return area1 >= 0f && area2 >= 0f && area3 >= 0f;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
}
