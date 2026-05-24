using UnityEngine;

public class HouseHPBarScaleUI : MonoBehaviour
{
    [SerializeField] private WallAttackTargetManager wallAttackTargetManager;
    [SerializeField] private RectTransform hpFill;

    private float fullScaleX = 1f;
    private float maxWalls = 1f;

    private void Awake()
    {
        if (hpFill == null)
        {
            Transform fillTransform = transform.Find("HouseHP");
            if (fillTransform != null)
            {
                hpFill = fillTransform as RectTransform;
            }
        }

        if (wallAttackTargetManager == null)
        {
            wallAttackTargetManager = FindObjectOfType<WallAttackTargetManager>();
        }

        if (hpFill != null)
        {
            hpFill.pivot = new Vector2(0f, hpFill.pivot.y);
            fullScaleX = Mathf.Abs(hpFill.localScale.x);
            Vector3 scale = hpFill.localScale;
            scale.x = fullScaleX;
            hpFill.localScale = scale;
        }
    }

    private void OnEnable()
    {
        UpdateHealthBar();
    }

    private void Update()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (wallAttackTargetManager == null || hpFill == null)
            return;

        int currentWalls = wallAttackTargetManager.GetAliveWallCount();

        // 动态校准最大值：首次检测到城墙时记录，墙上限被突破时更新
        if (maxWalls < currentWalls)
        {
            maxWalls = currentWalls;
        }

        float safeMaxWalls = Mathf.Max(1f, maxWalls);
        float healthPercent = Mathf.Clamp01(currentWalls / safeMaxWalls);

        Vector3 scale = hpFill.localScale;
        scale.x = fullScaleX * healthPercent;
        hpFill.localScale = scale;
    }
}
