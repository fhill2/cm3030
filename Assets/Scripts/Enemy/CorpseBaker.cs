using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Enemy
{
    public class CorpseBaker : MonoBehaviour
    {
        [Tooltip("Seconds after death to wait before baking.")]
        [SerializeField] private float bakeDelay = 8f;

        private bool baked;

        void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity != gameObject || baked) return;
            baked = true;
            StartCoroutine(BakeRoutine());
        }

        private IEnumerator BakeRoutine()
        {
            yield return new WaitForSeconds(bakeDelay);

            LODGroup lodGroup = GetComponent<LODGroup>();
            if (lodGroup != null) Destroy(lodGroup);

            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var doomed = new List<GameObject>();
            int bakedCount = 0;
            foreach (SkinnedMeshRenderer smr in smrs)
            {
                if (!smr.enabled || smr.sharedMesh == null)
                {
                    doomed.Add(smr.gameObject);
                    continue;
                }

                Mesh mesh = new Mesh();
                smr.BakeMesh(mesh);
                mesh.name = smr.name + "_corpse";

                GameObject replacement = new GameObject(smr.name + "_Corpse");
                replacement.transform.SetParent(smr.transform, false);
                var mf = replacement.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                var mr = replacement.AddComponent<MeshRenderer>();
                mr.sharedMaterials = smr.sharedMaterials;
                mr.shadowCastingMode = smr.shadowCastingMode;
                mr.receiveShadows = smr.receiveShadows;

                Cloth cloth = smr.GetComponent<Cloth>();
                if (cloth != null) Destroy(cloth);
                Destroy(smr);
                bakedCount++;
            }

            foreach (GameObject go in doomed)
                if (go != null) Destroy(go);

            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null) Destroy(animator);

            foreach (Collider col in GetComponentsInChildren<Collider>(true))
                col.enabled = false;
            foreach (CharacterController cc in GetComponentsInChildren<CharacterController>(true))
                cc.enabled = false;

            Debug.Log($"[CorpseBaker] {name}: baked {bakedCount} mesh(es), removed {doomed.Count} inactive mesh objects.");
        }
    }
}
