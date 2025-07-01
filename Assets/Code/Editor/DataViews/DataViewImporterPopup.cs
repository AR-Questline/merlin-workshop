using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.DataViews.Headers;
using Awaken.TG.Editor.DataViews.Structure;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.DataViews {
    public class DataViewImporterPopup {
        static readonly GUIContent FileLabel = new("File");
        
        string _file;

        public bool Draw(Rect rect, DataViewHeader[] headers) {
            var rects = new PropertyDrawerRects(rect);
            // _file = SirenixEditorFields.FilePathField(rects.AllocateLine(), FileLabel, _file, null, DataView.ExportDataExtension, false, false);
            rects.AllocateTop(5);
            if (GUI.Button(rects.AllocateLine(), "Import")) {
                Import(headers);
                return false;
            }
            if (GUI.Button(rects.AllocateLine(), "Cancel")) {
                return false;
            }
            return true;
        }
        
        void Import(DataViewHeader[] allHeaders) {
            using var stream = File.OpenRead(_file);
            using var reader = new StreamReader(stream);
            
            var headerEnumerator = new CharacterSeparatedLine(reader.ReadLine(), DataView.ExportDataSeparator).GetEnumerator();
            if (!headerEnumerator.MoveNext() || !headerEnumerator.Current.SequenceEqual("ID")) {
                Log.Important?.Error("First column must be ID");
                return;
            }
            
            if (!headerEnumerator.MoveNext() || !headerEnumerator.Current.SequenceEqual("Name")) {
                Log.Important?.Error("Second column must be Name");
                return;
            }

            List<DataViewHeader> headers = new(allHeaders.Length);
            
            while (headerEnumerator.MoveNext()) {
                var header = headerEnumerator.Current;
                headers.Add(null);
                foreach (var h in allHeaders) {
                    if (header.SequenceEqual(h.Name)) {
                        headers[^1] = h;
                        break;
                    }
                }
            }
            
            if (headers.All(h => h == null)) {
                Log.Important?.Error("No matching headers found");
                return;
            }
            
            while (!reader.EndOfStream) {
                var valueEnumerator = new CharacterSeparatedLine(reader.ReadLine(), DataView.ExportDataSeparator).GetEnumerator();
                IRowImporter importer = null;
                if (valueEnumerator.MoveNext()) {
                    string id = valueEnumerator.Current.ToString();
                    importer = IRowImporter.GetImporter(id);
                }

                // object name
                if (!valueEnumerator.MoveNext()) {
                    continue;
                }
                
                var dataViewSource = importer?.CreateSource();
                if (dataViewSource == null) {
                    continue;
                }
                
                int column = 0;
                while (valueEnumerator.MoveNext()) {
                    if (headers[column] is { SupportEditing: true } header) {
                        var headerMetadata = header.CreateMetadata(dataViewSource);
                        var typeMetadata = header.Type.CreateMetadata();
                        header.Import(new DataViewCell(dataViewSource, headerMetadata, typeMetadata), valueEnumerator.Current);
                        header.Type.FreeMetadata(ref typeMetadata);
                        header.FreeMetadata(ref headerMetadata);
                    }
                    column++;
                }
            }
        }

        interface IRowImporter {
            DataViewSource CreateSource();

            public static IRowImporter GetImporter(string id) {
                if (id.StartsWith("Object")) {
                    return new UnityObjectRowImporter(id);
                } else if (id.StartsWith("Quest")) {
                    return new QuestRowImporter(id);
                } else if (id.StartsWith("Loot")) {
                    // Importing loot cache data is not supported (it's readonly)
                    return null;
                } else {
                    Log.Important?.Error("Unknown ID type: " + id);
                    return null;
                }
            }
        }
        
        class UnityObjectRowImporter : IRowImporter {
            Object UnityObject { get; }
            
            public UnityObjectRowImporter(string id) {
                string guid = id.Replace("Object:", "");
                UnityObject = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
            }

            public DataViewSource CreateSource() {
                return UnityObject != null ? new DataViewSource(UnityObject) : null;
            }
        }

        class QuestRowImporter : IRowImporter {
            QuestTemplateBase _questTemplate;
            ObjectiveSpecBase _objectiveSpec;
            bool _hasObjective;

            public QuestRowImporter(string id) {
                string[] parts = id.Replace("Quest:", "").Split(':');
                string guid = parts[0];
                _questTemplate = AssetDatabase.LoadAssetAtPath<QuestTemplateBase>(AssetDatabase.GUIDToAssetPath(guid));
                if (_questTemplate == null) {
                    return;
                }
                
                if (parts.Length == 1) {
                    _hasObjective = false;
                } else {
                    _hasObjective = true;
                    string objectiveGuid = parts[1];
                    using var objectiveSpecs = _questTemplate.ObjectiveSpecs;
                    _objectiveSpec = objectiveSpecs.value.FirstOrDefault(o => o.Guid == objectiveGuid);
                }
            }

            public DataViewSource CreateSource() {
                if (_hasObjective && _questTemplate != null && _objectiveSpec != null) {
                    return new DataViewQuestObjectiveSource(_questTemplate, _objectiveSpec);
                } else if (_questTemplate != null) {
                    return new DataViewQuestSource(_questTemplate);
                } else {
                    return null;
                }
            }
        }
    }
}