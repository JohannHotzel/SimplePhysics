using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Cloth : MonoBehaviour
{
    [Header("RigidBodies")]
    public List<Transform> points = new List<Transform>();
    public int rows;
    public int cols;

    [Header("Appearance")]
    public Material lineMaterial;
    public Material clothMaterial;
    public bool showLineRenderers = true;
    public bool showMesh = true;

    [Header("MeshCollider & Collision Response")]
    public bool addMeshCollider = false;
    public bool meshColliderConvex = false;
    public bool relayCollisionImpulse = false;
    public int impulseNearestN = 3;
    public float impulseScale = 0.2f;

    private LineRenderer[] horizontalLines;
    private LineRenderer[] verticalLines;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    Rigidbody[] bodies;
    MeshCollider meshCol;


    public void Init(List<Transform> pts, int r, int c, Material lineMaterial, Material clothMaterial)
    {
        points = pts;
        rows = r;
        cols = c;
        this.lineMaterial = lineMaterial;
        this.clothMaterial = clothMaterial;
    }
    public void Awake()
    {
        bodies = new Rigidbody[points.Count];
        for (int i = 0; i < points.Count; i++)
            bodies[i] = points[i] ? points[i].GetComponent<Rigidbody>() : null;

        if (showLineRenderers)
            CreateLineRenderers();

        if (showMesh)
        {
            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = clothMaterial;
            CreateMesh();
            UpdateMeshVertices();

            if(addMeshCollider)
                CreateMeshCollider();
        }
    }
    private void LateUpdate()
    {
        if (showLineRenderers)
            UpdateLineRenderers();

        if (showMesh)
        {
            UpdateMeshVertices();

            if (addMeshCollider)
                UpdateMeshCollider();
        }
            
    }

    // Line Renderers
    private void CreateLineRenderers()
    {
        horizontalLines = new LineRenderer[rows];
        for (int r = 0; r < rows; r++)
        {
            horizontalLines[r] = CreateLineRenderer($"Row_{r}", cols);
        }

        verticalLines = new LineRenderer[cols];
        for (int c = 0; c < cols; c++)
        {
            verticalLines[c] = CreateLineRenderer($"Col_{c}", rows);
        }
    }
    private LineRenderer CreateLineRenderer(string name, int count)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = count;
        lr.material = lineMaterial;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.generateLightingData = true;

        return lr;
    }
    private void UpdateLineRenderers()
    {
        if (points == null || points.Count == 0) return;

        for (int r = 0; r < rows; r++)
        {
            var lr = horizontalLines[r];
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                lr.SetPosition(c, points[idx].position);
            }
        }

        for (int c = 0; c < cols; c++)
        {
            var lr = verticalLines[c];
            for (int r = 0; r < rows; r++)
            {
                int idx = r * cols + c;
                lr.SetPosition(r, points[idx].position);
            }
        }
    }

    // Mesh
    private void CreateMesh()
    {
        if (rows < 2 || cols < 2)
        {
            Debug.LogWarning("Cloth mesh needs at least 2 rows and 2 cols.");
            return;
        }

        mesh = new Mesh();
        mesh.name = "ClothMesh";

        mesh.MarkDynamic();

        int vCount = rows * cols;
        vertices = new Vector3[vCount];
        uvs = new Vector2[vCount];

        //UVs
        for (int r = 0; r < rows; r++)
        {
            float v = (rows > 1) ? (float)r / (rows - 1) : 0f;

            for (int c = 0; c < cols; c++)
            {
                float u = (cols > 1) ? (float)c / (cols - 1) : 0f;

                int i = r * cols + c;
                uvs[i] = new Vector2(u, v);
            }
        }

        //Triangles
        int quadCount = (rows - 1) * (cols - 1);
        triangles = new int[quadCount * 6];

        int t = 0;
        for (int r = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < cols - 1; c++)
            {
                int i0 = r * cols + c;
                int i1 = r * cols + (c + 1);
                int i2 = (r + 1) * cols + c;
                int i3 = (r + 1) * cols + (c + 1);

                triangles[t++] = i0;
                triangles[t++] = i1;
                triangles[t++] = i2;

                triangles[t++] = i1;
                triangles[t++] = i3;
                triangles[t++] = i2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var mf = GetComponent<MeshFilter>();
        mf.sharedMesh = mesh;
    }
    private void UpdateMeshVertices()
    {
        if (mesh == null || vertices == null) return;
        if (points == null || points.Count != rows * cols) return;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = transform.InverseTransformPoint(points[i].position);
        }

        mesh.vertices = vertices;
        if ((Time.frameCount & 3) == 0) mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    //MeshCollider and Collision Response
    void CreateMeshCollider()
    {
        meshCol = GetComponent<MeshCollider>();
        if (!meshCol) meshCol = gameObject.AddComponent<MeshCollider>();

        meshCol.sharedMesh = mesh;
        meshCol.convex = meshColliderConvex;

        var parentCols = GetComponents<Collider>();

        for (int i = 0; i < points.Count; i++)
        {
            var t = points[i];
            if (!t) continue;

            var pointCols = t.GetComponentsInChildren<Collider>();

            for (int p = 0; p < parentCols.Length; p++)
            {
                var pc = parentCols[p];
                if (!pc) continue;

                for (int s = 0; s < pointCols.Length; s++)
                {
                    var sc = pointCols[s];
                    if (!sc) continue;

                    Physics.IgnoreCollision(sc, pc, true);
                }
            }
        }
    }
    void UpdateMeshCollider()
    {
        if (meshCol == null || mesh == null) return;

        meshCol.sharedMesh = null;
        meshCol.sharedMesh = mesh;
    }
    void OnCollisionEnter(Collision c)
    {
        if(relayCollisionImpulse) RelayImpulse(c);
    }
    void RelayImpulse(Collision c)
    {
        if (bodies == null || bodies.Length == 0) return;
        if (c.contactCount == 0) return;

        Vector3 impulse = c.impulse * impulseScale;
        if (impulse.sqrMagnitude < 1e-8f) return;

        Vector3 p = c.contacts[0].point;

        int n = Mathf.Min(impulseNearestN, bodies.Length);

        var nearest = bodies
            .Where(rb => rb != null && !rb.isKinematic)
            .OrderBy(rb => (rb.worldCenterOfMass - p).sqrMagnitude)
            .Take(n);

        Vector3 per = impulse / n;

        foreach (var rb in nearest)
        {
            rb.AddForceAtPosition(-per, p, ForceMode.Impulse);
        }
    }
}
