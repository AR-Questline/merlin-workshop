using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.ScenesUtilities {
    [Overlay(typeof(SceneView), "Scene Ruler", defaultDisplay = false)]
    public class SceneViewRulerOverlay : Overlay {
        static Vector3? s_pointA;
        static Vector3? s_pointB;
        static GUIStyle s_labelStyle;
        static bool s_waitingForPointA;
        static bool s_waitingForPointB;
        static SceneViewRulerOverlay s_instance;

        EditorToolbarButton _setPointAButton;
        EditorToolbarButton _setPointBButton;
        EditorToolbarButton _clearButton;
        Label _statusLabel;

        static SceneViewRulerOverlay() {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        static GUIStyle LabelStyle {
            get {
                if (s_labelStyle == null) {
                    s_labelStyle = new GUIStyle(EditorStyles.boldLabel) {
                        normal = { textColor = Color.white },
                        fontSize = 14,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                return s_labelStyle;
            }
        }

        public override VisualElement CreatePanelContent() {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            _setPointAButton = new EditorToolbarButton(OnSetPointAClicked) {
                text = "A",
                tooltip = "Click to activate, then click in scene view to set Point A"
            };
            root.Add(_setPointAButton);

            _setPointBButton = new EditorToolbarButton(OnSetPointBClicked) {
                text = "B",
                tooltip = "Click to activate, then click in scene view to set Point B"
            };
            root.Add(_setPointBButton);

            _clearButton = new EditorToolbarButton(ClearPoints) {
                text = "Clear",
                tooltip = "Clear both points"
            };
            root.Add(_clearButton);

            _statusLabel = new Label("");
            _statusLabel.style.marginLeft = 8;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            root.Add(_statusLabel);

            root.RegisterCallback<AttachToPanelEvent>(_ => s_instance = this);
            root.RegisterCallback<DetachFromPanelEvent>(_ => {
                if (s_instance == this) {
                    s_instance = null;
                }
            });

            UpdateStatusLabel();
            return root;
        }

        void OnSetPointAClicked() {
            s_waitingForPointA = true;
            s_waitingForPointB = false;
            UpdateStatusLabel();
            UpdateButtonStyles();
            SceneView.RepaintAll();
        }

        void OnSetPointBClicked() {
            s_waitingForPointB = true;
            s_waitingForPointA = false;
            UpdateStatusLabel();
            UpdateButtonStyles();
            SceneView.RepaintAll();
        }

        void UpdateStatusLabel() {
            if (_statusLabel == null) return;

            if (s_waitingForPointA) {
                _statusLabel.text = "Click in scene to set Point A";
            } else if (s_waitingForPointB) {
                _statusLabel.text = "Click in scene to set Point B";
            } else if (s_pointA.HasValue && s_pointB.HasValue) {
                float distance = Vector3.Distance(s_pointA.Value, s_pointB.Value);
                _statusLabel.text = $"Distance: {distance:F2}m";
            } else if (s_pointA.HasValue) {
                _statusLabel.text = "Point A set";
            } else if (s_pointB.HasValue) {
                _statusLabel.text = "Point B set";
            } else {
                _statusLabel.text = "";
            }
        }

        void UpdateButtonStyles() {
            if (_setPointAButton == null || _setPointBButton == null) return;

            if (s_waitingForPointA) {
                _setPointAButton.style.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.5f);
                _setPointBButton.style.backgroundColor = StyleKeyword.Null;
            } else if (s_waitingForPointB) {
                _setPointAButton.style.backgroundColor = StyleKeyword.Null;
                _setPointBButton.style.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.5f);
            } else {
                _setPointAButton.style.backgroundColor = StyleKeyword.Null;
                _setPointBButton.style.backgroundColor = StyleKeyword.Null;
            }
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode) {
            ClearPoints();
        }

        static void OnSceneClosed(Scene scene) {
            ClearPoints();
        }

        static void OnSceneGUI(SceneView sceneView) {
            Event e = Event.current;

            // Handle mouse click when waiting for point
            if (e.type == EventType.MouseDown && e.button == 0 && (s_waitingForPointA || s_waitingForPointB)) {
                Vector2 mousePos = e.mousePosition;
                SetPoint(mousePos, s_waitingForPointA);
                s_waitingForPointA = false;
                s_waitingForPointB = false;
                s_instance?.UpdateStatusLabel();
                s_instance?.UpdateButtonStyles();
                e.Use();
            }

            // Draw the ruler visualization
            DrawRuler();
        }

        static void SetPoint(Vector2 mousePosition, bool isPointA) {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit)) {
                if (isPointA) {
                    s_pointA = hit.point;
                    Log.Debug?.Info($"Ruler Point A set at {hit.point}");
                } else {
                    s_pointB = hit.point;
                    Log.Debug?.Info($"Ruler Point B set at {hit.point}");
                }
                s_instance?.UpdateStatusLabel();
                SceneView.RepaintAll();
            } else {
                Log.Debug?.Warning($"Ruler: No collider hit for Point {(isPointA ? "A" : "B")}");
            }
        }

        static void ClearPoints() {
            s_pointA = null;
            s_pointB = null;
            s_waitingForPointA = false;
            s_waitingForPointB = false;
            s_instance?.UpdateStatusLabel();
            s_instance?.UpdateButtonStyles();
            SceneView.RepaintAll();
        }

        static void DrawRuler() {
            // Draw line first (so labels appear on top)
            if (s_pointA.HasValue && s_pointB.HasValue) {
                DrawLine(s_pointA.Value, s_pointB.Value);
            }

            // Draw points and labels last
            if (s_pointA.HasValue) {
                DrawPoint(s_pointA.Value, "A", Color.green);
            }

            if (s_pointB.HasValue) {
                DrawPoint(s_pointB.Value, "B", Color.blue);
            }
        }

        static void DrawLabelWithBackground(Vector3 worldPosition, string text) {
            var sceneCamera = SceneView.lastActiveSceneView?.camera;
            if (sceneCamera == null) return;

            var cameraToPoint = worldPosition - sceneCamera.transform.position;
            var dotProduct = Vector3.Dot(cameraToPoint.normalized, sceneCamera.transform.forward);

            // Only draw label if position is in front of camera
            if (dotProduct > 0) {
                Handles.BeginGUI();
                var labelSize = LabelStyle.CalcSize(new GUIContent(text));
                var labelRect = new Rect(0, 0, labelSize.x + 16, labelSize.y + 8);
                var screenPos = HandleUtility.WorldToGUIPoint(worldPosition);
                labelRect.center = screenPos;

                // Draw semi-transparent background
                var oldColor = GUI.color;
                GUI.color = new Color(0, 0, 0, 0.85f);
                GUI.DrawTexture(labelRect, EditorGUIUtility.whiteTexture);
                GUI.color = oldColor;

                // Draw the label
                GUI.Label(labelRect, text, LabelStyle);
                Handles.EndGUI();
            }
        }

        static void DrawPoint(Vector3 position, string label, Color color) {
            Handles.color = color;
            Handles.SphereHandleCap(0, position, Quaternion.identity, 0.2f, EventType.Repaint);

            string coordinatesText = $"Point {label}\n({position.x:F2}, {position.y:F2}, {position.z:F2})";
            Vector3 labelPos = position + Vector3.up * 1.2f;
            DrawLabelWithBackground(labelPos, coordinatesText);
        }

        static void DrawLine(Vector3 pointA, Vector3 pointB) {
            Handles.color = Color.yellow;
            Handles.DrawLine(pointA, pointB, 3f);

            float distance = Vector3.Distance(pointA, pointB);
            Vector3 midPoint = (pointA + pointB) * 0.5f;
            Vector3 labelPos = midPoint + Vector3.up * 1.5f;

            string distanceText = $"Distance: {distance:F2}m";
            DrawLabelWithBackground(labelPos, distanceText);
        }
    }
}