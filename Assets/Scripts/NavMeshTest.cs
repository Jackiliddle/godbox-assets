using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshProbe : MonoBehaviour
{
    void Start()
    {
        var agent = GetComponent<NavMeshAgent>();

        Debug.Log("Probe start position: " + transform.position);
        Debug.Log("Probe isOnNavMesh before warp: " + agent.isOnNavMesh);

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            Debug.Log("Found NavMesh near probe at: " + hit.position);
            agent.Warp(hit.position);
            Debug.Log("Probe isOnNavMesh after warp: " + agent.isOnNavMesh);
        }
        else
        {
            Debug.LogError("No NavMesh found near probe.");
        }
    }
}