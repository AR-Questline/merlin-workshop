using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;

namespace Awaken.TG.Editor.VisualScripting.Parsing.Scripts {
    public class UnitScript : Script {
        string _code;
        string _name;
        string _namespace;
        GraphInput _invokeInput;
        GraphOutput _invokeOutput;
        
        public UnitScript(string template) {
            _code = File.ReadAllText(template);
        }

        public void Replace(string keyword, string code) {
            _code = _code.Replace(keyword, code);
        }
        public void Replace(string keyword, int indent, IEnumerable<string> code) {
            _code = _code.Replace(keyword, string.Join($"\n{new string('\t', indent)}", code));
        }
        
        public void SetName(string name, string space) {
            _name = name;
            _namespace = space;
        }
        public void SetInvoke(GraphInput input, GraphOutput output) {
            _invokeInput = input;
            _invokeOutput = output;
        }

        public void Create() {
            FunctionScript invoke = FunctionMaker.Make(_invokeInput, _invokeOutput);
            foreach (var u in invoke.Usings) {
                AddUsing(u);
            }
            foreach (var u in UnitMaker.DefaultUsings) {
                AddUsing(u);
            }

            Replace("#CATEGORY#", _namespace[30..].Replace(".", "/"));
            Replace("#NAMESPACE#", _namespace);
            Replace("#NAME#", _name);
            Replace("#INPUT_FIELDS#", 2, InputFields(_invokeInput));
            Replace("#OUTPUT_FIELDS#", 2, OutputFields(_invokeOutput));
            Replace("#INPUT_DEFINITION#", 3, InputDefinition(_invokeInput));
            Replace("#OUTPUT_DEFINITION#", 3, OutputDefinition(_invokeOutput));
            Replace("#INPUT_GET#", 3, InputGet(_invokeInput));
            Replace("#OUTPUT_SET#", 3, OutputSet(_invokeOutput));
            Replace("#INVOKE_PARAMS#", FunctionMaker.AdditionalArguments(InvokeParams(_invokeInput, _invokeOutput)));
            Replace("#INVOKE_DEFINITION#", 2, InvokeDefinition(invoke));
            Replace("#USING#", 0, invoke.Usings);
            Replace("#ASYNC#", invoke.IsAsync ? "async " : string.Empty);

            var path = string.Join("/", UnitMaker.PathToCodeDirectory, _namespace[10..]).Replace(".", "/");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            path += $"/{_name}.cs";
            if (!File.Exists(path)) {
                File.Create(path).Close();
            }
                
            File.WriteAllText(path, _code);
        }
        
        IEnumerable<string> InputFields(GraphInput input) {
            return input.valueOutputs.Select(v => $"ValueInput {v.key};");
        }
        IEnumerable<string> OutputFields(GraphOutput output) {
            return output.valueInputs.Select(v => $"ValueOutput {v.key};");
        }

        IEnumerable<string> InputDefinition(GraphInput input) {
            return input.valueOutputs.Select(v => $"{v.key} = ValueInput<{Type(v.type)}>(\"{v.key}\");");
        }
        IEnumerable<string> OutputDefinition(GraphOutput output) {
            return output.valueInputs.Select(v => $"{v.key} = ValueOutput<{Type(v.type)}>(\"{v.key}\");");
        }

        IEnumerable<string> InputGet(GraphInput input) {
            return input.valueOutputs.Select(v => $"{Type(v.type)} _{v.key} = flow.GetValue<{Type(v.type)}>({v.key});");
        }
        IEnumerable<string> OutputSet(GraphOutput output) {
            return output.valueInputs.Select(v => $"flow.SetValue({v.key}, _{v.key});");
        }

        IEnumerable<string> InvokeParams(GraphInput input, GraphOutput output) {
            foreach (var v in input.valueOutputs) {
                yield return $"_{v.key}";
            }
            foreach (var v in output.valueInputs) {
                yield return $"out {Type(v.type)} _{v.key}";
            }
        }
        IEnumerable<string> InvokeDefinition(FunctionScript script) {
            yield return script.Header + " {";
            foreach (var line in script.Body) {
                yield return line;
            }
            yield return "}";
        }
    }
}