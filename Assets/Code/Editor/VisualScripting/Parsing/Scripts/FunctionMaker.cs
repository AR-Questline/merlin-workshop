using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Events;
using Awaken.TG.VisualScripts.Units.Fights;
using Awaken.TG.VisualScripts.Units.Generated.Stats;
using Awaken.Utility.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Editor.VisualScripting.Parsing.Scripts {
    public static class FunctionMaker {
        public static FunctionScript Make(GraphInput from, GraphOutput to) {
            FunctionScript script = new(from);
            script.Header = DefinitionHeader(from.valueOutputs, to.valueInputs, script);
            
            ParseFlowRecursively(from, script);
            
            return script;
        }

        static string DefinitionHeader(IEnumerable<ValueOutput> inputs, IEnumerable<ValueInput> outputs, FunctionScript script) {
            var inArgs = inputs.Select(i => $"{script.Type(i.type)} {i.key}");
            var outArgs = outputs.Select(o => $"out {script.Type(o.type)} {o.key}");
            return $"public #ASYNC#static void Invoke({script.Type(typeof(GameObject))} gameObject, {script.Type(typeof(Flow))} flow{AdditionalArguments(inArgs.Concat(outArgs))})";
        }

        public static string AdditionalArguments(IEnumerable<string> args) {
            StringBuilder builder = new StringBuilder();
            foreach (var arg in args) {
                builder.Append($", {arg}");
            }
            return builder.ToString();
        }

        public static string ValueOf(Type type, object value, FunctionScript script) {
            if (type == typeof(string)) {
                return $"\"{value}\"";
            } else if (type == typeof(bool)) {
                return (bool) value ? "true" : "false";
            } else if (type == typeof(Vector2)) {
                return $"new {script.Type<Vector2>()}{DoublesToFloats(value)}";
            } else if (type == typeof(Vector3)) {
                return $"new {script.Type<Vector3>()}{DoublesToFloats(value)}";
            } else if (type == typeof(Vector4)) {
                return $"new {script.Type<Vector4>()}{DoublesToFloats(value)}";
            } else if (type == typeof(float)) {
                return $"{value.ToString().Replace(",", ".")}F";
            } else if (type == typeof(Color)) {
                return $"new {script.Type<Color>()}{DoublesToFloats(value)[4..]}";
            } else if (type == typeof(Quaternion)) {
                return $"new {script.Type<Quaternion>()}{DoublesToFloats(value)}";
            } else if (type == typeof(Type)) {
                return $"typeof({script.Type(value as Type)})";
            } else {
                return value.ToString();
            }
        }
        static string DoublesToFloats(object args) {
            return Regex.Replace(args.ToString(), "[0-9]+[.][0-9]+", @"$&F");
        }
        
        static void ParseFlowRecursively(IUnit unit, FunctionScript script) {
            foreach (var next in FunctionMaker.CallAndGetNextInFlow(unit, script)) {
                ParseFlowRecursively(next, script);
            }
        }

        static IEnumerable<IUnit> CallAndGetNextInFlow(IUnit unit, FunctionScript script) {
            if (unit is not GraphInput && unit.controlInputs[0].connections.Count() > 1) {
                throw new Exception($"{unit.GetType().Name} has more than one input connected to control input. Consider using IfWithExit instead of If");
            }
            
            script.AddToCalled(unit);
            
            switch (unit) {
                case If uIf:
                    return ControlParser.CallAndGetNextInFlow_If(uIf, script);
                case IfWithExit uIf:
                    return ControlParser.CallAndGetNextInFlow_IfWithExit(uIf, script);
                case ForEach uForEach:
                    return ControlParser.CallAndGetNextInFlow_ForEach(uForEach, script);
                case For uFor:
                    return ControlParser.CallAndGetNextInFlow_For(uFor, script);
                case While uWhile:
                    return ControlParser.CallAndGetNextInFlow_While(uWhile, script);
                case NullCheck uNullCheck:
                    return ControlParser.CallAndGetNextInFlow_NullCheck(uNullCheck, script);
                case NullCheckWithExit uNullCheck:
                    return ControlParser.CallAndGetNextInFlow_NullCheckWithExit(uNullCheck, script);
                case Return r:
                    return ControlParser.CallAndGetNextInFlow_Return(r, script);
                case Continue c:
                    return ControlParser.CallAndGetNextInFlow_Continue(c, script);
                case Break b:
                    return ControlParser.CallAndGetNextInFlow_Break(b, script);
                case GraphOutput output:
                    return ControlParser.CallAndGetNextInFlow_Output(output, script);
                case SwitchOnInteger uSwitch:
                    return ControlParser.CallAndGetNextInFlow_SwitchOnInteger(uSwitch, script);
                case Sequence sequence:
                    return ControlParser.CallAndGetNextInFLow_Sequence(sequence, script);

                default:
                    Call(unit, script);
                    if (unit.controlOutputs.Any() && (unit.controlOutputs[0].connection?.destinationExists ?? false)) {
                        return MoreLinq.Yield(unit.controlOutputs[0].connection.destination.unit);
                    } else {
                        return Enumerable.Empty<IUnit>();
                    }
            }
        }
        
        public static void Call(IUnit unit, FunctionScript script) {
            script.AddToCalled(unit);
            
            switch (unit) {
                case GraphInput:
                    break;
                
                // == Variables
                
                case GetVariable getVariable:
                    VariableParser.GetVariable(getVariable, script);
                    break;
                case SetVariable setVariable:
                    VariableParser.SetVariable(setVariable, script);
                    break;
                case IsVariableDefined defined:
                    VariableParser.IsVariableDefined(defined, script);
                    break;
                
                // == Events
                
                case TriggerCustomEvent trigger:
                    EventsParser.TriggerCustomEvent(trigger, script);
                    break;
                
                case RegisterRecurringEvent evt:
                    EventsParser.RegisterRecurringEvent(evt, script);
                    break;
                case UnregisterRecurringEvent evt:
                    EventsParser.UnregisterRecurringEvent(evt, script);
                    break;

                // == Reflections
                
                case GetMember getMember:
                    MemberParser.Get(getMember, script);
                    break;
                case SetMember setMember:
                    MemberParser.Set(setMember, script);
                    break;
                case InvokeMember invokeMember:
                    MemberParser.Invoke(invokeMember, script);
                    break;
                
                // == Math
                
                case Subtract<float> subtract:
                    MathParser.Subtract(subtract, script);
                    break;
                case Subtract<Vector2> subtract:
                    MathParser.Subtract(subtract, script);
                    break;
                case Subtract<Vector3> subtract:
                    MathParser.Subtract(subtract, script);
                    break;
                case Subtract<Vector4> subtract:
                    MathParser.Subtract(subtract, script);
                    break;
                case GenericSubtract subtract:
                    MathParser.Subtract(subtract, script);
                    break;
                
                case Add<float> add:
                    MathParser.Add(add, script);
                    break;
                case Add<Vector2> add:
                    MathParser.Add(add, script);
                    break;
                case Add<Vector3> add:
                    MathParser.Add(add, script);
                    break;
                case Add<Vector4> add:
                    MathParser.Add(add, script);
                    break;
                
                case Sum<float> sum:
                    MathParser.Sum(sum, script);
                    break;
                case Sum<Vector2> sum:
                    MathParser.Sum(sum, script);
                    break;
                case Sum<Vector3> sum:
                    MathParser.Sum(sum, script);
                    break;
                case Sum<Vector4> sum:
                    MathParser.Sum(sum, script);
                    break;
                case GenericSum sum:
                    MathParser.Sum(sum, script);
                    break;

                case Multiply<float> multiply:
                    MathParser.Multiply(multiply, script);
                    break;
                case GenericMultiply multiply:
                    MathParser.Multiply(multiply, script);
                    break;
                
                case Divide<float> divide:
                    MathParser.Divide(divide, script);
                    break;
                case GenericDivide divide:
                    MathParser.Divide(divide, script);
                    break;
                
                case Modulo<float> modulo:
                    MathParser.Modulo(modulo, script);
                    break;
                case GenericModulo modulo:
                    MathParser.Modulo(modulo, script);
                    break;
                
                case Vector3Normalize normalize:
                    MathParser.Vector3Normalize(normalize, script);
                    break;
                
                case ScalarExponentiate exponentiate:
                    MathParser.ScalarExponentiate(exponentiate, script);
                    break;
                
                case ScalarMoveTowards moveTowards:
                    MathParser.ScalarMoveTowards(moveTowards, script);
                    break;
                
                // == Lists
                
                case CreateList create:
                    ListParser.CreateList(create, script);
                    break;
                case GetListItem get:
                    ListParser.GetListItem(get, script);
                    break;
                case SetListItem set:
                    ListParser.SetListItem(set, script);
                    break;
                case AddListItem add:
                    ListParser.AddListItem(add, script);
                    break;
                case InsertListItem insert:
                    ListParser.InsertListItem(insert, script);
                    break;
                case RemoveListItem remove:
                    ListParser.RemoveListItem(remove, script);
                    break;
                case RemoveListItemAt remove:
                    ListParser.RemoveListItemAt(remove, script);
                    break;
                case CountItems count:
                    ListParser.CountItems(count, script);
                    break;
                case ClearList clearList:
                    ListParser.ClearList(clearList, script);
                    break;
                case ListContainsItem contains:
                    ListParser.ListContainsItem(contains, script);
                    break;
                case MergeLists merge:
                    ListParser.MergeLists(merge, script);
                    break;

                // == Data Flow
                
                case SelectUnit select:
                    ControlParser.SelectUnit(select, script);
                    break;
                case NullCoalesce coalesce:
                    ControlParser.NullCoalesce(coalesce, script);
                    break;

                // == Logic
                
                case Equal equal:
                    LogicParser.Equal(equal, script);
                    break;
                case NotEqual notEqual:
                    LogicParser.NotEqual(notEqual, script);
                    break;
                case Greater greater:
                    LogicParser.Greater(greater, script);
                    break;
                case GreaterOrEqual greater:
                    LogicParser.GreaterOrEqual(greater, script);
                    break;
                case Less less:
                    LogicParser.Less(less, script);
                    break;
                case LessOrEqual lessOrEqual:
                    LogicParser.LessOrEqual(lessOrEqual, script);
                    break;
                
                case And and:
                    LogicParser.And(and, script);
                    break;
                case Or or:
                    LogicParser.Or(or, script);
                    break;
                case ExclusiveOr exclusiveOr:
                    LogicParser.ExclusiveOr(exclusiveOr, script);
                    break;
                case Negate negate:
                    LogicParser.Negate(negate, script);
                    break;

                // == Nesting
                
                case SubgraphUnit subgraph:
                    NestingParser.Subgraph(subgraph, script);
                    break;
                case ARGeneratedUnit u:
                    NestingParser.ARGeneratedUnit(u, script);
                    break;
                
                // == Special
                
                case Literal literal:
                    SpecialParser.Literal(literal, script);
                    break;
                case Cast cast:
                    SpecialParser.Cast(cast, script);
                    break;
                case This t:
                    SpecialParser.This(t, script);
                    break;
                case Null n:
                    SpecialParser.Null(n, script);
                    break;

                // == Not Implemented
                
                default:
                    throw new Exception($"Parsing {unit.GetType().Name} not implemented");
            }
        }
    }
}
