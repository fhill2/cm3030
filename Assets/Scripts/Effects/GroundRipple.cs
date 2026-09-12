using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    public class GroundRipple : MonoBehaviour
    {
        private const float StartSize = 0.4f;
        private const float FloorLift = 0.02f;

        private static Texture2D ringTexture;

        private Transform ring;
        private Material ringMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ringTexture = null;
        }

        public static GroundRipple Spawn(Vector3 floorPosition, float endSize, float duration, Color color)
        {
            GameObject go = new GameObject("GroundRipple");
            GroundRipple ripple = go.AddComponent<GroundRipple>();
            ripple.Build(floorPosition, endSize, duration, color);
            return ripple;
        }

        private void Build(Vector3 floorPosition, float endSize, float duration, Color color)
        {
            GameObject quad = new GameObject("Ring");
            quad.transform.SetParent(transform, false);
            quad.transform.position = floorPosition + Vector3.up * FloorLift;
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(StartSize, StartSize, 1f);
            ring = quad.transform;

            MeshFilter filter = quad.AddComponent<MeshFilter>();
            filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            ringMaterial = new Material(shader);
            ringMaterial.mainTexture = GetRingTexture();
            ringMaterial.color = color;

            // Additive transparency has to be set up by hand, since the
            // material is made in code rather than in the editor.
            if (ringMaterial.shader.name == "Universal Render Pipeline/Unlit")
            {
                ringMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                ringMaterial.SetOverrideTag("RenderType", "Transparent");
                ringMaterial.SetFloat("_Surface", 1f);
                ringMaterial.SetFloat("_Blend", 2f);
                ringMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                ringMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
                ringMaterial.SetFloat("_ZWrite", 0f);
                ringMaterial.SetFloat("_Cull", 2f);
                ringMaterial.renderQueue = (int)RenderQueue.Transparent;
            }

            renderer.sharedMaterial = ringMaterial;

            StartCoroutine(ExpandRoutine(endSize, duration, color));
        }

        private IEnumerator ExpandRoutine(float endSize, float duration, Color color)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Fast at the start, slowing as it goes, so it reads as an impact.
                float eased = 1f - (1f - progress) * (1f - progress);
                float size = Mathf.Lerp(StartSize, endSize, eased);
                ring.localScale = new Vector3(size, size, 1f);

                Color faded = color;
                faded.a = 1f - progress;
                ringMaterial.color = faded;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (ringMaterial != null) Destroy(ringMaterial);
        }

        // Ring drawn in code rather than imported, so there's no art asset to
        // manage. Built once and shared by every ripple.
        private static Texture2D GetRingTexture()
        {
            if (ringTexture != null) return ringTexture;

            const int size = 256;
            ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float offsetX = (x - half) / half;
                    float offsetY = (y - half) / half;
                    float distance = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);

                    float innerEdge = Mathf.Clamp01((distance - 0.62f) / 0.06f);
                    float outerEdge = 1f - Mathf.Clamp01((distance - 0.72f) / 0.28f);

                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Min(innerEdge, outerEdge)));
                }
            }

            ringTexture.SetPixels32(pixels);
            ringTexture.Apply(false, true);
            return ringTexture;
        }
    }
}