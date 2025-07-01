using System.Linq;
using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class NestingParser {
        public static void Subgraph(SubgraphUnit subgraph, FunctionScript script) {
            var inArgs = subgraph.valueInputs.Select(i => $"{script.Variable(i)}");
            var outArgs = subgraph.valueOutputs.Select(o => $"out {script.Type(o.type)} {script.Variable(o)}");
            string arguments = string.Join(", ", inArgs.Concat(outArgs));
            
            (string name, string space) = UnitMaker.UnitNameAndSpace(subgraph.nest.macro);
            script.AddUsing(space);
            script.AddFlow($"{name}.Invoke(gameObject, flow, {arguments});");
        }

        public static void ARGeneratedUnit(ARGeneratedUnit u, FunctionScript script) {
            var inArgs = u.valueInputs.Select(i => $"{script.Variable(i)}");
            var outArgs = u.valueOutputs.Select(o => $"out {script.Type(o.type)} {script.Variable(o)}");
            string arguments = string.Join(", ", inArgs.Concat(outArgs));
            script.AddFlow($"{script.Type(u.GetType())}.Invoke(gameObject, flow, {arguments});");
        }
    }
}