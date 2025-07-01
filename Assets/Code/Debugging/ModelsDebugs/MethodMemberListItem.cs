using System;
using System.Linq;
using System.Reflection;

namespace Awaken.TG.Debugging.ModelsDebugs {
    public class MethodMemberListItem {
        public MethodInfo MethodInfo { get; private set; }
        [UnityEngine.Scripting.Preserve] public Type ReturnType => MethodInfo.ReturnType;
        public string Name { get; private set; }
        public bool Callable { get; private set; }
        public int ParametersCount { get; private set; }
        public Parameter[] Parameters { get; private set; }

        public MethodMemberListItem(MethodInfo methodInfo) {
            MethodInfo = methodInfo;
            Name = MethodInfo.ReflectedType?.Name + "." + MethodInfo.Name;
            DefineCallable();
        }

        public object TryCall(object target) {
            if (Callable) {
                if ((Parameters?.Length ?? 0) == 0) {
                    return MethodInfo.Invoke(target, new object[]{});
                }

                if (CheckParameters()) {
                    return MethodInfo.Invoke(target, Parameters.Select(p => p.Value).ToArray());
                }
            }

            return null;
        }

        bool CheckParameters() {
            return Parameters.All(p => CheckPrimitive(p) || CheckReference(p));
            
            bool CheckPrimitive(Parameter parameter) {
                return parameter.IsPrimitive && parameter.Value != null && parameter.Value.GetType() == parameter.ParameterType;
            }
            
            bool CheckReference(Parameter parameter) {
                return !parameter.IsPrimitive && (parameter.Value == null || parameter.Value.GetType() == parameter.ParameterType);
            }
        }

        void DefineCallable() {
            var parameters = MethodInfo.GetParameters();
            ParametersCount = parameters.Length;

            if (ParametersCount == 0) {
                Callable = true;
                return;
            }

            if (parameters.All(p => p.HasDefaultValue || p.ParameterType.IsPrimitive || p.ParameterType == typeof(string))) {
                Callable = true;
                Parameters = parameters.Select(p => new Parameter(p)).ToArray();
                return;
            }

            Callable = false;
        }
    }

    public class Parameter {
        public string Name { get; }
        public Type ParameterType { get; }
        public bool IsPrimitive { get; }
        public bool IsString { get; }
        public object Value { get; set; }
        
        public Parameter(ParameterInfo parameterInfo) {
            Name = parameterInfo.Name;
            ParameterType = parameterInfo.ParameterType;
            IsPrimitive = ParameterType.IsPrimitive;
            IsString = ParameterType == typeof(string);
            if (parameterInfo.HasDefaultValue) {
                Value = parameterInfo.DefaultValue;
            } else if(ParameterType.IsValueType) {
                Value = Activator.CreateInstance(ParameterType);
            } else if (ParameterType == typeof(string)) {
                Value = "";
            }
        }
    }
}