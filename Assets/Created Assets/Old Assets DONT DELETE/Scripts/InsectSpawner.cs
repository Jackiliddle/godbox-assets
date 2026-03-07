using UnityEngine;

public class InsectSpawner : MonoBehaviour
{
    [Header("Spawn Sources")]
    public string flowerTag = "Flowers";

    [Header("Insect Prefabs")]
    public GameObject[] insectPrefabs;

    [Header("Spawn Control")]
    [Tooltip("Chance a flower will spawn insects at all (0..1). Example 0.25 = 25% of flowers).")]
    [Range(0f, 1f)]
    public float spawnChancePerFlower = 0.25f;

    [Tooltip("Hard cap on total insects spawned in the scene. 0 = no cap.")]
    public int maxTotalInsects = 0;

    [Header("Spawn Settings")]
    [Tooltip("How many insects spawn for a chosen flower.")]
    public int insectsPerChosenFlower = 1;

    public float spawnRadiusAroundFlower = 2f;

    [Header("Butterfly Behaviour")]
    public float hoverHeight = 5f;
    public float scaleMultiplier = 10f;

    [Header("Optional Tagging")]
    public string insectTag = "Insect";

    int spawnedCount = 0;

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
            if (maxTotalInsects > 0 && spawnedCount >= maxTotalInsects)
                break;

            // Decide if this flower gets insects
            if (Random.value > spawnChancePerFlower)
                continue;

            int count = Mathf.Max(0, insectsPerChosenFlower);
            for (int i = 0; i < count; i++)
            {
                if (maxTotalInsects > 0 && spawnedCount >= maxTotalInsects)
                    break;

                SpawnInsectForFlower(flower.transform);
                spawnedCount++;
            }
        }
    }

    void SpawnInsectForFlower(Transform flower)
    {
        var prefab = insectPrefabs[Random.Range(0, insectPrefabs.Length)];

        Vector2 r = Random.insideUnitCircle * spawnRadiusAroundFlower;
        Vector3 spawnPos = new Vector3(
            flower.position.x + r.x,
            flower.position.y + hoverHeight,
            flower.position.z + r.y
        );

        var go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        go.transform.localScale *= scaleMultiplier;

        if (!string.IsNullOrEmpty(insectTag) && !go.CompareTag(insectTag))
            go.tag = insectTag;

        var hover = go.GetComponent<ButterflyHoverWander>();
        if (hover != null)
        {
            hover.orbitCenter = flower;
            hover.baseHeight = hoverHeight;
            hover.flowerTag = flowerTag;
        }
        else
        {
            Debug.LogWarning($"{go.name} spawned but has no ButterflyHoverWander component.");
        }
    }
}