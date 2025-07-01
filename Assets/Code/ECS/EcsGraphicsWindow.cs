using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.ECS {
    public class EcsGraphicsWindow : UGUIWindowDisplay<EcsGraphicsWindow> {
        const uint MaxIdsCount = 1048576; // BRG internal limit
        const int MainPageSize = 24;
        const int BrgArraysPageSize = MainPageSize/2;
        const int PerTickRegistrations = 500;

        Tab _tab;
        int _brgArrayIndex = -1;

        BatchRendererGroup _cachedBatchRendererGroup;
        UnsafeBitmask _registeredMeshes;
        UnsafeBitmask _registeredMaterials;
        uint _registeredIndex = MaxIdsCount-1;

        int _mainPage;
        int _arrayMeshesPage;
        int _arrayMaterialsPage;

        protected override bool WithSearch => false;

        protected override void Initialize() {
            _registeredMeshes = new(MaxIdsCount, Allocator.Persistent);
            _registeredMaterials = new(MaxIdsCount, Allocator.Persistent);
        }

        protected override void Shutdown() {
            _registeredMeshes.Dispose();
            _registeredMaterials.Dispose();
        }

        protected override void DrawWindow() {
            var world = World.DefaultGameObjectInjectionWorld;
            var entitiesGraphicsSystem = world.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            _cachedBatchRendererGroup = entitiesGraphicsSystem.BatchRendererGroup;

            TGGUILayout.BeginHorizontal();
            if (GUILayout.Button("Meshes")) {
                _tab = Tab.Meshes;
                _registeredIndex = MaxIdsCount-1;
                _mainPage = 0;
            }
            if (GUILayout.Button("Materials")) {
                _tab = Tab.Materials;
                _registeredIndex = MaxIdsCount-1;
                _mainPage = 0;
            }
            if (GUILayout.Button("BRG Arrays")) {
                _tab = Tab.BRGArrays;
                _registeredIndex = MaxIdsCount-1;
                _mainPage = 0;
                _brgArrayIndex = -1;
            }
            TGGUILayout.EndHorizontal();
            GUILayout.Label($"Showing: {_tab}");
            GUILayout.Space(4);

            UpdateRegistered(_cachedBatchRendererGroup);

            if (_tab == Tab.Meshes) {
                _registeredMeshes.ToIndicesOfOneArray(ARAlloc.Temp, out var usedMeshes);
                TGGUILayout.PagedList(usedMeshes, DrawMeshes, ref _mainPage, MainPageSize);
                usedMeshes.Dispose();
            } else if (_tab == Tab.Materials) {
                _registeredMaterials.ToIndicesOfOneArray(ARAlloc.Temp, out var usedMaterials);
                TGGUILayout.PagedList(usedMaterials, DrawMaterials, ref _mainPage, MainPageSize);
                usedMaterials.Dispose();
            } else if (_tab == Tab.BRGArrays) {
                var registerSystem = world.GetExistingSystemManaged<RegisterMaterialsAndMeshesSystem>();
                var meshArrays = registerSystem.BRGRenderMeshArrays.GetValueArray(ARAlloc.Temp);
                _brgArrayIndex = math.clamp(_brgArrayIndex, -1, meshArrays.Length-1);
                if (_brgArrayIndex == -1) {
                    TGGUILayout.PagedList(meshArrays, DrawMeshArrays, ref _mainPage, MainPageSize);
                } else {
                    var cachedIndex = _brgArrayIndex;
                    if (GUILayout.Button("< Back")) {
                        _brgArrayIndex = -1;
                    }
                    var array = meshArrays[cachedIndex];
                    GUILayout.Label($"Unique Meshes: {array.UniqueMeshes.Length}");
                    TGGUILayout.PagedList(array.UniqueMeshes, DrawMeshes, ref _arrayMeshesPage, BrgArraysPageSize);

                    GUILayout.Space(8);
                    GUILayout.Label($"Unique Materials: {array.UniqueMaterials.Length}");
                    TGGUILayout.PagedList(array.UniqueMaterials, DrawMaterials, ref _arrayMaterialsPage, BrgArraysPageSize);
                }
                meshArrays.Dispose();
            }
        }

        void DrawMaterials(uint _, uint index) {
            var batchMaterialID = new BatchMaterialID() { value = index };
            DrawMaterials(batchMaterialID);
        }

        void DrawMaterials(int _, BatchMaterialID materialID) {
            DrawMaterials(materialID);
        }

        void DrawMaterials(BatchMaterialID materialID) {
            var material = _cachedBatchRendererGroup.GetRegisteredMaterial(materialID);
            GUILayout.Label($"{materialID.value}. Material: {material}");
        }

        void DrawMeshes(uint _, uint index) {
            var batchMeshID = new BatchMeshID() { value = index };
            DrawMeshes(batchMeshID);
        }

        void DrawMeshes(int _, BatchMeshID meshID) {
            DrawMeshes(meshID);
        }

        void DrawMeshes(BatchMeshID batchMeshID) {
            var mesh = _cachedBatchRendererGroup.GetRegisteredMesh(batchMeshID);
            GUILayout.Label($"{batchMeshID.value}. Mesh: {mesh}");
        }

        void DrawMeshArrays(int index, BRGRenderMeshArray array) {
            if (GUILayout.Button($"{index}. {array.UniqueMeshes.Length} meshes, {array.UniqueMaterials.Length} materials")) {
                _brgArrayIndex = index;
                _mainPage = 0;
                _arrayMeshesPage = 0;
                _arrayMaterialsPage = 0;
            }
        }

        void UpdateRegistered(BatchRendererGroup brg) {
            if (_tab == Tab.Meshes) {
                for (int i = 0; i < PerTickRegistrations; i++) {
                    _registeredIndex = (_registeredIndex+1) % MaxIdsCount;

                    var batchMeshID = new BatchMeshID() { value = _registeredIndex };
                    var mesh = brg.GetRegisteredMesh(batchMeshID);
                    if (mesh) {
                        _registeredMeshes.Up(_registeredIndex);
                    } else {
                        _registeredMeshes.Down(_registeredIndex);
                    }
                }
            } else if (_tab == Tab.Materials) {
                for (int i = 0; i < PerTickRegistrations; i++) {
                    _registeredIndex = (_registeredIndex+1) % MaxIdsCount;

                    var batchMaterialID = new BatchMaterialID() { value = _registeredIndex };
                    var material = brg.GetRegisteredMaterial(batchMaterialID);
                    if (material) {
                        _registeredMaterials.Up(_registeredIndex);
                    } else {
                        _registeredMaterials.Down(_registeredIndex);
                    }
                }
            }
        }

        enum Tab : byte {
            Meshes,
            Materials,
            BRGArrays,
        }

        [StaticMarvinButton(state: nameof(IsDebugWindowShown))]
        static void ShowEcsGraphicsWindow() {
            EcsGraphicsWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsDebugWindowShown() => EcsGraphicsWindow.IsShown;
    }
}
