//Inspired by: https://www.youtube.com/watch?v=qbv891Z_3fU
//            https://forum.unity.com/threads/salsa-lipsync-suite-lip-sync-emote-head-eye-and-eyelid-control-system.242135/page-23#post-4179130
//            https://fmod.com/resources/documentation-unity?version=2.02&page=examples-spectrum-analysis.html

using CrazyMinnow.SALSA;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.VendorWrappers.Salsa;
using UnityEngine;

namespace Awaken.TG.Main.AudioSystem {
    [RequireComponent(typeof(Salsa))]
    public class SalsaFmodEventEmitter : VoiceOversEventEmitter {
        [SerializeField] Emoter emphasisEmoter;
        [SerializeField] Emoter storyEmoter;
        readonly List<EmotionData> _emotions = new();
        Salsa _salsa;

        // === Initialization
        protected override void Prepare() {
            _salsa = GetComponent<Salsa>();
            //_salsa.getExternalAnalysis += GetExternalAnalysis;
        }

        // === LifeCycle
        protected override void OnSpeakingStarted(EmotionData[] emotions) {
            if (_salsa == null) {
                return;
            }
            
            if (emotions == null) {
                _emotions.Clear();
                _salsa.emoter = emphasisEmoter;
                return;
            }
            // Detach emoter if there are custom emotes to express. Otherwise you won't be able to use custom emotes with storyEmoter because default emphasis emotes will run with Salsa and override everything
            _salsa.emoter = emotions.Length <= 0 ? emphasisEmoter : null;
            _emotions.AddRange(emotions);
        }
        
        protected override void SpeakingUpdate(double timePlaying) {
            if (_emotions == null) {
                return;
            }

            for (int i = _emotions.Count - 1; i >= 0; i--) {
                var data = _emotions[i];
                if (data.startTime <= timePlaying) {
                    TriggerStoryEmotion(data);
                    _emotions.Remove(data);
                }
            }
        }

        protected override void OnSpeakingEnded() {
            // --- If there was any emotion left after completing loop - trigger them
            if (_emotions.Count > 0) {
                _emotions.ForEach(TriggerStoryEmotion);
                _emotions.Clear();
            }
        }

        public void TriggerEmotion(SalsaEmotion emotion) {
            if (_emotions is { Count: > 0 }) {
                return;
            }

            if (_salsa == null || emphasisEmoter == null) {
                return;
            }

            _salsa.emoter = emphasisEmoter;
            // emphasisEmoter.UpdateExpressionControllers();
            // emphasisEmoter.UpdateEmoteLists();
            // emphasisEmoter.ManualEmote(emotion.Index, emotion.ExpressionHandler);
        }

        // === Helpers
        void TriggerStoryEmotion(EmotionData data) {
            var emote = storyEmoter.emotes.FirstOrDefault(e => e.expData.name == data.emotionKey);
            if (emote == null) {
                return;
            }
            
            // storyEmoter.UpdateExpressionControllers();
            // storyEmoter.UpdateEmoteLists();
            // storyEmoter.ManualEmote(emote.expData.name, data.expressionHandler, data.roundDuration, isActivating: data.state == EmotionState.Enable);
        }
    }
}