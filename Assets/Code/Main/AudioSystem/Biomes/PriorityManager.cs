using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Audio;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem.Biomes {
    [NoPrefab, RequireComponent(typeof(ARFmodEventEmitter))]
    public class PriorityManager : View<Model> {

        // === Fields
        ARFmodEventEmitter _emitter;
        InfluencerMode _influencerMode;
        [SerializeReference] IAudioSource _audioPlaying;
        // The index on this list is the priority in IAudioSource
        [SerializeField, ListDrawerSettings(DraggableItems = false, ShowIndexLabels = true, ShowFoldout = false, IsReadOnly = true), InlineProperty, LabelWidth(25)]
        protected List<AudioCategory> priorityList;
        [SerializeField] bool playRandomClip;
        [SerializeField] bool autoPlay = true;
        
        // === Properties
        public ARFmodEventEmitter Emitter {
            get {
                if (_emitter == null) {
                    _emitter = GetComponent<ARFmodEventEmitter>();
                }
                return _emitter;
            }
        }
        public InfluencerMode InfluencerMode => _influencerMode ?? World.Only<InfluencerMode>();
        public bool ContainsValidAudioClips => priorityList.Count > 0 && priorityList.Any(s => !s.IsEmpty());
        protected IAudioSource AudioPlaying {
            get => _audioPlaying;
            set {
                _audioPlaying?.SetRefreshCallback(null);
                _audioPlaying = value;
                _audioPlaying?.SetRefreshCallback(FindNewPriorityAudioSource);
            }
        }

        // === Registration
        public void RegisterAudioSource(IAudioSource audioSource, bool withPlay = true) => RegisterAudioSource_Internal(audioSource, withPlay);

        public void RegisterAudioSources(IAudioSource[] sources, bool withPlay = true) {
            foreach (var source in sources) {
                RegisterAudioSource_Internal(source, withPlay);
            }
            FindNewPriorityAudioSource();
        }
        
        public void UnregisterAudioSources(IAudioSource[] sources) {
            foreach(var source in sources) {
                UnregisterAudioSource(source);
            }
            FindNewPriorityAudioSource();
        }

        void RegisterAudioSource_Internal(IAudioSource audioSource, bool withPlay = false) {
            AudioCategory audioCategory = GetCategoryFromAudioSource(audioSource);

            if (audioCategory.Contains(audioSource)) {
                return;
            }

            audioCategory.Add(audioSource);
            if (withPlay) {
                PlayIfHigherPrioritySound(audioCategory, audioSource);
            }
        }

        public void UnregisterAudioSource(IAudioSource audioSource) {
            AudioCategory audioCategory = GetCategoryFromAudioSource(audioSource);

            if (!audioCategory.Contains(audioSource)) {
                return;
            }

            audioCategory.Remove(audioSource);
            if (AudioPlaying == audioSource) {
                ClearCurrentlyPlayingAudioSource();
                FindNewPriorityAudioSource();
            }
        }

        // === Structural
        public void Init(params IAudioSource[] sourcesToAdd) {
            if (priorityList.Count != AudioCore.MusicPriorityCategoryCount) {
                priorityList.Clear();
                for (int i = 0; i < AudioCore.MusicPriorityCategoryCount; i++) {
                    priorityList.Add(new AudioCategory());
                }
            }

            gameObject.SetActive(true); // For easy visibility that this Manager is active

            foreach (IAudioSource audioSource in sourcesToAdd) {
                RegisterAudioSource_Internal(audioSource);
            }
        }

        public void ForceRecalculatePriority() {
            FindNewPriorityAudioSource();
        }

        public void Clear() {
            priorityList.ForEach(a => a.Clear());
            ClearCurrentlyPlayingAudioSource();
            gameObject.SetActive(false);
        }

        // === Internal
        protected void ValidateCurrentlyPlayingAudioSource() {
            if (AudioPlaying != null && !AudioPlaying.IsValid(this)) {
                ClearCurrentlyPlayingAudioSource();
            }
        }
        void ClearCurrentlyPlayingAudioSource() {
            AudioPlaying = null;
            // Emitter.ChangeEvent(new EventReference());
            // Emitter.Stop();
        }
        
        AudioCategory GetCategoryFromAudioSource(IAudioSource audioSource) {
            int priority = audioSource.GetPriority();
            while (priorityList.Count <= priority) {
                priorityList.Add(new AudioCategory());
            }
            return priorityList[priority];
        }

        void PlayIfHigherPrioritySound(AudioCategory audioCategory, IAudioSource audioSource) {
            if (!audioCategory.AllowCopyrightedMusic && audioSource.IsCopyrighted) {
                return;
            }
            
            if (AudioPlaying != null && AudioPlaying.GetPriority() > audioSource.GetPriority()) {
                return;
            }

            // If same sound track only update reference
            if (AudioPlaying != null && AudioPlaying.EventReference().Guid == audioSource.EventReference().Guid) {
                AudioPlaying = audioSource;
                return;
            }

            AudioPlaying = audioSource;
            SendAudioEvent();
        }

        [ContextMenu(nameof(FindNewPriorityAudioSource))]
        protected virtual void FindNewPriorityAudioSource() {
            ValidateCurrentlyPlayingAudioSource();
            
            IAudioSource newAudioSource = null;
            for (int i = priorityList.Count - 1; i >= 0 && newAudioSource == null; i--) {
                newAudioSource = playRandomClip ? priorityList[i].Random() : priorityList[i].Newest();
            }

            // If no additional sounds found. Use the first added audioSource. Should be the worlds default
            newAudioSource ??= WorldDefault();
            if (newAudioSource == null) {
                return;
            }

            // If same sound track only update reference
            if (AudioPlaying != null && AudioPlaying.EventReference().Guid == newAudioSource.EventReference().Guid) {
                AudioPlaying = newAudioSource;
                return;
            }

            // Run new sound
            AudioPlaying = newAudioSource;
            bool isPlaying = false;//Emitter.IsPlaying();
            if (autoPlay || isPlaying) {
                SendAudioEvent();
            } else {
                ChangeAudioEvent();
            }
        }

        protected IAudioSource WorldDefault() => priorityList[0].Oldest();

        // Should be only place where emitter is modified
        protected void SendAudioEvent() {
            if (AudioPlaying.Position.HasValue) {
                Emitter.transform.position = AudioPlaying.Position.Value;
            }

            //Emitter.PlayNewEventWithPauseTracking(AudioPlaying.EventReference());
        }

        protected void ChangeAudioEvent() {
            if (AudioPlaying.Position.HasValue) {
                Emitter.transform.position = AudioPlaying.Position.Value;
            }
            
            //Emitter.ChangeEvent(AudioPlaying.EventReference());
        }
    }
}