using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    public class Halo : MonoBehaviour
    {
        private const float RingSize = 0.8f;
        private const float PulseSpeed = 0.8f;
        private const float PulseAmount = 0.08f;
        private const float FloorLift = 0.005f;

        private static readonly Color GlowColor = new Color(1f, 0.78f, 0.35f, 1f);

        private static Texture2D ringTexture;

        private Transform ring;
        private Material ringMaterial;
        private Vector3 baseScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ringTexture = null;
        }

        public void Place(Vector3 floorPosition)
        {
            GameObject go = new GameObject("HaloRing");
            go.transform.position = floorPosition + Vector3.up * FloorLift;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            baseScale = new Vector3(RingSize, RingSize, 1f);
            go.transform.localScale = baseScale;
            ring = go.transform;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            ringMaterial = new Material(shader);
            ringMaterial.mainTexture = GetRingTexture();
            ringMaterial.color = GlowColor;

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
        }

        private void Update()
        {
            if (ring == null) return;

            float pulse = 1f + Mathf.Sin(Time.time * PulseSpeed * Mathf.PI * 2f) * PulseAmount;

            ring.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, 1f);

            if (ringMaterial != null)
            {
                Color glow = GlowColor;
                glow.a = Mathf.Clamp01(pulse);
                ringMaterial.color = glow;
            }
        }

        private void OnDestroy()
        {
            if (ring != null) Destroy(ring.gameObject);
            if (ringMaterial != null) Destroy(ringMaterial);
        }

        // Ring drawn in code rather than imported, so there's no art asset to manage
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

                    float innerEdge = Mathf.Clamp01((distance - 0.52f) / 0.05f);
                    float outerEdge = 1f - Mathf.Clamp01((distance - 0.64f) / 0.14f);

                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Min(innerEdge, outerEdge)));
                }
            }

            ringTexture.SetPixels32(pixels);
            ringTexture.Apply(false, true);
            return ringTexture;
        }
    }
}