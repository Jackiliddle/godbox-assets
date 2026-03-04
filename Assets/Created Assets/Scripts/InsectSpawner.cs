using UnityEngine;
using UnityEngine.AI;

public class InsectSpawner : MonoBehaviour
{
    [Header("Spawn Sources")]
    public string flowerTag = "Flowers";

    [Header("Insect Prefabs")]
    public GameObject[] insectPrefabs;

    [Header("Spawn Settings")]
    public int insectsPerFlower = 3;
    public float spawnRadiusAroundFlower = 2f;
    public float maxSpawnSearchDistance = 50f;
    public int attemptsPerInsect = 8;

    [Header("Ground Detection")]
    [Tooltip("How far above the flower to start the raycast.")]
    public float raycastHeight = 50f;

    [Tooltip("How far downward to raycast. Make this big if your flowers can be high up.")]
    public float raycastDownDistance = 500f;

    [Tooltip("Only raycast against these layers for ground. Set to your ground layer(s).")]
    public LayerMask groundMask = ~0;

    [Header("Spawn Height")]
    [Tooltip("Spawn this many units above the ground/navmesh hit.")]
    public float spawnHeightOffset = 5f;

    [Header("Optional Tagging")]
    public string insectTag = "Insect";

    void Start()
    {
        var flowers = GameObject.FindGameObjectsWithTag(flowerTag);

        if (flowers == null || flowers.Length == 0)
        {
            Debug.LogError($"No objects found with tag '{flowerTag}'.");
            return;
        }

        if (insectPrefabs == null || insectPrefabs.Length == 0)
        {
            Debug.LogError("No insectPrefabs assigned.");
            return;
        }

        foreach (var flower in flowers)
        {
            for (int i = 0; i < insectsPerFlower; i++)
            {
                TrySpawnInsectNearFlower(flower.transform.position);
            }
        }
    }

    void TrySpawnInsectNearFlower(Vector3 flowerPos)
    {
        var prefab = insectPrefabs[Random.Range(0, insectPrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerInsect; attempt++)
        {
            // Random candidate around flower (XZ)
            Vector2 r = Random.insideUnitCircle * spawnRadiusAroundFlower;
            Vector3 candidateXZ = new Vector3(flowerPos.x + r.x, flowerPos.y, flowerPos.z + r.y);

            // Raycast DOWN from above the flower to find ground
            Vector3 rayStart = new Vector3(candidateXZ.x, flowerPos.y + raycastHeight, candidateXZ.z);

            Vector3 sampleFrom = candidateXZ;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, raycastHeight + raycastDownDistance, groundMask))
            {
                sampleFrom = groundHit.point;
            }
            else
            {
                // Fallback: try a reasonable ground-ish Y instead of flower Y
                sampleFrom = new Vector3(candidateXZ.x, 0f, candidateXZ.z);
            }

            // Snap to NavMesh
            if (NavMesh.SamplePosition(sampleFrom, out NavMeshHit hit, maxSpawnSearchDistance, NavMesh.AllAreas))
            {
                Spawn(prefab, hit.position);
                return;
            }
        }

        Debug.LogWarning(
            $"Could not find NavMesh near flower to spawn insect. " +
            $"FlowerPos={flowerPos}, spawnRadius={spawnRadiusAroundFlower}, maxSearch={maxSpawnSearchDistance}"
        );
    }

    void Spawn(GameObject prefab, Vector3 navmeshPos)
    {
        Vector3 spawnPos = navmeshPos + Vector3.up * spawnHeightOffset;

        var go = Instantiate(prefab, spawnPos, Quaternion.identity);

        if (!string.IsNullOrEmpty(insectTag) && !go.CompareTag(insectTag))
        {
            go.tag = insectTag;
        }

        if (go.GetComponent<Wanderer>() == null)
        {
            Debug.LogWarning($"{go.name} spawned but has no Wanderer component.");
        }
    }
}