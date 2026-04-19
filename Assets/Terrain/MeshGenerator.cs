using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    public int xSize = 250;
    public int zSize = 250;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        StartCoroutine(CreateShape());
    }

    private void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }

    IEnumerator CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise(x * 0.6f, z * 0.6f);
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }


        triangles = new int[xSize * zSize * 6];
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

        yield return null;
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
        UpdateMesh();
    }
}
