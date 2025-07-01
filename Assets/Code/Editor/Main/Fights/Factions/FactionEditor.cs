using System;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Main.Fights.Factions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Fights.Factions {
    [CustomEditor(typeof(FactionTemplate))]
    public class FactionEditor : OdinEditor {
    
        FactionTemplate Target => target as FactionTemplate;
        
        FactionTree _factionTree;
        
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (_factionTree == null) {
                _factionTree = CreateTree();
            } 
            
            GUILayout.Space(10);
            
            int defaultIndent = EditorGUI.indentLevel;
            var bg = GUI.backgroundColor;
            foreach (var (faction, indent) in _factionTree.AllFactionsWithIndent) {
                EditorGUI.indentLevel = defaultIndent + indent;
                GUI.backgroundColor = ColorOfAntagonismTo(faction);
                EditorGUILayout.ObjectField(faction.Template, typeof(FactionTemplate), false);
            }
            GUI.backgroundColor = bg;
            EditorGUI.indentLevel = defaultIndent;

            GUILayout.Space(10);

            if (GUILayout.Button("Rebuild Faction Tree")) {
                _factionTree = CreateTree();
            }
        }

        FactionTree CreateTree() {
            return new(TemplatesSearcher.FindAllOfType<FactionTemplate>().ToArray());
        }

        Color ColorOfAntagonismTo(Faction faction) {
            return _factionTree.FactionByTemplate(Target).AntagonismTo(_factionTree.FactionByTemplate(faction.Template)) switch {
                Antagonism.Friendly => Color.green * 2,
                Antagonism.Neutral => Color.blue * 2,
                Antagonism.Hostile => Color.red * 2,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        Texture2D BgTexture(int width, int height, Color color) {
            Texture2D backgroundTexture = new(width, height);
            
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) {
                pixels[i] = color;
            }

            backgroundTexture.SetPixels(pixels);
            backgroundTexture.Apply();

            return backgroundTexture;
        }
    }
}