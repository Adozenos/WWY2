using System.Collections;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject monsterPrefab;   // 怪物预制体
    public Transform[] waypoints;      // 路径点数组（按顺序排列）
    public Transform spawnPoint;       // 生成点（可以为空，默认使用自身位置）
    public float spawnInterval = 2f;   // 生成间隔
    public int maxMonsters = 10;       // 同时存在的最大怪物数量

    private int currentCount = 0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (currentCount < maxMonsters)
                SpawnMonster();
        }
    }

    void SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("Monster Prefab 未赋值！");
            return;
        }

        Vector3 pos = spawnPoint ? spawnPoint.position : transform.position;
        GameObject monster = Instantiate(monsterPrefab, pos, Quaternion.identity);

        MonsterPathFollower follower = monster.GetComponent<MonsterPathFollower>();
        if (follower != null)
        {
            follower.waypoints = waypoints;
            follower.spawner = this;
        }
        else
        {
            Debug.LogWarning("怪物预制体上未找到 MonsterPathFollower 脚本！");
        }

        currentCount++;
    }

    // 怪物到达终点时调用
    public void OnMonsterReachDestination(MonsterPathFollower monster)
    {
        currentCount--;
        // 这里可以添加玩家扣血等逻辑，例如 GameManager.Instance.TakeDamage(1);
    }

    // 怪物被中途销毁时调用（例如被击杀）
    public void OnMonsterDestroyed()
    {
        currentCount--;
    }
}