using System;
using System.Collections.Generic;
using Awaken.TG.Main.Stories.Core;
using Sirenix.OdinInspector;
using UnityEditor.Graphs;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    public abstract class StoryGraphUtilityTool<TResult, TResultEntry>
        where TResult : IResult<TResultEntry>, new()
        where TResultEntry : IResultEntry {
        protected const int InputSectionOrder = -11;
        protected const int ButtonSectionOrder = -10;
        protected const int ResultSectionOrder = 0;
        
        protected const string InputSectionName = "Input";
        protected const string ResultSectionName = "Results";
        
        [field: BoxGroup(ResultSectionName, centerLabel: true), HideLabel, PropertyOrder(ResultSectionOrder)]
        [field: SerializeField, TableList(IsReadOnly = true, AlwaysExpanded = true, DefaultMinColumnWidth = 180, ShowPaging = true, NumberOfItemsPerPage = 100), Searchable(Recursive = false, FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        [field: InfoBox("Search is using graph name to find results \nUse fuzzy search approach so results may not be 100% accurate - more characters you provide, more accurate results will be", InfoMessageType.None)]
        List<TResultEntry> Results { get; set; } = new();
        
        [field: BoxGroup(ResultSectionName, centerLabel: true), PropertyOrder(ResultSectionOrder - 1)]
        [field: SerializeField, InlineProperty, HideLabel]
        protected TResult ResultController { get; private set; } = new();

        protected StoryGraphUtilityTool() { }
        
        public virtual void Execute() {
            if (!Validate()) return;

            ResultController.Clear();
            ExecuteTool();
            Results = ResultController.GatherResults();
        }
        
        [EnableIf(nameof(Validate)), GUIColor(0.102f, 0.569f, 0.769f)]
        [Button("Execute", ButtonSizes.Large), PropertyOrder(ButtonSectionOrder), PropertySpace(10, 10)]
        protected virtual void ExecuteButton() {
            Execute();
        }
        
        protected abstract bool Validate();
        protected abstract void ExecuteTool();
    }
    
    [Serializable]
    public class DefaultResult<T> : IResult<T> where T : IResultEntry {
        protected List<T> Results { get; private set; } = new();
        
        public List<T> GatherResults() => new (Results);
        public void Feed(IEnumerable<T> entries) => Results.AddRange(entries);
        public void Feed(T entry) => Results.Add(entry);
        public void Clear() => Results.Clear();
    }
    
    [Serializable]
    public class SearchResult<T> : DefaultResult<T> where T : IResultEntry {
        [SerializeField, ReadOnly, ShowIf("@!string.IsNullOrEmpty(" + nameof(currentlySearched) + ")"), PropertySpace(0, 10)]
        string currentlySearched;

        public void SetCurrentlySearched(string searched) => currentlySearched = searched;
    }
    
    [Serializable]
    public class DefaultResultEntry : IResultEntry {
        protected const string ACTIONS_SECTION_NAME = "Actions";
        
        [SerializeField, ReadOnly, TableColumnWidth(330)] NodeGraph targetGraph;
        [SerializeField, ReadOnly, HideInTables] StoryNode targetNode;
        [ShowIf("@!string.IsNullOrEmpty(" + nameof(notes) + ")"), PropertyOrder(1000)]
        [SerializeField, ReadOnly] string notes;

        public NodeGraph TargetGraph => targetGraph;
        
        public DefaultResultEntry(NodeGraph graph, StoryNode node , string resultNote = "") {
            targetGraph = graph;
            targetNode = node;
            notes = resultNote;
        }
            
        [ShowIf("@" + nameof(targetGraph) + "!= null")]
        [HorizontalGroup(ACTIONS_SECTION_NAME), Button("Jump to node", ButtonSizes.Small), TableColumnWidth(80)]
        protected virtual void Ping() {
            NodeEditorWindow.Open(targetGraph).CenterOnNode(targetNode);
        }
    }
}