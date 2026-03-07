
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Wanderer : MonoBehaviour
{
    [Header("Territory (around spawn point)")]
    [Tooltip("Max distance from spawn point the agent will wander.")]
    public float wanderRadius = 10f;

    [Tooltip("If true, wander inside a 10x10 square instead of a circle.")]
    public bool useSquareTerritory = false;

    [Header("Wander Timing")]
    [Tooltip("Minimum time between destination picks (seconds).")]
    public float minWait = 0.5f;

    [Tooltip("Maximum time between destination picks (seconds).")]
    public float maxWait = 2.0f;

    [Tooltip("If close enough to destination, we consider it reached.")]
    public float waypointTolerance = 0.7f;

    [Header("NavMesh Sampling")]
    [Tooltip("How far from the desired point we search for a valid NavMesh position.")]
    public float sampleRadius = 3.0f;

    [Header("Natural Motion (math-y meander)")]
    [Tooltip("How far ahead we aim each step (in world units).")]
    public float stepDistance = 3.0f;

    [Tooltip("Max turning angle per step (degrees). Higher = more erratic).")]
    public float turnJitterDegrees = 35f;

    [Tooltip("Occasionally do a bigger turn to avoid looking too straight-line.")]
    public float bigTurnChance = 0.12f;

    [Tooltip("Big turn angle range (degrees).")]
    public Vector2 bigTurnRange = new Vector2(80f, 160f);

    [Tooltip("How strongly we steer back toward home when near the edge (0..1).")]
    [Range(0f, 1f)]
    public float homePull = 0.35f;

    // Implementation details
    NavMeshAgent agent;
    float nextMoveTime;

    Vector3 homePosition;
    bool homeSet;

    // Internal heading we keep nudging around
    Vector3 heading;

    // Tunables for robust sampling
    const int k_SampleAttempts = 4;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Home position should be where it spawned, not some global map center.
        homePosition = transform.position;
        homeSet = true;

        // Start with a random heading on XZ.
        heading = Random.insideUnitSphere;
        heading.y = 0f;
        if (heading.sqrMagnitude < 0.001f) heading = Vector3.forward;
        heading.Normalize();

        ScheduleNextMove(0.1f);
    }

    void Update()
    {
        if (!homeSet || agent == null || !agent.isOnNavMesh) return;

        bool reached = !agent.pathPending && agent.remainingDistance <= waypointTolerance;

        if (Time.time >= nextMoveTime || reached)
        {
            TrySetNextMeanderDestination();
            ScheduleNextMove(Random.Range(minWait, maxWait));
        }
    }

    void ScheduleNextMove(float delay)
    {
        nextMoveTime = Time.time + Mathf.Max(0f, delay);
    }

    /// <summary>
    /// Pick a new destination using a jittered heading, home pull and NavMesh sampling.
    /// More robust than a single-sample attempt: tries a few times with small inward nudges.
    /// </summary>
    void TrySetNextMeanderDestination()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // 1) Slightly rotate heading (jitter)
        float turn = Random.Range(-turnJitterDegrees, turnJitterDegrees);

        // Occasionally do a bigger turn (looks less robotic)
        if (Random.value < bigTurnChance)
        {
            float big = Random.Range(bigTurnRange.x, bigTurnRange.y);
            turn += (Random.value < 0.5f ? -big : big);
        }

        heading = Quaternion.Euler(0f, turn, 0f) * heading;
        heading.y = 0f;
        heading.Normalize();

        // 2) Propose a target step forward from current position
        Vector3 desired = transform.position + heading * stepDistance;

        // 3) If drifting too far from home, pull heading back toward home
        Vector3 toHome = (homePosition - transform.position);
        toHome.y = 0f;

        float distFromHome = toHome.magnitude;
        if (distFromHome > wanderRadius * 0.8f && distFromHome > 0.001f)
        {
            Vector3 homeDir = toHome / distFromHome;
            heading = Vector3.Slerp(heading, homeDir, homePull).normalized;
            desired = transform.position + heading * stepDistance;
        }

        // 4) Clamp desired position inside territory (circle or square)
        desired = ClampToTerritory(desired);

        // 5) Snap to NavMesh (try multiple times and expand sample radius slightly to be robust)
        float tryRadius = Mathf.Max(0.01f, sampleRadius);
        for (int i = 0; i < k_SampleAttempts; i++)
        {
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, tryRadius, NavMesh.AllAreas))
            {
                // Only set destination if it's meaningfully different
                if (!ApproximatelySamePosition(agent.destination, hit.position))
                    agent.SetDestination(hit.position);
                return;
            }

            // Nudge inward a bit and increase sampling radius so we search gradually
            desired = Vector3.Lerp(desired, homePosition, 0.35f);
            tryRadius *= 1.5f;
        }

        // If we fail, do nothing and try again next cycle.
    }

    Vector3 ClampToTerritory(Vector3 p)
    {
        Vector3 local = p - homePosition;
        local.y = 0f;

        if (useSquareTerritory)
        {
            local.x = Mathf.Clamp(local.x, -wanderRadius, wanderRadius);
            local.z = Mathf.Clamp(local.z, -wanderRadius, wanderRadius);
        }
        else
        {
            float mag = local.magnitude;
            if (mag > wanderRadius && mag > 0.0001f)
                local = local / mag * wanderRadius;
        }

        return homePosition + new Vector3(local.x, 0f, local.z);
    }

    /// <summary>
    /// Sets a new home position at runtime (useful for respawning or moving groups).
    /// </summary>
    public void SetHomePosition(Vector3 newHome, bool resetHeading = false)
    {
        homePosition = newHome;
        homeSet = true;

        if (resetHeading)
        {
            heading = transform.forward;
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.001f) heading = Vector3.forward;
            heading.Normalize();
        }
    }

    /// <summary>
    /// Draw a visualization of the home and territory in the editor.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw home position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(homePosition == Vector3.zero ? transform.position : homePosition, 0.25f);

        // Draw territory
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        if (useSquareTerritory)
        {
            Vector3 hs = new Vector3(wanderRadius * 2f, 0f, wanderRadius * 2f);
            Gizmos.DrawWireCube(homePosition == Vector3.zero ? transform.position : homePosition, hs);
        }
        else
        {
            Gizmos.DrawWireSphere(homePosition == Vector3.zero ? transform.position : homePosition, wanderRadius);
        }

        // Current heading indicator
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 from = transform.position;
            Gizmos.DrawLine(from, from + heading.normalized * stepDistance);
            Gizmos.DrawSphere(from + heading.normalized * stepDistance, 0.08f);
        }
    }

    /// <summary>
    /// Helper: avoid setting identical destinations repeatedly (small epsilon).
    /// </summary>
    static bool ApproximatelySamePosition(Vector3 a, Vector3 b, float epsilon = 0.01f)
    {
        return (a - b).sqrMagnitude <= epsilon * epsilon;
    }

    void OnValidate()
    {
        // Keep values sane in the inspector
        wanderRadius = Mathf.Max(0f, wanderRadius);
        minWait = Mathf.Max(0f, minWait);
        maxWait = Mathf.Max(minWait, maxWait);
        waypointTolerance = Mathf.Max(0f, waypointTolerance);
        sampleRadius = Mathf.Max(0.01f, sampleRadius);
        stepDistance = Mathf.Max(0.01f, stepDistance);
        turnJitterDegrees = Mathf.Max(0f, turnJitterDegrees);
        bigTurnChance = Mathf.Clamp01(bigTurnChance);
        if (bigTurnRange.x > bigTurnRange.y)
            bigTurnRange = new Vector2(bigTurnRange.y, bigTurnRange.x);
        homePull = Mathf.Clamp01(homePull);
    }
}