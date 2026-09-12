using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class NestedPrefabTools
{
    private static readonly string[] Targets =
    {
        "Assets/Prefabs/fighter_enemy.prefab",
        "Assets/Prefabs/fighter_enemy_chain.prefab",
        "Assets/Prefabs/fighter_player_knight.prefab"
    };

    [MenuItem("Tools/Prefabs/Report Nested Prefab Links")]
    public static void ReportAll()
    {
        foreach (string path in ListRootPrefabs())
            Report(path);
    }

    [MenuItem("Tools/Prefabs/Unpack Nested Prefab Links")]
    public static void UnpackAll()
    {
        foreach (string path in Targets)
            Unpack(path);
        foreach (string path in Targets)
            Verify(path);
    }

    public static void Report(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning("[NestedPrefabTools] Could not load " + path);
            return;
        }

        List<GameObject> nested = new List<GameObject>();
        CollectOutermostInstanceRoots(root, nested);

        if (nested.Count == 0)
        {
            Debug.Log("[NestedPrefabTools] " + path + ": no nested prefab instances.");
        }
        else
        {
            foreach (GameObject instance in nested)
            {
                Object source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : "<unknown>";
                Debug.Log("[NestedPrefabTools] " + path + " -> nested instance at '" + GetHierarchyPath(instance.transform) + "' source: " + sourcePath);
            }
        }

        PrefabUtility.UnloadPrefabContents(root);
    }

    public static bool Unpack(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning("[NestedPrefabTools] Could not load " + path);
            return false;
        }

        int unpacked = 0;

        // Unpacking one level can expose another underneath, so this repeats.
        // The guard stops it looping forever if something never resolves.
        for (int guard = 0; guard < 100; guard++)
        {
            List<GameObject> nested = new List<GameObject>();
            CollectOutermostInstanceRoots(root, nested);
            if (nested.Count == 0) break;

            for (int i = nested.Count - 1; i >= 0; i--)
            {
                GameObject instance = nested[i];
                if (instance == null) continue;
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(instance))
                {
                    Object source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
                    string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : "<unknown>";
                    Debug.Log("[NestedPrefabTools] Unpacking " + sourcePath + " inside " + path);
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    unpacked++;
                }
            }
        }

        bool changed = unpacked > 0;
        if (changed)
        {
            int stripped = StripMissingScripts(root);
            if (stripped > 0)
                Debug.Log("[NestedPrefabTools] Removed " + stripped + " missing script component(s) from " + path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[NestedPrefabTools] Saved " + path + " (" + unpacked + " nested instances unpacked).");
        }
        else
        {
            Debug.Log("[NestedPrefabTools] " + path + ": nothing to unpack.");
        }

        PrefabUtility.UnloadPrefabContents(root);
        return changed;
    }

    public static void Verify(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null) return;

        int instanceParts = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (PrefabUtility.IsPartOfPrefabInstance(child.gameObject))
                instanceParts++;

        Cloth[] cloths = root.GetComponentsInChildren<Cloth>(true);

        StringBuilder report = new StringBuilder();
        report.Append("[NestedPrefabTools] Verify ").Append(path)
              .Append(": remaining instance parts=").Append(instanceParts)
              .Append(", Cloth components=").Append(cloths.Length);
        for (int i = 0; i < cloths.Length; i++)
            report.Append(" [").Append(cloths[i].gameObject.name).Append(" enabled=").Append(cloths[i].enabled).Append("]");

        Debug.Log(report.ToString());
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static int StripMissingScripts(GameObject root)
    {
        int total = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
            if (count > 0)
                total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }
        return total;
    }

    // Only prefabs directly in Assets/Prefabs, not in its subfolders.
    private static List<string> ListRootPrefabs()
    {
        List<string> result = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(assetPath).Replace('\\', '/') == "Assets/Prefabs")
                result.Add(assetPath);
        }
        return result;
    }

    private static void CollectOutermostInstanceRoots(GameObject go, List<GameObject> results)
    {
        if (PrefabUtility.IsOutermostPrefabInstanceRoot(go))
            results.Add(go);
        for (int i = 0; i < go.transform.childCount; i++)
            CollectOutermostInstanceRoots(go.transform.GetChild(i).gameObject, results);
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }
        return path;
    }
}