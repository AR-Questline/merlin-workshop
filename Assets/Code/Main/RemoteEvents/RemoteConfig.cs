using Awaken.Utility;
#if !UNITY_GAMECORE && !UNITY_PS5
using System;
using System.Collections.Generic;
using Awaken.TG.Main.Saving;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.RemoteEvents {
    /// <summary>
    /// Wrapper for GameAnalytics RemoteConfig.
    /// Caches on-startup config and allows for setting configuration entries overrides.
    /// </summary>
    public partial class RemoteConfig : SerializedService {
        public override ushort TypeForSerialization => SavedServices.RemoteConfig;

        // === Hack
        // GameAnalytics.OnRemoteConfigsUpdatedEvent fires from other thread so we can not use Unity stuffs
        // So we need wait on main thread until GameAnalytics.IsRemoteConfigsReady() becomes true
        // But we need stop waiting when this service become invalid but there is no signal for that
        static int s_currentIndex = 0;
        [UnityEngine.Scripting.Preserve] int _index = 0;
        
        static string s_remoteConfigOverrides;

        // === Data
        Dictionary<string, string> _remoteConfig = new Dictionary<string, string>();
        [Saved] Dictionary<string, string> _overrides = new Dictionary<string, string>();
        bool _initialized = false;
        bool Initialized {
            get => _initialized;
            set {
                _initialized = value;
                if (_initialized) {
                    Dump();
                    OnInitialized?.Invoke();
                    OnInitialized = null;
                }
            }
        }

        event Action OnInitialized; 

        // === Initialization
        public static void EDITOR_RuntimeReset() {
            s_currentIndex = 0;
        }
        
        [UnityEngine.Scripting.Preserve]
        public void Init(bool cleanStart) {
            _index = ++s_currentIndex;
            if (!cleanStart) {
                bool hasSerializedOverrides = !string.IsNullOrWhiteSpace(s_remoteConfigOverrides);
            
                if (hasSerializedOverrides) {
                    _overrides = JsonConvert.DeserializeObject<Dictionary<string, string>>(s_remoteConfigOverrides);
                }
            }

#if UNITY_EDITOR
            Initialized = true;
#else
            CheckConfiguration().Forget();
#endif
        }

#if !UNITY_EDITOR
        [UnityEngine.Scripting.Preserve]
        async Cysharp.Threading.Tasks.UniTaskVoid CheckConfiguration() {
            // while (Application.isPlaying && s_currentIndex == _index && !GameAnalyticsSDK.GameAnalytics.IsRemoteConfigsReady()) {
            //     await Cysharp.Threading.Tasks.UniTask.Delay(1000);
            // }

            // if (Application.isPlaying && GameAnalyticsSDK.GameAnalytics.IsRemoteConfigsReady()) {
            //     CacheConfiguration();
            // }
        }

        /// <summary>
        /// Get configuration from GameAnalytics and cache it
        /// </summary>
        void CacheConfiguration() {
            // if (!Initialized) {
            //     var serializedConfig = GameAnalyticsSDK.GameAnalytics.GetRemoteConfigsContentAsString();
            //     _remoteConfig = JsonConvert.DeserializeObject<Dictionary<string, string>>(serializedConfig);
            //     if (_overrides.Count < 1) {
            //         _overrides = new Dictionary<string, string>(_remoteConfig);
            //     }
            //     Initialized = true;
            // }
        }
#endif

        /// <summary>
        /// Register for initialization callback.
        /// If RemoteConfig is already initialized callback fires in this method call.
        /// </summary>
        /// <param name="callback">Callback to fire</param>
        public void ListenForConfiguration(Action callback) {
            if (!Initialized) {
                OnInitialized += callback;
            } else {
                callback?.Invoke();
            }
        }
        
        public void RemoveListenerForConfiguration(Action callback) {
            OnInitialized -= callback;
        }

        // === Queries
        /// <summary>
        /// Overrides value obtained from GameAnalytics
        /// </summary>
        /// <example>
        /// On title screen we want get active event, but in game we want start event only if user want.
        /// So from server we get 'on', we spawns appropriate model and set override to 'off'.
        /// User gets button to set override to 'on' only for active event.
        /// </example>
        /// <param name="key">Key</param>
        /// <param name="value">Value associated with key</param>
        public void SetOverride(string key, string value) {
            _overrides[key] = value;
            Dump();
        }
        
        public string GetValue(string key, string defaultValue = "", bool canBeOverride = true) {
            if (canBeOverride && _overrides.TryGetValue(key, out var value)) {
                return value;
            }

            if (_remoteConfig.TryGetValue(key, out value)) {
                return value;
            }

            return defaultValue;
        }

        // === Additional serialization
        /// <summary>
        /// Dump current configuration to <see cref="DataBundleData"/> for ability in data transfer between Worlds
        /// </summary>
        void Dump() {
            string overrides = JsonConvert.SerializeObject(_overrides);
            s_remoteConfigOverrides = overrides;
        }

        // === Fake entries classes
        /// <summary>
        /// Fake entry in configuration
        /// </summary>
        [Serializable]
        public abstract class RemoteConfigFakeEntry {
            public abstract string Key { get; }
            public abstract string Value { get; }
        }

        /// <summary>
        /// Fake ordinary configuration entry
        /// </summary>
        [Serializable]
        public class RemoteConfigFaker : RemoteConfigFakeEntry {
            [SerializeField, ValidateInput("ValidateKey", "Name length has to be > 0 && < 13")]
            string key;
            [SerializeField] string value;

            public override string Key => key;
            public override string Value => value;

            public RemoteConfigFaker(string key, string value) {
                this.key = key;
                this.value = value;
            }
            
            bool ValidateKey(string keyValue) => !string.IsNullOrEmpty(keyValue) && keyValue.Length < 13;
        }
    }
}

#endif
