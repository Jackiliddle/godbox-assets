using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PersonAnimatorDriver : MonoBehaviour
{
    public Animator animator;
    public string speedParameter = "Speed";
    public float dampTime = 0.1f;

    NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (agent == null || animator == null)
            return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(speedParameter, speed, dampTime, Time.deltaTime);
    }
}