using System.Collections.Generic;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.Water;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using Pathfinding;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.EditorOnly.WorkflowTools {
    [DisallowMultipleComponent]
    public class WaterVerifier : MonoBehaviour {
#if UNITY_EDITOR
        [ShowInInspector, 
         InfoBox("Water setup is incorrect", InfoMessageType.Error, VisibleIf = nameof(WaterSetupIncorrectly)),
         InfoBox("Water setup is correct", VisibleIf = nameof(_waterSetupCorrectly))]
        bool _waterSetupCorrectly;
        
        [Title("Checklist")]
        [PropertySpace]
        [EnableGUI, ShowInInspector, InlineButton("@GetOrAddMeshFilter()", "Add MeshFilter", ShowIf = "@!MeshFilter")]
        bool MeshFilter => _meshFilter;
        [EnableGUI, ShowInInspector, InlineButton("@GetOrAddMeshRenderer()", "Add MeshRenderer", ShowIf = "@!MeshRenderer")]
        bool MeshRenderer => _meshRenderer;
        [EnableGUI, ShowInInspector, InlineButton("@GetOrAddMeshCollider()", "Add MeshCollider", ShowIf = "@!MeshCollider")]
        bool MeshCollider => _meshCollider;
        [EnableGUI, ShowInInspector, InlineButton("@GetOrAddRecastMeshObj()", "Add RecastMeshObj", ShowIf = "@!RecastMeshObjStatic")]
        bool RecastMeshObjStatic => _recastMeshObjStatic;
        [EnableGUI, ShowInInspector, InlineButton("@GetOrAddWaterSurfaceTimeScale()", "Add WaterSurface", ShowIf = "@!WaterSurface")]
        bool WaterSurface => _waterSurface;
        [EnableGUI, ShowInInspector, InlineButton("@GetOrAddWaterSurfaceTimeScale()", "Add WaterSurfaceTimeScale", ShowIf = "@!WaterSurfaceTimescale")]
        bool WaterSurfaceTimescale => _waterSurfaceTimeScale;

        public float waterDepth = 10;
        public float waterNavMeshForgiveness = 1;
        [OnValueChanged(nameof(UpdateWindController)), EnableIn(PrefabKind.InstanceInScene)]
        public bool shouldIgnoreWind = false;
        
        Transform _childContainer;
        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;
        MeshCollider _meshCollider;
        RecastMeshObjStatic _recastMeshObjStatic;
        WaterSurface _waterSurface;
        WaterSurfaceTimeScale _waterSurfaceTimeScale;

        public void VerifyWater() {
            if (gameObject.layer != RenderLayers.Water) {
                gameObject.layer = RenderLayers.Water;
            }
            
            if (GetComponent<BoxCollider>() is {} bc) {
                DestroyImmediate(bc);
            }

            if (_childContainer == null) {
                _childContainer = transform.Find("WaterRenderer") ?? new GameObject("WaterRenderer").transform;
            }

            EnsureChildProperties(_childContainer);

            if (_meshFilter == null) {
                _meshFilter = _childContainer.GetComponent<MeshFilter>();
            }
            if (_meshRenderer == null) {
                _meshRenderer = _childContainer.GetComponent<MeshRenderer>();
            }
            if (_meshCollider == null) {
                _meshCollider = gameObject.GetComponent<MeshCollider>();
            }
            if (_recastMeshObjStatic == null) {
                _recastMeshObjStatic = gameObject.GetComponent<RecastMeshObjStatic>();
            }
            if (_waterSurface == null) {
                _waterSurface = gameObject.GetComponent<WaterSurface>();

                UpdateWindController();
            }
            if (_waterSurfaceTimeScale == null) {
                _waterSurfaceTimeScale = gameObject.GetComponent<WaterSurfaceTimeScale>();
            }
            
            if (_meshCollider) {
                if (_meshFilter) {
                    if (_meshCollider.sharedMesh != _meshFilter.sharedMesh) {
                        _meshCollider.sharedMesh = _meshFilter.sharedMesh;
                        UnityEditor.EditorUtility.SetDirty(_meshCollider);
                    }
                }
            }

            if (_meshRenderer) {
                if (_waterSurface && WaterSurfaceRequiresUpdate()) {
                    _waterSurface.enabled = true;
                    _waterSurface.geometryType = WaterGeometryType.Custom;
                    _waterSurface.meshRenderers ??= new List<MeshRenderer>();
                    _waterSurface.meshRenderers.Clear();
                    if (!_waterSurface.meshRenderers.Contains(_meshRenderer)) {
                        _waterSurface.meshRenderers.Add(_meshRenderer);
                    }
                    UnityEditor.EditorUtility.SetDirty(_waterSurface);
                }
                if (_meshRenderer.sharedMaterials.IsNullOrUnityEmpty() || !UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_meshRenderer.sharedMaterials[0], out string guid, out long _) || guid != "ec2706f568b826248a2c9e639b3debd8") {
                    Material[] meshRendererSharedMaterials = new Material[1];
                    // RainObstacle material
                    meshRendererSharedMaterials[0] = UnityEditor.AssetDatabase.LoadAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath("ec2706f568b826248a2c9e639b3debd8"), typeof(Material)) as Material;
                    _meshRenderer.sharedMaterials = meshRendererSharedMaterials;
                    
                    UnityEditor.EditorUtility.SetDirty(_meshRenderer);
                }
            }

            if (_recastMeshObjStatic && RecastGraphObjRequiresUpdate()) {
                _recastMeshObjStatic.mode = Pathfinding.RecastMeshObjStatic.Mode.UnwalkableSurface;
                _recastMeshObjStatic.waterProperties = new WaterProperties(waterDepth, waterNavMeshForgiveness);
                _recastMeshObjStatic.solid = true;
                _recastMeshObjStatic.includeInScan = Pathfinding.RecastMeshObjStatic.ScanInclusion.AlwaysInclude;
                _recastMeshObjStatic.geometrySource = Pathfinding.RecastMeshObjStatic.GeometrySource.Collider;
                UnityEditor.EditorUtility.SetDirty(_recastMeshObjStatic);
            }

            CheckState();
        }
        
        [Button(ButtonSizes.Medium), ShowIf(nameof(WaterSetupIncorrectly))]
        void MigrateComponentsToChildContainer() {
            SendMessage("MoveToWaterChildContainer", SendMessageOptions.DontRequireReceiver);
            MoveToChildIfPresent<MeshFilter>(transform, _childContainer);
            MoveToChildIfPresent<MeshRenderer>(transform, _childContainer);
        }

        void EnsureChildProperties(Transform childContainer) {
            GameObject childGO = childContainer.gameObject;
            if (childGO.layer != RenderLayers.RainObstacle) {
                childGO.layer = RenderLayers.RainObstacle;
                UnityEditor.EditorUtility.SetDirty(childGO);
            }
            
            bool anyChanges = false;
            if (childContainer.parent != transform) {
                childContainer.SetParent(transform);
                anyChanges = true;
            }
            
            if (childContainer.localPosition != Vector3.zero) {
                childContainer.localPosition = Vector3.zero;
                anyChanges = true;
            }
            
            if (childContainer.localRotation != Quaternion.identity) {
                childContainer.localRotation = Quaternion.identity;
                anyChanges = true;
            }
            
            if (childContainer.localScale != Vector3.one) {
                childContainer.localScale = Vector3.one;
                anyChanges = true;
            }
            
            if (anyChanges) {
                UnityEditor.EditorUtility.SetDirty(childContainer);
            }
        }

        static void MoveToChildIfPresent<T>(Transform parent, Transform child) where T : Component {
            T cComponent = child.GetComponent<T>();
            if (!cComponent) {
                cComponent = child.gameObject.AddComponent<T>();
            }

            var pComponent = parent.GetComponent<T>();
            if (cComponent && pComponent) {
                UnityEditor.EditorUtility.CopySerialized(pComponent, cComponent);
                DestroyImmediate(pComponent);
            }
        }

        void UpdateWindController() {
            if (_waterSurface == null) return;
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null) {
                var windController = FindAnyObjectByType<WindController>(FindObjectsInactive.Include);
                if (windController) {
                    if (shouldIgnoreWind) {
                        windController.UnregisterLake(_waterSurface);
                    } else {
                        windController.RegisterLake(_waterSurface);
                    }
                }
            }
        }

        bool WaterSurfaceRequiresUpdate() {
            return _waterSurface.geometryType != WaterGeometryType.Custom
                   || _waterSurface.meshRenderers.IsNullOrEmpty()
                   || _waterSurface.meshRenderers[0] != _meshRenderer;
        }

        bool RecastGraphObjRequiresUpdate() => _recastMeshObjStatic.dynamic
                                               || _recastMeshObjStatic.mode != Pathfinding.RecastMeshObjStatic.Mode.UnwalkableSurface
                                               || _recastMeshObjStatic.waterProperties != new WaterProperties(waterDepth, waterNavMeshForgiveness)
                                               || _recastMeshObjStatic.solid == false
                                               || _recastMeshObjStatic.includeInScan != Pathfinding.RecastMeshObjStatic.ScanInclusion.AlwaysInclude
                                               || _recastMeshObjStatic.geometrySource != Pathfinding.RecastMeshObjStatic.GeometrySource.Collider;


        
        void CheckState() {
            _waterSetupCorrectly = MeshFilter && MeshRenderer && MeshCollider && RecastMeshObjStatic && WaterSurfaceTimescale && WaterSurface;
        }

        void OnDestroy() {
            var windController = FindAnyObjectByType<WindController>(FindObjectsInactive.Include);
            if (windController) {
                windController.UnregisterLake(_waterSurface);
            }
        }

        // === Odin garbage
        bool WaterSetupIncorrectly => !_waterSetupCorrectly;
        
        [UsedImplicitly]
        void GetOrAddMeshFilter() {
            _meshFilter = _childContainer.gameObject.GetOrAddComponent<MeshFilter>();
        }
        
        [UsedImplicitly]
        void GetOrAddMeshRenderer() {
            _meshRenderer = _childContainer.gameObject.GetOrAddComponent<MeshRenderer>();
        }
        
        [UsedImplicitly]
        void GetOrAddMeshCollider() {
            _meshCollider = gameObject.GetOrAddComponent<MeshCollider>();
        }

        [UsedImplicitly]
        void GetOrAddRecastMeshObj() {
            _recastMeshObjStatic = gameObject.GetOrAddComponent<RecastMeshObjStatic>();
        }
        
        [UsedImplicitly]
        void GetOrAddWaterSurface() {
            _waterSurface = gameObject.GetOrAddComponent<WaterSurface>();
        }
        
        [UsedImplicitly]
        void GetOrAddWaterSurfaceTimeScale() {
            _waterSurfaceTimeScale = gameObject.GetOrAddComponent<WaterSurfaceTimeScale>();
        }
#endif
    }
}