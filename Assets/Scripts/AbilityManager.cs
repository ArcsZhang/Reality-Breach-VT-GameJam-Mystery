using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class AbilityManager : MonoBehaviour
{
    private sealed class NonFoldableState
    {
        public Transform Target;
        public Vector3 OriginalPosition;
        public Rigidbody2D Body;
        public bool HadBody;
        public bool BodySimulated;
        public Vector2 BodyVelocity;
        public float BodyAngularVelocity;
        public Renderer[] Renderers;
        public bool[] RendererEnabled;
        public Collider2D[] Colliders;
        public bool[] ColliderEnabled;
    }

    public enum AbilityType
    {
        Fold = 0
    }

    [Header("Ability Integration")]
    [SerializeField] private bool abilityInputEnabled = true;
    [SerializeField] private AbilityType activeAbility = AbilityType.Fold;

    [Header("Input")]
    [SerializeField] private int foldMouseButton = 0;

    [Header("References")]
    [SerializeField] private Camera worldCamera;

    [Header("Settings")]
    [SerializeField] private float minimumFoldDistance = 0.05f;
    [SerializeField] private float lineHalfLength = 40f;
    [SerializeField] private float unfoldNodeRadius = 0.25f;

    [Header("Initial Fold")]
    [SerializeField] private bool applyInitialFoldOnStart;
    [SerializeField] private Vector2 initialFoldPointA = new Vector2(-3f, 0f);
    [SerializeField] private Vector2 initialFoldPointB = new Vector2(3f, 0f);

    [Header("Visuals")]
    [SerializeField] private Color previewLineColor = new Color(1f, 0.85f, 0.2f, 0.8f);
    [SerializeField] private Color previewStripColor = new Color(1f, 0.85f, 0.2f, 0.14f);
    [SerializeField] private Color activeSeamColor = new Color(0.49f, 0.83f, 0.99f, 0.95f);
    [SerializeField] private Color unfoldNodeColor = new Color(0.65f, 0.95f, 0.99f, 0.95f);
    [SerializeField] private float lineWidth = 0.05f;

    private FoldableObject2D[] foldables;
    private bool isDraggingFold;
    private bool hasPreviewFold;
    private Vector2 dragStartPoint;
    private FoldData2D previewFold;
    private bool hasActiveFold;
    private FoldData2D activeFold;
    private Vector2 unfoldNodePosition;

    private LineRenderer previewLoLineRenderer;
    private LineRenderer previewHiLineRenderer;
    private LineRenderer activeSeamLineRenderer;
    private LineRenderer unfoldNodeLineRenderer;
    private MeshFilter previewStripMeshFilter;
    private MeshRenderer previewStripMeshRenderer;
    private Mesh previewStripMesh;
    private readonly Dictionary<Transform, NonFoldableState> nonFoldableStates = new Dictionary<Transform, NonFoldableState>();
    private readonly HashSet<Transform> foldableTransformSet = new HashSet<Transform>();

    public System.Action<FoldData2D> FoldApplied;
    public System.Action FoldCleared;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        CacheFoldables();
        InitializeVisuals();
        HidePreviewVisuals();
        SetActiveFoldVisualsVisible(false);
        TryApplyInitialFold();
    }

    private void Update()
    {
        if (!abilityInputEnabled || activeAbility != AbilityType.Fold)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        Vector2 mouseWorldPoint = GetMouseWorldPoint(mouse.position.ReadValue());

        if (hasActiveFold)
        {
            if (WasMouseButtonPressedThisFrame(mouse, foldMouseButton) && IsPointOnUnfoldNode(mouseWorldPoint))
            {
                ClearActiveFold();
            }

            return;
        }

        if (WasMouseButtonPressedThisFrame(mouse, foldMouseButton))
        {
            BeginFoldSelection(mouseWorldPoint);
        }

        if (!isDraggingFold)
        {
            return;
        }

        UpdatePreviewFold(mouseWorldPoint);

        if (!WasMouseButtonReleasedThisFrame(mouse, foldMouseButton))
        {
            return;
        }

        if (hasPreviewFold)
        {
            ApplyFold(previewFold, dragStartPoint);
        }

        CancelSelection();
    }

    [ContextMenu("Refresh Foldable Cache")]
    public void CacheFoldables()
    {
        foldables = FindObjectsByType<FoldableObject2D>();
        foldableTransformSet.Clear();
        for (int i = 0; i < foldables.Length; i++)
        {
            if (foldables[i] != null)
            {
                foldableTransformSet.Add(foldables[i].transform);
            }
        }
    }

    public void BeginFoldSelection(Vector2 worldPoint)
    {
        if (hasActiveFold)
        {
            return;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        dragStartPoint = worldPoint;
        isDraggingFold = true;
        hasPreviewFold = false;
        HidePreviewVisuals();
    }

    public void CancelSelection()
    {
        isDraggingFold = false;
        hasPreviewFold = false;
        HidePreviewVisuals();
    }

    public void ClearActiveFold()
    {
        if (foldables == null || foldables.Length == 0)
        {
            CacheFoldables();
        }

        RestoreShiftedNonFoldableObjects();

        for (int i = 0; i < foldables.Length; i++)
        {
            if (foldables[i] != null)
            {
                foldables[i].ResetFold();
            }
        }

        hasActiveFold = false;
        SetActiveFoldVisualsVisible(false);
        FoldCleared?.Invoke();
    }

    public void ApplyFold(FoldData2D fold)
    {
        ApplyFold(fold, Vector2.zero);
    }

    public void SetAbilityEnabled(bool enabled)
    {
        abilityInputEnabled = enabled;
    }

    public void SetActiveAbility(AbilityType ability)
    {
        activeAbility = ability;
    }

    private void ApplyFold(FoldData2D fold, Vector2 nodePoint)
    {
        if (foldables == null || foldables.Length == 0)
        {
            CacheFoldables();
        }

        RestoreShiftedNonFoldableObjects();

        activeFold = fold;
        hasActiveFold = true;
        unfoldNodePosition = nodePoint;

        for (int i = 0; i < foldables.Length; i++)
        {
            if (foldables[i] != null)
            {
                foldables[i].ApplyFold(activeFold);
            }
        }

        ShiftNonFoldableObjects(activeFold);

        UpdateActiveFoldVisuals();
        FoldApplied?.Invoke(activeFold);
    }

    private void TryApplyInitialFold()
    {
        if (!applyInitialFoldOnStart || hasActiveFold)
        {
            return;
        }

        if (!FoldGeometry2D.TryBuildFold(initialFoldPointA, initialFoldPointB, out FoldData2D initialFold, minimumFoldDistance))
        {
            Debug.LogWarning("Initial fold points are too close. Skipping initial fold.", this);
            return;
        }

        ApplyFold(initialFold, initialFoldPointA);
    }

    private void ShiftNonFoldableObjects(FoldData2D fold)
    {
        nonFoldableStates.Clear();

        Vector3 shiftDelta = new Vector3(-fold.Normal.x * fold.Gap, -fold.Normal.y * fold.Gap, 0f);
        Transform[] allTransforms = FindObjectsByType<Transform>();

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (!IsShiftableNonFoldable(candidate))
            {
                continue;
            }

            if (!TryGetProjectionRange(candidate, fold.Normal, out float minProjection, out float maxProjection))
            {
                continue;
            }

            Vector3 worldPosition = candidate.position;
            bool fullyInsideStrip = minProjection >= fold.Lo && maxProjection <= fold.Hi;
            bool fullyOnShiftedSide = minProjection > fold.Hi;

            if (!fullyInsideStrip && !fullyOnShiftedSide)
            {
                continue;
            }

            NonFoldableState state = CaptureNonFoldableState(candidate, worldPosition);
            nonFoldableStates[candidate] = state;

            if (fullyInsideStrip)
            {
                DisableNonFoldable(state);
                continue;
            }

            Rigidbody2D body = candidate.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position += new Vector2(shiftDelta.x, shiftDelta.y);
            }
            else
            {
                candidate.position = worldPosition + shiftDelta;
            }
        }
    }

    private void RestoreShiftedNonFoldableObjects()
    {
        if (nonFoldableStates.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<Transform, NonFoldableState> entry in nonFoldableStates)
        {
            NonFoldableState state = entry.Value;
            Transform target = state.Target;
            if (state == null || target == null)
            {
                continue;
            }

            if (state.HadBody && state.Body != null)
            {
                state.Body.position = new Vector2(state.OriginalPosition.x, state.OriginalPosition.y);
                state.Body.linearVelocity = state.BodyVelocity;
                state.Body.angularVelocity = state.BodyAngularVelocity;
                state.Body.simulated = state.BodySimulated;
            }
            else
            {
                target.position = state.OriginalPosition;
            }

            RestoreEnabledFlags(state.Renderers, state.RendererEnabled);
            RestoreEnabledFlags(state.Colliders, state.ColliderEnabled);
        }

        nonFoldableStates.Clear();
    }

    private NonFoldableState CaptureNonFoldableState(Transform candidate, Vector3 originalPosition)
    {
        NonFoldableState state = new NonFoldableState
        {
            Target = candidate,
            OriginalPosition = originalPosition,
            Body = candidate.GetComponent<Rigidbody2D>(),
            Renderers = candidate.GetComponentsInChildren<Renderer>(true),
            Colliders = candidate.GetComponentsInChildren<Collider2D>(true)
        };

        state.HadBody = state.Body != null;
        if (state.HadBody)
        {
            state.BodySimulated = state.Body.simulated;
            state.BodyVelocity = state.Body.linearVelocity;
            state.BodyAngularVelocity = state.Body.angularVelocity;
        }

        state.RendererEnabled = new bool[state.Renderers.Length];
        for (int i = 0; i < state.Renderers.Length; i++)
        {
            state.RendererEnabled[i] = state.Renderers[i] != null && state.Renderers[i].enabled;
        }

        state.ColliderEnabled = new bool[state.Colliders.Length];
        for (int i = 0; i < state.Colliders.Length; i++)
        {
            state.ColliderEnabled[i] = state.Colliders[i] != null && state.Colliders[i].enabled;
        }

        return state;
    }

    private static void DisableNonFoldable(NonFoldableState state)
    {
        for (int i = 0; i < state.Renderers.Length; i++)
        {
            if (state.Renderers[i] != null)
            {
                state.Renderers[i].enabled = false;
            }
        }

        for (int i = 0; i < state.Colliders.Length; i++)
        {
            if (state.Colliders[i] != null)
            {
                state.Colliders[i].enabled = false;
            }
        }

        if (state.HadBody && state.Body != null)
        {
            state.Body.linearVelocity = Vector2.zero;
            state.Body.angularVelocity = 0f;
            state.Body.simulated = false;
        }
    }

    private static void RestoreEnabledFlags(Renderer[] renderers, bool[] enabledFlags)
    {
        int count = Mathf.Min(renderers.Length, enabledFlags.Length);
        for (int i = 0; i < count; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = enabledFlags[i];
            }
        }
    }

    private static void RestoreEnabledFlags(Collider2D[] colliders, bool[] enabledFlags)
    {
        int count = Mathf.Min(colliders.Length, enabledFlags.Length);
        for (int i = 0; i < count; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabledFlags[i];
            }
        }
    }

    private static bool TryGetProjectionRange(Transform candidate, Vector2 normal, out float minProjection, out float maxProjection)
    {
        minProjection = float.PositiveInfinity;
        maxProjection = float.NegativeInfinity;

        Collider2D[] colliders = candidate.GetComponentsInChildren<Collider2D>(true);
        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                ExpandProjectionByBounds(collider.bounds, normal, ref minProjection, ref maxProjection);
            }

            return minProjection <= maxProjection;
        }

        Renderer[] renderers = candidate.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                ExpandProjectionByBounds(renderer.bounds, normal, ref minProjection, ref maxProjection);
            }

            return minProjection <= maxProjection;
        }

        float pointProjection = Vector2.Dot(new Vector2(candidate.position.x, candidate.position.y), normal);
        minProjection = pointProjection;
        maxProjection = pointProjection;
        return true;
    }

    private static void ExpandProjectionByBounds(Bounds bounds, Vector2 normal, ref float minProjection, ref float maxProjection)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector2[] corners =
        {
            new Vector2(min.x, min.y),
            new Vector2(min.x, max.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            float projection = Vector2.Dot(corners[i], normal);
            if (projection < minProjection)
            {
                minProjection = projection;
            }

            if (projection > maxProjection)
            {
                maxProjection = projection;
            }
        }
    }

    private bool IsShiftableNonFoldable(Transform candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (candidate == transform || candidate.IsChildOf(transform))
        {
            return false;
        }

        if (foldableTransformSet.Contains(candidate))
        {
            return false;
        }

        if (candidate.GetComponent<FoldableObject2D>() != null)
        {
            return false;
        }

        if (candidate.GetComponent<Camera>() != null)
        {
            return false;
        }

        bool has2DPhysics = candidate.GetComponent<Collider2D>() != null || candidate.GetComponent<Rigidbody2D>() != null;
        return has2DPhysics;
    }

    private void UpdatePreviewFold(Vector2 worldPoint)
    {
        if (!FoldGeometry2D.TryBuildFold(dragStartPoint, worldPoint, out FoldData2D fold, minimumFoldDistance))
        {
            hasPreviewFold = false;
            HidePreviewVisuals();
            return;
        }

        hasPreviewFold = true;
        previewFold = fold;
        UpdatePreviewVisuals(previewFold);
    }

    private Vector2 GetMouseWorldPoint(Vector2 screenPosition)
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
        return new Vector2(world.x, world.y);
    }

    private static bool WasKeyPressedThisFrame(Keyboard keyboard, Key key)
    {
        KeyControl control = keyboard[key];
        return control != null && control.wasPressedThisFrame;
    }

    private static bool WasMouseButtonPressedThisFrame(Mouse mouse, int button)
    {
        switch (button)
        {
            case 1:
                return mouse.rightButton.wasPressedThisFrame;
            case 2:
                return mouse.middleButton.wasPressedThisFrame;
            default:
                return mouse.leftButton.wasPressedThisFrame;
        }
    }

    private static bool WasMouseButtonReleasedThisFrame(Mouse mouse, int button)
    {
        switch (button)
        {
            case 1:
                return mouse.rightButton.wasReleasedThisFrame;
            case 2:
                return mouse.middleButton.wasReleasedThisFrame;
            default:
                return mouse.leftButton.wasReleasedThisFrame;
        }
    }

    private bool IsPointOnUnfoldNode(Vector2 point)
    {
        if (!hasActiveFold)
        {
            return false;
        }

        float sqrDistance = (point - unfoldNodePosition).sqrMagnitude;
        float clickRadius = unfoldNodeRadius * 1.5f;
        return sqrDistance <= clickRadius * clickRadius;
    }

    private void InitializeVisuals()
    {
        Material lineMaterial = CreateDefaultMaterial();

        previewLoLineRenderer = CreateLineRenderer("FoldPreviewLo", previewLineColor, lineWidth, lineMaterial, false);
        previewHiLineRenderer = CreateLineRenderer("FoldPreviewHi", previewLineColor, lineWidth, lineMaterial, false);
        activeSeamLineRenderer = CreateLineRenderer("FoldActiveSeam", activeSeamColor, lineWidth * 1.2f, lineMaterial, false);
        unfoldNodeLineRenderer = CreateLineRenderer("FoldUnfoldNode", unfoldNodeColor, lineWidth * 0.85f, lineMaterial, true);

        GameObject stripObject = new GameObject("FoldPreviewStrip");
        stripObject.transform.SetParent(transform, false);
        previewStripMeshFilter = stripObject.AddComponent<MeshFilter>();
        previewStripMeshRenderer = stripObject.AddComponent<MeshRenderer>();

        previewStripMesh = new Mesh
        {
            name = "FoldPreviewStripMesh"
        };

        previewStripMeshFilter.sharedMesh = previewStripMesh;
        previewStripMeshRenderer.sharedMaterial = CreateDefaultMaterial();
        previewStripMeshRenderer.sharedMaterial.color = previewStripColor;
        previewStripMeshRenderer.sortingOrder = 100;
    }

    private LineRenderer CreateLineRenderer(string childName, Color color, float width, Material material, bool loop)
    {
        GameObject lineObject = new GameObject(childName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = loop ? 24 : 2;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.material = material;
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = 110;
        line.enabled = false;
        return line;
    }

    private static Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        return new Material(shader);
    }

    private void UpdatePreviewVisuals(FoldData2D fold)
    {
        Vector2 axis = new Vector2(-fold.Normal.y, fold.Normal.x);
        Vector2 loCenter = fold.Normal * fold.Lo;
        Vector2 hiCenter = fold.Normal * fold.Hi;

        SetLine(previewLoLineRenderer, loCenter - axis * lineHalfLength, loCenter + axis * lineHalfLength);
        SetLine(previewHiLineRenderer, hiCenter - axis * lineHalfLength, hiCenter + axis * lineHalfLength);

        UpdatePreviewStripMesh(loCenter, hiCenter, axis);

        previewLoLineRenderer.enabled = true;
        previewHiLineRenderer.enabled = true;
        previewStripMeshRenderer.enabled = true;
    }

    private void UpdateActiveFoldVisuals()
    {
        Vector2 axis = new Vector2(-activeFold.Normal.y, activeFold.Normal.x);
        Vector2 seamCenter = activeFold.Normal * activeFold.Lo;
        SetLine(activeSeamLineRenderer, seamCenter - axis * lineHalfLength, seamCenter + axis * lineHalfLength);
        activeSeamLineRenderer.enabled = true;

        UpdateNodeVisual();
    }

    private void UpdateNodeVisual()
    {
        const int segmentCount = 24;
        unfoldNodeLineRenderer.positionCount = segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float angle = t * Mathf.PI * 2f;
            Vector2 p = unfoldNodePosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * unfoldNodeRadius;
            unfoldNodeLineRenderer.SetPosition(i, new Vector3(p.x, p.y, 0f));
        }

        unfoldNodeLineRenderer.enabled = true;
    }

    private void SetLine(LineRenderer line, Vector2 a, Vector2 b)
    {
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(a.x, a.y, 0f));
        line.SetPosition(1, new Vector3(b.x, b.y, 0f));
    }

    private void UpdatePreviewStripMesh(Vector2 loCenter, Vector2 hiCenter, Vector2 axis)
    {
        Vector2 p0 = loCenter - axis * lineHalfLength;
        Vector2 p1 = loCenter + axis * lineHalfLength;
        Vector2 p2 = hiCenter + axis * lineHalfLength;
        Vector2 p3 = hiCenter - axis * lineHalfLength;

        previewStripMesh.Clear();
        previewStripMesh.vertices = new[]
        {
            new Vector3(p0.x, p0.y, 0f),
            new Vector3(p1.x, p1.y, 0f),
            new Vector3(p2.x, p2.y, 0f),
            new Vector3(p3.x, p3.y, 0f)
        };
        previewStripMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        previewStripMesh.RecalculateBounds();
        previewStripMesh.RecalculateNormals();
    }

    private void HidePreviewVisuals()
    {
        if (previewLoLineRenderer != null)
        {
            previewLoLineRenderer.enabled = false;
        }

        if (previewHiLineRenderer != null)
        {
            previewHiLineRenderer.enabled = false;
        }

        if (previewStripMeshRenderer != null)
        {
            previewStripMeshRenderer.enabled = false;
        }
    }

    private void SetActiveFoldVisualsVisible(bool visible)
    {
        if (activeSeamLineRenderer != null)
        {
            activeSeamLineRenderer.enabled = visible;
        }

        if (unfoldNodeLineRenderer != null)
        {
            unfoldNodeLineRenderer.enabled = visible;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!hasActiveFold)
        {
            return;
        }

        Vector2 axis = new Vector2(-activeFold.Normal.y, activeFold.Normal.x);
        Vector2 center = activeFold.Normal * activeFold.Lo;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center - axis * 20f, center + axis * 20f);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(unfoldNodePosition, unfoldNodeRadius);
    }
}
