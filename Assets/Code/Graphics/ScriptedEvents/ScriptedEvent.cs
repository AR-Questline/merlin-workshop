using Awaken.TG.Assets;
using Awaken.TG.Graphics.ScriptedEvents.Animators;
using Awaken.TG.Graphics.ScriptedEvents.Triggers;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Graphics.ScriptedEvents {
    [RequireComponent(typeof(IHeroTrigger))]
    public class ScriptedEvent : MonoBehaviour {
        [SerializeField, PrefabAssetReference(AddressableGroup.Locations)] ARAssetReference asset;
        [SerializeField] FlagLogic availability;

        [ShowInInspector, ReadOnly] GameObject _instance;
        [ShowInInspector, ReadOnly] int _mainAssetRefCount;
        [ShowInInspector, ReadOnly] int _prolongedAssetRefCount;
        
        IEventListener _initListener;
        IEventListener _flagListener;

        [ShowInInspector, ReadOnly] bool _initialized;
        [ShowInInspector, ReadOnly] bool _available;
        [ShowInInspector, ReadOnly] bool _spawned;
        
        void Awake() {
            if (PlatformUtils.IsPlaying) {
                var heroTrigger = GetComponent<IHeroTrigger>();
                heroTrigger.OnHeroEnter += IncreaseMainAssetRefCount;
                heroTrigger.OnHeroExit += DecreaseMainAssetRefCount;
            }
        }

        void Start() {
            if (PlatformUtils.IsPlaying) {
                if (SceneLifetimeEvents.Get.EverythingInitialized) {
                    Init();
                } else {
                    _initListener = World.EventSystem.LimitedListenTo(
                        EventSelector.AnySource, SceneLifetimeEvents.Events.AfterSceneStoriesExecuted,
                        _ => Init(), 1
                    );
                }
            }
        }

        void OnDestroy() {
            if (PlatformUtils.IsPlaying) {
                World.EventSystem.TryDisposeListener(ref _initListener);
                World.EventSystem.TryDisposeListener(ref _flagListener);
                _initialized = false;
                RefreshSpawnedState();
            }
        }

        public void IncreaseMainAssetRefCount() {
            _mainAssetRefCount++;
            RefreshSpawnedState();
        }

        public void DecreaseMainAssetRefCount() {
            _mainAssetRefCount--;
            RefreshSpawnedState();
        }
        
        public void IncreaseProlongedAssetRefCount() {
            _prolongedAssetRefCount++;
            RefreshSpawnedState();
        }

        public void DecreaseProlongedAssetRefCount() {
            _prolongedAssetRefCount--;
            RefreshSpawnedState();
        }

        public void ReceiveEvent(ScriptedEventEventType type) {
            switch (type) {
                case ScriptedEventEventType.IncreaseProlongedAssetRefCount:
                    IncreaseProlongedAssetRefCount();
                    break;
                case ScriptedEventEventType.DecreaseProlongedAssetRefCount:
                    DecreaseProlongedAssetRefCount();
                    break;
                case ScriptedEventEventType.IncreaseMainAssetRefCount:
                    IncreaseMainAssetRefCount();
                    break;
                case ScriptedEventEventType.DecreaseMainAssetRefCount:
                    DecreaseMainAssetRefCount();
                    break;
            }
        }
        
        void Init() {
            _initialized = true;
            if (availability.HasFlag) {
                _flagListener = World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.UniqueFlagChanged(availability.Flag), _ => RefreshAvailability());
            }
            RefreshAvailability();
        }

        void RefreshAvailability() {
            _available = availability.Get(true);
            gameObject.SetActive(_available);
            RefreshSpawnedState();
        }

        void RefreshSpawnedState() {
            var shouldBeSpawned = _initialized && ((_mainAssetRefCount > 0 && _available) || _prolongedAssetRefCount > 0);
            if (shouldBeSpawned & !_spawned) {
                asset.LoadAsset<GameObject>().OnComplete(OnAssetLoaded);
                _spawned = true;
            } else if (!shouldBeSpawned & _spawned) {
                if (_instance) {
                    Object.Destroy(_instance);
                    _instance = null;
                }
                asset.ReleaseAsset();
                _spawned = false;
            }
        }
        
        void OnAssetLoaded(ARAsyncOperationHandle<GameObject> handle) {
            if (handle.Status == AsyncOperationStatus.Succeeded) {
                _instance = Instantiate(handle.Result, World.Services.Get<ViewHosting>().DynamicHost(gameObject.scene));
                foreach (var holder in _instance.GetComponentsInChildren<IScriptedEventHolder>()) {
                    holder.ScriptedEvent = this;
                }
                foreach (var animator in _instance.GetComponentsInChildren<Animator>()) {
                    foreach (var holder in animator.GetBehaviours<ScriptedEventStateMachineBehaviour>()) {
                        holder.ScriptedEvent = this;
                    }
                }
            }
        }
        
#if UNITY_EDITOR
        public EDITOR_Accessor EditorAccessor => new(this);
        public readonly struct EDITOR_Accessor {
            readonly ScriptedEvent _instance;

            public EDITOR_Accessor(ScriptedEvent instance) {
                _instance = instance;
            }

            public ref ARAssetReference Asset => ref _instance.asset;
            public ref FlagLogic Availability => ref _instance.availability;
        }
#endif
    }
}