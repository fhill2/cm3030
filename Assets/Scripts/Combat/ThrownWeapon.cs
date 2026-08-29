using System.Collections;
using UnityEngine;
using Game.Audio;
using Game.Core;
using Game.Shared;

namespace Game.Combat
{
    public class ThrownWeapon : MonoBehaviour
    {
        [Tooltip("Metres per second the weapon flies.")]
        [SerializeField] private float speed = 30f;

        [Tooltip("Radius of the hit check. Slightly generous so near misses still land.")]
        [SerializeField] private float radius = 0.4f;

        [Tooltip("Degrees per second the weapon spins in flight.")]
        [SerializeField] private float spin = 720f;

        [Tooltip("Upper bound of the random roll around the blade axis added on top of the tumble.")]
        [SerializeField] private float rollSpin = 120f;

        [Tooltip("Seconds of flight before it falls, so it never sails off the map.")]
        [SerializeField] private float lifetime = 3f;

        [Tooltip("Downward acceleration while in flight (m/s^2).")]
        [SerializeField] private float gravity = -5f;

        [Header("Sound")]
        [Tooltip("Whoosh clip under Resources/ played while the weapon is airborne.")]
        [SerializeField] private string whooshClip = "Throw/floraphonic-rotate-movement-whoosh-1-185335";

        [Tooltip("Clip under Resources/ played when the weapon strikes an enemy.")]
        [SerializeField] private string hitClip = "Throw/hit/axe-hit-trimmed";

        private GameObject thrower;
        private Vector3 flightDirection = Vector3.forward;
        private float strikeDamage;
        private float roll;
        private float fallSpeed;
        private float launchedAt;
        private bool spent;
        private AudioSource whooshSource;

        public void Launch(GameObject source, Vector3 direction)
        {
            thrower = source;
            flightDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            WeaponDef def = GetComponent<Weapon>()?.Def;
            strikeDamage = def != null && def.Speed > 0f ? def.Damage / def.Speed * 3f : 0f;
            roll = Random.Range(-rollSpin, rollSpin);
            fallSpeed = 0f;
            launchedAt = Time.time;

            AudioClip whoosh = LoadClip(whooshClip);
            if (whoosh != null)
            {
                whooshSource = gameObject.AddComponent<AudioSource>();
                whooshSource.clip = whoosh;
                whooshSource.loop = true;
                whooshSource.spatialBlend = 0f;
                whooshSource.volume = 0.9f;
                whooshSource.Play();
            }
        }

        private void Update()
        {
            if (spent) return;

            transform.Rotate(Vector3.right, spin * Time.deltaTime, Space.Self);
            transform.Rotate(Vector3.up, roll * Time.deltaTime, Space.Self);

            fallSpeed += gravity * Time.deltaTime;

            Vector3 velocity = flightDirection * speed + Vector3.up * fallSpeed;
            float step = velocity.magnitude * Time.deltaTime;

            IDamageable struck = CombatProbe.Sweep(transform.position, velocity.normalized,
                step, radius, thrower != null ? thrower.transform : null);

            if (struck != null)
            {
                struck.TakeDamage(strikeDamage, DamageType.Melee, thrower);
                PlayClip(hitClip);
                transform.position += velocity * Time.deltaTime;
                Fall();
                return;
            }

            transform.position += velocity * Time.deltaTime;

            if (Time.time - launchedAt > lifetime) Fall();
        }

        private static AudioClip LoadClip(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"[ThrownWeapon] No clip at Resources/{path}");
            return clip;
        }

        private static void PlayClip(string path)
        {
            AudioClip clip = LoadClip(path);
            if (clip != null)
                OneShotAudio.Play2D(clip, Vector3.zero);
        }

        private void Fall()
        {
            spent = true;

            if (whooshSource != null)
            {
                whooshSource.Stop();
                Destroy(whooshSource);
                whooshSource = null;
            }

            var gravity = GetComponent<Gravity>();
            if (gravity == null) gravity = gameObject.AddComponent<Gravity>();
            gravity.SelfRighting = true;

            Vector3 lateral = Random.insideUnitSphere;
            lateral.y = 0f;
            lateral = lateral.sqrMagnitude > 0.001f ? lateral.normalized : Vector3.forward;

            gravity.Launch(
                Vector3.up * 2.5f + lateral * 2f,
                Random.insideUnitSphere * 300f,
                thrower != null ? thrower.transform : null);

            StartCoroutine(HaloAfterDelay());
        }

        private IEnumerator HaloAfterDelay()
        {
            yield return new WaitForSeconds(1f);

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f))
                gameObject.AddComponent<Halo>().Place(hit.point);
        }
    }
}
