#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Game.Combat;

namespace Game.Core
{
    public static class EquipmentThumbnailGenerator
    {
        private const int OutputSize = 512;
        private const int RenderSize = 2048;
        private const float SmoothnessClamp = 0.35f;
        private const float RigY = -500f;

        [MenuItem("Tools/Equipment/Generate Thumbnails")]
        public static void GenerateAll()
        {
            try
            {
                var entries = EquipmentCatalog.Entries;
                if (entries.Count == 0)
                {
                    Debug.LogWarning("[Thumbnails] Equipment catalog is empty.");
                    return;
                }

                int done = 0;
                foreach (EquipmentEntry entry in entries)
                {
                    EditorUtility.DisplayProgressBar("Equipment Thumbnails", entry.Name,
                        done / (float)entries.Count);
                    GenerateOne(entry);
                    done++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[Thumbnails] Generated {done} thumbnails next to their prefabs.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void GenerateOne(EquipmentEntry entry)
        {
            if (entry.Prefab == null) return;

            string assetPath = AssetDatabase.GetAssetPath(entry.Prefab);
            if (string.IsNullOrEmpty(assetPath)) return;

            var rig = new GameObject("ThumbnailRig");
            rig.transform.position = new Vector3(0f, RigY, 0f);
            rig.hideFlags = HideFlags.HideAndDontSave;

            var materialCopies = new List<Material>();

            try
            {
                GameObject instance = Object.Instantiate(entry.Prefab, rig.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;

                foreach (var r in renderers)
                {
                    var shared = r.sharedMaterials;
                    var copies = new Material[shared.Length];
                    for (int i = 0; i < shared.Length; i++)
                    {
                        copies[i] = shared[i] != null ? Object.Instantiate(shared[i]) : null;
                        if (copies[i] != null && copies[i].HasProperty("_Smoothness"))
                        {
                            float s = copies[i].GetFloat("_Smoothness");
                            copies[i].SetFloat("_Smoothness", Mathf.Min(s, SmoothnessClamp));
                        }
                        if (copies[i] != null) materialCopies.Add(copies[i]);
                    }
                    r.sharedMaterials = copies;
                }

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                var camGo = new GameObject("ThumbnailCam");
                camGo.transform.SetParent(rig.transform);
                camGo.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + 1f);
                camGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, 0.3f) * 1.25f;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 5f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.enabled = false;
                cam.GetUniversalAdditionalCameraData();

                var keyGo = new GameObject("Key");
                keyGo.transform.SetParent(rig.transform);
                keyGo.transform.rotation = Quaternion.Euler(40f, 200f, 0f);
                var key = keyGo.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.2f;

                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(rig.transform);
                fillGo.transform.rotation = Quaternion.Euler(20f, 20f, 0f);
                var fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.4f;
                fill.color = new Color(0.7f, 0.75f, 0.85f);

                var renderDesc = new RenderTextureDescriptor(RenderSize, RenderSize, GraphicsFormat.R8G8B8A8_SRGB, 24);
                var renderRt = new RenderTexture(renderDesc);

                var resolveDesc = new RenderTextureDescriptor(RenderSize, RenderSize, GraphicsFormat.R8G8B8A8_SRGB, 0);
                var resolveRt = new RenderTexture(resolveDesc);

                cam.targetTexture = renderRt;
                cam.Render();
                Graphics.Blit(renderRt, resolveRt);

                var full = new Texture2D(RenderSize, RenderSize, TextureFormat.RGBA32, false);
                var prevActive = RenderTexture.active;
                RenderTexture.active = resolveRt;
                full.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0);
                full.Apply();
                RenderTexture.active = prevActive;
                FlipVertical(full);
                if (entry.Kind == EquipmentKind.Shield) FlipVertical(full);

                Texture2D output = Downsample(full, OutputSize);
                byte[] png = output.EncodeToPNG();

                string pngPath = Path.ChangeExtension(assetPath, ".png");
                File.WriteAllBytes(pngPath, png);
                AssetDatabase.ImportAsset(pngPath);

                var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                if (importer != null)
                {
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                }

                Object.DestroyImmediate(full);
                Object.DestroyImmediate(output);
                renderRt.Release();
                resolveRt.Release();
            }
            finally
            {
                Object.DestroyImmediate(rig);
                foreach (Material m in materialCopies)
                    if (m != null) Object.DestroyImmediate(m);
            }
        }

        private static void FlipVertical(Texture2D tex)
        {
            var pixels = tex.GetPixels();
            int w = tex.width;
            int h = tex.height;
            var flipped = new Color[pixels.Length];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    flipped[(h - 1 - y) * w + x] = pixels[y * w + x];

            tex.SetPixels(flipped);
            tex.Apply();
        }

        private static Texture2D Downsample(Texture2D source, int outSize)
        {
            int step = source.width / outSize;
            var tex = new Texture2D(outSize, outSize, TextureFormat.RGBA32, false);
            var src = source.GetPixels();
            var dst = new Color[outSize * outSize];
            float norm = 1f / (step * step);

            for (int y = 0; y < outSize; y++)
            {
                for (int x = 0; x < outSize; x++)
                {
                    Color acc = Color.clear;
                    int baseY = y * step;
                    int baseX = x * step;
                    for (int dy = 0; dy < step; dy++)
                        for (int dx = 0; dx < step; dx++)
                            acc += src[(baseY + dy) * source.width + baseX + dx];
                    dst[y * outSize + x] = acc * norm;
                }
            }

            tex.SetPixels(dst);
            tex.Apply();
            return tex;
        }
    }
}
#endif
