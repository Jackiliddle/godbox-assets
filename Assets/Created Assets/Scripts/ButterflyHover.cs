using UnityEngine;

public class ButterflyHoverWander : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Tag used for flower objects.")]
    public string flowerTag = "Flowers";

    [Tooltip("Assigned by spawner. If null, butterfly will try to find one.")]
    public Transform orbitCenter;

    [Header("World Bounds (around spawn point)")]
    [Tooltip("Allowed half-width on X from spawn/home position.")]
    public float territoryHalfX = 1.5f;

    [Tooltip("Allowed half-depth on Z from spawn/home position.")]
    public float territoryHalfZ = 1f;

    [Header("Orbit")]
    public float orbitRadius = 0.2f;
    public float orbitSpeedDegPerSec = 50f;

    [Header("Hover")]
    public float baseHeight = 0.35f;
    public float bobAmount = 0.08f;
    public float bobSpeed = 2f;

    [Header("Wander Between Flowers")]
    [Tooltip("How far to search for a new flower target.")]
    public float switchSearchRadius = 1.5f;

    [Tooltip("How often (seconds) it considers switching flowers.")]
    public Vector2 switchIntervalRange = new Vector2(2.5f, 6f);

    [Tooltip("Chance to switch when the timer fires (0..1).")]
    [Range(0f, 1f)]
    public float switchChance = 0.55f;

    [Header("Travel")]
    [Tooltip("How fast it moves its center from one flower to the next.")]
    public float travelSpeed = 0.8f;

    [Tooltip("Extra random drift added to avoid perfect circles.")]
    public float driftAmount = 0.08f;

    [Tooltip("How quickly it rotates to face direction of travel.")]
    public float turnSmoothing = 6f;

    float angleDeg;
    float direction;

    Vector3 virtualCenter;
    Vector3 driftOffset;
    float nextSwitchTime;
    Vector3 homePosition;

    void Awake()
    {
        angleDeg = Random.Range(0f, 360f);
        direction = Random.value < 0.5f ? -1f : 1f;

        driftOffset = Random.insideUnitSphere * driftAmount;
        driftOffset.y = 0f;
    }

    void Start()
    {
        homePosition = transform.position;

        if (orbitCenter == null)
        {
            orbitCenter = FindNearestFlower(transform.position);
        }

        if (orbitCenter != null)
        {
            virtualCenter = ClampToTerritory(orbitCenter.position);
        }
        else
        {
            virtualCenter = ClampToTerritory(transform.position);
        }

        ScheduleNextSwitch();
        transform.position = ClampToTerritoryWithHeight(ComputeTargetPosition());
    }

    void Update()
    {
        if (orbitCenter != null)
        {
            Vector3 targetCenter = ClampToTerritory(orbitCenter.position);

            virtualCenter = Vector3.MoveTowards(
                virtualCenter,
                targetCenter,
                travelSpeed * Time.deltaTime
            );

            virtualCenter = ClampToTerritory(virtualCenter);
        }

        if (Time.time >= nextSwitchTime)
        {
            if (Random.value < switchChance)
            {
                Transform next = PickNearbyFlower();
                if (next != null)
                    orbitCenter = next;

                driftOffset = Random.insideUnitSphere * driftAmount;
                driftOffset.y = 0f;

                if (Random.value < 0.35f)
                    direction *= -1f;
            }

            ScheduleNextSwitch();
        }

        angleDeg += orbitSpeedDegPerSec * direction * Time.deltaTime;

        Vector3 target = ClampToTerritoryWithHeight(ComputeTargetPosition());

        Vector3 prev = transform.position;
        transform.position = target;

        Vector3 travelDir = transform.position - prev;
        travelDir.y = 0f;

        if (travelDir.sqrMagnitude > 0.00001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(travelDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSmoothing);
        }
    }

    void ScheduleNextSwitch()
    {
        nextSwitchTime = Time.time + Random.Range(switchIntervalRange.x, switchIntervalRange.y);
    }

    Vector3 ComputeTargetPosition()
    {
        float rad = angleDeg * Mathf.Deg2Rad;

        float r = orbitRadius + Mathf.Sin(Time.time * 0.7f + rad) * 0.05f;
        float bob = Mathf.Sin(Time.time * bobSpeed + rad) * bobAmount;

        Vector3 circle = new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r);

        return virtualCenter + circle + driftOffset + Vector3.up * (baseHeight + bob);
    }

    Vector3 ClampToTerritory(Vector3 p)
    {
        Vector3 local = p - homePosition;
        local.x = Mathf.Clamp(local.x, -territoryHalfX, territoryHalfX);
        local.z = Mathf.Clamp(local.z, -territoryHalfZ, territoryHalfZ);
        local.y = 0f;

        return new Vector3(
            homePosition.x + local.x,
            p.y,
            homePosition.z + local.z
        );
    }

    Vector3 ClampToTerritoryWithHeight(Vector3 p)
    {
        Vector3 clamped = ClampToTerritory(p);
        return new Vector3(clamped.x, p.y, clamped.z);
    }

    Transform PickNearbyFlower()
    {
        Collider[] hits = Physics.OverlapSphere(virtualCenter, switchSearchRadius);
        Transform best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (!t.CompareTag(flowerTag))
                continue;

            if (orbitCenter != null && t == orbitCenter)
                continue;

            Vector3 candidate = ClampToTerritory(t.position);
            float d = Vector3.Distance(virtualCenter, candidate);

            float score =
                -Mathf.Abs(d - switchSearchRadius * 0.6f)
                + Random.Range(-0.5f, 0.5f);

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        if (best == null)
            best = FindNearestFlower(virtualCenter);

        return best;
    }

    Transform FindNearestFlower(Vector3 from)
    {
        GameObject[] flowers = GameObject.FindGameObjectsWithTag(flowerTag);
        if (flowers == null || flowers.Length == 0)
            return null;

        Transform best = null;
        float bestD = float.PositiveInfinity;

        for (int i = 0; i < flowers.Length; i++)
        {
            Vector3 flowerPos = ClampToTerritory(flowers[i].transform.position);
            float d = (flowerPos - from).sqrMagnitude;

            if (d < bestD)
            {
                bestD = d;
                best = flowers[i].transform;
            }
        }

        return best;
    }

    void OnValidate()
    {
        territoryHalfX = Mathf.Max(0.05f, territoryHalfX);
        territoryHalfZ = Mathf.Max(0.05f, territoryHalfZ);

        orbitRadius = Mathf.Max(0.01f, orbitRadius);
        orbitSpeedDegPerSec = Mathf.Max(0f, orbitSpeedDegPerSec);

        baseHeight = Mathf.Max(0f, baseHeight);
        bobAmount = Mathf.Max(0f, bobAmount);
        bobSpeed = Mathf.Max(0f, bobSpeed);

        switchSearchRadius = Mathf.Max(0.05f, switchSearchRadius);

        if (switchIntervalRange.x > switchIntervalRange.y)
            switchIntervalRange = new Vector2(switchIntervalRange.y, switchIntervalRange.x);

        switchChance = Mathf.Clamp01(switchChance);

        travelSpeed = Mathf.Max(0.01f, travelSpeed);
        driftAmount = Mathf.Max(0f, driftAmount);
        turnSmoothing = Mathf.Max(0f, turnSmoothing);
    }
}