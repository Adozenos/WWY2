using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("Animation States")]
    public string getHitState = "Get Hit";
    public string dieState = "Die";
    public string sleepState = "Sleep";

    [Header("Timing")]
    public float hitReactionDuration = 0.8f;
    public float sinkDuration = 3f;
    public float sinkDistance = 8f;

    [Header("Hitbox")]
    public bool autoCreateHitbox = true;
    public Vector3 hitboxCenter = new Vector3(0f, 2f, 0f);
    public Vector3 hitboxSize = new Vector3(4f, 4f, 8f);

    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private GroundInvader groundInvader;
    private FlyingInvader flyingInvader;
    private int currentHealth;
    private bool isDead;
    private Coroutine hitReactionRoutine;

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        animator = GetComponentInChildren<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        groundInvader = GetComponent<GroundInvader>();
        flyingInvader = GetComponent<FlyingInvader>();

        EnsureDamageHitbox();
    }

    public void TakeDamage(int damage, bool playGroundHitReaction)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, damage));
        if (currentHealth <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }

        if (playGroundHitReaction)
        {
            if (hitReactionRoutine != null)
                StopCoroutine(hitReactionRoutine);

            hitReactionRoutine = StartCoroutine(GroundHitReactionRoutine());
        }
    }

    private IEnumerator GroundHitReactionRoutine()
    {
        if (groundInvader != null)
            groundInvader.BeginExternalAnimationControl();

        PlayAnimation(getHitState);
        yield return new WaitForSeconds(Mathf.Max(0.05f, hitReactionDuration));

        if (!isDead && groundInvader != null)
            groundInvader.EndExternalAnimationControl();

        hitReactionRoutine = null;
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;

        if (hitReactionRoutine != null)
        {
            StopCoroutine(hitReactionRoutine);
            hitReactionRoutine = null;
        }

        StopMovementAndAi();
        PlayAnimation(dieState);

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.down * Mathf.Max(0f, sinkDistance);
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, sinkDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, endPosition, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        Destroy(gameObject);
    }

    private void StopMovementAndAi()
    {
        if (groundInvader != null)
            groundInvader.enabled = false;

        if (flyingInvader != null)
            flyingInvader.enabled = false;

        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
        }
    }

    private void PlayAnimation(string stateName)
    {
        if (animator != null && !string.IsNullOrEmpty(stateName))
            animator.Play(stateName);
    }

    private void EnsureDamageHitbox()
    {
        if (!autoCreateHitbox || HasUsableHitbox())
            return;

        GameObject hitboxObject = new GameObject("MonsterDamageHitbox");
        hitboxObject.transform.SetParent(transform, false);
        hitboxObject.transform.localPosition = hitboxCenter;
        hitboxObject.transform.localRotation = Quaternion.identity;
        hitboxObject.transform.localScale = Vector3.one;

        BoxCollider hitbox = hitboxObject.AddComponent<BoxCollider>();
        hitbox.isTrigger = true;
        hitbox.size = hitboxSize;
        hitbox.center = Vector3.zero;
    }

    private bool HasUsableHitbox()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider monsterCollider = colliders[i];
            if (monsterCollider != null &&
                monsterCollider.enabled &&
                monsterCollider.gameObject.name == "MonsterDamageHitbox")
            {
                return true;
            }
        }

        return false;
    }
}
