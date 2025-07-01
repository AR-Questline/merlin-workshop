using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using UnityEngine;

namespace Awaken.TG.Main.UI.Cursors {
    public class Cursor : MonoBehaviour, IService {
        public const string NoneCursor = "none";

        // === Unity References
        public List<Sprite> cursorSprites;
        public string defaultCursorName;
        bool _enabled = true;

        // === Fields
        static readonly Dictionary<string, CursorData> CursorsData = new();
        string _lastCursor = string.Empty;

        bool WasInitialized => CursorsData.Any();

        // Initialization

        public void Initialize() {
            foreach (Sprite cursorSprite in cursorSprites) {
                var texture = ExtractTexture(cursorSprite);
                var hotspot = ExtractHotspot(cursorSprite);

                CursorsData[cursorSprite.name] = new CursorData {
                    hotspot = hotspot,
                    texture = texture
                };
            }

            CursorsData[NoneCursor] = new CursorData {
                hotspot = Vector2.zero,
                texture = CreateInvisibleTexture()
            };

            SetCursor(defaultCursorName);
        }

        // === Operations
        public void ToggleEnabled(bool enable) {
            _enabled = enable;
        }

        public void ProcessEndFrame() {
            if (!_enabled) {
                return;
            }

            UIState uiState = UIStateStack.Instance.State;
            bool disableCursor = World.Services.TryGet<TitleScreen.TitleScreen>() == null && (uiState.IsMapInteractive || uiState.HideCursor);

            var hidden = RewiredHelper.IsGamepad || disableCursor;
            var cursorForce = World.LastOrNull<ForceCursorVisibility>();
            if (cursorForce) {
                hidden = !cursorForce.ShouldBeVisible;
            }
            SetCursor(hidden ? NoneCursor : defaultCursorName);
        }

        void OnApplicationFocus(bool hasFocus) {
            if (!WasInitialized) return;
            
            if (hasFocus) {
                ToggleEnabled(true);
            } else {
                ToggleEnabled(false);
                SetCursor(defaultCursorName);
            }
        }

        // === Helpers

        void SetCursor(string cursorName) {
            if (!CursorsData.TryGetValue(cursorName, out var cursorData)) {
                throw new ArgumentException($"There is no cursor named {cursorName}");
            }

            bool cursorVisible = UnityEngine.Cursor.visible;
            bool shouldUpdateVisibility = cursorName == NoneCursor && cursorVisible 
                                          || cursorName != NoneCursor && !cursorVisible;
            if (!_lastCursor.Equals(cursorName) || shouldUpdateVisibility) {
                _lastCursor = cursorName;
                if (cursorName.Equals(NoneCursor)) {
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    UnityEngine.Cursor.visible = false;
                    ForceClickMouseButtonInCenterOfGameWindow();
                } else {
                    UnityEngine.Cursor.SetCursor(cursorData.texture, cursorData.hotspot, CursorMode.Auto);
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                    UnityEngine.Cursor.visible = true;
                }
            }
        }

        Texture2D ExtractTexture(Sprite sprite) {
            if (sprite.texture.width == sprite.rect.width && sprite.texture.height == sprite.rect.height) {
                return sprite.texture;
            }

            var croppedTexture = new Texture2D((int) sprite.rect.width, (int) sprite.rect.height) {name = "Runtime_CursorTexture"};
            var pixels = sprite.texture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();

            return croppedTexture;
        }
        

        Vector2 ExtractHotspot(Sprite sprite) {
            return new Vector2( sprite.pivot.x, sprite.rect.height - sprite.pivot.y );
        }

        Texture2D CreateInvisibleTexture() {
            var texture = new Texture2D(25, 25, TextureFormat.RGBA32, false) {name = "Runtime_CursorInvisibleTexture"};
            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++) {
                pixels[i] = new Color(1, 1, 1, 0);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // === Helper struct
        public class CursorData {
            public Texture2D texture;
            public Vector2 hotspot;
        }

        public static void ForceClickMouseButtonInCenterOfGameWindow()
        {
#if UNITY_EDITOR
            var game = UnityEditor.EditorWindow.focusedWindow;
            if (game == null) return;
            Vector2 gameWindowCenter = game.rootVisualElement.contentRect.center;
            Event leftClickDown = new Event();
            leftClickDown.button = 0;
            leftClickDown.clickCount = 1;
            leftClickDown.type = EventType.MouseDown;
            leftClickDown.mousePosition = gameWindowCenter;
            game.SendEvent(leftClickDown);
#endif
        }
    }
}
