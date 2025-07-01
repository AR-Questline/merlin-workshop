using Awaken.TG.Main.UI.Components;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Awaken.TG.Editor.Main.UI.Components {
    [CustomEditor(typeof(ARButton)), CanEditMultipleObjects]
    public class ARButtonEditor : SelectableEditor {
        ARButton Target => (ARButton) target;
        bool _showNavigation;
        
        protected override void OnEnable() {
            base.OnEnable();
            _showNavigation = EditorPrefs.GetBool("SelectableEditor.ShowNavigation");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            CheckTargetGraphic();
            Target.Interactable = EditorGUILayout.Toggle("Interactable", Target.Interactable);

            DrawTransitionSetting();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreSubmitAction"));
            
            // Target graphics
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetGraphic"));
            // Draw alpha settings if sprite transition
            if (Target.Transition.HasFlag(ARButton.TransitionType.Sprite)) {
                DrawMinMax("normalAlphaRange");
            }
            EditorGUILayout.EndHorizontal();

            DrawColors();
            DrawScale();
            DrawSpriteTransition();
            DrawAudioClips();
            DrawNavigation();
            serializedObject.ApplyModifiedProperties();
        }

        void CheckTargetGraphic() {
            if (Target.TargetGraphic == null) {
                Target.TargetGraphic = Target.GetComponent<Graphic>();
            }
        }

        void DrawTransitionSetting() {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionType"));
            EditorGUILayout.LabelField("Time", GUILayout.Width(30));
            Target.transitionTime = EditorGUILayout.FloatField(Target.transitionTime, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        void DrawColors() {
            if (!Target.Transition.HasFlag(ARButton.TransitionType.Color)) {
                return;
            }
            DrawHeader("Color transition");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("normalColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pressColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disableColor"));
        }

        void DrawScale() {
            if (!Target.Transition.HasFlag(ARButton.TransitionType.Scale)) {
                return;
            }
            DrawHeader("Scale transition");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("normalScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pressScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("disableScale"));
        }

        void DrawSpriteTransition() {
            if (!Target.Transition.HasFlag(ARButton.TransitionType.Sprite)) {
                return;
            }
            DrawHeader("Sprite (graphic) transition");

            bool missingElement = false;
            missingElement |= DrawSpriteTransitionElement("hover");
            missingElement |= DrawSpriteTransitionElement("selected");
            missingElement |= DrawSpriteTransitionElement("additiveSelected");
            missingElement |= DrawSpriteTransitionElement("press");
            missingElement |= DrawSpriteTransitionElement("disable");

            if (missingElement && GUILayout.Button("Try find missing graphics")) {
                FindMissingSpriteGraphics();
            }
        }

        void DrawHeader(string headerText) {
            EditorGUILayout.BeginHorizontal();
            var oldColor = GUI.color;
            GUI.color = Color.green;
            DrawLabel(headerText);
            var boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.margin = new RectOffset(0,0,8,0);
            GUILayout.Box("", boxStyle, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = oldColor;
            EditorGUILayout.EndHorizontal();
        }

        void DrawLabel(string label) {
            GUIContent content = new GUIContent(label);
            EditorGUILayout.LabelField(content, GUILayout.Width(GUI.skin.label.CalcSize(content).x));
        }

        bool DrawSpriteTransitionElement(string elementName) {
            string graphicName = $"{elementName}Graphic";
            string alphaName = $"{elementName}AlphaRange";
            EditorGUILayout.BeginHorizontal();
            var graphicProperty = serializedObject.FindProperty(graphicName);
            EditorGUILayout.PropertyField(graphicProperty);
            DrawMinMax(alphaName);
            EditorGUILayout.EndHorizontal();
            return graphicProperty.objectReferenceValue == null;
        }

        void DrawMinMax(string propertyName) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName).FindPropertyRelative("min"), GUIContent.none, true, GUILayout.Width(35));
            DrawLabel("-");
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName).FindPropertyRelative("max"), GUIContent.none, true, GUILayout.Width(35));
        }

        void FindMissingSpriteGraphics() {
            if (Target.hoverGraphic == null) {
                FindOrCreate(ref Target.hoverGraphic, nameof(Target.hoverGraphic));
            }
            if (Target.selectedGraphic == null) {
                FindOrCreate(ref Target.selectedGraphic, nameof(Target.selectedGraphic));
            }
            if (Target.pressGraphic == null) {
                FindOrCreate(ref Target.pressGraphic, nameof(Target.pressGraphic));
            }
            if (Target.disableGraphic == null) {
                FindOrCreate(ref Target.disableGraphic, nameof(Target.disableGraphic));
            }
        }

        void FindOrCreate(ref Graphic graphic, string graphicName) {
            graphicName = char.ToUpper(graphicName[0]) + graphicName.Substring(1);
            Transform child = Target.transform.Find(graphicName);
            graphic = child?.GetComponent<Graphic>();
            if (graphic == null) {
                GameObject imageGO = new GameObject(graphicName, typeof(Image));
                var imageRT = imageGO.GetComponent<RectTransform>();
                imageRT.SetParent(Target.transform);
                imageRT.anchorMax = Vector2.one;
                imageRT.anchorMin = Vector2.zero;
                imageRT.anchoredPosition = Vector2.zero;
                imageRT.sizeDelta = Vector2.zero;
                imageRT.localScale = Vector3.one;
                graphic = imageGO.GetComponent<Graphic>();
            }
        }

        void DrawAudioClips() {
            DrawHeader("Audio");
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.disableAllSounds)));
            if (!Target.disableAllSounds) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.overrideSounds)));
                if (Target.overrideSounds) {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.clickSound)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.selectedSound)));
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.disableInactiveSounds)));
            if (!Target.disableInactiveSounds) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.overrideInactiveSounds)));
                if (Target.overrideInactiveSounds) {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.clickInactiveSound)));
                }
            }
        }
        
        void DrawNavigation() {
            DrawHeader("Navigation");

            SerializedProperty navigationProp = serializedObject.FindProperty("m_Navigation");
            EditorGUILayout.PropertyField(navigationProp);

            EditorGUI.BeginChangeCheck();
            _showNavigation = EditorGUILayout.Toggle("Visualize Navi", _showNavigation);
            if (EditorGUI.EndChangeCheck()) {
                EditorPrefs.SetBool("SelectableEditor.ShowNavigation", _showNavigation);
                OnDisable();
                OnEnable();
                SceneView.RepaintAll();
            }
        }
    }
}