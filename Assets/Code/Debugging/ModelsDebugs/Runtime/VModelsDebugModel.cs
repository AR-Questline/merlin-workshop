using System;
using System.Linq;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Runtime {
    [UsesPrefab("Debug/VModelsDebugModel")]
    public class VModelsDebugModel : View<ModelsDebugModel> {
        ModelsDebug _modelsDebug;
        UGUIWindow _window;
        string _searchModelId;

        void Awake() {
            var position = new Rect(10, 10, Screen.width / 1.5f, Screen.height / 1.5f);
            _window = new UGUIWindow(position, "Models debug", DrawWindow, () => Target.Discard(), DrawToolbox);
            _modelsDebug = new ModelsDebug();
            _modelsDebug.Init(true);
            _searchModelId = World.Any<MarvinMode>()?.SearchModelId;
            TryToFillSearchBar().Forget();
        }

        void OnGUI() {
            _window.OnGUI();
        }

        void DrawWindow() {
            try {
                _modelsDebug.RefreshNavigation();
                _modelsDebug.Draw();
            } catch (Exception e) {
                GUILayout.Label($"Encounter error: \n {e}");
                if (GUILayout.Button("Refresh")) {
                    _modelsDebug = new ModelsDebug();
                    _modelsDebug.Init(true);
                }
            }
        }

        async UniTaskVoid TryToFillSearchBar() {
            while(_modelsDebug != null) {
                await UniTask.Yield();
                if (!string.IsNullOrEmpty(_searchModelId)) {
                    await UniTask.NextFrame();
                    _modelsDebug.SetSelectedId(_searchModelId);
                    _searchModelId = string.Empty;
                    return;
                }
            }
        }

        void DrawToolbox() {
            if (GUILayout.Button("Nearest location", GUILayout.Width(120))) {
                var heroPosition = Hero.Current.Coords;
                using var locations = World.All<Location>().GetManagedEnumerator();
                var nearestLocation = locations
                    .OrderBy(l => (l.Coords - heroPosition).sqrMagnitude)
                    .FirstOrDefault();
                if (nearestLocation != null) {
                    _modelsDebug.SetSelectedId(nearestLocation.ID);
                }
            }
        }

        void OnDestroy() {
            _modelsDebug = null;
        }
    }
}