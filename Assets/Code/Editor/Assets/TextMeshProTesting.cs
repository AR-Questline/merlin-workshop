using System.Linq;
using Awaken.TG.Utility;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace Awaken.TG.Editor.Assets {
    public static class TextMeshProTesting {
        // === Menu items

        [MenuItem("TG/Debug/Create TMP Sprite text")]
        private static void CreateTMPSpriteText() {
            // create debug text
            UnityEngine.TextCore.Text.SpriteAsset spriteAsset = TMP_Settings.defaultSpriteAsset;
            var names = spriteAsset.spriteCharacterTable.Select(sct => sct.name);
            var text = names.Aggregate("textM", (acu, next) => $"{acu}{{{next}}}L, textM");

            // create and setup TextMeshPro text object
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject go = new GameObject("TMP_Sprite_Test", typeof(TextMeshProUGUI));
            go.transform.SetParent(canvas.transform);
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = text.FormatSprite();

            // set to full canvas
            RectTransform rt = tmp.GetComponent<RectTransform>();
            rt.anchorMax = Vector2.one;
            rt.anchorMin = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.one;
            rt.localPosition = Vector3.zero;
            rt.sizeDelta = Vector2.zero;

            // select in editor
            EditorGUIUtility.PingObject(TMP_Settings.defaultSpriteAsset);
            Selection.activeObject = TMP_Settings.defaultSpriteAsset;
        }

        [MenuItem("Assets/TG/Update TMP SpriteAsset", true)]
        private static bool ValidUpdateSpriteAsset() {
            return Selection.activeObject is UnityEngine.TextCore.Text.SpriteAsset;
        }

        [MenuItem("Assets/TG/Update TMP SpriteAsset")]
        private static void UpdateSpriteAsset() {
            UnityEngine.TextCore.Text.SpriteAsset spriteAsset = (UnityEngine.TextCore.Text.SpriteAsset)Selection.activeObject;
            var atlas = spriteAsset.spriteSheet;
            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetOrScenePath(atlas)).Cast<Sprite>();

            var includedSpritesName = spriteAsset.spriteCharacterTable.Select(sct => sct.name);
            var missingSprites = sprites.Where(s => !includedSpritesName.Contains(s.name));

            foreach (Sprite missingSprite in missingSprites) {
                AddSprite(spriteAsset, missingSprite);
            }

            spriteAsset.UpdateLookupTables();

            EditorUtility.SetDirty(spriteAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AddSprite(UnityEngine.TextCore.Text.SpriteAsset spriteAsset, Sprite spriteToAdd) {
            var spriteRect = spriteToAdd.rect;
            var glyphIndex = spriteAsset.spriteGlyphTable.Max(g => g.index) + 1;
            var glyph = new UnityEngine.TextCore.Text.SpriteGlyph(glyphIndex, spriteAsset.spriteGlyphTable.Last().metrics, new GlyphRect(spriteRect), 1, 0, spriteToAdd);
            spriteAsset.spriteGlyphTable.Add(glyph);
            var spriteCharacter = new UnityEngine.TextCore.Text.SpriteCharacter(0, glyph);
            spriteCharacter.name = spriteToAdd.name;
            spriteAsset.spriteCharacterTable.Add(spriteCharacter);
        }
    }
}