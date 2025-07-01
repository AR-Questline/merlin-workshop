using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.VisualScripts.States;
using Unity.VisualScripting;
using UnityEditor;
using static Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Editor.VisualScripting {
    public static class VGConverterUtils {
        public static IEnumerable<ScriptGraphAsset> AllScriptGraphs(bool printPath = false) {
            var scriptGuids = AssetDatabase.FindAssets("t:ScriptGraphAsset");
            foreach (var guid in scriptGuids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (printPath) {
                    Important?.Info(path);
                }
                yield return  AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(path);
            }
        }

        public static IEnumerable<StateGraphAsset> AllStateGraphs(bool printPath = false) {
            var stateGraphs = AssetDatabase.FindAssets("t:StateGraphAsset");
            foreach (var guid in stateGraphs) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (printPath) {
                    Important?.Info(path);
                }
                yield return  AssetDatabase.LoadAssetAtPath<StateGraphAsset>(path);
            }
        }

        public static IEnumerable<IUnit> AllUnits(bool printPath = false) {
            return AllScriptGraphs(printPath).SelectMany(asset => asset.graph.units)
                .Concat(AllStateGraphs(printPath).SelectMany(asset => AllUnitsOwnedBy(asset.graph)));
        }

        static IEnumerable<IUnit> AllUnitsOwnedBy(StateGraph graph) {
            return graph.states.SelectMany(state => state switch {
                SuperState super => super.nest.source == GraphSource.Embed ? AllUnitsOwnedBy(super.nest.embed) : Enumerable.Empty<IUnit>(),
                FlowState flow => flow.nest.source == GraphSource.Embed ? flow.nest.embed.units : Enumerable.Empty<IUnit>(),
                AnyState => Enumerable.Empty<IUnit>(),
                ARStateUnit => Enumerable.Empty<IUnit>(),
                _ => throw new Exception($"Processing units of {state.GetType()} not initialized"),
            });
        }
    }
}