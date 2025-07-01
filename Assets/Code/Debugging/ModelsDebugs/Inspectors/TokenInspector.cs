using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class TokenInspector : MemberListItemInspector<TokenText> {
        bool _foldout;
        
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            DrawValue(member, value, true, modelsDebug);
        }

        void DrawValue(MembersListItem member, object value, bool useFoldout, ModelsDebug modelsDebug) {
            TokenText tokenValue = CastedValue(value);

            if (useFoldout) {
                _foldout = TGGUILayout.Foldout(_foldout, member.Name);
            }

            if (_foldout || !useFoldout) {
                GUILayout.Label($"{tokenValue.Type.EnumName} - {tokenValue.InputValue}");
                using (new TGGUILayout.IndentScope()) {
                    foreach (var token in tokenValue.Tokens) {
                        DrawValue(member, token, false, modelsDebug);
                    }
                }
            }
        }
    }
}