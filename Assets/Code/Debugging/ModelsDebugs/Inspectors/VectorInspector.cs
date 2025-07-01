using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class Vector2Inspector : MemberListItemInspector<Vector2> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var vectorValue = CastedValue(value);
            using (var checkScope = new TGGUILayout.CheckChangeScope()) {
                var result = TGGUILayout.Vector2Field(member.Name, vectorValue);
                if (checkScope) {
                    member.SetValue(target, result);
                }
            }
        }
    }
    
    [UnityEngine.Scripting.Preserve]
    public class Vector3Inspector : MemberListItemInspector<Vector3> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var vectorValue = CastedValue(value);
            using (var checkScope = new TGGUILayout.CheckChangeScope()) {
                var result = TGGUILayout.Vector3Field(member.Name, vectorValue);
                if (checkScope) {
                    member.SetValue(target, result);
                }
            }
        }
    }
    
    [UnityEngine.Scripting.Preserve]
    public class Vector2IntInspector : MemberListItemInspector<Vector2Int> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var vectorValue = CastedValue(value);
            using (var checkScope = new TGGUILayout.CheckChangeScope()) {
                var result = TGGUILayout.Vector2IntField(member.Name, vectorValue);
                if (checkScope) {
                    member.SetValue(target, result);
                }
            }
        }
    }
    
    [UnityEngine.Scripting.Preserve]
    public class Vector3IntInspector : MemberListItemInspector<Vector3Int> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var vectorValue = CastedValue(value);
            using (var checkScope = new TGGUILayout.CheckChangeScope()) {
                var result = TGGUILayout.Vector3IntField(member.Name, vectorValue);
                if (checkScope) {
                    member.SetValue(target, result);
                }
            }
        }
    }
}