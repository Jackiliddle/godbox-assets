using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class WildlifeSpawner : MonoBehaviour
{
    [Header("Spawn Sources")]
    [Tooltip("All scene objects tagged 'Forest' will be used as wildlife spawn points.")]
    public string forestTag = "Forest";

    [Header("Wildlife Prefabs")]
    [Tooltip("Prefabs to spawn. Each prefab should have a NavMeshAgent and (optionally) a Wanderer.")]
    public GameObject[] wildlifePrefabs;

    [Header("Spawn Settings")]
    [Tooltip("How many animals spawn per forest object.")]
    public int wildlifePerForest = 4;

    [Tooltip("Random radius around each forest object to try spawn candidates.")]
    public float spawnRadiusAroundForest = 8f;

    [Tooltip("How far we are allowed to search for a NavMesh point from the candidate.")]
    public float maxSpawnSearchDistance = 50f;

    [Tooltip("How many random attempts per animal before giving up.")]
    public int attemptsPerWildlife = 10;

    [Header("Spawn Timing")]
    [Tooltip("Delay between each spawned wildlife animal.")]
    public float delayBetweenSpawns = 0.5f;

    [Tooltip("Optional extra delay after finishing one forest before starting the next.")]
    public float delayBetweenForests = 0.2f;

    [Header("Ground Detection")]
    [Tooltip("Raycast height above candidate point.")]
    public float raycastHeight = 50f;

    [Tooltip("Only raycast against these layers for ground. Set to your ground layer(s).")]
    public LayerMask groundMask = ~0;

    [Header("Optional Tagging")]
    [Tooltip("If set, spawned wildlife will be forced to this tag.")]
    public string wildlifeTag = "Wildlife";

    void Start()
    {
        StartCoroutine(SpawnAllForestsRoutine());
    }

    IEnumerator SpawnAllForestsRoutine()
    {
        var forests = GameObject.FindGameObjectsWithTag(forestTag);
        if (forests == null || forests.Length == 0)
        {
            Debug.LogError($"No objects found with tag '{forestTag}'.");
            yield break;
        }

        if (wildlifePrefabs == null || wildlifePrefabs.Length == 0)
        {
            Debug.LogError("No wildlifePrefabs assigned.");
            yield break;
        }

        foreach (var forest in forests)
        {
            for (int i = 0; i < wildlifePerForest; i++)
            {
                TrySpawnWildlifeNearForest(forest.transform.position);

                if (delayBetweenSpawns > 0f)
                    yield return new WaitForSeconds(delayBetweenSpawns);
            }

            if (delayBetweenForests > 0f)
                yield return new WaitForSeconds(delayBetweenForests);
        }
    }

    void TrySpawnWildlifeNearForest(Vector3 forestPos)
    {
        var prefab = wildlifePrefabs[Random.Range(0, wildlifePrefabs.Length)];

        for (int attempt = 0; attempt < attemptsPerWildlife; attempt++)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadiusAroundForest;
            Vector3 candidateXZ = new Vector3(forestPos.x + r.x, forestPos.y, forestPos.z + r.y);

            Vector3 rayStart = candidateXZ + Vector3.up * raycastHeight;
            Vector3 sampleFrom = candidateXZ;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, raycastHeight * 2f, groundMask))
            {
                sampleFrom = groundHit.point;
            }

            if (NavMesh.SamplePosition(sampleFrom, out NavMeshHit hit, maxSpawnSearchDistance, NavMesh.AllAreas))
            {
                Spawn(prefab, hit.position);
                return;
            }
        }

        Debug.LogWarning(
            $"Could not find NavMesh near forest to spawn wildlife. " +
            $"ForestPos={forestPos}, spawnRadius={spawnRadiusAroundForest}, maxSearch={maxSpawnSearchDistance}, attempts={attemptsPerWildlife}"
        );
    }

    void Spawn(GameObject prefab, Vector3 position)
    {
        var go = Instantiate(prefab, position, Quaternion.identity);

        if (!string.IsNullOrEmpty(wildlifeTag) && !go.CompareTag(wildlifeTag))
        {
            go.tag = wildlifeTag;
        }

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