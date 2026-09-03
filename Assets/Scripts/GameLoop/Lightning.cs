using System.Collections;
using UnityEngine;

namespace Game.Core
{
    // Flashes the sky at random intervals to go with the rain.
    // Drives the scene's directional light so the whole world lights up.
    // Raises OnLightning for Freddie's thunder audio.
    //
    // Goes on the GameManager object.
    public class Lightning : MonoBehaviour
    {
        [Tooltip("Leave empty to find the directional light automatically.")]
        [SerializeField] private Light sunLight;

        [SerializeField] private float minInterval = 5f;
        [SerializeField] private float maxInterval = 20f;
        [SerializeField] private float flashIntensity = 3f;
        [SerializeField] private float flashDuration = 0.35f;

        [Tooltip("Colour of the light during the flash.")]
        [SerializeField] private Color flashColour = new Color(1f, 0.96f, 0.82f);

        [Tooltip("How much brighter the environment lighting goes. This is what lifts the sky, since a directional light doesn't touch the skybox.")]
        [SerializeField] private float ambientBoost = 2.5f;

        [Tooltip("Seconds between the flash and the thunder clap.")]
        [SerializeField] private float thunderDelay = 1.5f;

        private float baseIntensity;
        private Color baseColour;
        private float baseAmbient;

        private IEnumerator Start()
        {
            if (sunLight == null)
            {
                foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.type == LightType.Directional) { sunLight = light; break; }
            }

            if (sunLight == null) yield break;

            baseIntensity = sunLight.intensity;
            baseColour = sunLight.color;
            baseAmbient = RenderSettings.ambientIntensity;

            while (true)
            {
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

                EventManager.RaiseLightning(new LightningArgs(thunderDelay));

                // Snap to full brightness, then fade. The sharp start is what
                // makes it read as a strike rather than a pulse.
                sunLight.intensity = flashIntensity;
                sunLight.color = flashColour;
                RenderSettings.ambientIntensity = baseAmbient * ambientBoost;

                for (float t = 0f; t < flashDuration; t += Time.deltaTime)
                {
                    float k = t / flashDuration;
                    sunLight.intensity = Mathf.Lerp(flashIntensity, baseIntensity, k);
                    sunLight.color = Color.Lerp(flashColour, baseColour, k);
                    RenderSettings.ambientIntensity =
                        Mathf.Lerp(baseAmbient * ambientBoost, baseAmbient, k);
                    yield return null;
                }

                sunLight.intensity = baseIntensity;
                sunLight.color = baseColour;
                RenderSettings.ambientIntensity = baseAmbient;
            }
        }
    }
}