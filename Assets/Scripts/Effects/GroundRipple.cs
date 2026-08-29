using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    public class GroundRipple : MonoBehaviour
    {
        private const float StartSize = 0.4f;
        private const float FloorLift = 0.02f;

        private static Texture2D s_ringTexture;

        private Transform ring;
        private Material ringMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_ringTexture = null;
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

            var filter = quad.AddComponent<MeshFilter>();
            filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var renderer = quad.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            ringMaterial = new Material(shader);
            ringMaterial.mainTexture = GetRingTexture();
            ringMaterial.color = color;

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
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float eased = 1f - (1f - k) * (1f - k);
                float size = Mathf.Lerp(StartSize, endSize, eased);
                ring.localScale = new Vector3(size, size, 1f);

                Color c = color;
                c.a = 1f - k;
                ringMaterial.color = c;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (ringMaterial != null) Destroy(ringMaterial);
        }

        private static Texture2D GetRingTexture()
        {
            if (s_ringTexture != null) return s_ringTexture;

            const int size = 256;
            s_ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    float innerEdge = Mathf.Clamp01((r - 0.62f) / 0.06f);
                    float outerEdge = 1f - Mathf.Clamp01((r - 0.72f) / 0.28f);

                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Min(innerEdge, outerEdge)));
                }
            }

            s_ringTexture.SetPixels32(pixels);
            s_ringTexture.Apply(false, true);
            return s_ringTexture;
        }
    }
}
