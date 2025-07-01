using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Awaken.TG.Main.Utility.Patchers {
    public abstract class Patcher {
        public Version PatcherFinalVersion => _finalVersion ??= FinalVersion; 
        protected virtual Version MinInputVersion => new Version(0, 0);
        protected virtual Version MaxInputVersion => new Version(Application.version);
        protected virtual Version FinalVersion => new Version(Application.version);
        
        Version _minInputVersion;
        Version _maxInputVersion;
        Version _finalVersion;

        protected Patcher() {
            _minInputVersion = MinInputVersion;
            _maxInputVersion = MaxInputVersion;
            _finalVersion = FinalVersion;
        }
        public virtual bool CanPatch(Version version) {
            return _finalVersion.CompareTo(version) != 0 && _maxInputVersion.CompareTo(version) >= 0 && _minInputVersion.CompareTo(version) <= 0;
        } 

        public virtual void StartGamePatch() { }

        public virtual void BeforeDeserializedModel(Model model) { }
        public virtual bool AfterDeserializedModel(Model model) => true;
        
        public virtual void AfterRestorePatch() { }

        [UnityEngine.Scripting.Preserve]
        protected JObject RemovePropertyFromJObject(JObject jObject, string propName) {
            List<JProperty> properties = jObject.Properties().ToList();
            for (int i = properties.Count - 1; i >= 0; i--) {
                if (properties[i].Name == propName) {
                    jObject.Remove(properties[i].Name);
                }
            }
            return jObject;
        }
    }
}