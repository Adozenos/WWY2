using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public sealed class DragonBoneOriginAnchor : MonoBehaviour
{
    [SerializeField] private string boneName = "Bone.006";
    [SerializeField] private bool autoFindVisualRoots = true;
    [SerializeField] private List<Transform> visualRoots = new List<Transform>();
    [SerializeField] private bool alignOnStart = true;
    [SerializeField] private bool alignEveryLateUpdate = true;

    private Transform targetBone;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachToBlackDragonsInScene()
    {
        Transform[] sceneTransforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform sceneTransform = sceneTransforms[i];
            if (sceneTransform.name != "BlackDragon")
            {
                continue;
            }

            if (sceneTransform.GetComponent<DragonBoneOriginAnchor>() == null)
            {
                sceneTransform.gameObject.AddComponent<DragonBoneOriginAnchor>();
            }
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (alignOnStart)
        {
            AlignBoneToOrigin();
        }
    }

    private void LateUpdate()
    {
        if (alignEveryLateUpdate)
        {
            AlignBoneToOrigin();
        }
    }

    [ContextMenu("Resolve References")]
    private void ResolveReferences()
    {
        targetBone = FindChildByName(transform, boneName);

        if (autoFindVisualRoots)
        {
            visualRoots.Clear();
            FindVisualRoots();
        }
    }

    [ContextMenu("Align Bone To Origin")]
    private void AlignBoneToOrigin()
    {
        if (targetBone == null)
        {
            targetBone = FindChildByName(transform, boneName);
        }

        if (targetBone == null)
        {
            Debug.LogWarning($"[{nameof(DragonBoneOriginAnchor)}] Could not find bone '{boneName}' under '{name}'.", this);
            return;
        }

        if (visualRoots.Count == 0 && autoFindVisualRoots)
        {
            FindVisualRoots();
        }

        Vector3 delta = transform.position - targetBone.position;
        if (delta.sqrMagnitude <= 0.00000001f)
        {
            return;
        }

        for (int i = visualRoots.Count - 1; i >= 0; i--)
        {
            Transform visualRoot = visualRoots[i];
            if (visualRoot == null)
            {
                visualRoots.RemoveAt(i);
                continue;
            }

            visualRoot.position += delta;
        }
    }

    private void FindVisualRoots()
    {
        if (targetBone != null)
        {
            Transform directRoot = GetDirectChildRoot(targetBone);
            if (directRoot != null)
            {
                visualRoots.Add(directRoot);
                return;
            }
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            visualRoots.Add(transform.GetChild(i));
        }
    }

    private Transform GetDirectChildRoot(Transform child)
    {
        Transform current = child;
        while (current != null && current.parent != transform)
        {
            current = current.parent;
        }

        return current != transform ? current : null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
