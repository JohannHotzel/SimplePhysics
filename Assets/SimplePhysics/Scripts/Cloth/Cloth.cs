using System.Collections.Generic;
using UnityEngine;

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

    private LineRenderer[] horizontalLines;
    private LineRenderer[] verticalLines;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;


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
        if(showLineRenderers)
            CreateLineRenderers();

        if (showMesh)
        {
            var mr = GetComponent<MeshRenderer>();
            mr.sharedMaterial = clothMaterial;
            CreateMesh();
            UpdateMeshVertices();
        }
    }
    private void LateUpdate()
    {
        if (showLineRenderers)
            UpdateLineRenderers();

        if (showMesh)
            UpdateMeshVertices();
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

}
