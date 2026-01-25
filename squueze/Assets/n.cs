 using UnityEngine;

public class LiverSqueezeController : MonoBehaviour
{
    [Header("Squeeze Settings")]
    [SerializeField] private float squeezeStrength = 0.3f;
    [SerializeField] private float squeezeRadius = 2f;
    [SerializeField] private float squeezeSpeed = 5f;
    [SerializeField] private float recoverySpeed = 3f;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = new Color(0.6f, 0.2f, 0.15f);
    [SerializeField] private Color squeezedColor = new Color(0.8f, 0.3f, 0.25f);

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private Color[] vertexColors;
    private MeshFilter meshFilter;
    private Renderer meshRenderer;
    private Camera mainCamera;

    private Vector3 squeezePoint;
    private bool isSqueezingActive = false;
    private bool isInitialized = false;

    void Start()
    {
        // Find MeshFilter - check this object first, then children
        meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogWarning("MeshFilter not found on this object. Checking children...");
            meshFilter = GetComponentInChildren<MeshFilter>();
        }

        if (meshFilter == null)
        {
            Debug.LogError("ERROR: No MeshFilter found! Please attach this script to an object with a MeshFilter, or to a parent of such an object.");
            Debug.LogError("Current object: " + gameObject.name);
            return;
        }

        // Check if mesh exists
        if (meshFilter.sharedMesh == null)
        {
            Debug.LogError("ERROR: MeshFilter has no mesh assigned!");
            return;
        }

        mainCamera = Camera.main;
        meshRenderer = meshFilter.GetComponent<Renderer>();

        if (meshRenderer == null)
        {
            Debug.LogError("ERROR: No Renderer found on the object with MeshFilter!");
            return;
        }

        // Clone the mesh
        mesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = mesh;

        // Store original vertices
        originalVertices = mesh.vertices;

        if (originalVertices.Length == 0)
        {
            Debug.LogError("ERROR: Mesh has no vertices!");
            return;
        }

        displacedVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);

        // Setup vertex colors
        vertexColors = new Color[originalVertices.Length];
        for (int i = 0; i < vertexColors.Length; i++)
        {
            vertexColors[i] = normalColor;
        }
        mesh.colors = vertexColors;

        isInitialized = true;
        Debug.Log("SUCCESS! Liver Squeeze Controller initialized with " + originalVertices.Length + " vertices.");
    }

    void Update()
    {
        if (!isInitialized) return;

        HandleInput();
        UpdateMeshDeformation();
    }

    void HandleInput()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Check if we hit this object or any child
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                {
                    squeezePoint = meshFilter.transform.InverseTransformPoint(hit.point);
                    isSqueezingActive = true;
                }
                else
                {
                    isSqueezingActive = false;
                }
            }
            else
            {
                isSqueezingActive = false;
            }
        }
        else
        {
            isSqueezingActive = false;
        }
    }

    void UpdateMeshDeformation()
    {
        bool meshModified = false;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 targetPosition = originalVertices[i];
            Color targetColor = normalColor;

            if (isSqueezingActive)
            {
                float distance = Vector3.Distance(originalVertices[i], squeezePoint);

                if (distance < squeezeRadius)
                {
                    float squeezeFactor = 1f - (distance / squeezeRadius);
                    squeezeFactor = Mathf.Pow(squeezeFactor, 2);

                    Vector3 directionToSqueeze = squeezePoint - originalVertices[i];
                    Vector3 inwardDirection = -originalVertices[i].normalized;
                    Vector3 squeezeDirection = (directionToSqueeze + inwardDirection * 0.5f).normalized;

                    targetPosition = originalVertices[i] + squeezeDirection * squeezeStrength * squeezeFactor;
                    targetColor = Color.Lerp(normalColor, squeezedColor, squeezeFactor);

                    meshModified = true;
                }
            }

            float speed = isSqueezingActive ? squeezeSpeed : recoverySpeed;
            displacedVertices[i] = Vector3.Lerp(displacedVertices[i], targetPosition, Time.deltaTime * speed);
            vertexColors[i] = Color.Lerp(vertexColors[i], targetColor, Time.deltaTime * speed * 2f);

            if (Vector3.Distance(displacedVertices[i], targetPosition) > 0.001f)
            {
                meshModified = true;
            }
        }

        if (meshModified)
        {
            mesh.vertices = displacedVertices;
            mesh.colors = vertexColors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
