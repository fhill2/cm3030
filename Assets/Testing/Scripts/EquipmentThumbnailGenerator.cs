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

        // Rig sits far below the scene so it can't appear in the game view.
        private const float RigY = -500f;

        [MenuItem("Tools/Equipment/Generate Thumbnails")]
        public static void GenerateAll()
        {
            try
            {
                IReadOnlyList<EquipmentEntry> entries = EquipmentCatalog.Entries;
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

            GameObject rig = new GameObject("ThumbnailRig");
            rig.transform.position = new Vector3(0f, RigY, 0f);
            rig.hideFlags = HideFlags.HideAndDontSave;

            List<Material> materialCopies = new List<Material>();

            try
            {
                GameObject instance = Object.Instantiate(entry.Prefab, rig.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;

                // Copies, so clamping the shine for the thumbnail doesn't change the real materials.
                foreach (Renderer renderer in renderers)
                {
                    Material[] shared = renderer.sharedMaterials;
                    Material[] copies = new Material[shared.Length];
                    for (int i = 0; i < shared.Length; i++)
                    {
                        copies[i] = shared[i] != null ? Object.Instantiate(shared[i]) : null;
                        if (copies[i] != null && copies[i].HasProperty("_Smoothness"))
                        {
                            float smoothness = copies[i].GetFloat("_Smoothness");
                            copies[i].SetFloat("_Smoothness", Mathf.Min(smoothness, SmoothnessClamp));
                        }
                        if (copies[i] != null) materialCopies.Add(copies[i]);
                    }
                    renderer.sharedMaterials = copies;
                }

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                GameObject cameraGo = new GameObject("ThumbnailCam");
                cameraGo.transform.SetParent(rig.transform);
                cameraGo.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + 1f);
                cameraGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                Camera camera = cameraGo.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, 0.3f) * 1.25f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.enabled = false;
                camera.GetUniversalAdditionalCameraData();

                GameObject keyGo = new GameObject("Key");
                keyGo.transform.SetParent(rig.transform);
                keyGo.transform.rotation = Quaternion.Euler(40f, 200f, 0f);
                Light key = keyGo.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.2f;

                GameObject fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(rig.transform);
                fillGo.transform.rotation = Quaternion.Euler(20f, 20f, 0f);
                Light fill = fillGo.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.4f;
                fill.color = new Color(0.7f, 0.75f, 0.85f);

                RenderTextureDescriptor renderDesc = new RenderTextureDescriptor(RenderSize, RenderSize, GraphicsFormat.R8G8B8A8_SRGB, 24);
                RenderTexture renderTarget = new RenderTexture(renderDesc);

                RenderTextureDescriptor resolveDesc = new RenderTextureDescriptor(RenderSize, RenderSize, GraphicsFormat.R8G8B8A8_SRGB, 0);
                RenderTexture resolveTarget = new RenderTexture(resolveDesc);

                camera.targetTexture = renderTarget;
                camera.Render();
                Graphics.Blit(renderTarget, resolveTarget);

                Texture2D full = new Texture2D(RenderSize, RenderSize, TextureFormat.RGBA32, false);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = resolveTarget;
                full.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0);
                full.Apply();
                RenderTexture.active = previousActive;

                // ReadPixels comes out upside down. Shields need a second flip
                // because they face the other way in their prefabs.
                FlipVertical(full);
                if (entry.Kind == EquipmentKind.Shield) FlipVertical(full);

                // Rendered large and shrunk down, which smooths the edges
                // better than rendering straight to the output size.
                Texture2D output = Downsample(full, OutputSize);
                byte[] png = output.EncodeToPNG();

                string pngPath = Path.ChangeExtension(assetPath, ".png");
                File.WriteAllBytes(pngPath, png);
                AssetDatabase.ImportAsset(pngPath);

                TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                if (importer != null)
                {
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                }

                Object.DestroyImmediate(full);
                Object.DestroyImmediate(output);
                renderTarget.Release();
                resolveTarget.Release();
            }
            finally
            {
                Object.DestroyImmediate(rig);
                foreach (Material material in materialCopies)
                    if (material != null) Object.DestroyImmediate(material);
            }
        }

        private static void FlipVertical(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            int width = texture.width;
            int height = texture.height;
            Color[] flipped = new Color[pixels.Length];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    flipped[(height - 1 - y) * width + x] = pixels[y * width + x];

            texture.SetPixels(flipped);
            texture.Apply();
        }

        // Averages each block of source pixels down to one output pixel.
        private static Texture2D Downsample(Texture2D source, int outputSize)
        {
            int step = source.width / outputSize;
            Texture2D texture = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false);
            Color[] sourcePixels = source.GetPixels();
            Color[] outputPixels = new Color[outputSize * outputSize];
            float weight = 1f / (step * step);

            for (int y = 0; y < outputSize; y++)
            {
                for (int x = 0; x < outputSize; x++)
                {
                    Color total = Color.clear;
                    int baseY = y * step;
                    int baseX = x * step;
                    for (int offsetY = 0; offsetY < step; offsetY++)
                        for (int offsetX = 0; offsetX < step; offsetX++)
                            total += sourcePixels[(baseY + offsetY) * source.width + baseX + offsetX];
                    outputPixels[y * outputSize + x] = total * weight;
                }
            }

            texture.SetPixels(outputPixels);
            texture.Apply();
            return texture;
        }
    }
}
#endif