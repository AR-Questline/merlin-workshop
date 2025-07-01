using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class LogicParser {
        public static void Equal(Equal equal, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(equal.comparison)} = {script.Variable(equal.a)} == {script.Variable(equal.b)};");
        }
        public static void NotEqual(NotEqual notEqual, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(notEqual.comparison)} = {script.Variable(notEqual.a)} != {script.Variable(notEqual.b)};");
        }

        public static void Greater(Greater greater, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(greater.comparison)} = {script.Variable(greater.a)} > {script.Variable(greater.b)};");
        }
        
        public static void GreaterOrEqual(GreaterOrEqual greater, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(greater.comparison)} = {script.Variable(greater.a)} >= {script.Variable(greater.b)};");
        }
        
        public static void Less(Less greater, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(greater.comparison)} = {script.Variable(greater.a)} < {script.Variable(greater.b)};");
        }
        
        public static void LessOrEqual(LessOrEqual greater, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(greater.comparison)} = {script.Variable(greater.a)} <= {script.Variable(greater.b)};");
        }

        public static void And(And and, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(and.result)} = {script.Variable(and.a)} && {script.Variable(and.b)};");
        }
        
        public static void Or(Or and, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(and.result)} = {script.Variable(and.a)} || {script.Variable(and.b)};");
        }
        
        public static void ExclusiveOr(ExclusiveOr and, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(and.result)} = {script.Variable(and.a)} ^ {script.Variable(and.b)};");
        }

        public static void Negate(Negate negate, FunctionScript script) {
            script.AddFlow($"bool {script.Variable(negate.output)} = !{script.Variable(negate.input)};");
        }
    }
}