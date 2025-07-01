using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.ECS.Flocks;
using Awaken.ECS.Flocks.Authorings;
using Awaken.TG.Graphics;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Awaken.Utility.Maths.Data;
using FMODUnity;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem.Biomes {
    public class AudioCore : MonoBehaviour, IService {
        public const int MusicPriorityCategoryCount = 6;
        const string DayAndNightFmodParam = "DayAndNightCycle";
        const string WeatherFmodParam = "Weather";
        const string CombatLevelParam = "CombatLevel";
        const string CombatTierParam = "Tier";
        const string AlertTierParam = "TierAlert";
        const string RainIntensityParam = "RainIntensity";
        const string WyrdnessParam = "Wyrdness";
        const string WyrdStalkerParam = "WyrdStalkerProximity";
        const string BirdsCount01 = "BirdsCount01";

        const string TimeScale = "TimeScale";
        const float RainIntensityUpdateSpeed = 0.5f;
        bool _enabled = true;
        bool _initialized;
        bool _sfxPaused;

        static CoreParameters s_parameters;

        GameRealTime GameRealTime => _gameRealTime ??= World.Any<GameRealTime>();
        GameRealTime _gameRealTime;

        [ReadOnly]
        public bool interpolateCombatLevel = true;
        [Title("Ambient queue"), SerializeField]
        public PriorityManager ambient;

        [Title("Music queue"), SerializeField] public PriorityManager music;
        [SerializeField] public PriorityManager musicAlert;
        [SerializeField] public CombatPriorityManager musicCombat;
        
        [Title("Snapshot queue"), SerializeField]
        public PriorityManager snapshot;

        // === World Default Configuration
        [Space(40)] [Title("World Configuration")]
        [SerializeField] public EventReference worldExplorationMusicDefault;
        // === Rain Configuration
        [Space(40)] [Title("Rain Configuration")]
        [SerializeField] public ARFmodEventEmitter rainEventEmitter;

        readonly HashSet<IRainIntensityModifier> _rainIntensityModifiers = new();
        DelayedValue _rainIntensityMultiplier;
        CombatLevel? _currentCombatLevel;

        HeroCombat _heroCombat;
        HeroCombat HeroCombat {
            get {
                if (_heroCombat is {HasBeenDiscarded: true}) {
                    _heroCombat = null;
                }
                return _heroCombat ??= World.Any<HeroCombat>();
            }
        }

        HeroWyrdNight _heroWyrdNight;
        HeroWyrdNight HeroWyrdNight {
            get {
                if (_heroWyrdNight is {HasBeenDiscarded: true}) {
                    _heroWyrdNight = null;
                }
                return _heroWyrdNight ??= World.Any<HeroWyrdNight>();
            }
        }
        
        WeatherController _weatherController;
        WeatherController WeatherController => _weatherController ?? World.Any<WeatherController>();
        public int HighestTierAlerted { get; private set; }
        bool HeroInDialogue => Hero.Current?.HasElement<Fights.NPCs.DialogueInvisibility>() ?? false;
        
        // === Unity Events
        void OnDisable() {
            Clear();
        }

        void Update() {
            if (!_initialized || !_enabled || GameRealTime == null) {
                return;
            }
            
            s_parameters.timeOfDay = GameRealTime.WeatherTime.DayTime;
            s_parameters.combatLevel = interpolateCombatLevel ? (HeroCombat?.AudioCombatLevel ?? 0) : (HeroCombat?.DesiredAudioCombatLevel ?? 0);
            
            int alertTier = HeroCombat?.TierForAlert ?? 0;
            HighestTierAlerted = interpolateCombatLevel ? Mathf.FloorToInt(HeroCombat?.HighestTierAlerted ?? 0) : alertTier;
            s_parameters.combatTier = HighestTierAlerted;
            s_parameters.alertTier = alertTier;
            
            s_parameters.rainIntensity = UpdateRainIntensity();
            s_parameters.birdsCount01 = GetBirdsCount01();
            
            if (HeroWyrdNight is { } heroWyrdNight){
                s_parameters.wyrdness = heroWyrdNight.IsHeroInWyrdness ? 1 : 0;
                s_parameters.wyrdStalkerProximity = heroWyrdNight.WyrdStalker.AudioProximity;
            } else {
                s_parameters.wyrdness = 0;
                s_parameters.wyrdStalkerProximity = 0;
            }
            UpdateEmitterParams(ambient.Emitter, s_parameters);
            UpdateEmitterParams(music.Emitter, s_parameters);
            UpdateGlobalParams(s_parameters, Time.timeScale);
            DetermineMusicToPlay(s_parameters.combatLevel);

            // if (s_parameters.rainIntensity > 0) {
            //     rainEventEmitter.SetParameter(RainIntensityParam, s_parameters.rainIntensity);
            //     if (!rainEventEmitter.IsPlaying()) {
            //         rainEventEmitter.Play();
            //     }
            // } else if (s_parameters.rainIntensity <= 0 && rainEventEmitter.IsPlaying()) {
            //     rainEventEmitter.Stop();
            // }
        }

        float UpdateRainIntensity() {
            SceneService sceneService = World.Services.TryGet<SceneService>();
            if (sceneService is not { IsOpenWorld: true }) {
                return 0;
            }
            
            if (_rainIntensityModifiers.RemoveWhere(static m => m == null || m.Owner == null) > 0) {
                RefreshRainIntensityMultiplier();
            }
            
            float rainIntensity = WeatherController?.RainIntensity ?? 0;
            _rainIntensityMultiplier.Update(Time.unscaledDeltaTime, RainIntensityUpdateSpeed);
            return rainIntensity * _rainIntensityMultiplier.Value;
        }

        float GetBirdsCount01() {
            var flyingFlockSoundSystem = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<FlyingFlockSoundSystem>();
            int birdsCount = flyingFlockSoundSystem.FlyingBirdsCount + flyingFlockSoundSystem.RestingBirdsCount + flyingFlockSoundSystem.TakingOffBirdsCount;
            return math.clamp(birdsCount / (float)FlockGroup.MaxEntitiesCount, 0, 1);
        }
        
        public void Initialize() {
            if (!_enabled) return;
            
            Clear();
            music.Init(new WorldAudioSource(worldExplorationMusicDefault, false));
            musicAlert.Init();
            musicCombat.Init();
            ambient.Init(new WorldAudioSource(new EventReference()));
            snapshot.Init(new WorldAudioSource(new EventReference()));
            _rainIntensityMultiplier.SetInstant(1);
            AttachListeners();
            
            _initialized = true;
        }

        void AttachListeners() {
            if (_initialized) {
                return;
            }
            
            UIStateStack uiStateStack = UIStateStack.Instance;
            uiStateStack.ListenTo(UIStateStack.Events.UIStateChanged, OnUIStateChange, this);
            ToggleBusPause(uiStateStack.State.PauseTime);
        }

        void OnUIStateChange(UIState uiState) {
            ToggleBusPause(uiState.PauseTime);
        }
        
        void ToggleBusPause(bool pause) {
            if (_sfxPaused == pause) {
                return;
            }
            _sfxPaused = pause;
            foreach (var busGroup in RichEnum.AllValuesOfType<BusGroup>()) {
                // if (busGroup.TryGetBus(out var bus)) {
                //     bus.setPaused(pause);
                // }
            }
        }
        
        void Clear() {
            music.Clear();
            musicAlert.Clear();
            musicCombat.Clear();
            ambient.Clear();
            snapshot.Clear();
            //rainEventEmitter.Stop();
            _heroCombat = null;
            _currentCombatLevel = null;
        }

        public void Toggle(bool enable) {
            _enabled = enable;
            if (enable) {
                Initialize();
            } else {
                Clear();
            }
        }

        public void Play() {
            // ambient.Emitter.Play();
            // snapshot.Emitter.Play();
            // rainEventEmitter.Play();
            DetermineMusicToPlay(HeroCombat?.AudioCombatLevel ?? 0);
        }

        public void Stop() {
            // music.Emitter.Stop();
            // musicAlert.Emitter.Stop();
            // musicCombat.Emitter.Stop();
            // ambient.Emitter.Stop();
            // snapshot.Emitter.Stop();
            // rainEventEmitter.Stop();
            
            _currentCombatLevel = null;
        }

        public void ResetMusic() {
            // music.Emitter.Stop(true);
            // musicAlert.Emitter.Stop(true);
            // musicCombat.Emitter.Stop(true);
            music.ForceRecalculatePriority();
            _currentCombatLevel = null;
            DetermineMusicToPlay(HeroCombat?.AudioCombatLevel ?? 0);
        }

        // === Public system interaction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterAudioSource(IAudioSource source, AudioType managerType, bool withPlay = true) {
            GetManagerFromType(managerType).RegisterAudioSource(source, withPlay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RegisterAudioSources(IAudioSource[] source, AudioType managerType, bool withPlay = true) {
            if (source is { Length: > 0 }) {
                GetManagerFromType(managerType).RegisterAudioSources(source, withPlay);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnregisterAudioSource(IAudioSource source, AudioType managerType) {
            GetManagerFromType(managerType).UnregisterAudioSource(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnregisterAudioSources(IAudioSource[] source, AudioType managerType) {
            if (source is { Length: > 0 }) {
                GetManagerFromType(managerType).UnregisterAudioSources(source);
            }
        }

        public void ForceRecalculateSoundPriority() {
            ambient.ForceRecalculatePriority();
            music.ForceRecalculatePriority();
            musicAlert.ForceRecalculatePriority();
            musicCombat.ForceRecalculatePriority();
            snapshot.ForceRecalculatePriority();
        }

        public void SetRainIntensityMultiplier(IRainIntensityModifier rainIntensityModifier, bool instant = false) {
            _rainIntensityModifiers.Add(rainIntensityModifier);
            RefreshRainIntensityMultiplier(instant);
        }

        public void RestoreRainIntensityMultiplier(IRainIntensityModifier rainIntensityModifier, bool instant = false) {
            _rainIntensityModifiers.Remove(rainIntensityModifier);
            RefreshRainIntensityMultiplier(instant);
        }

        void RefreshRainIntensityMultiplier(bool instant = false) {
            float value = _rainIntensityModifiers.Count > 0 ? _rainIntensityModifiers.MinBy(static r => r.MultiplierWhenUnderRoof).MultiplierWhenUnderRoof : 1;
            if (instant) {
                _rainIntensityMultiplier.SetInstant(value);
            } else {
                _rainIntensityMultiplier.Set(value);
            }
        }

        // === Helpers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        PriorityManager GetManagerFromType(AudioType type) =>
            type switch {
                AudioType.Music => music,
                AudioType.MusicAlert => musicAlert,
                AudioType.MusicCombat => musicCombat,
                AudioType.Ambient => ambient,
                AudioType.Snapshot => snapshot,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        static void UpdateEmitterParams(StudioEventEmitter emitter, CoreParameters parameters) {
            // if (!emitter.EventInstance.isValid() || !emitter.IsPlaying()) {
            //     return;
            // }
            // emitter.SetParameter(CombatLevelParam, parameters.combatLevel);
            // emitter.SetParameter(CombatTierParam, parameters.combatTier);
            // emitter.SetParameter(AlertTierParam, parameters.alertTier);
            // emitter.SetParameter(WyrdStalkerParam, parameters.wyrdStalkerProximity);
        }
        
        static void UpdateGlobalParams(CoreParameters parameters, float timeScale) {
            // var studioSystem = RuntimeManager.StudioSystem;
            // studioSystem.setParameterByName(DayAndNightFmodParam, parameters.timeOfDay);
            // studioSystem.setParameterByName(WeatherFmodParam, parameters.rainIntensity);
            // studioSystem.setParameterByName(WyrdnessParam, parameters.wyrdness);
            // studioSystem.setParameterByName(TimeScale, timeScale);
            // studioSystem.setParameterByName(BirdsCount01, parameters.birdsCount01);
        }

        void DetermineMusicToPlay(float combatLevel) {
            CombatLevel newCombatLevel;
            if (_heroWyrdNight?.IsHeroInWyrdness ?? false) {
                // --- Wyrdnight music implementation is based on combat level param.
                newCombatLevel = CombatLevel.Exploration;
            } else {
                newCombatLevel = combatLevel switch {
                    < 0.1f => CombatLevel.Exploration,
                    < 1.1f => CombatLevel.Alert,
                    _ => CombatLevel.Combat
                };
            }

            if (newCombatLevel == CombatLevel.Combat) {
                musicCombat.ForceRecalculatePriority();
            }

            bool combatLevelChanged = _currentCombatLevel != newCombatLevel;
            if (!combatLevelChanged && IsCorrectMusicEmitterPlaying(newCombatLevel)) {
                return;
            }

            _currentCombatLevel = newCombatLevel;
            bool isPlaying = newCombatLevel switch {
                CombatLevel.Exploration => PlayExplorationMusic(),
                CombatLevel.Alert => PlayAlertMusic(),
                CombatLevel.Combat => PlayCombatMusic(),
                _ => throw new ArgumentOutOfRangeException(nameof(combatLevel), combatLevel, null)
            };

            if (!isPlaying) {
                _currentCombatLevel = null;
            }
        }

        bool PlayExplorationMusic() {
            // music.ForceRecalculatePriority();
            // if (!music.Emitter.IsPlaying() && !HeroInDialogue) {
            //     music.Emitter.Play();
            // }
            // musicAlert.Emitter.Stop();
            // musicCombat.Emitter.Stop();
            // musicCombat.Reset();
            //
            // return music.Emitter.IsPlaying();
            return false;
        }

        bool PlayAlertMusic() {
            // if (!musicAlert.ContainsValidAudioClips) {
            //     return PlayExplorationMusic();
            // }
            // music.Emitter.Stop();
            // musicCombat.Emitter.Stop();
            //
            // musicAlert.ForceRecalculatePriority();
            // if (!musicAlert.Emitter.IsPlaying()) {
            //     musicAlert.Emitter.Play();
            // }
            //
            // return musicAlert.Emitter.IsPlaying();
            return false;
        }

        bool PlayCombatMusic() {
            // if (!musicCombat.ContainsValidAudioClips) {
            //     return PlayExplorationMusic();
            // }
            // music.Emitter.Stop();
            // musicAlert.Emitter.Stop();
            //
            // musicCombat.ForceRecalculatePriority();
            // if (!musicCombat.Emitter.IsPlaying()) {
            //     musicCombat.Emitter.Play();
            // }
            //
            // return musicCombat.Emitter.IsPlaying();
            return false;
        }

        public string GetAudioCoreState() {
            return
                $"{GetEmitterStateString(music.Emitter, "Music")}" +
                $"{GetEmitterStateString(musicAlert.Emitter, "Music Alert")}" +
                $"{GetEmitterStateString(musicCombat.Emitter, "Music Combat")}" +
                $"{GetEmitterStateString(ambient.Emitter, "Ambient")}" +
                $"{GetEmitterStateString(snapshot.Emitter, "Snapshot")}";
        }

        public static string GetGlobalParametersState() {
            // var studioSystem = RuntimeManager.StudioSystem;
            // studioSystem.getParameterByName(DayAndNightFmodParam, out float dayAndNight);
            // studioSystem.getParameterByName(WeatherFmodParam, out float rainIntensity);
            // studioSystem.getParameterByName(CombatLevelParam, out float combatLevel);
            // studioSystem.getParameterByName(CombatTierParam, out float combatTier);
            // studioSystem.getParameterByName(WyrdnessParam, out float wyrdness);
            // studioSystem.getParameterByName(WyrdStalkerParam, out float wyrdStalkerProximity);
            // studioSystem.getParameterByName(TimeScale, out float timeScale);
            // studioSystem.getParameterByName(BirdsCount01, out float birdsCount01);
            // string state = $"DayAndNight: {dayAndNight}\n" +
            //                $"RainIntensity: {rainIntensity}\n" +
            //                $"CombatLevel: {combatLevel}\n" +
            //                $"CombatTier: {combatTier}\n" +
            //                $"Wyrdness: {wyrdness}\n" +
            //                $"WyrdStalkerProximity: {wyrdStalkerProximity}\n" +
            //                $"TimeScale: {timeScale}\n" +
            //                $"BirdsCount01: {birdsCount01}";
            // return state;
            return string.Empty;
        }

        static string GetEmitterStateString(StudioEventEmitter emitter, string emitterName) {
            return $"{emitterName}:\n   Event: {emitter.EventReference}\n";
        }
        
        enum CombatLevel : byte {
            Exploration = 0,
            Alert = 1,
            Combat = 2,
        }
        
        internal struct CoreParameters {
            public float timeOfDay;
            public float rainIntensity;
            public float combatLevel;
            public float combatTier;
            public float alertTier;
            public float wyrdness;
            public float wyrdStalkerProximity;
            public float birdsCount01;
        }

        bool IsCorrectMusicEmitterPlaying(CombatLevel currentCombatLevel) {
            // bool isExplorationPlaying = music.Emitter.IsPlaying();
            // bool isAlertPlaying = musicAlert.Emitter.IsPlaying();
            // bool isCombatPlaying = musicCombat.Emitter.IsPlaying();
            // return currentCombatLevel switch {
            //     CombatLevel.Exploration => isExplorationPlaying && !isAlertPlaying && !isCombatPlaying,
            //     CombatLevel.Alert => isAlertPlaying && !isExplorationPlaying && !isCombatPlaying,
            //     CombatLevel.Combat => isCombatPlaying && !isExplorationPlaying && !isAlertPlaying,
            //     _ => false
            // };
            return false;
        }
    }
}