using Awaken.Utility.Debugging;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.WorkflowTools {
    public class ArrangeInLineUtility : EditorWindow {
        const string WindowTitle = "Arrange in Line";

        [SerializeField] float spacing = 1f;
        [SerializeField] ArrangementAxis axis = ArrangementAxis.X;
        [SerializeField] bool useWorldSpace = true;
        [SerializeField] SortMode sortMode = SortMode.Hierarchy;
        [SerializeField] AlignmentMode alignmentMode = AlignmentMode.Center;
        [SerializeField] bool sortDescending;

        Vector2 _scrollPosition;

        [MenuItem("GameObject/TG/Arrange in Line")]
        [MenuItem("TG/GameObject/Arrange in Line")]
        static void ShowWindow() {
            var window = GetWindow<ArrangeInLineUtility>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(300, 250);
            window.Show();
        }

        void OnGUI() {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Arrange Selected GameObjects", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            var selectedCount = Selection.gameObjects.Length;
            if (selectedCount == 0) {
                EditorGUILayout.HelpBox("No GameObjects selected. Please select objects in the hierarchy to arrange.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Selected Objects: {selectedCount}", EditorStyles.helpBox);
            EditorGUILayout.Space(10);

            // === Settings
            EditorGUILayout.LabelField("Arrangement Settings", EditorStyles.boldLabel);

            axis = (ArrangementAxis)EditorGUILayout.EnumPopup("Axis", axis);
            spacing = EditorGUILayout.FloatField("Spacing", spacing);
            useWorldSpace = EditorGUILayout.Toggle("Use World Space", useWorldSpace);
            alignmentMode = (AlignmentMode)EditorGUILayout.EnumPopup("Alignment", alignmentMode);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Sorting", EditorStyles.miniBoldLabel);
            sortMode = (SortMode)EditorGUILayout.EnumPopup("Sort By", sortMode);
            if (sortMode != SortMode.Hierarchy) {
                sortDescending = EditorGUILayout.Toggle("Descending Order", sortDescending);
            }

            EditorGUILayout.Space(10);

            // === Info
            var infoText = sortMode switch {
                SortMode.Hierarchy => "Objects will be arranged in the order they appear in the hierarchy.",
                SortMode.Position => "Objects will be sorted by their current position along the selected axis.",
                SortMode.SizeSmallest => "Objects will be sorted by size (smallest to largest) based on renderer/collider bounds.",
                SortMode.SizeLargest => "Objects will be sorted by size (largest to smallest) based on renderer/collider bounds.",
                SortMode.Name => "Objects will be sorted alphabetically by name.",
                _ => ""
            };
            if (!string.IsNullOrEmpty(infoText)) {
                EditorGUILayout.HelpBox(infoText, MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // === Action Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Arrange", GUILayout.Height(30))) {
                ArrangeSelectedObjects();
            }
            if (GUILayout.Button("Reset Selection Position", GUILayout.Height(30))) {
                ResetPositions();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // === Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Align X")) {
                QuickArrange(ArrangementAxis.X);
            }
            if (GUILayout.Button("Align Y")) {
                QuickArrange(ArrangementAxis.Y);
            }
            if (GUILayout.Button("Align Z")) {
                QuickArrange(ArrangementAxis.Z);
            }
            EditorGUILayout.EndHorizontal();
        }

        void ArrangeSelectedObjects() {
            var selected = Selection.gameObjects;
            if (selected.Length == 0) {
                Log.Important?.Warning("No objects selected to arrange.");
                return;
            }

            Undo.RecordObjects(selected.Select(go => go.transform as Object).ToArray(), "Arrange in Line");

            var transforms = selected.Select(go => go.transform).ToArray();

            // Sort transforms based on selected mode
            transforms = SortTransforms(transforms);

            // Calculate start position based on alignment mode
            var startPosition = CalculateStartPosition(transforms);

            // Arrange objects
            for (int i = 0; i < transforms.Length; i++) {
                var offset = i * spacing;
                var newPosition = startPosition + GetAxisVector() * offset;

                if (useWorldSpace) {
                    var currentPos = transforms[i].position;
                    transforms[i].position = SetPositionOnAxis(currentPos, newPosition);
                } else {
                    var currentPos = transforms[i].localPosition;
                    transforms[i].localPosition = SetPositionOnAxis(currentPos, newPosition);
                }
            }

            Log.Important?.Info($"Arranged {transforms.Length} objects in line along {axis} axis with {spacing} spacing.");
        }

        void QuickArrange(ArrangementAxis quickAxis) {
            axis = quickAxis;
            ArrangeSelectedObjects();
        }

        void ResetPositions() {
            var selected = Selection.gameObjects;
            if (selected.Length == 0) {
                return;
            }

            Undo.RecordObjects(selected.Select(go => go.transform as Object).ToArray(), "Reset Positions");

            foreach (var go in selected) {
                if (useWorldSpace) {
                    go.transform.position = Vector3.zero;
                } else {
                    go.transform.localPosition = Vector3.zero;
                }
            }

            Log.Important?.Info($"Reset positions for {selected.Length} objects.");
        }

        Transform[] SortTransforms(Transform[] transforms) {
            return sortMode switch {
                SortMode.Hierarchy => transforms,
                SortMode.Position => SortByPosition(transforms),
                SortMode.SizeSmallest => SortBySize(transforms, false),
                SortMode.SizeLargest => SortBySize(transforms, true),
                SortMode.Name => SortByName(transforms),
                _ => transforms
            };
        }

        Transform[] SortByPosition(Transform[] transforms) {
            var sorted = transforms.OrderBy(t => GetPositionAlongAxis(t));
            return sortDescending ? sorted.Reverse().ToArray() : sorted.ToArray();
        }

        Transform[] SortBySize(Transform[] transforms, bool largestFirst) {
            var sorted = largestFirst
                ? transforms.OrderByDescending(t => CalculateObjectSize(t.gameObject))
                : transforms.OrderBy(t => CalculateObjectSize(t.gameObject));
            return sortDescending ? sorted.Reverse().ToArray() : sorted.ToArray();
        }

        Transform[] SortByName(Transform[] transforms) {
            var sorted = transforms.OrderBy(t => t.name);
            return sortDescending ? sorted.Reverse().ToArray() : sorted.ToArray();
        }

        float CalculateObjectSize(GameObject go) {
            var bounds = CalculateObjectBounds(go);
            if (bounds.HasValue) {
                var size = bounds.Value.size;
                return axis switch {
                    ArrangementAxis.X => size.x,
                    ArrangementAxis.Y => size.y,
                    ArrangementAxis.Z => size.z,
                    _ => size.magnitude
                };
            }
            return 0f;
        }

        Bounds? CalculateObjectBounds(GameObject go) {
            Bounds? combinedBounds = null;

            // Try to get bounds from renderers
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers) {
                if (combinedBounds.HasValue) {
                    combinedBounds.Value.Encapsulate(renderer.bounds);
                } else {
                    combinedBounds = renderer.bounds;
                }
            }

            // If no renderers, try colliders
            if (!combinedBounds.HasValue) {
                var colliders = go.GetComponentsInChildren<Collider>();
                foreach (var collider in colliders) {
                    if (combinedBounds.HasValue) {
                        combinedBounds.Value.Encapsulate(collider.bounds);
                    } else {
                        combinedBounds = collider.bounds;
                    }
                }
            }

            return combinedBounds;
        }

        float GetPositionAlongAxis(Transform t) {
            var pos = useWorldSpace ? t.position : t.localPosition;
            return axis switch {
                ArrangementAxis.X => pos.x,
                ArrangementAxis.Y => pos.y,
                ArrangementAxis.Z => pos.z,
                _ => 0f
            };
        }

        Vector3 GetAxisVector() {
            return axis switch {
                ArrangementAxis.X => Vector3.right,
                ArrangementAxis.Y => Vector3.up,
                ArrangementAxis.Z => Vector3.forward,
                _ => Vector3.right
            };
        }

        Vector3 CalculateStartPosition(Transform[] transforms) {
            if (transforms.Length == 0) {
                return Vector3.zero;
            }

            var firstTransform = transforms[0];
            var referencePos = useWorldSpace ? firstTransform.position : firstTransform.localPosition;

            return alignmentMode switch {
                AlignmentMode.Start => GetAxisVector() * GetPositionAlongAxis(firstTransform),
                AlignmentMode.Center => CalculateCenterStartPosition(transforms),
                AlignmentMode.End => CalculateEndStartPosition(transforms),
                _ => referencePos
            };
        }

        Vector3 CalculateCenterStartPosition(Transform[] transforms) {
            var totalLength = (transforms.Length - 1) * spacing;
            var centerOffset = -totalLength / 2f;

            var firstTransform = transforms[0];
            var referencePos = useWorldSpace ? firstTransform.position : firstTransform.localPosition;

            return GetAxisVector() * (GetPositionAlongAxis(firstTransform) + centerOffset);
        }

        Vector3 CalculateEndStartPosition(Transform[] transforms) {
            var totalLength = (transforms.Length - 1) * spacing;

            var firstTransform = transforms[0];
            var referencePos = useWorldSpace ? firstTransform.position : firstTransform.localPosition;

            return GetAxisVector() * (GetPositionAlongAxis(firstTransform) - totalLength);
        }

        Vector3 SetPositionOnAxis(Vector3 currentPos, Vector3 newAxisPos) {
            return axis switch {
                ArrangementAxis.X => new Vector3(newAxisPos.x, currentPos.y, currentPos.z),
                ArrangementAxis.Y => new Vector3(currentPos.x, newAxisPos.y, currentPos.z),
                ArrangementAxis.Z => new Vector3(currentPos.x, currentPos.y, newAxisPos.z),
                _ => currentPos
            };
        }

        enum ArrangementAxis {
            X,
            Y,
            Z
        }

        enum AlignmentMode {
            Start,
            Center,
            End
        }

        enum SortMode {
            Hierarchy,
            Position,
            SizeSmallest,
            SizeLargest,
            Name
        }
    }
}
