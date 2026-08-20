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
        var nested = new List<GameObject>();
        CollectOutermostInstanceRoots(root, nested);
        if (nested.Count == 0)
        {
            Debug.Log("[NestedPrefabTools] " + path + ": no nested prefab instances.");
        }
        else
        {
            foreach (GameObject go in nested)
            {
                Object source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                string srcPath = source != null ? AssetDatabase.GetAssetPath(source) : "<unknown>";
                Debug.Log("[NestedPrefabTools] " + path + " -> nested instance at '" + GetHierarchyPath(go.transform) + "' source: " + srcPath);
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
        for (int guard = 0; guard < 100; guard++)
        {
            var nested = new List<GameObject>();
            CollectOutermostInstanceRoots(root, nested);
            if (nested.Count == 0) break;
            for (int i = nested.Count - 1; i >= 0; i--)
            {
                GameObject go = nested[i];
                if (go == null) continue;
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                {
                    Object source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    string srcPath = source != null ? AssetDatabase.GetAssetPath(source) : "<unknown>";
                    Debug.Log("[NestedPrefabTools] Unpacking " + srcPath + " inside " + path);
                    PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
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
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                instanceParts++;
        Cloth[] cloths = root.GetComponentsInChildren<Cloth>(true);
        var sb = new StringBuilder();
        sb.Append("[NestedPrefabTools] Verify ").Append(path)
          .Append(": remaining instance parts=").Append(instanceParts)
          .Append(", Cloth components=").Append(cloths.Length);
        for (int i = 0; i < cloths.Length; i++)
            sb.Append(" [").Append(cloths[i].gameObject.name).Append(" enabled=").Append(cloths[i].enabled).Append("]");
        Debug.Log(sb.ToString());
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static int StripMissingScripts(GameObject root)
    {
        int total = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (count > 0)
                total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
        return total;
    }

    private static List<string> ListRootPrefabs()
    {
        var result = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(p).Replace('\\', '/') == "Assets/Prefabs")
                result.Add(p);
        }
        return result;
    }

    private static void CollectOutermostInstanceRoots(GameObject go, List<GameObject> acc)
    {
        if (PrefabUtility.IsOutermostPrefabInstanceRoot(go))
            acc.Add(go);
        for (int i = 0; i < go.transform.childCount; i++)
            CollectOutermostInstanceRoots(go.transform.GetChild(i).gameObject, acc);
    }

    private static string GetHierarchyPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
