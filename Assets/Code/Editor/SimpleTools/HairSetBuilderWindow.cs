using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.SimpleTools {
    public class HairSetBuilderWindow : OdinEditorWindow {
        [MenuItem("TG/Assets/Hair-set Builder", priority = 100)]
        static void OpenWindow() {
            var window = GetWindow<HairSetBuilderWindow>();
            window.Show();
        }

        [SerializeField, FolderPath, ValidateInput("@" + nameof(ValidFolder), "Folder must exist")]
        string hairSetPath = "Assets/3DAssets/Characters/Humans";

        [SerializeField, ValidateInput("@" + nameof(ValidName), "Name must not be empty")]
        string hairSetName;

        [SerializeField, ValidateInput("@" + nameof(ValidPieces), "Pieces must not be empty"), OnValueChanged(nameof(BasicUpdatePreview))]
        List<GameObject> piecesToCombine = new();

        bool CanGenerate => ValidPieces && ValidName && ValidFolder;

        bool ValidPieces => piecesToCombine.Count > 0;
        bool ValidFolder => AssetDatabase.IsValidFolder(hairSetPath);
        bool ValidName => !string.IsNullOrEmpty(hairSetName);

        [Button]
        void FillPiecesFromSelection() {
            piecesToCombine.Clear();
            foreach (var o in Selection.objects) {
                if (o is GameObject go) {
                    piecesToCombine.Add(go);
                }
            }

            UpdatePreview();
        }

        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox), EnableIf("@" + nameof(CanGenerate))]
        void GenerateAsset() {
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(hairSetPath + "/" + hairSetName + ".prefab");
            GameObject hairSet = new GameObject(hairSetName);

            foreach (GameObject piece in piecesToCombine) {
                PrefabUtility.InstantiatePrefab(piece, hairSet.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(hairSet, assetPath);
            DestroyImmediate(hairSet);
            SearchUtils.PingAsset(assetPath);
        }
        
        // === Preview Input Handling ===

        List<GameObject> _previewObjects = new();

        void BasicUpdatePreview() {
            UpdatePreview(false);
        }
        void UpdatePreview(bool withRender = true) {
            _previewObjects.ForEach(DestroyImmediate);
            _previewObjects.Clear();
            if (_resultImage == null) {
                CreatePreview();
            }
            if (!ValidPieces) {
                _resultImage.image = Texture2D.grayTexture;
                if (withRender) RenderPreview();
                return;
            }
            
            foreach (GameObject go in piecesToCombine) {
                _previewObjects.Add(_preview.InstantiatePrefabInScene(go));
                Component[] components = _previewObjects[^1].GetComponentsInChildren<Component>();
                for (var index = 0; index < components.Length; index++) {
                    Component component = components[index];
                    if (component is not (Transform or MeshFilter or Renderer or Light or HDAdditionalLightData)) {
                        DestroyImmediate(component);
                    } else if (component is HDAdditionalLightData l) {
                        var light = l.GetComponent<Light>();
                        DestroyImmediate(l);
                        DestroyImmediate(light);
                    }
                }
            }
            if (withRender) RenderPreview();
        }

        // === Preview Handling ===
        const int ImageSize = 256;
        const float CameraDistance = 2f;
        
        static readonly Vector3 CameraFocus = new Vector3(0, 1.6985f, -0.0418f);
        static readonly Rect ImageDimension = new(0, 0, ImageSize, ImageSize);
        static readonly Vector3 DefaultCameraPosition = new(0, 0, CameraDistance);

        PreviewRenderUtility _preview;
        Image _resultImage;
        bool _mouseIsDown;
        Vector3 _previousMousePosition;

        protected override void OnEnable() {
            base.OnEnable();
            // setup basic Preview Render Utility
            _preview ??= new PreviewRenderUtility();
            Camera cam = _preview.camera;
            cam.farClipPlane = 10;
            cam.nearClipPlane = 0.01f;
            cam.cameraType = CameraType.SceneView;
            cam.renderingPath = RenderingPath.DeferredShading;
            cam.useOcclusionCulling = true;
            
            var hdAdditionalCameraData = cam.gameObject.AddComponent<HDAdditionalCameraData>();
            hdAdditionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hdAdditionalCameraData.backgroundColorHDR = new(1, 1, 1, 0);
            
            hdAdditionalCameraData.volumeLayerMask = ~0;
            Transform transform = cam.transform;
            transform.position = DefaultCameraPosition + CameraFocus;
            transform.LookAt(CameraFocus);

            var postProcessing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3DAssets/Lighting/Prefabs/Lighting_CampaignMap_Day.prefab");
            var resultGO = _preview.InstantiatePrefabInScene(postProcessing);

            BasicUpdatePreview();
        }

        protected override void OnDisable() {
            if (_preview is not null) {
                _preview.Cleanup();
                _preview = null;
            }
            base.OnDisable();
        }

        void CreatePreview() {
            _resultImage = new Image();

            // Attach image to bottom of window
            VisualElement container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.bottom = 0;
            container.Add(_resultImage);
            rootVisualElement.Add(container);
            
            _resultImage.RegisterCallback<PointerDownEvent>(OnPointerDownHandler);
            _resultImage.RegisterCallback<PointerMoveEvent>(OnPointerMoveHandler);
            _resultImage.RegisterCallback<PointerUpEvent>(OnPointerUpHandler);

            // create and assign Render Texture to Image
            _preview.BeginPreview(ImageDimension, GUIStyle.none);
            _preview.camera.Render();
            _resultImage.image = _preview.EndPreview();
        }

        void OnPointerDownHandler(PointerDownEvent evt) {
            _mouseIsDown = true;
            _previousMousePosition = evt.position;
        }

        void OnPointerMoveHandler(PointerMoveEvent evt) {
            // If the mouse is not down do nothing
            if (!_mouseIsDown) {
                return;
            }

            Vector3 delta = evt.position - _previousMousePosition;
            _previousMousePosition = evt.position;

            // pivot camera around focus
            _preview.camera.transform.position = RotateAroundPivot(_preview.camera.transform.position, CameraFocus, -delta);
            _preview.camera.transform.LookAt(CameraFocus);

            RenderPreview();
        }

        void RenderPreview() {
            _preview.BeginPreview(ImageDimension, GUIStyle.none);
            _preview.camera.Render();
            _resultImage.image = _preview.EndPreview();

            Repaint();
        }

        void OnPointerUpHandler(PointerUpEvent evt) {
            _mouseIsDown = false;
        }
        
        static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
            return Quaternion.Euler(angles.y, -angles.x, 0) * (point - pivot) + pivot;
        }
    }
}