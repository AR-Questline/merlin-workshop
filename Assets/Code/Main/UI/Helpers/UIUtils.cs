using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.UI.Helpers {
    public static class UIUtils {
        static readonly List<GameObject> SDisabledUI = new();
        
        public static void ShowUI() {
            SDisabledUI.ForEach(go => {
                if (go) {
                    go.SetActive(true);
                }
            });
            SDisabledUI.Clear();
        }

        public static void HideUI() {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(static c => {
                    if (c.transform.parent) {
                        return c.transform.parent.GetComponentInParent<Canvas>() == null;
                    }
                    return true;
                })
                .ToArray();
            
            canvases.ForEach(c => {
                var go = c.gameObject;
                go.SetActive(false);
                SDisabledUI.Add(go);
            });
        }

        /// <summary>
        /// Returns sprite reference. You have to manually handle releasing it.
        /// </summary>
        public static SpriteReference TrySetupSpriteReference(this ShareableSpriteReference shareableSpriteReference, Image image) {
            if (image != null && shareableSpriteReference is { IsSet: true }) {
                SpriteReference spriteReference = shareableSpriteReference.Get();
                spriteReference.SetSprite(image);
                image.TrySetActiveOptimized(true);
                return spriteReference;
            }

            image.TrySetActiveOptimized(false);
            return null;
        } 
        
        public static string Key(KeyBindings binding, bool hold = false) {
            string keyDisplayName = World.Services.Get<UIKeyMapping>().GetDisplayNameOf(binding, hold, ControlSchemes.Current());
            if (string.IsNullOrEmpty(keyDisplayName)) {
                return string.Empty;
            }
            
            bool hasOpenBracket = false;
            bool hasCloseBracket = false;
            foreach (char c in keyDisplayName) {
                if (c == '[') {
                    hasOpenBracket = true;
                } else if (c == ']') {
                    hasCloseBracket = true;
                }
                
                if (hasOpenBracket && hasCloseBracket) {
                    return keyDisplayName;
                }
            }
            
            return keyDisplayName.ToSprite().PercentSizeText(150);
        }
        
        public static void AddOverlayUIView(Model model, View parentView, Action afterOverlayDiscardedCallback = null) {
            model.AfterFullyInitialized(() => SetActiveDelayed(parentView, false).Forget());
            model.ListenToLimited(Model.Events.BeforeDiscarded, () => parentView.TrySetActiveOptimized(true), parentView);
            model.ListenToLimited(Model.Events.AfterDiscarded, () => afterOverlayDiscardedCallback?.Invoke(), parentView);
        }

        static async UniTaskVoid SetActiveDelayed(View view, bool active) {
            if (await AsyncUtil.DelayFrame(view, 2)) {
                view.TrySetActiveOptimized(active);
            }
        }
        
        public static void LogInvalidUIAware(this IUIAware uiAware, UIEvent uiEvent) {
#if DEBUG || AR_DEBUG
            if (uiAware.IsValid) {
                return;
            }
            
            var name = uiAware switch {
                IModel model => $"model: {model.ContextID}",
                IView view => $"view: {view.ID}",
                Component component => $"component: {component.gameObject.PathInSceneHierarchy()}",
                _ => uiAware.GetType().Name
            };
                
            var eventType = uiEvent.GetType();
            var context = uiEvent switch {
                UIAction action => $"event name: {eventType} with action name: {action.Data.actionName}",
                UIMouseButtonEvent mouse => $"event name: {eventType} with mouse button: {mouse.Button.ToString()}",
                UIKeyEvent key => $"event name: {eventType} with key name: {key.Key.ToString()}",
                _ => $"event name: {eventType}",
            };
                
            Log.Important?.Error($"UI ignored handling of IUIAware [{name}] due to an invalid state (discarded or null). Event context: {context}"); 
#endif
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidForUIHandle(this Model model) => model != null && !model.HasBeenDiscarded;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidForUIHandle(this Element element) => element != null && !element.HasBeenDiscarded && element.GenericParentModel != null && !element.GenericParentModel.HasBeenDiscarded;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidForUIHandle(this View view) => view != null && !view.HasBeenDiscarded && view.GenericTarget != null && !view.GenericTarget.HasBeenDiscarded;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidForUIHandle(this ViewComponent viewComponent) => viewComponent != null && !viewComponent.HasBeenDiscarded;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidForUIHandle(this Component component) => !component.IsUnityNull();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidForUIHandle(this IPresenter presenter) => presenter.GenericModel != null && !presenter.GenericModel.HasBeenDiscarded;
        public static bool IsValidController(this IUIHandlerSource handlerSource, ControlSchemeFlag controlSchemeFlag) {
            return controlSchemeFlag == ControlSchemeFlag.None ||
                   (controlSchemeFlag == ControlSchemeFlag.Gamepad && RewiredHelper.IsGamepad) ||
                   (controlSchemeFlag == ControlSchemeFlag.KeyboardAndMouse && !RewiredHelper.IsGamepad);
        }
    }
}