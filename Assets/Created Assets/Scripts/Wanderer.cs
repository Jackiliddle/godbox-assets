using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Wanderer : MonoBehaviour
{
    [Header("Map Bounds (20x20)")]
    public Transform mapCenter;
    public Vector2 mapSize = new Vector2(20f, 20f);

    [Header("Wander")]
    public float minWait = 0.5f;
    public float maxWait = 2.0f;
    public float waypointTolerance = 0.7f;
    public float sampleRadius = 2.0f;

    private NavMeshAgent agent;
    private float nextMoveTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        ScheduleNextMove(0.1f);
    }

    void Update()
    {
        if (mapCenter == null) return;

        // If time to pick a new destination
        if (Time.time >= nextMoveTime)
        {
            TrySetRandomDestination();
            ScheduleNextMove(Random.Range(minWait, maxWait));
        }
        // If reached destination sooner, allow picking another
        else if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            // Optional: speed up wandering responsiveness
        }
    }

    void ScheduleNextMove(float delay)
    {
        nextMoveTime = Time.time + delay;
    }

    void TrySetRandomDestination()
    {
        Vector3 dest = RandomPointInBounds();
        if (NavMesh.SamplePosition(dest, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        // else: fail silently, try again next tick
    }

    Vector3 RandomPointInBounds()
    {
        // 20x20 area centered on mapCenter: x in [-10, +10], z in [-10, +10]
        float halfX = mapSize.x * 0.5f;
        float halfZ = mapSize.y * 0.5f;

        float x = mapCenter.position.x + Random.Range(-halfX, halfX);
        float z = mapCenter.position.z + Random.Range(-halfZ, halfZ);

        // y can be anything; NavMesh.SamplePosition will fix it
        return new Vector3(x, mapCenter.position.y + 2f, z);
    }
}