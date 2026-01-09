using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class SoftMesh : MonoBehaviour
{
    [Header("Compute")]
    public ComputeShader compute;

    [Header("Impact")]
    public float minImpactImpulse = 0.5f;
    public float dent = 0.05f;
    public float kick = 1.5f;
    public float maxDistance = 1.0f;
    public LayerMask deformLayers;

    [Header("Spring")]
    public float springStiffness = 50f;
    public float damping = 8f;

    [Header("Collider")]
    public bool updateCollider = true;
    public int updateColliderEveryNFrames = 5;

    [Header("Debug")]
    public bool reset = false;

    // Sleep / activity flag
    bool inactive = false;

    MeshFilter mf;
    MeshCollider mc;
    Mesh mesh;

    Vector3[] cpuVerts;
    int frameCounter;

    //GPU Resources
    ComputeBuffer restBuf, targetBuf, posBuf, velBuf, activeFlagBuf;  // GPU Buffers
    int kSimulate, kImpact, kCheckActivity; // Kernels
    int vertexCount;
    uint[] activeFlagCPU = new uint[1];


    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mc = GetComponent<MeshCollider>();

        //One instance per SoftMesh otherwise they share the same buffer bindings!
        compute = Instantiate(compute);

        mesh = Instantiate(mf.sharedMesh);
        mesh.MarkDynamic();
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;

        var rest = mesh.vertices;
        vertexCount = rest.Length;

        cpuVerts = new Vector3[vertexCount];

        // Create Buffers
        restBuf = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        posBuf = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        velBuf = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        targetBuf = new ComputeBuffer(vertexCount, sizeof(float) * 3);
        restBuf.SetData(rest);
        targetBuf.SetData(rest);
        posBuf.SetData(rest);
        velBuf.SetData(new Vector3[vertexCount]);

        // Find Kernels
        kSimulate = compute.FindKernel("Simulate");
        kImpact = compute.FindKernel("ApplyImpact");
        kCheckActivity = compute.FindKernel("CheckActivity");

        // Bind Buffers to Kernels
        BindCommon(kSimulate);
        BindCommon(kImpact);


        // 1-uint activity flag buffer
        activeFlagBuf = new ComputeBuffer(1, sizeof(uint));
        activeFlagBuf.SetData(new uint[] { 0 });

        // Bind to kernel
        compute.SetBuffer(kCheckActivity, "_Vel", velBuf);
        compute.SetBuffer(kCheckActivity, "_ActiveFlag", activeFlagBuf);
        compute.SetInt("_VertexCount", vertexCount);


    }
    void OnDestroy()
    {
        restBuf?.Release();
        targetBuf?.Release();
        posBuf?.Release();
        velBuf?.Release();
        activeFlagBuf?.Release();

    }


    void FixedUpdate()
    {
        frameCounter++;

        if (reset)
        {
            ResetState();
            reset = false;
            inactive = false;
        }

        if (inactive)
            return;

        compute.SetFloat("_DT", Time.fixedDeltaTime);
        compute.SetFloat("_SpringK", springStiffness);
        compute.SetFloat("_Damping", damping);
        Dispatch(kSimulate);

        activeFlagCPU[0] = 0;
        activeFlagBuf.SetData(activeFlagCPU);
        Dispatch(kCheckActivity);
        activeFlagBuf.GetData(activeFlagCPU);
        inactive = (activeFlagCPU[0] == 0);

        if (inactive)
            return;

        posBuf.GetData(cpuVerts);

        mesh.vertices = cpuVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (updateCollider && mc != null)
        {
            int n = Mathf.Max(1, updateColliderEveryNFrames);

            if (frameCounter % n == 0)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = mesh;
            }
        }

    }


    // OnCollisionEnter applies an impact to the mesh on collision. Very slow on large meshes!
    void OnCollisionEnter(Collision c)
    {
        if (c.contactCount == 0) return;
        if (c.impulse.magnitude < minImpactImpulse) return;
        if ((deformLayers.value & (1 << c.gameObject.layer)) == 0) return;

        var cp = c.GetContact(0);

        Vector3 impactPointWS = cp.point;
        Vector3 pushDirWS = -cp.normal.normalized;

        //float kick = kick * c.impulse.magnitude;
        ApplyImpactGPU(impactPointWS, pushDirWS, kick, dent);
    }


    void ApplyImpactGPU(Vector3 impactPointWS, Vector3 pushDirWS, float kick, float dent)
    {
        inactive = false;

        // Convert impact point from world space to local space
        Vector3 impactPointLS = transform.InverseTransformPoint(impactPointWS);

        // Convert direction from world space to local space
        Vector3 pushDirLS = transform.InverseTransformDirection(pushDirWS).normalized;

        // Absolute lossy scale used to approximate world-space distance
        // from local-space delta (handles non-uniform scale)
        Vector3 s = transform.lossyScale;
        Vector3 absScale = new Vector3(
            Mathf.Abs(s.x),
            Mathf.Abs(s.y),
            Mathf.Abs(s.z)
        );

        compute.SetVector("_ImpactPointLS", impactPointLS);
        compute.SetVector("_PushDirLS", pushDirLS);

        // Scale correction so MaxDistance remains in world units
        compute.SetVector("_AbsScale", absScale);
        compute.SetFloat("_MaxDistanceWS", maxDistance);

        compute.SetFloat("_Kick", kick);
        compute.SetFloat("_Dent", dent);

        Dispatch(kImpact);
    }
    void ResetState()
    {
        restBuf.GetData(cpuVerts);

        targetBuf.SetData(cpuVerts);             // Target = Rest
        posBuf.SetData(cpuVerts);                // Pos = Rest
        velBuf.SetData(new Vector3[vertexCount]); // Vel = 0

        mesh.vertices = cpuVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (mc != null)
        {
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }
    }

    void Dispatch(int kernel)
    {
        int groups = Mathf.CeilToInt(vertexCount / 256f);
        compute.Dispatch(kernel, groups, 1, 1);
    }
    void BindCommon(int kernel)
    {
        compute.SetBuffer(kernel, "_Rest", restBuf);
        compute.SetBuffer(kernel, "_Target", targetBuf);
        compute.SetBuffer(kernel, "_Pos", posBuf);
        compute.SetBuffer(kernel, "_Vel", velBuf);
        compute.SetInt("_VertexCount", vertexCount);

    }
    



}
