using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PersonInteraction : MonoBehaviour
{
    public enum InteractionType
    {
        Fight,
        Love
    }

    [Header("Detection Box")]
    [Tooltip("Half-width of the interaction check box on X.")]
    public float detectHalfWidthX = 1.5f;

    [Tooltip("Half-depth of the interaction check box on Z.")]
    public float detectHalfDepthZ = 1f;

    [Tooltip("Half-height of the interaction check box on Y.")]
    public float detectHalfHeightY = 1f;

    [Tooltip("Only objects with this tag can be interacted with.")]
    public string peopleTag = "People";

    [Tooltip("How often to check for nearby people.")]
    public float checkInterval = 0.4f;

    [Header("Chance")]
    [Tooltip("Chance that an interaction actually starts when someone is found nearby.")]
    [Range(0f, 1f)]
    public float interactionChance = 0.25f;

    [Header("Interaction Timing")]
    [Tooltip("How long the interaction lasts.")]
    public float interactionDuration = 4f;

    [Tooltip("Cooldown before this person can interact again.")]
    public float cooldownDuration = 3f;

    [Header("Interaction Type Chances")]
    [Range(0f, 1f)]
    public float loveChance = 0.5f;

    [Header("Effects")]
    [Tooltip("Particle prefab for fights, e.g. smoke + swearing symbols.")]
    public GameObject fightEffectPrefab;

    [Tooltip("Particle prefab for love, e.g. purple smoke + hearts.")]
    public GameObject loveEffectPrefab;

    [Tooltip("Height offset for spawning the particle effect.")]
    public float effectHeightOffset = 1.2f;

    [Header("Visibility")]
    [Tooltip("Renderers to hide during interaction. Leave empty to auto-find children.")]
    public Renderer[] renderersToHide;

    NavMeshAgent agent;
    Wanderer wanderer;

    bool isInteracting;
    bool isOnCooldown;
    float nextCheckTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        wanderer = GetComponent<Wanderer>();

        if (renderersToHide == null || renderersToHide.Length == 0)
            renderersToHide = GetComponentsInChildren<Renderer>(true);
    }

    void Update()
    {
        if (isInteracting || isOnCooldown) return;
        if (Time.time < nextCheckTime) return;
        if (agent == null || !agent.isOnNavMesh) return;

        nextCheckTime = Time.time + checkInterval;
        TryFindInteractionPartner();
    }

    void TryFindInteractionPartner()
    {
        Vector3 halfExtents = new Vector3(detectHalfWidthX, detectHalfHeightY, detectHalfDepthZ);

        Collider[] hits = Physics.OverlapBox(transform.position, halfExtents, Quaternion.identity);

        PersonInteraction bestCandidate = null;
        float bestDistSqr = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (!hit.CompareTag(peopleTag)) continue;

            PersonInteraction other = hit.GetComponent<PersonInteraction>();
            if (other == null) continue;
            if (other == this) continue;
            if (other.isInteracting || other.isOnCooldown) continue;

            float dSqr = (other.transform.position - transform.position).sqrMagnitude;
            if (dSqr < bestDistSqr)
            {
                bestDistSqr = dSqr;
                bestCandidate = other;
            }
        }

        if (bestCandidate != null)
        {
            // Random chance so it does NOT always happen
            if (Random.value > interactionChance)
                return;

            // prevent both from starting separate coroutines at once
            if (GetInstanceID() < bestCandidate.GetInstanceID())
            {
                InteractionType type = Random.value < loveChance
                    ? InteractionType.Love
                    : InteractionType.Fight;

                StartCoroutine(HandleInteraction(bestCandidate, type));
            }
        }
    }

    IEnumerator HandleInteraction(PersonInteraction other, InteractionType type)
    {
        if (other == null) yield break;
        if (isInteracting || other.isInteracting) yield break;

        isInteracting = true;
        other.isInteracting = true;

        StopAgent(agent);
        StopAgent(other.agent);

        Vector3 midpoint = (transform.position + other.transform.position) * 0.5f;
        Vector3 effectPos = midpoint + Vector3.up * effectHeightOffset;

        SetVisible(false);
        other.SetVisible(false);

        GameObject effectPrefab = GetEffectPrefab(type);
        GameObject spawnedEffect = null;

        if (effectPrefab != null)
            spawnedEffect = Instantiate(effectPrefab, effectPos, Quaternion.identity);

        yield return new WaitForSeconds(interactionDuration);

        if (spawnedEffect != null)
            Destroy(spawnedEffect);

        SetVisible(true);
        other.SetVisible(true);

        ResumeAgent(agent);
        ResumeAgent(other.agent);

        StartCoroutine(StartCooldown());
        StartCoroutine(other.StartCooldown());

        isInteracting = false;
        other.isInteracting = false;
    }

    IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
    }

    GameObject GetEffectPrefab(InteractionType type)
    {
        switch (type)
        {
            case InteractionType.Fight:
                return fightEffectPrefab;
            case InteractionType.Love:
                return loveEffectPrefab;
        }

        return null;
    }

    void SetVisible(bool visible)
    {
        for (int i = 0; i < renderersToHide.Length; i++)
        {
            if (renderersToHide[i] != null)
                renderersToHide[i].enabled = visible;
        }
    }

    void StopAgent(NavMeshAgent nav)
    {
        if (nav == null) return;

        nav.isStopped = true;
        nav.ResetPath();
    }

    void ResumeAgent(NavMeshAgent nav)
    {
        if (nav == null) return;

        nav.isStopped = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 1f, 0.35f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(detectHalfWidthX * 2f, detectHalfHeightY * 2f, detectHalfDepthZ * 2f));
    }
}