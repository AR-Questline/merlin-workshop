using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public sealed class CharsInTTFWindow : EditorWindow {
        [SerializeField] string charsRange = "";
        [SerializeField] Font currentFont;
        
        Vector2 _scroll = Vector2.zero;
        FontData _fontData;

        [MenuItem("TG/TMP/Chars in Font (ttf)")]
        static void Init() {
            CharsInTTFWindow window = (CharsInTTFWindow)EditorWindow.GetWindow(typeof(CharsInTTFWindow));
            window.titleContent = new GUIContent("Chars in Font");
            window.ShowUtility();
        }

        void OnGUI() {
            if (currentFont != null) {
                using (new EditorGUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Refresh", GUILayout.Width(150))) {
                        _fontData = PickAllCharsRangeFromFont(currentFont);
                    }
                }
            }
            
            EditorGUILayout.Space();
            var newFont = EditorGUILayout.ObjectField("Source Font File", currentFont, typeof(Font), false) as Font;
            if (currentFont != newFont) {
                currentFont = newFont;
                _fontData = PickAllCharsRangeFromFont(currentFont);
            }
            charsRange = _fontData.charsRange;
            
            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.IntField("Glyphs Count", _fontData.glyphsCount);
            EditorGUILayout.LabelField("Character Sequence (Decimal)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(charsRange);
            EditorGUILayout.EndScrollView();
        }


        static FontData PickAllCharsRangeFromFont(Font font) {
            string newCharsRange = "";
            int glyphsCount = 0;
            
            if (font != null) {
                //UNITY_BUG: Unity's Font.CharacterInfo doesn't work properly on dynamic mode, we need to change it to Unicode first
                TrueTypeFontImporter fontReimporter = null;
                if (font.dynamic) {
                    var assetPath = AssetDatabase.GetAssetPath(font);
                    fontReimporter = (TrueTypeFontImporter)AssetImporter.GetAtPath(assetPath);
                    fontReimporter.fontTextureCase = FontTextureCase.Unicode;
                    fontReimporter.SaveAndReimport();
                }
                
                Vector2Int minMaxRange = new(-1, -1);
                glyphsCount = font.characterInfo.Length;
                for (int i = 0; i < font.characterInfo.Length; i++) {
                    var charInfo = font.characterInfo[i];
                    var apply = true;
                    
                    if (minMaxRange.x < 0 || minMaxRange.y < 0) {
                        apply = false;
                        minMaxRange = new Vector2Int(charInfo.index, charInfo.index);
                    }
                    else if (charInfo.index == minMaxRange.y + 1) {
                        apply = false;
                        minMaxRange.y = charInfo.index;
                    }
                    
                    if (apply || i == font.characterInfo.Length - 1) {
                        if (!string.IsNullOrEmpty(newCharsRange)) {
                            newCharsRange += "\n,";
                        }
                        newCharsRange += minMaxRange.x + "-" + minMaxRange.y;
                        
                        if (i == font.characterInfo.Length - 1) {
                            if (charInfo.index >= 0 && (charInfo.index < minMaxRange.x || charInfo.index > minMaxRange.y)) {
                                newCharsRange += "\n," + charInfo.index + "-" + charInfo.index;
                            }
                        } else {
                            minMaxRange = new Vector2Int(charInfo.index, charInfo.index);
                        }
                    }
                }

                ChangeBackToDynamicFont(fontReimporter);
            }
            
            return new FontData {
                charsRange = newCharsRange,
                glyphsCount = glyphsCount
            };
        }

        static void ChangeBackToDynamicFont(TrueTypeFontImporter fontReimporter) {
            if (fontReimporter != null) {
                fontReimporter.fontTextureCase = FontTextureCase.Dynamic;
                fontReimporter.SaveAndReimport();
            }
        }

        struct FontData {
            public string charsRange;
            public int glyphsCount;
        }
    }
}
