using System;
using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class SpecialParser {
        public static void Literal(Literal literal, FunctionScript script) {
            script.AddFlow($"{script.Type(literal.output)} {script.Variable(literal.output)} = {FunctionMaker.ValueOf(literal.type, literal.value, script)};");
        }

        public static void Cast(Cast cast, FunctionScript script) {
            if (!cast.defaultValues.TryGetValue(cast.type.key, out object type) || type == null) {
                throw new Exception("Cast must have defined type");
            }
            string name = script.Type(type as Type);
            script.AddFlow($"{name} {script.Variable(cast.output)} = ({name}) {script.Variable(cast.input)};");
        }

        public static void This(This t, FunctionScript script) {
            // parsing This unit is handled in FunctionScript.Variable method
        }
        
        public static void Null(Null n, FunctionScript script){
            // parsing Null unit is handled in FunctionScript.Variable method
        }

        public static void GetOwnerInParent(GetOwnerInParent getOwner, FunctionScript script) {
            script.AddUsing("Awaken.TG.Main.Character");
            //script.AddFlow();
        }
    }
}