using System.Collections;
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
    public float maxSpawnSearchDistance = 5f;

    [Tooltip("How many random attempts per person before giving up.")]
    public int attemptsPerPerson = 10;

    [Header("Spawn Timing")]
    [Tooltip("Delay between each spawned person.")]
    public float delayBetweenSpawns = 0.5f;

    [Tooltip("Optional extra delay after finishing one house before starting the next.")]
    public float delayBetweenHouses = 0.2f;

    [Header("Ground Detection")]
    [Tooltip("Raycast height above candidate point.")]
    public float raycastHeight = 50f;

    [Tooltip("Only raycast against these layers for ground. Set to your ground layer(s).")]
    public LayerMask groundMask = ~0;

    [Header("Spawn Placement Safety")]
    [Tooltip("Spawn slightly above the navmesh point to help the agent bind cleanly.")]
    public float spawnHeightOffset = 0.2f;

    [Tooltip("Extra navmesh snap radius after instantiating.")]
    public float postSpawnSnapDistance = 2f;

    void Start()
    {
        StartCoroutine(SpawnAllHousesRoutine());
    }

    IEnumerator SpawnAllHousesRoutine()
    {
        GameObject[] houses = GameObject.FindGameObjectsWithTag(houseTag);

        if (houses == null || houses.Length == 0)
        {
            Debug.LogError($"No objects found with tag '{houseTag}'.");
            yield break;
        }

        if (peoplePrefabs == null || peoplePrefabs.Length == 0)
        {
            Debug.LogError("No peoplePrefabs assigned.");
            yield break;
        }

        foreach (GameObject house in houses)
        {
            for (int i = 0; i < peoplePerHouse; i++)
            {
                TrySpawnPersonNearHouse(house.transform.position);

                if (delayBetweenSpawns > 0f)
                    yield return new WaitForSeconds(delayBetweenSpawns);
            }

            if (delayBetweenHouses > 0f)
                yield return new WaitForSeconds(delayBetweenHouses);
        }
    }

    void TrySpawnPersonNearHouse(Vector3 housePos)
    {
        GameObject prefab = peoplePrefabs[Random.Range(0, peoplePrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerPerson; attempt++)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadiusAroundHouse;
            Vector3 candidateXZ = new Vector3(housePos.x + r.x, housePos.y, housePos.z + r.y);

            Vector3 rayStart = candidateXZ + Vector3.up * raycastHeight;
            Vector3 sampleFrom = candidateXZ;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, raycastHeight * 2f, groundMask))
            {
                sampleFrom = groundHit.point;
            }

            Vector3 probePos = sampleFrom + Vector3.up * 1f;

            if (NavMesh.SamplePosition(probePos, out NavMeshHit hit, maxSpawnSearchDistance, NavMesh.AllAreas))
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

    void Spawn(GameObject prefab, Vector3 navMeshPosition)
    {
        Vector3 spawnPos = navMeshPosition + Vector3.up * spawnHeightOffset;

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            bool wasEnabled = agent.enabled;
            agent.enabled = false;

            go.transform.position = spawnPos;

            if (NavMesh.SamplePosition(go.transform.position, out NavMeshHit hit, postSpawnSnapDistance, NavMesh.AllAreas))
            {
                go.transform.position = hit.position + Vector3.up * spawnHeightOffset;
            }

            agent.enabled = wasEnabled;

            if (agent.enabled && agent.isOnNavMesh == false)
            {
                if (NavMesh.SamplePosition(go.transform.position, out NavMeshHit hit2, postSpawnSnapDistance, NavMesh.AllAreas))
                {
                    go.transform.position = hit2.position;
                }
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(go.transform.position);
            }
            else
            {
                Debug.LogWarning($"{go.name} spawned, but NavMeshAgent still failed to bind to NavMesh.");
            }
        }
        else
        {
            Debug.LogWarning($"{go.name} spawned but has no NavMeshAgent.");
        }

        Wanderer wanderer = go.GetComponent<Wanderer>();
        if (wanderer == null)
        {
            Debug.LogWarning($"{go.name} spawned but has no Wanderer component.");
        }
    }
}