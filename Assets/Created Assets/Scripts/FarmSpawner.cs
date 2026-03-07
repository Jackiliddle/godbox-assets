using UnityEngine;
using UnityEngine.AI;

public class FarmSpawner : MonoBehaviour
{
    [Header("Tags")]
    public string farmTag = "Farm";
    public string animalTag = "Animal";

    [Header("Animal Prefabs")]
    [Tooltip("Prefabs to spawn. Each prefab should have a NavMeshAgent and (optionally) a Wanderer.")]
    public GameObject[] animalPrefabs;

    [Header("Spawn Settings")]
    public int animalsPerFarm = 3;

    [Tooltip("Random radius around each farm to try spawn candidates.")]
    public float spawnRadiusAroundFarm = 3f;

    [Tooltip("How far we are allowed to search for a NavMesh point from the candidate.")]
    public float maxSpawnSearchDistance = 50f;

    [Tooltip("How many random attempts per animal before giving up.")]
    public int attemptsPerAnimal = 10;

    [Header("Ground Detection")]
    [Tooltip("Raycast height above candidate point.")]
    public float raycastHeight = 50f;

    [Tooltip("Only raycast against these layers for ground. Set to your ground layer(s).")]
    public LayerMask groundMask = ~0; // Everything by default (set this properly!)

    void Start()
    {
        var farms = GameObject.FindGameObjectsWithTag(farmTag);
        if (farms == null || farms.Length == 0)
        {
            Debug.LogError($"No objects found with tag '{farmTag}'.");
            return;
        }

        if (animalPrefabs == null || animalPrefabs.Length == 0)
        {
            Debug.LogError("No animalPrefabs assigned.");
            return;
        }

        foreach (var farm in farms)
        {
            for (int i = 0; i < animalsPerFarm; i++)
            {
                TrySpawnAnimalNearFarm(farm.transform.position);
            }
        }
    }

    void TrySpawnAnimalNearFarm(Vector3 farmPos)
    {
        var prefab = animalPrefabs[Random.Range(0, animalPrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerAnimal; attempt++)
        {
            // Random candidate around farm (XZ)
            Vector2 r = Random.insideUnitCircle * spawnRadiusAroundFarm;
            Vector3 candidateXZ = new Vector3(farmPos.x + r.x, farmPos.y, farmPos.z + r.y);

            // Raycast down to find ground point (handles arbitrary Y)
            Vector3 rayStart = candidateXZ + Vector3.up * raycastHeight;
            Vector3 sampleFrom = candidateXZ;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, raycastHeight * 2f, groundMask))
            {
                sampleFrom = groundHit.point;
            }

            // Snap to nearest navmesh within max distance
            if (NavMesh.SamplePosition(sampleFrom, out NavMeshHit hit, maxSpawnSearchDistance, NavMesh.AllAreas))
            {
                Spawn(prefab, hit.position);
                return;
            }
        }

        // Fallback: try sampling directly around the farm (bigger radius)
        if (NavMesh.SamplePosition(farmPos, out NavMeshHit fallbackHit, maxSpawnSearchDistance * 2f, NavMesh.AllAreas))
        {
            Spawn(prefab, fallbackHit.position);
            return;
        }

        Debug.LogWarning(
            $"Could not find NavMesh near farm to spawn animal. " +
            $"FarmPos={farmPos}, spawnRadius={spawnRadiusAroundFarm}, maxSearch={maxSpawnSearchDistance}, attempts={attemptsPerAnimal}"
        );
    }

    void Spawn(GameObject prefab, Vector3 position)
    {
        var go = Instantiate(prefab, position, Quaternion.identity);

        // Ensure tag (optional)
        if (!string.IsNullOrEmpty(animalTag) && !go.CompareTag(animalTag))
            go.tag = animalTag;

        if (go.GetComponent<NavMeshAgent>() == null)
        {
            Debug.LogWarning($"{go.name} spawned but has no NavMeshAgent.");
        }

        if (go.GetComponent<Wanderer>() == null)
        {
            Debug.LogWarning($"{go.name} spawned but has no Wanderer component.");
        }
    }
}