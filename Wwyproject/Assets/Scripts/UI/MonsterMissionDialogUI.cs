using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterMissionDialogUI : MonoBehaviour
{
    public static MonsterMissionDialogUI Instance { get; private set; }

    public bool IsShowing => currentDialog != null;

    private GameObject currentDialog;
    private Quaternion templateRotation;
    private bool isTemplate;

    private Text nameText;
    private TMP_Text nameTmpText;
    private Text informationText;
    private Button continueButton;
    private Button stopButton;
    private readonly List<string> contentList = new List<string>();
    private int contentIndex;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            isTemplate = true;
            templateRotation = transform.rotation;
            gameObject.SetActive(false);
            return;
        }

        isTemplate = false;
        BindInstanceUI();
    }

    public void ShowDialog(string npcName, string[] content, Transform npcTransform, Vector3 dialogOffset, Transform playerTransform)
    {
        if (!isTemplate)
        {
            return;
        }

        if (currentDialog != null)
        {
            return;
        }

        Vector3 spawnPosition = GetDialogPosition(npcTransform, dialogOffset, playerTransform);
        Quaternion spawnRotation = GetDialogRotation(spawnPosition, playerTransform);
        GameObject dialog = Instantiate(gameObject, spawnPosition, spawnRotation);
        dialog.name = gameObject.name + "_Dialog";
        dialog.SetActive(true);

        MonsterMissionDialogUI dialogUI = dialog.GetComponent<MonsterMissionDialogUI>();
        if (dialogUI == null)
        {
            Debug.LogWarning("Generated MonsterMissionUI dialog is missing MonsterMissionDialogUI.", this);
            Destroy(dialog);
            return;
        }

        currentDialog = dialog;
        dialogUI.InitializeInstance(npcName, content, OnCurrentDialogClosed);
    }

    private Vector3 GetDialogPosition(Transform npcTransform, Vector3 dialogOffset, Transform playerTransform)
    {
        if (npcTransform == null)
        {
            return transform.position;
        }

        if (playerTransform == null)
        {
            return npcTransform.position + dialogOffset;
        }

        Vector3 towardPlayer = playerTransform.position - npcTransform.position;
        towardPlayer.y = 0f;

        if (towardPlayer.sqrMagnitude <= 0.0001f)
        {
            return npcTransform.position + dialogOffset;
        }

        towardPlayer.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, towardPlayer).normalized;

        return npcTransform.position
            + Vector3.up * dialogOffset.y
            + towardPlayer * dialogOffset.z
            + right * dialogOffset.x;
    }

    private Quaternion GetDialogRotation(Vector3 dialogPosition, Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return templateRotation;
        }

        Vector3 directionToPlayer = playerTransform.position - dialogPosition;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude <= 0.0001f)
        {
            return templateRotation;
        }

        return Quaternion.LookRotation(directionToPlayer.normalized, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
    }

    private void InitializeInstance(string npcName, string[] content, System.Action onClosed)
    {
        BindInstanceUI();

        SetNameText(npcName);

        contentList.Clear();
        if (content != null)
        {
            contentList.AddRange(content);
        }

        contentIndex = 0;
        ShowCurrentContent();
        closedCallback = onClosed;
    }

    private System.Action closedCallback;

    private void BindInstanceUI()
    {
        Transform nameBackground = transform.Find("NameBackground");
        if (nameBackground != null)
        {
            nameText = nameBackground.GetComponentInChildren<Text>(true);
            nameTmpText = nameBackground.GetComponentInChildren<TMP_Text>(true);
        }

        Transform information = transform.Find("Information");
        if (information != null)
        {
            informationText = information.GetComponentInChildren<Text>(true);
        }

        Transform continueTransform = transform.Find("Continue");
        if (continueTransform != null)
        {
            continueButton = continueTransform.GetComponent<Button>();
        }

        Transform stopTransform = transform.Find("Stop");
        if (stopTransform != null)
        {
            stopButton = stopTransform.GetComponent<Button>();
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClick);
            continueButton.onClick.AddListener(OnContinueButtonClick);
        }
        else
        {
            Debug.LogWarning("MonsterMissionDialogUI could not find a Button named Continue under MonsterMissionUI.", this);
        }

        if (stopButton != null)
        {
            stopButton.onClick.RemoveListener(CloseDialog);
            stopButton.onClick.AddListener(CloseDialog);
        }
        else
        {
            Debug.LogWarning("MonsterMissionDialogUI could not find a Button named Stop under MonsterMissionUI.", this);
        }

        if (informationText == null)
        {
            Debug.LogWarning("MonsterMissionDialogUI could not find a Text component under MonsterMissionUI/Information.", this);
        }

        if (nameText == null && nameTmpText == null)
        {
            Debug.LogWarning("MonsterMissionDialogUI could not find a Text or TMP_Text component under MonsterMissionUI/NameBackground.", this);
        }
    }

    private void SetNameText(string npcName)
    {
        string safeName = npcName ?? string.Empty;

        if (nameText != null)
        {
            nameText.text = safeName;
            return;
        }

        if (nameTmpText != null)
        {
            nameTmpText.text = safeName;
        }
    }

    private void OnContinueButtonClick()
    {
        contentIndex++;
        if (contentIndex >= contentList.Count)
        {
            CloseDialog();
            return;
        }

        ShowCurrentContent();
    }

    private void ShowCurrentContent()
    {
        if (informationText == null)
        {
            return;
        }

        if (contentList.Count == 0)
        {
            informationText.text = string.Empty;
            return;
        }

        informationText.text = contentList[Mathf.Clamp(contentIndex, 0, contentList.Count - 1)];
    }

    private void CloseDialog()
    {
        closedCallback?.Invoke();
        Destroy(gameObject);
    }

    private void OnCurrentDialogClosed()
    {
        currentDialog = null;
    }
}
