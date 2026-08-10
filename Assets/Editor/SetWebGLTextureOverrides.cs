using UnityEngine;
using UnityEditor;

public static class SetWebGLTextureOverrides
{
    private static readonly string[] Roots =
    {
        "Assets/The_Modular_Medieval_Castle/Textures",
        "Assets/fighter"
    };
    private const int MaxSize = 512;

    [MenuItem("Tools/WebGL/Set WebGL Texture Overrides (Castle + Characters)")]
    public static void Apply()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", Roots);
        if (guids.Length == 0)
        {
            Debug.LogWarning("[WebGLTexOverride] No textures found under " + string.Join(", ", Roots));
            return;
        }

        int newlyOverridden = 0, skipped = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { skipped++; continue; }

            var format = importer.DoesSourceTextureHaveAlpha()
                ? TextureImporterFormat.ETC2_RGBA8
                : TextureImporterFormat.ETC_RGB4;

            var s = importer.GetPlatformTextureSettings("WebGL");
            bool wasOverridden = s.overridden;
            s.name = "WebGL";
            s.overridden = true;
            s.maxTextureSize = MaxSize;
            s.format = format;
            s.textureCompression = TextureImporterCompression.Compressed;
            s.compressionQuality = 50;
            s.crunchedCompression = false;
            s.allowsAlphaSplitting = false;

            importer.SetPlatformTextureSettings(s);
            importer.SaveAndReimport();
            if (!wasOverridden) newlyOverridden++;

            if (i % 10 == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Setting WebGL texture overrides", path, (float)i / guids.Length))
                    break;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        Debug.Log($"[WebGLTexOverride] Done. Total={guids.Length}, newly-overridden={newlyOverridden}, skipped={skipped}. " +
                  $"WebGL textures now ETC2 @ {MaxSize}. Rebuild and check the data file size.");
    }
}
