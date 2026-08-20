using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class EnforceWebGLBuildSettings
    {
        private const int WasmCodeOptimizationRuntimeSpeedLto = 1;

        [InitializeOnLoadMethod]
        private static void Enforce()
        {
            foreach (var profile in Resources.FindObjectsOfTypeAll<UnityEditor.Build.Profile.BuildProfile>())
            {
                var so = new SerializedObject(profile);
                var target = so.FindProperty("m_BuildTarget");
                if (target == null || target.intValue != 20) continue;
                var opt = so.FindProperty("m_PlatformBuildProfile.m_CodeOptimization");
                if (opt == null || opt.intValue == WasmCodeOptimizationRuntimeSpeedLto) continue;
                opt.intValue = WasmCodeOptimizationRuntimeSpeedLto;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
                Debug.Log("[EnforceWebGLBuildSettings] WebGL Code Optimization set to Runtime Speed with LTO");
            }
        }
    }
}
