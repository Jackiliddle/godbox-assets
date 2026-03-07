using System;
using UnityEngine;
using UnityEngine.AI;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public class CreatureMover : MonoBehaviour
    {
        [Header("Animator Params")]
        [SerializeField] private string m_VerticalID = "Vert";
        [SerializeField] private string m_StateID = "State";

        [Header("Look IK")]
        [SerializeField] private bool m_EnableLookIK = true;
        [SerializeField] private LookWeight m_LookWeight = new(1f, 0.3f, 0.7f, 1f);

        [Header("Run Threshold")]
        [Tooltip("When normalized speed is above this, set State=1 (run). Otherwise State=0 (walk).")]
        [Range(0f, 1f)]
        [SerializeField] private float m_RunThreshold = 0.6f;

        private Animator m_Animator;
        private NavMeshAgent m_Agent;

        private void Awake()
        {
            m_Animator = GetComponent<Animator>();
            m_Agent = GetComponent<NavMeshAgent>();

            // Agent-driven motion and rotation
            if (m_Agent != null)
            {
                m_Agent.updatePosition = true;
                m_Agent.updateRotation = true;
            }

            // Root motion usually fights NavMeshAgent movement.
            if (m_Animator != null)
            {
                m_Animator.applyRootMotion = false;
            }
        }

        private void Update()
        {
            if (m_Agent == null || !m_Agent.enabled) return;

            UpdateAnimationFromNavMesh(Time.deltaTime);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!m_EnableLookIK) return;
            if (m_Animator == null) return;
            if (m_Agent == null || !m_Agent.enabled) return;

            // Look where we're steering (usually a point ahead on the path)
            Vector3 lookTarget = m_Agent.steeringTarget;
            m_Animator.SetLookAtPosition(lookTarget);
            m_Animator.SetLookAtWeight(m_LookWeight.weight, m_LookWeight.body, m_LookWeight.head, m_LookWeight.eyes);
        }

        private void UpdateAnimationFromNavMesh(float deltaTime)
        {
            // Project velocity onto XZ
            Vector3 v = m_Agent.velocity;
            v.y = 0f;

            float max = Mathf.Max(0.01f, m_Agent.speed);
            float speed01 = Mathf.Clamp01(v.magnitude / max);

            // Many animal anim controllers only care about magnitude on "Vert"
            m_Animator.SetFloat(m_VerticalID, speed01);

            // "State" used as walk/run blend or discrete state
            float runState = (speed01 > m_RunThreshold) ? 1f : 0f;
            m_Animator.SetFloat(m_StateID, runState);
        }

        [Serializable]
        private struct LookWeight
        {
            public float weight;
            public float body;
            public float head;
            public float eyes;

            public LookWeight(float weight, float body, float head, float eyes)
            {
                this.weight = weight;
                this.body = body;
                this.head = head;
                this.eyes = eyes;
            }
        }
    }
}