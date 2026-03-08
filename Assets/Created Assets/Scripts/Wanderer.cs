using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Wanderer : MonoBehaviour
{
    [Header("Territory (around spawn point)")]
    [Tooltip("Half-width of allowed wander area on X axis.")]
    public float territoryHalfX = 1.5f;

    [Tooltip("Half-depth of allowed wander area on Z axis.")]
    public float territoryHalfZ = 1.0f;

    [Header("Wander Timing")]
    [Tooltip("Minimum time between destination picks (seconds).")]
    public float minWait = 1.5f;

    [Tooltip("Maximum time between destination picks (seconds).")]
    public float maxWait = 3.5f;

    [Tooltip("If close enough to destination, we consider it reached.")]
    public float waypointTolerance = 0.15f;

    [Header("NavMesh Sampling")]
    [Tooltip("How far from the desired point we search for a valid NavMesh position.")]
    public float sampleRadius = 0.35f;

    [Header("Natural Motion")]
    [Tooltip("How far ahead we aim each step.")]
    public float stepDistance = 0.35f;

    [Tooltip("Max turning angle per step (degrees).")]
    public float turnJitterDegrees = 20f;

    [Tooltip("Occasionally do a bigger turn.")]
    public float bigTurnChance = 0.08f;

    [Tooltip("Big turn angle range (degrees).")]
    public Vector2 bigTurnRange = new Vector2(45f, 90f);

    [Tooltip("How strongly we steer back toward home when near the edge.")]
    [Range(0f, 1f)]
    public float homePull = 0.55f;

    [Header("Agent Movement")]
    [Tooltip("Lower this if they still move too fast.")]
    public float agentSpeed = 0.6f;

    [Tooltip("How quickly the agent accelerates.")]
    public float agentAcceleration = 2f;

    [Tooltip("How quickly the agent turns.")]
    public float agentAngularSpeed = 180f;

    NavMeshAgent agent;
    float nextMoveTime;

    Vector3 homePosition;
    bool homeSet;

    Vector3 heading;

    const int k_SampleAttempts = 5;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed = agentSpeed;
            agent.acceleration = agentAcceleration;
            agent.angularSpeed = agentAngularSpeed;
            agent.stoppingDistance = waypointTolerance;
            agent.autoBraking = true;
        }

        homePosition = transform.position;
        homeSet = true;

        heading = Random.insideUnitSphere;
        heading.y = 0f;

        if (heading.sqrMagnitude < 0.001f)
            heading = Vector3.forward;

        heading.Normalize();

        ScheduleNextMove(0.25f);
    }

    void Update()
    {
        if (!homeSet || agent == null || !agent.isOnNavMesh)
            return;

        bool reached =
            !agent.pathPending &&
            agent.remainingDistance <= waypointTolerance;

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

    void TrySetNextMeanderDestination()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        float turn = Random.Range(-turnJitterDegrees, turnJitterDegrees);

        if (Random.value < bigTurnChance)
        {
            float big = Random.Range(bigTurnRange.x, bigTurnRange.y);
            turn += (Random.value < 0.5f ? -big : big);
        }

        heading = Quaternion.Euler(0f, turn, 0f) * heading;
        heading.y = 0f;

        if (heading.sqrMagnitude < 0.001f)
            heading = Vector3.forward;

        heading.Normalize();

        Vector3 desired = transform.position + heading * stepDistance;

        Vector3 toHome = homePosition - transform.position;
        toHome.y = 0f;

        float edgeX = territoryHalfX * 0.8f;
        float edgeZ = territoryHalfZ * 0.8f;

        Vector3 localFromHome = transform.position - homePosition;
        localFromHome.y = 0f;

        bool nearEdge =
            Mathf.Abs(localFromHome.x) > edgeX ||
            Mathf.Abs(localFromHome.z) > edgeZ;

        if (nearEdge && toHome.sqrMagnitude > 0.001f)
        {
            Vector3 homeDir = toHome.normalized;
            heading = Vector3.Slerp(heading, homeDir, homePull).normalized;
            desired = transform.position + heading * stepDistance;
        }

        desired = ClampToTerritory(desired);

        float tryRadius = Mathf.Max(0.01f, sampleRadius);

        for (int i = 0; i < k_SampleAttempts; i++)
        {
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, tryRadius, NavMesh.AllAreas))
            {
                if (!ApproximatelySamePosition(agent.destination, hit.position))
                    agent.SetDestination(hit.position);

                return;
            }

            desired = Vector3.Lerp(desired, homePosition, 0.4f);
            desired = ClampToTerritory(desired);
            tryRadius *= 1.35f;
        }
    }

    Vector3 ClampToTerritory(Vector3 p)
    {
        Vector3 local = p - homePosition;
        local.y = 0f;

        local.x = Mathf.Clamp(local.x, -territoryHalfX, territoryHalfX);
        local.z = Mathf.Clamp(local.z, -territoryHalfZ, territoryHalfZ);

        return homePosition + new Vector3(local.x, 0f, local.z);
    }

    public void SetHomePosition(Vector3 newHome, bool resetHeading = false)
    {
        homePosition = newHome;
        homeSet = true;

        if (resetHeading)
        {
            heading = transform.forward;
            heading.y = 0f;

            if (heading.sqrMagnitude < 0.001f)
                heading = Vector3.forward;

            heading.Normalize();
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 drawHome = Application.isPlaying || homePosition != Vector3.zero
            ? homePosition
            : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(drawHome, 0.05f);

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(
            drawHome,
            new Vector3(territoryHalfX * 2f, 0.01f, territoryHalfZ * 2f)
        );

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 from = transform.position;
            Vector3 to = from + heading.normalized * stepDistance;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawSphere(to, 0.03f);
        }
    }

    static bool ApproximatelySamePosition(Vector3 a, Vector3 b, float epsilon = 0.01f)
    {
        return (a - b).sqrMagnitude <= epsilon * epsilon;
    }

    void OnValidate()
    {
        territoryHalfX = Mathf.Max(0.05f, territoryHalfX);
        territoryHalfZ = Mathf.Max(0.05f, territoryHalfZ);

        minWait = Mathf.Max(0f, minWait);
        maxWait = Mathf.Max(minWait, maxWait);

        waypointTolerance = Mathf.Max(0.01f, waypointTolerance);
        sampleRadius = Mathf.Max(0.01f, sampleRadius);
        stepDistance = Mathf.Max(0.05f, stepDistance);

        turnJitterDegrees = Mathf.Max(0f, turnJitterDegrees);
        bigTurnChance = Mathf.Clamp01(bigTurnChance);

        if (bigTurnRange.x > bigTurnRange.y)
            bigTurnRange = new Vector2(bigTurnRange.y, bigTurnRange.x);

        homePull = Mathf.Clamp01(homePull);

        agentSpeed = Mathf.Max(0.01f, agentSpeed);
        agentAcceleration = Mathf.Max(0.01f, agentAcceleration);
        agentAngularSpeed = Mathf.Max(0.01f, agentAngularSpeed);

        if (agent != null)
        {
            agent.speed = agentSpeed;
            agent.acceleration = agentAcceleration;
            agent.angularSpeed = agentAngularSpeed;
            agent.stoppingDistance = waypointTolerance;
        }
    }
}