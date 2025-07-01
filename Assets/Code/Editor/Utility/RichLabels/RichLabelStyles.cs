using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels {
    public static class RichLabelStyles {
        static GUIStyle s_neutralLabelStyle;
        static GUIStyle s_includedLabelStyle;
        static GUIStyle s_excludedLabelStyle;
        static GUIStyle s_historyElementLabelStyle;
        static GUIStyle s_listItemStyle;

        static GUIStyle s_listStyle;

        public static GUIStyle NeutralLabel => s_neutralLabelStyle ??= GetGuiStyle(InclusionState.Neutral);
        public static GUIStyle IncludedLabel => s_includedLabelStyle ??= GetGuiStyle(InclusionState.Included);
        public static GUIStyle ExcludedLabel => s_excludedLabelStyle ??= GetGuiStyle(InclusionState.Excluded);
        public static GUIStyle ListItem => s_listItemStyle ??= new GUIStyle() {
            hover = new GUIStyleState() {
                background = GetTexture(new Color(.80f,.66f,0.5f, 0.5f))
            },
            active = new GUIStyleState() {
                background = GetTexture(new Color(.6f,.5f,0.4f, 0.2f))
            },
            
        };
        public static GUIStyle HistoryElementLabelStyle => s_historyElementLabelStyle ??= new GUIStyle() {
            fontSize = 11
        };
        
        static GUIStyle GetGuiStyle(InclusionState inclusionState) {
            var style = new GUIStyle() {
                normal = {
                    textColor = GetTextColor(inclusionState),
                },
                hover = {
                    textColor = GetTextColor(inclusionState),
                },
                active = {
                    textColor = GetTextColor(inclusionState),
                },
                clipping = TextClipping.Ellipsis,
                fontSize = 10,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                wordWrap = true
            };
            return style;
        }

        public static Color GetTextColor(InclusionState inclusionState) {
            return inclusionState switch {
                InclusionState.Neutral => Color.white,
                InclusionState.Included => Color.green,
                InclusionState.Excluded => Color.red,
                _ => Color.white
            };
        }

        static Texture2D GetTexture(Color color) {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            for (int x = 0; x < texture.width; x++) {
                for (int y = 0; y < texture.height; y++) {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        public enum InclusionState {
            Neutral,
            Included,
            Excluded
        }
    }
}