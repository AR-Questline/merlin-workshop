using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Awaken.TG.Main.Utility.UI {
    /// <summary>
    /// Set proper Sprite Asset for TMP_Text based on active controller. Doesn't support controller change during runtime.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class VCSpriteAssetController : ViewComponent {
        [SerializeField] TMP_Text textWithSprite;

        ARAssetReference _spriteAssetReference;

        protected override void OnAttach() {
            Refresh(RewiredHelper.ActiveController());
            World.EventSystem.ListenTo(EventSelector.AnySource, Focus.Events.ControllerChanged, this, Refresh);
        }

        void Refresh(ControllerType controllerType) {
            ShareableARAssetReference shareableARAssetReference =
                controllerType switch {
                    ControllerType.Keyboard or ControllerType.Mouse => 
                        CommonReferences.Get.KeyIconsSpriteAssetPCReference,
                    ControllerType.Joystick or ControllerType.Custom when PlatformUtils.IsPS5 || RewiredHelper.IsSony =>
                        CommonReferences.Get.KeyIconsSpriteAssetPSReference,
                    ControllerType.Joystick or ControllerType.Custom => 
                        CommonReferences.Get.KeyIconsSpriteAssetXboxReference,
                    _ => throw new ArgumentOutOfRangeException(nameof(controllerType), controllerType, null)
                };

            if (shareableARAssetReference is not {IsSet: true}) {
                Log.Important?.Error($"Sprite asset reference is not set in CommonReferences for ControllerType: {controllerType}!", this);
                return;
            }
            
            _spriteAssetReference?.ReleaseAsset();
            _spriteAssetReference = shareableARAssetReference.GetAndLoad<SpriteAsset>(OnCompleted);
        }

        void OnCompleted(ARAsyncOperationHandle<SpriteAsset> handle) {
            if (textWithSprite != null) {
                SpriteAsset spriteAsset = handle.Result;
                textWithSprite.spriteAsset = spriteAsset;
            } else {
                _spriteAssetReference?.ReleaseAsset();
            }
        }

        void Reset() {
            textWithSprite = GetComponent<TMP_Text>();
        }

        protected override void OnDiscard() {
            _spriteAssetReference?.ReleaseAsset();
            _spriteAssetReference = null;
        }
    }
}