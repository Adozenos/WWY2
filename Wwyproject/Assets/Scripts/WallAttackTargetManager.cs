using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WallAttackTargetManager : MonoBehaviour
{
    public static WallAttackTargetManager instance { get; private set; }

    [Header("Wall Targets")]
    public List<Wall> attackWalls = new List<Wall>();
    public float targetSampleRadius = 30f;
    public string targetPointName = "GeneratedWallAttackTarget";

    private readonly Dictionary<Transform, Transform> generatedTargets = new Dictionary<Transform, Transform>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (WallManager.instance != null && WallManager.instance.wallList != null)
        {
            attackWalls = WallManager.instance.wallList;
        }
    }

    public bool TryDamageWall(GameObject obj, float damage)
    {
        if (obj == null) return false;

        Wall wall = obj.GetComponentInParent<Wall>();
        if (wall == null) return false;

        wall.HP -= damage;
        return true;
    }

    public int GetAliveWallCount()
    {
        if (attackWalls == null) return 0;

        int count = 0;
        foreach (Wall w in attackWalls)
        {
            if (w != null && w.gameObject != null && w.HP > 0)
                count++;
        }
        return count;
    }

    public Transform GetRandomTarget(Vector3 spawnPosition)
    {
        if (attackWalls == null || attackWalls.Count == 0)
            return null;

        int startIndex = Random.Range(0, attackWalls.Count);
        for (int i = 0; i < attackWalls.Count; i++)
        {
            int index = (startIndex + i) % attackWalls.Count;
            Wall wall = attackWalls[index];
            if (wall == null || wall.gameObject == null || wall.HP <= 0)
                continue;

            if (TryGetTargetForWall(wall.transform, out Transform target))
                return target;
        }

        Debug.LogWarning($"{name} could not sample a NavMesh attack target near any configured wall.", this);
        return null;
    }

    private bool TryGetTargetForWall(Transform wall, out Transform target)
    {
        target = null;
        if (!NavMesh.SamplePosition(wall.position, out NavMeshHit hit, targetSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name} could not sample NavMesh near wall target {wall.name} within {targetSampleRadius} units.", wall);
            return false;
        }

        target = GetOrCreateGeneratedTarget(wall);
        target.position = hit.position;
        return true;
    }

    private Transform GetOrCreateGeneratedTarget(Transform wall)
    {
        if (generatedTargets.TryGetValue(wall, out Transform target) && target != null)
            return target;

        GameObject targetObject = new GameObject($"{targetPointName}_{wall.name}");
        targetObject.transform.SetParent(transform, false);
        target = targetObject.transform;
        generatedTargets[wall] = target;
        return target;
    }
}
