using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh = null;
    Vector3[] vertices;
    Color[] colors;
    int[] triangles;

    // Source: https://www.youtube.com/watch?v=wbpMiKiSKm8
    [Tooltip("Controls increase in frequency of octaves")]
    public float lacunarity = 2;

    [Tooltip("Controls decrease in amplitude of octaves")]
    public float persistance = 0.5f;

    public int octaves = 3;

    public Gradient gradient;

    GameObject player;
    Vector2 lastGridPosition;

    public int xSize = 250;
    public int zSize = 250;
    int xHalfSize;
    int zHalfSize;

    float maxHeight;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        colors = new Color[vertices.Length];
        triangles = new int[xSize * zSize * 6];
        maxHeight = Mathf.Pow(persistance, octaves - 1);
        xHalfSize = xSize / 2;
        zHalfSize = zSize / 2;

        GetComponent<MeshFilter>().mesh = mesh;

        player = GameObject.FindGameObjectWithTag("Player");

        CreateShape();
    }

    private void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateNormals();
    }

    void OnValidate()
    {
        if (mesh)
        {
            CreateShape();
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

    void CreateShape()
    {
        Vector3 playerPos = player ? player.transform.position : Vector3.zero;

        lastGridPosition = new Vector2(
            Mathf.Floor(playerPos.x),
            Mathf.Floor(playerPos.z)
        );

        for (int i = 0, z = -zHalfSize; z <= zHalfSize; z++)
        {
            for (int x = -xHalfSize; x <= xHalfSize; x++)
            {
                float worldX = lastGridPosition.x + x;
                float worldZ = lastGridPosition.y + z;

                float y = GetHeight(worldX, worldZ);

                // Keep mesh centered around player
                vertices[i] = new Vector3(x, y, z);

                float gradientValue = y / maxHeight;
                colors[i] = gradient.Evaluate(gradientValue);

                i++;
            }
        }
        
        int vertex = 0;
        int trianglesCounter = 0;
        for (int z = 0; z < zSize; z++)
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

        UpdateMesh();
    }

    private void OnDrawGizmos()
    {
        if (vertices == null)
        {
            return;
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], .01f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player)
        {
            Vector3 playerPos = player ? player.transform.position : Vector3.zero;

            Vector2 snappedPlayerPos = new Vector2(
                Mathf.Floor(playerPos.x),
                Mathf.Floor(playerPos.z)
            );

            if (lastGridPosition.x != snappedPlayerPos.x || lastGridPosition.y != snappedPlayerPos.y)
            {
                // Move mesh with player
                transform.position = new Vector3(
                    player.transform.position.x,
                    0,
                    player.transform.position.z
                );

                // Regenerate using new offset
                CreateShape();
            }
        }
    }
}
