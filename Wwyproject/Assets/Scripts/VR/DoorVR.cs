using UnityEngine;
using Valve.VR;

/// <summary>VR 版门。靠近按 Trigger 旋转开关，无需 Animator。</summary>
public class DoorVR : MonoBehaviour
{
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float speed = 3f;

    private bool isOpen;
    private Transform player;
    private Quaternion closedRot, openRot;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go) player = go.transform;
        closedRot = transform.localRotation;
        openRot = closedRot * Quaternion.Euler(0f, openAngle, 0f);
    }

    void Update()
    {
        if (player == null) return;
        if (Vector3.Distance(player.position, transform.position) > interactDistance) return;

        if (SteamVR_Actions.default_InteractUI.GetStateUp(SteamVR_Input_Sources.LeftHand)
         || SteamVR_Actions.default_InteractUI.GetStateUp(SteamVR_Input_Sources.RightHand))
        {
            isOpen = !isOpen;
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, isOpen ? openRot : closedRot, Time.deltaTime * speed);
    }
}
