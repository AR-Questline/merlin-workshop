using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.Extensions;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.SimpleTools {
    public class PatchNotes : ScriptableObject, ISerializationCallbackReceiver {
        const string OperationsGroup = "Operations";
        const string AddPathNoteGroup = OperationsGroup + "/Add Patch Note";
        const string SerializationGroup = "Serialization";
        
        const string Path = "Assets/Data/PatchNotes.asset";
        static PatchNotes s_instance;
        public static PatchNotes Instance => s_instance ??= AssetDatabase.LoadAssetAtPath<PatchNotes>(Path);
        
        static readonly string DirectoryPath = Application.dataPath + "/../PatchNotes";

        readonly List<PatchNote> _notes = new();
        
        [ShowInInspector, HideLabel, OnValueChanged(nameof(RefreshFilter))] 
        Filter _filter;
        
        [ShowInInspector, ShowIf(nameof(FilterByVersion)), OnValueChanged(nameof(RefreshFilter))] 
        string _filterVersion;
        
        [ShowInInspector] 
        [TableList(AlwaysExpanded = true, ShowPaging = true, NumberOfItemsPerPage = 8, CellPadding = 10, IsReadOnly = true)] 
        List<PatchNote> _filteredNotes;
        
        bool FilterByVersion => _filter == Filter.ByVersion;
        
        // === Operations
        
        [BoxGroup(AddPathNoteGroup), ShowInInspector, HideLabel, Multiline] string _pathNoteToAdd;
        [BoxGroup(AddPathNoteGroup), Button(ButtonSizes.Medium, Name = "Add")]
        void AddPatchNote() {
            if (_pathNoteToAdd.IsNullOrWhitespace()) return;
            _notes.Add(new PatchNote(_pathNoteToAdd, GitUtils.GetUserName()));
            _pathNoteToAdd = string.Empty;
            RefreshFilter();
            SerializeNotes();
        }

        [BoxGroup(OperationsGroup), Button]
        void ExportPatchNotes(bool withAuthors = false, bool assignCurrentVersion = false) {
            string version = Application.version;
            bool changed = false;

            List<PatchNote> versionedNotes = new();
            foreach (var note in _notes) {
                if (note.HasVersion) {
                    if (note.Version == version) {
                        versionedNotes.Add(note);
                    }
                } else {
                    if (assignCurrentVersion) {
                        note.SetVersion(version);
                        changed = true;
                    }
                    versionedNotes.Add(note);
                }
            }

            StringBuilder builder = new("PatchNotes ");
            builder.Append(version);
            builder.AppendLine();
            foreach (var note in versionedNotes) {
                builder.Append("- ");
                if (withAuthors) {
                    builder.Append("[");
                    builder.Append(note.Author.Trim());
                    builder.Append("] ");
                }
                builder.Append(note.Note.Trim());
                builder.AppendLine();
            }

            Log.Important?.Info(builder.ToString());
            
            RefreshFilter();
            
            if (changed) {
                SerializeNotes();
            }
        }
        
        void RemovePatchNote(PatchNote patchNote) {
            _notes.Remove(patchNote);
            RefreshFilter();
            SerializeNotes();
        }
        
        void RefreshFilter() {
            _filteredNotes ??= new();
            _filteredNotes.Clear();
            _filteredNotes.AddRange(_filter switch {
                Filter.LastOne => GetLastOne(_notes),
                Filter.ByVersion => _notes.Where(note => note.MatchVersion(_filterVersion)),
                Filter.NeedTesting => _notes.Where(static note => note.NeedTesting),
                Filter.All => _notes,
                _ => Enumerable.Empty<PatchNote>()
            });

            static IEnumerable<PatchNote> GetLastOne(IEnumerable<PatchNote> allNotes) {
                var note = allNotes.MaxBy(static note => note.CreationTime ?? new DateTime(0), true);
                return note != null ? note.Yield() : Enumerable.Empty<PatchNote>();
            }
        }

        void SerializeNotes(bool forceAll = false) {
            if (!Directory.Exists(DirectoryPath)) {
                Directory.CreateDirectory(DirectoryPath);
            }
            foreach (var note in _notes) {
                if (forceAll || note.IsDirty()) {
                    var path = note.FilePath;
                    if (File.Exists(path)) {
                        File.Delete(path);
                    }

                    var json = JsonConvert.SerializeObject(note, Formatting.Indented);
                    File.WriteAllText(path, json);
                    note.SetNotDirty();
                }
            }
        }

        void DeserializeNotes() {
            _notes.Clear();
            foreach (var file in Directory.GetFiles(DirectoryPath)) {
                try {
                    var json = File.ReadAllText(file);
                    var note = JsonConvert.DeserializeObject<PatchNote>(json);
                    note.SetName(new FileInfo(file).Name);
                    _notes.Add(note);
                } catch (Exception e) {
                    Log.Important?.Error($"Failed to deserialize patch note: {file}. {e}");
                }
            }
            RefreshFilter();
        }
        
        [Button, HorizontalGroup(SerializationGroup)]
        void ForceDeserialization() {
            bool proceed = EditorUtility.DisplayDialog(
                "PatchNotes deserialization",
                "It will override any not saved changes.\nUse only when you are certain what you are doing.\nDo you want to proceed?",
                "Deserialize", "Stop"
            );
            if (proceed) {
                DeserializeNotes();
            }
        }

        [Button, HorizontalGroup(SerializationGroup)]
        void ForceSerialization() {
            bool proceed = EditorUtility.DisplayDialog(
                "PatchNotes serialization",
                "It will override any hand-made changes on disk.\nDo you want to proceed?",
                "Serialize", "Stop"
            );
            if (proceed) {
                SerializeNotes(true);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() => SerializeNotes();
        void ISerializationCallbackReceiver.OnAfterDeserialize() => DeserializeNotes();

        [Serializable]
        class PatchNote {
            const string MessageGroup = "Message";
            const string MetadataGroup = "Metadata";

            [JsonProperty, SerializeField, VerticalGroup(MessageGroup), HideLabel, Multiline] string note;
            [JsonProperty, SerializeField, VerticalGroup(MessageGroup), HideLabel] string author;

            [JsonProperty, SerializeField, VerticalGroup(MetadataGroup), ShowIf(nameof(HasVersion)), DisplayAsString] string version;
            
            [JsonProperty, SerializeField, VerticalGroup(MetadataGroup), InlineProperty, ShowIf(nameof(WasCreated))] 
            TagWithDate created;

            [JsonProperty, SerializeField, VerticalGroup(MetadataGroup), InlineProperty, ShowIf(nameof(WasTested))] 
            TagWithDate tested;

            string _name;
            bool _dirty;
            
            public PatchNote(string note, string author) {
                this.note = note;
                this.author = author;
                created.Set(DateTime.UtcNow);
                SetDirty();
            }

            [JsonIgnore] public string Note => note;
            [JsonIgnore] public string Author => author;
            
            [JsonIgnore] public bool HasVersion => !version.IsNullOrWhitespace();
            [JsonIgnore] public bool WasCreated => created.IsValid;
            [JsonIgnore] public bool WasTested => tested.IsValid;

            [JsonIgnore] public bool NeedTesting => !WasTested;

            [JsonIgnore] public string Version => version;
            [JsonIgnore] public DateTime? CreationTime => created.Date;

            [JsonIgnore] public string FilePath => DirectoryPath + "/" + Name;
             string Name {
                get {
                    if (_name.IsNullOrWhitespace()) {
                        var name = PathUtils.ValidFileName(note);
                        if (name.Length > 20) {
                            name = name[..20];
                        }
                        _name = $"{created.GetDateHash()}_{name}.data";
                    }
                    return _name;
                }
            }

            public void SetVersion(string version) {
                this.version = version;
                SetDirty();
            }
            
            public bool MatchVersion(string version) {
                return version.IsNullOrWhitespace() ? Version.IsNullOrWhitespace() : Version == version;
            }

            [VerticalGroup(MetadataGroup), Button, ShowIf(nameof(NeedTesting))] 
            void Tested() {
                tested.Set(DateTime.UtcNow);
                SetDirty();
                PatchNotes.Instance.SerializeNotes();
            }

            [VerticalGroup(MetadataGroup), Button, GUIColor(1.5f, 0, 0)]
            void Remove() {
                if (EditorUtility.DisplayDialog("Remove patch note", "This action cannot be undone. Are you certain?", "Yes", "No")) {
                    File.Delete(FilePath);
                    PatchNotes.Instance.RemovePatchNote(this);
                }
            }
            
            public void SetName(string name) {
                _name = name;
            }

            public bool IsDirty() {
                return _dirty;
            }
            public void SetDirty() {
                _dirty = true;
            }
            public void SetNotDirty() {
                _dirty = false;
            }

            [Serializable]
            struct TagWithDate {
                [JsonProperty, SerializeField, HideInInspector] long ticks;
                
                string _text;
                
                [ShowInInspector, ShowIf(nameof(IsValid)), DisplayAsString, HideLabel] 
                string Text { get {
                    if (_text.IsNullOrWhitespace()) {
                        _text = GetText();
                    }
                    return _text;
                }}

                [JsonIgnore] public bool IsValid => ticks > 0;
                [JsonIgnore] public DateTime? Date => IsValid ? new DateTime(ticks) : null;
                public string GetDateHash() {
                    return new DateTime(ticks).ToString("yyMMdd_hhmmss");
                }

                public void Set(DateTime time) {
                    ticks = time.Ticks;
                    _text = GetText();
                }

                public void Clear() {
                    ticks = 0;
                    _text = null;
                }
                
                string GetText() => new DateTime(ticks).ToString(CultureInfo.InvariantCulture);
            }
        }

        enum Filter {
            All,
            ByVersion,
            NeedTesting,
            LastOne,
        }
    }
}