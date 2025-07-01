using System;
using System.Linq;
using Awaken.TG.Main.Memories;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging {
    public class PrefMemoryWindow : OdinEditorWindow {
        [MenuItem("TG/Saves/Prefs memory", false, 3000)]
        static void ShowWindow() {
            var window = EditorWindow.GetWindow<PrefMemoryWindow>();
            window.Show();
        }

        [OnValueChanged(nameof(Refresh)), ShowInInspector]
        string _search;
        [TableList(ShowPaging = true, IsReadOnly = true), ShowInInspector] 
        Values[] _values;

        protected override void Initialize() {
            base.Initialize();
            Refresh();
        }

        void Refresh() {
            bool hasSearch = !string.IsNullOrWhiteSpace(_search);
            _values = PrefMemory.Keys
                .Where(k => !hasSearch || k.IndexOf(_search, StringComparison.InvariantCultureIgnoreCase) > -1)
                .Select(k => new Values{
                    key = k,
                    value = PrefMemory.Get(k),
                    window = this,
                })
                .ToArray();
        }

        class Values {
            public string key;
            public object value;
            [HideInInspector]
            public PrefMemoryWindow window;

            [Button]
            void Delete() {
                PrefMemory.DeleteKey(key);
                window.Refresh();
            }
        }
    }
}