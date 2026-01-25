 using UnityEngine;
using System.Collections.Generic;

public class CuttableMesh : MonoBehaviour
{
    [Header("Cutting Settings")]
    [SerializeField] private float eraseRadius = 0.3f;
    [SerializeField] private KeyCode cutKey = KeyCode.Mouse0;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private List<int> hiddenTriangles;
    private Camera mainCamera;

    private void Start()
    {
        InitializeMesh();
        mainCamera = Camera.main;
    }

    private void InitializeMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("CuttableMesh requires a MeshFilter component!");
            return;
        }

        if (GetComponent<MeshCollider>() == null)
        {
            gameObject.AddComponent<MeshCollider>();
        }

        mesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = mesh;

        vertices = mesh.vertices;
        triangles = mesh.triangles;
        hiddenTriangles = new List<int>();
    }

    private void Update()
    {
        if (Input.GetKey(cutKey))
        {
            EraseWithMouse();
        }
    }

    private void EraseWithMouse()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                HideAtPoint(hit.point);
            }
        }
    }

    private void HideAtPoint(Vector3 worldPoint)
    {
        if (mesh == null) return;

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        bool meshModified = false;

        // Go through all triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Skip if already hidden
            if (hiddenTriangles.Contains(i)) continue;

            // Get the three vertices of this triangle
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            // Calculate triangle center
            Vector3 triangleCenter = (v1 + v2 + v3) / 3f;

            // Check if triangle center is within erase radius
            float distance = Vector3.Distance(triangleCenter, localPoint);

            if (distance < eraseRadius)
            {
                // Mark triangle as hidden
                hiddenTriangles.Add(i);
                meshModified = true;
            }
        }

        if (meshModified)
        {
            UpdateMesh();
        }
    }

    private void UpdateMesh()
    {
        // Create new triangle array without hidden triangles
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (!hiddenTriangles.Contains(i))
            {
                newTriangles.Add(triangles[i]);
                newTriangles.Add(triangles[i + 1]);
                newTriangles.Add(triangles[i + 2]);
            }
        }

        mesh.triangles = newTriangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Update collider
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    public void ResetMesh()
    {
        if (mesh == null) return;

        hiddenTriangles.Clear();
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Destroy(mesh);
        }
    }
}
