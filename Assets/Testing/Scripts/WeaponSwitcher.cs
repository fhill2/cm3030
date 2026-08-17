using UnityEngine;
using Game.Combat;

namespace Game.Core
{
    public class WeaponSwitcher : MonoBehaviour
    {
        private Equipment player;
        private Equipment enemy;
        private GameObject playerDefaultWeapon;
        private GameObject playerDefaultShield;
        private GameObject enemyDefaultWeapon;
        private GameObject enemyDefaultShield;

        public Equipment Player()
        {
            if (player == null)
            {
                GameObject go = GameObject.FindGameObjectWithTag("Player");
                if (go != null)
                {
                    player = go.GetComponent<Equipment>();
                    if (player != null && playerDefaultWeapon == null)
                    {
                        playerDefaultWeapon = player.WeaponPrefab;
                        playerDefaultShield = player.ShieldPrefab;
                    }
                }
            }
            return player;
        }

        public Equipment Enemy()
        {
            if (enemy == null)
            {
                GameObject go = GameObject.FindGameObjectWithTag("Enemy");
                if (go != null)
                {
                    enemy = go.GetComponent<Equipment>();
                    if (enemy != null && enemyDefaultWeapon == null)
                    {
                        enemyDefaultWeapon = enemy.WeaponPrefab;
                        enemyDefaultShield = enemy.ShieldPrefab;
                    }
                }
            }
            return enemy;
        }

        public GameObject PlayerDefaultWeapon => playerDefaultWeapon;
        public GameObject PlayerDefaultShield => playerDefaultShield;
        public GameObject EnemyDefaultWeapon => enemyDefaultWeapon;
        public GameObject EnemyDefaultShield => enemyDefaultShield;

        public void SelectPlayerWeapon(GameObject prefab)
        {
            Equipment equipment = Player();
            if (equipment != null) equipment.EquipWeapon(prefab);
        }

        public void SelectPlayerShield(GameObject prefab)
        {
            Equipment equipment = Player();
            if (equipment != null) equipment.EquipShield(prefab);
        }

        public void SelectEnemyWeapon(GameObject prefab)
        {
            Equipment equipment = Enemy();
            if (equipment != null) equipment.EquipWeapon(prefab);
        }

        public void SelectEnemyShield(GameObject prefab)
        {
            Equipment equipment = Enemy();
            if (equipment != null) equipment.EquipShield(prefab);
        }
    }
}
