using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Templates {
    public class TemplatesProvider : IService {
        TemplatesLoader _loader;

        public bool AllLoaded => _loader?.FinishedLoading ?? false;
        Dictionary<string, ITemplate> GUIDMap => _loader.guidMap;
        MultiMap<Type, ITemplate> TypeMap => _loader.typeMap;

        [UnityEngine.Scripting.Preserve] public string[] Guids {
            get {
                ValidateState();
                return GUIDMap.Keys.ToArray();
            }
        }

        public IEnumerable<ITemplate> AllTemplates {
            get {
                ValidateState();
                return TypeMap.InnerValues;
            }
        }

        public void StartLoading(bool reload = false) {
            if (TemplatesLoader.LoadFromAddressables && reload) {
                _loader = null;
            }
            _loader ??= TemplatesLoader.CreateAndLoad();
        }
        
        public T Get<T>(string guid) {
            ValidateState();
            if (GUIDMap.TryGetValue(guid, out ITemplate template)) {
                if (template is T result) {
                    return result;
                }
                Exception wrongTypeException = new ArgumentException($"Loaded template {template} is not of type {typeof(T)}");
                Debug.LogException(wrongTypeException);
                return default;
            }

            Log.Minor?.Info("Could not find asset with guid: " + guid);
            return default;
        }

        public IEnumerable<ITemplate> GetAllOfType(Type type, TemplateTypeFlag templateType = TemplateTypeFlag.Regular) {
            ValidateState();
            if (TypeMap.TryGetValue(type, out HashSet<ITemplate> templates)) {
                if (templateType == TemplateTypeFlag.All) {
                    return templates;
                } else {
                    return templates.Where(t => templateType.Contains(t.TemplateType));
                }
            }
            return Array.Empty<ITemplate>();
        }

        public IEnumerable<T> GetAllOfType<T>(TemplateTypeFlag types = TemplateTypeFlag.Regular) where T : ITemplate {
            return GetAllOfType(typeof(T), types).Cast<T>();
        }
        
        public IEnumerable<T> GetAllOfType<T>(ICollection<string> tags, TemplateTypeFlag templateType = TemplateTypeFlag.Regular) where T : ITagged, ITemplate {
            return GetAllOfType<T>(templateType).Where(t => TagUtils.HasRequiredTags(t, tags));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateState() {
            if (!AllLoaded) {
                throw new InvalidOperationException("Templates requested before they were fully loaded!");
            }
        }
        
        // === Editor
        public delegate List<ITemplate> AllOfTypeDelegate(Type type, List<ITemplate> results, bool exactType = false, TemplateTypeFlag templateType = TemplateTypeFlag.All);
        static AllOfTypeDelegate s_editorAllOfTypeFunc;

        public static void AssignEditorDelegate(AllOfTypeDelegate func) {
            s_editorAllOfTypeFunc = func;
        }

        public static IEnumerable<T> EditorGetAllOfType<T>(TemplateTypeFlag templateType = TemplateTypeFlag.All) where T : ITemplate {
            return EditorGetAllOfType(typeof(T), templateType: templateType).Cast<T>();
        }

        public static List<ITemplate> EditorGetAllOfType(Type type, List<ITemplate> results = null, bool exactType = false, TemplateTypeFlag templateType = TemplateTypeFlag.All) {
            return s_editorAllOfTypeFunc.Invoke(type, results, exactType, templateType);
        }
    }
}