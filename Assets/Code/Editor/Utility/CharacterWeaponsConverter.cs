using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Utility.Reflections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public class CharacterWeaponsConverter : OdinEditorWindow {
        public List<GameObject> weapons = new();
        public float multiplyHeroLengthBy = 0.5f;

        [MenuItem("TG/Assets/Prefabs/Convert CharacterWeapons additionalLengthForNpcWhenFightingHero")]
        static void ShowWindow() {
            var window = GetWindow<CharacterWeaponsConverter>();
            window.titleContent = new GUIContent("Convert CharacterWeapons additionalLengthForNpcWhenFightingHero");
            window.Show();
        }

        [Button]
        public void Convert() {
            AssetDatabase.StartAssetEditing();
            try {
                foreach (var weapon in weapons) {
                    if (weapon) {
                        CharacterWeapon[] characterWeapons = weapon.GetComponentsInChildren<CharacterWeapon>();
                        foreach (CharacterWeapon characterWeapon in characterWeapons) {
                            if (characterWeapon) {
                                var lengthForHero = characterWeapon.GetType().GetFieldRecursive("additionalLengthForHero");
                                float heroValue = (float)lengthForHero.GetValue(characterWeapon);
                                ReflectionExtension.SetField(characterWeapon, "additionalLengthForNpcWhenFightingHero", heroValue * multiplyHeroLengthBy);
                            }
                        }
                        EditorUtility.SetDirty(weapon);
                    }
                }
            } finally {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            weapons.Clear();
        }

        [Button, ShowIf(nameof(FolderIsSelected))]
        public void FindCharacterWeaponsInSelectedFolder() {
            if (Selection.activeObject is DefaultAsset folder) {
                var folderPath = AssetDatabase.GetAssetPath(folder);
                var prefabGuids = AssetDatabase.FindAssets("t:prefab", new[] { folderPath });
                var count = prefabGuids.Length;

                for (int i = 0; i < count; i++) {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab && prefab.GetComponent<CharacterWeapon>() != null) {
                        weapons.Add(prefab);
                    }
                }
            }
        }

        bool FolderIsSelected() {
            return Selection.activeObject is DefaultAsset;
        }
    }
}