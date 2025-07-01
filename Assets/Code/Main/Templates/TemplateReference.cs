using System;
using System.Diagnostics;
using System.Linq;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Newtonsoft.Json;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Templates {
    [Serializable]
    public sealed partial class TemplateReference : IEquatable<TemplateReference> {
        public ushort TypeForSerialization => SavedTypes.TemplateReference;
        
        // === Fields
        [Saved, SerializeField] string _guid;

        public string GUID => _guid;
        public bool IsSet => !string.IsNullOrEmpty(_guid);

        // === Constructors

        public TemplateReference() { }
        
        public TemplateReference(ITemplate instance) {
            _guid = instance.GUID;
        }

        public TemplateReference(string guid) {
            _guid = guid;
        }

        // === Operations

        public T Get<T>(object debugTarget = null) where T : ITemplate {
            if (TryGet<T>(out var instance, debugTarget)) {
                return instance;
            }

            DEBUG_EmptyGUIDWarning(debugTarget);
            return default;
        }
        
        public bool TryGet<T>(out T instance, object debugTarget = null) where T : ITemplate {
            if (!string.IsNullOrEmpty(_guid)) {
                instance = TemplatesUtil.Load<T>(_guid);
                
                if (instance == null) {
                    DEBUG_NullResultWarning(debugTarget);
                    return false;
                } else if (PlatformUtils.IsPlaying) {
                    TemplateTypeFlag allowedTypes = TemplateTypeFlag.Regular;
                    
                    if (debugTarget is ProxyDebugTargetSource proxy) {
                        allowedTypes = proxy.allowedTemplateTypes;
                    } else if (debugTarget is CommonReferences or GameConstants) {
                        allowedTypes |= TemplateTypeFlag.System;
                    }
                    
                    if (instance.TemplateType == TemplateType.System && !allowedTypes.HasFlagFast(TemplateTypeFlag.System)) {
                        Log.Minor?.Error($"Tried to get 'System' template from non system source '{_guid}' '{instance}' '{LogUtils.GetDebugName(debugTarget)}'");
                    } else if (!allowedTypes.Contains(instance.TemplateType)) {
                        Log.Minor?.Error($"Tried to get {instance.TemplateType.ToStringFast()} template '{_guid}' '{instance}' '{LogUtils.GetDebugName(debugTarget)}' when only {allowedTypes.ToStringFast()} are allowed");
                    }
                }

                return true;
            }

            instance = default;
            return false;
        }
        
        public T TryGet<T>(object debugTarget = null) where T : ITemplate {
            TryGet(out T instance, debugTarget);
            return instance;
        }

        public void WriteSavables(JsonWriter jsonWriter, JsonSerializer serializer) {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName(nameof(_guid));
            jsonWriter.WriteValue(_guid);
            jsonWriter.WriteEndObject();
        }

        // === Equality members
        
        public bool Equals(ITemplate other) {
            return _guid == other?.GUID;
        }
        
        public bool Equals(TemplateReference other) {
            return _guid == other?._guid;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TemplateReference) obj);
        }

        public override int GetHashCode() {
            return (_guid != null ? _guid.GetHashCode() : 0);
        }
        
        public static bool operator ==(TemplateReference a, TemplateReference b) {
            return Equals(a, b);
        }
        
        public static bool operator !=(TemplateReference a, TemplateReference b) {
            return !Equals(a, b);
        }
        
        // === DEBUG
        [Conditional("DEBUG")]
        void DEBUG_EmptyGUIDWarning(object debugTarget) {
            ExtractDebugData(debugTarget, out var unityTarget, out var name);
            Log.Minor?.Warning($"Template reference has empty guid. Called for: [{name}]", unityTarget);
        }

        [Conditional("DEBUG")]
        void DEBUG_NullResultWarning(object debugTarget) {
            ExtractDebugData(debugTarget, out var unityTarget, out var name);
            Log.Minor?.Warning($"Template reference (with guid: [{_guid}]) has empty result. Called for: [{name}]", unityTarget);
        }
        
        static void ExtractDebugData(object debugTarget, out Object unityTarget, out string name) {
            if (debugTarget is ProxyDebugTargetSource proxy) {
                debugTarget = proxy.realTarget;
            }
            unityTarget = debugTarget switch {
                MonoBehaviour monoBehaviour => monoBehaviour,
                ScriptableObject scriptableObject => scriptableObject,
                IModel model                => model.Views.FirstOrDefault(v => v is MonoBehaviour) as MonoBehaviour,
                _                           => null,
            };
            name = debugTarget switch {
                null => "Null",
                _ => LogUtils.GetDebugName(debugTarget)
            };
        }
        
        public class ProxyDebugTargetSource {
            public readonly object realTarget;
            public readonly TemplateTypeFlag allowedTemplateTypes;

            public ProxyDebugTargetSource(object realTarget, TemplateTypeFlag allowedTemplateTypes) {
                this.realTarget = realTarget;
                this.allowedTemplateTypes = allowedTemplateTypes;
            }
        }

        public static SerializationAccessor Serialization(TemplateReference instance) => new(instance);
        public struct SerializationAccessor {
            TemplateReference _instance;
            
            public SerializationAccessor(TemplateReference instance) {
                _instance = instance;
            }
            
            public ref string Guid => ref _instance._guid;
        }
    }
}
