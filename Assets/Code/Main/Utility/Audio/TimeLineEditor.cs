#if UNITY_EDITOR
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.TimeLines.Markers;
using CrazyMinnow.SALSA;
using FMOD;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Awaken.TG.Main.Utility.Audio {
    [ExecuteInEditMode]
    public class TimeLineEditor : MonoBehaviour, INotificationReceiver {
        // --- audio settings
        [FoldoutGroup("Spectrum Settings")] [SerializeField] float averageModifier = 1000f;
        [FoldoutGroup("Spectrum Settings")] [Tooltip("how many samples of audio; must be the power of 2")] [SerializeField] int windowSize = 64;
        [FoldoutGroup("Spectrum Settings")] [SerializeField] DSP_FFT_WINDOW_TYPE windowShape = DSP_FFT_WINDOW_TYPE.HAMMING; //fft - Fast Fourier Transform
        
        // Salsa _salsa;
        // ChannelGroup _channelGroup;
        // DSP _dsp; //digital signal processor
        // DSP_PARAMETER_FFT _fftParam;
        
        float[] _samples;
        PlayableDirector _director;
        bool _isPlaying;

        // --- Fields & Properties
        PlayableDirector Director {
            get {
                if (_director == null) {
                    _director = GetComponent<PlayableDirector>();
                    if (_director == null) {
                        _director = gameObject.AddComponent<PlayableDirector>();
                    }
                }
                return _director;
            }
        }

        SEditorText _sEditorText;

        public static void Open(TimelineAsset timelineAsset, SEditorText sEditorText) {
            AssetDatabase.OpenAsset(timelineAsset);
            AssetDatabase.OpenAsset(EditorGUIUtility.Load("TimeLines/TimeLineEditor.prefab"));
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject prefab = prefabStage.prefabContentsRoot;
            TimeLineEditor timeLineEditor = prefab.GetComponent<TimeLineEditor>();
            timeLineEditor._sEditorText = sEditorText;
            timeLineEditor.Director.playableAsset = timelineAsset;
            timeLineEditor.PrepareSalsaAndFmod();
        }

        public void OnNotify(Playable origin, INotification notification, object context) {
            // if (notification is EmotionMarker emotionMarker) {
            //     var emote = _salsa.emoter.emotes.FirstOrDefault(e => e.expData.name.Contains(emotionMarker.emotionKey, StringComparison.InvariantCultureIgnoreCase));
            //     if (emote == null) {
            //         return;
            //     }
            //
            //     _salsa.emoter.UpdateExpressionControllers();
            //     _salsa.emoter.UpdateEmoteLists();
            //     _salsa.emoter.ManualEmote(emote.expData.name, ExpressionComponent.ExpressionHandler.OneWay, isActivating: emotionMarker.emotionState == EmotionState.Enable);
            // }
        }

        void PrepareSalsaAndFmod() {
            // _samples = new float[windowSize];
            //
            // _salsa = GetComponentInChildren<Salsa>();
            // _salsa.getExternalAnalysis += GetExternalAnalysis;
            // _salsa.Initialize();
            //
            // RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out _dsp);
            // _dsp.setParameterInt((int) DSP_FFT.WINDOW, (int) windowShape);
            // _dsp.setParameterInt((int) DSP_FFT.WINDOWSIZE, windowSize * 2);
        }

        void LateUpdate() {
            // if (_salsa != null) {
            //     try {
            //         SalsaUtil.editorTime = (float) EditorApplication.timeSinceStartup;
            //         _salsa.PlaySalsaInEditor();
            //         if ((bool) (UnityEngine.Object) _salsa.emoter) {
            //             _salsa.emoter.PlayEmoterInEditor();
            //         }
            //
            //         _salsa.queueProcessor.PlayQueueInEditor();
            //     } catch (Exception) {
            //         // ignored
            //     }
            // }
        }

        float GetExternalAnalysis() {
            // TimelineAsset timelineAsset = Director.playableAsset as TimelineAsset;
            // if (timelineAsset == null) {
            //     return 0;
            // }
            // var tracks = timelineAsset.GetOutputTracks().ToList();
            // FMODEventPlayable playable = tracks.OfType<FMODEventTrack>().First().GetClips().First().asset as FMODEventPlayable;
            // if (playable?.behavior == null) {
            //     return 0;
            // }
            //
            // _dsp.getParameterData((int) DSP_FFT.SPECTRUMDATA, out IntPtr unmanagedData, out uint _);
            // _fftParam = (DSP_PARAMETER_FFT) Marshal.PtrToStructure(unmanagedData, typeof(DSP_PARAMETER_FFT));
            //
            // playable.behavior.eventInstance.getChannelGroup(out _channelGroup);
            // _channelGroup.addDSP(0, _dsp);
            //
            // if (_fftParam.numchannels > 0) {
            //     _fftParam.getSpectrum(0, ref _samples);
            //     float value = Mathf.Clamp01(_samples.Average() * averageModifier);
            //     return value;
            // }

            return 0;
        }

        [Button("Save Changes to SText")]
        public void SaveToSText() {
            TimelineAsset timelineAsset = Director.playableAsset as TimelineAsset;
            if (timelineAsset == null || _sEditorText == null) {
                EditorUtility.DisplayDialog("Error", "Failed to save changes to SText!", "Close");
                return;
            }
            _sEditorText.emotions.Clear();
            foreach (IMarker marker in timelineAsset.markerTrack.GetMarkers()) {
                if (marker is EmotionMarker e) {
                    _sEditorText.emotions.Add(new EmotionData(e.time, e.duration, e.expressionHandler, e.emotionKey, e.emotionState));
                }
            }
            EditorUtility.SetDirty(_sEditorText);
            EditorUtility.SetDirty(_sEditorText.genericParent);
            EditorUtility.SetDirty(_sEditorText.genericParent.Graph);
            AssetDatabase.SaveAssetIfDirty(_sEditorText.genericParent);
            AssetDatabase.SaveAssetIfDirty(_sEditorText.genericParent.Graph);
        }
    }
}
#endif