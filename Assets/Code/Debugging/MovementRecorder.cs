#if UNITY_EDITOR
using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Awaken.TG.Debugging {
    [ExecuteAlways]
    public class MovementRecorder : MonoBehaviour {

        [FolderPath] public string saveTo;

        AnimationClip _clip;
        GameObjectRecorder _mRecorder;

        [ShowInInspector] [LabelText("Is Recording:")] [DisplayAsString]
        bool _isRecording;

        [Button("Start")]
        [HideIf(nameof(_isRecording))]
        public void StartRecording() {
            // Create recorder and record the script GameObject.
            _mRecorder = new GameObjectRecorder(gameObject);

            _clip = new AnimationClip();

            // Bind all the Transforms on the GameObject and all its children.
            _mRecorder.BindComponentsOfType<Transform>(gameObject, true);
            _isRecording = true;
        }

        void LateUpdate() {
            if (_clip == null || !_isRecording)
                return;

            // Take a snapshot and record all the bindings values for this frame.
            _mRecorder.TakeSnapshot(Time.deltaTime);
        }

        [Button("Stop")]
        [ShowIf(nameof(_isRecording))]
        public void StopRecording() {
            if (_clip == null || !_isRecording)
                return;

            if (_mRecorder.isRecording) {
                // Save the recorded session to the clip.
                _mRecorder.SaveToClip(_clip);

            }

            string fileName = $"Anim_{DateTime.Now:HH_mm_ss}.anim";
            AssetDatabase.CreateAsset(_clip, System.IO.Path.Combine(saveTo, fileName));

            _isRecording = false;
        }
    }
}
#endif