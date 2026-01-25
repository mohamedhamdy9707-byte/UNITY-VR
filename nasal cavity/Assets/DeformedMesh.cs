using UnityEngine;

public class DeformableMesh : MonoBehaviour
{
    [Header("Deformation Settings")]
    [Range(0.1f, 10f)]
    public float stiffness = 3f;
    
    [Range(0.01f, 5f)]
    public float deformationRadius = 1f;
    
    [Range(0f, 1f)]
    public float damping = 0.7f;
    
    [Range(0.1f, 5f)]
    public float deformationStrength = 2f;
    
    [Header("Smoothing Settings")]
    [Range(0f, 1f)]
    public float smoothingStrength = 0.3f;
    
    [Header("Collision Settings")]
    public bool preventPenetration = true;
    
    [Range(0.01f, 1f)]
    public float collisionForce = 0.5f;
    
    [Header("Performance Settings")]
    [Range(1, 5)]
    public int vertexUpdateSkip = 2;
    
    [Header("References")]
    public Transform sphereTransform;
    public Rigidbody sphereRigidbody;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public int affectedVerticesCount = 0;
    public int totalVertices = 0;
    
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private Vector3[] vertexVelocities;
    private Vector3[] previousVertices;
    private bool meshInitialized = false;
    
    private Vector3 accumulatedCollisionForce;
    private int updateCounter = 0;
    
    private float cachedSphereRadius;
    
    void Start()
    {
        InitializeMesh();
        
        if (sphereTransform != null && sphereRigidbody == null)
        {
            sphereRigidbody = sphereTransform.GetComponent<Rigidbody>();
            if (sphereRigidbody == null)
            {
                sphereRigidbody = sphereTransform. gameObject. AddComponent<Rigidbody>();
                sphereRigidbody.useGravity = false;
                sphereRigidbody. collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
        
        if (sphereTransform != null)
        {
            SphereCollider sphereCollider = sphereTransform.GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                sphereCollider = sphereTransform.gameObject.AddComponent<SphereCollider>();
            }
            
            cachedSphereRadius = sphereCollider.radius * sphereTransform.lossyScale.x;
        }
        
        SphereCollider triggerCol = gameObject.GetComponent<SphereCollider>();
        if (triggerCol == null)
        {
            triggerCol = gameObject.AddComponent<SphereCollider>();
        }
        triggerCol.isTrigger = true;
        triggerCol. radius = deformationRadius * 2f;
        triggerCol.center = Vector3.zero;
    }
    
    void InitializeMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        if (meshFilter == null)
        {
            Debug.LogError("No MeshFilter found");
            return;
        }
        
        if (meshFilter.mesh == null)
        {
            Debug.LogError("MeshFilter has no mesh");
            return;
        }
        
        if (!meshFilter.mesh.isReadable)
        {
            Debug.LogError("Mesh is not readable!  Enable Read/Write in import settings");
            return;
        }
        
        mesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = mesh;
        
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        vertexVelocities = new Vector3[originalVertices. Length];
        previousVertices = new Vector3[originalVertices. Length];
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
            previousVertices[i] = originalVertices[i];
        }
        
        totalVertices = originalVertices.Length;
        meshInitialized = true;
        
        Debug.Log($"Mesh initialized: {totalVertices} vertices");
        
