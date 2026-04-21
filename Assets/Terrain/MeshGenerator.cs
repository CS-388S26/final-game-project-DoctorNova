using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh = null;
    [NonSerialized]
    public Vector3[] vertices;
    Color[] colors;
    int[] triangles;
    [NonSerialized]
    public int[,] neighbors;
    [NonSerialized]
    public int numberOfNeighbors;
    public int neighborsDepth = 4;

    // Source: https://www.youtube.com/watch?v=wbpMiKiSKm8
    [Tooltip("Controls increase in frequency of octaves")]
    public float lacunarity = 2;

    [Tooltip("Controls decrease in amplitude of octaves")]
    public float persistance = 0.5f;

    public int octaves = 3;

    public Gradient gradient;

    GameObject player;
    [NonSerialized]
    public Vector2Int lastGridPosition;
    [NonSerialized]
    public bool IsGeneratingTerrain = false;

    public int xSize = 250;
    public int zSize = 250;
    [NonSerialized]
    public int xHalfSize;
    [NonSerialized]
    public int zHalfSize;

    float maxHeight;
    float inversMaxHeight;
    MeshCollider meshCollider = null;

    bool updatingTerrain = false;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();

        GetComponent<MeshFilter>().mesh = mesh;
        meshCollider = GetComponent<MeshCollider>();

        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void InitializeTerrainGeneration()
    {
        // Preallocate memory and pre calculate values to improve performance
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        colors = new Color[vertices.Length];
        triangles = new int[xSize * zSize * 6];
        maxHeight = Mathf.Pow(persistance, octaves - 1);
        inversMaxHeight = 1.0f / maxHeight;
        xHalfSize = xSize / 2;
        zHalfSize = zSize / 2;

        numberOfNeighbors = (2 * neighborsDepth + 1) * (2 * neighborsDepth + 1);
        neighbors = new int[vertices.Length, numberOfNeighbors];

        Vector3 playerPos = player ? player.transform.position : Vector3.zero;

        lastGridPosition = new Vector2Int(
            Mathf.FloorToInt(playerPos.x),
            Mathf.FloorToInt(playerPos.z)
        );

        int trianglesCounter = 0;
        for (int vertex = 0, z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[trianglesCounter + 0] = vertex + 0;
                triangles[trianglesCounter + 1] = vertex + xSize + 1;
                triangles[trianglesCounter + 2] = vertex + 1;
                triangles[trianglesCounter + 3] = vertex + 1;
                triangles[trianglesCounter + 4] = vertex + xSize + 1;
                triangles[trianglesCounter + 5] = vertex + xSize + 2;

                vertex++;
                trianglesCounter += 6;
            }

            vertex++;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            int vertexX = i % (xSize + 1);
            int vertexZ = i / (xSize + 1);

            int nI = 0;

            for (int offsetX = -neighborsDepth; offsetX <= neighborsDepth; offsetX++)
            {
                for (int offsetZ = -neighborsDepth; offsetZ <= neighborsDepth; offsetZ++)
                {
                    int nX = vertexX + offsetX;
                    int nZ = vertexZ + offsetZ;

                    // Ignore invalid indices
                    if (nX < 0 || nX >= (xSize + 1) || nZ < 0 || nZ >= (zSize + 1))
                    {
                        continue;
                    }

                    // Save index of neighbouring vertex in neighbors array
                    neighbors[i, nI] = nZ * (xSize + 1) + nX;
                    nI++;
                }
            }

            // Mark remaining neighbors as not existing
            for (; nI < numberOfNeighbors; nI++)
            {
                neighbors[i, nI] = -1;
            }
        }
    }

    private void UpdateMesh()
    {
        mesh.Clear();

        // increase the max amount of indices this mesh can have
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        if (meshCollider)
        {
            meshCollider.sharedMesh = mesh;
        }
    }

    void OnValidate()
    {
        if (mesh)
        {
            if (Application.isPlaying)
            {
                Vector3 playerPos = player ? player.transform.position : Vector3.zero;

                lastGridPosition = new Vector2Int(
                    Mathf.FloorToInt(playerPos.x),
                    Mathf.FloorToInt(playerPos.z)
                );
            }
            else
            {
                lastGridPosition = Vector2Int.zero;
            }

            GenerateTerrain();
        }
    }

    float GetHeight(float x, float z)
    {
        float y = 0;

        for (int o = 0; o < octaves; o++)
        {
            float frequency = Mathf.Pow(lacunarity, o);
            float amplitude = Mathf.Pow(persistance, o);

            y += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
        }

        return Mathf.Clamp(y, 0, maxHeight);
    }

    void CreateVertices()
    {
        for (int i = 0, z = -zHalfSize; z <= zHalfSize; z++)
        {
            for (int x = -xHalfSize; x <= xHalfSize; x++)
            {
                // Keep mesh centered around player
                float worldX = lastGridPosition.x + x;
                float worldZ = lastGridPosition.y + z;
                float y = GetHeight(worldX, worldZ);
                float worldY = transform.position.y + y;

                vertices[i] = new Vector3(worldX, worldY, worldZ);

                float gradientValue = y * inversMaxHeight;
                colors[i] = gradient.Evaluate(gradientValue);

                i++;
            }
        }
    }

    IEnumerator UpdateTerrain(Vector2Int newPlayerPos)
    {
        Vector2Int diff = newPlayerPos - lastGridPosition;

        // Handle X-axis movement (columns)
        if (diff.x != 0)
        {
            int step = diff.x > 0 ? 1 : -1;

            int tilesMoved = Mathf.Abs(diff.x);
            for (int move = 0; move < tilesMoved; move++)
            {
                Vector2Int currentPos = lastGridPosition + new Vector2Int(step * (move + 1), 0);

                if (step > 0)
                {
                    // Shift all columns left by one (overwrite column 0, keep col xSize)
                    for (int z = 0; z <= zSize; z++)
                    {
                        for (int x = 0; x < xSize; x++)
                        {
                            vertices[z * (xSize + 1) + x] = vertices[z * (xSize + 1) + x + 1];
                            colors[z * (xSize + 1) + x] = colors[z * (xSize + 1) + x + 1];
                        }
                    }

                    yield return new WaitForEndOfFrame();

                    // Fill the new rightmost column with fresh vertices
                    float worldX = currentPos.x + xHalfSize;
                    for (int z = 0; z <= zSize; z++)
                    {
                        float worldZ = currentPos.y - zHalfSize + z;
                        float y = GetHeight(worldX, worldZ);
                        int idx = z * (xSize + 1) + xSize;
                        vertices[idx] = new Vector3(worldX, transform.position.y + y, worldZ);
                        colors[idx] = gradient.Evaluate(y * inversMaxHeight);
                    }
                }
                else
                {
                    // Shift all columns right by one (overwrite column xSize, keep col 0)
                    for (int z = 0; z <= zSize; z++)
                    {
                        for (int x = xSize; x > 0; x--)
                        {
                            vertices[z * (xSize + 1) + x] = vertices[z * (xSize + 1) + x - 1];
                            colors[z * (xSize + 1) + x] = colors[z * (xSize + 1) + x - 1];
                        }
                    }

                    yield return new WaitForEndOfFrame();

                    // Fill the new leftmost column with fresh vertices
                    float worldX = currentPos.x - xHalfSize;
                    for (int z = 0; z <= zSize; z++)
                    {
                        float worldZ = currentPos.y - zHalfSize + z;
                        float y = GetHeight(worldX, worldZ);
                        int idx = z * (xSize + 1);
                        vertices[idx] = new Vector3(worldX, transform.position.y + y, worldZ);
                        colors[idx] = gradient.Evaluate(y * inversMaxHeight);
                    }
                }
            }
        }

        if (diff.x != 0 && diff.y != 0)
        {
            yield return new WaitForEndOfFrame();
        }

        // Handle Z-axis movement (rows)
        if (diff.y != 0)
        {

            int step = diff.y > 0 ? 1 : -1;

            int tilesMoved = Mathf.Abs(diff.y);
            for (int move = 0; move < tilesMoved; move++)
            {
                Vector2Int currentPos = lastGridPosition + new Vector2Int(diff.x, step * (move + 1));

                if (step > 0)
                {
                    // Shift all rows down by one (overwrite row 0, keep row zSize)
                    for (int z = 0; z < zSize; z++)
                    {
                        for (int x = 0; x <= xSize; x++)
                        {
                            vertices[z * (xSize + 1) + x] = vertices[(z + 1) * (xSize + 1) + x];
                            colors[z * (xSize + 1) + x] = colors[(z + 1) * (xSize + 1) + x];
                        }
                    }

                    yield return new WaitForEndOfFrame();

                    // Fill the new top row with fresh vertices
                    float worldZ = currentPos.y + zHalfSize;
                    for (int x = 0; x <= xSize; x++)
                    {
                        float worldX = currentPos.x - xHalfSize + x;
                        float y = GetHeight(worldX, worldZ);
                        int idx = zSize * (xSize + 1) + x;
                        vertices[idx] = new Vector3(worldX, transform.position.y + y, worldZ);
                        colors[idx] = gradient.Evaluate(y * inversMaxHeight);
                    }
                }
                else
                {
                    // Shift all rows up by one (overwrite row zSize, keep row 0)
                    for (int z = zSize; z > 0; z--)
                    {
                        for (int x = 0; x <= xSize; x++)
                        {
                            vertices[z * (xSize + 1) + x] = vertices[(z - 1) * (xSize + 1) + x];
                            colors[z * (xSize + 1) + x] = colors[(z - 1) * (xSize + 1) + x];
                        }
                    }

                    yield return new WaitForEndOfFrame();

                    // Fill the new bottom row with fresh vertices
                    float worldZ = currentPos.y - zHalfSize;
                    for (int x = 0; x <= xSize; x++)
                    {
                        float worldX = currentPos.x - xHalfSize + x;
                        float y = GetHeight(worldX, worldZ);
                        int idx = x;
                        vertices[idx] = new Vector3(worldX, transform.position.y + y, worldZ);
                        colors[idx] = gradient.Evaluate(y * inversMaxHeight);
                    }
                }
            }
        }

        if (diff.x != 0 || diff.y != 0)
        {
            UpdateMesh();
        }

        lastGridPosition = newPlayerPos;
        updatingTerrain = false;
    }

    public void Update()
    {
        if (player && IsGeneratingTerrain && !updatingTerrain)
        {
            Vector3 playerPos = player.transform.position;

            Vector2Int snappedPlayerPos = new Vector2Int(
                Mathf.FloorToInt(playerPos.x),
                Mathf.FloorToInt(playerPos.z)
            );

            if (lastGridPosition.x != snappedPlayerPos.x || lastGridPosition.y != snappedPlayerPos.y)
            {
                updatingTerrain = true;
                StartCoroutine(UpdateTerrain(snappedPlayerPos));
            }
        }
    }

    public void GenerateTerrain()
    {
        InitializeTerrainGeneration();
        CreateVertices();
        UpdateMesh();
        IsGeneratingTerrain = true;
    }

    public bool SphereCollision(Vector3 center, float radius, out Vector3 normal, out float penetration)
    {
        int gridX = (int)(center.x - (lastGridPosition.x - xHalfSize));
        int gridZ = (int)(center.z - (lastGridPosition.y - zHalfSize));
        int closestTerrainVertex = gridZ * (xSize + 1) + gridX;
        normal = Vector3.zero;
        penetration = 0;

        if (closestTerrainVertex < 0 || closestTerrainVertex >= vertices.Length)
        {
            return false;
        }

        Vector3 vertex = vertices[closestTerrainVertex];
        Vector3 distance = center - vertex;
        // End early if the closest terrain vertex is to far away
        if (distance.sqrMagnitude > radius * radius)
        {
            return false;
        }

        // Each quad is defined by its bottom-left corner vertex (BL).
        // The 4 quads that share closestTerrainVertex as a corner are:
        //   BL offsets in grid space: (-1,-1), (0,-1), (-1,0), (0,0)
        // i.e. the vertex can be the BR, BL, TR, or TL of each quad respectively.
        (int, int)[] offsets = {
            (-1,-1), (0,-1), (-1,0), (0,0)
        };
        bool collided = false;

        foreach (var (ox, oz) in offsets)
        {
            int blX = gridX + ox;
            int blZ = gridZ + oz;

            // Skip quads that are out of bounds
            if (blX < 0 || blX >= xSize || blZ < 0 || blZ >= zSize)
                continue;

            // The 4 corners of this quad
            Vector3 bl = vertices[blZ * (xSize + 1) + blX];
            Vector3 br = vertices[blZ * (xSize + 1) + blX + 1];
            Vector3 tl = vertices[(blZ + 1) * (xSize + 1) + blX];
            Vector3 tr = vertices[(blZ + 1) * (xSize + 1) + blX + 1];

            // Each quad is split into 2 triangles: (bl, tl, br) and (br, tl, tr)
            // Test both and resolve if collision found
            Vector3 normal1;
            float penetration1;
            if (ResolveTriangleSphere(bl, tl, br, center, radius, out normal1, out penetration1))
                collided = true;

            Vector3 normal2;
            float penetration2;
            if (ResolveTriangleSphere(br, tl, tr, center, radius, out normal2, out penetration2))
                collided = true;

            normal = normal + normal1 + normal2;
            penetration = penetration + penetration1 + penetration2;
        }

        return collided;
    }

    // Returns true and resolves the collision if the sphere overlaps the triangle.
    private bool ResolveTriangleSphere(Vector3 a, Vector3 b, Vector3 c,
        Vector3 center, float radius, out Vector3 normal, out float penetration)
    {
        // 1. Find the closest point on the triangle to the sphere center
        Vector3 closest = ClosestPointOnTriangle(a, b, c, center);

        // 2. Check if it's within radius
        Vector3 delta = center - closest;
        float distSq = delta.sqrMagnitude;

        if (distSq < radius * radius)
        {
            float dist = Mathf.Sqrt(distSq);
            normal = dist > 0.0001f ? delta / dist : Vector3.up;
            penetration = radius - dist;
            return true;
        }

        penetration = 0;
        normal = Vector3.zero;
        return false;
    }

    private Vector3 ClosestPointOnTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = p - a;

        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) return a;

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) return b;

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0 && d5 <= d6) return c;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
        {
            float v = d1 / (d1 - d3);
            return a + v * ab;
        }

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
        {
            float w = d2 / (d2 - d6);
            return a + w * ac;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + w * (c - b);
        }

        float denom = 1.0f / (va + vb + vc);
        float vf = vb * denom;
        float wf = vc * denom;
        return a + ab * vf + ac * wf;
    }
}
