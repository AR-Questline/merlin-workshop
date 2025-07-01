using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Graphics.LeshyRenderer {
    public class LeshyBakedGitPopup : EditorWindow {
        List<bool> _todoStates = new List<bool>();

        void CreateGUI() {
            var sceneName = SceneManager.GetActiveScene().name;
            _todoStates.Clear();

            var label = new HelpBox("<b><size=300%>Now commit all the Leshy files listed below!</size></b>", HelpBoxMessageType.Warning);
            rootVisualElement.Add(label);

            AddToggle("Matrices at ", $"StreamingAssets/Leshy/{sceneName}/Matrices.bin", true);
            AddToggle("CellsCatalog at ", $"StreamingAssets/Leshy/{sceneName}/CellsCatalog.leshy", true);
            AddToggle("Colliders at ", $"Data/Leshy/{sceneName}/*_Collider.prefab", true);
            AddToggle("Prefabs data at ", $"Data/Leshy/{sceneName}Prefabs.asset", true);
            AddToggle("Textures at ", $"StreamingAssets/DepthTextures/{sceneName}/depth_tex_*", true);
            AddToggle("Yours own changes", "", false);

            var button = new Button(this.Close);
            button.text = "Close";
            button.SetEnabled(false);
            rootVisualElement.Add(button);
        }

        void AddToggle(string label, string path, bool defaultState) {
            var toggle = new Toggle(label + path);

            toggle.style.flexDirection = FlexDirection.RowReverse;
            var labelElement = toggle.Q<Label>();
            labelElement.style.flexGrow = 1;
            labelElement.style.paddingLeft = 5;
            var toggleVisual = toggle.Q<VisualElement>(className: "unity-toggle__input");
            toggleVisual.style.flexGrow = 0;

            toggle.RegisterValueChangedCallback(CreateToggleCallback(path));
            rootVisualElement.Add(toggle);
            try {
                toggle.value = defaultState;
            } catch {
                // ignored
            }
        }

        EventCallback<ChangeEvent<bool>> CreateToggleCallback(string path) {
            var index = _todoStates.Count;
            _todoStates.Add(false);
            return evt => {
                _todoStates[index] = evt.newValue;
                UpdateStateChanged();
                if (_todoStates[index] && !string.IsNullOrEmpty(path)) {
                    GitUtils.GetOutput($"add {path}", "");
                }
            };
        }

        void UpdateStateChanged() {
            rootVisualElement.Q<Button>().SetEnabled(_todoStates.IsEmpty() || _todoStates.All(static v => v));
        }
    }
}
