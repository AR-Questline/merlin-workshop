using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.Utility.Debugging;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility {
    public class EditorAssetsAutosave : IDisposable {
        bool _loggingEnabled;

        readonly EditorAutosaveTick _autosaveTick;
        readonly List<Object> _assetsToSave;
        readonly Action _onAutosaveComplete;

        readonly string _prefix;
        readonly StringBuilder _logBuilder = new();

        public EditorAssetsAutosave(List<Object> assetsToSave, float saveInterval = 120.0f, string targetContext = null, Action onAutosaveComplete = null, bool loggingEnabled = true) {
            _autosaveTick = new EditorAutosaveTick(TrySaveAssets, saveInterval);
            _assetsToSave = assetsToSave;
            _onAutosaveComplete = onAutosaveComplete;

            _prefix = string.IsNullOrEmpty(targetContext) ? "[Editor Autosave]" : $"[{targetContext} Editor Autosave]";
            SetLogging(loggingEnabled);
        }
        
        public void SetAssetsToSave(List<Object> assets) {
            _assetsToSave.Clear();
            AppendAssetsToSave(assets);
        }
        
        public void AppendAssetsToSave(List<Object> assets) {
            _assetsToSave.AddRange(assets);
        }
        
        public void SetLogging(bool enabled) {
            _loggingEnabled = enabled;
        }

        void TrySaveAssets() {
            _logBuilder.Clear();
            LogProcess($"{_prefix} Autosaving assets...");
            int savedAssets = 0;
            
            foreach (Object asset in _assetsToSave.Where(asset => asset != null)) {
                if (EditorUtility.IsDirty(asset.GetInstanceID())) {
                    _logBuilder.AppendLine($"{_prefix} Saving asset {asset.name}");
                    AssetDatabase.SaveAssetIfDirty(asset);
                    savedAssets++;
                } else {
                    _logBuilder.AppendLine($"{_prefix} Asset {asset.name} is not dirty, skipping...");
                }
            }

            LogProcess(_logBuilder.ToString());
            LogProcess($"{_prefix} Autosave complete. Saved {savedAssets} assets.");
            _onAutosaveComplete?.Invoke();
        }
        
        void LogProcess(string log) {
            if (_loggingEnabled) {
                Log.Minor?.Info(log);
            }
        }

        public void Dispose() {
            _autosaveTick.Dispose();
        }
    }
    
    public class EditorAutosaveTick : IDisposable {
        double _timeToSave;
        bool _loggingEnabled;

        readonly Action _onAutosaveTick;
        readonly float _saveInterval;
        
        public EditorAutosaveTick(Action onAutosaveTick, float saveInterval = 120.0f) {
            _onAutosaveTick = onAutosaveTick;
            _saveInterval = saveInterval;
            _timeToSave = EditorApplication.timeSinceStartup + _saveInterval;
            EditorApplication.update += Tick;
        }
        
        void Tick() {
            if (EditorApplication.timeSinceStartup > _timeToSave) {
                _onAutosaveTick?.Invoke();
                _timeToSave = EditorApplication.timeSinceStartup + _saveInterval;
            }
        }

        public void Dispose() {
            EditorApplication.update -= Tick;
        }
    }
}
