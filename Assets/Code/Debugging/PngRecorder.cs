using System;
using System.IO;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.Events;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Debugging
{
    public class PngRecorder : MonoBehaviour
    {
        // === Editable properties

        public string targetFolder = ".";
        public int frameRate = 60;

        public UnityEvent onStartRecording;
        public UnityEvent onStopRecording;

        // === Fields

        int _frameNumber;
        string _baseName;

        // === Properties

        public bool IsRecording { get; private set; }

        // === Events

        public event Action OnStateChanged;

        // === Recording

        public void StartRecording() {
            // take control of frame rate
            Time.captureFramerate = frameRate;
            // internal state
            _baseName = DateTime.Now.ToString("s").Replace(":","");
            _frameNumber = 1;
            IsRecording = true;
            // events
            OnStateChanged?.Invoke();
            Log.Important?.Warning("Start taking screenshots");
            
            onStartRecording?.Invoke();
        }

        public void LateUpdate() {
            if (IsRecording) {
                string filename = Path.Combine(targetFolder, string.Format($"{_baseName}-{_frameNumber:00000}.png", _baseName, _frameNumber));
                ScreenCapture.CaptureScreenshot(filename);
                _frameNumber++;
            }

            #if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.R)) {
                if (!IsRecording) {
                    StartRecording();
                } else {
                    FinishRecording();
                }
            }
            #endif
        }

        public void FinishRecording() {
            IsRecording = false;
            Time.captureFramerate = 0;
            // events
            OnStateChanged?.Invoke();
            
            onStopRecording?.Invoke();
        }
    }
}
