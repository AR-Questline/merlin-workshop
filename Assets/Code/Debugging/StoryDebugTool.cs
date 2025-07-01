using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Debugging {
    /// <summary>
    /// Debug Editor tool for checking and modifying WorldMemory variables.
    /// </summary>
    public class StoryDebugTool : MonoBehaviour {
        Services Services => World.Services;
        
        public ValueType type;
        [Tags(TagsCategory.Flag)]
        public string flag;
        
        public string key;
        public string context = "";
        [ShowIf("@type == ValueType.Float")]
        public float valueFloat;
        [ShowIf("@type == ValueType.Bool")]
        public bool valueBool;
        [Button]
        void Set() {
            if (!string.IsNullOrWhiteSpace(flag)) {
                Services?.Get<GameplayMemory>().Context().Set(flag, true);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(key)) {
                throw new ArgumentException("Wrong key");
            }

            if (type == ValueType.Float) {
                Services?.Get<GameplayMemory>().Context(context).Set(key, valueFloat);
            } else if (type == ValueType.Bool) {
                Services?.Get<GameplayMemory>().Context(context).Set(key, valueBool);
            }
        }

        [Space(10)]
        [OnValueChanged(nameof(Refresh))]
        public string filterContext = "";
        [OnValueChanged(nameof(Refresh))]
        public string filterKey = "";
        [ShowInInspector] [TableList(ShowPaging = true, NumberOfItemsPerPage = 20, IsReadOnly = true)]
        MemoryEntry[] _regularEntries;

        IEnumerable<MemoryEntry> RegularProgress
            => Services?.Get<GameplayMemory>()
                       .FilteredContextsBy(filterContext)
                       .SelectMany(c => c.GetAll()
                                         .Where(f => !f.Key.Contains("once"))
                                         .Where(f => f.Key.Contains(filterKey))
                                         .Select(f => new MemoryEntry(f.Key, f.Value, c.Selector)));

        int _frameCounter;
        void Update() {
            if (Time.frameCount > _frameCounter + 30) {
                Refresh();
            }
        }
        
        void Refresh() {
            _regularEntries = RegularProgress?.ToArray();
            _frameCounter = Time.frameCount;
        }

        // === Helper classes
        public enum ValueType {
            Float,
            Bool,
        }

        public struct MemoryEntry {
            static readonly Regex GUIDSearch = new(@"(?:\b|_)([a-z0-9]{32})(?:\b|_)", RegexOptions.Compiled | RegexOptions.Singleline);
            
            static OnDemandCache<StringCollectionSelector, string> s_selectorNames = new(s => s.ToString());
            static OnDemandCache<string, string> s_keyNames = new(s => {
                var foundGuid = GUIDSearch.Match(s);
                if (foundGuid.Success) {
                    string foundGroup = foundGuid.Groups[1].Value;
                    s = s.Replace(foundGroup, TryTranslateTemplateGUID(foundGroup));
                }
                return s;
            });
            
            [ReadOnly, VerticalGroup("keys")] public string key;
            [ReadOnly, VerticalGroup("keys")] [UnityEngine.Scripting.Preserve] public string context;
            [ReadOnly, VerticalGroup("values"), HideLabel] public object value;

            public MemoryEntry(string key, object value, StringCollectionSelector selector) {
                this.key = s_keyNames[key];
                this.value = value;
                this.context = s_selectorNames[selector];
            }

            static string TryTranslateTemplateGUID(string guid) {
                if (guid.Length != 32) return guid;
                Object instance = TemplatesUtil.Load<ITemplate>(guid) as Object;
                
                return instance == null 
                           ? guid 
                           : instance.name;
            }
        }
    }
}