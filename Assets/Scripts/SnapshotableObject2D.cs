using UnityEngine;

public class SnapshotableObject : MonoBehaviour
{
    private GameObject outlineObject;
    private Rigidbody2D body;
    private MeshRenderer meshRenderer;


    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        body = GetComponent<Rigidbody2D>();
        CreateOutline();
    }

    private void CreateOutline()
    {
        MeshFilter originalFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null || originalFilter == null) return;

        outlineObject = new GameObject("SelectionOutline");
        outlineObject.transform.SetParent(transform, false);
        outlineObject.transform.localScale = Vector3.one * 1.08f;

        MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
        outlineFilter.sharedMesh = originalFilter.sharedMesh;

        MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();  // Use mesh renderer
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        outlineRenderer.material = mat;
        outlineRenderer.sortingOrder = meshRenderer.sortingOrder - 1;

        outlineObject.SetActive(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outlineObject != null)
            outlineObject.SetActive(highlighted);
    }

    public void Freeze()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
    }

    public void Unfreeze()
    {
        if (body != null)
            body.simulated = true;
    }

    public void MoveTo(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        if (body != null)
            body.position = position;
    }
}