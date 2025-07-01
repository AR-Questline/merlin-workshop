using System;
using System.Text;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.Editor;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEditor;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    [CustomEditor(typeof(DrakeLodGroup))]
    public class DrakeLodGroupEditor : UnityEditor.Editor {
        const float CameraIconSize = 32;
        const float CameraTopOffset = 4;
        const float CameraBottomOffset = 1;
        const float HalfCameraIconSize = CameraIconSize * 0.5f;
        const float SliderHeight = CameraIconSize+CameraTopOffset+CameraBottomOffset;

        static readonly StringBuilder LodStatsBuilder = new StringBuilder(128);
        static readonly int CameraSliderId = "DrakeLODCameraIDHash".GetHashCode();
        static readonly Color[] LodColors = {
            new Color(0.4831376f, 0.6211768f, 0.0219608f, 1f),
            new Color(0.279216f, 0.4078432f, 0.5835296f, 1f),
            new Color(0.2070592f, 0.5333336f, 0.6556864f, 1f),
            new Color(0.5333336f, 0.16f, 0.0282352f, 1f),
            new Color(0.3827448f, 0.2886272f, 0.5239216f, 1f),
            new Color(0.8f, 0.4423528f, 0.0f, 1f),
            new Color(0.4486272f, 0.4078432f, 0.050196f, 1f),
            new Color(0.7749016f, 0.6368624f, 0.0250984f, 1f)
        };
        static readonly Color CulledLODColor = new Color(0.4f, 0.0f, 0.0f, 1f);

        static GUIContent s_cameraIcon;
        static GUIStyle s_coloredBox;
        static GUIStyle ColoredBox {
            get {
                if (s_coloredBox == null) {
                    s_coloredBox = new GUIStyle(GUI.skin.box);
                    s_coloredBox.normal.background = EditorGUIUtility.whiteTexture;
                    s_coloredBox.normal.scaledBackgrounds = Array.Empty<Texture2D>();
                }
                return s_coloredBox;
            }
        }

        static float s_normalizedCameraDistance = 0.1f;
        static Tool s_previousTool;

        bool _debugExpanded;

        void OnEnable() {
            s_cameraIcon = EditorGUIUtility.IconContent("Camera Icon");
            SceneView.duringSceneGui += Repaint;
        }

        void OnDisable() {
            SceneView.duringSceneGui -= Repaint;
        }

        void Repaint(SceneView _) {
            Repaint();
        }

        public override void OnInspectorGUI() {
            var drakeLodGroup = (DrakeLodGroup)target;
            var isEditable = PrefabsHelper.IsLowestEditablePrefabStage(drakeLodGroup);

            if (DrawInspector(drakeLodGroup, isEditable)) {
                return;
            }

            if (drakeLodGroup.IsStatic != drakeLodGroup.gameObject.isStatic) {
                drakeLodGroup.BakeStatic();
                EditorUtility.SetDirty(drakeLodGroup);
            }

            DrawDebugInspector();
        }

        bool DrawInspector(DrakeLodGroup drakeLodGroup, bool isEditable) {
            using var _ = new EditorGUI.DisabledGroupScope(!isEditable);
            if (DrawValidationAndOperations(drakeLodGroup)) {
                return true;
            }

            DrawLods(drakeLodGroup);
            return false;
        }

        void DrawDebugInspector() {
            GUILayout.Space(8);
            _debugExpanded = EditorGUILayout.Foldout(_debugExpanded, "Debug", true);
            if (!_debugExpanded) {
                return;
            }
            using var _ = new EditorGUI.DisabledGroupScope(true);
            base.OnInspectorGUI();
        }

        bool DrawValidationAndOperations(DrakeLodGroup drakeLodGroup) {
            var state = DrakeLodGroupEditorHelper.GetDrakeLodGroupState(drakeLodGroup, out var lodGroup);
            if (state == DrakeLodGroupState.BakedButUnityPresent) {
                EditorGUILayout.HelpBox("Baked DrakeLodGroup and LODGroup found.", MessageType.Error);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove lod group")) {
                    DestroyImmediate(lodGroup);
                }
                if (GUILayout.Button("Remove DrakeLodGroup")) {
                    DestroyImmediate(drakeLodGroup);
                    return true;
                }
                EditorGUILayout.EndHorizontal();
            } else if (state == DrakeLodGroupState.CorrectlyBaked) {
                if (GUILayout.Button("Authoring mode")) {
                    DrakeEditorHelpers.Unbake(drakeLodGroup);
                    return true;
                }
            } else if (state == DrakeLodGroupState.NotBaked) {
                EditorGUILayout.HelpBox("DrakeLodGroup is not baked so shouldn't be here.", MessageType.Error);
                if (GUILayout.Button("Exchange DrakeLodGroup with DrakeToBake")) {
                    var gameObject = drakeLodGroup.gameObject;
                    DestroyImmediate(drakeLodGroup);
                    gameObject.AddComponent<DrakeToBake>();
                    return true;
                }
            } else if (state == DrakeLodGroupState.UnknownState) {
                EditorGUILayout.HelpBox("DrakeLodGroup is in unknown state.", MessageType.Error);
            }
            return false;
        }

        // === Lods drawing
        static void DrawLods(DrakeLodGroup drakeLodGroup) {
            var sceneLodData = UpdateCamera(drakeLodGroup);
            DrawLodStats(drakeLodGroup, sceneLodData);
            if (!sceneLodData.camera.orthographic) {
                DrawCameraLodsSlider(drakeLodGroup, sceneLodData);
            }
        }

        static void DrawLodStats(DrakeLodGroup drakeLodGroup, in SceneLodData sceneLodData) {
            var lodsData = drakeLodGroup.MeshLODGroupComponent;
            var previousDistance = 0f;
            var previousWasValid = true;

            var firstTrisCount = 0u;
            var firstVertsCount = 0;

            var previousTrisCount = 0u;
            var previousVertsCount = 0;

            for (var i = 0; previousWasValid && i < 8; ++i) {
                var distance = i < 4 ? lodsData.LODDistances0[i] : lodsData.LODDistances1[i - 4];
                if (!float.IsFinite(distance)) {
                    previousWasValid = false;
                    continue;
                }

                var active = IsCurrentLod(distance, previousDistance, sceneLodData.maxDistance);

                var mask = 1 << i;
                var trisCount = 0u;
                var vertsCount = 0;
                var renderersCount = 0;
                foreach (var drakeMeshRenderer in drakeLodGroup.Renderers) {
                    if ((drakeMeshRenderer.LodMask & mask) == 0) {
                        continue;
                    }
                    var mesh = DrakeEditorHelpers.LoadAsset<Mesh>(drakeMeshRenderer.MeshReference);
                    DrakeEditorHelpers.MeshStats(mesh, out var vertexCount, out var triangleCount, out _);
                    trisCount += triangleCount;
                    vertsCount += vertexCount;
                    renderersCount++;
                }

                if (i == 0) {
                    firstTrisCount = trisCount;
                    firstVertsCount = vertsCount;
                    previousTrisCount = trisCount;
                    previousVertsCount = vertsCount;
                }

                LodStatsBuilder.Append("LOD");
                LodStatsBuilder.Append(i);
                LodStatsBuilder.Append(" <");
                LodStatsBuilder.Append(previousDistance.ToString("f0"));
                LodStatsBuilder.Append("..");
                LodStatsBuilder.Append(distance.ToString("f0"));
                LodStatsBuilder.Append("> with ");
                LodStatsBuilder.Append(renderersCount);
                LodStatsBuilder.Append(" renderers");

                var contentColor = GUI.contentColor;
                GUI.contentColor = active ? Color.blue : Color.white;
                EditorGUILayout.LabelField(LodStatsBuilder.ToString());
                GUI.contentColor = contentColor;
                LodStatsBuilder.Clear();

                var vertsOriginalPercentage = (float)vertsCount / firstVertsCount;
                var vertsPreviousPercentage = (float)vertsCount / previousVertsCount;
                LodStatsBuilder.Append("\t");
                LodStatsBuilder.Append(vertsCount);
                if (i > 0) {
                    LodStatsBuilder.Append("[");
                    LodStatsBuilder.Append(vertsOriginalPercentage.ToString("P1"));
                    LodStatsBuilder.Append("LOD0");
                    if (i > 1) {
                        LodStatsBuilder.Append("|");
                        LodStatsBuilder.Append(vertsPreviousPercentage.ToString("P1"));
                        LodStatsBuilder.Append("LOD");
                        LodStatsBuilder.Append(i-1);
                    }
                    LodStatsBuilder.Append("]");
                }
                LodStatsBuilder.Append(" verts");

                EditorGUILayout.LabelField(LodStatsBuilder.ToString());
                LodStatsBuilder.Clear();

                var trisOriginalPercentage = (float)trisCount / firstTrisCount;
                var trisPreviousPercentage = (float)trisCount / previousTrisCount;
                LodStatsBuilder.Append("\t");
                LodStatsBuilder.Append(trisCount);
                if (i > 0) {
                    LodStatsBuilder.Append("[");
                    LodStatsBuilder.Append(trisOriginalPercentage.ToString("P1"));
                    LodStatsBuilder.Append("LOD0");
                    if (i > 1) {
                        LodStatsBuilder.Append("|");
                        LodStatsBuilder.Append(trisPreviousPercentage.ToString("P1"));
                        LodStatsBuilder.Append("LOD");
                        LodStatsBuilder.Append(i - 1);
                    }
                    LodStatsBuilder.Append("]");
                }
                LodStatsBuilder.Append(" tris");

                EditorGUILayout.LabelField(LodStatsBuilder.ToString());


                LodStatsBuilder.Clear();

                previousDistance = distance;
                previousTrisCount = trisCount;
                previousVertsCount = vertsCount;
            }
            EditorGUILayout.LabelField($"Cull <{previousDistance:f0}..{float.PositiveInfinity:f0}>");
        }

        // -- Camera LODs slider
        static void DrawCameraLodsSlider(DrakeLodGroup drakeLodGroup, in SceneLodData sceneLodData) {
            var wasEnabled = GUI.enabled;
            GUI.enabled = true;

            var currentDistance = sceneLodData.maxDistance * s_normalizedCameraDistance;
            EditorGUILayout.LabelField($"Camera distance: {currentDistance:f2}");
            var fullRect = GUILayoutUtility.GetRect(0, SliderHeight, GUILayout.ExpandWidth(true));
            DrawLodsBackground(drakeLodGroup, sceneLodData.maxDistance, fullRect);
            var changed = DrawCameraSlider(fullRect);

            if (changed) {
                var worldDistance = s_normalizedCameraDistance * sceneLodData.maxDistance;
                worldDistance /= sceneLodData.distanceScale;
                float size;
                if (sceneLodData.camera.orthographic) {
                    size = worldDistance;
                    if (sceneLodData.camera.aspect < 1.0) {
                        size *= sceneLodData.camera.aspect;
                    }
                } else {
                    var fov = sceneLodData.camera.fieldOfView;
                    size = worldDistance * Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
                }

                var cameraLookAt = Quaternion.LookRotation(sceneLodData.vectorFromCamera.normalized, Vector3.up);
                SceneView.lastActiveSceneView.LookAtDirect(sceneLodData.worldReferencePoint, cameraLookAt, size);
            }

            GUI.enabled = wasEnabled;
        }

        static void DrawLodsBackground(DrakeLodGroup drakeLodGroup, float maxDistance, Rect fullWidthRect) {
            var contentColor = GUI.contentColor;

            fullWidthRect.x += HalfCameraIconSize;
            var startX = fullWidthRect.x;
            var fullWidth = fullWidthRect.width - CameraIconSize;
            fullWidthRect.width = 0;
            var previousDistance = 0f;
            for (var i = 0; i < 8; ++i) {
                var distance = i < 4 ?
                    drakeLodGroup.MeshLODGroupComponent.LODDistances0[i] :
                    drakeLodGroup.MeshLODGroupComponent.LODDistances1[i - 4];
                if (!float.IsFinite(distance)) {
                    continue;
                }
                var percentage = (distance-previousDistance) / maxDistance;
                var width = fullWidth * percentage;
                fullWidthRect.x += fullWidthRect.width;
                fullWidthRect.width = width;

                DrawColorBox(fullWidthRect, LodColors[i]);
                GUI.contentColor = IsCurrentLod(distance, previousDistance, maxDistance) ? Color.blue : Color.white;
                GUI.Label(fullWidthRect, $"LOD{i}", EditorStyles.whiteLabel);

                previousDistance = distance;
            }

            fullWidthRect.x += fullWidthRect.width;
            fullWidthRect.width = fullWidth-(fullWidthRect.x-startX);

            DrawColorBox(fullWidthRect, CulledLODColor);
            GUI.contentColor = IsCurrentLod(float.PositiveInfinity, previousDistance, maxDistance) ? Color.blue : Color.white;
            GUI.Label(fullWidthRect, "Cull", EditorStyles.whiteLabel);

            GUI.contentColor = contentColor;
        }

        static bool DrawCameraSlider(in Rect fullWidthRect) {
            int cameraId = GUIUtility.GetControlID(CameraSliderId, FocusType.Passive);

            var cameraRect = CalcCameraRect(fullWidthRect, s_normalizedCameraDistance);
            var sliderRect = CalcLineRect(cameraRect);

            Event evt = Event.current;

            var changed = false;
            if (evt.GetTypeForControl(cameraId) == EventType.Repaint) {
                DrawColorBox(sliderRect, Color.black);
                GUI.Label(cameraRect, s_cameraIcon, GUIStyle.none);
            } else if (evt.GetTypeForControl(cameraId) == EventType.MouseDown) {
                if (cameraRect.Contains(evt.mousePosition)) {
                    evt.Use();
                    GUIUtility.hotControl = cameraId;
                    s_previousTool = Tools.current;
                    Tools.current = Tool.None;
                    changed = true;
                }
            } else if (evt.GetTypeForControl(cameraId) == EventType.MouseDrag) {
                if (GUIUtility.hotControl == cameraId) {
                    evt.Use();
                    var remappedPosition =
                        math.clamp(evt.mousePosition.x, fullWidthRect.x + HalfCameraIconSize, fullWidthRect.xMax - HalfCameraIconSize);
                    remappedPosition -= fullWidthRect.x + HalfCameraIconSize;
                    s_normalizedCameraDistance = math.clamp(remappedPosition / (fullWidthRect.width - CameraIconSize), 0.001f, 1f);
                    changed = true;
                }
            } else if (evt.GetTypeForControl(cameraId) == EventType.MouseUp) {
                if (GUIUtility.hotControl == cameraId) {
                    GUIUtility.hotControl = 0;
                    evt.Use();
                    changed = true;
                    Tools.current = s_previousTool;
                }
            }

            return changed;
        }

        // === Scene LODs
        void OnSceneGUI() {
            var drakeLodGroup = (DrakeLodGroup)target;

            var position = (Vector3)drakeLodGroup.LodGroupSerializableData.ToWorldReferencePoint().Value;

            var camera = SceneView.currentDrawingSceneView.camera;
            var fov = 70f;
            if (!camera.orthographic) {
                fov = camera.fieldOfView;
            }
            var distanceScale = LODGroupExtensions.CalculateLodDistanceScale(fov, QualitySettings.lodBias, false, 0);
            var distanceScaleRcp = math.rcp(distanceScale);

            for (var i = 0; i < 8; ++i) {
                var distance = i < 4 ?
                    drakeLodGroup.MeshLODGroupComponent.LODDistances0[i] :
                    drakeLodGroup.MeshLODGroupComponent.LODDistances1[i - 4];
                if (!float.IsFinite(distance)) {
                    continue;
                }

                var radius = distance * distanceScaleRcp;
                HandlesUtils.DrawSphere(position, radius, LodColors[i]);
            }
        }

        // === Helpers
        static SceneLodData UpdateCamera(DrakeLodGroup drakeLodGroup) {
            var localReferencePoint = drakeLodGroup.MeshLODGroupComponent.LocalReferencePoint;
            var worldReferencePoint = drakeLodGroup.transform.TransformPoint(localReferencePoint);

            var camera = SceneView.lastActiveSceneView.camera;
            var lodParams = LODGroupExtensions.CalculateLODParams(camera);
            var cameraPosition = camera.transform.position;

            var vectorFromCamera = worldReferencePoint - cameraPosition;
            var currentDistance = vectorFromCamera.magnitude * lodParams.distanceScale;

            var maxDistance = 0f;
            for (var i = 0; i < 8; ++i) {
                var distance = i < 4 ?
                    drakeLodGroup.MeshLODGroupComponent.LODDistances0[i] :
                    drakeLodGroup.MeshLODGroupComponent.LODDistances1[i - 4];
                if (float.IsFinite(distance)) {
                    maxDistance = Mathf.Max(maxDistance, distance);
                }
            }
            maxDistance *= 1.1f;

            s_normalizedCameraDistance = currentDistance / maxDistance;

            return new SceneLodData(camera, maxDistance, lodParams.distanceScale, worldReferencePoint, vectorFromCamera);
        }

        static Rect CalcCameraRect(Rect totalRect, float percentage) {
            totalRect.width -= CameraIconSize;
            totalRect.y += CameraTopOffset;
            totalRect.height -= CameraTopOffset + CameraBottomOffset;
            totalRect.x += Mathf.Round(totalRect.width * math.saturate(percentage));
            totalRect.width = CameraIconSize;
            return totalRect;
        }

        static Rect CalcLineRect(Rect cameraRect) {
            cameraRect.y += cameraRect.height * 0.5f;
            cameraRect.height = SliderHeight - CameraTopOffset - HalfCameraIconSize;
            cameraRect.x += HalfCameraIconSize-1;
            cameraRect.width = 2;
            return cameraRect;
        }

        static void DrawColorBox(in Rect rect, in Color color) {
            var backgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Box(rect, GUIContent.none, ColoredBox);
            GUI.backgroundColor = backgroundColor;
        }

        static bool IsCurrentLod(float currentDistance, float previousDistance, float maxDistance) {
            var cameraDistance = math.saturate(s_normalizedCameraDistance) * maxDistance;
            return previousDistance <= cameraDistance && cameraDistance < currentDistance;
        }

        readonly struct SceneLodData {
            public readonly Camera camera;
            public readonly float maxDistance;
            public readonly float distanceScale;
            public readonly Vector3 worldReferencePoint;
            public readonly Vector3 vectorFromCamera;

            public SceneLodData(Camera camera, float maxDistance, float distanceScale, Vector3 worldReferencePoint, Vector3 vectorFromCamera) {
                this.camera = camera;
                this.maxDistance = maxDistance;
                this.distanceScale = distanceScale;
                this.worldReferencePoint = worldReferencePoint;
                this.vectorFromCamera = vectorFromCamera;
            }
        }
    }
}
