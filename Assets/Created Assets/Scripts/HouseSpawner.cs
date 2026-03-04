using UnityEngine;
using UnityEngine.AI;

public class HouseSpawner : MonoBehaviour
{
    [Header("Spawn Sources")]
    [Tooltip("All scene objects tagged 'House' will be used as spawn points.")]
    public string houseTag = "House";

    [Header("People Prefabs")]
    [Tooltip("Prefabs to spawn. Each prefab should be tagged 'People'.")]
    public GameObject[] peoplePrefabs;

    [Header("Spawn Settings")]
    public int peoplePerHouse = 2;
    public float spawnRadiusAroundHouse = 1.5f;

    [Header("Map Bounds (20x20)")]
    [Tooltip("Center of the map area (your disc).")]
    public Transform mapCenter;
    [Tooltip("Width and length of walkable area. 20x20 means x=20 z=20.")]
    public Vector2 mapSize = new Vector2(20f, 20f);

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

        if (mapCenter == null)
        {
            Debug.LogError("mapCenter is not assigned.");
            return;
        }

        foreach (var house in houses)
        {
            for (int i = 0; i < peoplePerHouse; i++)
            {
                SpawnPersonNearHouse(house.transform.position);
            }
        }
    }

    void SpawnPersonNearHouse(Vector3 housePos)
    {
        var prefab = peoplePrefabs[Random.Range(0, peoplePrefabs.Length)];

        // Random point near the house
        Vector2 r = Random.insideUnitCircle * spawnRadiusAroundHouse;
        Vector3 candidate = new Vector3(housePos.x + r.x, housePos.y + 2f, housePos.z + r.y);

        // Snap to NavMesh if possible
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            var go = Instantiate(prefab, hit.position, Quaternion.identity);
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
            Debug.LogWarning("Could not find NavMesh near house to spawn person.");
        }
    }
}