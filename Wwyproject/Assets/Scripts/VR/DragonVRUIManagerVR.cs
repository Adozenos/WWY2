using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// VR UI 管理器。负责手柄按键提示、重置/退出菜单、操作说明面板。
/// </summary>
public class DragonVRUIManagerVR : Singleton<DragonVRUIManagerVR>
{
    [Header("Menus")]
    public GameObject resetQuitMenu;
    public GameObject instructionsPanel;

    [Header("Menu Buttons")]
    public GameObject resetBtn;
    public GameObject quitBtn;

    [Header("Hints")]
    public float showHintsDelay = 3f;

    private bool isHintsShowing;
    private bool isInstructionsShowing;
    private bool isMenuShowing;
    private GameObject currentBtn;

    void Start()
    {
        // Ensure menus start hidden
        if (resetQuitMenu != null) resetQuitMenu.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);

        // Show hints after a delay
        Invoke("ShowButtonHints", showHintsDelay);

        // Bind scene events
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // ===========================
    // SCENE EVENTS
    // ===========================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Invoke("ShowButtonHints", showHintsDelay);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        HideAllButtonHints();
        UnbindMenuInputs();
    }

    // ===========================
    // BUTTON HINTS
    // ===========================

    public void SetControllerButtonHints(bool state)
    {
        if (state)
            ShowButtonHints();
        else
            HideAllButtonHints();
    }

    /// <summary>切换按键提示显示/隐藏（菜单键回调）</summary>
    public void ToggleButtonHints()
    {
        SetControllerButtonHints(!isHintsShowing);
    }

    private void ShowButtonHints()
    {
        if (Valve.VR.InteractionSystem.Player.instance == null) return;

        ControllerButtonHints.HideAllButtonHints(Valve.VR.InteractionSystem.Player.instance.leftHand);
        ControllerButtonHints.HideAllButtonHints(Valve.VR.InteractionSystem.Player.instance.rightHand);

        // Right hand hints
        ControllerButtonHints.ShowTextHint(Valve.VR.InteractionSystem.Player.instance.rightHand, SteamVR_Actions.default_GrabGrip, "下龙/落地");
        ControllerButtonHints.ShowTextHint(Valve.VR.InteractionSystem.Player.instance.rightHand, SteamVR_Actions.default_GrabPinch, "交互/龙攻击");
        ControllerButtonHints.ShowTextHint(Valve.VR.InteractionSystem.Player.instance.rightHand, SteamVR_Actions.default_ShowMenu, "按键说明");

        // Left hand hints
        ControllerButtonHints.ShowTextHint(Valve.VR.InteractionSystem.Player.instance.leftHand, SteamVR_Actions.default_GrabGrip, "上龙/升空");
        ControllerButtonHints.ShowTextHint(Valve.VR.InteractionSystem.Player.instance.rightHand, SteamVR_Actions.default_Teleport, "移动");

        isHintsShowing = true;
    }

    private void HideAllButtonHints()
    {
        if (Valve.VR.InteractionSystem.Player.instance == null) return;
        ControllerButtonHints.HideAllButtonHints(Valve.VR.InteractionSystem.Player.instance.leftHand);
        ControllerButtonHints.HideAllButtonHints(Valve.VR.InteractionSystem.Player.instance.rightHand);
        isHintsShowing = false;
    }

    // ===========================
    // MENU MANAGEMENT
    // ===========================

    /// <summary>显示重置/退出菜单并绑定导航输入</summary>
    public void ShowMenu()
    {
        if (resetQuitMenu == null) return;

        resetQuitMenu.SetActive(true);
        isMenuShowing = true;

        // Bind menu navigation (left hand trackpad)
        SteamVR_Actions.default_Teleport.AddOnStateDownListener(OnMenuUp, SteamVR_Input_Sources.LeftHand);
        SteamVR_Actions.default_GrabGrip.AddOnStateUpListener(OnMenuDown, SteamVR_Input_Sources.LeftHand);
        // Confirm selection with trigger
        SteamVR_Actions.default_GrabPinch.AddOnStateUpListener(OnMenuConfirm, SteamVR_Input_Sources.LeftHand);
    }

    /// <summary>隐藏重置/退出菜单并解绑导航输入</summary>
    public void HideMenu()
    {
        if (resetQuitMenu != null)
            resetQuitMenu.SetActive(false);
        isMenuShowing = false;

        EventSystem.current?.SetSelectedGameObject(null);
        currentBtn = null;

        UnbindMenuInputs();
    }

    private void UnbindMenuInputs()
    {
        SteamVR_Actions.default_Teleport.RemoveOnStateDownListener(OnMenuUp, SteamVR_Input_Sources.LeftHand);
        SteamVR_Actions.default_GrabGrip.RemoveOnStateUpListener(OnMenuDown, SteamVR_Input_Sources.LeftHand);
        SteamVR_Actions.default_GrabPinch.RemoveOnStateUpListener(OnMenuConfirm, SteamVR_Input_Sources.LeftHand);
    }

    private void OnMenuUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (currentBtn == null)
            currentBtn = resetBtn;
        else
        {
            Selectable up = currentBtn.GetComponent<Button>()?.FindSelectableOnUp();
            if (up != null)
                currentBtn = up.gameObject;
        }

        if (currentBtn != null)
            EventSystem.current?.SetSelectedGameObject(currentBtn);
    }

    private void OnMenuDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (currentBtn == null)
            currentBtn = quitBtn;
        else
        {
            Selectable down = currentBtn.GetComponent<Button>()?.FindSelectableOnDown();
            if (down != null)
                currentBtn = down.gameObject;
        }

        if (currentBtn != null)
            EventSystem.current?.SetSelectedGameObject(currentBtn);
    }

    private void OnMenuConfirm(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (currentBtn == null) return;

        ExecuteEvents.Execute(currentBtn, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
    }

    // ===========================
    // BUTTON ACTIONS
    // ===========================

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===========================
    // INSTRUCTIONS
    // ===========================

    public void ToggleInstructions()
    {
        if (instructionsPanel == null) return;
        isInstructionsShowing = !isInstructionsShowing;
        instructionsPanel.SetActive(isInstructionsShowing);
    }
}
