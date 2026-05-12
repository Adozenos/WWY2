using UnityEngine;

public class AirMonsterFollower : MonoBehaviour
{
    [HideInInspector] public Transform[] waypoints;
    [HideInInspector] public MonsterSpawner spawner;
    public float speed = 4f;
    public float rotationSpeed = 5f;

    [Header("飞行高度偏移")]
    [Tooltip("飞龙距离地面的高度（米）")]
    public float heightOffset = 2f;          // 飞行高度偏移量

    [Header("地面检测")]
    [Tooltip("射线起始点高于当前位置的高度")]
    public float groundCheckHeight = 5f;
    [Tooltip("射线向下检测的最大距离")]
    public float groundCheckDistance = 10f;
    [Tooltip("选择地面所在的 Layer")]
    public LayerMask groundLayer = 1;

    private int currentIndex = 0;
    private bool reachedDestination = false;
    private Animator animator;
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;

        // 开局先吸附到地形高度 + 偏移
        AdjustHeightToGround();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0 || reachedDestination)
            return;

        // 1. 计算水平目标点（忽略路径点自身的 Y 坐标）
        Transform target = waypoints[currentIndex];
        Vector3 horizontalTarget = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 direction = horizontalTarget - transform.position;
        float horizontalDistance = direction.magnitude;
        float step = speed * Time.deltaTime;

        // 2. 水平旋转
        Vector3 lookDir = new Vector3(direction.x, 0f, direction.z);
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // 3. 水平移动
        if (horizontalDistance <= step)
        {
            transform.position = horizontalTarget;
            currentIndex++;
            if (currentIndex >= waypoints.Length)
                ReachDestination();
        }
        else
        {
            Vector3 moveDir = direction / horizontalDistance;
            Vector3 newPos = transform.position + moveDir * step;
            // 先不改变 Y，后面统一调整
            newPos.y = transform.position.y;
            transform.position = newPos;
        }

        // 4. 每帧根据地形调整高度（关键！）
        AdjustHeightToGround();

        // 5. 动画速度
        currentSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;
        if (animator != null)
            animator.SetFloat("Speed", currentSpeed);
    }

    /// <summary>
    /// 用射线检测地面高度，并让飞龙保持 heightOffset 的离地距离
    /// </summary>
    void AdjustHeightToGround()
    {
        // 关键修复：射线起点必须是固定的，或者至少是基于脚底的，而不是基于当前位置的累加。
        // 我们改为从龙的正下方稍微往上的地方发射，或者从最高点往下扫。
        // 建议方案：从龙的中心点正上方较高处往下打。
        Vector3 rayStart = transform.position + Vector3.up * 20f; // 给一个足够高的固定值，确保能覆盖地形

        Debug.DrawRay(rayStart, Vector3.down * 50f, Color.red, 0.1f);

        RaycastHit hit;
        // 增加逻辑：只有当我们确实击中了东西，且那个东西在我们下方时，才调整高度
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f, groundLayer))
        {
            // 关键修复：只有当命中点低于当前位置时，或者强制贴地时才生效
            // 防止因为射线打到了侧面的山壁而导致飞上天
            float targetY = hit.point.y + heightOffset;

            // 防止龙穿过地面（如果targetY比当前Y低很多，直接瞬移；如果只差一点，平滑处理）
            if (transform.position.y < targetY)
            {
                // 如果目标点比当前高，通常意味着我们要飞起来越过障碍，这时候不应该瞬移，而是让移动逻辑处理
                // 但为了防飞走，我们限制最大上升速度或直接忽略过高的点
                // 简单粗暴版：如果目标点比当前高太多（比如超过5米），就不理它，防止卡在山壁上飞走
                if (targetY - transform.position.y > 5f)
                {
                    return; // 目标太高了，可能是打到了奇怪的东西，忽略这次调整
                }
            }

            Vector3 pos = transform.position;
            pos.y = targetY;
            transform.position = pos;
        }
    }

    void ReachDestination()
    {
        reachedDestination = true;
        spawner?.OnMonsterReachDestination(this);
        spawner = null;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (!reachedDestination && spawner != null)
        {
            spawner.OnMonsterDestroyed();
            spawner = null;
        }
    }
}