// COPYRIGHT 2025  @Neil Augereau / github : neilaugereau
using UnityEngine;
using UnityEditor;
using System.IO;

public class MissingScriptsFinder
{
    [MenuItem("Tools/Find Missing Scripts/In Scene")]
    static void FindInScene()
    {
        int count = CheckSceneObjects();
        Debug.Log($"[Scene Scan] Missing scripts found: {count}");
    }

    [MenuItem("Tools/Find Missing Scripts/In Prefabs")]
    static void FindInPrefabs()
    {
        int count = CheckAllPrefabs();
        Debug.Log($"[Prefabs Scan] Missing scripts found: {count}");
    }

    [MenuItem("Tools/Find Missing Scripts/Everywhere")]
    static void FindEverywhere()
    {
        int countScene = CheckSceneObjects();
        int countPrefabs = CheckAllPrefabs();
        Debug.Log($"[Full Scan] Scene: {countScene}, Prefabs: {countPrefabs}, Total: {countScene + countPrefabs}");
    }

    static int CheckSceneObjects()
    {
        int count = 0;

        foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
        {
            count += CheckGameObject(go);
        }

        return count;
    }

    static int CheckAllPrefabs()
    {
        int count = 0;
        string[] prefabPaths = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                count += CheckGameObject(instance, path);
                GameObject.DestroyImmediate(instance);
            }
        }

        return count;
    }

    static int CheckGameObject(GameObject go, string assetPath = "")
    {
        int count = 0;
        Component[] components = go.GetComponents<Component>();

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                string path = GetGameObjectPath(go);
                if (!string.IsNullOrEmpty(assetPath))
                    Debug.LogWarning($"[Prefab] Missing script in '{assetPath}' at: {path}", go);
                else
                    Debug.LogWarning($"[Scene] Missing script at: {path}", go);
                count++;
            }
        }

        foreach (Transform child in go.transform)
        {
            count += CheckGameObject(child.gameObject, assetPath);
        }

        return count;
    }

    static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }
        return path;
    }
}
