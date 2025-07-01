using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class EnumerableInspector : MemberListItemInspector<IEnumerable> {
        Dictionary<string, bool> _collapsed = new Dictionary<string, bool>();
        Dictionary<string, bool[]> _itemsCollapse = new Dictionary<string, bool[]>();
        string[] _searchContext;
        int _searchHash;

        public override void Draw(MembersListItem member, object value, object target, ModelsDebug modelsDebug, string[] searchContext, int searchHash) {
            _searchContext = searchContext;
            _searchHash = searchHash;
            if (!IsInContext(member, value, searchContext, searchHash)) {
                return;
            }
            var oldEnable = GUI.enabled;
            GUI.enabled = true;
            DrawValue(member, value, target, modelsDebug);
            GUI.enabled = oldEnable;
            _searchContext = Array.Empty<string>();
            _searchHash = -1;
        }

        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var castedTarget = CastedValue(value);
            var enumerable = castedTarget as object[] ?? castedTarget.Cast<object>().ToArray();

            string dictionaryKey = target.GetHashCode() + member.Name;

            bool collapsed = ObtainCollapses(dictionaryKey, enumerable, out bool[] itemsCollapse);

            if (GUILayout.Button((collapsed ? "\u25B6" : "\u25BC") + $" <color=lightblue>{member.Name} [{itemsCollapse.Length}]:</color>", LabelStyle)) {
                collapsed = !collapsed;
                _collapsed[dictionaryKey] = collapsed;
            }

            if (collapsed) {
                return;
            }

            int i = -1;
            foreach (object o in enumerable) {
                TGGUILayout.BeginHorizontal();
                ++i;
                GUILayout.Label("", GUILayout.Width(10F));
                
                if (GUILayout.Button((itemsCollapse[i] ? "\u25B6" : "\u25BC") + $" <color=lightblue>{i}.</color>", LabelStyle)) {
                    itemsCollapse[i] = !itemsCollapse[i];
                }

                if (!itemsCollapse[i]) {
                    if (o == null) {
                        GUILayout.Label(YellowNull, LabelStyle);
                    } else {
                        (bool changedValueType, object result) = o switch {
                            int oInt => DrawValueType(TGGUILayout.DelayedIntField, oInt),
                            float oFloat => DrawValueType(TGGUILayout.DelayedFloatField, oFloat),
                            string oString => DrawValueType(TGGUILayout.DelayedTextField, oString),
                            Vector2 oV2 => DrawValueType(v => TGGUILayout.Vector2Field("", v), oV2),
                            Vector3 oV3 => DrawValueType(v => TGGUILayout.Vector3Field("", v), oV3),
                            Vector4 oV4 => DrawValueType(v => TGGUILayout.Vector4Field("", v), oV4),
                            Vector2Int oV2Int => DrawValueType(v => TGGUILayout.Vector2IntField("", v), oV2Int),
                            Vector3Int oV3Int => DrawValueType(v => TGGUILayout.Vector3IntField("", v), oV3Int),
                            _ => DrawReferenced(o, target, modelsDebug),
                        };

                        if (changedValueType && castedTarget is IList list) {
                            list[i] = result;
                        }
                    }
                }

                TGGUILayout.EndHorizontal();
            }
        }

        protected override bool ValueInContext(object value, Lazy<string> stringValue, string searchPart) {
            return true;
        }

        static (bool, T) DrawValueType<T>(Func<T, T> drawer, T value) {
            using var checkScope = new TGGUILayout.CheckChangeScope();
            var result = drawer(value);
            return (checkScope, result);
        }

        (bool, object) DrawReferenced(object o, object target, ModelsDebug modelsDebug) {
            TGGUILayout.BeginVertical();
            var t = new MembersList(o);
            foreach (MembersListItem membersListItem in t.Items) {
                var memberValue = membersListItem.Value(o);
                MemberListItemInspector.GetInspector(membersListItem, memberValue, target).Draw(membersListItem, memberValue, target, modelsDebug, _searchContext, _searchHash);
            }
            TGGUILayout.EndVertical();

            return (false, null);
        }

        bool ObtainCollapses(string dictionaryKey, object[] enumerable, out bool[] itemsCollapse) {
            if (!_collapsed.TryGetValue(dictionaryKey, out var collapsed)) {
                collapsed = true;
                _collapsed[dictionaryKey] = true;
            }

            if (!_itemsCollapse.TryGetValue(dictionaryKey, out itemsCollapse)) {
                itemsCollapse = Array.Empty<bool>();
                _itemsCollapse[dictionaryKey] = itemsCollapse;
            }

            if ((itemsCollapse?.Length ?? -1) != enumerable.Length) {
                itemsCollapse = new bool[enumerable.Length];
                for (int index = 0; index < itemsCollapse.Length; index++) {
                    itemsCollapse[index] = false;
                }

                _itemsCollapse[dictionaryKey] = itemsCollapse;
            }

            return collapsed;
        }
    }
}