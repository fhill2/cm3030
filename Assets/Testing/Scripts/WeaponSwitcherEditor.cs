#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Combat;
using Game.Core;

[CustomEditor(typeof(WeaponSwitcher))]
public class WeaponSwitcherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WeaponSwitcher switcher = (WeaponSwitcher)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to switch equipment.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
        Equipment player = switcher.Player();
        if (player == null)
            EditorGUILayout.HelpBox("No Player-tagged object with Equipment found.", MessageType.Warning);
        else
        {
            DrawPicker("Weapon", EquipmentCatalog.Weapons, player.WeaponPrefab,
                switcher.PlayerDefaultWeapon, switcher.SelectPlayerWeapon);
            DrawPicker("Shield", EquipmentCatalog.Shields, player.ShieldPrefab,
                switcher.PlayerDefaultShield, switcher.SelectPlayerShield);
        }

        EditorGUILayout.Space(10f);

        EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel);
        Equipment enemy = switcher.Enemy();
        if (enemy == null)
            EditorGUILayout.HelpBox("No Enemy-tagged object with Equipment found.", MessageType.Warning);
        else
        {
            DrawPicker("Weapon", EquipmentCatalog.Weapons, enemy.WeaponPrefab,
                switcher.EnemyDefaultWeapon, switcher.SelectEnemyWeapon);
            DrawPicker("Shield", EquipmentCatalog.Shields, enemy.ShieldPrefab,
                switcher.EnemyDefaultShield, switcher.SelectEnemyShield);
        }
    }

    // Slot 0 is always the starting gear, so the list can be reset.
    private void DrawPicker(string label, IReadOnlyList<EquipmentEntry> entries,
        GameObject current, GameObject defaultPrefab, System.Action<GameObject> select)
    {
        string[] options = new string[entries.Count + 1];
        options[0] = defaultPrefab != null ? "Default (" + defaultPrefab.name + ")" : "Default";

        int selected = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            EquipmentEntry entry = entries[i];
            string stats = entry.Kind == EquipmentKind.Weapon
                ? entry.Name + "   DMG " + entry.Damage.ToString("0") + "   LV " + entry.Level
                : entry.Name + "   LV " + entry.Level;
            options[i + 1] = stats;

            if (current == entry.Prefab) selected = i + 1;
        }

        int picked = EditorGUILayout.Popup(label, selected, options);
        if (picked != selected)
        {
            select(picked == 0 ? defaultPrefab : entries[picked - 1].Prefab);
        }
    }
}
#endif