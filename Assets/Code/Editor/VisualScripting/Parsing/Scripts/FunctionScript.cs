using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.VisualScripting.Parsing.Scripts {
    public class FunctionScript : Script {
        HashSet<IUnit> _calledUnits = new();
        Dictionary<string, IUnit> _unitByVariable = new();
        Dictionary<Type, string> _defaultValuesByType = new();

        List<string> _flow = new();

        public int IndentLevel { get; set; }

        public IEnumerable<string> Usings => _using;
        public bool IsAsync => _async;
        public string Header { get; set; }
        public IEnumerable<string> Body => _flow;


        public FunctionScript(IUnit input) {
            AddToCalled(input);
            foreach (var arg in input.valueOutputs) {
                _unitByVariable.Add(arg.key, input);
            }

            IndentLevel = 1;
        }

        public void AddFlow(string flow) {
            _flow.Add($"{new string('\t', IndentLevel)}{flow}");
        }

        public bool WasCalled(IUnit unit) {
            return _calledUnits.Contains(unit);
        }

        public void AddToCalled(IUnit unit) {
            _calledUnits.Add(unit);
        }

        public string Variable(IUnitValuePort port) {
            switch (port) {
                case ValueOutput output:
                    var unit = output.unit;
                    if (!WasCalled(unit)) Call(unit);

                    string namePrefix = output.key;
                    if (namePrefix.StartsWith("&")) {
                        namePrefix = namePrefix[1..];
                    }

                    string name = namePrefix;
                    int index = 1;
                    while (_unitByVariable.TryGetValue(name, out IUnit u)) {
                        if (u == unit) {
                            return name;
                        }

                        name = $"{namePrefix}_{index++}";
                    }

                    _unitByVariable.Add(name, unit);
                    return name;

                case ValueInput input:
                    var valueFrom = input.connection?.source;
                    if (valueFrom == null) {
                        return DefaultInput(input);
                    } else {
                        return valueFrom.unit switch {
                            This => ThisValue(input),
                            Null => "null",
                            _ => Variable(valueFrom)
                        };
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        string DefaultInput(ValueInput input) {
            input.unit.defaultValues.TryGetValue(input.key, out object defaultValue);
            if (defaultValue != null) {
                return FunctionMaker.ValueOf(input.type, defaultValue, this);
            }
            return ThisValue(input);
        }

        string ThisValue(ValueInput input) {
            if (input.nullMeansSelf) {
                if (input.type == typeof(GameObject)) {
                    return "gameObject";
                } else if (input.type == typeof(Transform)) {
                    return "gameObject.transform";
                }
            }

            Log.Important?.Info(" > [Parsing] Used default value for: " + input.key + " in: " + input.graph.title);
            if (!_defaultValuesByType.TryGetValue(input.type, out string value)) {
                value = $"default{input.type.Name}";
                AddFlow(input.type.IsSubclassOf(typeof(Component))
                    ? $"{Type(input.type)} {value} = gameObject.GetComponent<{Type(input.type)}>();"
                    : $"{Type(input.type)} {value} = default;");
                _defaultValuesByType.Add(input.type, value);
            }

            return value;
        }

        public string Type(ValueOutput output) {
            if (output.type != typeof(object)) {
                return Type(output.type);
            }

            var connectedTypes = output.validConnections.Select(c => c.destination.type);
            var type = connectedTypes.FirstOrDefault(t => t != typeof(object)) ?? typeof(object);
            return Type(type);
        }

        public void Call(IUnit unit) {
            FunctionMaker.Call(unit, this);
        }
    }
}