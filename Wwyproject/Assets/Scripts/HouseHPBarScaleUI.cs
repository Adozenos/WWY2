using UnityEngine;

public class HouseHPBarScaleUI : MonoBehaviour
{
    [SerializeField] private WallAttackTargetManager wallAttackTargetManager;
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private float maxWalls = 5f;

    private float fullScaleX = 1f;

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
            fullScaleX = Mathf.Abs(hpFill.localScale.x);
            Vector3 scale = hpFill.localScale;
            scale.x = fullScaleX;
            hpFill.localScale = scale;
        }

        // 记录初始城墙数量作为最大血量
        if (wallAttackTargetManager != null)
        {
            maxWalls = CountAliveWalls();
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

    private int CountAliveWalls()
    {
        if (wallAttackTargetManager == null || wallAttackTargetManager.attackWalls == null)
            return 0;

        int count = 0;
        foreach (Transform t in wallAttackTargetManager.attackWalls)
        {
            if (t != null)
                count++;
        }
        return count;
    }

    private void UpdateHealthBar()
    {
        if (wallAttackTargetManager == null || hpFill == null)
            return;

        float currentWalls = CountAliveWalls();
        float safeMaxWalls = Mathf.Max(1f, maxWalls);
        float healthPercent = Mathf.Clamp01(currentWalls / safeMaxWalls);

        Vector3 scale = hpFill.localScale;
        scale.x = fullScaleX * healthPercent;
        hpFill.localScale = scale;
    }
}
