using UnityEngine;

public class ButterflyHoverWander : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Tag used for flower objects.")]
    public string flowerTag = "Flowers";

    [Tooltip("Assigned by spawner. If null, butterfly will try to find one.")]
    public Transform orbitCenter;

    [Header("Orbit")]
    public float orbitRadius = 2f;
    public float orbitSpeedDegPerSec = 50f;

    [Header("Hover")]
    public float baseHeight = 5f;     // your “up by 5”
    public float bobAmount = 0.4f;
    public float bobSpeed = 2f;

    [Header("Wander Between Flowers")]
    [Tooltip("How far to search for a new flower target.")]
    public float switchSearchRadius = 15f;

    [Tooltip("How often (seconds) it considers switching flowers.")]
    public Vector2 switchIntervalRange = new Vector2(2.5f, 6f);

    [Tooltip("Chance to switch when the timer fires (0..1).")]
    [Range(0f, 1f)]
    public float switchChance = 0.55f;

    [Header("Travel")]
    [Tooltip("How fast it moves its center from one flower to the next.")]
    public float travelSpeed = 2.5f;

    [Tooltip("Extra random drift added to avoid perfect circles.")]
    public float driftAmount = 0.6f;

    [Tooltip("How quickly it rotates to face direction of travel.")]
    public float turnSmoothing = 6f;

    // internal state
    float angleDeg;
    float direction;

    Vector3 virtualCenter;         // where we're orbiting *right now* (moves between flowers)
    Vector3 driftOffset;
    float nextSwitchTime;

    void Awake()
    {
        angleDeg = Random.Range(0f, 360f);
        direction = Random.value < 0.5f ? -1f : 1f;

        driftOffset = Random.insideUnitSphere * driftAmount;
        driftOffset.y = 0f;
    }

    void Start()
    {
        if (orbitCenter == null)
        {
            orbitCenter = FindNearestFlower(transform.position);
        }

        if (orbitCenter != null)
        {
            virtualCenter = orbitCenter.position;
        }
        else
        {
            // No flowers found: just hover where you spawned.
            virtualCenter = transform.position;
        }

        ScheduleNextSwitch();
        transform.position = ComputeTargetPosition();
    }

    void Update()
    {
        // Smoothly move the virtual center toward the current flower
        if (orbitCenter != null)
        {
            virtualCenter = Vector3.MoveTowards(
                virtualCenter,
                orbitCenter.position,
                travelSpeed * Time.deltaTime
            );
        }

        // Occasionally pick a different flower
        if (Time.time >= nextSwitchTime)
        {
            if (Random.value < switchChance)
            {
                Transform next = PickNearbyFlower();
                if (next != null) orbitCenter = next;

                // Small random drift change so it doesn't look like a robot on rails
                driftOffset = Random.insideUnitSphere * driftAmount;
                driftOffset.y = 0f;

                // Sometimes flip orbit direction
                if (Random.value < 0.35f) direction *= -1f;
            }

            ScheduleNextSwitch();
        }

        // Orbit motion
        angleDeg += orbitSpeedDegPerSec * direction * Time.deltaTime;

        Vector3 target = ComputeTargetPosition();

        // Move
        Vector3 prev = transform.position;
        transform.position = target;

        // Face direction of travel (looks more like flying than spinning)
        Vector3 travelDir = (transform.position - prev);
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

        // Slightly wobble the radius to avoid perfect circles
        float r = orbitRadius + Mathf.Sin(Time.time * 0.7f + rad) * 0.3f;

        float bob = Mathf.Sin(Time.time * bobSpeed + rad) * bobAmount;

        Vector3 circle = new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r);

        return virtualCenter + circle + driftOffset + Vector3.up * (baseHeight + bob);
    }

    Transform PickNearbyFlower()
    {
        Collider[] hits = Physics.OverlapSphere(virtualCenter, switchSearchRadius);
        Transform best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (!t.CompareTag(flowerTag)) continue;

            // Prefer not-the-same, not-too-close, not-too-far
            float d = Vector3.Distance(virtualCenter, t.position);
            if (orbitCenter != null && t == orbitCenter) continue;

            float score =
                -Mathf.Abs(d - switchSearchRadius * 0.6f)     // prefer mid-range
                + Random.Range(-0.5f, 0.5f);                  // break ties

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        // If no nearby colliders found (no colliders on flowers), fallback to global search.
        if (best == null)
            best = FindNearestFlower(virtualCenter);

        return best;
    }

    Transform FindNearestFlower(Vector3 from)
    {
        GameObject[] flowers = GameObject.FindGameObjectsWithTag(flowerTag);
        if (flowers == null || flowers.Length == 0) return null;

        Transform best = null;
        float bestD = float.PositiveInfinity;

        for (int i = 0; i < flowers.Length; i++)
        {
            float d = (flowers[i].transform.position - from).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = flowers[i].transform;
            }
        }

        return best;
    }
}