        if (totalVertices > 3000)
        {
            Debug.LogWarning($"High vertex count ({totalVertices})! Consider simplifying mesh for better VR performance.");
        }
    }
    
    void Update()
    {
        if (!meshInitialized || sphereTransform == null) return;
        
        updateCounter++;
        accumulatedCollisionForce = Vector3.zero;
        affectedVerticesCount = 0;
        
        Vector3 sphereWorldPos = sphereTransform.position;
        Vector3 sphereLocalPos = transform.InverseTransformPoint(sphereWorldPos);
        float localSphereRadius = cachedSphereRadius / transform.lossyScale.x;
        
        float maxDeformDist = localSphereRadius + deformationRadius;
        float maxDeformDistSq = maxDeformDist * maxDeformDist;
        
        int skipFactor = vertexUpdateSkip;
        
        // Store previous frame for smoothing
        System.Array.Copy(displacedVertices, previousVertices, displacedVertices.Length);
        
        // Deform vertices
        for (int i = 0; i < displacedVertices.Length; i += skipFactor)
        {
            Vector3 currentVertex = displacedVertices[i];
            Vector3 dirToVertex = currentVertex - sphereLocalPos;
            float distSq = dirToVertex.sqrMagnitude;
            
            if (distSq > maxDeformDistSq)
            {
                Vector3 restoreForce = (originalVertices[i] - currentVertex) * stiffness;
                vertexVelocities[i] += restoreForce * Time. deltaTime;
            }
            else
            {
                float dist = Mathf.Sqrt(distSq);
                
                if (preventPenetration && dist < localSphereRadius)
                {
                    affectedVerticesCount++;
                    
                    Vector3 pushDir = (dist > 0.001f) ? dirToVertex / dist : Vector3. up;
                    float penetration = localSphereRadius - dist;
                    
                    Vector3 correction = pushDir * penetration;
                    displacedVertices[i] += correction;
                    vertexVelocities[i] += correction * stiffness * deformationStrength;
                    
                    Vector3 worldPushDir = transform.TransformDirection(pushDir);
                    accumulatedCollisionForce -= worldPushDir * penetration * collisionForce * 10f;
                }
                else if (dist < maxDeformDist)
                {
                    affectedVerticesCount++;
                    
                    float deformAmount = maxDeformDist - dist;
                    float influence = deformAmount / deformationRadius;
                    influence = influence * influence * (3f - 2f * influence);
                    
                    Vector3 pushDir = (dist > 0.001f) ? dirToVertex / dist : Vector3.up;
                    Vector3 pushForce = pushDir * deformAmount * deformationStrength * influence;
                    
                    vertexVelocities[i] += pushForce * Time.deltaTime;
                }
            }
            
            vertexVelocities[i] *= (1f - damping);
            displacedVertices[i] += vertexVelocities[i] * Time.deltaTime;
            
            // Simple temporal smoothing
            displacedVertices[i] = Vector3.Lerp(previousVertices[i], displacedVertices[i], 1f - smoothingStrength);
        }
        
        // Fill in skipped vertices with simple interpolation
        if (skipFactor > 1)
        {
            for (int i = 0; i < displacedVertices.Length; i++)
            {
                if (i % skipFactor != 0)
                {
                    int prev = (i / skipFactor) * skipFactor;
                    int next = Mathf.Min(prev + skipFactor, displacedVertices.Length - 1);
                    
                    if (next > prev)
                    {
                        float t = (float)(i - prev) / (next - prev);
                        displacedVertices[i] = Vector3.Lerp(displacedVertices[prev], displacedVertices[next], t);
                    }
                }
            }
        }
        
        if (preventPenetration && sphereRigidbody != null && accumulatedCollisionForce. sqrMagnitude > 0.01f)
        {
            sphereRigidbody.AddForce(accumulatedCollisionForce, ForceMode.Force);
        }
        
        mesh.vertices = displacedVertices;
        
        if (updateCounter % 3 == 0)
        {
            mesh.RecalculateNormals();
        }
        
        mesh.RecalculateBounds();
    }
    
    void OnDrawGizmos()
    {
        if (! showDebugInfo || sphereTransform == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sphereTransform.position, cachedSphereRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sphereTransform.position, cachedSphereRadius + deformationRadius * transform.lossyScale.x);
        
        if (Application. isPlaying && accumulatedCollisionForce.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(sphereTransform. position, accumulatedCollisionForce * 0.1f);
        }
    }
    
    public void ResetMesh()
    {
        if (! meshInitialized) return;
        
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            displacedVertices[i] = originalVertices[i];
            vertexVelocities[i] = Vector3.zero;
            previousVertices[i] = originalVertices[i];
        }
        
        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}