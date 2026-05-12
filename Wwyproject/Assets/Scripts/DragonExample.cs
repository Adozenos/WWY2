using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class DragonExample : MonoBehaviour
{
    private enum DragonRideState
    {
        Unmounted,
        MountedGroundIdle,
        MountedGroundMove,
        MountedTakeOff,
        MountedAirMove,
        MountedLanding
    }

    private enum UnmountedWakeAnimation
    {
        Sleep,
        Idle,
        Scream
    }

    [Header("Mount")]
    [SerializeField] private float wakeDistance = 18f;
    [SerializeField] private float mountDistance = 6f;
    [SerializeField] private Vector3 mountOffset = new Vector3(0f, 2.5f, -1.5f);
    [SerializeField] private Vector3 dismountOffset = new Vector3(2f, 0f, 0f);

    [Header("Movement")]
    [SerializeField] private float groundMoveSpeed = 8f;
    [SerializeField] private float runMoveSpeed = 14f;
    [SerializeField] private float airMoveSpeed = 16f;
    [SerializeField] private float airGlideMoveSpeed = 24f;
    [SerializeField] private float yawSpeed = 120f;
    [SerializeField] private float groundBrake = 12f;

    [Header("Flight")]
    [SerializeField] private float takeoffRiseHeight = 8f;
    [SerializeField] private float takeoffDuration = 1.2f;
    [SerializeField] private float takeoffToHoverNormalizedTime = 0.9f;
    [SerializeField] private float pitchSpeed = 80f;
    [SerializeField] private float maxPitchAngle = 45f;
    [SerializeField] private float landingDescendSpeed = 8f;
    [SerializeField] private float landingGroundEpsilon = 0.05f;
    [SerializeField] private float groundCheckDistance = 2.5f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("SoulEater Animation States")]
    [SerializeField] private string sleepState = "Sleep";
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string screamState = "Scream";
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string runState = "Run";
    [SerializeField] private string takeOffState = "Take Off";
    [SerializeField] private string flyForwardState = "Fly Forward";
    [SerializeField] private string flyGlideState = "Fly Glide";
    [SerializeField] private string flyFloatState = "Fly Float";
    [SerializeField] private string landState = "Land";
    [SerializeField] private string defendState = "Defend";
    [SerializeField] private string flyFireballState = "Fly Fireball Shoot";
    [SerializeField] private string[] groundAttackStates = { "Basic Attack", "Tail Attack", "Fireball Shoot" };
    [SerializeField] private float animationBlendTime = 0.12f;
    [SerializeField] private int idleLoopsBeforeScream = 2;
    [SerializeField] private float screamReturnNormalizedTime = 0.95f;
    [SerializeField] private float attackLockDuration = 1.1f;
    [SerializeField] private float defendRefreshInterval = 0.25f;

    [Header("Rider")]
    [SerializeField] private Transform riderRootOverride;
    [SerializeField] private string riderObjectName = "FPSController";

    [Header("Third Person Camera")]
    [SerializeField] private Camera mainRideCameraOverride;
    [SerializeField] private Camera firstPersonCameraOverride;
    [SerializeField] private string mainRideCameraName = "long";
    [SerializeField] private Vector3 followCameraOffset = new Vector3(0f, 3.2f, -8f);
    [SerializeField] private Vector3 followLookAtOffset = new Vector3(0f, 1.8f, 2.5f);
    [SerializeField] private float followSmooth = 8f;

    private const string PreferredRideCameraName = "long";

    private Animator anim;
    private Rigidbody dragonRigidbody;
    private DragonRideState state;
    private bool stateInitialized;

    private Transform riderRoot;
    private Transform riderOriginalParent;
    private FirstPersonController firstPersonController;
    private CharacterController characterController;
    private Rigidbody riderRigidbody;
    private bool riderOriginalUseGravity;
    private bool riderOriginalIsKinematic;
    private bool riderMissingLogged;

    private Camera mainRideCamera;
    private Camera firstPersonCamera;
    private AudioListener mainRideAudioListener;
    private AudioListener firstPersonAudioListener;
    private bool mountedCameraMode;
    private bool cameraSnapped;
    private bool rideCameraPoseCaptured;
    private Vector3 rideCameraLocalOffset;
    private Quaternion rideCameraLocalRotation;
    private Transform mainRideCameraOriginalParent;
    private bool mainRideCameraOriginalParentCaptured;

    private float groundY;
    private Vector3 takeoffStartPosition;
    private Quaternion takeoffStartRotation;
    private float takeoffElapsed;
    private float landingElapsed;
    private bool dragonRigidbodyOriginalUseGravity;
    private bool dragonRigidbodyOriginalIsKinematic;
    private RigidbodyConstraints dragonRigidbodyOriginalConstraints;

    private string currentAnimationState;
    private UnmountedWakeAnimation unmountedWakeAnimation = UnmountedWakeAnimation.Sleep;
    private float actionLockedUntil;
    private float nextDefendRefreshTime;
    private int groundAttackIndex;

    private void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("DragonExample requires an Animator on the same GameObject.", this);
            enabled = false;
            return;
        }

        ResolveDragonRigidbody();
        ResolveRiderReferences();
        ResolveCameraReferences();
        CaptureRideCameraPoseFromScene();

        groundY = transform.position.y;
        EnterState(DragonRideState.Unmounted);
        SetMountedCameraMode(false, true);
    }

    private void Update()
    {
        HandleMountInput();

        if (IsMounted())
        {
            HandleMountedRotation();
        }

        switch (state)
        {
            case DragonRideState.Unmounted:
                UpdateUnmounted();
                break;
            case DragonRideState.MountedGroundIdle:
                UpdateMountedGroundIdle();
                break;
            case DragonRideState.MountedGroundMove:
                UpdateMountedGroundMove();
                break;
            case DragonRideState.MountedTakeOff:
                UpdateMountedTakeOff();
                break;
            case DragonRideState.MountedAirMove:
                UpdateMountedAirMove();
                break;
            case DragonRideState.MountedLanding:
                UpdateMountedLanding();
                break;
        }
    }

    private void LateUpdate()
    {
        UpdateRideCameraFollow();
    }

    private void HandleMountInput()
    {
        if (state == DragonRideState.Unmounted && Input.GetKeyDown(KeyCode.F))
        {
            TryMount();
        }
    }

    private void ResolveRiderReferences()
    {
        if (riderRootOverride != null)
        {
            riderRoot = riderRootOverride;
        }
        else if (riderRoot == null && !string.IsNullOrEmpty(riderObjectName))
        {
            GameObject riderObject = GameObject.Find(riderObjectName);
            if (riderObject != null)
            {
                riderRoot = riderObject.transform;
            }
        }

        if (riderRoot == null)
        {
            return;
        }

        firstPersonController ??= riderRoot.GetComponent<FirstPersonController>();
        characterController ??= riderRoot.GetComponent<CharacterController>();
        riderRigidbody ??= riderRoot.GetComponent<Rigidbody>();

        if (firstPersonCamera == null)
        {
            firstPersonCamera = firstPersonCameraOverride != null
                ? firstPersonCameraOverride
                : riderRoot.GetComponentInChildren<Camera>(true);
        }

        if (firstPersonAudioListener == null && firstPersonCamera != null)
        {
            firstPersonAudioListener = firstPersonCamera.GetComponent<AudioListener>();
        }
    }

    private void ResolveCameraReferences()
    {
        if (mainRideCamera == null)
        {
            if (mainRideCameraOverride != null)
            {
                mainRideCamera = mainRideCameraOverride;
            }
            else
            {
                GameObject preferredCameraObject = GameObject.Find(PreferredRideCameraName);
                if (preferredCameraObject != null)
                {
                    mainRideCamera = preferredCameraObject.GetComponent<Camera>();
                }

                if (mainRideCamera == null && !string.IsNullOrEmpty(mainRideCameraName))
                {
                    GameObject mainCameraObject = GameObject.Find(mainRideCameraName);
                    if (mainCameraObject != null)
                    {
                        mainRideCamera = mainCameraObject.GetComponent<Camera>();
                    }
                }
            }
        }

        if (mainRideAudioListener == null && mainRideCamera != null)
        {
            mainRideAudioListener = mainRideCamera.GetComponent<AudioListener>();
        }

        if (firstPersonCamera == null && firstPersonCameraOverride != null)
        {
            firstPersonCamera = firstPersonCameraOverride;
        }

        if (firstPersonAudioListener == null && firstPersonCamera != null)
        {
            firstPersonAudioListener = firstPersonCamera.GetComponent<AudioListener>();
        }
    }

    private void CaptureRideCameraPoseFromScene()
    {
        if (mainRideCamera == null)
        {
            return;
        }

        rideCameraLocalOffset = transform.InverseTransformPoint(mainRideCamera.transform.position);
        rideCameraLocalRotation = Quaternion.Inverse(transform.rotation) * mainRideCamera.transform.rotation;
        rideCameraPoseCaptured = true;
    }

    private void AttachRideCameraToDragon()
    {
        if (mainRideCamera == null)
        {
            return;
        }

        if (!mainRideCameraOriginalParentCaptured)
        {
            mainRideCameraOriginalParent = mainRideCamera.transform.parent;
            mainRideCameraOriginalParentCaptured = true;
        }

        mainRideCamera.transform.SetParent(transform, true);

        if (rideCameraPoseCaptured)
        {
            mainRideCamera.transform.localPosition = rideCameraLocalOffset;
            mainRideCamera.transform.localRotation = rideCameraLocalRotation;
        }
    }

    private void DetachRideCameraFromDragon()
    {
        if (mainRideCamera == null || !mainRideCameraOriginalParentCaptured)
        {
            return;
        }

        mainRideCamera.transform.SetParent(mainRideCameraOriginalParent, true);
    }

    private bool IsMounted()
    {
        return state != DragonRideState.Unmounted;
    }

    private void TryMount()
    {
        ResolveRiderReferences();
        ResolveCameraReferences();
        CaptureRideCameraPoseFromScene();
        ResolveDragonRigidbody();

        if (riderRoot == null)
        {
            if (!riderMissingLogged)
            {
                Debug.LogWarning("DragonExample could not find rider object. Assign riderRootOverride or keep riderObjectName as FPSController.", this);
                riderMissingLogged = true;
            }

            return;
        }

        if (Vector3.Distance(riderRoot.position, transform.position) > mountDistance)
        {
            return;
        }

        riderOriginalParent = riderRoot.parent;

        if (riderRigidbody != null)
        {
            riderOriginalUseGravity = riderRigidbody.useGravity;
            riderOriginalIsKinematic = riderRigidbody.isKinematic;
            riderRigidbody.velocity = Vector3.zero;
            riderRigidbody.angularVelocity = Vector3.zero;
            riderRigidbody.useGravity = false;
            riderRigidbody.isKinematic = true;
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = false;
        }

        if (characterController != null && characterController.enabled)
        {
            characterController.enabled = false;
        }

        riderRoot.SetParent(transform, false);
        riderRoot.localPosition = mountOffset;
        riderRoot.localRotation = Quaternion.identity;

        groundY = transform.position.y;
        ConfigureMountedRigidbody(true);
        SetMountedCameraMode(true, true);
        EnterState(DragonRideState.MountedGroundIdle);
    }

    private void Dismount()
    {
        if (state != DragonRideState.MountedGroundIdle && state != DragonRideState.MountedGroundMove)
        {
            return;
        }

        if (riderRoot != null)
        {
            Vector3 dismountWorldPosition = transform.TransformPoint(dismountOffset);

            if (characterController != null && characterController.enabled)
            {
                characterController.enabled = false;
            }

            riderRoot.SetParent(riderOriginalParent, true);
            riderRoot.position = dismountWorldPosition;
            riderRoot.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        if (riderRigidbody != null)
        {
            riderRigidbody.isKinematic = riderOriginalIsKinematic;
            riderRigidbody.useGravity = riderOriginalUseGravity;
            riderRigidbody.velocity = Vector3.zero;
            riderRigidbody.angularVelocity = Vector3.zero;
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

        ConfigureMountedRigidbody(false);
        SetMountedCameraMode(false, true);
        EnterState(DragonRideState.Unmounted);
    }

    private void HandleMountedRotation()
    {
        float yawInput = Input.GetAxis("Mouse X");
        float pitchInput = state == DragonRideState.MountedAirMove ? Input.GetAxis("Mouse Y") : 0f;
        if (Mathf.Abs(yawInput) < 0.0001f && Mathf.Abs(pitchInput) < 0.0001f)
        {
            return;
        }

        Vector3 euler = transform.eulerAngles;
        float pitch = NormalizeAngle(euler.x);
        float yaw = euler.y + yawInput * yawSpeed * Time.deltaTime;

        if (state == DragonRideState.MountedAirMove)
        {
            pitch = Mathf.Clamp(pitch - pitchInput * pitchSpeed * Time.deltaTime, -maxPitchAngle, maxPitchAngle);
        }
        else
        {
            pitch = Mathf.MoveTowards(pitch, 0f, pitchSpeed * Time.deltaTime);
        }

        MoveDragonRotation(Quaternion.Euler(pitch, yaw, 0f));
    }

    private void UpdateUnmounted()
    {
        ResolveRiderReferences();
        bool riderIsNear = riderRoot != null && Vector3.Distance(riderRoot.position, transform.position) <= wakeDistance;
        if (!riderIsNear)
        {
            SetUnmountedWakeAnimation(UnmountedWakeAnimation.Sleep);
            return;
        }

        UpdateUnmountedWakeLoop();
    }

    private void UpdateUnmountedWakeLoop()
    {
        if (unmountedWakeAnimation == UnmountedWakeAnimation.Sleep)
        {
            SetUnmountedWakeAnimation(UnmountedWakeAnimation.Idle);
            return;
        }

        if (unmountedWakeAnimation == UnmountedWakeAnimation.Idle &&
            GetCurrentStateNormalizedTime(idleState) >= Mathf.Max(1, idleLoopsBeforeScream))
        {
            SetUnmountedWakeAnimation(UnmountedWakeAnimation.Scream);
            return;
        }

        if (unmountedWakeAnimation == UnmountedWakeAnimation.Scream &&
            GetCurrentStateNormalizedTime(screamState) >= screamReturnNormalizedTime)
        {
            SetUnmountedWakeAnimation(UnmountedWakeAnimation.Idle);
        }
    }

    private void SetUnmountedWakeAnimation(UnmountedWakeAnimation wakeAnimation)
    {
        string stateName = sleepState;
        switch (wakeAnimation)
        {
            case UnmountedWakeAnimation.Sleep:
                stateName = sleepState;
                break;
            case UnmountedWakeAnimation.Idle:
                stateName = idleState;
                break;
            case UnmountedWakeAnimation.Scream:
                stateName = screamState;
                break;
        }

        if (unmountedWakeAnimation == wakeAnimation && currentAnimationState == stateName)
        {
            return;
        }

        unmountedWakeAnimation = wakeAnimation;
        PlayAnimationState(stateName, true);
    }

    private void UpdateMountedGroundIdle()
    {
        BrakeHorizontalVelocity(groundBrake);

        if (HandleGroundActionInput())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            Dismount();
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            BeginTakeOff();
            return;
        }

        if (Input.GetKey(KeyCode.W))
        {
            EnterState(DragonRideState.MountedGroundMove);
            return;
        }

        PlayLocomotionState(idleState);
    }

    private void UpdateMountedGroundMove()
    {
        if (HandleGroundActionInput())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            Dismount();
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            BeginTakeOff();
            return;
        }

        if (!Input.GetKey(KeyCode.W))
        {
            EnterState(DragonRideState.MountedGroundIdle);
            return;
        }

        bool running = IsShiftHeld();
        MoveForward(running ? runMoveSpeed : groundMoveSpeed, true);
        PlayLocomotionState(running ? runState : walkState);
    }

    private void BeginTakeOff()
    {
        groundY = transform.position.y;
        EnterState(DragonRideState.MountedTakeOff);
    }

    private void UpdateMountedTakeOff()
    {
        takeoffElapsed += Time.deltaTime;
        float duration = Mathf.Max(0.01f, takeoffDuration);
        float t = Mathf.Clamp01(takeoffElapsed / duration);
        float smoothT = SmoothStep01(t);

        Vector3 nextPosition = takeoffStartPosition;
        nextPosition.y = Mathf.Lerp(takeoffStartPosition.y, groundY + takeoffRiseHeight, smoothT);
        MoveDragonPosition(nextPosition);
        MoveDragonRotation(Quaternion.Slerp(takeoffStartRotation, Quaternion.Euler(0f, takeoffStartRotation.eulerAngles.y, 0f), smoothT));

        bool takeOffAnimDone = IsCurrentStateDone(takeOffState, takeoffToHoverNormalizedTime);
        if (takeOffAnimDone || t >= 1f)
        {
            EnterState(DragonRideState.MountedAirMove);
        }
    }

    private void UpdateMountedAirMove()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            BeginLanding();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            PlayActionState(flyFireballState, attackLockDuration);
        }

        if (Input.GetMouseButton(1))
        {
            RefreshDefendAction();
        }

        if (Input.GetKey(KeyCode.W))
        {
            bool gliding = IsShiftHeld();
            MoveForward(gliding ? airGlideMoveSpeed : airMoveSpeed, false);
            PlayLocomotionState(gliding ? flyGlideState : flyForwardState);
            return;
        }

        StopDragonImmediately();
        PlayLocomotionState(flyFloatState);
    }

    private void BeginLanding()
    {
        EnterState(DragonRideState.MountedLanding);
    }

    private void UpdateMountedLanding()
    {
        landingElapsed += Time.deltaTime;
        SetDragonVelocity(Vector3.down * landingDescendSpeed);

        if (IsGrounded(out RaycastHit hit))
        {
            Vector3 groundedPosition = transform.position;
            groundedPosition.y = Mathf.Max(hit.point.y + landingGroundEpsilon, transform.position.y);
            MoveDragonPosition(groundedPosition);
            groundY = transform.position.y;
            SetDragonVelocity(Vector3.zero);
            EnterState(DragonRideState.MountedGroundIdle);
        }
    }

    private bool HandleGroundActionInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            string attackState = GetNextGroundAttackState();
            PlayActionState(attackState, attackLockDuration);
            return true;
        }

        if (Input.GetMouseButton(1))
        {
            RefreshDefendAction();
            return true;
        }

        return Time.time < actionLockedUntil;
    }

    private string GetNextGroundAttackState()
    {
        if (groundAttackStates == null || groundAttackStates.Length == 0)
        {
            return "Basic Attack";
        }

        string attackState = groundAttackStates[groundAttackIndex % groundAttackStates.Length];
        groundAttackIndex++;
        return attackState;
    }

    private void RefreshDefendAction()
    {
        actionLockedUntil = Time.time + defendRefreshInterval;
        if (Time.time >= nextDefendRefreshTime)
        {
            PlayAnimationState(defendState, true);
            nextDefendRefreshTime = Time.time + defendRefreshInterval;
        }
    }

    private void PlayActionState(string stateName, float lockDuration)
    {
        actionLockedUntil = Time.time + Mathf.Max(0.01f, lockDuration);
        PlayAnimationState(stateName, true);
    }

    private void PlayLocomotionState(string stateName, bool force = false)
    {
        if (!force && Time.time < actionLockedUntil)
        {
            return;
        }

        PlayAnimationState(stateName, force);
    }

    private void PlayAnimationState(string stateName, bool force = false)
    {
        if (anim == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        if (!force && currentAnimationState == stateName)
        {
            return;
        }

        if (!AnimatorHasState(stateName))
        {
            Debug.LogWarning($"Dragon animator does not have state '{stateName}'.", this);
            return;
        }

        anim.CrossFadeInFixedTime(stateName, animationBlendTime, 0, 0f);
        currentAnimationState = stateName;
    }

    private bool AnimatorHasState(string stateName)
    {
        int shortHash = Animator.StringToHash(stateName);
        int fullHash = Animator.StringToHash($"Base Layer.{stateName}");
        return anim.HasState(0, shortHash) || anim.HasState(0, fullHash);
    }

    private bool IsCurrentStateDone(string stateName, float normalizedTime)
    {
        if (anim == null)
        {
            return false;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        return (stateInfo.IsName(stateName) || stateInfo.IsName($"Base Layer.{stateName}"))
            && stateInfo.normalizedTime >= normalizedTime;
    }

    private float GetCurrentStateNormalizedTime(string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName))
        {
            return 0f;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(stateName) && !stateInfo.IsName($"Base Layer.{stateName}"))
        {
            return 0f;
        }

        return stateInfo.normalizedTime;
    }

    private void MoveForward(float speed, bool lockToGround)
    {
        Vector3 forward = transform.forward;
        if (lockToGround)
        {
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        forward.Normalize();
        Vector3 desiredVelocity = forward * speed;

        if (lockToGround)
        {
            desiredVelocity.y = dragonRigidbody != null ? dragonRigidbody.velocity.y : 0f;
        }

        SetDragonVelocity(desiredVelocity);
    }

    private void KeepDragonAtGroundY()
    {
        if (IsGrounded(out RaycastHit hit))
        {
            Vector3 groundedPosition = transform.position;
            groundedPosition.y = Mathf.Max(hit.point.y + landingGroundEpsilon, transform.position.y);
            MoveDragonPosition(groundedPosition);
            groundY = transform.position.y;
        }
    }

    private void EnterState(DragonRideState newState)
    {
        if (stateInitialized && state == newState)
        {
            return;
        }

        state = newState;
        stateInitialized = true;
        actionLockedUntil = 0f;

        switch (state)
        {
            case DragonRideState.Unmounted:
                SetDragonVelocity(Vector3.zero);
                currentAnimationState = null;
                unmountedWakeAnimation = UnmountedWakeAnimation.Sleep;
                break;
            case DragonRideState.MountedGroundIdle:
                SetMountedRotationConstraints(false);
                LevelDragonPitchAndRoll();
                SetRigidbodyGravity(true);
                KeepDragonAtGroundY();
                PlayAnimationState(idleState, true);
                break;
            case DragonRideState.MountedGroundMove:
                SetMountedRotationConstraints(false);
                LevelDragonPitchAndRoll();
                SetRigidbodyGravity(true);
                KeepDragonAtGroundY();
                break;
            case DragonRideState.MountedTakeOff:
                SetMountedRotationConstraints(false);
                LevelDragonPitchAndRoll();
                SetRigidbodyGravity(false);
                SetDragonVelocity(Vector3.zero);
                takeoffElapsed = 0f;
                takeoffStartPosition = transform.position;
                takeoffStartRotation = transform.rotation;
                PlayAnimationState(takeOffState, true);
                break;
            case DragonRideState.MountedAirMove:
                SetMountedRotationConstraints(true);
                SetRigidbodyGravity(false);
                SetDragonVelocity(Vector3.zero);
                PlayAnimationState(flyFloatState, true);
                break;
            case DragonRideState.MountedLanding:
                SetMountedRotationConstraints(false);
                LevelDragonPitchAndRoll();
                SetRigidbodyGravity(false);
                landingElapsed = 0f;
                PlayAnimationState(landState, true);
                break;
        }
    }

    private void SetMountedCameraMode(bool mounted, bool snapImmediately)
    {
        ResolveCameraReferences();

        mountedCameraMode = mounted;
        cameraSnapped = false;

        bool hasFirstPersonCamera = firstPersonCamera != null;
        bool hasMainRideCamera = mainRideCamera != null;

        if (hasMainRideCamera)
        {
            bool enableMainRide = mounted || !hasFirstPersonCamera;
            mainRideCamera.enabled = enableMainRide;
            if (mainRideAudioListener != null)
            {
                mainRideAudioListener.enabled = enableMainRide;
            }
        }

        if (hasFirstPersonCamera)
        {
            bool enableFirstPerson = !mounted;
            firstPersonCamera.enabled = enableFirstPerson;
            if (firstPersonAudioListener != null)
            {
                firstPersonAudioListener.enabled = enableFirstPerson;
            }
        }

        if (mounted)
        {
            if (!rideCameraPoseCaptured)
            {
                CaptureRideCameraPoseFromScene();
            }

            AttachRideCameraToDragon();
        }
        else
        {
            DetachRideCameraFromDragon();
        }

        if (mounted && snapImmediately)
        {
            SnapRideCamera();
        }
    }

    private void UpdateRideCameraFollow()
    {
        if (!mountedCameraMode || mainRideCamera == null)
        {
            return;
        }

        if (!cameraSnapped)
        {
            SnapRideCamera();
        }

        if (mainRideCamera.transform.parent == transform && rideCameraPoseCaptured)
        {
            mainRideCamera.transform.localPosition = rideCameraLocalOffset;
            mainRideCamera.transform.localRotation = rideCameraLocalRotation;
            return;
        }

        if (rideCameraPoseCaptured)
        {
            mainRideCamera.transform.position = transform.TransformPoint(rideCameraLocalOffset);
            mainRideCamera.transform.rotation = transform.rotation * rideCameraLocalRotation;
            return;
        }

        Vector3 desiredPosition = transform.TransformPoint(followCameraOffset);
        float smoothFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSmooth) * Time.deltaTime);
        mainRideCamera.transform.position = Vector3.Lerp(mainRideCamera.transform.position, desiredPosition, smoothFactor);

        Vector3 lookTarget = transform.TransformPoint(followLookAtOffset);
        Vector3 lookDirection = lookTarget - mainRideCamera.transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            mainRideCamera.transform.rotation = Quaternion.Slerp(mainRideCamera.transform.rotation, desiredRotation, smoothFactor);
        }
    }

    private void SnapRideCamera()
    {
        if (mainRideCamera == null)
        {
            return;
        }

        if (mainRideCamera.transform.parent == transform && rideCameraPoseCaptured)
        {
            mainRideCamera.transform.localPosition = rideCameraLocalOffset;
            mainRideCamera.transform.localRotation = rideCameraLocalRotation;
            cameraSnapped = true;
            return;
        }

        if (rideCameraPoseCaptured)
        {
            mainRideCamera.transform.position = transform.TransformPoint(rideCameraLocalOffset);
            mainRideCamera.transform.rotation = transform.rotation * rideCameraLocalRotation;
            cameraSnapped = true;
            return;
        }

        Vector3 desiredPosition = transform.TransformPoint(followCameraOffset);
        mainRideCamera.transform.position = desiredPosition;

        Vector3 lookTarget = transform.TransformPoint(followLookAtOffset);
        Vector3 lookDirection = lookTarget - desiredPosition;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            mainRideCamera.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        cameraSnapped = true;
    }

    private void ResolveDragonRigidbody()
    {
        if (dragonRigidbody != null)
        {
            return;
        }

        dragonRigidbody = GetComponent<Rigidbody>();
        if (dragonRigidbody == null)
        {
            return;
        }

        dragonRigidbodyOriginalUseGravity = dragonRigidbody.useGravity;
        dragonRigidbodyOriginalIsKinematic = dragonRigidbody.isKinematic;
        dragonRigidbodyOriginalConstraints = dragonRigidbody.constraints;
    }

    private void ConfigureMountedRigidbody(bool mounted)
    {
        ResolveDragonRigidbody();
        if (dragonRigidbody == null)
        {
            return;
        }

        if (mounted)
        {
            dragonRigidbody.isKinematic = false;
            dragonRigidbody.useGravity = true;
            dragonRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            dragonRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            SetMountedRotationConstraints(false);
            dragonRigidbody.velocity = Vector3.zero;
            dragonRigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            dragonRigidbody.velocity = Vector3.zero;
            dragonRigidbody.angularVelocity = Vector3.zero;
            dragonRigidbody.useGravity = dragonRigidbodyOriginalUseGravity;
            dragonRigidbody.isKinematic = dragonRigidbodyOriginalIsKinematic;
            dragonRigidbody.constraints = dragonRigidbodyOriginalConstraints;
        }
    }

    private void SetMountedRotationConstraints(bool allowPitch)
    {
        if (dragonRigidbody == null)
        {
            return;
        }

        dragonRigidbody.constraints = allowPitch
            ? RigidbodyConstraints.FreezeRotationZ
            : RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void LevelDragonPitchAndRoll()
    {
        Vector3 euler = transform.eulerAngles;
        MoveDragonRotation(Quaternion.Euler(0f, euler.y, 0f));
    }

    private void SetRigidbodyGravity(bool useGravity)
    {
        if (dragonRigidbody != null)
        {
            dragonRigidbody.useGravity = useGravity;
        }
    }

    private void SetDragonVelocity(Vector3 velocity)
    {
        if (dragonRigidbody != null)
        {
            if (dragonRigidbody.isKinematic)
            {
                return;
            }

            dragonRigidbody.velocity = velocity;
            return;
        }

        transform.position += velocity * Time.deltaTime;
    }

    private void MoveDragonPosition(Vector3 position)
    {
        if (dragonRigidbody != null)
        {
            dragonRigidbody.MovePosition(position);
            return;
        }

        transform.position = position;
    }

    private void MoveDragonRotation(Quaternion rotation)
    {
        if (dragonRigidbody != null)
        {
            dragonRigidbody.MoveRotation(rotation);
            return;
        }

        transform.rotation = rotation;
    }

    private void BrakeHorizontalVelocity(float brakeStrength)
    {
        if (dragonRigidbody == null)
        {
            return;
        }

        Vector3 velocity = dragonRigidbody.velocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, brakeStrength * Time.deltaTime);
        dragonRigidbody.velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
    }

    private void StopDragonImmediately()
    {
        if (dragonRigidbody == null)
        {
            return;
        }

        dragonRigidbody.velocity = Vector3.zero;
        dragonRigidbody.angularVelocity = Vector3.zero;
    }

    private bool IsGrounded(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        return Physics.Raycast(origin, Vector3.down, out hit, Mathf.Max(0.1f, groundCheckDistance), groundMask, QueryTriggerInteraction.Ignore);
    }

    private static bool IsShiftHeld()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private static float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
