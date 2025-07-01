using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class ControlParser {
        
        // == Flow
        
        public static IEnumerable<IUnit> CallAndGetNextInFlow_ForEach(ForEach uForEach, FunctionScript script) {
            if (uForEach.body.connection?.destinationExists ?? false) {
                bool hasIndex = uForEach.currentIndex.connections.Any();
                if (hasIndex) {
                    script.AddFlow($"int {script.Variable(uForEach.currentIndex)} = 0");
                }
                script.AddFlow($"foreach (var {script.Variable(uForEach.currentItem)} in {script.Variable(uForEach.collection)})" + " {");
                script.IndentLevel++;
                
                yield return uForEach.body.connection.destination.unit;
                if (hasIndex) {
                    script.AddFlow($"{script.Variable(uForEach.currentIndex)}++");
                }
                
                script.IndentLevel--;
                script.AddFlow("}");
            }
            if (uForEach.exit.connection?.destinationExists ?? false) {
                yield return uForEach.exit.connection.destination.unit;
            }
        }

        public static IEnumerable<IUnit> CallAndGetNextInFlow_For(For uFor, FunctionScript script) {
            if (uFor.body.connection?.destinationExists ?? false) {
                string index = script.Variable(uFor.currentIndex);
                string first = script.Variable(uFor.firstIndex);
                string last = script.Variable(uFor.lastIndex);
                string step = script.Variable(uFor.step);
                string condition = $"{step}>0 ? {index}<{last} : {index}>{last}";
                script.AddFlow($"for(int {index} = {first}; {condition}; {index} += {step})" + " {");
                script.IndentLevel++;
                
                yield return uFor.body.connection.destination.unit;
                
                script.IndentLevel--;
                script.AddFlow("}");
            }
            if (uFor.exit.connection?.destinationExists ?? false) {
                yield return uFor.exit.connection.destination.unit;
            }
        }

        public static IEnumerable<IUnit> CallAndGetNextInFlow_While(While uWhile, FunctionScript script) {
            if (uWhile.body.connection?.destinationExists ?? false) {
                script.AddFlow("while(true) {");
                script.IndentLevel++;
                
                script.AddFlow($"if (!{script.Variable(uWhile.condition)}) break;");
                yield return uWhile.body.connection.destination.unit;
                
                script.IndentLevel--;
                script.AddFlow("}");
            }
            if (uWhile.exit.connection?.destinationExists ?? false) {
                yield return uWhile.exit.connection.destination.unit;
            }
        }
        
        public static IEnumerable<IUnit> CallAndGetNextInFlow_If(If uIf, FunctionScript script) {
            if (uIf.ifTrue.connection?.destinationExists ?? false) {
                script.AddFlow($"if ({script.Variable(uIf.condition)})" + " {");
                script.IndentLevel++;
                yield return uIf.ifTrue.connection.destination.unit;
                script.IndentLevel--;
                if (uIf.ifFalse.connection?.destinationExists ?? false) {
                    script.AddFlow("} else {");
                    script.IndentLevel++;
                    yield return uIf.ifFalse.connection.destination.unit;
                    script.IndentLevel--;
                }
                script.AddFlow("}");
            } else if (uIf.ifFalse.connection?.destinationExists ?? false) {
                script.AddFlow($"if (!{script.Variable(uIf.condition)})" + " {");
                script.IndentLevel++;
                yield return uIf.ifFalse.connection.destination.unit;
                script.IndentLevel--;
                script.AddFlow("}");
            }
        }

        public static IEnumerable<IUnit> CallAndGetNextInFlow_IfWithExit(IfWithExit uIf, FunctionScript script) {
            if (uIf.ifTrue.connection?.destinationExists ?? false) {
                script.AddFlow($"if ({script.Variable(uIf.condition)})" + " {");
                script.IndentLevel++;
                yield return uIf.ifTrue.connection.destination.unit;
                script.IndentLevel--;
                if (uIf.ifFalse.connection?.destinationExists ?? false) {
                    script.AddFlow("} else {");
                    script.IndentLevel++;
                    yield return uIf.ifFalse.connection.destination.unit;
                    script.IndentLevel--;
                }
                script.AddFlow("}");
            } else if (uIf.ifFalse.connection?.destinationExists ?? false) {
                script.AddFlow($"if (!{script.Variable(uIf.condition)})" + " {");
                script.IndentLevel++;
                yield return uIf.ifFalse.connection.destination.unit;
                script.IndentLevel--;
                script.AddFlow("}");
            }

            if (uIf.exit.connection?.destinationExists ?? false) {
                yield return uIf.exit.connection.destination.unit;
            }
        }
        
        
        public static IEnumerable<IUnit> CallAndGetNextInFlow_SwitchOnInteger(SwitchOnInteger uSwitch, FunctionScript script) {
            script.AddFlow($"switch ({script.Variable(uSwitch.selector)})" + " {");
            script.IndentLevel++;
            
            foreach (int option in uSwitch.options) {
                if (uSwitch.branches.First(b => b.Key.Equals(option)).Value.connection?.destinationExists ?? false) {
                    script.AddFlow($"case {option}:");
                    script.IndentLevel++;
                    
                    yield return uSwitch.branches.First(b => b.Key.Equals(option)).Value.connection.destination.unit;
                    script.AddFlow("break;");
                    
                    script.IndentLevel--;
                }
            }

            if (uSwitch.@default.connection?.destinationExists ?? false) {
                script.AddFlow("default:");
                script.IndentLevel++;

                yield return uSwitch.@default.connection.destination.unit;
                script.AddFlow("break;");

                script.IndentLevel--;
            }

            script.IndentLevel--;
            script.AddFlow("}");
        }

        public static IEnumerable<IUnit> CallAndGetNextInFLow_Sequence(Sequence sequence, FunctionScript script) {
            foreach (var output in sequence.multiOutputs) {
                if (output.connection?.destinationExists ?? false) {
                    yield return output.connection.destination.unit;
                }
            }
        }

        public static IEnumerable<IUnit> CallAndGetNextInFlow_NullCheck(NullCheck nullCheck, FunctionScript script) {
            if (nullCheck.ifNotNull.connection?.destinationExists ?? false) {
                script.AddFlow($"if ({script.Variable(nullCheck.input)} != null)" + " {");
                script.IndentLevel++;
                yield return nullCheck.ifNotNull.connection.destination.unit;
                script.IndentLevel--;
                if (nullCheck.ifNull.connection?.destinationExists ?? false) {
                    script.AddFlow("} else {");
                    script.IndentLevel++;
                    yield return nullCheck.ifNull.connection.destination.unit;
                    script.IndentLevel--;
                }
                script.AddFlow("}");
            } else if (nullCheck.ifNull.connection?.destinationExists ?? false) {
                script.AddFlow($"if ({script.Variable(nullCheck.input)} == null)" + " {");
                script.IndentLevel++;
                yield return nullCheck.ifNull.connection.destination.unit;
                script.IndentLevel--;
                script.AddFlow("}");
            }
        }

        public static IEnumerable<IUnit> CallAndGetNextInFlow_NullCheckWithExit(NullCheckWithExit nullCheck, FunctionScript script) {
            if (nullCheck.ifNotNull.connection?.destinationExists ?? false) {
                script.AddFlow($"if ({script.Variable(nullCheck.input)} != null)" + " {");
                script.IndentLevel++;
                yield return nullCheck.ifNotNull.connection.destination.unit;
                script.IndentLevel--;
                if (nullCheck.ifNull.connection?.destinationExists ?? false) {
                    script.AddFlow("} else {");
                    script.IndentLevel++;
                    yield return nullCheck.ifNull.connection.destination.unit;
                    script.IndentLevel--;
                }
                script.AddFlow("}");
            } else if (nullCheck.ifNull.connection?.destinationExists ?? false) {
                script.AddFlow($"if ({script.Variable(nullCheck.input)} == null)" + " {");
                script.IndentLevel++;
                yield return nullCheck.ifNull.connection.destination.unit;
                script.IndentLevel--;
                script.AddFlow("}");
            }

            if (nullCheck.exit.connection?.destinationExists ?? false) {
                yield return nullCheck.exit.connection.destination.unit;
            }
        }

        public static IEnumerable<IUnit> CallAndGetNextInFlow_Return(Return r, FunctionScript script) {
            script.AddFlow("return;");
            return Enumerable.Empty<IUnit>();
        }

        public static IEnumerable<IUnit> CallAndGetNextInFlow_Continue(Continue c, FunctionScript script) {
            script.AddFlow("continue;");
            return Enumerable.Empty<IUnit>();
        }

        public static IEnumerable<IUnit> CallAndGetNextInFlow_Break(Break b, FunctionScript script) {
            script.AddFlow("break;");
            return Enumerable.Empty<IUnit>();
            
        }
        
        public static IEnumerable<IUnit> CallAndGetNextInFlow_Output(GraphOutput output, FunctionScript script) {
            foreach (var arg in output.valueInputs) {
                script.AddFlow($"{arg.key} = {script.Variable(arg)};");
            }
            return Enumerable.Empty<IUnit>();
        }
        
        // == Data

        public static void SelectUnit(SelectUnit select, FunctionScript script) {
            script.AddFlow($"{script.Type(select.selection)} {script.Variable(select.selection)} = {script.Variable(select.condition)} ? {script.Variable(select.ifTrue)} : {script.Variable(select.ifFalse)};");
        }

        public static void NullCoalesce(NullCoalesce coalesce, FunctionScript script) {
            script.AddFlow($"{script.Type(coalesce.result)} {script.Variable(coalesce.result)} = {script.Variable(coalesce.input)} ?? {script.Variable(coalesce.fallback)};");
        }
    }
}