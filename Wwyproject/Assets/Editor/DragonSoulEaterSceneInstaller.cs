using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DragonSoulEaterSceneInstaller
{
    private const string ScenePath = "Assets/Sun_Temple/Scenes/DemoScene.unity";
    private const string SoulEaterRedPrefabPath = "Assets/FourEvilDragonsPBR/Prefab/DragonSoulEater/Red.prefab";
    private const string DragonName = "BlackDragon";

    [MenuItem("Tools/Dragon/Install SoulEater Red In DemoScene")]
    public static void InstallSoulEaterRedInDemoScene()
    {
        EditorSceneManager.OpenScene(ScenePath);

        GameObject oldDragon = GameObject.Find(DragonName);
        Vector3 position = oldDragon != null ? oldDragon.transform.position : Vector3.zero;
        Quaternion rotation = oldDragon != null ? oldDragon.transform.rotation : Quaternion.identity;
        Vector3 scale = oldDragon != null ? oldDragon.transform.localScale : Vector3.one;

        SerializedObject oldDragonExample = null;
        DragonExample oldExampleComponent = oldDragon != null ? oldDragon.GetComponent<DragonExample>() : null;
        if (oldExampleComponent != null)
        {
            oldDragonExample = new SerializedObject(oldExampleComponent);
        }

        if (oldDragon != null)
        {
            Undo.DestroyObjectImmediate(oldDragon);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoulEaterRedPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Could not load SoulEater prefab at {SoulEaterRedPrefabPath}.");
            return;
        }

        GameObject dragon = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(dragon, "Install SoulEater Red Dragon");
        dragon.name = DragonName;
        dragon.transform.SetPositionAndRotation(position, rotation);
        dragon.transform.localScale = scale;

        DragonBoneOriginAnchor anchor = dragon.GetComponent<DragonBoneOriginAnchor>();
        if (anchor != null)
        {
            Object.DestroyImmediate(anchor);
        }

        DragonExample dragonExample = dragon.GetComponent<DragonExample>();
        if (dragonExample == null)
        {
            dragonExample = Undo.AddComponent<DragonExample>(dragon);
        }

        CopySharedDragonExampleSettings(oldDragonExample, dragonExample);
        ApplySoulEaterDragonExampleDefaults(dragonExample);

        Rigidbody rigidbody = dragon.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = Undo.AddComponent<Rigidbody>(dragon);
        }

        rigidbody.mass = 500f;
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationZ;

        BoxCollider collider = dragon.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = Undo.AddComponent<BoxCollider>(dragon);
        }

        FitColliderToRenderers(dragon, collider);

        EditorUtility.SetDirty(dragon);
        EditorSceneManager.MarkSceneDirty(dragon.scene);
        EditorSceneManager.SaveScene(dragon.scene);
        AssetDatabase.Refresh();

        Debug.Log($"Installed {SoulEaterRedPrefabPath} as {DragonName} in {ScenePath}.");
    }

    private static void CopySharedDragonExampleSettings(SerializedObject oldDragonExample, DragonExample newDragonExample)
    {
        if (oldDragonExample == null || newDragonExample == null)
        {
            return;
        }

        SerializedObject newSerializedObject = new SerializedObject(newDragonExample);
        string[] sharedFields =
        {
            "wakeDistance",
            "mountDistance",
            "mountOffset",
            "dismountOffset",
            "groundMoveSpeed",
            "airMoveSpeed",
            "yawSpeed",
            "groundBrake",
            "takeoffRiseHeight",
            "takeoffDuration",
            "takeoffToHoverNormalizedTime",
            "pitchSpeed",
            "maxPitchAngle",
            "landingDescendSpeed",
            "landingGroundEpsilon",
            "groundCheckDistance",
            "groundMask",
            "riderRootOverride",
            "riderObjectName",
            "mainRideCameraOverride",
            "firstPersonCameraOverride",
            "mainRideCameraName",
            "followCameraOffset",
            "followLookAtOffset",
            "followSmooth"
        };

        foreach (string field in sharedFields)
        {
            SerializedProperty oldProperty = oldDragonExample.FindProperty(field);
            SerializedProperty newProperty = newSerializedObject.FindProperty(field);
            if (oldProperty != null && newProperty != null)
            {
                newProperty.serializedObject.CopyFromSerializedProperty(oldProperty);
            }
        }

        newSerializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplySoulEaterDragonExampleDefaults(DragonExample dragonExample)
    {
        SerializedObject serializedObject = new SerializedObject(dragonExample);
        SetFloat(serializedObject, "wakeDistance", 18f);
        SetFloat(serializedObject, "runMoveSpeed", 14f);
        SetFloat(serializedObject, "airGlideMoveSpeed", 24f);
        SetFloat(serializedObject, "attackLockDuration", 1.1f);
        SetFloat(serializedObject, "defendRefreshInterval", 0.25f);
        SetString(serializedObject, "sleepState", "Sleep");
        SetString(serializedObject, "idleState", "Idle");
        SetString(serializedObject, "screamState", "Scream");
        SetString(serializedObject, "walkState", "Walk");
        SetString(serializedObject, "runState", "Run");
        SetString(serializedObject, "takeOffState", "Take Off");
        SetString(serializedObject, "flyForwardState", "Fly Forward");
        SetString(serializedObject, "flyGlideState", "Fly Glide");
        SetString(serializedObject, "flyFloatState", "Fly Float");
        SetString(serializedObject, "landState", "Land");
        SetString(serializedObject, "defendState", "Defend");
        SetString(serializedObject, "flyFireballState", "Fly Fireball Shoot");
        SetInt(serializedObject, "idleLoopsBeforeScream", 2);
        SetFloat(serializedObject, "screamReturnNormalizedTime", 0.95f);

        SerializedProperty attacks = serializedObject.FindProperty("groundAttackStates");
        if (attacks != null)
        {
            attacks.arraySize = 3;
            attacks.GetArrayElementAtIndex(0).stringValue = "Basic Attack";
            attacks.GetArrayElementAtIndex(1).stringValue = "Tail Attack";
            attacks.GetArrayElementAtIndex(2).stringValue = "Fireball Shoot";
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FitColliderToRenderers(GameObject dragon, BoxCollider collider)
    {
        Renderer[] renderers = dragon.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            collider.center = new Vector3(0f, 2.5f, 0f);
            collider.size = new Vector3(4f, 5f, 9f);
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localCenter = dragon.transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = new Vector3(
            SafeDivide(worldBounds.size.x, dragon.transform.lossyScale.x),
            SafeDivide(worldBounds.size.y, dragon.transform.lossyScale.y),
            SafeDivide(worldBounds.size.z, dragon.transform.lossyScale.z));

        collider.center = localCenter;
        collider.size = localSize;
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) < 0.0001f ? value : value / divisor;
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }
}
