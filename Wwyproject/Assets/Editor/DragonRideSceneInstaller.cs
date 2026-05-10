using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DragonRideSceneInstaller
{
    private const string TargetScenePath = "Assets/Sun_Temple/Scenes/DemoScene.unity";
    private const string DragonPrefabPath = "Assets/RedDragon 1.2/Assets/Prefab/BlackDragon.prefab";
    private const string FpsPrefabPath = "Assets/Standard Assets/Characters/FirstPersonCharacter/Prefabs/FPSController.prefab";

    [MenuItem("Tools/Dragon Ride/Install In Sun Temple DemoScene")]
    public static void InstallInSunTempleDemoScene()
    {
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            throw new InvalidOperationException($"Could not open scene: {TargetScenePath}");
        }

        GameObject sunTempleController = FindRootObject(scene, "FpsController");
        Vector3 spawnPosition = sunTempleController != null ? sunTempleController.transform.position : new Vector3(-45.3f, 9.8f, -215.4f);
        Quaternion spawnRotation = sunTempleController != null ? sunTempleController.transform.rotation : Quaternion.Euler(0f, -15f, 0f);

        GameObject fpsController = FindRootObject(scene, "FPSController");
        if (fpsController == null)
        {
            fpsController = InstantiatePrefab(FpsPrefabPath);
            fpsController.name = "FPSController";
        }

        fpsController.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        fpsController.SetActive(true);
        SetTagIfExists(fpsController, "Player");

        GameObject fpsCameraObject = FindFirstCameraObject(fpsController);
        Camera fpsCamera = fpsCameraObject != null ? fpsCameraObject.GetComponent<Camera>() : null;
        if (fpsCamera != null)
        {
            fpsCamera.enabled = true;
            AudioListener fpsAudioListener = fpsCamera.GetComponent<AudioListener>();
            if (fpsAudioListener != null)
            {
                fpsAudioListener.enabled = true;
            }
        }

        Camera sourceSunCamera = sunTempleController != null ? sunTempleController.GetComponentInChildren<Camera>(true) : null;
        if (sunTempleController != null)
        {
            sunTempleController.SetActive(false);
        }

        GameObject dragon = FindRootObject(scene, "BlackDragon");
        if (dragon == null)
        {
            dragon = InstantiatePrefab(DragonPrefabPath);
            dragon.name = "BlackDragon";
        }

        Vector3 flatForward = spawnRotation * Vector3.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f)
        {
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();
        Vector3 dragonPosition = spawnPosition + flatForward * 4f;
        dragonPosition.y = spawnPosition.y;
        dragon.transform.SetPositionAndRotation(dragonPosition, spawnRotation);
        dragon.SetActive(true);

        ConfigureDragonExample(dragon, fpsController.transform, fpsCamera);

        if (dragon.GetComponent<DragonBoneOriginAnchor>() == null)
        {
            dragon.AddComponent<DragonBoneOriginAnchor>();
        }

        GameObject rideCameraObject = GameObject.Find("long");
        if (rideCameraObject == null)
        {
            rideCameraObject = new GameObject("long");
            Undo.RegisterCreatedObjectUndo(rideCameraObject, "Create dragon ride camera");
        }

        Camera rideCamera = rideCameraObject.GetComponent<Camera>();
        if (rideCamera == null)
        {
            rideCamera = rideCameraObject.AddComponent<Camera>();
        }

        AudioListener rideAudioListener = rideCameraObject.GetComponent<AudioListener>();
        if (rideAudioListener == null)
        {
            rideAudioListener = rideCameraObject.AddComponent<AudioListener>();
        }

        rideCameraObject.transform.SetPositionAndRotation(
            dragon.transform.TransformPoint(new Vector3(0f, 6f, -8f)),
            Quaternion.LookRotation((dragon.transform.TransformPoint(new Vector3(0f, 6f, 1f)) - dragon.transform.TransformPoint(new Vector3(0f, 6f, -8f))).normalized, Vector3.up));
        rideCamera.enabled = false;
        rideAudioListener.enabled = false;
        rideCameraObject.SetActive(true);

        CopyPostProcessing(sourceSunCamera, fpsCamera);
        CopyPostProcessing(sourceSunCamera, rideCamera);
        ConfigureDragonExample(dragon, fpsController.transform, fpsCamera, rideCamera);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Installed RedDragon ride setup into Sun_Temple DemoScene.");
    }

    private static GameObject InstantiatePrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException($"Missing prefab: {prefabPath}");
        }

        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                return root;
            }
        }

        return null;
    }

    private static GameObject FindFirstCameraObject(GameObject root)
    {
        Camera camera = root.GetComponentInChildren<Camera>(true);
        return camera != null ? camera.gameObject : null;
    }

    private static void SetTagIfExists(GameObject target, string tagName)
    {
        string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i] == tagName)
            {
                target.tag = tagName;
                return;
            }
        }
    }

    private static void ConfigureDragonExample(GameObject dragon, Transform rider, Camera firstPersonCamera, Camera rideCamera = null)
    {
        MonoBehaviour dragonExample = dragon.GetComponent("DragonExample") as MonoBehaviour;
        if (dragonExample == null)
        {
            throw new InvalidOperationException("BlackDragon does not have DragonExample.");
        }

        SerializedObject serialized = new SerializedObject(dragonExample);
        SetFloat(serialized, "mountDistance", 6f);
        SetVector3(serialized, "mountOffset", new Vector3(0f, 2.5f, -1.5f));
        SetVector3(serialized, "dismountOffset", new Vector3(0f, 2.5f, -1.5f));
        SetFloat(serialized, "groundMoveSpeed", 10f);
        SetFloat(serialized, "airMoveSpeed", 10f);
        SetFloat(serialized, "yawSpeed", 10f);
        SetFloat(serialized, "takeoffRiseHeight", 20f);
        SetString(serialized, "riderObjectName", "FPSController");
        SetString(serialized, "mainRideCameraName", "long");
        SetObject(serialized, "riderRootOverride", rider);
        SetObject(serialized, "firstPersonCameraOverride", firstPersonCamera);
        SetObject(serialized, "mainRideCameraOverride", rideCamera);
        SetVector3(serialized, "followCameraOffset", new Vector3(0f, 6f, -8f));
        SetVector3(serialized, "followLookAtOffset", new Vector3(0f, 6f, 1f));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyPostProcessing(Camera source, Camera destination)
    {
        if (source == null || destination == null)
        {
            return;
        }

        foreach (Component sourceComponent in source.GetComponents<Component>())
        {
            if (sourceComponent == null || sourceComponent is Transform || sourceComponent is Camera || sourceComponent is AudioListener)
            {
                continue;
            }

            Type sourceType = sourceComponent.GetType();
            if (sourceType.FullName != "UnityEngine.PostProcessing.PostProcessingBehaviour")
            {
                continue;
            }

            Component existing = destination.GetComponent(sourceType);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            ComponentUtility.CopyComponent(sourceComponent);
            ComponentUtility.PasteComponentAsNew(destination.gameObject);
        }
    }

    private static void SetFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetString(SerializedObject serialized, string propertyName, string value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetVector3(SerializedObject serialized, string propertyName, Vector3 value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.vector3Value = value;
        }
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }
}
