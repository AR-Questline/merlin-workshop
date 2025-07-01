using Awaken.Utility;
using System;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace Awaken.TG.Assets {
    /// <summary>
    /// Wrapper to lazy load sprite
    /// Hides asynchronous operation via exposing synchronous API
    /// To share sprite <see cref="ShareableSpriteReference"/>
    /// </summary>
    [Serializable]
    public sealed partial class SpriteReference : IReleasableReference {
        public ushort TypeForSerialization => SavedTypes.SpriteReference;

        // === Editor references
        [Saved] public ARAssetReference arSpriteReference;

        static ImageSpriteLoader s_imageLoader = new();
        static SpriteRendererSpriteLoader s_spriteRendererLoader = new();
        static ImageElementSpriteLoader s_imageElementSpriteLoader = new();
        
        // === Operations
        public bool IsSet => arSpriteReference is { IsSet: true };

        /// <summary>
        /// Register sprite to auto release and set sprite to image
        /// This is async operation, image will be set after sprite get loaded
        /// </summary>
        /// <param name="owner">View which uses sprite</param>
        /// <param name="image">Sprite target</param>
        /// <param name="afterAssign">Optional callback after sprite loaded and assigned</param>
        public void RegisterAndSetup(IReleasableOwner owner, Image image, Action<Image, Sprite> afterAssign = null) {
            try {
                owner.RegisterReleasableHandle(this);
                SetSprite(image, afterAssign);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
        
       public void RegisterAndSetup(IReleasableOwner owner, VisualElement image, Action<VisualElement, Sprite> afterAssign = null) {
            owner.RegisterReleasableHandle(this);
            SetSprite(image, afterAssign);
       }
        
        /// <summary>
        /// Set Sprite in Image after sprite get loaded
        /// </summary>
        public void SetSprite(Image image, Action<Image, Sprite> afterAssign = null) {
            s_imageLoader.SetSprite(image, arSpriteReference, afterAssign);
        }

        /// <summary>
        /// Set Sprite in VisualElement background image after sprite get loaded
        /// </summary>
        public void SetSprite(VisualElement image, Action<VisualElement, Sprite> afterAssign = null) {
            s_imageElementSpriteLoader.SetSprite(component: image, asset: arSpriteReference, afterAssign: afterAssign);
        }

        /// <summary>
        /// Set Sprite in SpriteRenderer after sprite get loaded
        /// </summary>
        public void SetSprite(SpriteRenderer renderer, Action<SpriteRenderer, Sprite> afterAssign = null) {
            s_spriteRendererLoader.SetSprite(renderer, arSpriteReference, afterAssign);
        }

        /// <summary>
        /// Release sprite asset
        /// </summary>
        public void Release() {
            arSpriteReference.ReleaseAsset();
        }
        
        public void Preload() {
            arSpriteReference?.PreloadLight<Sprite>();
        }
        public void Preload(GameObject owner) {
            arSpriteReference?.Preload<Sprite>(() => owner != null);
        }
        public void Preload(Func<bool> shouldExtendTimeout) {
            arSpriteReference?.Preload<Sprite>(shouldExtendTimeout);
        }

        public static SpriteReference Create(string GUID) {
            return new SpriteReference {
                arSpriteReference = new ARAssetReference(GUID)
            };
        }
        public SpriteReference DeepCopy() {
            return new() {
                arSpriteReference = arSpriteReference.DeepCopy(),
            };
        }
        
        abstract class SpriteLoader<TComponent> where TComponent : Component {
            public void SetSprite(TComponent component, ARAssetReference asset, Action<TComponent, Sprite> afterAssign = null) {
                IAssetLoadingGate gate = GetLoadingGate(component);

                var handle = asset.LoadAsset<Sprite>();
                // loadOperation.OnComplete do the same but allocates because delegate captures\
                // normally it's fine, but here we do optimization as it's hot-path
                if (handle.IsDone) {
                    OnSpriteLoaded(handle, component, asset, gate, afterAssign);
                } else {
                    handle.OnComplete(_ => OnSpriteLoaded(handle, component, asset, gate, afterAssign), _ => gate?.Unlock());
                }
            }
            
            void OnSpriteLoaded(ARAsyncOperationHandle<Sprite> handle, TComponent component, ARAssetReference asset, IAssetLoadingGate gate, Action<TComponent, Sprite> afterAssign) {
                if (component != null) {
                    AssignSprite(component, handle.Result);
                    afterAssign?.Invoke(component, handle.Result);
                } else {
                    asset.ReleaseAsset();
                }
                gate?.Unlock();
            }
            
            IAssetLoadingGate GetLoadingGate(TComponent component) {
                var go = component.gameObject;

                var parentView = go.GetComponentInParent<View>();
                if (parentView == null) return null;
                
                var loadingGate = go.GetComponentInParent<IAssetLoadingGate>(true);

                // If there is no IAssetLoadingGate between Image and View create default one
                if (parentView != loadingGate?.OwnerView) {
                    loadingGate = CreateDefaultGate(parentView, component);
                }
                
                var locked = loadingGate?.TryLock() ?? false;
                if (!locked) {
                    loadingGate = null;
                }

                return loadingGate;
            }

            protected abstract void AssignSprite(TComponent component, Sprite sprite);
            protected abstract IAssetLoadingGate CreateDefaultGate(View view, TComponent component);
        }

        class ImageSpriteLoader : SpriteLoader<Image> {
            protected override void AssignSprite(Image component, Sprite sprite) {
                component.sprite = sprite;
            }

            protected override IAssetLoadingGate CreateDefaultGate(View view, Image component) {
                var viewCanvasGroup = view.gameObject.AddComponent<CanvasGroup>();
                viewCanvasGroup.alpha = 1;
                var loadingGate = view.gameObject.AddComponent<AssetLoadingGate>();
                loadingGate.gate = viewCanvasGroup;
                return loadingGate;
            }
        }

        class SpriteRendererSpriteLoader : SpriteLoader<SpriteRenderer> {
            protected override void AssignSprite(SpriteRenderer component, Sprite sprite) {
                component.sprite = sprite;
            }

            protected override IAssetLoadingGate CreateDefaultGate(View view, SpriteRenderer component) {
                return new SpriteRendererLoadingGate(view, component);
            }

            class SpriteRendererLoadingGate : IAssetLoadingGate {
                public View OwnerView { get; }
                GameObject _gate;

                public SpriteRendererLoadingGate(View view, SpriteRenderer renderer) {
                    OwnerView = view;
                    _gate = renderer.gameObject;
                }
                
                public bool TryLock() {
                    _gate.SetActive(false);
                    return true;
                }

                public void Unlock() {
                    if (_gate) {
                        _gate.SetActive(true);
                    }
                }
            }
        }
        
        class ImageElementSpriteLoader {
            public void SetSprite(VisualElement component, ARAssetReference asset, Action<VisualElement, Sprite> afterAssign = null) {
                var handle = asset.LoadAsset<Sprite>();

                if (handle.IsDone) {
                    OnSpriteLoaded(handle, component, asset, afterAssign);
                } else {
                    handle.OnComplete(_ => OnSpriteLoaded(handle, component, asset, afterAssign));
                }
            }
            
            void OnSpriteLoaded(ARAsyncOperationHandle<Sprite> handle, VisualElement component, ARAssetReference asset, Action<VisualElement, Sprite> afterAssign) {
                if (component != null) {
                    AssignSprite(component, handle.Result);
                    afterAssign?.Invoke(component, handle.Result);
                } else {
                    asset.ReleaseAsset();
                }
            }
            
            void AssignSprite(VisualElement component, Sprite sprite) {
                component.SetBackgroundImage(sprite);
            }
        }
    }
}