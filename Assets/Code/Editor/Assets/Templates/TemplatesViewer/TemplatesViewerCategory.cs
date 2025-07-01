using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Sirenix.Utilities;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer {
    [Serializable]
    public class TemplatesViewerCategory {
        [SerializeField] List<string> types = new();
        [SerializeField] string name;
        [SerializeField] MultiColumnHeaderState columns;
        [SerializeField] List<string> tagFilters = new();
        [SerializeField] string nameFilter;
        [SerializeField] TemplatesViewerConfig owner;

        public MultiColumnHeaderState Columns => columns;
        public TemplatesViewerConfig Owner => owner;

        public string Name {
            get => name;
            set => name = value;
        }

        public string NameFilter {
            get => nameFilter;
            set => nameFilter = value;
        }

        public List<string> Types => types;
        public List<string> TagFilters => tagFilters;
        
        public void Init(TemplatesViewerConfig owner) {
            this.owner = owner;
            columns = new MultiColumnHeaderState(ColumnsFactory.GetDefault(this));
        }
        
        public void AddColumn(MultiColumnHeaderState.Column newColumn) {
            var newColumns = columns.columns.Append(newColumn).ToArray();
            columns = new MultiColumnHeaderState(newColumns);
        }

        public void RemoveColumn(MultiColumnHeaderState.Column column) {
            var newColumns = columns.columns.Where(c => c != column).ToArray();
            columns = new MultiColumnHeaderState(newColumns);
            owner.RemoveColumn(column.userData);
        }

        public void RemoveAllColumns() {
            foreach (MultiColumnHeaderState.Column column in columns.columns) {
                owner.RemoveColumn(column.userData);
            }
        }

        public bool FilterTemplate(ITemplate template) {
            return FilterTags(template) && FilterName(template);
        }

        bool FilterTags(ITemplate template) {
            if (tagFilters.IsNullOrEmpty()) {
                return true;
            }

            if (template is ITagged tagged) {
                IEnumerable<string> satisfiedTags = tagFilters.Except(tagged.Tags);
                return !satisfiedTags.Any();
            }
            return false;
        }

        bool FilterName(ITemplate template) {
            if (nameFilter.IsNullOrWhitespace()) {
                return true;
            }
            string templateName = TemplatesUtil.TemplateToObject(template).name;
            return Regex.Match(templateName, nameFilter).Success;
        }

        public void RefreshColumns() {
            foreach (MultiColumnHeaderState.Column column in columns.columns) {
                TemplatesViewerColumn columnData = owner.GetColumn(column.userData);
                columnData.Refresh();
            }
        }
    }
}