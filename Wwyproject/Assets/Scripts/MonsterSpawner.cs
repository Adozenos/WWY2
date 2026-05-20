using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] monsterPrefabs;
    public Transform[] waypoints;
    public Transform spawnPoint;
    public Transform[] spawnPoints;
    public float spawnSampleRadius = 20f;
    public WallAttackTargetManager wallAttackTargetManager;
    public float spawnInterval = 2f;
    public int maxMonsters = 10;

    private int currentCount;

    private void Start()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogError("MonsterSpawner has no monster prefabs assigned.", this);
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (currentCount < maxMonsters)
                SpawnMonster();
        }
    }

    private void SpawnMonster()
    {
        GameObject prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        Transform selectedSpawnPoint = GetRandomSpawnPoint();
        Vector3 requestedPosition = selectedSpawnPoint != null ? selectedSpawnPoint.position : transform.position;
        Vector3 spawnPosition = GetNavMeshSpawnPosition(requestedPosition);
        Transform attackTarget = wallAttackTargetManager != null
            ? wallAttackTargetManager.GetRandomTarget(spawnPosition)
            : null;
        Quaternion spawnRotation = GetSpawnRotation(spawnPosition, attackTarget, selectedSpawnPoint);

        GameObject monster = Instantiate(prefab, spawnPosition, spawnRotation);
        EnsureMonsterHealth(monster);
        ConfigureMonsterMovement(monster, attackTarget);

        currentCount++;
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int startIndex = Random.Range(0, spawnPoints.Length);
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform candidate = spawnPoints[(startIndex + i) % spawnPoints.Length];
                if (candidate != null)
                    return candidate;
            }
        }

        return spawnPoint;
    }

    private Vector3 GetNavMeshSpawnPosition(Vector3 requestedPosition)
    {
        if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, spawnSampleRadius, NavMesh.AllAreas))
            return hit.position;

        Debug.LogWarning($"MonsterSpawner could not find NavMesh near spawn point {requestedPosition} within {spawnSampleRadius} units. Using raw spawn position.", this);
        return requestedPosition;
    }

    private Quaternion GetSpawnRotation(Vector3 spawnPosition, Transform attackTarget, Transform selectedSpawnPoint)
    {
        if (attackTarget != null)
        {
            Vector3 direction = attackTarget.position - spawnPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        return selectedSpawnPoint != null ? selectedSpawnPoint.rotation : transform.rotation;
    }

    private void EnsureMonsterHealth(GameObject monster)
    {
        if (monster != null && monster.GetComponent<MonsterHealth>() == null)
            monster.AddComponent<MonsterHealth>();
    }

    private void ConfigureMonsterMovement(GameObject monster, Transform attackTarget)
    {
        if (monster == null)
            return;

        MonsterPathFollower groundFollower = monster.GetComponent<MonsterPathFollower>();
        if (groundFollower != null)
        {
            groundFollower.waypoints = waypoints;
            groundFollower.spawner = this;
            return;
        }

        AirMonsterFollower airFollower = monster.GetComponent<AirMonsterFollower>();
        if (airFollower != null)
        {
            airFollower.waypoints = waypoints;
            airFollower.spawner = this;
            return;
        }

        GroundInvader groundInvader = monster.GetComponent<GroundInvader>();
        if (groundInvader != null)
        {
            groundInvader.spawner = this;
            groundInvader.targetWall = attackTarget;
            WarnIfMissingTarget(attackTarget, monster);
            return;
        }

        FlyingInvader flyingInvader = monster.GetComponent<FlyingInvader>();
        if (flyingInvader != null)
        {
            flyingInvader.spawner = this;
            flyingInvader.targetWall = attackTarget;
            WarnIfMissingTarget(attackTarget, monster);
            return;
        }

        Debug.LogWarning("Monster prefab has no supported movement script.", monster);
    }

    private void WarnIfMissingTarget(Transform attackTarget, GameObject monster)
    {
        if (attackTarget == null)
            Debug.LogWarning("MonsterSpawner could not assign a wall attack target. Check WallAttackTargetManager.attackWalls and nearby NavMesh.", monster);
    }

    public void OnMonsterReachDestination(MonsterPathFollower monster)
    {
        currentCount = Mathf.Max(0, currentCount - 1);
    }

    public void OnMonsterReachDestination(AirMonsterFollower monster)
    {
        currentCount = Mathf.Max(0, currentCount - 1);
    }

    public void OnMonsterDestroyed()
    {
        currentCount = Mathf.Max(0, currentCount - 1);
    }
}
