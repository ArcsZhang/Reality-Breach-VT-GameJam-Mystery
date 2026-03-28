using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SnapshotAbility : Ability
{
    [Serializable]
    public sealed class SnapshotCapturedEntry
    {
        public GameObject Original;
        public Vector2 OffsetFromCenter;
        public bool SourceIsTrigger;

        /// <summary> Polygon of the overlapping region in world space (from collider path clipped to the rect), or null if none / not a polygon collider. </summary>
        public List<Vector2> ClippedPolygonWorld;

        /// <summary> Centroid of <see cref="ClippedPolygonWorld"/> at capture time; default if no clipped polygon. </summary>
        public Vector2 ClippedCentroidWorld;
    }

    public struct SnapshotRectangle2D
    {
        public Vector2 Min;
        public Vector2 Max;

        public Vector2 Center => (Min + Max) * 0.5f;
        public Vector2 Size => Max - Min;

        public SnapshotRectangle2D(Vector2 a, Vector2 b)
        {
            Min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            Max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        public bool Contains(Vector2 point)
        {
            return point.x >= Min.x && point.x <= Max.x && point.y >= Min.y && point.y <= Max.y;
        }
    }

    [Header("Snapshot HUD")]
    [Tooltip("Optional. If null, a screen overlay with a RawImage is created at runtime.")]
    [SerializeField] private RawImage snapshotHudImage;

    [Tooltip("World-space offset from the follow target (e.g. above the player).")]
    [SerializeField] private Vector3 hudFollowWorldOffset;

    [Tooltip("Extra nudge in canvas space after placement (small UI tweak).")]
    [SerializeField] private Vector2 hudScreenPixelOffset;

    [Tooltip("Assign the player (or camera rig). If null, tries tag \"Player\", then falls back to the main camera.")]
    [SerializeField] private Transform hudFollowTarget;

    [Tooltip("Z of the capture plane for screen projection (usually 0 for 2D).")]
    [SerializeField] private float hudCaptureWorldZ;

    [Tooltip("If the RawImage sits under a layout group, this stops the group from forcing a square/strip size.")]
    [SerializeField] private bool hudIgnoreParentLayout = true;

    [SerializeField] private int snapshotTextureMaxEdge = 512;
    [SerializeField] private float minSnapshotWorldSize = 0.05f;

    private Vector2 dragStartPoint;
    private Vector2 dragEndPoint;
    private bool isDraggingSnapshot;

    private SnapshotRectangle2D previewSnapshot;
    private bool hasPreviewSnapshot;

    private LineRenderer previewBorderLineRenderer;
    private MeshFilter previewFillMeshFilter;
    private MeshRenderer previewFillMeshRenderer;
    private Mesh previewFillMesh;

    private SnapshotableObject[] snapshottables;
    private SnapshotCapturedEntry[] currentSnapshot;

    private RenderTexture snapshotRenderTexture;
    private Camera snapshotCamera;

    private SnapshotRectangle2D lastCapturedRectWorld;

    /// <summary> Parent-local offset from follow anchor to capture rect center (same space as <see cref="RectTransform.anchoredPosition"/>). Avoids pixel drift from mixing screen-space deltas with UI conversion. </summary>
    private Vector2 hudParentLocalOffsetFromFollow;

    public IReadOnlyList<SnapshotCapturedEntry> CurrentSnapshot =>
        currentSnapshot ?? Array.Empty<SnapshotCapturedEntry>();

    public SnapshotRectangle2D LastCapturedRectWorld => lastCapturedRectWorld;
    public bool HasHudSnapshot => snapshotHudImage != null && snapshotHudImage.texture != null;

    public void Awake()
    {
        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        InitializeVisuals();
        EnsureSnapshotHud();
        if (snapshotHudImage != null && snapshotHudImage.texture == null)
        {
            snapshotHudImage.enabled = false;
        }

        CacheSnapshottables();
        EnsureSnapshotRenderCamera();
        TryAssignDefaultHudFollowTarget();
    }

    private void TryAssignDefaultHudFollowTarget()
    {
        if (hudFollowTarget != null)
        {
            return;
        }

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            hudFollowTarget = playerGo.transform;
        }
    }

    private void LateUpdate()
    {
        UpdateSnapshotHudFollow();
    }

    private void OnDestroy()
    {
        if (snapshotRenderTexture != null)
        {
            snapshotRenderTexture.Release();
            Destroy(snapshotRenderTexture);
        }

        if (snapshotCamera != null)
        {
            Destroy(snapshotCamera.gameObject);
        }
    }

    public override void onAbilitySwitch()
    {
        cancelSnapshotSelection();
    }

    public override void onClear()
    {
        cancelSnapshotSelection();
    }

    public override void onUpdate()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        Vector2 mouseWorldPoint = GetMouseWorldPoint(mouse.position.ReadValue());

        if (mouse.leftButton.wasPressedThisFrame)
        {
            beginSnapshotSelection(mouseWorldPoint);
        }

        if (!isDraggingSnapshot)
        {
            return;
        }

        updateSnapshotPreview();

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (hasPreviewSnapshot)
            {
                takeSnapshot(previewSnapshot);
            }

            cancelSnapshotSelection();
        }
    }

    private void beginSnapshotSelection(Vector2 worldPoint)
    {
        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        dragStartPoint = worldPoint;
        isDraggingSnapshot = true;
    }

    private void updateSnapshotPreview()
    {
        dragEndPoint = GetMouseWorldPoint(Mouse.current.position.ReadValue());
        SnapshotRectangle2D rect = new SnapshotRectangle2D(dragStartPoint, dragEndPoint);

        hasPreviewSnapshot = true;
        previewSnapshot = rect;
        updatePreviewVisuals(rect);

        for (int i = 0; i < snapshottables.Length; i++)
        {
            if (snapshottables[i] == null)
            {
                continue;
            }

            snapshottables[i].SetHighlighted(OverlapsRect(snapshottables[i], rect));
        }
    }

    private void updatePreviewVisuals(SnapshotRectangle2D rect)
    {
        Vector2 bottomLeft = new Vector2(rect.Min.x, rect.Min.y);
        Vector2 bottomRight = new Vector2(rect.Max.x, rect.Min.y);
        Vector2 topRight = new Vector2(rect.Max.x, rect.Max.y);
        Vector2 topLeft = new Vector2(rect.Min.x, rect.Max.y);

        previewBorderLineRenderer.SetPosition(0, bottomLeft);
        previewBorderLineRenderer.SetPosition(1, bottomRight);
        previewBorderLineRenderer.SetPosition(2, topRight);
        previewBorderLineRenderer.SetPosition(3, topLeft);
        previewBorderLineRenderer.enabled = true;

        previewFillMesh.Clear();
        previewFillMesh.vertices = new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
        previewFillMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        previewFillMesh.RecalculateBounds();
        previewFillMeshRenderer.enabled = true;
    }

    private void InitializeVisuals()
    {
        GameObject borderObj = new GameObject("SnapshotPreviewBorder");
        borderObj.transform.parent = transform;
        previewBorderLineRenderer = borderObj.AddComponent<LineRenderer>();
        previewBorderLineRenderer.positionCount = 4;
        previewBorderLineRenderer.loop = true;
        previewBorderLineRenderer.widthMultiplier = 0.05f;
        previewBorderLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        previewBorderLineRenderer.startColor = Color.cyan;
        previewBorderLineRenderer.endColor = Color.cyan;
        previewBorderLineRenderer.enabled = false;

        GameObject fillObj = new GameObject("SnapshotPreviewFill");
        fillObj.transform.parent = transform;
        previewFillMeshFilter = fillObj.AddComponent<MeshFilter>();
        previewFillMeshRenderer = fillObj.AddComponent<MeshRenderer>();
        previewFillMeshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        previewFillMeshRenderer.material.color = new Color(0f, 1f, 1f, 0.5f);
        previewFillMesh = new Mesh();
        previewFillMeshFilter.mesh = previewFillMesh;
        previewFillMeshRenderer.enabled = false;
    }

    private void EnsureSnapshotHud()
    {
        if (snapshotHudImage != null)
        {
            return;
        }

        GameObject canvasGo = new GameObject("SnapshotHudCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject rawGo = new GameObject("SnapshotPreview");
        rawGo.transform.SetParent(canvasGo.transform, false);
        RawImage raw = rawGo.AddComponent<RawImage>();
        RectTransform rt = rawGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        snapshotHudImage = raw;
    }

    private void EnsureSnapshotRenderCamera()
    {
        if (snapshotCamera != null)
        {
            return;
        }

        GameObject camGo = new GameObject("SnapshotRenderCamera");
        camGo.transform.SetParent(transform, false);
        snapshotCamera = camGo.AddComponent<Camera>();
        snapshotCamera.enabled = false;
        snapshotCamera.clearFlags = CameraClearFlags.SolidColor;
        snapshotCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        snapshotCamera.orthographic = true;
        snapshotCamera.nearClipPlane = WorldCamera != null ? WorldCamera.nearClipPlane : 0.3f;
        snapshotCamera.farClipPlane = WorldCamera != null ? WorldCamera.farClipPlane : 1000f;
        if (WorldCamera != null)
        {
            snapshotCamera.cullingMask = WorldCamera.cullingMask;
        }
    }

    private void cancelSnapshotSelection()
    {
        isDraggingSnapshot = false;
        hasPreviewSnapshot = false;
        previewBorderLineRenderer.enabled = false;
        previewFillMeshRenderer.enabled = false;

        for (int i = 0; i < snapshottables.Length; i++)
        {
            if (snapshottables[i] != null)
            {
                snapshottables[i].SetHighlighted(false);
            }
        }
    }

    private void CacheSnapshottables()
    {
        snapshottables = FindObjectsByType<SnapshotableObject>();
    }

    private void takeSnapshot(SnapshotRectangle2D rect)
    {
        Vector2 size = rect.Size;
        if (size.x < minSnapshotWorldSize || size.y < minSnapshotWorldSize)
        {
            return;
        }

        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        EnsureSnapshotRenderCamera();

        RenderWorldRectToSnapshotTexture(rect);
        FillCapturedObjects(rect);

        lastCapturedRectWorld = rect;
        RefreshHudParentLocalOffsetFromFollow(rect);
        UpdateSnapshotHudFollow();

        Debug.Log($"Snapshot captured: {currentSnapshot?.Length ?? 0} object(s).");
    }

    private void RefreshHudParentLocalOffsetFromFollow(SnapshotRectangle2D rect)
    {
        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        if (WorldCamera == null || snapshotHudImage == null)
        {
            return;
        }

        if (!TryGetHudParentCanvasAndEventCamera(out RectTransform parentRt, out Canvas root, out Camera eventCam))
        {
            return;
        }

        Transform follow = hudFollowTarget != null ? hudFollowTarget : WorldCamera.transform;
        Vector3 captureWorld = new Vector3(rect.Center.x, rect.Center.y, hudCaptureWorldZ);
        Vector3 followWorld = follow.position + hudFollowWorldOffset;

        Vector3 scCapture = WorldCamera.WorldToScreenPoint(captureWorld);
        Vector3 scFollow = WorldCamera.WorldToScreenPoint(followWorld);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, scCapture, eventCam, out Vector2 localCapture))
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, scFollow, eventCam, out Vector2 localFollow))
        {
            return;
        }

        hudParentLocalOffsetFromFollow = localCapture - localFollow;
    }

    private bool TryGetHudParentCanvasAndEventCamera(out RectTransform parentRt, out Canvas root, out Camera eventCam)
    {
        parentRt = null;
        root = null;
        eventCam = null;

        if (snapshotHudImage == null)
        {
            return false;
        }

        root = snapshotHudImage.canvas.rootCanvas;
        parentRt = snapshotHudImage.transform.parent as RectTransform;
        if (parentRt == null)
        {
            parentRt = root.transform as RectTransform;
        }

        if (parentRt == null)
        {
            return false;
        }

        eventCam = GetCanvasEventCamera(root);
        return true;
    }

    private void FillCapturedObjects(SnapshotRectangle2D rect)
    {
        Vector2 center = rect.Center;
        List<SnapshotCapturedEntry> captured = new List<SnapshotCapturedEntry>();

        for (int i = 0; i < snapshottables.Length; i++)
        {
            if (snapshottables[i] == null)
            {
                continue;
            }

            if (!OverlapsRect(snapshottables[i], rect))
            {
                continue;
            }

            Vector2 objectPos = snapshottables[i].transform.position;
            bool capturedAnyPiece = false;

            PolygonCollider2D poly = snapshottables[i].GetComponent<PolygonCollider2D>();
            Collider2D sourceCollider = snapshottables[i].GetComponent<Collider2D>();
            bool sourceIsTrigger = (poly != null && poly.isTrigger) || (sourceCollider != null && sourceCollider.isTrigger);

            if (poly != null && poly.pathCount > 0)
            {
                for (int pathIndex = 0; pathIndex < poly.pathCount; pathIndex++)
                {
                    Vector2[] path = poly.GetPath(pathIndex);
                    var world = new List<Vector2>(path.Length);
                    for (int p = 0; p < path.Length; p++)
                    {
                        world.Add(poly.transform.TransformPoint(path[p]));
                    }

                    List<Vector2> clipped = FoldGeometry2D.ClipPolygonToAxisAlignedRect(world, rect.Min, rect.Max);
                    if (clipped == null || clipped.Count < 3)
                    {
                        continue;
                    }

                    var entry = new SnapshotCapturedEntry
                    {
                        Original = snapshottables[i].gameObject,
                        OffsetFromCenter = objectPos - center,
                        SourceIsTrigger = sourceIsTrigger,
                        ClippedPolygonWorld = clipped,
                        ClippedCentroidWorld = ComputePolygonCentroid(clipped)
                    };

                    captured.Add(entry);
                    capturedAnyPiece = true;
                }
            }
            else
            {
                var entry = new SnapshotCapturedEntry
                {
                    Original = snapshottables[i].gameObject,
                    OffsetFromCenter = objectPos - center,
                    SourceIsTrigger = sourceIsTrigger,
                    ClippedPolygonWorld = null
                };

                captured.Add(entry);
                capturedAnyPiece = true;
            }

            if (capturedAnyPiece)
            {
                snapshottables[i].Freeze();
            }
        }

        currentSnapshot = captured.ToArray();
    }

    private static Vector2 ComputePolygonCentroid(IReadOnlyList<Vector2> polygon)
    {
        if (polygon == null || polygon.Count == 0)
        {
            return Vector2.zero;
        }

        Vector2 sum = Vector2.zero;
        for (int i = 0; i < polygon.Count; i++)
        {
            sum += polygon[i];
        }

        return sum / polygon.Count;
    }

    /// <summary>
    /// Called from <see cref="AbilityManager"/> when Snapshot is already active: paste held capture and hide the HUD.
    /// Uses the <b>HUD preview center</b> on screen (world point under the floating RawImage), not the mouse — so placement matches the preview.
    /// </summary>
    public bool TryPasteSnapshotAtCursor()
    {
        if (currentSnapshot == null || currentSnapshot.Length == 0)
        {
            return false;
        }

        pasteSnapshotAtWorld(GetPasteAnchorWorld());
        return true;
    }

    /// <summary>
    /// World point where the snapshot rect center should land: under the HUD image center (matches follow-target preview), else mouse.
    /// </summary>
    private Vector2 GetPasteAnchorWorld()
    {
        if (TryGetHudCenterScreenPoint(out Vector2 screenPt))
        {
            return ScreenPositionToWorldPoint(screenPt);
        }

        Mouse mouse = Mouse.current;
        return mouse != null
            ? ScreenPositionToWorldPoint(mouse.position.ReadValue())
            : Vector2.zero;
    }

    private bool TryGetHudCenterScreenPoint(out Vector2 screenPoint)
    {
        screenPoint = default;
        if (snapshotHudImage == null || !snapshotHudImage.enabled || snapshotHudImage.texture == null)
        {
            return false;
        }

        RectTransform rt = snapshotHudImage.rectTransform;
        Canvas root = snapshotHudImage.canvas != null ? snapshotHudImage.canvas.rootCanvas : null;
        Camera uiCam = root != null && root.renderMode == RenderMode.ScreenSpaceCamera ? root.worldCamera : null;
        Vector3 worldUi = rt.TransformPoint(rt.rect.center);
        screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, worldUi);
        return true;
    }

    private Vector2 ScreenPositionToWorldPoint(Vector2 screenPosition)
    {
        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        float z = Mathf.Abs(WorldCamera.transform.position.z);
        Vector3 world = WorldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, z));
        return new Vector2(world.x, world.y);
    }

    /// <summary>
    /// Spawns a rigid fragment per clipped entry, unfreezes all captured originals, clears the held snapshot HUD data.
    /// Fragment pivots sit on the clipped centroid; layout is preserved by aligning capture rect center to <paramref name="pasteWorld"/>.
    /// </summary>
    private void pasteSnapshotAtWorld(Vector2 pasteWorld)
    {
        Vector2 rectCenter = lastCapturedRectWorld.Center;

        for (int i = 0; i < currentSnapshot.Length; i++)
        {
            SnapshotCapturedEntry entry = currentSnapshot[i];
            if (entry.Original == null)
            {
                continue;
            }

            SnapshotableObject originalSnap = entry.Original.GetComponent<SnapshotableObject>();
            if (originalSnap != null)
            {
                originalSnap.Unfreeze();
            }

            if (entry.ClippedPolygonWorld == null || entry.ClippedPolygonWorld.Count < 3)
            {
                continue;
            }

            float z = entry.Original.transform.position.z;
            Vector2 fragmentWorldPos = pasteWorld + (entry.ClippedCentroidWorld - rectCenter);
            SpawnClippedFragment(entry, fragmentWorldPos, z);
        }

        currentSnapshot = Array.Empty<SnapshotCapturedEntry>();
        if (snapshotHudImage != null)
        {
            snapshotHudImage.texture = null;
            snapshotHudImage.enabled = false;
        }

        CacheSnapshottables();
    }

    private void SpawnClippedFragment(SnapshotCapturedEntry entry, Vector2 worldPosition, float z)
    {
        IReadOnlyList<Vector2> worldPoly = entry.ClippedPolygonWorld;
        int n = worldPoly.Count;
        Vector2 centroid = entry.ClippedCentroidWorld;

        var localPath = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            localPath[i] = worldPoly[i] - centroid;
        }

        GameObject go = new GameObject("SnapshotFragment");
        go.transform.SetPositionAndRotation(new Vector3(worldPosition.x, worldPosition.y, z), entry.Original.transform.rotation);
        go.layer = entry.Original.layer;

        PolygonCollider2D poly = go.AddComponent<PolygonCollider2D>();
        poly.SetPath(0, localPath);
        poly.isTrigger = entry.SourceIsTrigger;

        Mesh mesh = BuildConvexPolygonMesh2D(localPath);
        var meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateFragmentMaterial(entry.Original);

        Rigidbody2D srcRb = entry.Original.GetComponent<Rigidbody2D>();
        if (srcRb != null)
        {
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = srcRb.bodyType;
            float fragArea = polygonArea(localPath);
            PolygonCollider2D origPoly = entry.Original.GetComponent<PolygonCollider2D>();
            float origArea = origPoly != null ? polygonArea(origPoly) : 0f;
            rb.mass = origArea > 1e-5f
                ? Mathf.Max(0.0001f, srcRb.mass * (fragArea / origArea))
                : srcRb.mass;
            rb.gravityScale = srcRb.gravityScale;
            rb.linearDamping = srcRb.linearDamping;
            rb.angularDamping = srcRb.angularDamping;
            rb.constraints = srcRb.constraints;
            rb.interpolation = srcRb.interpolation;
            rb.collisionDetectionMode = srcRb.collisionDetectionMode;
        }

        SpriteRenderer origSprite = entry.Original.GetComponent<SpriteRenderer>();
        if (origSprite != null)
        {
            meshRenderer.sortingLayerID = origSprite.sortingLayerID;
            meshRenderer.sortingOrder = origSprite.sortingOrder;
        }

        go.AddComponent<SnapshotableObject>();
    }

    private static float polygonArea(Vector2[] localLoop)
    {
        double a = 0.0;
        for (int i = 0; i < localLoop.Length; i++)
        {
            Vector2 p = localLoop[i];
            Vector2 q = localLoop[(i + 1) % localLoop.Length];
            a += (double)p.x * q.y - (double)p.y * q.x;
        }

        return Mathf.Abs((float)(a * 0.5));
    }

    private static float polygonArea(PolygonCollider2D poly)
    {
        if (poly == null || poly.pathCount == 0)
        {
            return 1f;
        }

        Vector2[] path = poly.GetPath(0);
        return polygonArea(path);
    }

    private static Material CreateFragmentMaterial(GameObject original)
    {
        var sr = original.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sharedMaterial != null)
        {
            var mat = new Material(sr.sharedMaterial);
            mat.color = sr.color;
            return mat;
        }

        var mr = original.GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterial != null)
        {
            return new Material(mr.sharedMaterial);
        }

        var m = new Material(Shader.Find("Sprites/Default"));
        m.color = Color.white;
        return m;
    }

    /// <summary> Fan triangulation (valid for convex polygons produced by axis-aligned clipping of a convex loop). </summary>
    private static Mesh BuildConvexPolygonMesh2D(Vector2[] localLoop)
    {
        int n = localLoop.Length;
        var vertices = new Vector3[n];
        var uvs = new Vector2[n];
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            Vector2 p = localLoop[i];
            vertices[i] = new Vector3(p.x, p.y, 0f);
            minX = Mathf.Min(minX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxX = Mathf.Max(maxX, p.x);
            maxY = Mathf.Max(maxY, p.y);
        }

        float dx = Mathf.Max(1e-6f, maxX - minX);
        float dy = Mathf.Max(1e-6f, maxY - minY);
        for (int i = 0; i < n; i++)
        {
            uvs[i] = new Vector2((localLoop[i].x - minX) / dx, (localLoop[i].y - minY) / dy);
        }

        var triangles = new List<int>((n - 2) * 3);
        for (int i = 1; i < n - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        var mesh = new Mesh
        {
            name = "SnapshotFragmentMesh",
            vertices = vertices,
            uv = uvs,
            triangles = triangles.ToArray()
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private void RenderWorldRectToSnapshotTexture(SnapshotRectangle2D rect)
    {
        float w = rect.Size.x;
        float h = rect.Size.y;
        float aspect = w / h;

        int heightPx = snapshotTextureMaxEdge;
        int widthPx = Mathf.Max(16, Mathf.RoundToInt(heightPx * aspect));

        if (snapshotRenderTexture == null || snapshotRenderTexture.width != widthPx || snapshotRenderTexture.height != heightPx)
        {
            if (snapshotRenderTexture != null)
            {
                snapshotRenderTexture.Release();
                Destroy(snapshotRenderTexture);
            }

            snapshotRenderTexture = new RenderTexture(widthPx, heightPx, 0, RenderTextureFormat.ARGB32)
            {
                name = "SnapshotRT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear
            };
            snapshotRenderTexture.Create();
        }

        float camZ = WorldCamera.transform.position.z;
        Vector3 center = new Vector3(rect.Center.x, rect.Center.y, camZ);
        snapshotCamera.transform.SetPositionAndRotation(center, WorldCamera.transform.rotation);
        snapshotCamera.orthographicSize = h * 0.5f;
        snapshotCamera.aspect = w / h;
        snapshotCamera.orthographic = true;
        snapshotCamera.targetTexture = snapshotRenderTexture;
        snapshotCamera.Render();

        snapshotCamera.targetTexture = null;

        if (snapshotHudImage != null)
        {
            snapshotHudImage.enabled = true;
            snapshotHudImage.texture = snapshotRenderTexture;
            snapshotHudImage.color = Color.white;
            snapshotHudImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            LayoutSnapshotHudToWorldSelection(rect);
        }
    }

    /// <summary> Sizes the RawImage to match the on-screen footprint of the world selection (no scaling up). </summary>
    private void LayoutSnapshotHudToWorldSelection(SnapshotRectangle2D rect)
    {
        if (snapshotHudImage == null || snapshotRenderTexture == null)
        {
            return;
        }

        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        if (WorldCamera == null)
        {
            return;
        }

        RectTransform hudRt = snapshotHudImage.rectTransform;
        hudRt.anchorMin = hudRt.anchorMax = new Vector2(0.5f, 0.5f);
        hudRt.pivot = new Vector2(0.5f, 0.5f);

        if (!TryGetHudParentCanvasAndEventCamera(out RectTransform parentRt, out _, out Camera eventCam))
        {
            return;
        }

        float z = hudCaptureWorldZ;
        Vector3[] corners =
        {
            new Vector3(rect.Min.x, rect.Min.y, z),
            new Vector3(rect.Max.x, rect.Min.y, z),
            new Vector3(rect.Min.x, rect.Max.y, z),
            new Vector3(rect.Max.x, rect.Max.y, z)
        };

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector3 worldCorner in corners)
        {
            Vector3 screen = WorldCamera.WorldToScreenPoint(worldCorner);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt,
                    screen,
                    eventCam,
                    out Vector2 local))
            {
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }
        }

        float w = Mathf.Max(1f, maxX - minX);
        float h = Mathf.Max(1f, maxY - minY);
        hudRt.sizeDelta = new Vector2(w, h);

        if (hudIgnoreParentLayout)
        {
            LayoutElement layoutElement = snapshotHudImage.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = snapshotHudImage.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.ignoreLayout = true;
        }
    }

    private Camera GetCanvasEventCamera(Canvas root)
    {
        if (root == null)
        {
            return null;
        }

        if (root.renderMode == RenderMode.ScreenSpaceCamera)
        {
            return root.worldCamera != null ? root.worldCamera : WorldCamera;
        }

        return null;
    }

    private void UpdateSnapshotHudFollow()
    {
        if (snapshotHudImage == null || snapshotHudImage.texture == null)
        {
            return;
        }

        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        if (WorldCamera == null)
        {
            return;
        }

        if (!TryGetHudParentCanvasAndEventCamera(out RectTransform parentRt, out _, out Camera eventCam))
        {
            return;
        }

        Transform follow = hudFollowTarget != null ? hudFollowTarget : WorldCamera.transform;
        Vector3 followWorld = follow.position + hudFollowWorldOffset;
        Vector3 scFollow = WorldCamera.WorldToScreenPoint(followWorld);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRt,
                scFollow,
                eventCam,
                out Vector2 localFollow))
        {
            return;
        }

        snapshotHudImage.rectTransform.anchoredPosition = localFollow + hudParentLocalOffsetFromFollow + hudScreenPixelOffset;
    }

    private bool OverlapsRect(SnapshotableObject obj, SnapshotRectangle2D rect)
    {
        Collider2D col = obj.GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            return b.min.x <= rect.Max.x && b.max.x >= rect.Min.x &&
                   b.min.y <= rect.Max.y && b.max.y >= rect.Min.y;
        }

        return rect.Contains(obj.transform.position);
    }

    private Vector2 GetMouseWorldPoint(Vector2 screenPosition)
    {
        return ScreenPositionToWorldPoint(screenPosition);
    }
}
