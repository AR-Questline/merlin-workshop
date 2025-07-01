using System;
using System.Text;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Awaken.Utility.Debugging {
    public class PlayerLoopVisualizationWindow : UGUIWindowDisplay<PlayerLoopVisualizationWindow> {
        int _indentLevel;
        OnDemandCache<string, bool> _expanded = new(static _ => true);

        protected override void DrawWindow() {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            DrawPlayerLoop(playerLoop);
        }

        void DrawPlayerLoop(in PlayerLoopSystem playerLoop) {
            var loopPointName = Name(playerLoop);
            if (!ShouldDraw(playerLoop)) {
                return;
            }

            var isLeaf = playerLoop.subSystemList.IsNullOrEmpty();

            if (isLeaf) {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new string('-', _indentLevel*4), GUILayout.ExpandWidth(false));
                GUILayout.Label(loopPointName);
                GUILayout.EndHorizontal();
            } else {
                var isExpanded = _expanded[loopPointName];
                GUILayout.BeginHorizontal();
                GUILayout.Label(new string('-', _indentLevel*4), GUILayout.ExpandWidth(false));
                isExpanded = TGGUILayout.Foldout(isExpanded, loopPointName);
                GUILayout.EndHorizontal();
                _expanded[loopPointName] = isExpanded;
                if (!isExpanded) {
                    return;
                }
                _indentLevel++;
                if (playerLoop.subSystemList.IsNotNullOrEmpty()) {
                    foreach (var subPlayerLoop in playerLoop.subSystemList) {
                        DrawPlayerLoop(subPlayerLoop);
                    }
                }
                _indentLevel--;
            }
        }

        bool ShouldDraw(in PlayerLoopSystem playerLoop) {
            if (SearchContext.HasSearchInterest(Name(playerLoop))) {
                return true;
            }
            if (playerLoop.subSystemList.IsNotNullOrEmpty()) {
                foreach (var subPlayerLoop in playerLoop.subSystemList) {
                    if (ShouldDraw(subPlayerLoop)) {
                        return true;
                    }
                }
            }
            return false;
        }

        string Name(in PlayerLoopSystem playerLoop) => TypeName(playerLoop.type) ?? (_indentLevel == 0 ? "Main" : "Unknown");

        static string TypeName(Type type) {
            if (type == null) {
                return null;
            }

            var sb = new StringBuilder();
            var name = type.Name;
            if (!type.IsGenericType) {
                return name;
            }

            var backTickIndex = name.IndexOf('`');
            if (backTickIndex == -1) {
                sb.Append(name);
            } else {
                sb.Append(name.Substring(0, backTickIndex));
            }

            sb.Append("<");
            var genericArguments = type.GetGenericArguments();
            for (int i = 0; i < genericArguments.Length; i++) {
                sb.Append(TypeName(genericArguments[i]));
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append(">");
            return sb.ToString();
        }
    }

    static class PlayerLoopMarvinButtons {
        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void PlayerLoopShowWindow() {
            PlayerLoopVisualizationWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsDebugWindowShown() => PlayerLoopVisualizationWindow.IsShown;
    }
}
