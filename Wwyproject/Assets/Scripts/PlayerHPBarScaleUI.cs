using UnityEngine;

public class PlayerHPBarScaleUI : MonoBehaviour
{
    [SerializeField] private Player targetPlayer;
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private float maxHP = 100f;

    private float fullScaleX = 1f;

    private void Awake()
    {
        if (hpFill == null)
        {
            Transform fillTransform = transform.Find("HP");
            if (fillTransform != null)
            {
                hpFill = fillTransform as RectTransform;
            }
        }

        if (targetPlayer == null)
        {
            DragonExample dragon = FindObjectOfType<DragonExample>();
            if (dragon != null)
            {
                targetPlayer = dragon.GetComponent<Player>();
            }
        }

        if (hpFill != null)
        {
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
        if (targetPlayer == null || hpFill == null)
        {
            return;
        }

        float safeMaxHP = Mathf.Max(1f, maxHP);
        float healthPercent = Mathf.Clamp01(targetPlayer.HP / safeMaxHP);

        Vector3 scale = hpFill.localScale;
        scale.x = fullScaleX * healthPercent;
        hpFill.localScale = scale;
    }
}
