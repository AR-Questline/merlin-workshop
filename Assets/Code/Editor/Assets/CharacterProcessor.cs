using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.Assets {
    public class CharacterProcessor : OdinEditorWindow {

        [MenuItem("TG/TMP/CharacterProcessor")]
        static void Init() {
            GetWindow(typeof(CharacterProcessor), true, "Character Processor").Show();
        }
        
        [Button]
        private void Except(TextAsset origin, TextAsset[] except, string saveAs) {
            Process(saveAs, () => origin.text.Except(except.SelectMany(asset => asset.text).Distinct()));
        }
        
        [Button]
        private void Intersect(TextAsset origin, TextAsset[] intersect, string saveAs) {
            Process(saveAs, () => origin.text.Intersect(intersect.SelectMany(asset => asset.text).Distinct()));
        }

        [Button]
        private void Merge(TextAsset[] texts, string saveAs) {
            Process(saveAs, () => texts.SelectMany(asset => asset.text).Distinct());
        }


        private void Process(string saveAs, Func<IEnumerable<char>> output) {
            var directory = Path.GetDirectoryName(saveAs);
            if (string.IsNullOrWhiteSpace(directory)) {
                throw new ArgumentException("Save As is not valid path");
            }

            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var text = output().Aggregate(new StringBuilder(), (builder, c) => builder.Append(c)).ToString();
            
            File.WriteAllText(saveAs, text);

            AssetDatabase.Refresh();
        }
    }
}