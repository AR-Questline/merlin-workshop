using System;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public class XboxBuildVersionPopup : EditorWindow {
        Action<string> _onVersionChosen;
        XDocument _gameConfigXmlDoc;
        string _gameConfigPath;
        int first, second, third;

        public static XboxBuildVersionPopup Show(Action<string> onVersionChosen, XDocument gameConfigXmlDoc, string gameConfigPath, XAttribute versionAttribute) {
            string[] version = versionAttribute.Value.Split('.');
            var window = GetWindow<XboxBuildVersionPopup>(true, "Change MS.Config Version", true);
            window.maxSize = new Vector2(300, 75);
            window.minSize = new Vector2(300, 75);
            
            int.TryParse(version[0], out window.first);
            int.TryParse(version[1], out window.second);
            int.TryParse(version[2], out window.third);
            
            window._onVersionChosen = onVersionChosen;
            window._gameConfigXmlDoc = gameConfigXmlDoc;
            window._gameConfigPath = gameConfigPath;
            
            window.ShowUtility();
            return window;
        }

        void OnGUI() {
            EditorGUILayout.PrefixLabel("Version:");
            EditorGUILayout.BeginHorizontal();
            int[] options = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            string[] optionsString = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            first = EditorGUILayout.IntPopup(first, optionsString, options);
            EditorGUILayout.LabelField(".", GUILayout.Width(10));
            second = EditorGUILayout.IntPopup(second, optionsString, options);
            EditorGUILayout.LabelField(".", GUILayout.Width(10));
            third = EditorGUILayout.IntPopup(third, optionsString, options);
            EditorGUILayout.LabelField(".", GUILayout.Width(10));
            EditorGUILayout.IntPopup(0, new []{"0"}, new []{0});
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply New Version")) {
                Accept();
            }

            if (GUILayout.Button("Cancel")) {
                Cancel();
            }
            EditorGUILayout.EndHorizontal();
        }

        void Accept() {
            string newVersion = $"{first}.{second}.{third}.0";
            XElement identityElement = (from identity in _gameConfigXmlDoc.Descendants("Identity") select identity).First();
            identityElement.SetAttributeValue("Version", newVersion);
            _gameConfigXmlDoc.Save(_gameConfigPath);
            _onVersionChosen.Invoke(newVersion);
            Close();
        }

        void Cancel() {
            Close();
        }
    }
}