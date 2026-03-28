using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class SnapshotAbility : Ability
{

    public struct SnapshotRectangle2D
    {
        public Vector2 Min;
        public Vector2 Max;

        public SnapshotRectangle2D(Vector2 a, Vector2 b)
        {
            // Calculates the top right and bottom left corners of the rectangle defined by points a and b
            Min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            Max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        public bool Contains(Vector2 point)
        {
            return point.x >= Min.x && point.x <= Max.x && point.y >= Min.y && point.y <= Max.y;
        }
    }
    //private bool isDraggingFold;
    //private bool hasPreviewFold;

    private Vector2 dragStartPoint; // Where you started dragging (world)
    private Vector2 dragEndPoint;
    private Boolean isDraggingSnapshot;


    private bool hasActiveSnapshot;  // Whether a snapshot is currently active in the world (after releasing mouse)
    private SnapshotRectangle2D previewSnapshot;
    private bool hasPreviewSnapshot; // Whether we're currently showing a preview of the snapshot while dragging

    private LineRenderer previewBorderLineRenderer;
    private MeshFilter previewFillMeshFilter;
    private MeshRenderer previewFillMeshRenderer;
    private Mesh previewFillMesh;


    public void Awake()
    {
        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }
        InitializeVisuals();
    }

    public override void onAbilitySwitch()
    {
        throw new System.NotImplementedException();
    }

    public override void onClear()
    {
        throw new System.NotImplementedException();
    }

    public override void onUpdate()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        Vector2 mouseWorldPoint = GetMouseWorldPoint(mouse.position.ReadValue());

        // Check for left click to start snapshot selection
        if (mouse.leftButton.wasPressedThisFrame)
        {
            beginSnapshotSelection(mouseWorldPoint);
            Debug.Log("Started snapshot selection");

        }
        if (!isDraggingSnapshot) return;

        // As long as we're dragging, update current drag end point and update the snapshot preview
        updateSnapshotPreview();


        if (mouse.leftButton.wasReleasedThisFrame)
        {
            cancelSnapshotSelection();
            Debug.Log("Cancelled snapshot selection");
            return;
        }




    }


    private void beginSnapshotSelection(Vector2 worldPoint)
    {
        if (hasActiveSnapshot)
        {
            return;
        }

        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        Debug.Log("Begin Snapshot Selection at: " + worldPoint);
        // Set drag start point to where mouse was pressed, begin the dragging process. 
        dragStartPoint = worldPoint;
        isDraggingSnapshot = true;
    }

    private void updateSnapshotPreview()
    {
        // Create a SnapshotRectangle object
        dragEndPoint = GetMouseWorldPoint(Mouse.current.position.ReadValue());
        SnapshotRectangle2D rect = new SnapshotRectangle2D(dragStartPoint, dragEndPoint);  // Create the rectangle with the two defining points

        hasPreviewSnapshot = true;
        previewSnapshot = rect;
        updatePreviewVisuals(rect);
    }


// Handle visuals. 
    private void updatePreviewVisuals(SnapshotRectangle2D rect) 
    {

        Debug.Log($"Attempting to render snapshot with Drag Begin: {dragStartPoint} and end at {dragEndPoint}");

        // Calculate the four corners of the rectangle for the LineRenderer
        Vector2 bottomLeft = new Vector2(rect.Min.x, rect.Min.y);
        Vector2 bottomRight = new Vector2(rect.Max.x, rect.Min.y);
        Vector2 topRight = new Vector2(rect.Max.x, rect.Max.y);
        Vector2 topLeft = new Vector2(rect.Min.x, rect.Max.y);

        // Updates the border itself
        previewBorderLineRenderer.SetPosition(0, bottomLeft);
        previewBorderLineRenderer.SetPosition(1, bottomRight);
        previewBorderLineRenderer.SetPosition(2, topRight);
        previewBorderLineRenderer.SetPosition(3, topLeft);
        previewBorderLineRenderer.enabled = true;

        // Get the fill mesh
        previewFillMesh.Clear();
        previewFillMesh.vertices = new Vector3[] { bottomLeft, bottomRight, topRight, topLeft };
        previewFillMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };  // Draw the triangles to render
        previewFillMesh.RecalculateBounds();
        previewBorderLineRenderer.enabled = true;
        previewFillMeshRenderer.enabled = true;

    }

    private void InitializeVisuals()
    {
        // Create a child GameObject for the border
        GameObject borderObj = new GameObject("SnapshotPreviewBorder");
        borderObj.transform.parent = this.transform;
        previewBorderLineRenderer = borderObj.AddComponent<LineRenderer>();
        previewBorderLineRenderer.positionCount = 4;  // We need 4 points to draw the rectangle
        previewBorderLineRenderer.loop = true;  // Loop to connect the last point back to the first
        previewBorderLineRenderer.widthMultiplier = 0.05f;  // Set the width of the line
        previewBorderLineRenderer.material = new Material(Shader.Find("Sprites/Default"));  // Use a simple material
        previewBorderLineRenderer.startColor = Color.cyan;
        previewBorderLineRenderer.endColor = Color.cyan;
        previewBorderLineRenderer.enabled = false;  // Start disabled
        // Create a child GameObject for the fill
        GameObject fillObj = new GameObject("SnapshotPreviewFill");
        fillObj.transform.parent = this.transform;
        previewFillMeshFilter = fillObj.AddComponent<MeshFilter>();
        previewFillMeshRenderer = fillObj.AddComponent<MeshRenderer>();
        previewFillMeshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        previewFillMeshRenderer.material.color = new Color(0f, 1f, 1f, 0.5f);  // Semi-transparent cyan
        previewFillMesh = new Mesh();
        previewFillMeshFilter.mesh = previewFillMesh;
        previewFillMeshRenderer.enabled = false;  // Start disabled
    }

    private void cancelSnapshotSelection()
    {
        isDraggingSnapshot = false;
        hasPreviewSnapshot = false;
        previewBorderLineRenderer.enabled = false;
        previewFillMeshRenderer.enabled = false;
    }



    private void takeSnapshot()
    {

    }

    private void releaseSnapshot()
    {

    }


















        private Vector2 GetMouseWorldPoint(Vector2 screenPosition)
    {
        if (WorldCamera == null)
        {
            WorldCamera = Camera.main;
        }

        Vector3 world = WorldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
        return new Vector2(world.x, world.y);
    }
}
