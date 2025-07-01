using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Awaken.TG.Assets {
    /// <summary>
    /// Load sprites from various assets and use them as sprites list or sprites atlas
    /// Order of sprites depends on loading timing
    /// </summary>
    [Serializable]
    public class LazySprites : IReleasableReference {

        // === Properties
        /// <summary>
        /// Count of loaded sprites 
        /// </summary>
        [UnityEngine.Scripting.Preserve] public int Length => _sprites.Count;
        
        // === Editor references
        [ARAssetReferenceSettings(new []{typeof(Sprite), typeof(Texture2D)}, true)]
        public List<ARAssetReference> spritesReferences;

        // === Internal references and state
        List<Sprite> _sprites = new List<Sprite>();
        List<ARAsyncOperationHandle<Sprite[]>> _spritesRequestHandle = new List<ARAsyncOperationHandle<Sprite[]>>();
        event Action _notifyEvent;
        // Do we started loading already? 
        bool _started = false;

        // === Accessors
        /// <summary>
        /// Access to sprites as sprites array
        /// This do NOT do range check
        /// </summary>
        /// <param name="index">Index of required sprite</param>
        [UnityEngine.Scripting.Preserve] public Sprite this[int index] => _sprites[index];

        /// <summary>
        /// Search sprites with FirstOrDefault witch compare required name as Equals
        /// </summary>
        /// <param name="name">Sprite name</param>
        /// <returns>If founded sprite with name, otherwise null</returns>
        public Sprite TryGet(string name) => _sprites.FirstOrDefault(s => s.name.Equals(name));
        
        // === Operations
        /// <summary>
        /// Start loading sprites.
        /// Loading operation is async operation, but StartLoad is non blocking call
        /// </summary>
        public void StartLoad() {
            // Start loading only if we not loaded already
            if (!_started) {
                spritesReferences.ForEach( spritesheet => spritesheet.LoadAsset<Sprite[]>().OnComplete(OnSpritesLoaded) );
            }
            _started = true;
        }

        /// <summary>
        /// Release all sprites and clear internal state
        /// </summary>
        public void Release() {
            _sprites.Clear();
            _spritesRequestHandle.ForEach( handle => {
                if (handle.IsValid()) {
                    handle.Release();
                }else if (!handle.IsDone) {
                    handle.OnComplete(operationHandle => {
                        if (operationHandle.IsValid()) {
                            Addressables.Release(operationHandle);
                        }
                    });
                }
            } );
            _notifyEvent = null;
            _spritesRequestHandle.Clear();
            
            _started = false;
        }

        /// <summary>
        /// Register callback to get notify when all sprites get loaded
        /// </summary>
        public void NotifyOnLoaded(Action notifyAction) {
            // If already loaded all just call
            if (_spritesRequestHandle.Count == spritesReferences.Count) {
                notifyAction?.Invoke();
            } else {
                _notifyEvent -= notifyAction;
                _notifyEvent += notifyAction;
            }
        }

        void OnSpritesLoaded(ARAsyncOperationHandle<Sprite[]> handle) {
            // Store handle to release
            _spritesRequestHandle.Add(handle);
            // Add to sprite list if successfully loaded
            if (handle.Status == AsyncOperationStatus.Succeeded) {
                _sprites.AddRange(handle.Result);
            }

            // Trigger callback if we loaded all sprites
            if (_spritesRequestHandle.Count == spritesReferences.Count) {
                _notifyEvent?.Invoke();
                _notifyEvent = null;
            }
        }
    }
}