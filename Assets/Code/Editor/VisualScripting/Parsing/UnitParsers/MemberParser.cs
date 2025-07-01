using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Editor.VisualScripting.Parsing.Scripts;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Parsing.UnitParsers {
    public static class MemberParser {
        public static void Get(GetMember getMember, FunctionScript script) {
            string targetOrNamespace = getMember.target == null ? script.Type(getMember.getter.info.DeclaringType) : script.Variable(getMember.target);
            script.AddFlow($"{script.Type(getMember.getter.type)} {script.Variable(getMember.value)} = {targetOrNamespace}.{getMember.getter.name};");
        }

        public static void Set(SetMember setMember, FunctionScript script) {
            string targetOrNamespace = setMember.target == null ? script.Type(setMember.setter.info.DeclaringType) : script.Variable(setMember.target);
            script.AddFlow($"{targetOrNamespace}.{setMember.setter.name} = {script.Variable(setMember.input)};");
            if (setMember.output.connections.Any()) {
                script.AddFlow($"{script.Variable(setMember.output)} = {script.Variable(setMember.input)};");
            }
        }
        
        public static void Invoke(InvokeMember invokeMember, FunctionScript script) {
            StringBuilder callBuilder = new();
            
            switch (invokeMember.member.source) {
                case Member.Source.Constructor:
                    CallConstructor(callBuilder, invokeMember, script);
                    break;
                case Member.Source.Method:
                    CallMethod(callBuilder, invokeMember, script);
                    break;
                default:
                    throw new NotImplementedException();
            }

            script.AddFlow(callBuilder.ToString());
        }

        static void CallConstructor(StringBuilder callBuilder, InvokeMember invokeMember, FunctionScript script) {
            callBuilder.Append(script.Type(invokeMember.result.type));
            callBuilder.Append(" ");
            callBuilder.Append(script.Variable(invokeMember.result));
            callBuilder.Append(" = new ");
            callBuilder.Append(script.Type(invokeMember.member.type));
            AppendArguments(callBuilder, invokeMember.inputParameters, invokeMember.outputParameters, script);
            callBuilder.Append(";");
        }

        static void CallMethod(StringBuilder callBuilder, InvokeMember invokeMember, FunctionScript script) {
            if (invokeMember.member.isGettable) {
                callBuilder.Append(script.Type(invokeMember.result.type));
                callBuilder.Append(" ");
                callBuilder.Append(script.Variable(invokeMember.result));
                callBuilder.Append(" = ");
            }

            if (invokeMember.member.methodInfo.IsStatic) {
                callBuilder.Append(script.Type(invokeMember.member.methodInfo.ReflectedType));
            } else {
                callBuilder.Append(script.Variable(invokeMember.target));
            }
            
            callBuilder.Append(".");
            callBuilder.Append(invokeMember.member.name);
            AppendArguments(callBuilder, invokeMember.inputParameters, invokeMember.outputParameters, script);
        }

        static void AppendArguments(StringBuilder builder, Dictionary<int, ValueInput> inParameters, Dictionary<int, ValueOutput> outParameters, FunctionScript script) {
            builder.Append("(");
            for (int i = 0; i < inParameters.Count + outParameters.Count; i++) {
                if (i > 0) {
                    builder.Append(", ");
                }
                
                if (inParameters.TryGetValue(i, out ValueInput input)) {
                    builder.Append(script.Variable(input));
                } else if (outParameters.TryGetValue(i, out ValueOutput output)) {
                    builder.Append($"out {script.Type(output.type)} {script.Variable(output)}");
                } else {
                    throw new Exception("Cannot find argument");
                }
            }
            builder.Append(");");
        }
    }
}