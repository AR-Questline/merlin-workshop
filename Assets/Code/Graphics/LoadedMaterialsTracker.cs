using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.CommonInterfaces;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Graphics {
    public class LoadedMaterialsTracker {
        Dictionary<string, int> _materialKeyToLoadRequestsCountMap = new();
        Dictionary<string, Material> _materialKeyToLoadedMaterialMap = new();
        public static LoadedMaterialsTracker Instance { get; private set; }
        public Dictionary<string, Material> MaterialKeyToLoadedMaterialMap => _materialKeyToLoadedMaterialMap;
        
        public event Action<string, Material> OnNewMaterialLoaded;
        public event Action<string> OnMaterialUnloaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureCreated() {
            Instance ??= new LoadedMaterialsTracker();
        }
        
        LoadedMaterialsTracker() {
            foreach (var provider in AddressableMaterialsLoadingEventsProviders.Providers) {
                Subscribe(provider);
            }
            AddressableMaterialsLoadingEventsProviders.OnNewProviderAdded += Subscribe;
            AddressableMaterialsLoadingEventsProviders.OnProviderRemoved += Unsubscribe;
        }

        void Subscribe(IAddressableMaterialsLoadingEventsProvider loadingEventsProvider) {
            loadingEventsProvider.OnStartedLoadingMaterial += OnProviderMaterialStartedLoading;
            loadingEventsProvider.OnUnloadingMaterial += OnProviderUnloadingMaterial;
            loadingEventsProvider.OnLoadedMaterial += OnProviderLoadedMaterial;
        }
        
        void Unsubscribe(IAddressableMaterialsLoadingEventsProvider loadingEventsProvider) {
            if (loadingEventsProvider == null) {
                return;
            }
            loadingEventsProvider.OnStartedLoadingMaterial -= OnProviderMaterialStartedLoading;
            loadingEventsProvider.OnUnloadingMaterial -= OnProviderUnloadingMaterial;
            loadingEventsProvider.OnLoadedMaterial -= OnProviderLoadedMaterial;
        }
        
        public void OnProviderMaterialStartedLoading(string runtimeKey) {
            var loadRequestsCount = _materialKeyToLoadRequestsCountMap.GetValueOrDefault(runtimeKey, 0);
            loadRequestsCount++;
            _materialKeyToLoadRequestsCountMap[runtimeKey] = loadRequestsCount;
        }

        void OnProviderLoadedMaterial(string runtimeKey, Material material) {
            if (_materialKeyToLoadedMaterialMap.TryAdd(runtimeKey, material)) {
                CallNewMaterialLoaded(in runtimeKey, in material);
            }
        }

        void OnProviderUnloadingMaterial(string runtimeKey) {
            if (_materialKeyToLoadRequestsCountMap.TryGetValue(runtimeKey, out var loadRequestsCount) == false) {
                Log.Important?.Error($"Called unloaded event more times than loaded event on material {runtimeKey}");
                return;
            }
            loadRequestsCount--;
            if (loadRequestsCount == 0) {
                _materialKeyToLoadRequestsCountMap.Remove(runtimeKey);
                _materialKeyToLoadedMaterialMap.Remove(runtimeKey);
                CallMaterialUnloaded(runtimeKey);
                return;
            }
            _materialKeyToLoadRequestsCountMap[runtimeKey] = loadRequestsCount;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CallNewMaterialLoaded(in string runtimeKey, in Material material) {
            try {
                OnNewMaterialLoaded?.Invoke(runtimeKey, material);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CallMaterialUnloaded(in string runtimeKey) {
            try {
                OnMaterialUnloaded?.Invoke(runtimeKey);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}