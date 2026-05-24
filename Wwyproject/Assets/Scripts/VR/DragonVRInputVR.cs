using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class DragonVRInputVR : Singleton<DragonVRInputVR>
{
    public Transform headCamera;
    [Header("Dragon")]
    public GameObject dragonObject;
    public Transform riderPoint;

    [Header("Riding")]
    public float mountDistance = 8f;
    public float flySpeed = 16f;
    public float sprintMultiplier = 2f;
    public float takeOffHeight = 20f;
    public float takeOffDuration = 2f;
    public float landDuration = 2f;
    public float rotationSensitivity = 2f;
    public float pitchSensitivity = 0.01f;

    [Header("Physics Layers")]
    public LayerMask groundLayerMask = ~0;

    [Header("Fireball")]
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public float fireballSpeed = 45f;
    public float fireballLifetime = 6f;

    [Header("UI")]
    public GameObject resetQuitMenu;

    [HideInInspector] public bool isMounted;
    private bool isSprinting, wantsForward, isTransitioning;

    private Animator anim;
    private Rigidbody rb;
    private DragonExample dragonExample;
    private Transform riderRoot, riderOriginalParent;
    private CharacterController riderCC;
    private bool riderOriginalCCEnabled, rbOriginalKinematic;
    private RigidbodyConstraints rbOriginalConstraints;
    private Transform head;

    // SteamVR tracking: raw pose data (independent of Transform hierarchy)
    private SteamVR_Behaviour_Pose headPose;
    private Quaternion prevTrackingRot;
    private string currentRidingAnim;

    private const string ANIM_FLY_FORWARD = "Fly Forward";
    private const string ANIM_FLY_GLIDE = "Fly Glide";
    private const string ANIM_FLY_FLOAT = "Fly Float";
    private const string ANIM_TAKE_OFF = "Take Off";
    private const string ANIM_LAND = "Land";
    private const string ANIM_FIREBALL = "Fly Fireball Shoot";
    private const string ANIM_IDLE = "Idle";
    public GameObject dragon;
    void Start()
    {
        if (!dragonObject) { var de = FindObjectOfType<DragonExample>(); if (de) dragonObject = de.gameObject; }
        if (dragonObject)
        {
            anim = dragonObject.GetComponent<Animator>();
            rb = dragonObject.GetComponent<Rigidbody>();
            dragonExample = dragonObject.GetComponent<DragonExample>();
            if (rb) { rbOriginalKinematic = rb.isKinematic; rbOriginalConstraints = rb.constraints; }
        }

        var p = Valve.VR.InteractionSystem.Player.instance;
        if (p)
        {
            head = p.hmdTransform ? p.hmdTransform : Camera.main?.transform;
            riderRoot = p.transform;
            riderOriginalParent = riderRoot.parent;
            riderCC = riderRoot.GetComponent<CharacterController>();
            if (riderCC) riderOriginalCCEnabled = riderCC.enabled;
            if (head) headPose = head.GetComponent<SteamVR_Behaviour_Pose>();
        }

        if (dragonExample) dragonExample.enabled = true;
        if (resetQuitMenu) resetQuitMenu.SetActive(false);

        BindInputs();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void Update()
    {
        if (isMounted && !isTransitioning) UpdateRiding();
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneUnloaded -= OnSceneUnloaded; }

    // ==================== BINDINGS ====================

    void BindInputs()
    {
        var L = SteamVR_Input_Sources.LeftHand; var R = SteamVR_Input_Sources.RightHand;
        SteamVR_Actions.default_GrabGrip.AddOnStateDownListener(OnLeftGrip, L);
        SteamVR_Actions.default_GrabPinch.AddOnStateDownListener(OnLeftTriggerDown, L);
        SteamVR_Actions.default_GrabPinch.AddOnStateUpListener(OnLeftTriggerUp, L);
        SteamVR_Actions.default_MenuMoveUp.AddOnStateDownListener(OnForwardDown, L);
        SteamVR_Actions.default_MenuMoveUp.AddOnStateUpListener(OnForwardUp, L);
        SteamVR_Actions.default_MenuMoveLeft.AddOnStateDownListener(OnMenuToggle, L);
        SteamVR_Actions.default_GrabGrip.AddOnStateDownListener(OnRightGrip, R);
        SteamVR_Actions.default_GrabPinch.AddOnStateDownListener(OnRightTrigger, R);
        SteamVR_Actions.default_ShowMenu.AddOnStateDownListener(OnRightMenu, R);
    }

    void UnbindInputs()
    {
        var L = SteamVR_Input_Sources.LeftHand; var R = SteamVR_Input_Sources.RightHand;
        SteamVR_Actions.default_GrabGrip.RemoveOnStateDownListener(OnLeftGrip, L);
        SteamVR_Actions.default_GrabPinch.RemoveOnStateDownListener(OnLeftTriggerDown, L);
        SteamVR_Actions.default_GrabPinch.RemoveOnStateUpListener(OnLeftTriggerUp, L);
        SteamVR_Actions.default_MenuMoveUp.RemoveOnStateDownListener(OnForwardDown, L);
        SteamVR_Actions.default_MenuMoveUp.RemoveOnStateUpListener(OnForwardUp, L);
        SteamVR_Actions.default_MenuMoveLeft.RemoveOnStateDownListener(OnMenuToggle, L);
        SteamVR_Actions.default_GrabGrip.RemoveOnStateDownListener(OnRightGrip, R);
        SteamVR_Actions.default_GrabPinch.RemoveOnStateDownListener(OnRightTrigger, R);
        SteamVR_Actions.default_ShowMenu.RemoveOnStateDownListener(OnRightMenu, R);
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) { BindInputs(); if (isMounted) ForceDismount(); }
    void OnSceneUnloaded(Scene s) { UnbindInputs(); if (isMounted) ForceDismount(); }

    // ==================== HANDLERS ====================

    void OnLeftGrip(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { if (!isMounted && !isTransitioning && NearDragon()) StartCoroutine(MountAndTakeOff()); }
    void OnLeftTriggerDown(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { if (isMounted) isSprinting = true; }
    void OnLeftTriggerUp(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { isSprinting = false; }
    void OnForwardDown(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { wantsForward = true; }
    void OnForwardUp(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { wantsForward = false; }
    void OnMenuToggle(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { if (resetQuitMenu) resetQuitMenu.SetActive(!resetQuitMenu.activeSelf); }
    void OnRightGrip(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { if (isMounted && !isTransitioning) StartCoroutine(DismountAndLand()); }
    void OnRightTrigger(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { if (isMounted && !isTransitioning) DragonAttack(); }
    void OnRightMenu(SteamVR_Action_Boolean a, SteamVR_Input_Sources s) { var ui = DragonVRUIManagerVR.Instance; if (ui) ui.ToggleButtonHints(); }

    // ==================== MOUNT / DISMOUNT ====================

    bool NearDragon() => dragonObject && Valve.VR.InteractionSystem.Player.instance
        && Vector3.Distance(Valve.VR.InteractionSystem.Player.instance.transform.position, dragonObject.transform.position) <= mountDistance;

    IEnumerator MountAndTakeOff()
    {
        isTransitioning = true;
        if (dragonExample) dragonExample.enabled = false;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationZ;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (anim) anim.applyRootMotion = false;

        riderRoot.SetParent(dragonObject.transform, false);
        riderRoot.localPosition = riderPoint ? riderPoint.localPosition : Vector3.zero;
        riderRoot.localRotation = Quaternion.identity;

        if (headPose != null)
            prevTrackingRot = headPose.poseAction[headPose.inputSource].localRotation;
        else
            prevTrackingRot = head.localRotation;

        if (riderCC) { riderOriginalCCEnabled = riderCC.enabled; riderCC.enabled = false; }

        Quaternion startRot = dragonObject.transform.rotation;
        Vector3 startPos = dragonObject.transform.position;
        Vector3 targetPos = startPos + Vector3.up * takeOffHeight;

        PlayAnim(ANIM_TAKE_OFF);

        for (float t = 0; t < takeOffDuration; t += Time.deltaTime)
        {
            float p = Mathf.Clamp01(t / takeOffDuration);
            SetDragonPosition(Vector3.Lerp(startPos, targetPos, p));
            SetDragonRotation(Quaternion.Euler(0f, startRot.eulerAngles.y, 0f));
            yield return null;
        }

        isMounted = true;
        isTransitioning = false;
        if (anim) anim.SetBool("IsFlying", true);
        PlayAnim(ANIM_FLY_FLOAT);
    }

    IEnumerator DismountAndLand()
    {
        isTransitioning = true; isMounted = false; isSprinting = false;
        if (anim) anim.SetBool("IsFlying", false);
        PlayAnim(ANIM_LAND);

        if (riderRoot)
        {
            riderRoot.SetParent(riderOriginalParent, true);
            Vector3 g = riderRoot.position; g.y = GroundY(riderRoot.position) + 0.1f;
            riderRoot.position = g;
            riderRoot.rotation = Quaternion.Euler(0f, riderRoot.rotation.eulerAngles.y, 0f);
        }

        Vector3 from = dragonObject.transform.position;
        Vector3 to = from; to.y = GroundY(from);
        for (float t = 0; t < landDuration; t += Time.deltaTime)
        {
            float p = Mathf.Clamp01(t / landDuration);
            SetDragonPosition(Vector3.Lerp(from, to, p));
            yield return null;
        }

        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = rbOriginalKinematic;
            rb.constraints = rbOriginalConstraints;
        }

        if (anim) anim.applyRootMotion = true;
        if (riderCC) riderCC.enabled = riderOriginalCCEnabled;
        if (dragonExample) dragonExample.enabled = true;
        PlayAnim(ANIM_IDLE);
        isTransitioning = false;
    }

    void ForceDismount()
    {
        isMounted = isTransitioning = false; isSprinting = false;
        if (anim) anim.SetBool("IsFlying", false);
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = rbOriginalKinematic; rb.constraints = rbOriginalConstraints; }
        if (anim) anim.applyRootMotion = true;

        if (riderRoot) riderRoot.SetParent(riderOriginalParent, true);
        if (riderRoot) riderRoot.rotation = Quaternion.Euler(0f, riderRoot.rotation.eulerAngles.y, 0f);

        if (riderCC) riderCC.enabled = riderOriginalCCEnabled;
        if (dragonExample) dragonExample.enabled = true;
        if (anim) anim.Play(ANIM_IDLE);
    }

    // ==================== RIDING ====================

    void UpdateRiding()
    {
        if (!dragonObject || !head) return;

      dragon.transform.localRotation = headCamera.transform.localRotation;
    }

    // ==================== ATTACK ====================

    void DragonAttack() { PlayAnim(ANIM_FIREBALL); StartCoroutine(SpawnFireball(0.5f)); }
    IEnumerator SpawnFireball(float d)
    {
        yield return new WaitForSeconds(d);
        if (!fireballPrefab || !fireballSpawnPoint) yield break;
        var fb = Instantiate(fireballPrefab, fireballSpawnPoint.position, fireballSpawnPoint.rotation);
        var proj = fb.GetComponent<DragonFireballProjectile>();
        if (proj) proj.Initialize(fireballSpawnPoint.forward, fireballSpeed, fireballLifetime, dragonObject);
        else { var r = fb.GetComponent<Rigidbody>(); if (r) r.velocity = fireballSpawnPoint.forward * fireballSpeed; Destroy(fb, fireballLifetime); }
    }

    // ==================== HELPERS ====================

    void SetDragonPosition(Vector3 pos)
    {
        if (rb && !rb.isKinematic) rb.MovePosition(pos);
        else dragonObject.transform.position = pos;
    }

    void SetDragonRotation(Quaternion rot)
    {
        if (rb && !rb.isKinematic) rb.MoveRotation(rot);
        else dragonObject.transform.rotation = rot;
    }

    void PlayAnim(string n) { if (anim && !string.IsNullOrEmpty(n) && !anim.GetCurrentAnimatorStateInfo(0).IsName(n)) anim.CrossFadeInFixedTime(n, 0.15f, 0, 0f); }
    float GroundY(Vector3 p) => Physics.Raycast(p + Vector3.up * 50f, Vector3.down, out var h, 100f, groundLayerMask, QueryTriggerInteraction.Ignore) ? h.point.y : 0f;
}
