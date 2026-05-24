using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject[] monsterPrefabs;     // 可以拖入多个不同的龙预制体
    public Transform[] waypoints;           // 路径点（所有怪物共用）
    public Transform spawnPoint;            // 生成点
    public float spawnInterval = 2f;
    public int maxMonsters = 10;

    private int currentCount = 0;
    private int spawnedCount = 0;
    private Coroutine spawnRoutine;

    public int MaxMonster => maxMonsters;
    public int CurrentCount => currentCount;
    public int SpawnedCount => spawnedCount;
    public bool IsWaveRunning => spawnRoutine != null;
    public bool HasFinishedSpawning => spawnedCount >= maxMonsters && spawnRoutine == null;

    private void OnEnable()
    {
        BeginWave();
    }

    private void OnDisable()
    {
        StopWave();
    }

    public void BeginWave()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogError("未设置任何怪物预制体！");
            return;
        }

        if (spawnRoutine != null)
            return;

        currentCount = 0;
        spawnedCount = 0;
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    public void StopWave()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (spawnedCount < maxMonsters)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnMonster();
        }

        spawnRoutine = null;
    }

    void SpawnMonster()
    {
        if (spawnedCount >= maxMonsters)
            return;

        // 随机选取一个预制体
        GameObject prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;

        GameObject monster = Instantiate(prefab, pos, Quaternion.identity);

        // 获取移动组件（可能为地面或空中版本）
        MonsterPathFollower groundFollower = monster.GetComponent<MonsterPathFollower>();
        AirMonsterFollower airFollower = monster.GetComponent<AirMonsterFollower>();

        if (groundFollower != null)
        {
            groundFollower.waypoints = waypoints;
            groundFollower.spawner = this;
        }
        else if (airFollower != null)
        {
            airFollower.waypoints = waypoints;
            airFollower.spawner = this;
        }
        else
        {
            Debug.LogWarning("怪物预制体上没有挂载任何移动脚本！");
        }

        currentCount++;
        spawnedCount++;
    }

    // 到达终点回调
    public void OnMonsterReachDestination(MonsterPathFollower monster)
    {
        currentCount = Mathf.Max(0, currentCount - 1);
    }

    // 空中怪物也复用同一个回调（重载或统一调用）
    public void OnMonsterReachDestination(AirMonsterFollower monster)
    {
        currentCount = Mathf.Max(0, currentCount - 1);
    }

    // 销毁时降计数
    public void OnMonsterDestroyed()
    {
        currentCount = Mathf.Max(0, currentCount - 1);
    }
}
