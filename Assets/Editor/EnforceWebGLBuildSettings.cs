using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class EnforceWebGLBuildSettings
    {
        private const int WasmCodeOptimizationRuntimeSpeedLto = 1;

        // BuildTarget.WebGL
        private const int WebGLBuildTarget = 20;

        [InitializeOnLoadMethod]
        private static void Enforce()
        {
            foreach (UnityEditor.Build.Profile.BuildProfile profile in
                     Resources.FindObjectsOfTypeAll<UnityEditor.Build.Profile.BuildProfile>())
            {
                SerializedObject serialized = new SerializedObject(profile);

                SerializedProperty target = serialized.FindProperty("m_BuildTarget");
                if (target == null || target.intValue != WebGLBuildTarget) continue;

                SerializedProperty optimization = serialized.FindProperty("m_PlatformBuildProfile.m_CodeOptimization");
                if (optimization == null || optimization.intValue == WasmCodeOptimizationRuntimeSpeedLto) continue;

                optimization.intValue = WasmCodeOptimizationRuntimeSpeedLto;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                Debug.Log("[EnforceWebGLBuildSettings] WebGL Code Optimization set to Runtime Speed with LTO");
            }
        }
    }
}