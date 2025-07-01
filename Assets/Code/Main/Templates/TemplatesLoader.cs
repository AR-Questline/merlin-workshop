using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Stories.Core;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Templates {
    public class TemplatesLoader {
        const int TemplatesPreallocate = 11000;

        public static bool LoadFromAddressables { get; private set; } = false;

        public readonly Dictionary<string, ITemplate> guidMap = new(TemplatesPreallocate);
        public readonly MultiMap<Type, ITemplate> typeMap = new(40);

        public bool FinishedLoading { get; private set; }

        public static TemplatesLoader CreateAndLoad() {
            var result = new TemplatesLoader();
            result.LoadAssets();
            return result;
        }

        TemplatesLoader() { }

        void LoadAssets() {
#if UNITY_EDITOR
            if (LoadFromAddressables) {
                LoadAssetsInBuild().Forget();
            } else {
                LoadAssetsInEditor();
            }
#else
            LoadAssetsInBuild().Forget();
#endif
        }

        [UnityEngine.Scripting.Preserve]
        async UniTaskVoid LoadAssetsInBuild() {
            await LoadAssetsWithLabel(LoadType.GameObjects);
            await LoadAssetsWithLabel(LoadType.ScriptableObjects);

            FinishedLoading = true;

            async UniTask LoadAssetsWithLabel(LoadType loadType) {
                const int BufferSize = 50;

                var label = loadType == LoadType.GameObjects ? TemplatesUtil.AddressableLabel : TemplatesUtil.AddressableLabelSO;
                var locationsHandle = Addressables.LoadResourceLocationsAsync(label);
                var arLocationsHandle = new ARAsyncOperationHandle<IList<IResourceLocation>>(locationsHandle);
                IList<IResourceLocation> locations = await arLocationsHandle;

                var distinctGuids = GetGuidsFromLocations(locations);
                var bufferedHandles = new UnsafePinnableList<(string guid, AsyncOperationHandle<Object> handle)>(BufferSize);

                foreach (var guid in distinctGuids) {
                    await WaitForAvailableHandles(bufferedHandles, loadType, BufferSize - 1);
                    var handle = Addressables.LoadAssetAsync<Object>(guid);
                    bufferedHandles.Add((guid, handle));
                }

                await WaitForAvailableHandles(bufferedHandles, loadType, 0);
            }
        }

        async UniTask WaitForAvailableHandles(UnsafePinnableList<(string guid, AsyncOperationHandle<Object> handle)> bufferedHandles, LoadType loadType,
            int limit) {
            while (bufferedHandles.Count > limit) {
                await UniTask.NextFrame();

                for (int i = bufferedHandles.Count - 1; i >= 0; i--) {
                    var pair = bufferedHandles[i];
                    if (pair.handle.IsDone) {
                        bufferedHandles.SwapBackRemove(i);

                        var result = pair.handle.Result;
                        if (result == null) {
                            Log.Important?.Error($"Failed to load template with guid {pair.guid}");
                        } else {
                            if (loadType == LoadType.GameObjects) {
                                OnGameObjectLoaded(result, pair.guid);
                            } else {
                                OnScriptableObjectLoaded(result, pair.guid);
                            }
                        }
                    }
                }
            }
        }

        void OnGameObjectLoaded(Object obj, string guid) {
            if (obj is GameObject go) {
                var template = go.GetComponent<ITemplate>();
                if (template == null) {
                    Log.Important?.Warning($"Loaded template without ITemplate component: {obj} {guid}", go);
                }

                AddToMap(guid, template);
            } else {
                Log.Important?.Error($"Loaded template that is not game object: {obj.name} {guid}", obj);
            }
        }

        void OnScriptableObjectLoaded(Object obj, string guid) {
            if (obj is ITemplate template) {
                AddToMap(guid, template);
            } else {
                Log.Important?.Error($"Loaded template that is not scriptable object: {obj} {guid}", obj);
            }
        }

        void AddToMap(string guid, ITemplate template) {
            guidMap.Add(guid, template);
            typeMap.Add(template.GetType(), template);
            template.GUID = guid;
        }

        [UnityEngine.Scripting.Preserve]
        HashSet<string> GetGuidsFromLocations(IList<IResourceLocation> locations) {
            var result = new HashSet<string>();
            string lastKey = null;
            foreach (var location in locations) {
                if (location.PrimaryKey == lastKey) {
                    continue;
                }
                lastKey = location.PrimaryKey;
                if (TryGetGuidFromLocation(location, out string guid)) {
                    result.Add(guid);
                }
            }

            return result;
        }

        bool TryGetGuidFromLocation(IResourceLocation location, out string guid) {
            return TemplatesUtil.TryGetGUIDFromAddress(location.PrimaryKey, out guid);
        }

        enum LoadType : byte {
            GameObjects = 0,
            ScriptableObjects = 1,
        }

#if UNITY_EDITOR
        void LoadAssetsInEditor() {
            var templates = TemplatesProvider.EditorGetAllOfType<ITemplate>();

            foreach (var template in templates) {
                if (template is ScriptableObject so) {
                    OnScriptableObjectLoaded(so, template.GUID);
                } else if (template is Component c) {
                    OnGameObjectLoaded(c.gameObject, template.GUID);
                }
            }

            FinishedLoading = true;

            if (guidMap.Count > TemplatesPreallocate) {
                Log.Important?.Error(
                    $"Increase {nameof(TemplatesPreallocate)} in {nameof(TemplatesLoader)}!!! Current number of templates: {guidMap.Count}");
            }
        }
#endif
    }
}