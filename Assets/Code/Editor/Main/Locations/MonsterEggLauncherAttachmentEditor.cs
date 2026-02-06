using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.Utility.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Locations {
    [CustomEditor(typeof(MonsterEggLauncherAttachment))]
    public class MonsterEggLauncherAttachmentEditor : UnityEditor.Editor {
        MonsterEggLauncherAttachment _target;
        Tool _lastTool = Tool.None;
        bool _showTargets = false;
        static bool s_showGizmos = false;
        
        // Cached style to avoid creating new one every frame
        static GUIStyle _labelStyle;
        static GUIStyle LabelStyle {
            get {
                if (_labelStyle == null) {
                    _labelStyle = new GUIStyle(EditorStyles.boldLabel) {
                        normal = { textColor = Color.white },
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                return _labelStyle;
            }
        }

        void OnEnable() {
            _target = (MonsterEggLauncherAttachment)target;
            _lastTool = Tools.current;
        }

        void OnDisable() {
            Tools.current = _lastTool;
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            DrawHeader();
            DrawProjectileSettings();
            DrawTimingSettings();
            DrawTargetSettings();
            DrawExplosionSettings();
            DrawTargetsList();
            DrawDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawHeader() {
            EditorGUILayout.LabelField("Monster Egg Launcher", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.shouldStartEnabled)), new GUIContent("Start Enabled"));

            EditorGUILayout.Space();
        }

        void DrawProjectileSettings() {
            var projectileAssetProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.projectileAsset));
            EditorGUILayout.PropertyField(projectileAssetProp, new GUIContent("Projectile Asset"));
            
            var projectileSpeedProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.projectileSpeed));
            EditorGUILayout.PropertyField(projectileSpeedProp, new GUIContent("Projectile Speed"));
            
            var highShotProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.highShot));
            EditorGUILayout.PropertyField(highShotProp, new GUIContent("High Shot", "Use high arc trajectory for projectiles"));
        }

        void DrawTimingSettings() {
            var launchIntervalMinProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.launchIntervalMin));
            EditorGUILayout.PropertyField(launchIntervalMinProp, new GUIContent("Launch Interval Min (s)"));
            
            var launchIntervalMaxProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.launchIntervalMax));
            EditorGUILayout.PropertyField(launchIntervalMaxProp, new GUIContent("Launch Interval Max (s)"));
        }

        void DrawTargetSettings() {
            var eggLandingOffsetProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.eggLandingOffset));
            EditorGUILayout.PropertyField(eggLandingOffsetProp, new GUIContent("Egg Landing Offset"));
            
            var maxLaunchDistanceProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.maxDistanceOfTargetFromHero));
            EditorGUILayout.PropertyField(maxLaunchDistanceProp, new GUIContent("Max Distance of Target From Hero"));
            
            EditorGUI.indentLevel++;
            var shouldUsePredictionProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.shouldUsePrediction));
            EditorGUILayout.PropertyField(shouldUsePredictionProp, new GUIContent("Use Prediction"));
            EditorGUI.indentLevel--;
        }

        void DrawExplosionSettings() {
            var explosionProp = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.explosion));
            var explosionEnabledProp = explosionProp.FindPropertyRelative("enabled");
            
            GUIUtils.PushLabelWidth(120f);
            EditorGUILayout.PropertyField(explosionEnabledProp, new GUIContent("Enable Explosions"));
            GUIUtils.PopLabelWidth();
            
            if (explosionEnabledProp.boolValue) {
                DrawExplosionDetails(explosionProp);
            }
            
            EditorGUILayout.Space();
        }

        void DrawExplosionDetails(SerializedProperty explosionProp) {
            EditorGUI.indentLevel++;
            
            var explosionRadiusProp = explosionProp.FindPropertyRelative("radius");
            EditorGUILayout.PropertyField(explosionRadiusProp, new GUIContent("Radius"));
            
            var explosionDurationProp = explosionProp.FindPropertyRelative("duration");
            EditorGUILayout.PropertyField(explosionDurationProp, new GUIContent("Duration"));
            
            var explosionDamageProp = explosionProp.FindPropertyRelative("damage");
            EditorGUILayout.PropertyField(explosionDamageProp, new GUIContent("Damage"));
            
            var explosionDamageTypeProp = explosionProp.FindPropertyRelative("damageType");
            EditorGUILayout.PropertyField(explosionDamageTypeProp, new GUIContent("Damage Type"));
            
            var forceDamageProp = explosionProp.FindPropertyRelative("forceDamage");
            EditorGUILayout.PropertyField(forceDamageProp, new GUIContent("Force Damage"));
            
            var poiseDamageProp = explosionProp.FindPropertyRelative("poiseDamage");
            EditorGUILayout.PropertyField(poiseDamageProp, new GUIContent("Poise Damage"));
            
            var persistentAoEProp = explosionProp.FindPropertyRelative("persistentAoE");
            EditorGUILayout.PropertyField(persistentAoEProp, new GUIContent("Persistent AoE", "Location to spawn at explosion contact point"));
            
            EditorGUI.indentLevel--;
        }

        void DrawTargetsList() {
            _showTargets = EditorGUILayout.Foldout(_showTargets, $"Targets ({_target.targets.Length})", EditorStyles.foldoutHeader);
            
            DrawTargetButtons();
            if (_showTargets) {
                DrawTargetHelpBox();
                if (_target.targets.Length > 0) {
                    DrawTargetItems();
                }
            }
        }

        void DrawTargetButtons() {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Target", GUILayout.Width(100))) {
                AddTarget();
                serializedObject.Update();
            }
                
            if (GUILayout.Button("Snap All to Surfaces", GUILayout.Width(120))) {
                SnapAllToSurfaces();
            }
            
            if (GUILayout.Button("Clear All", GUILayout.Width(100))) {
                ClearAllTargets();
                serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        void DrawTargetItems() {
            var targetsProperty = serializedObject.FindProperty(nameof(MonsterEggLauncherAttachment.targets));
            
            for (int i = 0; i < _target.targets.Length; i++) {
                if (i >= targetsProperty.arraySize) {
                    break; // Skip if serialized property array is out of sync
                }
                
                DrawSingleTarget(i, targetsProperty);
            }
        }

        void DrawSingleTarget(int index, SerializedProperty targetsProperty) {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Target {index}", EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(25))) {
                RemoveTarget(index);
                serializedObject.Update();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return; // Exit early since array changed
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.BeginChangeCheck();
            var target = _target.targets[index];
            
            // Position field
            var newPos = EditorGUILayout.Vector3Field("Position", target.position);
            
            // Spawner field with bounds check
            var spawnerProp = targetsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(nameof(MonsterEggTarget.spawnerToSpawn));
            EditorGUILayout.PropertyField(spawnerProp, new GUIContent("Spawner To Spawn"));
            
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(_target, "Modify Target");
                target.position = newPos;
                _target.targets[index] = target;
                EditorUtility.SetDirty(_target);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        void DrawTargetHelpBox() {
            EditorGUILayout.HelpBox("Add Target: Raycasts from Scene view camera to first non-trigger collider\nSnap All to Surfaces: Snaps all existing positions to surfaces below them\nDrag handles in Scene view to move positions", MessageType.Info);
        }

        void OnSceneGUI() {
            if (_target == null || _target.targets == null) return;

            // Enable handle interaction
            Tools.current = Tool.None;

            var transform = _target.transform;
            var launcherPos = transform.position;
            var sceneCamera = SceneView.lastActiveSceneView?.camera;

            // Draw and handle each target position
            for (int i = 0; i < _target.targets.Length; i++) {
                var target = _target.targets[i];
                var worldPos = transform.TransformPoint(target.position);

                EditorGUI.BeginChangeCheck();
                
                // Create position handle
                var newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                
                // Only draw label if position is in front of camera
                if (sceneCamera != null) {
                    var cameraToPoint = worldPos - sceneCamera.transform.position;
                    var dotProduct = Vector3.Dot(cameraToPoint.normalized, sceneCamera.transform.forward);
                    
                    // Only draw label if target is in front of camera (dot product > 0)
                    if (dotProduct > 0) {
                        var labelPos = worldPos + Vector3.up * 0.8f;
                        var labelContent = $"Target {i}";
                        
                        // Calculate label size for background
                        var labelSize = LabelStyle.CalcSize(new GUIContent(labelContent));
                        var labelRect = new Rect(0, 0, labelSize.x + 8, labelSize.y + 4);
                        
                        // Draw with background
                        Handles.BeginGUI();
                        var screenPos = HandleUtility.WorldToGUIPoint(labelPos);
                        labelRect.center = screenPos;
                        
                        // Draw semi-transparent background
                        var oldColor = GUI.color;
                        GUI.color = new Color(0, 0, 0, 0.7f);
                        GUI.DrawTexture(labelRect, EditorGUIUtility.whiteTexture);
                        GUI.color = oldColor;
                        
                        // Draw the label
                        GUI.Label(labelRect, labelContent, LabelStyle);
                        Handles.EndGUI();
                    }
                }

                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(_target, $"Move Target {i}");
                    var newLocalPos = transform.InverseTransformPoint(newWorldPos);
                    target.position = newLocalPos;
                    _target.targets[i] = target;
                    EditorUtility.SetDirty(_target);
                }

                // Draw connection line
                Handles.color = Color.yellow;
                Handles.DrawLine(launcherPos, worldPos);
                
                // Draw direction arrow
                var direction = (worldPos - launcherPos).normalized;
                if (direction != Vector3.zero) {
                    var arrowSize = Mathf.Min(Vector3.Distance(launcherPos, worldPos) * 0.1f, 1f);
                    Handles.ArrowHandleCap(0, worldPos - direction * arrowSize, Quaternion.LookRotation(-direction), arrowSize, EventType.Repaint);
                }

                // Draw gizmos if enabled
                if (s_showGizmos) {
                    DrawRangeGizmos(worldPos);
                }
            }
        }

        void DrawDebugSection() {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            var buttonText = s_showGizmos ? "Hide Range Gizmos" : "Show Range Gizmos";
            if (GUILayout.Button(buttonText, GUILayout.Width(150))) {
                s_showGizmos = !s_showGizmos;
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawRangeGizmos(Vector3 targetPosition) {
            // Draw egg landing offset range circle
            Handles.color = new Color(1f, 1f, 0f, 0.5f); // Semi-transparent yellow
            Handles.DrawWireDisc(targetPosition, Vector3.up, _target.eggLandingOffset);
            
            // Draw filled circle for better visibility
            Handles.color = new Color(1f, 1f, 0f, 0.1f); // More transparent yellow fill
            Handles.DrawSolidDisc(targetPosition, Vector3.up, _target.eggLandingOffset);
            
            // Draw explosion radius if enabled
            if (_target.explosion.enabled) {
                var totalRadius = _target.eggLandingOffset + _target.explosion.radius;
                
                // Draw outer circle for explosion range
                Handles.color = new Color(1f, 0.5f, 0f, 0.5f); // Semi-transparent orange
                Handles.DrawWireDisc(targetPosition, Vector3.up, totalRadius);
                
                // Draw filled circle for explosion area
                Handles.color = new Color(1f, 0.5f, 0f, 0.1f); // More transparent orange fill
                Handles.DrawSolidDisc(targetPosition, Vector3.up, totalRadius);
            }
        }

        void AddTarget(Vector3? position = null) {
            if (position.HasValue) {
                // Direct position specified - use it as-is
                Undo.RecordObject(_target, "Add Target");
                
                var newArray = new MonsterEggTarget[_target.targets.Length + 1];
                for (int i = 0; i < _target.targets.Length; i++) {
                    newArray[i] = _target.targets[i];
                }
                newArray[_target.targets.Length] = new MonsterEggTarget { 
                    position = position.Value,
                    spawnerToSpawn = default
                };
                
                _target.targets = newArray;
                EditorUtility.SetDirty(_target);
                return;
            }

            // Use raycast from Scene view camera to find surface
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || _target == null) return;

            var cameraTransform = sceneView.camera.transform;
            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            
            // Find first non-trigger collider hit
            var hits = Physics.RaycastAll(ray, Mathf.Infinity);
            RaycastHit? validHit = null;
            
            float closestDistance = Mathf.Infinity;
            foreach (var hit in hits) {
                // Skip trigger colliders
                if (hit.collider.isTrigger) continue;
                
                if (hit.distance < closestDistance) {
                    closestDistance = hit.distance;
                    validHit = hit;
                }
            }
            
            if (validHit.HasValue) {
                Undo.RecordObject(_target, "Add Target");
                
                var worldPos = validHit.Value.point;
                var localPos = _target.transform.InverseTransformPoint(worldPos);
                
                var newArray = new MonsterEggTarget[_target.targets.Length + 1];
                for (int i = 0; i < _target.targets.Length; i++) {
                    newArray[i] = _target.targets[i];
                }
                newArray[_target.targets.Length] = new MonsterEggTarget {
                    position = localPos,
                    spawnerToSpawn = default
                };
                
                _target.targets = newArray;
                EditorUtility.SetDirty(_target);
            }
        }

        void RemoveTarget(int index) {
            if (index < 0 || index >= _target.targets.Length) return;
            
            Undo.RecordObject(_target, "Remove Target");
            
            var newArray = new MonsterEggTarget[_target.targets.Length - 1];
            int newIndex = 0;
            
            for (int i = 0; i < _target.targets.Length; i++) {
                if (i != index) {
                    newArray[newIndex] = _target.targets[i];
                    newIndex++;
                }
            }
            
            _target.targets = newArray;
            EditorUtility.SetDirty(_target);
        }

        void ClearAllTargets() {
            if (_target.targets.Length == 0) return;
            
            Undo.RecordObject(_target, "Clear All Targets");
            _target.targets = new MonsterEggTarget[0];
            EditorUtility.SetDirty(_target);
        }

        void SnapAllToSurfaces() {
            if (_target == null || _target.targets.Length == 0) return;
            
            bool alreadyRecording = false;
            const float SurfaceThreshold = 0.1f; // Distance threshold to consider "already at surface"
            
            for (int i = 0; i < _target.targets.Length; i++) {
                var target = _target.targets[i];
                var worldPos = _target.transform.TransformPoint(target.position);
                
                // Use Ground utility to find surface below the position
                var groundHeight = Ground.HeightAt(worldPos, Ground.LayerMask, Ground.FindClosestType.FindClosest);
                var surfaceWorldPos = new Vector3(worldPos.x, groundHeight, worldPos.z);
                var distanceToSurface = Vector3.Distance(worldPos, surfaceWorldPos);
                
                // Only snap if position is not already close to the surface
                if (distanceToSurface > SurfaceThreshold) {
                    if (!alreadyRecording) {
                        Undo.RecordObject(_target, "Snap All Targets to Surfaces");
                        alreadyRecording = true;
                    }
                    var newLocalPos = _target.transform.InverseTransformPoint(surfaceWorldPos);
                    target.position = newLocalPos;
                    _target.targets[i] = target;
                }
            }
            
            if (alreadyRecording) {
                EditorUtility.SetDirty(_target);
            }
        }
    }
}
