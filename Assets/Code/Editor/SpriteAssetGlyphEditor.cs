using Awaken.Utility.Debugging;

namespace Awaken.TG.Editor {
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.TextCore;
    using UnityEngine.TextCore.Text;

    public class SpriteAssetGlyphEditor : EditorWindow
    {
        [SerializeField] SpriteAsset spriteAsset;
        [Space]
        [SerializeField] int width, height, bx, by, advance;
        
        bool IsAssetValid => spriteAsset != null && spriteAsset.spriteGlyphTable != null;
    
        [MenuItem("Tools/Sprite Asset Glyph Editor")]
        public static void ShowWindow()
        {
            GetWindow<SpriteAssetGlyphEditor>("Glyph Metrics Editor");
        }

        private void OnGUI()
        {
            spriteAsset = (SpriteAsset)EditorGUILayout.ObjectField("Sprite Asset", spriteAsset, typeof(SpriteAsset), false);
        
            width = EditorGUILayout.IntField("Glyph Width", width);
            height = EditorGUILayout.IntField("Glyph Height", height);
            bx = EditorGUILayout.IntField("Horizontal bearing X", bx);
            by = EditorGUILayout.IntField("Horizontal bearing Y", by);
            advance = EditorGUILayout.IntField("Horizontal Advance", advance);
        
            if (IsAssetValid && GUILayout.Button("Apply Glyph Metrics"))
            {
                ApplyGlyphRect();
            }
        }

        private void ApplyGlyphRect()
        {
            if (!IsAssetValid)
            {
                Log.Minor?.Error("No valid Sprite Asset selected!");
                return;
            }

            Undo.RecordObject(spriteAsset, "Modify Glyphs");
        
            foreach (var glyph in spriteAsset.spriteGlyphTable)
            {
                glyph.metrics = new GlyphMetrics(width, height, bx, by, advance);
            }

            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();
            Log.Minor?.Info("Glyph Metrics updated and saved.");
        }
    }
}