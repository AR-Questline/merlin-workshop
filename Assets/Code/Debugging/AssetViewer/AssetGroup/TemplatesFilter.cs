using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Sirenix.Utilities;
using UnityEngine;

namespace Awaken.TG.Debugging.AssetViewer.AssetGroup {
    [Serializable]
    public class TemplatesFilter {

        [SerializeField] string nameRegex;
        [SerializeField] string[] components;
        [SerializeField] string[] disallowedComponents;

        public IEnumerable<T> FilterTemplates<T>(IEnumerable<T> templates) where T : Template {
            foreach (T template in templates) {
                if (template.name.Contains("DropableParent") || !nameRegex.IsNullOrWhitespace() && !Regex.Match(template.name, nameRegex).Success) {
                    continue;
                }

                if (!IsMatchingComponents(template)) {
                    continue;
                }

                yield return template;
            }
        }

        bool IsMatchingComponents(Template template) {
            if (components.IsNullOrEmpty() && disallowedComponents.IsNullOrEmpty()) {
                return true;
            }

            foreach (string componentType in components) {
                var component = template.GetComponent(componentType);
                if (component == null) {
                    return false;
                }
            }
            
            foreach (string componentType in disallowedComponents) {
                var component = template.GetComponent(componentType);
                if (component != null) {
                    return false;
                }
            }

            return true;
        }
    }
}