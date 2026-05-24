using UnityEngine;
using UnityEngine.UI;

public class MonsterMissionTaskUI : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private MonsterSpawner monsterSpawner;
    [SerializeField] private Button startButton;
    [SerializeField] private bool hideStartButtonAfterStart;

    [Header("Mission UI")]
    [SerializeField] private GameObject wallHealthBar;
    [SerializeField] private Text monsterCountText;
    [SerializeField] private GameObject victoryCanvas;
    [SerializeField] private FadeCanvasUI victoryFadeUI;

    [Header("Text")]
    [SerializeField] private string monsterCountFormat = "Monsters: {0}";

    private bool missionRunning;

    private void Awake()
    {
        if (monsterSpawner == null)
            monsterSpawner = FindObjectOfType<MonsterSpawner>(true);

        if (startButton != null)
            startButton.onClick.AddListener(StartMission);

        HideMissionUI();

        if (victoryCanvas != null)
            victoryCanvas.SetActive(false);

        if (monsterSpawner != null)
            monsterSpawner.enabled = false;
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartMission);
    }

    private void Update()
    {
        if (!missionRunning)
            return;

        int monsterCount = GetCurrentMonsterCount();
        UpdateMonsterCountText(monsterCount);

        if (monsterSpawner != null && monsterSpawner.HasFinishedSpawning && monsterCount <= 0)
            CompleteMission();
    }

    public void StartMission()
    {
        if (missionRunning || monsterSpawner == null)
            return;

        missionRunning = true;
        ShowMissionUI();

        if (victoryCanvas != null)
            victoryCanvas.SetActive(false);

        if (hideStartButtonAfterStart && startButton != null)
            startButton.gameObject.SetActive(false);

        monsterSpawner.enabled = true;
        monsterSpawner.BeginWave();
        UpdateMonsterCountText(GetCurrentMonsterCount());
    }

    private void CompleteMission()
    {
        if (!missionRunning) return;

        missionRunning = false;
        HideMissionUI();

        if (victoryCanvas != null)
            victoryCanvas.SetActive(true);

        if (victoryFadeUI != null)
        {
            victoryFadeUI.FadeIn();
            Invoke("FadeOut", 5);
        }
            
        else if (victoryCanvas != null)
            victoryCanvas.GetComponent<FadeCanvasUI>()?.FadeIn();
    }

    private int GetCurrentMonsterCount()
    {
        if (MonsterManage.instance == null)
            return 0;

        int groundCount = MonsterManage.instance.monsterList != null ? MonsterManage.instance.monsterList.Count : 0;
        int flyCount = MonsterManage.instance.flyMonsterList != null ? MonsterManage.instance.flyMonsterList.Count : 0;
        return groundCount + flyCount;
    }

    private void ShowMissionUI()
    {
        if (wallHealthBar != null)
            wallHealthBar.SetActive(true);

        if (monsterCountText != null)
            monsterCountText.gameObject.SetActive(true);
    }

    private void HideMissionUI()
    {
        if (wallHealthBar != null)
            wallHealthBar.SetActive(false);

        if (monsterCountText != null)
            monsterCountText.gameObject.SetActive(false);
    }

    private void UpdateMonsterCountText(int monsterCount)
    {
        if (monsterCountText != null)
            monsterCountText.text = string.Format(monsterCountFormat, monsterCount);
    }

    private void FadeOut()
    {
        victoryFadeUI.FadeOut();
    }
}

