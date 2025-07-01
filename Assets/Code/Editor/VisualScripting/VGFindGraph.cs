using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.VisualScripts.Units.General;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Localization.Plugins.XLIFF.V20;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.VisualScripting {
    public class VGFindGraph : OdinEditorWindow {

        Dictionary<Object, UnitPointers> _results = new();
        
        [TableList(ShowPaging = true), PropertySpace(0, 15)] 
        public List<Result> results = new();
        
        [Button]
        void FindSubgraph(ScriptGraphAsset graph) {
            FindUnits(unit => unit is SubgraphUnit subgraph && subgraph.nest.source == GraphSource.Macro && subgraph.nest.macro == graph);
        }
        
        void FindUnitOfType(Type type, string unitDefaultValue) {
            FindUnits(u => (unitDefaultValue.IsNullOrWhitespace() || u.defaultValues.Values.Contains(unitDefaultValue)) && u.GetType() == type);
        }

        [LabelWidth(140)]
        [ShowInInspector, TypeDrawerSettings(BaseType = typeof(Unit), Filter = TypeInclusionFilter.IncludeConcreteTypes)]
        [FoldoutGroup("typeSearch", GroupName = "Find Units of Type"), HorizontalGroup("typeSearch/horizontal", MaxWidth = 400)]
        Type _unitTypeToSearch;
        
        [LabelWidth(160)]
        [ShowInInspector, HorizontalGroup("typeSearch/horizontal", MaxWidth = 400)]
        string _methodNameToSearch;
        
        [Button("Search"), FoldoutGroup("typeSearch"), HorizontalGroup("typeSearch/horizontal2", MaxWidth = 400)]
        void FindUnitsOfType() {
            if (_unitTypeToSearch == null) {
                Log.Important?.Error("No type selected to search for Unit");
                return;
            }
            FindUnitOfType(_unitTypeToSearch, _methodNameToSearch);
        }
        
        [Button]
        void FindMemberUnitOfType(Type type, string methodName) {
            FindUnits(u => {
                if (u is MemberUnit memberUnit) {
                    Type memberTargetType = memberUnit.member?.targetType;
                    if (memberTargetType == null) {
                        Log.Important?.Error("MemberUnit was lost: " + memberUnit.member?.targetTypeName + " -> Graph title: " + memberUnit.graph.title);
                        return false;
                    }
                    
                    bool typeMatches = memberTargetType == type;
                    var methodInfo = memberUnit.member.methodInfo;
                    
                    if (methodInfo == null) {
                        return typeMatches && methodName.IsNullOrWhitespace();
                    }
                    
                    if (typeMatches) {
                        return methodName.IsNullOrWhitespace() || methodInfo.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase);
                    }
                }

                return false;
            });
        }

        [Button(Stretch = false, ButtonAlignment = 0), PropertyOrder(1)]
        void FindBrokenUnits() {
            FindUnits(u => {
                bool result = true;
                try {
                    result = u is MissingType 
                               or MemberUnit 
                                  and ({member: {targetType: null}} 
                                      or {member: {source: Member.Source.Method, methodInfo: null}}
                                      or {member: {source: Member.Source.Constructor, constructorInfo: null}}
                                      or {member: {source: Member.Source.Field, fieldInfo: null}}
                                      or {member: {source: Member.Source.Property, propertyInfo: null}});

                } catch (Exception e) {
                    Debug.LogException(e);
                }
                
                return result;
            });
        }
        
        [Button]
        void FindWithGuid(string guid) {
            var g = Guid.Parse(guid);
            FindUnits(u => u.guid == g);
        }

        [Button]
        void FindFromSafeGraph(string text) {
            var data = SafeGraph.Data.Parse(text);
            FindUnits(u => u.guid == data.guid);
        }

        [Button(Name = "Find Variable")]
        void FindVariable(string name, VariableUnitType type) {
            if (type == VariableUnitType.Get) {
                FindUnits(unit => unit is GetVariable v && MatchName(v.name));
            } else if (type == VariableUnitType.SafeGet) {
                FindUnits(unit => unit is SafeGetVariable v && MatchName(v.Name));
            } else if (type == VariableUnitType.Set) {
                FindUnits(unit => unit is SetVariable v && MatchName(v.name));
            } else if (type == VariableUnitType.IsDefined) {
                FindUnits(unit => unit is IsVariableDefined v && MatchName(v.name));
            } else if (type == VariableUnitType.AllUnity) {
                FindUnits(unit => unit is UnifiedVariableUnit v && MatchName(v.name));
            } else if (type == VariableUnitType.All) {
                FindUnits(unit => 
                    (unit is UnifiedVariableUnit v && MatchName(v.name)) 
                    || (unit is SafeGetVariable safe && MatchName(safe.Name))
                );
            }

            bool MatchName(ValueInput nameInput) {
                return nameInput.hasDefaultValue && (string) nameInput.unit.defaultValues[nameInput.key] == name;
            }
        }

        enum VariableUnitType {
            Get,
            SafeGet,
            Set,
            IsDefined,
            AllUnity,
            All,
        }
        
        // === Searching

        void FindUnits(Func<IUnit, bool> predicate) {
            ClearResults();
            
            foreach (var asset in AssetDatabase.FindAssets("t:ScriptGraphAsset").Select(AssetsUtils.LoadAssetByGuid<ScriptGraphAsset>)) {
                FindUnitsInFlowGraph(asset, asset.graph, GraphReference.New(asset, false), "", predicate);
            }
            foreach (var asset in AssetDatabase.FindAssets("t:StateGraphAsset").Select(AssetsUtils.LoadAssetByGuid<StateGraphAsset>)) {
                FindUnitsInStateGraph(asset, asset.graph, GraphReference.New(asset, false), "", predicate);
            }
            
            FlushResult();
        }

        void FindUnitsInStateGraph(Object owner, StateGraph stateGraph, GraphReference reference, string path, Func<IUnit, bool> predicate) {
            foreach (var state in stateGraph.states) {
                if (state is FlowState flowState && flowState.nest.source == GraphSource.Embed) {
                    FindUnitsInFlowGraph(owner, flowState.nest.embed, reference.ChildReference(flowState, false), $"{path}/{flowState.Description().title}", predicate);
                }
                if (state is SuperState superState && superState.nest.source == GraphSource.Embed) {
                    FindUnitsInStateGraph(owner, superState.nest.embed, reference.ChildReference(superState, false), $"{path}/{superState.Description().title}", predicate);
                }
            }

            foreach (var transition in stateGraph.transitions) {
                if (transition is FlowStateTransition flowStateTransition && flowStateTransition.nest.source == GraphSource.Embed) {
                    FindUnitsInFlowGraph(owner, flowStateTransition.nest.embed, reference.ChildReference(flowStateTransition, false), $"{path}/{flowStateTransition.Description().title}", predicate);
                }
            }
        }

        void FindUnitsInFlowGraph(Object owner, FlowGraph flowGraph, GraphReference reference, string path, Func<IUnit, bool> predicate) {
            var positions = new List<Vector2>();
            foreach (var unit in flowGraph.units) {
                if (predicate(unit)) {
                    positions.Add(unit.position);
                } else if(unit is SubgraphUnit subgraph && subgraph.nest.source == GraphSource.Embed) {
                    FindUnitsInFlowGraph(owner, subgraph.nest.embed, reference.ChildReference(subgraph, false), $"{path}/{subgraph.Description().title}", predicate);
                }
            }
            if (positions.Count > 0) {
                AddResult(owner, path, reference, positions);
            }
        }

        void ClearResults() {
            _results.Clear();
            results.Clear();
        }

        void AddResult(Object owner, string path, GraphReference reference, List<Vector2> positions) {
            if (!_results.TryGetValue(owner, out var pointers)) {
                pointers = new UnitPointers();
                _results.Add(owner, pointers);
            }
            foreach (var pos in positions) {
                pointers.pointers.Add(new UnitPointer(path, reference, pos));
            }
        }

        void FlushResult() {
            results.AddRange(_results.Select(pair => new Result(pair.Key, pair.Value.pointers)));
        }
        
        // == Window Creation

        [MenuItem("Assets/Visual Scripting/Find Graph")]
        static void OpenWindowSelected() {
            var window = OpenWindow();
            var graph = Selection.objects.FirstOrDefault(o => o is ScriptGraphAsset) as ScriptGraphAsset;
            if (graph != null) {
                window.FindSubgraph(graph);
            }
        }
        
        [MenuItem("TG/Visual Scripting/Find Graph")]
        static VGFindGraph OpenWindow() {
            var window = GetWindow<VGFindGraph>();
            window.Show();
            return window;
        }

        public class Result {
            
            [ShowInInspector, HideLabel, DisplayAsString(false), VerticalGroup(nameof(owner))]
            public Object owner;

            [TableList, VerticalGroup(nameof(pointers))]
            public List<UnitPointer> pointers;

            public Result(Object owner, List<UnitPointer> pointers) {
                this.owner = owner;
                this.pointers = pointers;
            }

            [Button("Ping"), VerticalGroup(nameof(owner))]
            void PingOwner() {
                EditorGUIUtility.PingObject(owner);
            }
        }
    }
    
    class UnitPointers {
        public List<UnitPointer> pointers = new();
        public int Count => pointers.Count;
    }

    [Serializable]
    public struct UnitPointer {
        [ShowInInspector, HideLabel, DisplayAsString(false), VerticalGroup(nameof(path))] 
        public string path;
        
        [HideInTables] public GraphReference reference;
        [HideInTables] public Vector2 position;
            
        public UnitPointer(string path, GraphReference reference, Vector2 position) {
            this.reference = reference;
            this.path = path;
            this.position = position;
        }
        
        [Button("Ping", ButtonSizes.Small), VerticalGroup("pointer"), TableColumnWidth(10)]
        void Ping() {
            GraphWindow.OpenActive(reference);
            GraphWindow.active.context.graph.zoom = 1f;
            GraphWindow.active.context.graph.pan = position;
        }
    }
}