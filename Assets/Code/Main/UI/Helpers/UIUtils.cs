using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.GameObjects;
using UnityEngine;
using UnityEngine.UI;

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
    }
}