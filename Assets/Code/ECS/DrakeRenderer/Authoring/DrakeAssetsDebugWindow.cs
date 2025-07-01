using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.UI;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeAssetsDebugWindow : UGUIWindowDisplay<DrakeAssetsDebugWindow> {
        protected override bool WithSearch => false;

        bool _showInvalid;
        bool _showLoading;
        bool _showNotSucceeded;
        bool _showLoaded;

        Tab _tab;
        int _mainPage;

        protected override void DrawWindow() {
            TGGUILayout.BeginHorizontal();
            if (GUILayout.Button("Meshes")) {
                _tab = Tab.Meshes;
                _mainPage = 0;
            }
            if (GUILayout.Button("Materials")) {
                _tab = Tab.Materials;
                _mainPage = 0;
            }
            GUILayout.Label($"Showing: {_tab}");
            TGGUILayout.EndHorizontal();

            _showInvalid = GUILayout.Toggle(_showInvalid, "Show Invalid");
            _showLoading = GUILayout.Toggle(_showLoading, "Show Loading");
            _showNotSucceeded = GUILayout.Toggle(_showNotSucceeded, "Show Not Succeeded");
            _showLoaded = GUILayout.Toggle(_showLoaded, "Show Loaded");

            GUILayout.Space(4);

            var drakeRendererLoadingManager = DrakeRendererManager.Instance.LoadingManager;


            if (_tab == Tab.Meshes) {
                Draw(drakeRendererLoadingManager.MeshLoadingData);
            } else {
                Draw(drakeRendererLoadingManager.MaterialLoadingData);
            }
        }

        void Draw<T>(List<DrakeRendererLoadingManager.AddressableLoadingData<T>> loadingData) where T : Object {
            var allCount = loadingData.Count;
            var referencedCount = 0;
            var loadedCount = 0;

            var mask = new UnsafeBitmask((uint)allCount, ARAlloc.Temp);

            for (var i = 0; i < loadingData.Count; i++) {
                if (loadingData[i].counter < 1) {
                    continue;
                }

                referencedCount++;

                var loadingHandle = loadingData[i].loadingHandle;
                if (!loadingHandle.IsValid()) {
                    if (_showInvalid) {
                        mask.Up((uint)i);
                    }
                } else if (!loadingHandle.IsDone) {
                    if (_showLoading) {
                        mask.Up((uint)i);
                    }
                } else if (loadingHandle.Status != AsyncOperationStatus.Succeeded) {
                    if (_showNotSucceeded) {
                        mask.Up((uint)i);
                    }
                } else {
                    loadedCount++;
                    if (_showLoaded) {
                        mask.Up((uint)i);
                    }
                }
            }

            GUILayout.Label($"{typeof(T).Name} - Loaded: {loadedCount} Ref:{referencedCount} All: {allCount}");
            mask.ToIndicesOfOneArray(ARAlloc.Temp, out var indices);
            TGGUILayout.PagedList(indices, DrawElement, ref _mainPage, 18);
            indices.Dispose();

            void DrawElement(uint _, uint drawIndex) {
                var key = loadingData[(int)drawIndex].key;
                var counter = loadingData[(int)drawIndex].counter;
                var loadingHandle = loadingData[(int)drawIndex].loadingHandle;
                if (!loadingHandle.IsValid()) {
                    GUILayout.Label($"{key}, count [{counter}] - Invalid handle");
                } else if (!loadingHandle.IsDone) {
                    GUILayout.Label($"{key}, count [{counter}] is loading {loadingHandle.PercentComplete:P}");
                } else if (loadingHandle.Status != AsyncOperationStatus.Succeeded) {
                    GUILayout.Label($"{key}, count [{counter}] loaded but {loadingHandle.Status}");
                } else {
                    GUILayout.Label($"{key}, count [{counter}] loaded {loadingHandle.Result}");
                }
            }
        }

        enum Tab : byte {
            Meshes,
            Materials
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowDrakeAssetsDebugWindow() {
            DrakeAssetsDebugWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsDebugWindowShown() => DrakeAssetsDebugWindow.IsShown;
    }
}