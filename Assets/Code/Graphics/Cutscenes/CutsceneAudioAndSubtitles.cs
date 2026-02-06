using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Utility.Video.Subtitles;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Graphics.Cutscenes {
    public class CutsceneAudioAndSubtitles : MonoBehaviour, ICutsceneAttachment {
        [SerializeField] EventReference _audio;
        [SerializeField] SubtitlesData _subtitleData;
        [SerializeField] ARFmodEventEmitter _audioPlayer;
        [SerializeField] bool startAfterTransitions;
        
        public SubtitlesData CurrentSubtitles => _subtitleData;

        public void OnCutsceneInit(VCutsceneBase vCutscene) {
            if (!startAfterTransitions) {
                StartAudioAndSubtitles(vCutscene);
            }
        }

        public void OnCutsceneStart(VCutsceneBase vCutscene) {
            if (startAfterTransitions) {
                StartAudioAndSubtitles(vCutscene);
            }
        }

        void StartAudioAndSubtitles(VCutsceneBase vCutscene) {
            if (_audioPlayer && !_audio.IsNull) {
                // _audioPlayer.PlayNewEventWithPauseTracking(_audio);
            }
            if (_subtitleData != null) {
                var cutscene = vCutscene.Target;
                var vSimpleSubtitlesHost = VSimpleSubtitlesHost.BindToModel(cutscene);
                cutscene.AddElement(new CutsceneSubtitles(vCutscene, vSimpleSubtitlesHost, this));
            }
        }

        public void OnCutsceneEnd(VCutsceneBase vCutscene) {
            if (_audioPlayer) {
                // _audioPlayer.Stop();
            }
        }
        
        public void OnCutscenePaused() {
            if (_audioPlayer) {
                // _audioPlayer.Pause();
            }
        }
        
        public void OnCutsceneUnpaused() {
            if (_audioPlayer) {
                // _audioPlayer.UnPause();
            }
        }
    }
}