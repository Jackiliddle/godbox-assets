using UnityEngine;
using UnityEngine.AI;

public class FarmSpawner : MonoBehaviour
{
    [Header("Tags")]
    public string farmTag = "Farm";
    public string animalTag = "Animal";

    [Header("Animal Prefabs")]
    public GameObject[] animalPrefabs;

    [Header("Spawn Settings")]
    public int animalsPerFarm = 3;
    public float spawnRadiusAroundFarm = 2f;

    [Header("Map Bounds (20x20)")]
    public Transform mapCenter;
    public Vector2 mapSize = new Vector2(20f, 20f);

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

        if (mapCenter == null)
        {
            Debug.LogError("mapCenter is not assigned.");
            return;
        }

        foreach (var farm in farms)
        {
            for (int i = 0; i < animalsPerFarm; i++)
            {
                SpawnAnimalNearFarm(farm.transform.position);
            }
        }
    }

    void SpawnAnimalNearFarm(Vector3 farmPos)
    {
        var prefab = animalPrefabs[Random.Range(0, animalPrefabs.Length)];

        Vector2 r = Random.insideUnitCircle * spawnRadiusAroundFarm;
        Vector3 candidate = new Vector3(farmPos.x + r.x, farmPos.y + 2f, farmPos.z + r.y);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            var go = Instantiate(prefab, hit.position, Quaternion.identity);

            if (!go.CompareTag(animalTag))
                go.tag = animalTag;

            var wander = go.GetComponent<Wanderer>();
            if (wander != null)
            {
                wander.mapCenter = mapCenter;
                wander.mapSize = mapSize;
            }
            else
            {
                Debug.LogWarning($"{go.name} spawned but has no Wanderer component.");
            }
        }
        else
        {
            Debug.LogWarning("Could not find NavMesh near farm to spawn animal.");
        }
    }
}