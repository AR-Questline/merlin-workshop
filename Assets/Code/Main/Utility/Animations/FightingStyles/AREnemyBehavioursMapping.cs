using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Animations.FightingStyles {
    [CreateAssetMenu(menuName = "TG/Enemy Behaviours Mapping", order = 0)]
    public class AREnemyBehavioursMapping : ScriptableObject {
        [SerializeField, ShowIf("@false")] int saveIndex;
        
        [field: SerializeReference, OnValueChanged(nameof(VerifySaveIndexes))]
        [field: ListDrawerSettings(ListElementLabelName = nameof(EnemyBehaviourBase.Editor_GetName))]
        public List<EnemyBehaviourBase> CombatBehaviours { get; private set; }

        void VerifySaveIndexes() {
#if UNITY_EDITOR
            bool anythingChanged = false;
            foreach (EnemyBehaviourBase combatBehaviour in CombatBehaviours.Where(cb => cb is { saveIndex: -1 })) {
                combatBehaviour.saveIndex = ++saveIndex;
                anythingChanged = true;
            }

            if (anythingChanged) {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

#if UNITY_EDITOR
        void OnValidate() {
            VerifySaveIndexes();
        }

        [UnityEditor.MenuItem("TG/Assets/Validate all enemy behaviours mapping")]
        static void ValidateAllAREnemyBehavioursMapping() {
            var assets = UnityEditor.AssetDatabase.FindAssets("t:AREnemyBehavioursMapping", new [] {"Assets/Data/CombatBehaviours"});
            foreach (string guid in assets) {
                var mapping = UnityEditor.AssetDatabase.LoadAssetAtPath<AREnemyBehavioursMapping>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                mapping.VerifySaveIndexes();
            }
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}