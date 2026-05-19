using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] monsterPrefabs;
    public Transform startPointsParent;
    public Transform targetWall;
    public Transform wallAttackTarget;
    public float spawnInterval = 4f;
    public int maxMonsters = 30;
    public float spawnSampleRadius = 20f;

    private int currentCount = 0;

    void Start()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogError("No monster prefabs assigned to MonsterSpawner.");
            return;
        }
        if (startPointsParent == null)
        {
            Debug.LogError("No startPointsParent assigned to MonsterSpawner.");
            return;
        }
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
        GameObject prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];

        int childCount = startPointsParent.childCount;
        if (childCount == 0)
        {
            Debug.LogError("startPointsParent has no child spawn points.");
            return;
        }
        Transform spawnPoint = startPointsParent.GetChild(Random.Range(0, childCount));
        Vector3 pos = spawnPoint.position;
        bool sampledSpawn = NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit spawnHit, spawnSampleRadius, NavMesh.AllAreas);
        if (sampledSpawn)
        {
            pos = spawnHit.position;
        }
        else
        {
            Debug.LogWarning($"MonsterSpawner could not find NavMesh within {spawnSampleRadius} units of spawn point {spawnPoint.name}. Spawning at the original point.", spawnPoint);
        }

        Transform target = wallAttackTarget != null ? wallAttackTarget : targetWall;
        Quaternion spawnRotation = GetSpawnRotation(pos, spawnPoint, target);
        GameObject monster = Instantiate(prefab, pos, spawnRotation);

        GroundInvader groundInvader = monster.GetComponent<GroundInvader>();
        FlyingInvader flyingInvader = monster.GetComponent<FlyingInvader>();

        if (groundInvader != null)
        {
            groundInvader.targetWall = target;
            groundInvader.spawner = this;
        }
        else if (flyingInvader != null)
        {
            flyingInvader.targetWall = target;
            flyingInvader.spawner = this;
        }
        else
        {
            Debug.LogWarning("Monster prefab has no GroundInvader or FlyingInvader script.");
        }

        currentCount++;
    }

    Quaternion GetSpawnRotation(Vector3 spawnPosition, Transform spawnPoint, Transform target)
    {
        if (target != null)
        {
            Vector3 toTarget = target.position - spawnPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
                return Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        return spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
    }

    public void OnMonsterDestroyed()
    {
        currentCount--;
    }

    // Legacy support for old scripts
    public void OnMonsterReachDestination(MonsterPathFollower monster)
    {
        currentCount--;
    }

    public void OnMonsterReachDestination(AirMonsterFollower monster)
    {
        currentCount--;
    }
}
