using System;
using System.Collections.Generic;
using Awaken.TG.Editor.ToolbarTools;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.WorkflowTools {
    public partial class AssetPicker {
        [Serializable]
        struct FilteredAsset {
            [InlineButton(nameof(Jump)), LabelText("@index"), LabelWidth(40), InlineProperty]
            public GameObject asset;

            [HideInInspector] public int index;

            void Jump() {
                s_jumpToIndex = index;
            }
        }

        [Serializable]
        class AssetPickerSelection {
            [ShowInInspector, ReadOnly, HorizontalGroup("Selected Asset")]
            GameObject _selectedAsset;

            [ShowInInspector, ReadOnly, HorizontalGroup("Selected Asset", width: 80), LabelText("  Index"), LabelWidth(40), DisplayAsString]
            int _index = -1;

            [ReadOnly, SerializeField, DontValidate]
            GameObject instantiatedAsset;

            List<FilteredAsset> _assets;

            public GameObject SelectedAsset => _selectedAsset;

            public int Index {
                get => _index;
                set {
                    if (value == _index && instantiatedAsset != null) return;
                    if (_assets.Count == 0) {
                        _index = -1;
                        return;
                    }
                    
                    _index = value % _assets.Count;

                    if (_index < 0) {
                        _index += _assets.Count;
                    }

                    var prevInstance = instantiatedAsset;
                    if (_index >= 0 && _index < _assets.Count) {
                        _selectedAsset = _assets[_index].asset;
                        SpawnAssetAtIndex(_index, false);
                    } else {
                        _selectedAsset = null;
                    }

                    if (prevInstance != null) {
                        DestroyImmediate(prevInstance);
                    }
                }
            }

            public void AssignAssets(List<FilteredAsset> assets) {
                _assets = assets;
            }

            public void AssetsChanged() {
                _index = -1;
            }

            /// <summary>
            /// Spawning should be handled by changing selection index or explicit via button
            /// </summary>
            [Button, ShowIf("@instantiatedAsset == null"), PropertyOrder(100)]
            public void SpawnAsset() {
                // Handle new editor case
                if (_index < 0) {
                    if (_assets.Count == 0) return;
                    _index = 0;
                    if (_selectedAsset == null) {
                        _selectedAsset = _assets[_index].asset;
                    }
                }

                SpawnAssetAtIndex(Index);
            }

            void SpawnAssetAtIndex(int index, bool keepScale = true) {
                if (index < 0 || index >= _assets.Count) return;
                if (_assets[index].asset == null) return;

                Vector3 spawnPosition;
                Quaternion spawnRotation = Quaternion.identity;
                Vector3 scale = Vector3.zero;
                Transform spawnParent = null;

                
                if (instantiatedAsset != null) {
                    Transform prevInstanceTransform = instantiatedAsset.transform;
                    prevInstanceTransform.GetPositionAndRotation(out spawnPosition, out spawnRotation);
                    scale = prevInstanceTransform.localScale;
                    spawnParent = prevInstanceTransform.parent;
                } else {
                    Transform cameraTransform = SceneView.lastActiveSceneView.camera.transform;
                    spawnPosition = cameraTransform.position
                                    + cameraTransform.forward * 5;
                }

                instantiatedAsset = (GameObject) PrefabUtility.InstantiatePrefab(_selectedAsset, SceneManager.GetActiveScene());
                Transform newAssetTransform = instantiatedAsset.transform;
                SnapToGroundToolbar.AssignObjectAlwaysSnapping(newAssetTransform);

                if (spawnParent != null) {
                    newAssetTransform.SetParent(spawnParent);
                }

                newAssetTransform.SetPositionAndRotation(spawnPosition, spawnRotation);
                if (keepScale && scale != Vector3.zero) {
                    newAssetTransform.localScale = scale;
                }
                Selection.activeGameObject = instantiatedAsset;
            }

            public static AssetPickerSelection operator ++(AssetPickerSelection selection) {
                selection.Index++;
                return selection;
            }

            public static AssetPickerSelection operator --(AssetPickerSelection selection) {
                selection.Index--;
                return selection;
            }
        }
    }
}