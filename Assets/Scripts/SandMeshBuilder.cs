using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SandMeshBuilder : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public Transform referenceTransform;

    [Header("Grid Dimensions")]
    [Tooltip("Camera width divided by decimation filter magnitude")]
    public int gridWidth = 160;
    [Tooltip("Camera height divided by decimation filter magnitude")]
    public int gridHeight = 120;

    [Header("Mesh Cleanup")]
    [Tooltip("Anything below this is considered invalid/hidden terrain.")]
    public float invalidYThreshold = -500f;

    [Tooltip("Only stitch points together if edges are shorter than this.")]
    public float maxEdge = 0.1f;

    public void GeneratePhysicalMesh(ComputeBuffer pointBuffer)
    {
        AsyncGPUReadback.Request(pointBuffer, request =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU Readback failed! The buffer might be invalid.");
                return;
            }

            var gpuData = request.GetData<Vector3>();
            Vector3[] worldVertices = gpuData.ToArray();

            List<int> triangles = new List<int>();

            // Build triangles only from valid points
            for (int y = 0; y < gridHeight - 1; y++)
            {
                for (int x = 0; x < gridWidth - 1; x++)
                {
                    int i = y * gridWidth + x;
                    int iRight = i + 1;
                    int iTop = i + gridWidth;
                    int iTopRight = i + gridWidth + 1;

                    // Skip any quad touching invalid underground points
                    if (worldVertices[i].y < invalidYThreshold ||
                        worldVertices[iRight].y < invalidYThreshold ||
                        worldVertices[iTop].y < invalidYThreshold ||
                        worldVertices[iTopRight].y < invalidYThreshold)
                    {
                        continue;
                    }

                    // Triangle 1
                    if (Vector3.Distance(worldVertices[i], worldVertices[iRight]) < maxEdge &&
                        Vector3.Distance(worldVertices[i], worldVertices[iTop]) < maxEdge)
                    {
                        triangles.Add(i);
                        triangles.Add(iTop);
                        triangles.Add(iRight);
                    }

                    // Triangle 2
                    if (Vector3.Distance(worldVertices[iRight], worldVertices[iTop]) < maxEdge &&
                        Vector3.Distance(worldVertices[iTop], worldVertices[iTopRight]) < maxEdge)
                    {
                        triangles.Add(iRight);
                        triangles.Add(iTop);
                        triangles.Add(iTopRight);
                    }
                }
            }

            // Use chosen reference transform for local conversion
            Transform t = referenceTransform != null ? referenceTransform : transform;

            // Compact the mesh so ONLY used vertices survive
            Dictionary<int, int> oldToNew = new Dictionary<int, int>();
            List<Vector3> compactVertices = new List<Vector3>();
            List<int> compactTriangles = new List<int>();

            for (int tri = 0; tri < triangles.Count; tri++)
            {
                int oldIndex = triangles[tri];

                if (!oldToNew.TryGetValue(oldIndex, out int newIndex))
                {
                    newIndex = compactVertices.Count;
                    oldToNew[oldIndex] = newIndex;

                    Vector3 localVertex = t.InverseTransformPoint(worldVertices[oldIndex]);
                    compactVertices.Add(localVertex);
                }

                compactTriangles.Add(newIndex);
            }

            Mesh solidMesh = new Mesh();
            solidMesh.indexFormat = IndexFormat.UInt32;
            solidMesh.vertices = compactVertices.ToArray();
            solidMesh.triangles = compactTriangles.ToArray();
            solidMesh.RecalculateNormals();
            solidMesh.RecalculateBounds();

            if (meshFilter != null)
                meshFilter.mesh = solidMesh;

            if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = solidMesh;
            }

            Debug.Log($"Physical Sand Mesh Generated Successfully! Vertices={compactVertices.Count}, Triangles={compactTriangles.Count / 3}");
        });
    }
}