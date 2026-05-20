using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class GroundInvader : MonoBehaviour
{
    [Header("Target")]
    public Transform targetWall;
    public float targetSampleRadius = 100f;

    [HideInInspector] public MonsterSpawner spawner;

    [Header("Distances")]
    public float runDistance = 30f;
    public float walkDistance = 10f;
    public float attackRange = 3f;

    [Header("Speeds")]
    public float runSpeed = 80f;
    public float walkSpeed = 3f;

    [Header("Animation States")]
    public string runState = "Run";
    public string walkState = "Walk";
    public string screamState = "Scream";
    public string[] attackStates = { "Basic Attack", "Claw Attack", "Horn Attack" };
    public float attackAnimDuration = 1.5f;

    [Header("Player Detection")]
    public float playerDetectRange = 15f;
    public string playerTag = "Player";
    public float screamDuration = 3f;
    public float screamCooldown = 8f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private Vector3 sampledTargetPosition;
    private bool hasValidTarget;
    private bool warnedInvalidPath;
    private bool useDirectMovement;
    private int attackIndex;
    private float stateTimer;
    private bool isScreaming;
    private string currentAnimationState;
    private bool hasSpeedParameter;
    private Vector3 currentDestination;
    private bool destinationSet;
    private float lastScreamTime = -999f;
    private bool externalAnimationControl;

    private enum State { Run, Walk, Attack }
    private State currentState;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();

        useDirectMovement = !EnsureAgentOnNavMesh();

        hasValidTarget = TryUpdateTargetPosition();
        if (!hasValidTarget)
            return;

        FindPlayer();

        EnterState(State.Run);
    }

    void Update()
    {
        if (externalAnimationControl)
            return;

        if (targetWall == null) return;
        if (animator == null) return;
        if (player == null)
            FindPlayer();
        if (!useDirectMovement && !agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name} GroundInvader is not on NavMesh, switching to direct movement fallback.", this);
            useDirectMovement = true;
        }
        if (!hasValidTarget && !TryUpdateTargetPosition())
            return;

        float dist = GetDistanceToTarget();

        if (currentState == State.Run && !isScreaming && player != null && Time.time >= lastScreamTime + screamCooldown)
        {
            float playerDist = Vector3.Distance(transform.position, player.position);
            if (playerDist <= playerDetectRange)
            {
                StartCoroutine(DoScream());
                return;
            }
        }

        if (isScreaming) return;

        switch (currentState)
        {
            case State.Run:
                MoveTowardTarget(runSpeed);
                SetAnimatorSpeed(runSpeed);
                if (dist <= attackRange)
                    EnterState(State.Attack);
                else if (dist <= runDistance)
                    EnterState(State.Walk);
                break;

            case State.Walk:
                MoveTowardTarget(walkSpeed);
                SetAnimatorSpeed(walkSpeed);
                if (dist <= attackRange)
                    EnterState(State.Attack);
                else if (dist > runDistance)
                    EnterState(State.Run);
                break;

            case State.Attack:
                StopMovement();
                Vector3 dir = sampledTargetPosition - transform.position;
                dir.y = 0;
                if (dir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(dir);
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    string atkState = attackStates[attackIndex % attackStates.Length];
                    currentAnimationState = atkState;
                    animator.Play(atkState);
                    attackIndex++;
                    stateTimer = attackAnimDuration;
                }
                if (dist > attackRange + 2f)
                    EnterState(State.Walk);
                break;
        }
    }

    public void BeginExternalAnimationControl()
    {
        externalAnimationControl = true;
        isScreaming = false;
        StopMovement();
    }

    public void EndExternalAnimationControl()
    {
        externalAnimationControl = false;
        currentAnimationState = null;

        if (!useDirectMovement && agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        EnterState(currentState);
    }

    void EnterState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case State.Run:
                PlayAnimation(runState);
                SetDestinationToTarget(true);
                break;
            case State.Walk:
                PlayAnimation(walkState);
                SetDestinationToTarget(true);
                break;
            case State.Attack:
                StopMovement();
                break;
        }
    }

    IEnumerator DoScream()
    {
        isScreaming = true;
        lastScreamTime = Time.time;
        StopMovement();
        if (player != null)
            FaceTowards(player.position);
        PlayAnimation(screamState);
        yield return new WaitForSeconds(screamDuration);
        isScreaming = false;
        if (!useDirectMovement && agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
        SetDestinationToTarget(true);
        EnterState(State.Run);
    }

    void FindPlayer()
    {
        if (string.IsNullOrEmpty(playerTag))
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            player = playerObj.transform;
    }

    bool EnsureAgentOnNavMesh()
    {
        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, targetSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        Debug.LogWarning($"{name} GroundInvader could not find NavMesh within {targetSampleRadius} units of spawn position {transform.position}. Direct movement fallback will be used.", this);
        return false;
    }

    bool TryUpdateTargetPosition()
    {
        if (targetWall == null)
        {
            Debug.LogWarning($"{name} GroundInvader has no targetWall assigned.", this);
            return false;
        }

        if (!NavMesh.SamplePosition(targetWall.position, out NavMeshHit hit, targetSampleRadius, NavMesh.AllAreas))
        {
            sampledTargetPosition = targetWall.position;
            hasValidTarget = true;
            useDirectMovement = true;
            Debug.LogWarning($"{name} GroundInvader could not find a NavMesh target near {targetWall.name} within {targetSampleRadius} units. Using direct movement toward the raw target position.", targetWall);
            return true;
        }

        sampledTargetPosition = hit.position;
        hasValidTarget = true;
        warnedInvalidPath = false;
        SetDestinationToTarget(true);
        return true;
    }

    void SetDestinationToTarget(bool force = false)
    {
        if (useDirectMovement || !hasValidTarget || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (!force && destinationSet && agent.hasPath && (currentDestination - sampledTargetPosition).sqrMagnitude <= 0.01f)
            return;

        if (!agent.SetDestination(sampledTargetPosition))
        {
            Debug.LogWarning($"{name} GroundInvader could not set destination to sampled wall target {sampledTargetPosition}.", this);
            hasValidTarget = false;
            destinationSet = false;
            return;
        }

        currentDestination = sampledTargetPosition;
        destinationSet = true;

        if (!warnedInvalidPath && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            SwitchToDirectMovementAfterInvalidPath();
        }
    }

    void MoveTowardTarget(float speed)
    {
        if (useDirectMovement)
        {
            MoveDirectly(speed);
            return;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            useDirectMovement = true;
            MoveDirectly(speed);
            return;
        }

        agent.speed = speed;
        agent.isStopped = false;
        SetDestinationToTarget();
        ValidateCurrentPath();
    }

    void ValidateCurrentPath()
    {
        if (useDirectMovement || warnedInvalidPath || agent == null || !agent.enabled || !agent.isOnNavMesh || agent.pathPending)
            return;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            SwitchToDirectMovementAfterInvalidPath();
    }

    void SwitchToDirectMovementAfterInvalidPath()
    {
        Debug.LogWarning($"{name} GroundInvader path to wall target is invalid.", this);
        warnedInvalidPath = true;
        useDirectMovement = true;
        destinationSet = false;
    }

    void MoveDirectly(float speed)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        Vector3 target = sampledTargetPosition;
        target.y = transform.position.y;
        Vector3 toTarget = target - transform.position;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    void StopMovement()
    {
        if (!useDirectMovement && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void FaceTowards(Vector3 worldPosition)
    {
        Vector3 dir = worldPosition - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    float GetDistanceToTarget()
    {
        if (!useDirectMovement && agent != null && agent.enabled && agent.isOnNavMesh && agent.pathPending)
            return float.PositiveInfinity;

        if (!useDirectMovement && agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath)
            return agent.remainingDistance;

        Vector3 target = sampledTargetPosition;
        target.y = transform.position.y;
        return Vector3.Distance(transform.position, target);
    }

    void PlayAnimation(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName) || currentAnimationState == stateName)
            return;

        currentAnimationState = stateName;
        animator.Play(stateName);
    }

    void CacheAnimatorParameters()
    {
        hasSpeedParameter = false;
        if (animator == null)
            return;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == "Speed" && parameters[i].type == AnimatorControllerParameterType.Float)
            {
                hasSpeedParameter = true;
                return;
            }
        }
    }

    void SetAnimatorSpeed(float speed)
    {
        if (hasSpeedParameter)
            animator.SetFloat("Speed", speed);
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.OnMonsterDestroyed();
            spawner = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectRange);
    }
}
