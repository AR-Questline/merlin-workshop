using System;
using Awaken.Utility.Editor.SearchableMenu;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.MoreGUI {
    public static class AREditorPopup {
        const string CommandName = "AREditorPopup.MenuItemClicked";
        
        static readonly int Id = nameof(AREditorPopup).GetHashCode();
        static readonly GUIContent MixedValueContent = EditorGUIUtility.TrTextContent("—", "Mixed Values");
        static readonly GUIStyle Style = EditorStyles.popup;

        static Instance s_instance;
        
        public static int Draw(in Rect rect, GUIContent label, int selected, GUIContent[] paths, GUIContent[] names) {
            return Draw(rect, label, selected, new EagerData(paths, names));
        }
        
        public static int Draw(in Rect rect, int selected, GUIContent[] paths, GUIContent[] names) {
            return Draw(rect, selected, new EagerData(paths, names));
        }
        
        public static int Draw(in Rect rect, int selected, GUIContent[] paths, GUIContent[] names, string defaultValue) {
            return Draw(rect, selected, new EagerData(paths, names), defaultValue);
        }
        
        public static int Draw(in Rect rect, GUIContent label, int selected, Func<GUIContent[]> paths, Func<int, GUIContent> name) {
            return Draw(rect, label, selected, new LazyData(paths, name));
        }
        
        public static int Draw(in Rect rect, int selected, Func<GUIContent[]> paths, Func<int, GUIContent> name) {
            return Draw(rect, selected, new LazyData(paths, name));
        }

        public static int Draw<TSource>(in Rect rect, GUIContent label, int selected, TSource[] sources , Func<TSource, GUIContent> path, Func<TSource, GUIContent> name) {
            return Draw(rect, label, selected, new SourcedLazyData<TSource>(sources, path, name));
        }
        
        public static int Draw<TSource>(in Rect rect, int selected, TSource[] sources , Func<TSource, GUIContent> path, Func<TSource, GUIContent> name) {
            return Draw(rect, selected, new SourcedLazyData<TSource>(sources, path, name));
        }
        
        static int Draw<TData>(in Rect rect, GUIContent label, int selected, in TData data) where TData : struct, IData {
            int controlID = GUIUtility.GetControlID(Id, FocusType.Keyboard, rect);
            var contentRect = EditorGUI.PrefixLabel(rect, controlID, label);
            return Draw(contentRect, selected, data, controlID);
        }
        
        static int Draw<TData>(in Rect rect, int selected, in TData data, string defaultValue = "") where TData : struct, IData {
            int controlID = GUIUtility.GetControlID(Id, FocusType.Keyboard, rect);
            return Draw(rect, selected, data, controlID, defaultValue);
        }

        static int Draw<TData>(in Rect rect, int selected, in TData data, int controlID, string defaultValue = "") where TData : struct, IData {
            var evt = Event.current;
            switch (evt.type) {
                case EventType.MouseDown when evt.button == 0 && rect.Contains(evt.mousePosition): 
                case EventType.KeyDown when evt.character == ' ' && GUIUtility.keyboardControl == controlID:
                    s_instance = new Instance(controlID);

                    GUIContent[] paths = data.GetPaths();
                    
                    SearchableMenuPresenter wnd = ScriptableObject.CreateInstance<SearchableMenuPresenter>();
                    foreach (var path in paths) {
                        GUIContent temp = path;
                        wnd.AddEntry(path.text, () => {
                            int index = Array.IndexOf(paths, temp);
                            s_instance.Select(index);
                        });
                    }
                    
                    if (!string.IsNullOrEmpty(defaultValue)) {
                        wnd.AddSeparator();
                        wnd.AddEntry(defaultValue, () => s_instance.Select(-1));
                    }
                    
                    wnd.ShowAtCursorPosition();
                    
                    GUIUtility.keyboardControl = controlID;
                    evt.Use();
                    break;
                case EventType.Repaint:
                    bool hover = rect.Contains(evt.mousePosition);
                    GUIContent selectedName;
                    if (selected < 0) {
                        selectedName = string.IsNullOrEmpty(defaultValue) ? GUIContent.none : new GUIContent(defaultValue);
                    } else {
                        selectedName = data.Name(selected);
                    }
                    var content = EditorGUI.showMixedValue ? MixedValueContent : selectedName;
                    Style.Draw(rect, content, controlID, false, hover);
                    break;
                case EventType.ExecuteCommand:
                    if (evt.commandName == CommandName && s_instance.Id == controlID) {
                        GUI.changed = EditorGUI.showMixedValue || selected != s_instance.Selected;
                        selected = s_instance.Selected;
                        s_instance = null;
                        evt.Use();
                    }
                    break;
            }
            return selected;
        }
        
        interface IData {
            [Pure] GUIContent[] GetPaths();
            [Pure] GUIContent Name(int index);
        }

        readonly struct EagerData : IData {
            readonly GUIContent[] _paths;
            readonly GUIContent[] _names;

            public EagerData(GUIContent[] paths, GUIContent[] names) {
                _paths = paths;
                _names = names;
            }

            public GUIContent[] GetPaths() => _paths;
            public GUIContent Name(int index) => _names[index];
        }

        readonly struct LazyData : IData {
            readonly Func<GUIContent[]> _paths;
            readonly Func<int, GUIContent> _name;
            
            public LazyData(Func<GUIContent[]> paths, Func<int, GUIContent> name) {
                _paths = paths;
                _name = name;
            }
            
            public GUIContent[] GetPaths() => _paths();
            public readonly GUIContent Name(int index) => _name(index);
        }

        readonly struct SourcedLazyData<TSource> : IData {
            readonly TSource[] _sources;
            readonly Func<TSource, GUIContent> _path;
            readonly Func<TSource, GUIContent> _name;

            public SourcedLazyData(TSource[] sources, Func<TSource, GUIContent> path, Func<TSource, GUIContent> name) {
                _sources = sources;
                _path = path;
                _name = name;
            }

            public GUIContent[] GetPaths() { 
                var contents = new GUIContent[_sources.Length];
                for (int i = 0; i < _sources.Length; i++) {
                    contents[i] = _path(_sources[i]);
                }
                return contents;
            }

            public GUIContent Name(int index) {
                return index >= 0 && index < _sources.Length ? _name(_sources[index]) : GUIContent.none;
            }
        }

        class Instance {
            public int Id { get; }
            public EditorWindow Window { get; }
            public int Selected { get; private set; }

            public Instance(int id) {
                this.Id = id;
                Window = EditorWindow.focusedWindow;
            }

            public void Select(int index) {
                Selected = index;
                if (Window) {
                    Window.SendEvent(EditorGUIUtility.CommandEvent(CommandName));
                }
            }
        }
    }
}