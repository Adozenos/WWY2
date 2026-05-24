using UnityEngine;
using Valve.VR;

/// <summary>VR 版 NPC 交互。当右手碰到 NPC 时按 Trigger 触发对话。</summary>
public class NPCObjectVR : MonoBehaviour
{
    [SerializeField] private string npcName = "NPC";
    [SerializeField][TextArea] private string[] contentList;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private Vector3 dialogOffset = new Vector3(0f, 2f, 1.5f);

    private Transform player;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("WanJia");
        if (go) player = go.transform;
    }

    void OnTriggerStay(Collider other)
    {
        if (player == null || MonsterMissionDialogUI.Instance == null || MonsterMissionDialogUI.Instance.IsShowing)
            return;

        if (Vector3.Distance(player.position, transform.position) > interactDistance)
            return;

        // 右手 Trigger 触发
        if (SteamVR_Actions.default_GrabPinch.GetStateUp(SteamVR_Input_Sources.RightHand))
        {
            MonsterMissionDialogUI.Instance.ShowDialog(npcName, contentList, transform, dialogOffset, player);
        }
    }
}
