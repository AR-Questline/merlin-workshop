using System.Globalization;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.Debugging;
using TMPro;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Stories.Debugging.DebugSetupComponents {
    public class DebugSetVariable : MonoBehaviour, IDebugComponent {
        
        public TextMeshProUGUI textComponent;
        public TextMeshProUGUI variableAName;
        public TextMeshProUGUI variableBName;
        public TMP_InputField inputFieldA;
        public TMP_InputField inputFieldB;

        CEditorVariable _element;

        public void Init(Story story, CEditorVariable element) {
            _element = element;
            textComponent.text = element.Summary();
            
            InitVariable(story, element.variableA, variableAName, inputFieldA);
            InitVariable(story, element.variableB, variableBName, inputFieldB);
        }

        void InitVariable(Story story, Variable variable, TextMeshProUGUI label, TMP_InputField input) {
            label.text = variable.Label();
            float value = variable.GetValue(story, _element.context, _element.contexts);
            input.text = value.ToString(CultureInfo.InvariantCulture);
        }

        public void Apply(Story story) {
            ApplyVariable(_element.variableA, inputFieldA);
            ApplyVariable(_element.variableB, inputFieldB);
        }

        void ApplyVariable(Variable variable, TMP_InputField input) {
            if (!float.TryParse(input.text, out float value)) {
                Log.Important?.Error($"Invalid number for {variable.Label()}");
                return;
            }

            variable.debugOverride = value;
        }
    }
}