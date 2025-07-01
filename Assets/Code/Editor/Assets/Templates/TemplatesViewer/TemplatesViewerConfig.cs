using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes;
using Awaken.Utility.Collections;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer {
    
    public class TemplatesViewerConfig : ScriptableObject {
        public const string DefaultPath = "Assets/Editor Default Resources/Templates/TemplatesViewerConfig.asset";
        
        [SerializeField] List<TemplatesViewerCategory> categories = new();
        [SerializeField] List<TemplatesViewerColumn> allColumns = new();
        [SerializeField] int selectedTab = -1;
        public IEnumerable<TemplatesViewerCategory> Categories => categories;

        public int SelectedTab {
            get => selectedTab;
            set => selectedTab = value;
        }

        public static TemplatesViewerConfig LoadOrCreate(string path) {
            TemplatesViewerConfig config = AssetDatabase.LoadAssetAtPath<TemplatesViewerConfig>(path);
            return config == null ? Create(path) : config;
        }

        static TemplatesViewerConfig Create(string path) {
            TemplatesViewerConfig config = ScriptableObject.CreateInstance<TemplatesViewerConfig>();
            AssetDatabase.CreateAsset(config, path);
            EditorUtility.SetDirty(config);
            return config;
        }

        public TemplatesViewerCategory CurrentCategory() {
            if (selectedTab < 0 || selectedTab >= categories.Count) {
                return null;
            }

            return categories[selectedTab];
        }

        public void AddCategory(TemplatesViewerCategory category) {
            categories.Add(category);
        }

        public void RemoveCategory(int index) {
            categories[index].RemoveAllColumns();
            categories.RemoveAt(index);
        }

        public void AddColumn(TemplatesViewerColumn column) {
            column.ID = GetNextID();
            AssetDatabase.AddObjectToAsset(column, this);
            allColumns.Add(column);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public void RemoveColumn(int id) {
            var column = GetColumn(id);
            AssetDatabase.RemoveObjectFromAsset(column);
            DestroyImmediate(column);
            allColumns.Remove(column);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public TemplatesViewerColumn GetColumn(int id) {
            foreach (TemplatesViewerColumn templatesViewerColumn in allColumns) {
                if (templatesViewerColumn.ID == id) {
                    return templatesViewerColumn;
                }
            }
            return null;
        }

        public int GetNextID() {
            if (allColumns.IsNullOrEmpty()) {
                return 0;
            }
            var idList = allColumns.Select(c => c.ID)
                .OrderBy(s => s)
                .ToList();
            var result = Enumerable.Range(idList.Min(), idList.Count)
                .Except(idList)
                .ToList();
            
            if (result.Any()) {
                return result.First();
            }
            return idList.Last() + 1;
        }
    }
}