using System;
using System.Linq;
using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class VariableParser {
        public static void GetVariable(GetVariable getVariable, FunctionScript script) {
            string typename = script.Type(getVariable.value);
            if (getVariable.specifyFallback) {
                throw new Exception("Fallback value not implemented");
            } else {
                script.AddFlow($"{typename} {script.Variable(getVariable.value)} = ({typename}){GetVariables(getVariable.kind, getVariable.@object, script)}.Get({script.Variable(getVariable.name)});");
            }
        }

        public static void SetVariable(SetVariable setVariable, FunctionScript script) {
            script.AddFlow($"{GetVariables(setVariable.kind, setVariable.@object, script)}.Set({script.Variable(setVariable.name)}, {script.Variable(setVariable.input)});");
            if (setVariable.output.validConnections.Any()) {
                script.AddFlow($"{script.Type(setVariable.output)} {script.Variable(setVariable.output)} = {script.Variable(setVariable.input)};");
            }
        }

        public static void IsVariableDefined(IsVariableDefined defined, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(defined.isVariableDefined)} = {GetVariables(defined.kind, defined.@object, script)}.IsDefined({script.Variable(defined.name)});");
        }
        
        static string GetVariables(VariableKind kind, ValueInput @object, FunctionScript script) {
            return kind switch {
                VariableKind.Flow => "flow.variables",
                VariableKind.Graph => "Variables.Graph(flow.stack)",
                VariableKind.Object => $"Variables.Object({script.Variable(@object)})",
                VariableKind.Scene => "Variables.Scene(flow.stack.scene)",
                VariableKind.Application => "Variables.Application",
                VariableKind.Saved => "Variables.Saved",
                _ => throw new UnexpectedEnumValueException<VariableKind>(kind)
            };
        }

    }
}