using UnityEngine;

public class NPCObject : MonoBehaviour
{
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private string[] contentList;
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private float aimMaxDistance = 100f;
    [SerializeField] private Vector3 dialogOffset = new Vector3(0f, 2f, 1.5f);
    [SerializeField] private LayerMask interactMask = ~0;

    private Transform player;
    private Collider cachedCollider;
    private bool warnedMissingCollider;

    private void Awake()
    {
        cachedCollider = GetComponentInChildren<Collider>();
    }

    private void Update()
    {
        if (MonsterMissionDialogUI.Instance != null && MonsterMissionDialogUI.Instance.IsShowing)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!CanInteract())
        {
            return;
        }

        MonsterMissionDialogUI.Instance.ShowDialog(npcName, contentList, transform, dialogOffset, GetPlayerTransform());
    }

    private bool CanInteract()
    {
        if (MonsterMissionDialogUI.Instance == null)
        {
            Debug.LogWarning("NPCObject could not find MonsterMissionDialogUI in the scene.", this);
            return false;
        }

        if (cachedCollider == null)
        {
            if (!warnedMissingCollider)
            {
                Debug.LogWarning("NPCObject needs a Collider on itself or a child object for mouse interaction.", this);
                warnedMissingCollider = true;
            }

            return false;
        }

        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            Debug.LogWarning("NPCObject could not find an object tagged Player.", this);
            return false;
        }

        if (Vector3.Distance(playerTransform.position, transform.position) > interactDistance)
        {
            return false;
        }

        Camera interactionCamera = GetInteractionCamera();
        if (interactionCamera == null)
        {
            Debug.LogWarning("NPCObject could not find an enabled camera for interaction.", this);
            return false;
        }

        Ray ray = interactionCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            return false;
        }

        return hit.collider.GetComponentInParent<NPCObject>() == this;
    }

    private Transform GetPlayerTransform()
    {
        if (player != null)
        {
            return player;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return null;
        }

        player = playerObject.transform;
        return player;
    }

    private Camera GetInteractionCamera()
    {
        if (Camera.main != null && Camera.main.enabled)
        {
            return Camera.main;
        }

        Camera[] cameras = FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].enabled && cameras[i].gameObject.activeInHierarchy)
            {
                return cameras[i];
            }
        }

        return null;
    }
    public void TriggerDialogVR()
    {
        if (MonsterMissionDialogUI.Instance == null)
            return;

        MonsterMissionDialogUI.Instance.ShowDialog(
            npcName,
            contentList,
            transform,
            dialogOffset,
            GetPlayerTransform()
        );
    }
}
