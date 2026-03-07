using UnityEngine;
using UnityEngine.AI;

public class HouseSpawner : MonoBehaviour
{
    [Header("Spawn Sources")]
    [Tooltip("All scene objects tagged 'House' will be used as spawn points.")]
    public string houseTag = "House";

    [Header("People Prefabs")]
    [Tooltip("Prefabs to spawn. Each prefab should have a NavMeshAgent and (optionally) a Wanderer.")]
    public GameObject[] peoplePrefabs;

    [Header("Spawn Settings")]
    public int peoplePerHouse = 2;

    [Tooltip("Random radius around each house to try spawn candidates.")]
    public float spawnRadiusAroundHouse = 3f;

    [Tooltip("How far we are allowed to search for a NavMesh point from the candidate.")]
    public float maxSpawnSearchDistance = 50f;

    [Tooltip("How many random attempts per person before giving up.")]
    public int attemptsPerPerson = 10;

    [Header("Ground Detection")]
    [Tooltip("Raycast height above candidate point.")]
    public float raycastHeight = 50f;

    [Tooltip("Only raycast against these layers for ground. Set to your ground layer(s).")]
    public LayerMask groundMask = ~0; // Everything by default (set this properly!)

    void Start()
    {
        var houses = GameObject.FindGameObjectsWithTag(houseTag);
        if (houses == null || houses.Length == 0)
        {
            Debug.LogError($"No objects found with tag '{houseTag}'.");
            return;
        }

        if (peoplePrefabs == null || peoplePrefabs.Length == 0)
        {
            Debug.LogError("No peoplePrefabs assigned.");
            return;
        }

        foreach (var house in houses)
        {
            for (int i = 0; i < peoplePerHouse; i++)
            {
                TrySpawnPersonNearHouse(house.transform.position);
            }
        }
    }

    void TrySpawnPersonNearHouse(Vector3 housePos)
    {
        var prefab = peoplePrefabs[Random.Range(0, peoplePrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerPerson; attempt++)
        {
            // Random candidate around house (XZ)
            Vector2 r = Random.insideUnitCircle * spawnRadiusAroundHouse;
            Vector3 candidateXZ = new Vector3(housePos.x + r.x, housePos.y, housePos.z + r.y);

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

        Debug.LogWarning(
            $"Could not find NavMesh near house to spawn person. " +
            $"HousePos={housePos}, spawnRadius={spawnRadiusAroundHouse}, maxSearch={maxSpawnSearchDistance}, attempts={attemptsPerPerson}"
        );
    }

    void Spawn(GameObject prefab, Vector3 position)
    {
        var go = Instantiate(prefab, position, Quaternion.identity);

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