using UnityEngine;

public class MonsterPathFollower : MonoBehaviour
{
    [HideInInspector] public Transform[] waypoints;
    [HideInInspector] public MonsterSpawner spawner;
    public float speed = 3f;
    public float rotationSpeed = 10f;

    [Header("地面检测")]
    [Tooltip("射线检测的起始高度偏移（高于脚下）")]
    public float groundCheckHeight = 2f;
    [Tooltip("射线检测最大距离")]
    public float groundCheckDistance = 5f;
    [Tooltip("选择哪个层是地面")]
    public LayerMask groundLayer = 1;   // 默认是 "Default" 层，你需要改为 Ground 层

    private int currentIndex = 0;
    private bool reachedDestination = false;
    private Animator animator;
    private Vector3 lastPosition;
    private float currentSpeed;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;

        // 开局就把自己贴到地面上
        SnapToGround();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0 || reachedDestination)
            return;

        // 1. 获取下一个路径点的水平目标位置（Y 坐标用龙自己的当前高度）
        Transform target = waypoints[currentIndex];
        Vector3 horizontalTarget = new Vector3(target.position.x, transform.position.y, target.position.z);

        // 2. 计算水平方向及距离
        Vector3 direction = horizontalTarget - transform.position;
        float horizontalDistance = direction.magnitude;
        float step = speed * Time.deltaTime;

        // 3. 水平旋转（只转 Y 轴，避免倾斜）
        Vector3 lookDir = new Vector3(direction.x, 0f, direction.z);
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 4. 水平移动
        if (horizontalDistance <= step)
        {
            // 到达当前路径点（水平距离足够小）
            transform.position = horizontalTarget;
            currentIndex++;

            if (currentIndex >= waypoints.Length)
                ReachDestination();
        }
        else
        {
            Vector3 moveDir = direction / horizontalDistance;
            Vector3 newPos = transform.position + moveDir * step;
            // 移动前先不改变 Y，移动后再统一贴地
            newPos.y = transform.position.y;
            transform.position = newPos;
        }

        // 5. 每一次移动后都进行一次地面检测，把龙“吸”在地面上
        SnapToGround();

        // 6. 计算当前速度，用于动画
        currentSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        if (animator != null)
            animator.SetFloat("Speed", currentSpeed);
    }

    /// <summary>
    /// 从当前头顶上方打射线向下，找到地面层并吸附
    /// </summary>
    void SnapToGround()
    {
        Vector3 rayStart = transform.position + Vector3.up * groundCheckHeight;
        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
        }
        // 如果射线没打到地面，就保持当前高度不变（不会掉下去）
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