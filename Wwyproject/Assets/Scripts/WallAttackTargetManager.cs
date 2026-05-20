using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WallAttackTargetManager : MonoBehaviour
{
    [Header("Wall Targets")]
    public List<Transform> attackWalls = new List<Transform>();
    public float targetSampleRadius = 30f;
    public string targetPointName = "GeneratedWallAttackTarget";

    private readonly Dictionary<Transform, Transform> generatedTargets = new Dictionary<Transform, Transform>();

    public Transform GetRandomTarget(Vector3 spawnPosition)
    {
        if (attackWalls == null || attackWalls.Count == 0)
            return null;

        int startIndex = Random.Range(0, attackWalls.Count);
        for (int i = 0; i < attackWalls.Count; i++)
        {
            int index = (startIndex + i) % attackWalls.Count;
            Transform wall = attackWalls[index];
            if (wall == null)
                continue;

            if (TryGetTargetForWall(wall, out Transform target))
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
