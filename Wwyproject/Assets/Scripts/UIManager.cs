using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI 文本")]
    public Text monsterArriveText;   // 上方倒计时文本
    public Text monsterCountText;    // 下方怪物数量文本

    [Header("UI 按钮")]
    public Button startButton;       // 开始任务
    public Button endButton;         // 终止任务

    [Header("游戏配置")]
    public float initialCountdown = 60f;
    public int totalMonstersThisWave = 10;

    private float currentCountdown;
    private int currentMonsterCount;
    private bool isMissionActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 关键改动：初始化时隐藏文本
        monsterArriveText.gameObject.SetActive(false);
        monsterCountText.gameObject.SetActive(false);

        // 按钮状态初始化
        startButton.interactable = true;
        endButton.interactable = false;
    }

    void Update()
    {
        if (!isMissionActive) return;

        if (currentCountdown > 0)
        {
            currentCountdown -= Time.deltaTime;
            UpdateArriveText();
        }
        else
        {
            CheckMissionResult();
        }
    }

    #region UI 更新
    void UpdateArriveText()
    {
        int secondsLeft = Mathf.CeilToInt(currentCountdown);
        monsterArriveText.text = $"怪物还有 {secondsLeft} 秒到达";
    }

    void UpdateMonsterCountText()
    {
        monsterCountText.text = $"本轮怪物还有 {currentMonsterCount} 个";
    }
    #endregion

    #region 任务控制
    // 点击“开始任务”
    public void OnStartMission()
    {
        isMissionActive = true;
        currentCountdown = initialCountdown;
        currentMonsterCount = totalMonstersThisWave;

        // 关键改动：显示文本
        monsterArriveText.gameObject.SetActive(true);
        monsterCountText.gameObject.SetActive(true);

        startButton.interactable = false;
        endButton.interactable = true;

        UpdateArriveText();
        UpdateMonsterCountText();
    }

    // 点击“终止任务”
    public void OnEndMission()
    {
        isMissionActive = false;

        // 关键改动：隐藏文本
        monsterArriveText.gameObject.SetActive(false);
        monsterCountText.gameObject.SetActive(false);

        startButton.interactable = true;
        endButton.interactable = false;
    }

    // 检查任务结果
    void CheckMissionResult()
    {
        isMissionActive = false;
        startButton.interactable = true;
        endButton.interactable = false;

        // 任务结束后，你可以选择隐藏文本，或者显示结果文字
        if (currentMonsterCount <= 0)
        {
            monsterArriveText.text = "任务完成！";
        }
        else
        {
            monsterArriveText.text = "任务失败！";
        }
        // 这里可以选择不隐藏，让玩家看到结果
        // monsterArriveText.gameObject.SetActive(false); 
        // monsterCountText.gameObject.SetActive(false);
    }
    #endregion

    #region 外部调用
    public void OnMonsterDefeated()
    {
        if (!isMissionActive) return;

        currentMonsterCount--;
        UpdateMonsterCountText();

        if (currentMonsterCount <= 0)
        {
            CheckMissionResult();
        }
    }
    #endregion
}