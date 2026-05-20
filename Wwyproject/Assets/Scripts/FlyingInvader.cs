using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FlyingInvader : MonoBehaviour
{
    [Header("Target")]
    public Transform targetWall;
    public float targetSampleRadius = 100f;

    [HideInInspector] public MonsterSpawner spawner;

    [Header("Distances")]
    public float flyForwardDistance = 40f;
    public float hoverDistance = 15f;
    public float groundRunDistance = 10f;

    [Header("Speeds")]
    public float groundRunSpeed = 60f;
    public float glideSpeed = 16f;
    public float flyForwardSpeed = 10f;
    public float rotationSpeed = 4f;

    [Header("Flight")]
    public float flightHeight = 20f;
    public float minGroundClearance = 12f;
    public float takeoffDuration = 1.5f;
    public float takeoffRiseSpeed = 8f;
    public float heightAdjustSpeed = 12f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask = ~0;
    public float obstacleCheckDistance = 35f;
    public float obstacleCheckRadius = 3f;
    public float obstacleClearance = 18f;
    public float groundProbeHeight = 120f;

    [Header("Fireball")]
    public float fireballInterval = 3f;
    public float fireballAnimDuration = 1.2f;

    [Header("Animation States")]
    public string runState = "Run";
    public string takeOffState = "Take Off";
    public string flyGlideState = "Fly Glide";
    public string flyForwardState = "Fly Forward";
    public string flyFloatState = "Fly Float";
    public string flyFireballState = "Fly Fireball Shoot";

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 sampledGroundTarget;
    private bool hasGroundTarget;
    private float stateTimer;
    private float fireballTimer;
    private float fireballStateTimer;
    private Vector3 takeoffStartPos;
    private float takeoffElapsed;
    private string currentAnimationState;
    private bool warnedInvalidPath;
    private bool hasSpeedParameter;

    private enum State { GroundRun, TakeOff, Glide, FlyForward, HoverFire }
    private State currentState;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        CacheAnimatorParameters();

        hasGroundTarget = TryUpdateGroundTarget();

        if (EnsureAgentOnNavMesh() && hasGroundTarget)
        {
            agent.speed = groundRunSpeed;
            SetGroundDestination();
            EnterState(State.GroundRun);
        }
        else
        {
            Debug.LogWarning($"{name} FlyingInvader could not start ground run, so it will take off from its current position.", this);
            EnterState(State.TakeOff);
        }
    }

    void Update()
    {
        if (targetWall == null || animator == null)
            return;

        float planarDistance = GetPlanarDistanceToTarget();

        switch (currentState)
        {
            case State.GroundRun:
                UpdateGroundRun();
                if (stateTimer >= groundRunDistance / Mathf.Max(0.1f, groundRunSpeed))
                    EnterState(State.TakeOff);
                break;

            case State.TakeOff:
                UpdateTakeOff();
                break;

            case State.Glide:
                UpdateScriptedFlight(glideSpeed, flyGlideState);
                if (planarDistance <= flyForwardDistance)
                    EnterState(State.FlyForward);
                break;

            case State.FlyForward:
                UpdateScriptedFlight(flyForwardSpeed, flyForwardState);
                if (planarDistance <= hoverDistance)
                    EnterState(State.HoverFire);
                else if (planarDistance > flyForwardDistance)
                    EnterState(State.Glide);
                break;

            case State.HoverFire:
                UpdateHoverFire();
                break;
        }
    }

    void UpdateGroundRun()
    {
        stateTimer += Time.deltaTime;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            EnterState(State.TakeOff);
            return;
        }

        agent.speed = groundRunSpeed;
        agent.isStopped = false;
        SetAnimatorSpeed(groundRunSpeed);
        SetGroundDestination();
    }

    void UpdateTakeOff()
    {
        StopAgent();
        PlayAnimation(takeOffState);

        takeoffElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(takeoffElapsed / Mathf.Max(0.1f, takeoffDuration));
        float desiredY = Mathf.Max(takeoffStartPos.y + flightHeight, GetDesiredFlightY(transform.position));
        float nextY = Mathf.Lerp(takeoffStartPos.y, desiredY, t);
        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, nextY, takeoffRiseSpeed * Time.deltaTime);
        transform.position = pos;

        FaceFlightTarget();

        if (t >= 1f)
        {
            DisableAgentForFlight();
            EnterState(State.Glide);
        }
    }

    void UpdateScriptedFlight(float speed, string animationState)
    {
        DisableAgentForFlight();
        PlayAnimation(animationState);
        SetAnimatorSpeed(speed);

        Vector3 target = GetFlightTargetPosition();
        Vector3 toTarget = target - transform.position;
        Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);

        if (horizontal.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        float desiredY = GetDesiredFlightY(transform.position);
        Vector3 nextPosition = transform.position;
        Vector3 horizontalStep = horizontal.normalized * speed * Time.deltaTime;
        if (horizontalStep.sqrMagnitude > horizontal.sqrMagnitude)
            horizontalStep = horizontal;

        nextPosition += horizontalStep;
        nextPosition.y = Mathf.MoveTowards(nextPosition.y, desiredY, heightAdjustSpeed * Time.deltaTime);
        transform.position = nextPosition;
    }

    void UpdateHoverFire()
    {
        DisableAgentForFlight();
        FaceFlightTarget();

        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, GetDesiredFlightY(pos), heightAdjustSpeed * Time.deltaTime);
        transform.position = pos;

        if (fireballStateTimer > 0f)
        {
            fireballStateTimer -= Time.deltaTime;
            if (fireballStateTimer <= 0f)
                PlayAnimation(flyFloatState);
        }
        else
        {
            PlayAnimation(flyFloatState);
        }

        fireballTimer -= Time.deltaTime;
        if (fireballTimer <= 0f)
        {
            currentAnimationState = flyFireballState;
            animator.Play(flyFireballState);
            fireballStateTimer = fireballAnimDuration;
            fireballTimer = fireballInterval;
        }
    }

    void EnterState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case State.GroundRun:
                PlayAnimation(runState);
                break;
            case State.TakeOff:
                takeoffStartPos = transform.position;
                takeoffElapsed = 0f;
                PlayAnimation(takeOffState);
                break;
            case State.Glide:
                PlayAnimation(flyGlideState);
                break;
            case State.FlyForward:
                PlayAnimation(flyForwardState);
                break;
            case State.HoverFire:
                fireballTimer = 0.5f;
                fireballStateTimer = 0f;
                PlayAnimation(flyFloatState);
                break;
        }
    }

    bool EnsureAgentOnNavMesh()
    {
        if (agent == null)
            return false;

        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, targetSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        Debug.LogWarning($"{name} FlyingInvader could not find NavMesh within {targetSampleRadius} units of spawn position {transform.position}.", this);
        return false;
    }

    bool TryUpdateGroundTarget()
    {
        if (targetWall == null)
        {
            Debug.LogWarning($"{name} FlyingInvader has no targetWall assigned.", this);
            return false;
        }

        if (!NavMesh.SamplePosition(targetWall.position, out NavMeshHit hit, targetSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name} FlyingInvader could not find a NavMesh target near {targetWall.name} within {targetSampleRadius} units.", targetWall);
            return false;
        }

        sampledGroundTarget = hit.position;
        hasGroundTarget = true;
        warnedInvalidPath = false;
        return true;
    }

    void SetGroundDestination()
    {
        if (!hasGroundTarget || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (!agent.SetDestination(sampledGroundTarget))
        {
            Debug.LogWarning($"{name} FlyingInvader could not set ground destination to {sampledGroundTarget}.", this);
            hasGroundTarget = false;
            return;
        }

        if (!warnedInvalidPath && !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"{name} FlyingInvader ground path to wall target is invalid.", this);
            warnedInvalidPath = true;
        }
    }

    void StopAgent()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    void DisableAgentForFlight()
    {
        if (agent != null && agent.enabled)
            agent.enabled = false;
    }

    Vector3 GetFlightTargetPosition()
    {
        Vector3 target = targetWall.position;
        if (hasGroundTarget)
        {
            target.x = sampledGroundTarget.x;
            target.z = sampledGroundTarget.z;
        }
        return target;
    }

    float GetPlanarDistanceToTarget()
    {
        Vector3 target = GetFlightTargetPosition();
        Vector2 a = new Vector2(transform.position.x, transform.position.z);
        Vector2 b = new Vector2(target.x, target.z);
        return Vector2.Distance(a, b);
    }

    float GetDesiredFlightY(Vector3 fromPosition)
    {
        float desiredY = fromPosition.y;
        Vector3 probeOrigin = fromPosition + Vector3.up * groundProbeHeight;
        if (TryRaycastIgnoringSelf(probeOrigin, Vector3.down, out RaycastHit groundHit, groundProbeHeight * 2f))
            desiredY = Mathf.Max(desiredY, groundHit.point.y + Mathf.Max(flightHeight, minGroundClearance));

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = (GetFlightTargetPosition() - fromPosition);
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.01f)
        {
            forward.Normalize();
            Vector3 origin = fromPosition + Vector3.up * obstacleCheckRadius;
            if (TrySphereCastIgnoringSelf(origin, obstacleCheckRadius, forward, out RaycastHit obstacleHit, obstacleCheckDistance))
            {
                float closeness = 1f - Mathf.Clamp01(obstacleHit.distance / Mathf.Max(0.1f, obstacleCheckDistance));
                float lift = obstacleClearance * (0.4f + closeness);
                desiredY = Mathf.Max(desiredY, obstacleHit.point.y + lift);
            }
        }

        return desiredY;
    }

    bool TryRaycastIgnoringSelf(Vector3 origin, Vector3 direction, out RaycastHit bestHit, float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        return TryGetNearestNonSelfHit(hits, out bestHit);
    }

    bool TrySphereCastIgnoringSelf(Vector3 origin, float radius, Vector3 direction, out RaycastHit bestHit, float distance)
    {
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        return TryGetNearestNonSelfHit(hits, out bestHit);
    }

    bool TryGetNearestNonSelfHit(RaycastHit[] hits, out RaycastHit bestHit)
    {
        bestHit = default;
        bool found = false;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance < bestDistance)
            {
                bestHit = hit;
                bestDistance = hit.distance;
                found = true;
            }
        }

        return found;
    }

    void FaceFlightTarget()
    {
        Vector3 dir = GetFlightTargetPosition() - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
}
