using System.Globalization;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.Utility.Debugging;
using TMPro;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Stories.Debugging.DebugSetupComponents {
    public class DebugSetStat : MonoBehaviour, IDebugComponent {
        
        public TextMeshProUGUI textComponent;
        public TMP_InputField inputField;

        CEditorHasStats _element;

        public void Init(Story story, CEditorHasStats element) {
            _element = element;
            textComponent.text = element.Summary();
            
            Stat stat = ExtractStat(story, element);
            inputField.text = stat.ModifiedValue.ToString(CultureInfo.InvariantCulture);
        }

        public void Apply(Story story) {
            if (!float.TryParse(inputField.text, out float value)) {
                Log.Important?.Error($"Invalid number for {textComponent.text}");
                return;
            }

            Stat stat = ExtractStat(story, _element);
            float diff = value - stat.ModifiedValue;
            stat.IncreaseBy(diff);
        }

        Stat ExtractStat(Story story, CEditorHasStats element) {
            StoryRole role = StoryRole.DefaultForStat(element.StatType);
            IWithStats statOwner = role.RetrieveFrom<IWithStats>(story);
            Stat stat = statOwner.Stat(element.StatType);
            return stat;
        }
    }
}