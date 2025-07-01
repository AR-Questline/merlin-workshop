using Awaken.TG.Assets;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Sessions;

namespace Awaken.TG.Main.Scenes {
    public class SceneLifetimeEvents : IEventSource, IListenerOwner {
        static SceneLifetimeEvents s_instance = new();
        static CacheVersion s_version;
        public static SceneLifetimeEvents Get {
            get {
                if (s_version.NeedUpdate()) {
                    World.EventSystem.RemoveAllListenersOwnedBy(s_instance);
                    s_instance = new SceneLifetimeEvents();
                }
                return s_instance;
            }
        }

        public bool ValidMainSceneState { get; private set; }
        public bool ValidAdditiveSceneState { get; private set; }
        public bool ValidDomainState { get; private set; }
        public bool MainSceneFullyInitialized { get; private set; }
        public bool AdditiveSceneFullyInitialized { get; private set; }
        public bool MainSceneDiscarded { [UnityEngine.Scripting.Preserve] get; private set; }
        public bool AdditiveSceneDiscarded { [UnityEngine.Scripting.Preserve] get; private set; }
        public bool InInterior { get; private set; }

        public bool EverythingInitialized => ValidMainSceneState && MainSceneFullyInitialized && (!ValidAdditiveSceneState || AdditiveSceneFullyInitialized);
        
        public string ID { get; }
        

        SceneLifetimeEvents() {
            ID = nameof(SceneLifetimeEvents) + "_" + s_version;
            
            World.EventSystem.ListenTo(ID, Events.OnSceneBecameValid, this,
                data => {
                    if (data.IsMainScene) {
                        ValidMainSceneState = true;
                        MainSceneDiscarded = false;
                    } else {
                        ValidAdditiveSceneState = true;
                        AdditiveSceneDiscarded = false;
                    }
                });
            World.EventSystem.ListenTo(ID, Events.AfterDomainDrop, this,
                data => {
                    ValidDomainState = false;
                    if (data.IsMainScene) {
                        ValidMainSceneState = false;
                        MainSceneFullyInitialized = false;
                    } else {
                        ValidAdditiveSceneState = false;
                        AdditiveSceneFullyInitialized = false;
                    }
                });
            World.EventSystem.ListenTo(ID, Events.AfterSceneFullyInitialized, this,
                data => {
                    if (data.IsMainScene) {
                        MainSceneFullyInitialized = true;
                    } else {
                        AdditiveSceneFullyInitialized = true;
                        World.EventSystem.Trigger(SceneLifetimeEvents.Get, Events.SafeAfterSceneChanged, this);
                    }
                });
            World.EventSystem.ListenTo(ID, Events.AfterSceneDiscarded, this,
                data => {
                    if (data.IsMainScene) {
                        MainSceneDiscarded = true;
                    } else {
                        AdditiveSceneDiscarded = true;
                    }
                });
            World.EventSystem.ListenTo(ID, Events.AfterSceneStoriesExecuted, this,
                _ => {
                    World.EventSystem.Trigger(SceneLifetimeEvents.Get, Events.SafeAfterSceneChanged, this);
                });
            World.EventSystem.ListenTo(ID, Events.InteriorStateChanged, this,
                state => {
                    if (!state) {
                        World.EventSystem.Trigger(SceneLifetimeEvents.Get, Events.SafeAfterSceneChanged, this);
                    }
                });
        }

        public static class Events {
            // All events should be called in the order here
            
            // Scene loading
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEventData> BeforeSceneValid = new(nameof(BeforeSceneValid));
            public static readonly Event<SceneLifetimeEvents, SceneReference> AfterNewDomainSet = new(nameof(AfterNewDomainSet));
            public static readonly Event<SceneLifetimeEvents, bool> InteriorStateChanged = new(nameof(InteriorStateChanged));
            public static readonly Event<SceneLifetimeEvents, bool> OpenWorldStateChanged = new(nameof(OpenWorldStateChanged));
            
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEventData> OnSceneBecameValid = new(nameof(OnSceneBecameValid));
            public static readonly Event<SceneLifetimeEvents, SceneReference> OnFullSceneLoaded = new(nameof(OnFullSceneLoaded));

            // Scene initialization
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEventData> AfterServicesInitialized = new(nameof(AfterServicesInitialized));
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEventData> AfterWorldInitialized = new(nameof(AfterWorldInitialized));
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEventData> AfterSceneFullyInitialized = new(nameof(AfterSceneFullyInitialized));
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEventData> AfterSceneStoriesExecuted = new(nameof(AfterSceneStoriesExecuted));
            
            /// <summary>
            /// Use this event if you are only interested in an action after a scene has changed and all loading operations are completed
            /// </summary>
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEvents> SafeAfterSceneChanged = new(nameof(SafeAfterSceneChanged));
            
            // Scene lifetime end
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEventData> AfterSceneDiscarded = new(nameof(AfterSceneDiscarded));
            public static readonly Event<SceneLifetimeEvents, SceneLifetimeEventData> AfterDomainDrop = new(nameof(AfterDomainDrop));
        }
        
        public void ValidateDomain(SceneReference sceneReference, bool isAdditive) {
            ValidDomainState = true;
            var previousAdditive = InInterior;
            InInterior = isAdditive;
            
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, Events.AfterNewDomainSet, sceneReference);
            
            if (previousAdditive != isAdditive) {
                World.EventSystem.Trigger(SceneLifetimeEvents.Get, Events.InteriorStateChanged, InInterior);
            }
        }
    }

    public struct SceneLifetimeEventData {
        public bool IsMainScene { get; }
        public SceneReference SceneReference { get; }
        
        public SceneLifetimeEventData(bool isMainScene, SceneReference sceneReference) {
            IsMainScene = isMainScene;
            SceneReference = sceneReference;
        }
    }
}