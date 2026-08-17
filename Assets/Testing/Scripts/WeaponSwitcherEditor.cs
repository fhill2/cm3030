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
        var sw = (WeaponSwitcher)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to switch equipment.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
        Equipment player = sw.Player();
        if (player == null)
            EditorGUILayout.HelpBox("No Player-tagged object with Equipment found.", MessageType.Warning);
        else
        {
            DrawPicker("Weapon", EquipmentCatalog.Weapons, player.WeaponPrefab,
                sw.PlayerDefaultWeapon, sw.SelectPlayerWeapon);
            DrawPicker("Shield", EquipmentCatalog.Shields, player.ShieldPrefab,
                sw.PlayerDefaultShield, sw.SelectPlayerShield);
        }

        EditorGUILayout.Space(10f);

        EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel);
        Equipment enemy = sw.Enemy();
        if (enemy == null)
            EditorGUILayout.HelpBox("No Enemy-tagged object with Equipment found.", MessageType.Warning);
        else
        {
            DrawPicker("Weapon", EquipmentCatalog.Weapons, enemy.WeaponPrefab,
                sw.EnemyDefaultWeapon, sw.SelectEnemyWeapon);
            DrawPicker("Shield", EquipmentCatalog.Shields, enemy.ShieldPrefab,
                sw.EnemyDefaultShield, sw.SelectEnemyShield);
        }
    }

    private void DrawPicker(string label, IReadOnlyList<EquipmentEntry> entries,
        GameObject current, GameObject def, System.Action<GameObject> select)
    {
        string[] options = new string[entries.Count + 1];
        options[0] = def != null ? "Default (" + def.name + ")" : "Default";

        int selected = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            EquipmentEntry e = entries[i];
            string stats = e.Kind == EquipmentKind.Weapon
                ? e.Name + "   DMG " + e.Damage.ToString("0") + "   LV " + e.Level
                : e.Name + "   LV " + e.Level;
            options[i + 1] = stats;

            if (current == e.Prefab) selected = i + 1;
        }

        int picked = EditorGUILayout.Popup(label, selected, options);
        if (picked != selected)
        {
            select(picked == 0 ? def : entries[picked - 1].Prefab);
        }
    }
}
#endif
