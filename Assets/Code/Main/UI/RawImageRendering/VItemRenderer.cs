using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Rendering;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Cameras;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Awaken.TG.Main.UI.RawImageRendering {
    [UsesPrefab("UI/RawImageRendering/VItemRenderer")]
    public class VItemRenderer : View<ItemRenderer> {
        [SerializeField] Camera uiCamera;
        [SerializeField] RawImage rawImage;
        [SerializeField] Transform itemParent;
        [SerializeField] float renderTextureSizeModifier = 1;
        
        static readonly int DoubleSidedEnable = Shader.PropertyToID("_DoubleSidedEnable");
        ReferenceInstance<GameObject> _spawnedInstance;
        
        RenderTexture _renderTexture;
        RenderTextureDescriptor _lastDescriptor;

        const float AdditionalFov = 2f;
        const float MinimalFov = 20f;

        public override Transform DetermineHost() => null;
        
        void RotateConditional() {
            Item item = Target.ItemToRender;
            if (item.IsWeapon || item.IsShield) {
                itemParent.Rotate(Vector3.up * 270);
            } else if (item.IsArrow) {
                itemParent.Rotate(Vector3.right * 90);
            }
        }
        
        protected override void OnInitialize() {
            transform.position += Target.PositionOffset;
            SetupRenderTexture();
            SetupRawImage();

            ChangeRenderedItem();
            Target.ListenTo(Model.Events.AfterChanged, ChangeRenderedItem, this);
        }

        void SetupRawImage() {
            rawImage.transform.SetParent(Target.ViewParent, false);
            RectTransform rectTransform = rawImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
        
        void SetupRenderTexture() {
            var rectTransform = Target.ViewParent.GetComponent<RectTransform>();
            var pixelsRect = rectTransform.GetPixelsRect();

            var width = Mathf.RoundToInt(pixelsRect.width*renderTextureSizeModifier);
            var height = Mathf.RoundToInt(pixelsRect.height*renderTextureSizeModifier);

            // Correct pixels misalignment
            if (Mathf.Abs(width - height) < 3) {
                width = height = Mathf.Max(width, height);
            }

            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGBHalf, 0);
            desc.dimension = TextureDimension.Tex2D;
            desc.sRGB = false;
            desc.autoGenerateMips = false;
            desc.useMipMap = false;
            desc.mipCount = 0;

            if (!_renderTexture) {
                _renderTexture = new(desc);
            } else if (_renderTexture && !CompareDescriptors(_lastDescriptor, desc)) {
                _renderTexture.Release();
                _renderTexture = new(desc);
            }
            _lastDescriptor = desc;
            
            uiCamera.targetTexture = _renderTexture;
            rawImage.texture = _renderTexture;
        }

        void ChangeRenderedItem() {
            _spawnedInstance?.ReleaseInstance();
            _spawnedInstance = new ReferenceInstance<GameObject>(Target.ModelToRender);
            GameObjects.DestroyAllChildrenSafely(itemParent);
            itemParent.rotation = Quaternion.identity;

            RotateConditional();
            
            if (_spawnedInstance.Reference is {IsSet: true}) {
                AwaitChildrenDestroy().Forget();
            }
        }

        async UniTaskVoid AwaitChildrenDestroy() {
            await UniTask.WaitUntil(() => this == null || itemParent.childCount == 0);
            if (this != null) {
                _spawnedInstance.Instantiate(itemParent).OnCompleteForceAsync(OnItemPrefabLoaded);
            }
        }

        void OnItemPrefabLoaded(ARAsyncOperationHandle<GameObject> handle) {
            if (handle.Status == AsyncOperationStatus.Succeeded) {
                handle.Result.GetComponentsInChildren<Transform>().ForEach(UpdateObjectData);
                AdjustCameraFovToItemBounds();
            } else {
                _spawnedInstance?.ReleaseInstance();
                _spawnedInstance = null;
            }
        }
        
        void AdjustCameraFovToItemBounds() {
            if (uiCamera == null) {
                return;
            }
            
            var cameraPosition = uiCamera.transform.position;
            var cameraForward = uiCamera.transform.forward;
            var bounds = TransformBoundsUtil.FindBounds(itemParent, false);
            
            cameraPosition.y = bounds.center.y;
            uiCamera.transform.position = cameraPosition;
                
            var boundMinDir = bounds.min - cameraPosition;
            var boundMinDot = Vector3.Dot(boundMinDir.normalized, cameraForward);
                
            var boundMaxDir = bounds.max - cameraPosition;
            var boundMaxDot = Vector3.Dot(boundMaxDir.normalized, cameraForward);
                
            var minDot = Mathf.Min(boundMinDot, boundMaxDot);
            uiCamera.fieldOfView = Mathf.Max(MinimalFov, 2 * Mathf.Acos(minDot) * Mathf.Rad2Deg + AdditionalFov);
        }

        static void UpdateObjectData(Transform t) {
            GameObject go = t.gameObject;
            go.layer = RenderLayers.UI;
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null) {
                meshRenderer.renderingLayerMask = LightRenderLayers.UIMask;
                MaterialPropertyBlock propertyBlock = new();
                meshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(DoubleSidedEnable, 1);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        protected override IBackgroundTask OnDiscard() {
            if (rawImage != null && rawImage.gameObject != null) {
                Destroy(rawImage.gameObject);
            }
            _lastDescriptor = default;
            uiCamera.targetTexture = null;
            rawImage.texture = null;
            if (_renderTexture) {
                _renderTexture.Release();
                _renderTexture = null;
            }
            _spawnedInstance?.ReleaseInstance();
            return base.OnDiscard();
        }

        static bool CompareDescriptors(RenderTextureDescriptor left, RenderTextureDescriptor right) {
            if (left.autoGenerateMips != right.autoGenerateMips) {
                return false;
            }
            if (left.useMipMap != right.useMipMap) {
                return false;
            }
            if (left.width != right.width) {
                return false;
            }
            if (left.height != right.height) {
                return false;
            }
            if (left.msaaSamples != right.msaaSamples) {
                return false;
            }
            if (left.mipCount != right.mipCount) {
                return false;
            }
            if (left.graphicsFormat != right.graphicsFormat) {
                return false;
            }
            if (left.colorFormat != right.colorFormat) {
                return false;
            }
            if (left.depthStencilFormat != right.depthStencilFormat) {
                return false;
            }
            if (left.sRGB != right.sRGB) {
                return false;
            }
            return true;
        }
    }
}
