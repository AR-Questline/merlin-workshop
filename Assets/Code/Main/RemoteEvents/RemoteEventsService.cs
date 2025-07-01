#if !UNITY_GAMECORE && !UNITY_PS5
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.RemoteEvents {
    public class RemoteEventsService : MonoBehaviour, IService {
        public const string DebugPrefsKey = "RemoteEventsService_Debug";
        public const string ActiveOnValue = "on";
        [UnityEngine.Scripting.Preserve]  const string ActiveOffValue = "off";
        public const string ActiveEventSuffix = "_active";

        // === Editor references

        [OnValueChanged("ValidateSpecs"), InlineEditor]
        public Object[] remoteEventSpecs = new Object[0];

        // === Editor configuration

        [InfoBox("Editor fake values for testing events")]
        public List<RemoteActivationFaker> activationKeysFaker = new List<RemoteActivationFaker>();
        public List<RemoteConfig.RemoteConfigFaker> configFaker = new List<RemoteConfig.RemoteConfigFaker>();
        
        // === Queries
        public IEnumerable<IRemoteEventSpec> EventsSpecs => remoteEventSpecs.Select(ExtractSpec);

        // === Available event data
        /// <summary>
        /// Try spawn event data for further in-game activation options, info and so on
        /// This DO NOT activate any event, just (should) allows user to activate event 
        /// </summary>
        public void DataInit() {
            World.Services.Get<RemoteConfig>().ListenForConfiguration(SpawnAvailableEventData);
        }

        void SpawnAvailableEventData() {
            RemoteConfig config = World.Services.Get<RemoteConfig>();

            // For editor we want setup overrides (for testing)
#if DEBUG
            if (Application.isEditor || PrefMemory.GetBool(DebugPrefsKey)) {
                foreach (var activationData in activationKeysFaker) {
                    config.SetOverride(activationData.Key, activationData.Value);
                }
                foreach (var configData in configFaker) {
                    config.SetOverride(configData.Key, configData.Value);
                }
            }
#endif

            // Find first active event
            IRemoteEventSpec currentEvent = null;
            var specs = EventsSpecs.GetEnumerator();
            while (currentEvent == null && specs.MoveNext()) {
                var eventSpec = specs.Current;
                if (eventSpec == null) {
                    continue;
                }
                if (IsAvailableEvent(eventSpec.RemoteKey, config)) {
                    currentEvent = eventSpec;
                }
            }
            specs.Dispose();

            // Spawn event data (some graphic in menu or something).
            // Event data is a model so programmer can show event activation button in many ways
            if (currentEvent != null) {
                var remoteEvent = currentEvent.CreateEventData(EventEndDate(currentEvent.RemoteKey) ?? DateTime.UtcNow) as Model;
                AddToWorld(currentEvent, remoteEvent);
            }
        }

        public void EnableInGameEvent(string remoteKey) {
            RemoteConfig config = World.Services.Get<RemoteConfig>();
            var activeEventKey = InGameEventKey(remoteKey);
            config.SetOverride(activeEventKey, ActiveOnValue);
        }
        
        // === Event creation
        
        /// <summary>
        /// Spawns real event based on current remote+overrides config
        /// Only one event can be active
        /// </summary>
        public void EventInit() {
            // One event constraint
            if (World.HasAny<IRemoteEvent>()) {
                var eventNamesEnumerable = World.All<IRemoteEvent>().Select(e => e.Name);
                var eventNames = string.Join(", ", eventNamesEnumerable);
                Log.Important?.Warning($"Run has currently active event-/s: {eventNames}");
                return;
            }
            
            SpawnEvent();
        }

        void SpawnEvent() {
            var remoteConfig = World.Services.Get<RemoteConfig>();
            foreach (IRemoteEventSpec eventSpec in EventsSpecs) {
                if (IsActiveEvent(eventSpec.RemoteKey, remoteConfig)) {
                    var remoteEvent = eventSpec.CreateEvent(EventEndDate(eventSpec.RemoteKey) ?? DateTime.UtcNow) as Model;
                    // One event constraint
                    AddToWorld(eventSpec, remoteEvent);
                    return;
                }
            }
        }

        public void SceneSwitch() {
            World.Services.TryGet<RemoteConfig>()?.RemoveListenerForConfiguration(SpawnAvailableEventData);
        }

        // === Helpers
        static IRemoteEventSpec ExtractSpec(Object specObject) {
            if (specObject is IRemoteEventSpec remoteEventSpec) {
                return remoteEventSpec;
            }
            if (specObject is GameObject specGameObject) {
                return specGameObject.GetComponent<IRemoteEventSpec>();
            }

            return null;
        }
        
        void AddToWorld(IRemoteEventSpec eventSpec, Model remoteEvent) {
            if (remoteEvent == null) {
                Log.Important?.Error($"Event spec {eventSpec.Name} returned event that is no subtype of Model");
                return;
            }

            World.Add(remoteEvent);
        }

        // === Config operators
        public static bool IsAvailableEvent(string eventKey, RemoteConfig config, bool canBeOverride = true) {
            return string.Equals(config.GetValue(eventKey, canBeOverride: canBeOverride).Split('_')[0], ActiveOnValue, StringComparison.InvariantCultureIgnoreCase);
        }
        
        public static bool IsActiveEvent(string eventKey, RemoteConfig config) {
            var activeEventKey = InGameEventKey(eventKey);
            return string.Equals(config.GetValue(activeEventKey).Split('_')[0], ActiveOnValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string InGameEventKey(string remoteKey) => remoteKey + ActiveEventSuffix;

        [UnityEngine.Scripting.Preserve]
        public bool IsAnyEventActive() {
            RemoteConfig config = World.Services.Get<RemoteConfig>();
            return EventsSpecs.Any(spec => IsActiveEvent(spec.RemoteKey, config));
        }

        DateTime? EventEndDate(string key) {
            try {
                RemoteConfig config = World.Services.Get<RemoteConfig>();
                var eventData = config.GetValue(key);
                var splittedData = eventData.Split('_');
                if (splittedData.Length == 2) {
                    string date = splittedData[1];
                    int.TryParse(date.Substring(0, 2), out int day);
                    int.TryParse(date.Substring(2, 2), out int month);
                    int.TryParse(date.Substring(4, 2), out int year);
                    int.TryParse(date.Substring(6, 2), out int hour);
                    return new DateTime(2000 + year, month, day, hour, 0, 0);
                }
            } catch (Exception) {
                // ignored
            }
            return null;
        }

        // === Editor
        /// <summary>
        /// Just for editor data validation
        /// </summary>
        void ValidateSpecs() {
            remoteEventSpecs = remoteEventSpecs.Where(s => ExtractSpec(s) != null).ToArray();
            RevalidateFaker();
        }

        void RevalidateFaker() {
            var allKeys = remoteEventSpecs
                .Select(ExtractSpec)
                .Select(s => s.RemoteKey)
                .ToArray();

            var keysToAdd = allKeys
                .Where(key => activationKeysFaker.All(a => key != a.Key));
            var configFakersToRemove = activationKeysFaker
                .Where(f => allKeys.All(k => k != f.Key))
                .ToArray();
            
            foreach (string keyToAdd in keysToAdd) {
                activationKeysFaker.Add(new RemoteActivationFaker(keyToAdd));
            }

            for (int i = activationKeysFaker.Count - 1; i >= 0; i--) {
                if (configFakersToRemove.Contains(activationKeysFaker[i])) {
                    activationKeysFaker.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Fake event activation entry
        /// </summary>
        [Serializable]
        public class RemoteActivationFaker: RemoteConfig.RemoteConfigFakeEntry {
            [SerializeField, ReadOnly] string key;
            [SerializeField] bool active = false;
            [SerializeField] string date = null;

            public override string Key => key;
            public override string Value => (active ? ActiveOnValue : "off") + "_" + date;

            public RemoteActivationFaker(string key) {
                this.key = key;
            }
        }
    }
}

#endif