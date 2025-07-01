using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;

namespace Awaken.TG.Editor.VisualScripting.Parsing.Scripts {
    public class Script {
        protected HashSet<string> _using = new();
        protected bool _async = false;

        public void AddUsing(string u) {
            _using.Add($"using {u};");
        }

        public void AddAsync() {
            _async = true;
        }
        
        public string Type(Type type) {
            if (type == typeof(object)) {
                return "object";
            } else if (type == typeof(UnityEngine.Object)) {
                AddUsing("Object = UnityEngine.Object");
                return "Object";
            } else if (type == typeof(Random)) {
                AddUsing("Random = UnityEngine.Random");
                return "Random";
            } else {
                AddUsing(type.Namespace);
                return Regex.Replace(type.Name, @"\`.+", $"<{string.Join(", ", type.GetGenericArguments().Select(Type))}>");
            }
        }
        public string Type<T>() {
            return Type(typeof(T));
        }
    }
}