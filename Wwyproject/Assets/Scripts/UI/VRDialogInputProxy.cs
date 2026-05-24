using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class VRDialogInputProxy : MonoBehaviour
{
    public float aimMaxDistance = 100f;
    public LayerMask interactMask = ~0;

    private Camera vrCamera;

    void Start()
    {
        vrCamera = Camera.main;
    }

    void Update()
    {
        // SteamVR Trigger（右手）
        if (SteamVR_Actions.default_GrabPinch.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        if (MonsterMissionDialogUI.Instance != null &&
            MonsterMissionDialogUI.Instance.IsShowing)
        {
            return;
        }

        if (vrCamera == null) return;

        // 从屏幕中心发射射线（VR头显视角）
        Ray ray = new Ray(vrCamera.transform.position, vrCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, interactMask))
        {
            NPCObject npc = hit.collider.GetComponentInParent<NPCObject>();

            if (npc != null)
            {
                npc.TriggerDialogVR();   // ?? 关键：交给NPC处理
            }
        }
    }
}